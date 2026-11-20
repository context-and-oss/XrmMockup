using Microsoft.Extensions.Logging.Abstractions;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;
using XrmMockup.PermissionAnalyzer.Tool.Options;

namespace XrmMockup.PermissionAnalyzer.Workspace;

/// <summary>Opt-in MSBuild/Roslyn frontend. The workspace is disposed after source summaries are extracted.</summary>
public static class PermissionAnalysisLoader
{
    public static async Task<PermissionAnalysis> LoadAsync(string projectOrSolutionPath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(projectOrSolutionPath))!;
        var configPath = Path.Combine(directory, "appsettings.json");
        var configuration = File.Exists(configPath) ? ConfigReader.Read(configPath)
            : new LoadedConfiguration(directory, new PermissionsConfiguration(), null);
        var settings = AnalysisSettings.Resolve(projectOrSolutionPath, null, configuration);
        var contents = settings.MetadataDirectory is { } metadataDirectory ? MetadataFileLoader.Load(metadataDirectory) : null;
        var builder = new ManifestBuilder(directory, RuleSetup.Create(configuration.Permissions), contents?.Lookups);
        builder.AddEntryMethods(configuration.Permissions.EntryPoints);
        if (settings.MetadataDirectory is { } metadata)
        {
            builder.AddWorkflows(WorkflowLoader.Load(metadata));
            if (PrivilegeCatalogLoader.Load(metadata) is var (privileges, apis)) builder.AddPrivilegeCatalog(privileges, apis);
        }
        var incomplete = await SourceLoader.AddAsync(builder, settings, NullLoggerFactory.Instance, cancellationToken, collectIncompleteSource: true);
        return new LoadedAnalysis(builder, incomplete);
    }

    private sealed class LoadedAnalysis(ManifestBuilder source, IReadOnlyList<UnresolvedOperation> incomplete) : PermissionAnalysis
    {
        public override Manifest Analyze(PermissionScope scope, PermissionMetadata metadata)
        {
            if (scope is null) throw new ArgumentNullException(nameof(scope));
            if (metadata is null) throw new ArgumentNullException(nameof(metadata));
            var builder = source.WithMetadata(metadata);
            EntryPointSelector selector;
            if (scope.PluginType is not null) selector = EntryPointSelector.TypedPlugin(scope);
            else
            {
                builder.AddOperation(scope.Message!, scope.Table!, scope.Columns);
                selector = EntryPointSelector.Operation(scope.Message!, scope.Table!);
            }
            var manifest = builder.Build(metadata.RoleGovernedTables, selector);
            if (manifest.EntryPoints.Count == 0) throw new ArgumentException($"No registered entry point matches {scope}.", nameof(scope));
            if (incomplete.Count == 0) return manifest;
            return new Manifest(manifest.EntryPoints.Select(e => new EntryPoint(e.Id, e.Kind, e.Type, e.Assembly,
                e.Trigger, e.ImpersonatingUserId, e.Declaration, e.Requirements, e.Unresolved.Concat(incomplete).ToList(),
                e.Assumptions, e.Triggers, e.GlobalRequirements, e.Truncated)).ToList(), [], manifest.NotRoleGoverned);
        }
    }
}
