namespace XrmMockup.PermissionAnalyzer.Tool;

internal static class ExitCodes
{
    public const int Success = 0;

    /// <summary>
    /// Reserved for <c>check</c>: the roles don't grant everything the code needs.
    /// </summary>
    public const int MissingPrivileges = 1;

    /// <summary>
    /// Invalid arguments, missing files or a failed analysis.
    /// </summary>
    public const int Error = 2;
}
