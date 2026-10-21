namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// What starts an entry point: the message, table and stage of a plugin step, or the trigger binding of an Azure
/// Function, such as <c>HttpTrigger</c>.
/// </summary>
public sealed record Trigger(string? Message = null, string? Table = null, string? Stage = null, string? Binding = null);
