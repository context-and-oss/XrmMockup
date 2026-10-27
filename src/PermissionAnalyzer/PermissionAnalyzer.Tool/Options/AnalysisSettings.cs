namespace XrmMockup.PermissionAnalyzer.Tool.Options;

/// <summary>
/// Absolute paths for one run, after command-line options are applied over the configuration.
/// </summary>
/// <param name="Target">The solution or project to analyze.</param>
/// <param name="MetadataDirectory">The XrmMockup metadata folder, or <c>null</c> to analyze without roles.</param>
/// <param name="ExcludeProjects">Patterns for projects to leave out: the configured ones, then the command line's.</param>
/// <param name="PluginSolutions">
/// Solutions or projects with plugin code to load for cascades only: the configured ones, then the command line's.
/// </param>
internal sealed record AnalysisSettings(string Target, string? MetadataDirectory, IReadOnlyList<string> ExcludeProjects, IReadOnlyList<string> PluginSolutions)
{
    private const string DefaultMetadataDirectory = "./Metadata";

    /// <summary>
    /// Command-line paths are relative to the current directory; configured paths to the configuration file.
    /// </summary>
    public static AnalysisSettings Resolve(
        string? solution,
        string? metadata,
        LoadedConfiguration configuration,
        IReadOnlyList<string>? exclude = null,
        IReadOnlyList<string>? pluginSolutions = null)
    {
        var excludeProjects = configuration.Permissions.ExcludeProjects.Concat(exclude ?? []).ToList();
        var currentDirectory = Directory.GetCurrentDirectory();

        var target = solution is { } cliTarget ? Path.GetFullPath(cliTarget, currentDirectory)
            : configuration.Permissions.Solution is { } configuredTarget ? Path.GetFullPath(configuredTarget, configuration.Directory)
            : FindSolution(currentDirectory);
        if (!File.Exists(target)) throw new ToolException($"Solution or project not found: {target}");

        // The analyzed solution is loaded once, as itself
        var plugins = configuration.Permissions.PluginSolutions.Select(p => Path.GetFullPath(p, configuration.Directory))
            .Concat((pluginSolutions ?? []).Select(p => Path.GetFullPath(p, currentDirectory)))
            .Distinct(StringComparer.Ordinal)
            .Where(p => !string.Equals(p, target, StringComparison.Ordinal))
            .ToList();
        foreach (var plugin in plugins)
        {
            if (!File.Exists(plugin)) throw new ToolException($"Plugin solution or project not found: {plugin}");
        }

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
            return new AnalysisSettings(target, Directory.Exists(fallback) ? fallback : null, excludeProjects, plugins);
        }

        if (!Directory.Exists(metadataDirectory)) throw new ToolException($"Metadata folder not found: {metadataDirectory}");
        return new AnalysisSettings(target, metadataDirectory, excludeProjects, plugins);
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
            0 => throw new ToolException($"No solution found in {directory}. Use {CliOptions.Solution.Primary} to give a solution or project."),
            _ => throw new ToolException($"Several solutions found in {directory}. Use {CliOptions.Solution.Primary} to pick one."),
        };
    }
}
