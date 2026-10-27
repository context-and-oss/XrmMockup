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
/// One run of the tool: analyze the code, check it against roles when there are any, and write the report.
/// </summary>
internal static class AnalysisRunner
{
    /// <summary>
    /// Runs the analysis and writes the report. Exits with <see cref="ExitCodes.MissingPrivileges"/> when
    /// <see cref="AnalysisRequest.FailOnMissing"/> is set and the roles don't grant everything, and with
    /// <see cref="ExitCodes.Error"/> on errors.
    /// </summary>
    public static async Task<int> RunAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        using var loggerFactory = ToolLogging.Create();
        var logger = loggerFactory.CreateLogger(typeof(AnalysisRunner));

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        logger.LogInformation("XrmMockup Permission Analyzer v{Version}", version);

        try
        {
            var configuration = ConfigReader.Read(request.Config);
            var settings = AnalysisSettings.Resolve(request.Solution, request.Metadata, configuration, request.Exclude, request.PluginSolutions);

            var roles = LoadRoles(settings.MetadataDirectory, logger);
            // Role names are checked before the slow part, so a typo fails fast
            var coverageOptions = request.NoRoles
                ? null
                : CoverageSetup.Create(roles, request.Roles, request.CallerRoles, configuration.Permissions, request.FailOnMissing);

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
                // Other entry points' settings are expected not to match when the analysis is narrowed to some
                if (request.Scope is null)
                {
                    foreach (var key in coverage.UnusedPrincipals)
                    {
                        logger.LogWarning("{Setting}:Principals:{Key} matches no analyzed assembly or function", PermissionsConfiguration.SectionPath, key);
                    }
                    foreach (var id in coverage.UnusedUsers)
                    {
                        logger.LogWarning("{Setting}:Users:{Id} is a user no analyzed operation runs as", PermissionsConfiguration.SectionPath, id);
                    }
                }
                if (coverageOptions.Roles is null && coverageOptions.CallerRoles is null && !coverage.UsesConfiguredRoles)
                {
                    if (request.FailOnMissing)
                    {
                        throw new ToolException(
                            $"No roles to check against: no {CliOptions.Role.Primary} or {CliOptions.CallerRole.Primary}, and nothing analyzed matches " +
                            $"{PermissionsConfiguration.SectionPath}:Principals or {PermissionsConfiguration.SectionPath}:Users");
                    }
                    // The settings name no identity of the code analyzed, so the run just lists what it needs
                    coverage = null;
                }
            }

            var writer = ManifestWriters.Create(request.Format, request.Report, root, version);
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

            return coverage is not null && request.FailOnMissing ? ExitCode(coverage, request.Strict, logger) : ExitCodes.Success;
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

    /// <summary>
    /// The exit code of a run with <c>--fail-on-missing</c> or <c>--strict</c>, with the reason it fails logged.
    /// </summary>
    private static int ExitCode(CoverageReport coverage, bool strict, ILogger logger)
    {
        var summary = coverage.Summary;
        if (summary.Passed(strict))
        {
            if (summary.Incomplete)
            {
                logger.LogWarning(
                    "The roles grant every privilege found, but the result is incomplete: {Unresolved} unresolved operations, " +
                    "{NotChecked} requirements without roles, {Unverifiable} privileges not tied to a table that the role metadata predates " +
                    "and {Truncated} entry points or methods whose analysis stopped at the call limit weren't checked. " +
                    "See 'Not checked' in the report, or pass {Strict} to fail on them",
                    summary.Unresolved, summary.NotChecked, summary.Unverifiable, summary.Truncated, CliOptions.Strict.Primary);
            }
            return ExitCodes.Success;
        }

        if (summary.Missing > 0)
        {
            var privileges = coverage.Missing.Select(m => $"{m.Table} {m.Privilege}").Concat(coverage.MissingGlobal.Select(m => m.Privilege)).Distinct().ToList();
            logger.LogError("The roles don't grant {Count} privileges: {Privileges}. See the report", privileges.Count, string.Join(", ", privileges));
        }
        else
        {
            logger.LogError(
                "{Strict}: {Unresolved} unresolved, {NotChecked} not checked, {Unverifiable} unverifiable, {Truncated} stopped at the call limit",
                CliOptions.Strict.Primary, summary.Unresolved, summary.NotChecked, summary.Unverifiable, summary.Truncated);
        }
        return ExitCodes.MissingPrivileges;
    }

    private static IReadOnlyList<LookupColumn> LoadLookups(string metadataDirectory, ILogger logger)
    {
        var lookups = LookupLoader.Load(metadataDirectory);
        logger.LogInformation("Loaded {Count} lookup columns from {Directory}", lookups.Count, metadataDirectory);
        return lookups;
    }

    /// <summary>
    /// The plugin steps, the workflows and the privilege catalog of the metadata folder.
    /// </summary>
    private static void AddMetadata(ManifestBuilder builder, string metadataDirectory, ILogger logger)
    {
        var steps = PluginStepLoader.Load(metadataDirectory);
        logger.LogInformation("Loaded {Count} plugin steps from {Directory}", steps.Count, metadataDirectory);
        builder.AddPluginSteps(steps);

        var workflows = WorkflowLoader.Load(metadataDirectory);
        logger.LogInformation("Loaded {Count} workflows that run on a table from {Directory}", workflows.Count, metadataDirectory);
        builder.AddWorkflows(workflows);

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
