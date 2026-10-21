using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// One table access made by a service call. <see cref="Table"/> is <c>null</c> when the table couldn't be
/// resolved, and <see cref="Reason"/> says why.
/// </summary>
internal sealed record DataAccess(
    string MethodId,
    SourceLocation? Declaration,
    string Operation,
    SourceLocation Source,
    string? Table,
    IReadOnlyList<Privilege> Privileges,
    string? Reason);
