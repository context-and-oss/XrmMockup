namespace XrmMockup.PermissionAnalyzer.Tool;

/// <summary>
/// An error in the input, such as a missing file. Reported without a stack trace.
/// </summary>
internal sealed class ToolException(string message) : Exception(message);
