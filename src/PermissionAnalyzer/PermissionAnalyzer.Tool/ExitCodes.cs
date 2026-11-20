namespace XrmMockup.PermissionAnalyzer.Tool;

internal static class ExitCodes
{
    public const int Success = 0;

    /// <summary>
    /// With <c>--fail-on-missing</c> or <c>--strict</c>: the roles don't grant everything the code needs.
    /// </summary>
    public const int MissingPrivileges = 1;

    /// <summary>
    /// Invalid arguments, missing files or a failed analysis.
    /// </summary>
    public const int Error = 2;
}
