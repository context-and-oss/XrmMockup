using Microsoft.Extensions.Logging;

namespace XrmMockup.Tools.Common.Logging;

public static class ShortCategoryConsoleExtensions
{
    /// <summary>
    /// Adds a console logger that writes with <see cref="ShortCategoryConsoleFormatter"/>. With
    /// <see cref="ShortCategoryConsoleFormatterOptions.StandardError"/> set, every line goes to standard error.
    /// </summary>
    public static ILoggingBuilder AddShortCategoryConsole(this ILoggingBuilder builder, Action<ShortCategoryConsoleFormatterOptions>? configure = null)
    {
        var options = new ShortCategoryConsoleFormatterOptions();
        configure?.Invoke(options);
        return builder
            .AddConsole(console =>
            {
                console.FormatterName = ShortCategoryConsoleFormatter.FormatterName;
                if (options.StandardError) console.LogToStandardErrorThreshold = LogLevel.Trace;
            })
            .AddConsoleFormatter<ShortCategoryConsoleFormatter, ShortCategoryConsoleFormatterOptions>(configure ?? (_ => { }));
    }
}
