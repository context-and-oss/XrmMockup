using Microsoft.Extensions.Logging;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Workspace;

namespace XrmMockup.PermissionAnalyzer.Workspace;

/// <summary>The shared source-loading orchestration for the CLI and SDK.</summary>
internal static class SourceLoader
{
    public static async Task<IReadOnlyList<UnresolvedOperation>> AddAsync(ManifestBuilder builder, AnalysisSettings settings, ILoggerFactory loggerFactory, CancellationToken cancellationToken, bool collectIncompleteSource = false)
    {
        var root = Path.GetDirectoryName(settings.Target)!;
        var filter = new ProjectFilter(root, settings.ExcludeProjects);
        using var loader = new CodeLoader(loggerFactory.CreateLogger<CodeLoader>(), collectIncompleteSource);
        await loader.AddAsync(settings.Target, builder, filter, cancellationToken);
        foreach (var plugins in settings.PluginSolutions)
            await loader.AddAsync(plugins, builder, filter, cancellationToken, pluginCode: true);
        var logger = loggerFactory.CreateLogger(typeof(SourceLoader));
        foreach (var pattern in filter.Unused) logger.LogWarning("Exclude pattern '{Pattern}' matches no project", pattern);
        foreach (var unmatched in builder.UnmatchedEntryMethods()) logger.LogWarning("No method in the solution matches entry point {Method}", unmatched);
        return loader.IncompleteSource;
    }
}
