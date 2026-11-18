namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

internal static class RequestReasons
{
    private const string NoRulePrefix = "no rule for message '";
    private const string CustomApiPrefix = "custom API '";
    private const string RealTimeWorkflowPrefix = "fires real-time workflow '";
    private const string BackgroundWorkflowPrefix = "fires background workflow '";

    public static string NoRule(string message) => NoRulePrefix + message + "'";

    /// <summary>
    /// A custom API that neither the code nor the privilege catalog has, so neither its code nor its execute
    /// privilege is known.
    /// </summary>
    public static string CustomApi(string message) => CustomApiPrefix + message + "' is called; its code and execute privilege are unknown";

    /// <summary>
    /// A custom API in the privilege catalog whose code isn't loaded: its execute privilege is checked, its code isn't.
    /// </summary>
    public static string CustomApiCode(string message) => CustomApiPrefix + message + "' is called; its code isn't analyzed";

    public static string ExecutePrivilegeComputed(string message) => CustomApiPrefix + message + "' needs an execute privilege set at runtime";

    public static string PrivilegeCatalogMissing(string message, string privilege) =>
        CustomApiPrefix + message + $"' needs execute privilege '{privilege}', which needs a privilege catalog (PrivilegeCatalog.xml) to look up";

    public static string PrivilegeNotInCatalog(string message, string privilege) =>
        CustomApiPrefix + message + $"' needs execute privilege '{privilege}', which isn't in the privilege catalog";

    public static string PrivilegeWithoutTablePrivilege(string message, string privilege) =>
        CustomApiPrefix + message + $"' needs execute privilege '{privilege}', whose access right isn't a table privilege";

    /// <summary>
    /// A privilege a request rule names that the privilege catalog doesn't have, such as one misspelled in a configured
    /// rule.
    /// </summary>
    public static string NamedPrivilegeNotInCatalog(string message, string privilege) =>
        $"the {message} request needs privilege '{privilege}', which isn't in the privilege catalog";

    public static string NamedPrivilegeWithoutTablePrivilege(string message, string privilege) =>
        $"the {message} request needs privilege '{privilege}', whose access right isn't a table privilege";

    /// <summary>
    /// The message of a <see cref="NoRule"/> reason, or <c>null</c> for any other reason.
    /// </summary>
    public static string? NoRuleMessage(string reason) =>
        reason.StartsWith(NoRulePrefix, StringComparison.Ordinal) && reason.EndsWith("'", StringComparison.Ordinal)
            ? reason.Substring(NoRulePrefix.Length, reason.Length - NoRulePrefix.Length - 1)
            : null;

    /// <summary>
    /// A reason about calling a custom API: its code or its execute privilege.
    /// </summary>
    public static bool IsCustomApi(string reason) => reason.StartsWith(CustomApiPrefix, StringComparison.Ordinal);

    /// <summary>
    /// A workflow from the metadata that an operation fires, whose steps aren't analyzed.
    /// </summary>
    public static string Workflow(string name, bool realTime) =>
        (realTime ? RealTimeWorkflowPrefix : BackgroundWorkflowPrefix) + name + "', whose steps aren't analyzed";

    /// <summary>
    /// A reason about a fired <see cref="Workflow"/>.
    /// </summary>
    public static bool IsWorkflow(string reason) =>
        reason.StartsWith(RealTimeWorkflowPrefix, StringComparison.Ordinal) || reason.StartsWith(BackgroundWorkflowPrefix, StringComparison.Ordinal);
}
