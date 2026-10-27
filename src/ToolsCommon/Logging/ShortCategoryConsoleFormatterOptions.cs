using Microsoft.Extensions.Logging.Console;

namespace XrmMockup.Tools.Common.Logging;

/// <summary>
/// Options for <see cref="ShortCategoryConsoleFormatter"/>.
/// </summary>
public sealed class ShortCategoryConsoleFormatterOptions : SimpleConsoleFormatterOptions
{
    /// <summary>
    /// Whether every log line goes to standard error, which then decides whether colors are used by default.
    /// </summary>
    public bool StandardError { get; set; }
}
