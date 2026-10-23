using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;
using XrmMockup.PermissionAnalyzer.Tool.Workspace;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// What <c>required</c> and <c>check</c> have in common: analyze the code, check it against roles when there are
/// any, and write the report.
/// </summary>
internal static class AnalysisRunner
{
    /// <summary>
    /// Runs the analysis and writes the report. <paramref name="exitCode"/> turns the result into the exit code;
    /// errors exit with <see cref="ExitCodes.Error"/>.
    /// </summary>
    public static async Task<int> RunAsync(
        string commandName,
        AnalysisRequest request,
        bool requireRoles,
        Func<CoverageReport?, ILogger, int> exitCode,
        CancellationToken cancellationToken)
    {
        using var loggerFactory = ToolLogging.Create();
        var logger = loggerFactory.CreateLogger(typeof(AnalysisRunner).Namespace + "." + commandName);

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        logger.LogInformation("XrmMockup Permission Analyzer v{Version}", version);

        try
        {
            var configuration = ConfigReader.Read(request.Config);
            var settings = AnalysisSettings.Resolve(request.Solution, request.Metadata, configuration, request.Exclude, request.PluginSolutions);

            var roles = LoadRoles(settings.MetadataDirectory, logger);
            // Role names are checked before the slow part, so a typo fails fast
            var coverageOptions = CoverageSetup.Create(roles, request.Roles, request.CallerRoles, configuration.Permissions, requireRoles);

            var root = Path.GetDirectoryName(settings.Target)!;
            var lookups = settings.MetadataDirectory is { } lookupDirectory ? LoadLookups(lookupDirectory, logger) : [];
            var builder = new ManifestBuilder(root, RuleSetup.Create(configuration.Permissions), lookups);
            if (settings.MetadataDirectory is { } metadataDirectory) AddMetadata(builder, metadataDirectory, logger);
            builder.AddEntryMethods(configuration.Permissions.EntryPoints.Concat(request.EntryMethods).Distinct(StringComparer.Ordinal));
            if (request.Operation is { } operation) builder.AddOperation(operation.Message, operation.Table, operation.Columns);

            var filter = new ProjectFilter(root, settings.ExcludeProjects);
            var loader = new CodeLoader(loggerFactory.CreateLogger<CodeLoader>());
            await loader.AddAsync(settings.Target, builder, filter, cancellationToken);
            foreach (var plugins in settings.PluginSolutions)
            {
                await loader.AddAsync(plugins, builder, filter, cancellationToken, pluginCode: true);
            }
            foreach (var pattern in filter.Unused)
            {
                logger.LogWarning("Exclude pattern '{Pattern}' matches no project", pattern);
            }

            foreach (var unmatched in builder.UnmatchedEntryMethods())
            {
                logger.LogWarning("No method in the solution matches entry point {Method}", unmatched);
            }

            var manifest = builder.Build(roles?.SelectMany(r => r.Privileges.Keys), request.Scope);
            if (request.Scope is not null && manifest.EntryPoints.Count == 0)
            {
                throw new ToolException($"No entry point matches {request.Scope}");
            }

            CoverageReport? coverage = null;
            if (coverageOptions is not null)
            {
                coverage = CoverageEvaluator.Evaluate(manifest, coverageOptions);
                if (requireRoles && coverageOptions.Roles is null && coverageOptions.CallerRoles is null && !coverage.UsesConfiguredRoles)
                {
                    throw new ToolException(
                        $"No roles to check against: no {CliOptions.Role.Primary} or {CliOptions.CallerRole.Primary}, and nothing analyzed matches " +
                        $"{PermissionsConfiguration.SectionPath}:Principals or {PermissionsConfiguration.SectionPath}:Users");
                }
            }

            var report = request.Report;
            IManifestWriter writer = request.Format switch
            {
                ReportFormat.Json => new JsonManifestWriter(report.Filter),
                ReportFormat.Markdown => new MarkdownManifestWriter(report.Filter),
                ReportFormat.Sarif => new SarifManifestWriter(report.Filter, SarifManifestWriter.RepositoryPath(root), version),
                _ => new TextManifestWriter(report),
            };
            if (request.Output is null)
            {
                writer.Write(manifest, coverage, Console.Out);
            }
            else
            {
                var path = Path.GetFullPath(request.Output);
                await using var file = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(manifest, coverage, file);
                logger.LogInformation("Wrote {Path}", path);
            }

            return exitCode(coverage, logger);
        }
        catch (ToolException ex)
        {
            logger.LogError("{Message}", ex.Message);
            return ExitCodes.Error;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Analysis failed");
            return ExitCodes.Error;
        }
    }

    private static IReadOnlyList<LookupColumn> LoadLookups(string metadataDirectory, ILogger logger)
    {
        var lookups = LookupLoader.Load(metadataDirectory);
        logger.LogInformation("Loaded {Count} lookup columns from {Directory}", lookups.Count, metadataDirectory);
        return lookups;
    }

    /// <summary>
    /// The plugin steps and the privilege catalog of the metadata folder.
    /// </summary>
    private static void AddMetadata(ManifestBuilder builder, string metadataDirectory, ILogger logger)
    {
        var steps = PluginStepLoader.Load(metadataDirectory);
        logger.LogInformation("Loaded {Count} plugin steps from {Directory}", steps.Count, metadataDirectory);
        builder.AddPluginSteps(steps);

        if (PrivilegeCatalogLoader.Load(metadataDirectory) is var (privileges, customApis))
        {
            logger.LogInformation("Loaded {Count} privileges and {CustomApis} custom APIs from {File}", privileges.Count, customApis.Count, PrivilegeCatalogLoader.FileName);
            builder.AddPrivilegeCatalog(privileges, customApis);
        }
        else
        {
            logger.LogWarning("No {File} in {Directory}, so execute privileges set by name can't be looked up. Regenerate the metadata to add it", PrivilegeCatalogLoader.FileName, metadataDirectory);
        }
    }

    private static IReadOnlyList<RoleDefinition>? LoadRoles(string? metadataDirectory, ILogger logger)
    {
        if (metadataDirectory is null)
        {
            logger.LogWarning("No metadata folder found, so security roles and plugin step registrations aren't used. Pass {Metadata} or set {Setting}:MetadataDirectory", CliOptions.Metadata.Primary, PermissionsConfiguration.SectionPath);
            return null;
        }

        var roles = RoleLoader.Load(metadataDirectory);
        if (roles.Count == 0)
        {
            logger.LogWarning("No security roles in {Directory}", Path.Combine(metadataDirectory, RoleLoader.SecurityRolesFolder));
            return null;
        }

        logger.LogInformation("Loaded {Count} security roles from {Directory}", roles.Count, metadataDirectory);
        return roles;
    }
}
