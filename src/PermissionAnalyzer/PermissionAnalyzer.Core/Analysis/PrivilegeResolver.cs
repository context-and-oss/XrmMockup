using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Analysis;

/// <summary>
/// Resolves what the hits of an evaluation need that the evaluation can't know on its own: the execute privileges
/// of the custom APIs it calls, and the privileges request rules name, looked up in the privilege catalog.
/// </summary>
internal sealed class PrivilegeResolver
{
    private readonly IReadOnlyDictionary<string, PrivilegeDefinition>? _catalog;
    private readonly IReadOnlyDictionary<string, CustomApiDefinition> _catalogCustomApis;
    private readonly HashSet<string> _customApis = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EntryCandidate> _customApiCode = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="catalog">The privileges by name; <c>null</c> when there is no privilege catalog.</param>
    /// <param name="catalogCustomApis">The custom APIs in the privilege catalog, by unique name.</param>
    /// <param name="candidates">The entry points, whose custom APIs are the code of the custom APIs.</param>
    /// <param name="stepMessages">The messages of the plugin steps in the metadata.</param>
    public PrivilegeResolver(
        IReadOnlyDictionary<string, PrivilegeDefinition>? catalog,
        IReadOnlyDictionary<string, CustomApiDefinition> catalogCustomApis,
        IEnumerable<EntryCandidate> candidates,
        IEnumerable<string> stepMessages)
    {
        _catalog = catalog;
        _catalogCustomApis = catalogCustomApis;

        // Messages the solution implements or registers steps on, which no rule knows, are its custom APIs
        var list = candidates.ToList();
        _customApis.UnionWith(list.Where(c => c.Kind != EntryPointKind.Operation).Select(c => c.Trigger?.Message).Concat(stepMessages).OfType<string>());
        foreach (var api in list.Where(c => c.Kind == EntryPointKind.CustomApi).OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (api.Trigger?.Message is { } message && !_customApiCode.ContainsKey(message)) _customApiCode[message] = api;
        }
    }

    /// <summary>
    /// The hits of <paramref name="results"/>, with calls to custom APIs and privileges named by request rules
    /// resolved. Pass the entry point of a custom API as <paramref name="customApi"/> to add its own execute
    /// privilege, which its caller needs.
    /// </summary>
    public ResolvedHits Resolve(IReadOnlyList<EvaluationResult> results, EntryCandidate? customApi = null)
    {
        var hits = new ResolvedHits { Truncated = results.Any(r => r.Truncated) };
        hits.Requirements.AddRange(results.SelectMany(r => r.Requirements));
        hits.Assumptions.AddRange(results.SelectMany(r => r.Assumptions));
        foreach (var hit in results.SelectMany(r => r.Unresolved))
        {
            if (RequestReasons.NoRuleMessage(hit.Reason) is { } message && IsKnownCustomApi(message))
            {
                ExecutePrivilegeHits(message, ExecutePrivilegeOf(message), hit.RunsAs, hit.Source, hits);

                // The code of a custom API runs as a cascade, so only a custom API without code stays unresolved
                if (!_customApiCode.ContainsKey(message)) hits.Unresolved.Add(hit with { Reason = RequestReasons.CustomApiCode(message) });
                continue;
            }
            hits.Unresolved.Add(hit with { Reason = CustomApi(hit.Reason) });
        }
        foreach (var hit in results.SelectMany(r => r.Globals))
        {
            NamedPrivilegeHits(hit.Operation, hit.Privilege, hit.RunsAs, hit.Basis, hit.Source, hits);
        }
        if (customApi is { Trigger.Message: { } apiMessage, Declaration: { } declaration })
        {
            ExecutePrivilegeHits(apiMessage, ExecutePrivilegeOf(apiMessage), RunsAs.Caller, new RequirementSource(declaration, []), hits);
        }
        return hits;
    }

    /// <summary>
    /// A request without a rule is a custom API call when the solution implements the message or registers steps on
    /// it, or the message has a publisher prefix, as custom API and custom action names do.
    /// </summary>
    private string CustomApi(string reason) =>
        RequestReasons.NoRuleMessage(reason) is { } message && (_customApis.Contains(message) || message.IndexOf('_') > 0)
            ? RequestReasons.CustomApi(message)
            : reason;

    /// <summary>
    /// The code or the privilege catalog has the custom API, so what calling it needs is known.
    /// </summary>
    private bool IsKnownCustomApi(string message) => _customApiCode.ContainsKey(message) || _catalogCustomApis.ContainsKey(message);

    /// <summary>
    /// The execute privilege of a custom API: from its registration in code, else from the privilege catalog.
    /// <c>null</c> when it needs none.
    /// </summary>
    private ExecutePrivilege? ExecutePrivilegeOf(string message) =>
        (_customApiCode.TryGetValue(message, out var code) ? code.ExecutePrivilege : null)
        ?? (_catalogCustomApis.TryGetValue(message, out var api) && !string.IsNullOrEmpty(api.ExecutePrivilegeName) ? new ExecutePrivilege(api.ExecutePrivilegeName) : null);

    /// <summary>
    /// What calling the custom API <paramref name="message"/> needs for <paramref name="privilege"/>: table
    /// privileges, a privilege not tied to a table, or an unresolved operation when the privilege can't be looked up.
    /// A privilege name is only looked up in the privilege catalog, never derived from its spelling.
    /// </summary>
    private void ExecutePrivilegeHits(string message, ExecutePrivilege? privilege, RunsAs runsAs, RequirementSource source, ResolvedHits hits)
    {
        if (privilege is null) return;
        if (privilege is { Table: { } table, Privilege: { } tablePrivilege })
        {
            hits.Requirements.Add(new RequirementHit(table, tablePrivilege, runsAs, RequirementBasis.ExecutePrivilege, source));
            return;
        }

        void Unresolved(string reason) => hits.Unresolved.Add(new UnresolvedHit(message, [], reason, runsAs, source));
        if (privilege.Name is not { } name) Unresolved(RequestReasons.ExecutePrivilegeComputed(message));
        else if (_catalog is null) Unresolved(RequestReasons.PrivilegeCatalogMissing(message, name));
        else if (!_catalog.TryGetValue(name, out var definition)) Unresolved(RequestReasons.PrivilegeNotInCatalog(message, name));
        else if (!CatalogHits(definition, runsAs, RequirementBasis.ExecutePrivilege, source, hits)) Unresolved(RequestReasons.PrivilegeWithoutTablePrivilege(message, name));
    }

    /// <summary>
    /// What <paramref name="operation"/> needs for <paramref name="privilege"/>, a privilege its request rule names.
    /// Without a privilege catalog it is taken as a privilege not tied to a table; with one, it is looked up there,
    /// as it may be tied to tables instead.
    /// </summary>
    private void NamedPrivilegeHits(string operation, string privilege, RunsAs runsAs, RequirementBasis basis, RequirementSource source, ResolvedHits hits)
    {
        void Unresolved(string reason) => hits.Unresolved.Add(new UnresolvedHit(operation, [], reason, runsAs, source));
        if (_catalog is null) hits.Globals.Add(new GlobalHit(privilege, runsAs, basis, source));
        else if (!_catalog.TryGetValue(privilege, out var definition)) Unresolved(RequestReasons.NamedPrivilegeNotInCatalog(operation, privilege));
        else if (!CatalogHits(definition, runsAs, basis, source, hits)) Unresolved(RequestReasons.NamedPrivilegeWithoutTablePrivilege(operation, privilege));
    }

    /// <summary>
    /// Adds what <paramref name="definition"/> grants: a privilege not tied to a table, or its table privilege on each
    /// of its tables. <c>false</c> when it is tied to tables but its access right isn't a table privilege, so no role
    /// can be checked for it.
    /// </summary>
    private static bool CatalogHits(PrivilegeDefinition definition, RunsAs runsAs, RequirementBasis basis, RequirementSource source, ResolvedHits hits)
    {
        if (definition.Tables.Count == 0)
        {
            hits.Globals.Add(new GlobalHit(definition.Name, runsAs, basis, source));
            return true;
        }
        if (definition.Privilege is not { } granted) return false;
        foreach (var table in definition.Tables)
        {
            hits.Requirements.Add(new RequirementHit(table.ToLowerInvariant(), granted, runsAs, basis, source));
        }
        return true;
    }
}

/// <summary>
/// What evaluations need once <see cref="PrivilegeResolver"/> has resolved them, in the order found.
/// </summary>
internal sealed class ResolvedHits
{
    public List<RequirementHit> Requirements { get; } = [];
    public List<UnresolvedHit> Unresolved { get; } = [];
    public List<AssumptionHit> Assumptions { get; } = [];
    public List<GlobalHit> Globals { get; } = [];

    /// <summary>
    /// Set when an evaluation stopped at <see cref="Evaluator.MaxVisits"/>, so the hits may be incomplete.
    /// </summary>
    public bool Truncated { get; init; }
}

/// <summary>
/// A privilege not tied to a table that an operation needs.
/// </summary>
internal sealed record GlobalHit(string Privilege, RunsAs RunsAs, RequirementBasis Basis, RequirementSource Source);
