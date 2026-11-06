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

    /// <summary>
    /// Methods with data operations that no entry point reaches, sorted by id. Empty when the analysis was
    /// scoped to some entry points.
    /// </summary>
    public IReadOnlyList<MethodManifest> Unreached { get; } = unreached;

    /// <summary>
    /// Required tables that no security role mentions. XrmMockup lets every operation on these through,
    /// so they need no privilege. <c>null</c> when no roles were loaded.
    /// </summary>
    public IReadOnlyList<string>? NotRoleGoverned { get; } = notRoleGoverned;

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
