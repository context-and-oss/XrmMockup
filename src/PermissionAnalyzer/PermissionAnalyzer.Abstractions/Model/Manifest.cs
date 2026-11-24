namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// The result of an analysis: every entry point with what it needs, sorted by id, and the operations no entry
/// point reaches.
/// </summary>
public sealed class Manifest(
    IReadOnlyList<EntryPoint> entryPoints,
    IReadOnlyList<MethodManifest> unreached,
    IReadOnlyList<string>? notRoleGoverned = null)
{
    public IReadOnlyList<EntryPoint> EntryPoints { get; } = entryPoints;

    /// <summary>Known findings do not cover unsupported source, unknown operations or truncated analysis.</summary>
    public bool Incomplete => Sections.Any(s => s.Truncated || s.Unresolved.Any(u => u.RunsAs.Kind != RunsAsKind.System));

    /// <summary>Attach source-loading failures to every selected section, including role-free reports.</summary>
    public Manifest WithDiagnostics(IReadOnlyList<UnresolvedOperation> diagnostics)
    {
        if (diagnostics.Count == 0) return this;
        IReadOnlyList<UnresolvedOperation> Merge(IReadOnlyList<UnresolvedOperation> existing) => existing.Concat(diagnostics)
            .Distinct().OrderBy(u => u.Source.Location).ThenBy(u => u.Operation, StringComparer.Ordinal)
            .ThenBy(u => u.Reason, StringComparer.Ordinal).ThenBy(u => u.RunsAs).ToList();
        var entries = EntryPoints.Select(e => new EntryPoint(e.Id, e.Kind, e.Type, e.Assembly, e.Trigger,
            e.ImpersonatingUserId, e.Declaration, e.Requirements, Merge(e.Unresolved), e.Assumptions,
            e.Triggers, e.GlobalRequirements, e.Truncated)).ToList();
        var methods = Unreached.Select(m => new MethodManifest(m.Id, m.Declaration, m.Requirements,
            Merge(m.Unresolved), m.Assumptions, m.GlobalRequirements, m.Truncated)).ToList();
        if (entries.Count + methods.Count == 0) methods.Add(new MethodManifest("source loading", null, [], Merge([]), []));
        return new Manifest(entries, methods, NotRoleGoverned);
    }

    /// <summary>
    /// Methods with data operations that no entry point reaches, sorted by id. Empty when the analysis was
    /// scoped to some entry points.
    /// </summary>
    public IReadOnlyList<MethodManifest> Unreached { get; } = unreached;

    /// <summary>
    /// Required tables absent from the supplied role metadata. This is an inventory gap, not proof of a
    /// Dataverse security exemption. <c>null</c> when no role inventory was supplied.
    /// </summary>
    public IReadOnlyList<string>? NotRoleGoverned { get; } = notRoleGoverned;

    /// <summary>Inventory gaps; the legacy NotRoleGoverned property is retained for API compatibility.</summary>
    public IReadOnlyList<string>? TablesWithoutRoleMetadata => NotRoleGoverned;

    /// <summary>
    /// The entry points, then the unreached methods.
    /// </summary>
    public IReadOnlyList<IManifestSection> Sections { get; } = [.. entryPoints, .. unreached];

    /// <summary>
    /// Every requirement, of entry points and unreached methods.
    /// </summary>
    public IEnumerable<Requirement> AllRequirements =>
        Sections.SelectMany(s => s.Requirements);

    /// <summary>
    /// Every requirement of a privilege not tied to a table, of entry points and unreached methods.
    /// </summary>
    public IEnumerable<GlobalRequirement> AllGlobalRequirements =>
        Sections.SelectMany(s => s.GlobalRequirements);

    /// <summary>
    /// Every table that appears in a requirement, sorted.
    /// </summary>
    public IReadOnlyList<string> Tables =>
        AllRequirements.Select(r => r.Table).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList();
}
