using System.CommandLine;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;
using XrmMockup.PermissionAnalyzer.Tool.Workspace;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// <c>required</c>: the privileges the code needs, per entry point.
/// </summary>
internal static class RequiredCommand
{
    public const string Name = "required";

    public static Command Create()
    {
        var solutionOption = new Option<string?>(CliOptions.Solution.Primary, CliOptions.Solution.Alias)
        {
            Description = CliOptions.Solution.Description
        };

        var projectOption = new Option<string?>(CliOptions.Project.Primary, CliOptions.Project.Alias)
        {
            Description = CliOptions.Project.Description
        };

        var metadataOption = new Option<string?>(CliOptions.Metadata.Primary, CliOptions.Metadata.Alias)
        {
            Description = CliOptions.Metadata.Description
        };

        var excludeOption = new Option<string[]>(CliOptions.Exclude.Primary, CliOptions.Exclude.Alias)
        {
            Description = CliOptions.Exclude.Description
        };

        var configOption = new Option<string?>(CliOptions.Config.Primary, CliOptions.Config.Alias)
        {
            Description = CliOptions.Config.Description
        };

        var formatOption = new Option<string>(CliOptions.Format.Primary, CliOptions.Format.Alias)
        {
            Description = CliOptions.Format.Description,
            DefaultValueFactory = _ => CliOptions.Format.AllowedValues[0]
        };
        formatOption.AcceptOnlyFromAmong(CliOptions.Format.AllowedValues);

        var outputOption = new Option<string?>(CliOptions.Output.Primary, CliOptions.Output.Alias)
        {
            Description = CliOptions.Output.Description
        };

        var pluginOption = new Option<string?>(CliOptions.Plugin.Primary) { Description = CliOptions.Plugin.Description };
        var customApiOption = new Option<string?>(CliOptions.CustomApi.Primary) { Description = CliOptions.CustomApi.Description };
        var functionOption = new Option<string?>(CliOptions.Function.Primary) { Description = CliOptions.Function.Description };
        var entryOption = new Option<string[]>(CliOptions.Entry.Primary) { Description = CliOptions.Entry.Description };

        var command = new Command(Name, "List the table privileges the code needs, per entry point")
        {
            pluginOption,
            customApiOption,
            functionOption,
            entryOption,
            solutionOption,
            projectOption,
            metadataOption,
            excludeOption,
            configOption,
            formatOption,
            outputOption
        };

        command.Validators.Add(result =>
        {
            if (result.GetResult(solutionOption) is not null && result.GetResult(projectOption) is not null)
            {
                result.AddError($"Use either {CliOptions.Solution.Primary} or {CliOptions.Project.Primary}, not both.");
            }

            var scopes = new Option[] { pluginOption, customApiOption, functionOption, entryOption }.Count(o => result.GetResult(o) is not null);
            if (scopes > 1)
            {
                result.AddError($"Use only one of {CliOptions.Plugin.Primary}, {CliOptions.CustomApi.Primary}, {CliOptions.Function.Primary} and {CliOptions.Entry.Primary}.");
            }
        });

        command.SetAction((parseResult, cancellationToken) => RunAsync(
            parseResult.GetValue(solutionOption),
            parseResult.GetValue(projectOption),
            parseResult.GetValue(metadataOption),
            parseResult.GetValue(excludeOption),
            parseResult.GetValue(configOption),
            parseResult.GetValue(formatOption)!,
            parseResult.GetValue(outputOption),
            Scope(parseResult.GetValue(pluginOption), parseResult.GetValue(customApiOption), parseResult.GetValue(functionOption), parseResult.GetValue(entryOption)),
            parseResult.GetValue(entryOption) ?? [],
            cancellationToken));

        return command;
    }

    private static async Task<int> RunAsync(
        string? solution,
        string? project,
        string? metadata,
        string[]? exclude,
        string? config,
        string format,
        string? output,
        EntryPointSelector? scope,
        string[] entryMethods,
        CancellationToken cancellationToken)
    {
        using var loggerFactory = ToolLogging.Create();
        var logger = loggerFactory.CreateLogger(typeof(RequiredCommand).FullName!);

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        logger.LogInformation("XrmMockup Permission Analyzer v{Version}", version);

        try
        {
            var configuration = ConfigReader.Read(config);
            var settings = AnalysisSettings.Resolve(solution, project, metadata, configuration, exclude);

            var roles = LoadRoles(settings.MetadataDirectory, logger);

            var root = Path.GetDirectoryName(settings.Target)!;
            var builder = new ManifestBuilder(root);
            if (settings.MetadataDirectory is { } metadataDirectory)
            {
                var steps = PluginStepLoader.Load(metadataDirectory);
                logger.LogInformation("Loaded {Count} plugin steps from {Directory}", steps.Count, metadataDirectory);
                builder.AddPluginSteps(steps);
            }
            builder.AddEntryMethods(configuration.Permissions.EntryPoints.Concat(entryMethods).Distinct(StringComparer.Ordinal));

            var filter = new ProjectFilter(root, settings.ExcludeProjects);
            await new CodeLoader(loggerFactory.CreateLogger<CodeLoader>()).AddAsync(settings.Target, builder, filter, cancellationToken);

            foreach (var unmatched in builder.UnmatchedEntryMethods())
            {
                logger.LogWarning("No method in the solution matches entry point {Method}", unmatched);
            }

            var manifest = builder.Build(roles?.SelectMany(r => r.Privileges.Keys), scope);
            if (scope is not null && manifest.EntryPoints.Count == 0)
            {
                throw new ToolException($"No entry point matches {scope}");
            }

            IManifestWriter writer = format == "json" ? new JsonManifestWriter() : new TextManifestWriter();
            if (output is null)
            {
                writer.Write(manifest, Console.Out);
            }
            else
            {
                var path = Path.GetFullPath(output);
                await using var file = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(manifest, file);
                logger.LogInformation("Wrote {Path}", path);
            }

            return ExitCodes.Success;
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

    private static EntryPointSelector? Scope(string? plugin, string? customApi, string? function, string[]? entry) =>
        plugin is not null ? EntryPointSelector.Plugin(plugin)
        : customApi is not null ? EntryPointSelector.CustomApi(customApi)
        : function is not null ? EntryPointSelector.Function(function)
        : entry is { Length: > 0 } ? EntryPointSelector.Methods(entry)
        : null;

    private static IReadOnlyList<RoleDefinition>? LoadRoles(string? metadataDirectory, ILogger logger)
    {
        if (metadataDirectory is null)
        {
            logger.LogWarning("No metadata folder found, so security roles and plugin step registrations aren't used. Pass --metadata or set XrmMockup:Permissions:MetadataDirectory");
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
