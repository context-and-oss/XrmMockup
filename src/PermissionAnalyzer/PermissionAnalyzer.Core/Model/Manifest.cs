namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// The result of an analysis: every method that performs data operations, sorted by id.
/// </summary>
public sealed class Manifest(IReadOnlyList<MethodManifest> methods, IReadOnlyList<string>? notRoleGoverned = null)
{
    public IReadOnlyList<MethodManifest> Methods { get; } = methods;

    /// <summary>
    /// Required tables that no security role mentions. XrmMockup lets every operation on these through,
    /// so they need no privilege. <c>null</c> when no roles were loaded.
    /// </summary>
    public IReadOnlyList<string>? NotRoleGoverned { get; } = notRoleGoverned;

    /// <summary>
    /// Every table that appears in a requirement, sorted.
    /// </summary>
    public IReadOnlyList<string> Tables =>
        Methods.SelectMany(m => m.Requirements).Select(r => r.Table).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList();
}
