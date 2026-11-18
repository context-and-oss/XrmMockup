namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// Where code starts running.
/// </summary>
public enum EntryPointKind
{
    /// <summary>
    /// A plugin step registered in code (XrmPluginCore, DAXIF) or in the plugin metadata.
    /// </summary>
    PluginStep,

    /// <summary>
    /// An <c>IPlugin</c> without known steps: its <c>Execute(IServiceProvider)</c>.
    /// </summary>
    Plugin,

    CustomApi,

    /// <summary>
    /// A workflow <c>CodeActivity</c>.
    /// </summary>
    CodeActivity,

    /// <summary>
    /// An Azure Function, isolated (<c>[Function]</c>) or in-process (<c>[FunctionName]</c>).
    /// </summary>
    Function,

    /// <summary>
    /// A method given with <c>--entry</c> or <c>XrmMockup:Permissions:EntryPoints</c>.
    /// </summary>
    Method,

    /// <summary>
    /// A user operation on a table given with <c>--operation</c>, such as an update in an app, with the plugin
    /// steps it fires.
    /// </summary>
    Operation,
}
