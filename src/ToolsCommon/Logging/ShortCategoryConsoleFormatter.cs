using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace XrmMockup.Tools.Common.Logging;

/// <summary>
/// A console formatter that shows only the short class name instead of the full namespace. Register it with
/// <see cref="ShortCategoryConsoleExtensions.AddShortCategoryConsole"/>.
/// </summary>
public sealed class ShortCategoryConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "shortCategory";

    private readonly IOptionsMonitor<ShortCategoryConsoleFormatterOptions> _options;

    public ShortCategoryConsoleFormatter(IOptionsMonitor<ShortCategoryConsoleFormatterOptions> options)
        : base(FormatterName)
    {
        _options = options;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null)
        {
            return;
        }

        var options = _options.CurrentValue;

        // Write timestamp if configured
        if (!string.IsNullOrEmpty(options.TimestampFormat))
        {
            var timestamp = options.UseUtcTimestamp
                ? DateTimeOffset.UtcNow
                : DateTimeOffset.Now;
            textWriter.Write(timestamp.ToString(options.TimestampFormat));
        }

        // Write log level with color using ANSI codes
        var logLevelString = GetLogLevelString(logEntry.LogLevel);
        var redirected = options.StandardError ? Console.IsErrorRedirected : Console.IsOutputRedirected;
        var useColor = options.ColorBehavior == LoggerColorBehavior.Enabled ||
            (options.ColorBehavior == LoggerColorBehavior.Default && !redirected);

        if (useColor)
        {
            var ansiColor = GetAnsiColor(logEntry.LogLevel);
            textWriter.Write($"{ansiColor}{logLevelString}{AnsiReset}");
        }
        else
        {
            textWriter.Write(logLevelString);
        }

        // Write short category name (just the class name)
        var shortCategory = GetShortCategoryName(logEntry.Category);
        textWriter.Write($" {shortCategory}: ");

        // Write message
        textWriter.WriteLine(message);

        // Write exception if present
        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine(logEntry.Exception.ToString());
        }
    }

    private static string GetShortCategoryName(string category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return category;
        }

        var lastDot = category.LastIndexOf('.');
        return lastDot >= 0 ? category[(lastDot + 1)..] : category;
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => logLevel.ToString().ToLowerInvariant()
    };

    // ANSI escape codes for colors
    private const string AnsiReset = "\x1b[0m";

    private static string GetAnsiColor(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "\x1b[90m",      // Bright black (gray)
        LogLevel.Debug => "\x1b[90m",      // Bright black (gray)
        LogLevel.Information => "\x1b[32m", // Green
        LogLevel.Warning => "\x1b[33m",    // Yellow
        LogLevel.Error => "\x1b[31m",      // Red
        LogLevel.Critical => "\x1b[91m",   // Bright red
        _ => ""
    };
}
