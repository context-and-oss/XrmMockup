using Microsoft.CodeAnalysis;
using XrmMockup.PermissionAnalyzer.Core.Discovery;
using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Analysis;

/// <summary>
/// Builds a <see cref="Manifest"/> from one or more compilations: finds the entry points, follows the calls
/// from each and collects the privileges the reached operations need. A project that targets several
/// frameworks is added once per target; its duplicate findings merge into one.
/// </summary>
/// <param name="rootDirectory">The directory source locations are reported relative to.</param>
/// <param name="rules">
/// Rules for the requests passed to <c>Execute</c>, which replace the built-in rule for their message or add one.
/// </param>
public sealed class ManifestBuilder(string rootDirectory, IEnumerable<RequestRule>? rules = null)
{
    private const string InjectedAssumption = "service is injected";

    private readonly SourcePaths _paths = new(rootDirectory);
    private readonly RequestRules _rules = new(rules);
    private readonly CallGraph _graph = new();
    private readonly Dictionary<string, EntryCandidate> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PluginClass> _plugins = new(StringComparer.Ordinal);
    private readonly List<PluginStepRegistration> _steps = [];
    private readonly List<string> _entryMethods = [];
    private readonly HashSet<string> _customApis = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds the code in <paramref name="compilation"/>. Returns <c>false</c> when the compilation doesn't
    /// reference the Dataverse SDK, so it can't contain any operations.
    /// </summary>
    public bool Add(Compilation compilation, CancellationToken cancellationToken = default)
    {
        if (XrmSymbols.Create(compilation) is not { } symbols) return false;

        _graph.Merge(new SummaryExtractor(compilation, symbols, _paths, _rules).Extract(cancellationToken));

        var discovery = new EntryPointDiscovery(compilation, symbols, _paths).Discover(cancellationToken);
        foreach (var entry in discovery.Entries) AddEntry(entry);
        foreach (var plugin in discovery.Plugins)
        {
            var key = plugin.Type + "\n" + plugin.Assembly;
            if (_plugins.TryGetValue(key, out var existing)) _plugins[key] = existing with { HasCodeSteps = existing.HasCodeSteps || plugin.HasCodeSteps };
            else _plugins.Add(key, plugin);
        }
        return true;
    }

    /// <summary>
    /// Adds the plugin steps registered in the environment, from the XrmMockup metadata.
    /// </summary>
    public void AddPluginSteps(IEnumerable<PluginStepRegistration> steps) => _steps.AddRange(steps);

    /// <summary>
    /// Adds methods to analyze as entry points, such as <c>Contoso.Sync.Importer.Run</c>, which matches every
    /// overload, or a full id with parameter types.
    /// </summary>
    public void AddEntryMethods(IEnumerable<string> methods) => _entryMethods.AddRange(methods);

    /// <summary>
    /// Builds the manifest. Pass the tables that at least one security role mentions to fill
    /// <see cref="Manifest.NotRoleGoverned"/>; leave it <c>null</c> when no roles are known. With a
    /// <paramref name="scope"/>, only the matching entry points are analyzed and nothing is reported as unreached.
    /// </summary>
    public Manifest Build(IEnumerable<string>? roleGovernedTables = null, EntryPointSelector? scope = null)
    {
        var candidates = new Dictionary<string, EntryCandidate>(_entries, StringComparer.Ordinal);
        AddMetadataSteps(candidates);
        AddMethodEntries(candidates);

        // Messages the solution implements or registers steps on, which no rule knows, are its custom APIs
        _customApis.Clear();
        _customApis.UnionWith(candidates.Values.Select(c => c.Trigger?.Message).Concat(_steps.Select(s => s.Message)).OfType<string>());

        var entryPoints = new List<EntryPoint>();
        var reached = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in candidates.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (scope is not null && !scope.Matches(Describe(candidate, [], [], []))) continue;

            var result = new Evaluator(_graph, Context(candidate)).Evaluate(candidate.Handlers);
            reached.UnionWith(result.Reached);
            entryPoints.Add(Describe(candidate, result));
        }

        var unreached = scope is null ? Unreached(reached) : [];
        var manifest = new Manifest(entryPoints, unreached);
        if (roleGovernedTables is null) return manifest;

        var governed = new HashSet<string>(roleGovernedTables.Select(t => t.ToLowerInvariant()), StringComparer.Ordinal);
        return new Manifest(entryPoints, unreached, manifest.Tables.Where(t => !governed.Contains(t)).ToList());
    }

    private void AddEntry(EntryCandidate entry)
    {
        if (!_entries.TryGetValue(entry.Id, out var existing))
        {
            _entries.Add(entry.Id, entry);
            return;
        }

        // The same registration compiled for another target framework, or a step registered twice
        var handlers = existing.Handlers.Concat(entry.Handlers).Distinct().ToList();
        var declaration = existing.Declaration is null || entry.Declaration is not null && entry.Declaration.CompareTo(existing.Declaration) < 0
            ? entry.Declaration
            : existing.Declaration;
        _entries[entry.Id] = existing with
        {
            // Independent of the order compilations are added in
            Assembly = string.CompareOrdinal(entry.Assembly, existing.Assembly) < 0 ? entry.Assembly : existing.Assembly,
            Handlers = handlers.OrderBy(h => h.Target, StringComparer.Ordinal).ThenBy(h => h.ObjectType, StringComparer.Ordinal).ToList(),
            Declaration = declaration,
            ImpersonatingUserId = existing.ImpersonatingUserId is null || entry.ImpersonatingUserId is not null && string.CompareOrdinal(entry.ImpersonatingUserId, existing.ImpersonatingUserId) < 0
                ? entry.ImpersonatingUserId
                : existing.ImpersonatingUserId,
        };
    }

    /// <summary>
    /// Steps from the metadata. A step of a plugin that registers its steps in code adds the impersonating user
    /// to the matching code step, or runs <c>Execute</c> when no code step matches. A plain <c>IPlugin</c> gets
    /// one entry point per step, or one for the whole plugin when the metadata has no steps for it.
    /// </summary>
    private void AddMetadataSteps(Dictionary<string, EntryCandidate> candidates)
    {
        var classes = _plugins.Values.OrderBy(p => p.Type, StringComparer.Ordinal).ThenBy(p => p.Assembly, StringComparer.Ordinal).ToList();
        var stepsByClass = new Dictionary<PluginClass, List<PluginStepRegistration>>();
        foreach (var step in _steps)
        {
            var matches = classes.Where(c => c.Type == step.TypeName && step.AssemblyName is not null && c.Assembly == step.AssemblyName).ToList();
            if (matches.Count == 0) matches = classes.Where(c => c.Type == step.TypeName).ToList();
            foreach (var match in matches)
            {
                if (!stepsByClass.TryGetValue(match, out var list)) stepsByClass.Add(match, list = []);
                list.Add(step);
            }
        }

        var bases = new HashSet<string>(
            classes.SelectMany(c => _graph.Types.TryGetValue(c.TypeId, out var type) ? type.Ancestors : []),
            StringComparer.Ordinal);

        foreach (var plugin in classes)
        {
            stepsByClass.TryGetValue(plugin, out var steps);
            var handlers = plugin.Execute is null ? [] : new[] { new Handler(plugin.Execute, plugin.TypeId) };

            if (steps is null || steps.Count == 0)
            {
                // A base class of other plugins, such as a DAXIF Plugin class, isn't registered itself
                if (!plugin.HasCodeSteps && !bases.Contains(plugin.TypeId))
                {
                    var id = $"plugin:{plugin.Type}";
                    if (!candidates.ContainsKey(id))
                    {
                        candidates.Add(id, new EntryCandidate(id, EntryPointKind.Plugin, plugin.Type, plugin.Assembly, null, null, plugin.Declaration, handlers));
                    }
                }
                continue;
            }

            foreach (var step in steps)
            {
                var stage = EntryPointDiscovery.StageName(step.Stage);
                var table = string.IsNullOrEmpty(step.Table) ? null : step.Table!.ToLowerInvariant();
                var user = step.ImpersonatingUserId?.ToLowerInvariant();

                var code = plugin.HasCodeSteps
                    ? candidates.Values.FirstOrDefault(c =>
                        c.Kind == EntryPointKind.PluginStep
                        && c.Type == plugin.Type
                        && string.Equals(c.Trigger?.Message, step.Message, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(c.Trigger?.Table, table, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(c.Trigger?.Stage, stage, StringComparison.OrdinalIgnoreCase))
                    : null;

                if (code is not null)
                {
                    if (user is not null && code.ImpersonatingUserId is null) candidates[code.Id] = code with { ImpersonatingUserId = user };
                    continue;
                }

                var id = EntryPointDiscovery.StepId(plugin.Type, step.Message, table, stage);
                if (candidates.TryGetValue(id, out var existing))
                {
                    if (user is not null && existing.ImpersonatingUserId is null) candidates[id] = existing with { ImpersonatingUserId = user };
                    continue;
                }
                candidates.Add(id, new EntryCandidate(id, EntryPointKind.PluginStep, plugin.Type, plugin.Assembly, new Trigger(step.Message, table, stage), user, plugin.Declaration, handlers));
            }
        }
    }

    private void AddMethodEntries(Dictionary<string, EntryCandidate> candidates)
    {
        foreach (var method in _entryMethods)
        {
            var matches = _graph.Nodes.Values
                .Where(n => n.Id == n.Member && (n.Id == method || n.Id.StartsWith(method + "(", StringComparison.Ordinal)))
                .OrderBy(n => n.Id, StringComparer.Ordinal);
            foreach (var node in matches)
            {
                var id = $"method:{node.Id}";
                if (candidates.ContainsKey(id)) continue;
                var type = node.Type is { } typeId ? typeId : "";
                candidates.Add(id, new EntryCandidate(id, EntryPointKind.Method, type, null, null, null, node.Declaration, [new Handler(node.Id, null)]));
            }
        }
    }

    /// <summary>
    /// Method entries named in <see cref="AddEntryMethods"/> that match no method in the solution.
    /// </summary>
    public IReadOnlyList<string> UnmatchedEntryMethods() =>
        _entryMethods
            .Where(m => !_graph.Nodes.Values.Any(n => n.Id == n.Member && (n.Id == m || n.Id.StartsWith(m + "(", StringComparison.Ordinal))))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Who the entry point's code runs as. Plugins and activities run as the calling user, or the step's
    /// impersonating user; functions and other code as an application user.
    /// </summary>
    private static EntryContext Context(EntryCandidate candidate)
    {
        switch (candidate.Kind)
        {
            case EntryPointKind.Function:
                return new EntryContext(RunsAs.ApplicationUser, new Identity(RunsAs.ApplicationUser with { Assumed = true }, InjectedAssumption));

            case EntryPointKind.Method:
                // Who calls an explicit method isn't known; app code usually runs as its application user
                return new EntryContext(RunsAs.ApplicationUser with { Assumed = true }, new Identity(RunsAs.ApplicationUser with { Assumed = true }, InjectedAssumption));

            default:
                var caller = candidate.ImpersonatingUserId is { } user ? RunsAs.User(user) : RunsAs.Caller;
                return new EntryContext(caller, new Identity(caller with { Assumed = true }, InjectedAssumption));
        }
    }

    /// <summary>
    /// Methods with operations that no entry point reaches, each analyzed on its own.
    /// </summary>
    private List<MethodManifest> Unreached(HashSet<string> reached)
    {
        var evaluator = new Evaluator(_graph, new EntryContext(RunsAs.Caller, new Identity(RunsAs.Caller with { Assumed = true }, InjectedAssumption)));
        return _graph.Nodes.Values
            .Where(n => n.Accesses.Count > 0 && !reached.Contains(n.Id))
            .GroupBy(n => n.Member, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                var results = g.OrderBy(n => n.Id, StringComparer.Ordinal).Select(n => evaluator.EvaluateAlone(n, n.Display)).ToList();
                var member = _graph.Nodes.TryGetValue(g.Key, out var memberNode) ? memberNode : null;
                var declaration = member?.Declaration ?? g.Select(n => n.MemberDeclaration).Where(d => d is not null).Min();
                var (requirements, unresolved, assumptions) = Aggregate(results);
                return new MethodManifest(g.Key, declaration, requirements, unresolved, assumptions);
            })
            .ToList();
    }

    private EntryPoint Describe(EntryCandidate candidate, EvaluationResult result)
    {
        var (requirements, unresolved, assumptions) = Aggregate([result]);
        return Describe(candidate, requirements, unresolved, assumptions);
    }

    /// <summary>
    /// A request without a rule is a custom API call when the solution implements the message or registers steps on
    /// it, or the message has a publisher prefix, as custom API and custom action names do.
    /// </summary>
    private string CustomApi(string reason) =>
        RequestReasons.NoRuleMessage(reason) is { } message && (_customApis.Contains(message) || message.IndexOf('_') > 0)
            ? RequestReasons.CustomApi(message)
            : reason;

    private static EntryPoint Describe(EntryCandidate candidate, IReadOnlyList<Requirement> requirements, IReadOnlyList<UnresolvedOperation> unresolved, IReadOnlyList<Assumption> assumptions) =>
        new(
            candidate.Id,
            candidate.Kind,
            candidate.Type,
            candidate.Assembly,
            candidate.Trigger,
            candidate.ImpersonatingUserId,
            candidate.Declaration,
            requirements,
            unresolved,
            assumptions);

    /// <summary>
    /// Merges the hits of an evaluation: one requirement per table, privilege and identity, with the strongest
    /// basis and one source per location (the shortest call path), and one unresolved operation per location,
    /// reason and identity, with the privileges of each. All sorted.
    /// </summary>
    private (IReadOnlyList<Requirement>, IReadOnlyList<UnresolvedOperation>, IReadOnlyList<Assumption>) Aggregate(IEnumerable<EvaluationResult> results)
    {
        var list = results.ToList();
        var truncated = list.Any(r => r.Truncated);

        var requirements = list
            .SelectMany(r => r.Requirements)
            .GroupBy(h => (h.Table, h.Privilege, h.RunsAs))
            .Select(g => new Requirement(
                g.Key.Table,
                g.Key.Privilege,
                g.Key.RunsAs,
                g.Min(h => h.Basis),
                FirstPerLocation(g.Select(h => h.Source))))
            .OrderBy(r => r.Table, StringComparer.Ordinal)
            .ThenBy(r => r.Privilege)
            .ThenBy(r => r.RunsAs)
            .ThenBy(r => r.Basis)
            .ToList();

        var unresolved = list
            .SelectMany(r => r.Unresolved)
            .GroupBy(h => (h.Source.Location, h.Operation, Reason: CustomApi(h.Reason), h.RunsAs))
            .Select(g => g.First() with { Reason = g.Key.Reason, Privileges = g.SelectMany(h => h.Privileges).Distinct().OrderBy(p => p).ToList() })
            .OrderBy(h => h.Source.Location)
            .ThenBy(h => h.Operation, StringComparer.Ordinal)
            .ThenBy(h => h.Reason, StringComparer.Ordinal)
            .ThenBy(h => string.Join(",", h.Privileges), StringComparer.Ordinal)
            .ThenBy(h => h.RunsAs)
            .Select(h => new UnresolvedOperation(h.Operation, h.Privileges, h.Reason, h.RunsAs, h.Source))
            .ToList();

        var assumptions = list
            .SelectMany(r => r.Assumptions)
            .GroupBy(h => (h.Source.Location, h.Reason))
            .Select(g => g.First())
            .OrderBy(h => h.Source.Location)
            .ThenBy(h => h.Reason, StringComparer.Ordinal)
            .Select(h => new Assumption(h.Reason, h.Source))
            .ToList();

        if (truncated)
        {
            var location = assumptions.FirstOrDefault()?.Source ?? requirements.SelectMany(r => r.Sources).FirstOrDefault();
            if (location is not null)
            {
                assumptions.Insert(0, new Assumption($"analysis stopped after {Evaluator.MaxVisits} calls; requirements may be incomplete", location));
            }
        }

        return (requirements, unresolved, assumptions);
    }

    /// <summary>
    /// One source per location, the first found; the evaluation is breadth-first, so it has the shortest path.
    /// </summary>
    private static IReadOnlyList<RequirementSource> FirstPerLocation(IEnumerable<RequirementSource> sources)
    {
        var seen = new HashSet<SourceLocation>();
        return sources.Where(s => seen.Add(s.Location)).OrderBy(s => s).ToList();
    }
}
