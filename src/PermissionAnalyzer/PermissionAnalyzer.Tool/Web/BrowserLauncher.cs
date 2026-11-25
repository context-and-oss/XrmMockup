using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace XrmMockup.PermissionAnalyzer.Tool.Web;

/// <summary>Best-effort desktop integration; never makes a browser a requirement for the explorer.</summary>
internal static class BrowserLauncher
{
    internal static void Open(string address, ILogger logger, Func<ProcessStartInfo, Process?>? start = null)
    {
        try
        {
            var info = StartInfo(address);
            var process = (start ?? Process.Start)(info);
            if (process is null)
                logger.LogWarning("Could not open the default browser. Open {Address} manually", address);
            else if (!OperatingSystem.IsWindows())
            {
                // Desktop launch helpers can start successfully but exit unsuccessfully
                // (for example xdg-open in a headless shell). Observe without delaying startup.
                _ = ObserveAsync(process, address, logger);
                // ObserveAsync owns the process until it exits.
            }
            else process.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            logger.LogWarning("Could not open the default browser. Open {Address} manually", address);
            logger.LogDebug(ex, "Default browser launch failed");
        }
    }

    internal static ProcessStartInfo StartInfo(string address, string? platform = null)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "http" || uri.Host != "127.0.0.1"
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new InvalidOperationException("Only the local explorer URL may be launched.");
        platform ??= OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
        if (platform == "windows") return new ProcessStartInfo(address) { UseShellExecute = true };
        var info = new ProcessStartInfo(platform == "macos" ? "open" : "xdg-open") { UseShellExecute = false };
        info.ArgumentList.Add(address);
        return info;
    }

    private static async Task ObserveAsync(Process process, string address, ILogger logger)
    {
        try
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) logger.LogWarning("Could not open the default browser. Open {Address} manually", address);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        { logger.LogDebug(ex, "Could not observe browser launch helper"); }
        finally { process.Dispose(); }
    }
}
