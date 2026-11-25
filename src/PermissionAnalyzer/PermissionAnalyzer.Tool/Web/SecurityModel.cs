using System.Text.Json;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;

namespace XrmMockup.PermissionAnalyzer.Tool.Web;

/// <summary>A frozen extraction with fresh, isolated evaluation state for each interactive query.</summary>
internal sealed class SecurityModel(string root, ExtractedAnalysis graph,
    IReadOnlyList<UnresolvedOperation> diagnostics, IReadOnlyList<RoleDefinition>? roles,
    PermissionsConfiguration configuration, CoverageOptions? defaults, AnalysisProvenance? inputs,
    bool requirementsOnly = false, IReadOnlyList<TableColumnCatalog>? columnCatalog = null)
{
    internal object Catalog()
    {
        var manifest = Builder().Build(requirementsOnly ? null : roles?.SelectMany(r => r.Privileges.Keys)).WithDiagnostics(diagnostics);
        return new
        {
            requirementsOnly,
            incomplete = manifest.Incomplete,
            defaults = new { roles = defaults?.Roles?.Names ?? [], callerRoles = defaults?.CallerRoles?.Names ?? [],
                roleOrigin = defaults?.Roles?.Origin, callerRoleOrigin = defaults?.CallerRoles?.Origin },
            roles = (roles ?? []).OrderBy(r => r.Name, StringComparer.Ordinal).ThenBy(r => r.RoleId).Select(r => new
            {
                r.Name, r.RoleId,
                globalPrivilegesKnown = r.GlobalPrivileges is not null,
                grants = r.Privileges.OrderBy(t => t.Key, StringComparer.Ordinal)
                    .SelectMany(t => t.Value.OrderBy(p => p.Key).Select(p => new Grant(t.Key, p.Key.ToString(), p.Value.ToString())))
                    .Concat((r.GlobalPrivileges ?? new Dictionary<string, PrivilegeDepth>())
                        .OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new Grant(null, p.Key, p.Value.ToString()))).ToList()
            }).ToList(),
            columnCatalog = columnCatalog ?? [],
            tables = manifest.Sections.SelectMany(s => s.Requirements.Select(r => r.Table))
                .Concat((roles ?? []).SelectMany(r => r.Privileges.Keys))
                .Concat((columnCatalog ?? []).Select(t => t.Table))
                .Distinct(StringComparer.Ordinal).OrderBy(t => t, StringComparer.Ordinal).ToList(),
            entryPoints = manifest.EntryPoints.Select(e => new { e.Id, kind = e.Kind.ToString(), e.Type }).ToList(),
            methods = graph.Nodes.Where(n => !n.IsConstructor && n.Id == n.Member).Select(n => n.Member).Distinct(StringComparer.Ordinal)
                .OrderBy(m => m, StringComparer.Ordinal).ToList()
        };
    }

    internal JsonElement Query(SecurityQuery query)
    {
        query.Validate();
        var builder = Builder();
        var scope = query.Scope switch
        {
            "all" => null,
            "plugin" => EntryPointSelector.Plugin(query.Target!),
            "custom-api" => EntryPointSelector.CustomApi(query.Target!),
            "function" => EntryPointSelector.Function(query.Target!),
            "method" => EntryPointSelector.Methods([query.Target!]),
            "operation" => EntryPointSelector.Operation(query.Message!, query.Target!),
            _ => throw new ToolException("Unknown query scope.")
        };
        if (query.Scope == "method") builder.AddEntryMethods([query.Target!]);
        if (query.Scope == "operation") builder.AddOperation(query.Message!, query.Target!, query.Attributes.Length == 0 ? null : query.Attributes);
        var noRoles = requirementsOnly || query.NoRoles;
        if (requirementsOnly && (query.Roles.Length > 0 || query.CallerRoles.Length > 0))
            throw new ToolException("This session was started with --no-roles.");
        var manifest = builder.Build(noRoles ? null : roles?.SelectMany(r => r.Privileges.Keys), scope).WithDiagnostics(diagnostics);
        if (scope is not null && manifest.EntryPoints.Count == 0) throw new ToolException($"No entry point matches {scope}");
        // Empty selections retain startup/configured identities. Explicit selections replace only their identity.
        var selected = noRoles ? null : CoverageSetup.Create(roles, query.Roles, query.CallerRoles, configuration,
            requireRoles: false, query.Roles.Length == 0 ? defaults?.Roles : null,
            query.CallerRoles.Length == 0 ? defaults?.CallerRoles : null);
        var coverage = selected is null ? null : CoverageEvaluator.Evaluate(manifest, selected);
        if (selected?.Roles is null && selected?.CallerRoles is null && coverage?.UsesConfiguredRoles == false) coverage = null;
        var filter = new ReportFilter(query.Tables, query.Privileges, [], []);
        using var writer = new StringWriter();
        new JsonManifestWriter(filter, inputs).Write(manifest, coverage, writer);
        using var document = JsonDocument.Parse(writer.ToString());
        return document.RootElement.Clone();
    }

    private ManifestBuilder Builder()
    {
        var builder = ManifestBuilder.Restore(root, graph);
        builder.AddEntryMethods(configuration.EntryPoints);
        return builder;
    }

    private sealed record Grant(string? Table, string Privilege, string Depth);
}

internal sealed record SecurityQuery(string Scope = "all", string? Target = null, string? Message = null,
    string[]? Attributes = null, string[]? Roles = null, string[]? CallerRoles = null,
    string[]? Tables = null, string[]? Privileges = null, bool NoRoles = false)
{
    public string[] Attributes { get; init; } = Attributes ?? [];
    public string[] Roles { get; init; } = Roles ?? [];
    public string[] CallerRoles { get; init; } = CallerRoles ?? [];
    public string[] Tables { get; init; } = Tables ?? [];
    public string[] Privileges { get; init; } = Privileges ?? [];

    internal void Validate()
    {
        if (Scope is not ("all" or "plugin" or "custom-api" or "function" or "method" or "operation"))
            throw new ToolException("Unknown query scope.");
        if (Scope != "all" && string.IsNullOrWhiteSpace(Target)) throw new ToolException("A query target is required.");
        if (Scope == "operation" && (string.IsNullOrWhiteSpace(Message) || OperationRequest.Parse($"{Message} {Target}", Attributes) is null))
            throw new ToolException("An operation needs a message and table, such as Update account.");
        foreach (var values in new[] { Attributes, Roles, CallerRoles, Tables, Privileges })
            if (values.Length > 100 || values.Any(v => string.IsNullOrWhiteSpace(v) || v.Length > 1024))
                throw new ToolException("Query lists must contain at most 100 nonempty values, each at most 1024 characters.");
        if ((Target?.Length ?? 0) > 1024 || (Message?.Length ?? 0) > 1024) throw new ToolException("Query target is too long.");
        if (NoRoles && (Roles.Length > 0 || CallerRoles.Length > 0)) throw new ToolException("Requirements-only queries cannot select roles.");
    }
}
