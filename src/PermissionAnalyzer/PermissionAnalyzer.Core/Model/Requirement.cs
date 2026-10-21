namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A privilege on a table that the code needs, with every call site that needs it.
/// </summary>
public sealed class Requirement(string table, Privilege privilege, RequirementBasis basis, IReadOnlyList<SourceLocation> sources)
{
    /// <summary>
    /// Table logical name.
    /// </summary>
    public string Table { get; } = table;

    public Privilege Privilege { get; } = privilege;

    public RequirementBasis Basis { get; } = basis;

    /// <summary>
    /// Call sites that need this privilege, sorted by file and position.
    /// </summary>
    public IReadOnlyList<SourceLocation> Sources { get; } = sources;
}
