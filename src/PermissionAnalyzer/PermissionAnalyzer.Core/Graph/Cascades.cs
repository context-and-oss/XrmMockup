using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Graph;

/// <summary>
/// A plugin step or custom API that runs when an operation fires its message. <see cref="FilteredAttributes"/> are
/// the columns an Update step runs for, or <c>null</c> when it runs on every update. <see cref="Code"/> is
/// <c>null</c> for a step from the metadata whose plugin code isn't loaded. <see cref="Asynchronous"/> is set for a
/// step that runs asynchronously; <see cref="IsCustomApi"/> for the code of a custom API, which no bypass parameter
/// skips.
/// </summary>
internal sealed record CascadeStep(
    string Id,
    string Message,
    string? Table,
    string? ImpersonatingUserId,
    IReadOnlyList<string>? FilteredAttributes,
    EntryCandidate? Code,
    bool Asynchronous = false,
    bool IsCustomApi = false);

/// <summary>
/// An evaluation with the hits of every step its operations fire, and the ids of those steps, sorted.
/// </summary>
internal sealed record CascadeResult(EvaluationResult Result, IReadOnlyList<string> Fired);

/// <summary>
/// Expands an evaluation with the plugin steps and custom APIs its operations fire, and the steps those fire in
/// turn, as Dataverse runs them in the request pipeline. A fired step runs as the identity of the operation that
/// fired it, unless it impersonates. Each step is expanded once per identity and basis, which also stops cycles.
/// An operation on an unknown table only runs the steps without a table; the others are reported as unresolved.
/// An update runs an Update step filtered on columns when it sets one of them, and, with the basis
/// <see cref="RequirementBasis.FilterUnknown"/>, when the columns it sets aren't all known. A request with a bypass
/// parameter doesn't run the plugin steps it bypasses.
/// </summary>
internal sealed class Cascades(CallGraph graph, IEnumerable<CascadeStep> steps, Func<EntryCandidate, RunsAs, EntryContext> context)
{
    private readonly IReadOnlyList<CascadeStep> _steps = steps.OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
    private readonly Dictionary<(string, RunsAs), EvaluationResult> _evaluations = [];

    /// <summary>
    /// <paramref name="result"/> with everything it fires. Pass the step being expanded as <paramref name="self"/>,
    /// so it doesn't fire itself again under the same identity.
    /// </summary>
    public CascadeResult Expand(EvaluationResult result, string? self = null, RunsAs? caller = null)
    {
        if (_steps.Count == 0 || result.Triggers.Count == 0) return new CascadeResult(result, []);

        var expanded = new EvaluationResult { Truncated = result.Truncated };
        Copy(result, expanded, [], RequirementBasis.Direct);

        var fired = new SortedSet<string>(StringComparer.Ordinal);
        var done = new HashSet<(string, RunsAs, RequirementBasis)>();
        if (self is not null && caller is not null) done.Add((self, caller, RequirementBasis.Direct));

        // Breadth first, so the first path to a step is the shortest
        var queue = new Queue<(EvaluationResult Result, RequirementBasis Basis)>();
        queue.Enqueue((result, RequirementBasis.Direct));
        while (queue.Count > 0)
        {
            var (current, currentBasis) = queue.Dequeue();
            var triggers = current.Triggers
                .GroupBy(t => (Message: t.Message.ToLowerInvariant(), Table: t.Table?.ToLowerInvariant(), t.RunsAs, t.Basis, Columns: t.Columns is null ? null : string.Join(",", t.Columns), t.Bypass))
                .Select(g => g.First());
            foreach (var trigger in triggers)
            {
                if (trigger.Table == TriggerHit.AnyTable && Unmatched(trigger) is { Count: > 0 } unmatched)
                {
                    expanded.Unresolved.Add(new UnresolvedHit(
                        trigger.Message,
                        [],
                        $"may fire any of {unmatched.Count} {trigger.Message} steps, because its table isn't known",
                        trigger.RunsAs,
                        trigger.Source));
                }

                foreach (var (step, filterBasis) in Matching(trigger))
                {
                    fired.Add(step.Id);
                    var runsAs = step.ImpersonatingUserId is { } user ? RunsAs.User(user) : trigger.RunsAs;
                    var basis = Max(Max(currentBasis, trigger.Basis), filterBasis);
                    if (!done.Add((step.Id, runsAs, basis))) continue;

                    var table = trigger.Table is null ? trigger.Message : $"{trigger.Message} {trigger.Table}";
                    var path = trigger.Source.Path.Concat([table, step.Id]).ToList();
                    if (step.Code is not { } code)
                    {
                        expanded.Unresolved.Add(new UnresolvedHit(
                            trigger.Message,
                            [],
                            $"fires plugin step {step.Id}, whose code isn't analyzed",
                            runsAs,
                            new RequirementSource(trigger.Source.Location, path)));
                        continue;
                    }

                    var evaluation = Evaluate(code, runsAs);
                    Copy(evaluation, expanded, path, basis);
                    expanded.Truncated |= evaluation.Truncated;
                    queue.Enqueue((Prefixed(evaluation, path), basis));
                }
            }
        }

        return new CascadeResult(expanded, fired.ToList());
    }

    /// <summary>
    /// The steps registered for the trigger's message on its table, with the basis their filter gives. A step
    /// without a table runs on every table, so it is also the only step a trigger on an unknown table is known to
    /// run. Steps the trigger bypasses don't run.
    /// </summary>
    private IEnumerable<(CascadeStep Step, RequirementBasis Basis)> Matching(TriggerHit trigger)
    {
        foreach (var step in _steps)
        {
            if (!string.Equals(step.Message, trigger.Message, StringComparison.OrdinalIgnoreCase)) continue;
            if (step.Table is not null && !string.Equals(step.Table, trigger.Table, StringComparison.OrdinalIgnoreCase)) continue;
            if (Bypassed(step, trigger.Bypass)) continue;

            // As in XrmMockup, a filter applies to Update only
            if (step.FilteredAttributes is not { Count: > 0 } filter || !step.Message.Equals("Update", StringComparison.OrdinalIgnoreCase))
            {
                yield return (step, RequirementBasis.Direct);
            }
            else if (trigger.Columns is null)
            {
                yield return (step, RequirementBasis.FilterUnknown);
            }
            else if (trigger.Columns.Intersect(filter, StringComparer.OrdinalIgnoreCase).Any())
            {
                yield return (step, RequirementBasis.Direct);
            }
        }
    }

    /// <summary>
    /// The steps of the trigger's message that run on one table, which a trigger on an unknown table may or may not
    /// run. Firing them all would include the privileges of every such step in the solution, so they are reported
    /// instead.
    /// </summary>
    private List<CascadeStep> Unmatched(TriggerHit trigger) =>
        _steps.Where(s => s.Table is not null && string.Equals(s.Message, trigger.Message, StringComparison.OrdinalIgnoreCase) && !Bypassed(s, trigger.Bypass)).ToList();

    private static bool Bypassed(CascadeStep step, PluginBypass bypass) =>
        !step.IsCustomApi && (bypass & (step.Asynchronous ? PluginBypass.Asynchronous : PluginBypass.Synchronous)) != 0;

    private EvaluationResult Evaluate(EntryCandidate code, RunsAs runsAs)
    {
        if (!_evaluations.TryGetValue((code.Id, runsAs), out var result))
        {
            result = new Evaluator(graph, context(code, runsAs)).Evaluate(code.Handlers);
            _evaluations.Add((code.Id, runsAs), result);
        }
        return result;
    }

    /// <summary>
    /// Adds the hits of <paramref name="from"/> to <paramref name="to"/>, behind <paramref name="path"/> and with at
    /// most <paramref name="basis"/> as their basis.
    /// </summary>
    private static void Copy(EvaluationResult from, EvaluationResult to, IReadOnlyList<string> path, RequirementBasis basis)
    {
        foreach (var hit in from.Requirements) to.Requirements.Add(hit with { Basis = Max(hit.Basis, basis), Source = Prefix(hit.Source, path) });
        foreach (var hit in from.Unresolved) to.Unresolved.Add(hit with { Source = Prefix(hit.Source, path) });
        foreach (var hit in from.Assumptions) to.Assumptions.Add(hit with { Source = Prefix(hit.Source, path) });
        foreach (var hit in from.Globals) to.Globals.Add(hit with { Basis = Max(hit.Basis, basis), Source = Prefix(hit.Source, path) });
        to.Reached.UnionWith(from.Reached);
    }

    /// <summary>
    /// The triggers of <paramref name="result"/>, behind <paramref name="path"/>, to expand them in turn.
    /// </summary>
    private static EvaluationResult Prefixed(EvaluationResult result, IReadOnlyList<string> path)
    {
        var prefixed = new EvaluationResult();
        prefixed.Triggers.AddRange(result.Triggers.Select(t => t with { Source = Prefix(t.Source, path) }));
        return prefixed;
    }

    private static RequirementSource Prefix(RequirementSource source, IReadOnlyList<string> path) =>
        path.Count == 0 ? source : source with { Path = [.. path, .. source.Path] };

    private static RequirementBasis Max(RequirementBasis a, RequirementBasis b) => a > b ? a : b;
}
