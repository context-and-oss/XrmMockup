namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A plugin step from the XrmMockup metadata, matched to a plugin class by <see cref="TypeName"/> and
/// <see cref="AssemblyName"/>, the way XrmMockup matches it.
/// </summary>
/// <param name="TypeName">Full name of the plugin class.</param>
/// <param name="AssemblyName">Name of the assembly with the plugin class, when known.</param>
/// <param name="Message">SDK message, such as <c>Update</c>.</param>
/// <param name="Table">Primary table logical name; <c>null</c> for any table.</param>
/// <param name="Stage">Pipeline stage: 10, 20, 30 or 40.</param>
/// <param name="ImpersonatingUserId">The user the step runs as, when it doesn't run as the caller.</param>
/// <param name="FilteredAttributes">The columns an Update step is filtered on; <c>null</c> for none.</param>
/// <param name="Asynchronous">Whether the step runs asynchronously.</param>
public sealed record PluginStepRegistration(
    string TypeName,
    string? AssemblyName,
    string Message,
    string? Table,
    int Stage,
    string? ImpersonatingUserId,
    IReadOnlyList<string>? FilteredAttributes = null,
    bool Asynchronous = false);
