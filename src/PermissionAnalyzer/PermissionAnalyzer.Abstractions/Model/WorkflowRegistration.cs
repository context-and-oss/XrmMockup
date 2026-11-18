namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A workflow from the XrmMockup metadata that runs automatically on a table. Its steps aren't analyzed, so the
/// operations that fire it report it as unresolved.
/// </summary>
/// <param name="Name">Name of the workflow.</param>
/// <param name="Table">Primary table logical name.</param>
/// <param name="OnCreate">Whether it runs when a row is created.</param>
/// <param name="OnDelete">Whether it runs when a row is deleted.</param>
/// <param name="UpdateColumns">The columns whose update runs it; <c>null</c> when an update doesn't.</param>
/// <param name="RealTime">Whether it runs in the request (real-time) rather than in the background.</param>
/// <param name="RunsAsUserId">The user it runs as, its owner, when it doesn't run as the calling user.</param>
public sealed record WorkflowRegistration(
    string Name,
    string Table,
    bool OnCreate,
    bool OnDelete,
    IReadOnlyList<string>? UpdateColumns,
    bool RealTime,
    string? RunsAsUserId);
