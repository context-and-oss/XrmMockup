namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// Builds the <see cref="EntryPoint.Id"/> of each kind of entry point, and reads the name back out of one.
/// </summary>
internal static class EntryPointIds
{
    private const string PluginPrefix = "plugin:";
    private const string CustomApiPrefix = "custom-api:";
    private const string ActivityPrefix = "activity:";
    private const string FunctionPrefix = "function:";
    private const string MethodPrefix = "method:";
    private const string OperationPrefix = "operation:";
    private const string WorkflowPrefix = "workflow:";

    /// <summary>
    /// A plugin step, on any table when <paramref name="table"/> is <c>null</c>.
    /// </summary>
    public static string PluginStep(string type, string message, string? table, string stage) =>
        $"{PluginPrefix}{type}:{message}:{table ?? "any"}:{stage}";

    /// <summary>
    /// A plugin without known steps.
    /// </summary>
    public static string Plugin(string type) => PluginPrefix + type;

    public static string CustomApi(string name) => CustomApiPrefix + name;

    public static string Activity(string type) => ActivityPrefix + type;

    /// <summary>
    /// A function, by its app's assembly, since two apps can have functions of the same name: such as
    /// <c>function:Contoso.Functions/ProcessOrder</c>, the form <c>Principals</c> names it by.
    /// </summary>
    public static string Function(string? assembly, string name) => FunctionPrefix + (assembly is null ? "" : assembly + "/") + name;

    public static string Method(string methodId) => MethodPrefix + methodId;

    /// <summary>
    /// A workflow from the metadata, run by <paramref name="message"/> on its table. Not an entry point: operations
    /// only fire it.
    /// </summary>
    public static string Workflow(string name, string message, string table) => $"{WorkflowPrefix}{name}:{message}:{table}";

    /// <summary>
    /// A user operation, with the columns it sets when they are known.
    /// </summary>
    public static string Operation(string display, IReadOnlyList<string>? columns) =>
        OperationPrefix + display + (columns is null ? "" : $"({string.Join(",", columns)})");

    /// <summary>
    /// The name of the function <paramref name="id"/> is the id of, without its assembly; <c>null</c> for any other id.
    /// </summary>
    public static string? FunctionName(string id) => After(id, FunctionPrefix) is { } qualified ? qualified.Substring(qualified.LastIndexOf('/') + 1) : null;

    /// <summary>
    /// The assembly and name of the function <paramref name="id"/> is the id of, such as
    /// <c>Contoso.Functions/ProcessOrder</c>; <c>null</c> for any other id.
    /// </summary>
    public static string? QualifiedFunctionName(string id) => After(id, FunctionPrefix);

    /// <summary>
    /// The method id of the method entry <paramref name="id"/> is the id of; <c>null</c> for any other id.
    /// </summary>
    public static string? MethodId(string id) => After(id, MethodPrefix);

    private static string? After(string id, string prefix) =>
        id.StartsWith(prefix, StringComparison.Ordinal) ? id.Substring(prefix.Length) : null;
}
