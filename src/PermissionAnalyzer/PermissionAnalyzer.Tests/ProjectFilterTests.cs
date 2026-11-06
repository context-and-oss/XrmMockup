using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Workspace;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class ProjectFilterTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "repo");

    private static string ProjectPath(string relative) => Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

    [Theory]
    [InlineData("*.Tests", "src/Contoso.Plugins.Tests/Contoso.Plugins.Tests.csproj", true)]
    [InlineData("*.tests", "src/Contoso.Plugins.Tests/Contoso.Plugins.Tests.csproj", true)]
    [InlineData("*.Tests", "src/Contoso.Plugins/Contoso.Plugins.csproj", false)]
    [InlineData("Contoso.?lugins", "src/Contoso.Plugins/Contoso.Plugins.csproj", true)]
    [InlineData("Contoso", "src/Contoso.Plugins/Contoso.Plugins.csproj", false)]
    [InlineData("tests/*", "tests/Unit/Unit.csproj", true)]
    [InlineData("tests/*", "src/Tests/Tests.csproj", false)]
    [InlineData(@"tests\*", "tests/Unit/Unit.csproj", true)]
    public void MatchesNamesOrRelativePaths(string pattern, string project, bool excluded)
    {
        var filter = new ProjectFilter(Root, [pattern]);
        Assert.Equal(excluded ? pattern.Replace('\\', '/') : null, filter.Excludes(ProjectPath(project)));
    }

    [Fact]
    public void PathPatternsMatchRelativeToTheProjectsSolution()
    {
        var filter = new ProjectFilter(Root, ["tests/*"]);

        Assert.Null(filter.Excludes(ProjectPath("plugins/tests/Unit/Unit.csproj")));
        Assert.Equal("tests/*", filter.Excludes(ProjectPath("plugins/tests/Unit/Unit.csproj"), ProjectPath("plugins")));
    }

    [Fact]
    public void ReportsPatternsThatMatchNothing()
    {
        var filter = new ProjectFilter(Root, ["*.Tests", "Typo", " ", "*.TESTS"]);
        filter.Excludes(ProjectPath("a/A.Tests.csproj"));
        filter.Excludes(ProjectPath("b/B.csproj"));

        Assert.Equal(["*.Tests", "Typo"], filter.Patterns);
        Assert.Equal(["Typo"], filter.Unused);
    }

    [Fact]
    public void CommandLinePatternsAddToConfiguredOnes()
    {
        var directory = Directory.CreateTempSubdirectory("permissions-config").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), """
                { "XrmMockup": { "Permissions": { "Solution": "Contoso.slnx", "ExcludeProjects": ["*.Tests", "tests/*"] } } }
                """);

            var configuration = ConfigReader.Read(Path.Combine(directory, "appsettings.json"));
            var settings = AnalysisSettings.Resolve(null, null, configuration, ["Tools"]);

            Assert.Equal(["*.Tests", "tests/*", "Tools"], settings.ExcludeProjects);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PluginSolutionsAreRelativeToWhereTheyAreGiven()
    {
        var directory = Directory.CreateTempSubdirectory("permissions-config").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(directory, "plugins"));
            File.WriteAllText(Path.Combine(directory, "Contoso.slnx"), "<Solution />");
            File.WriteAllText(Path.Combine(directory, "plugins", "Plugins.slnx"), "<Solution />");
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), """
                { "XrmMockup": { "Permissions": { "Solution": "Contoso.slnx", "PluginSolutions": ["plugins/Plugins.slnx", "Contoso.slnx"] } } }
                """);
            var configuration = ConfigReader.Read(Path.Combine(directory, "appsettings.json"));

            var settings = AnalysisSettings.Resolve(null, null, configuration, pluginSolutions: [Path.Combine(directory, "plugins", "Plugins.slnx")]);
            Assert.Equal([Path.Combine(directory, "plugins", "Plugins.slnx")], settings.PluginSolutions);

            var missing = Assert.Throws<ToolException>(() => AnalysisSettings.Resolve(null, null, configuration, pluginSolutions: [Path.Combine(directory, "Missing.sln")]));
            Assert.Equal($"Plugin solution or project not found: {Path.Combine(directory, "Missing.sln")}", missing.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
