namespace XrmMockup.PermissionAnalyzer.Tests;

internal static class TestMetadata
{
    public static string DirectoryPath { get; } = FindDirectory();

    private static string FindDirectory()
    {
        // CallerFilePath contains a virtual source path in normalized CI builds.
        // Locate the checked-in fixture from the test assembly's runtime location instead.
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "tests", "XrmMockup365Test", "Metadata");
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException($"Could not find engine test metadata above {AppContext.BaseDirectory}");
    }
}
