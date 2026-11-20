namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// What starts an entry point: the message, table and stage of a plugin step, or the trigger binding of an Azure
/// Function, such as <c>HttpTrigger</c>. <see cref="FilteredAttributes"/> are the columns an Update step is
/// filtered on, sorted; <c>null</c> when it runs on every update. <see cref="Asynchronous"/> is set for a step that
/// runs asynchronously.
/// </summary>
public sealed record Trigger(
    string? Message = null,
    string? Table = null,
    string? Stage = null,
    string? Binding = null,
    IReadOnlyList<string>? FilteredAttributes = null,
    bool Asynchronous = false)
{
    /// <summary>
    /// As in XrmMockup, a step's filtered attributes apply to Update only. Messages ignore case.
    /// </summary>
    internal static bool Filters(string? message) => string.Equals(message, Messages.Update, StringComparison.OrdinalIgnoreCase);
}
