namespace XrmMockup.PermissionAnalyzer.Core.Coverage;

/// <summary>Structured live provenance, kept separate from the concise label used in coverage summaries.</summary>
internal sealed record LiveRoleSource(
    string UserName,
    Guid UserId,
    string EnvironmentUrl,
    DateTimeOffset ReadAt,
    IReadOnlyList<LiveRoleAssignment> Assignments,
    bool IsCaller = false);

internal sealed record LiveRoleAssignment(string RoleName, Guid RootRoleId, string? TeamName = null, Guid? TeamId = null);
