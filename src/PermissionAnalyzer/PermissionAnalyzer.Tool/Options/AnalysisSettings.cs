namespace XrmMockup.PermissionAnalyzer.Tool.Options;

/// <summary>
/// Absolute paths for one run, after command-line options are applied over the configuration.
/// </summary>
/// <param name="Target">The solution or project to analyze.</param>
/// <param name="MetadataDirectory">The XrmMockup metadata folder, or <c>null</c> to analyze without roles.</param>
/// <param name="ExcludeProjects">Patterns for projects to leave out: the configured ones, then the command line's.</param>
internal sealed record AnalysisSettings(string Target, string? MetadataDirectory, IReadOnlyList<string> ExcludeProjects)
{
    private const string DefaultMetadataDirectory = "./Metadata";

    /// <summary>
    /// Command-line paths are relative to the current directory; configured paths to the configuration file.
    /// </summary>
    public static AnalysisSettings Resolve(
        string? solution,
        string? project,
        string? metadata,
        LoadedConfiguration configuration,
        IReadOnlyList<string>? exclude = null)
    {
        var excludeProjects = configuration.Permissions.ExcludeProjects.Concat(exclude ?? []).ToList();
        var currentDirectory = Directory.GetCurrentDirectory();

        var target = (solution ?? project) is { } cliTarget ? Path.GetFullPath(cliTarget, currentDirectory)
            : configuration.Permissions.Solution is { } configuredTarget ? Path.GetFullPath(configuredTarget, configuration.Directory)
            : FindSolution(currentDirectory);
        if (!File.Exists(target)) throw new ToolException($"Solution or project not found: {target}");

        string? metadataDirectory;
        if (metadata is not null)
        {
            metadataDirectory = Path.GetFullPath(metadata, currentDirectory);
        }
        else if ((configuration.Permissions.MetadataDirectory ?? configuration.MetadataOutputDirectory) is { } configured)
        {
            metadataDirectory = Path.GetFullPath(configured, configuration.Directory);
        }
        else
        {
            // Nothing configured: use the metadata tool's default folder if it's there
            var fallback = Path.GetFullPath(DefaultMetadataDirectory, configuration.Directory);
            return new AnalysisSettings(target, Directory.Exists(fallback) ? fallback : null, excludeProjects);
        }

        if (!Directory.Exists(metadataDirectory)) throw new ToolException($"Metadata folder not found: {metadataDirectory}");
        return new AnalysisSettings(target, metadataDirectory, excludeProjects);
    }

    private static string FindSolution(string directory)
    {
        var solutions = Directory.GetFiles(directory, "*.sln")
            .Concat(Directory.GetFiles(directory, "*.slnx"))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        return solutions.Count switch
        {
            1 => solutions[0],
            0 => throw new ToolException($"No solution found in {directory}. Use {CliOptions.Solution.Primary} or {CliOptions.Project.Primary}."),
            _ => throw new ToolException($"Several solutions found in {directory}. Use {CliOptions.Solution.Primary} to pick one."),
        };
    }
}
