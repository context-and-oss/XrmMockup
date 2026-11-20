using Microsoft.Extensions.Logging;
using XrmMockup.Tools.Common.Logging;

namespace XrmMockup.PermissionAnalyzer.Tool;

internal static class ToolLogging
{
    /// <summary>
    /// Logs go to standard error, so a report written to standard output can be piped as is.
    /// </summary>
    public static ILoggerFactory Create() => LoggerFactory.Create(builder => builder
        .AddShortCategoryConsole(options =>
        {
            options.StandardError = true;
            options.TimestampFormat = "HH:mm:ss ";
        })
        .AddFilter("Microsoft", LogLevel.Warning));
}
