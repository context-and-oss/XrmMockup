namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A privilege on a table that the code needs under one identity, with every call site that needs it.
/// </summary>
public sealed class Requirement(string table, Privilege privilege, RunsAs runsAs, RequirementBasis basis, IReadOnlyList<RequirementSource> sources)
{
    /// <summary>
    /// Table logical name.
    /// </summary>
    public string Table { get; } = table;

    public Privilege Privilege { get; } = privilege;

    public RunsAs RunsAs { get; } = runsAs;

    public RequirementBasis Basis { get; } = basis;

    /// <summary>
    /// Call sites that need this privilege, sorted by file and position.
    /// </summary>
    public IReadOnlyList<RequirementSource> Sources { get; } = sources;
}
