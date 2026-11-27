using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using XrmMockup.PermissionAnalyzer.Tool.Web;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class BrowserLauncherTests
{
    [Theory]
    [InlineData("windows", "http://127.0.0.1:8765/", true)]
    [InlineData("macos", "open", false)]
    [InlineData("linux", "xdg-open", false)]
    public void UsesDefaultBrowserWithoutShellCommandConcatenation(string platform, string executable, bool shell)
    {
        const string url = "http://127.0.0.1:8765/";
        var info = BrowserLauncher.StartInfo(url, platform);
        Assert.Equal(executable, info.FileName);
        Assert.Equal(shell, info.UseShellExecute);
        if (!shell) Assert.Equal(url, Assert.Single(info.ArgumentList));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("file:///tmp/file")]
    [InlineData("http://127.0.0.1:8765/?token=secret")]
    public void RefusesNonExplorerUrls(string address) => Assert.Throws<InvalidOperationException>(() => BrowserLauncher.StartInfo(address));

    [Fact]
    public void MissingBrowserDoesNotFailTheExplorer()
    {
        BrowserLauncher.Open("http://127.0.0.1:8765/", NullLogger.Instance, _ => throw new Win32Exception("No browser"));
        BrowserLauncher.Open("http://127.0.0.1:8765/", NullLogger.Instance, _ => null);
    }
}
