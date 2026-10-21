using Microsoft.Extensions.Logging;

namespace XrmMockup.PermissionAnalyzer.Tool;

internal static class ToolLogging
{
    /// <summary>
    /// Logs go to standard error, so a report written to standard output can be piped as is.
    /// </summary>
    public static ILoggerFactory Create() => LoggerFactory.Create(builder => builder
        .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace)
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.IncludeScopes = false;
        })
        .AddFilter("Microsoft", LogLevel.Warning));
}
