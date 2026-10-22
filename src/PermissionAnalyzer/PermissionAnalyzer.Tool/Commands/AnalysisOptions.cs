using System.CommandLine;
using XrmMockup.PermissionAnalyzer.Core.Analysis;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// The options <c>required</c> and <c>check</c> share: what to analyze, the scope, the roles and the output.
/// </summary>
internal sealed class AnalysisOptions
{
    private readonly Option<string?> _solution = new(CliOptions.Solution.Primary, CliOptions.Solution.Alias) { Description = CliOptions.Solution.Description };
    private readonly Option<string?> _project = new(CliOptions.Project.Primary, CliOptions.Project.Alias) { Description = CliOptions.Project.Description };
    private readonly Option<string?> _metadata = new(CliOptions.Metadata.Primary, CliOptions.Metadata.Alias) { Description = CliOptions.Metadata.Description };
    private readonly Option<string[]> _exclude = new(CliOptions.Exclude.Primary, CliOptions.Exclude.Alias) { Description = CliOptions.Exclude.Description };
    private readonly Option<string[]> _pluginSolution = new(CliOptions.PluginSolution.Primary) { Description = CliOptions.PluginSolution.Description };
    private readonly Option<string?> _config = new(CliOptions.Config.Primary, CliOptions.Config.Alias) { Description = CliOptions.Config.Description };
    private readonly Option<string?> _output = new(CliOptions.Output.Primary, CliOptions.Output.Alias) { Description = CliOptions.Output.Description };
    private readonly Option<string?> _plugin = new(CliOptions.Plugin.Primary) { Description = CliOptions.Plugin.Description };
    private readonly Option<string?> _customApi = new(CliOptions.CustomApi.Primary) { Description = CliOptions.CustomApi.Description };
    private readonly Option<string?> _function = new(CliOptions.Function.Primary) { Description = CliOptions.Function.Description };
    private readonly Option<string[]> _entry = new(CliOptions.Entry.Primary) { Description = CliOptions.Entry.Description };
    private readonly Option<string?> _operation = new(CliOptions.Operation.Primary) { Description = CliOptions.Operation.Description };
    private readonly Option<string[]> _attributes = new(CliOptions.Attributes.Primary) { Description = CliOptions.Attributes.Description };
    private readonly Option<string[]> _role = new(CliOptions.Role.Primary, CliOptions.Role.Alias) { Description = CliOptions.Role.Description };
    private readonly Option<string[]> _callerRole = new(CliOptions.CallerRole.Primary) { Description = CliOptions.CallerRole.Description };
    private readonly Option<string> _format;

    public AnalysisOptions()
    {
        _format = new Option<string>(CliOptions.Format.Primary, CliOptions.Format.Alias)
        {
            Description = CliOptions.Format.Description,
            DefaultValueFactory = _ => CliOptions.Format.AllowedValues[0]
        };
        _format.AcceptOnlyFromAmong(CliOptions.Format.AllowedValues);
    }

    /// <summary>
    /// Adds the options and their validation to <paramref name="command"/>, scope and roles first.
    /// </summary>
    public void AddTo(Command command)
    {
        foreach (var option in new Option[] { _plugin, _customApi, _function, _entry, _operation, _attributes, _role, _callerRole, _solution, _project, _metadata, _exclude, _pluginSolution, _config, _format, _output })
        {
            command.Options.Add(option);
        }

        command.Validators.Add(result =>
        {
            if (result.GetResult(_solution) is not null && result.GetResult(_project) is not null)
            {
                result.AddError($"Use either {CliOptions.Solution.Primary} or {CliOptions.Project.Primary}, not both.");
            }

            var scopes = new Option[] { _plugin, _customApi, _function, _entry, _operation }.Count(o => result.GetResult(o) is not null);
            if (scopes > 1)
            {
                result.AddError($"Use only one of {CliOptions.Plugin.Primary}, {CliOptions.CustomApi.Primary}, {CliOptions.Function.Primary}, {CliOptions.Entry.Primary} and {CliOptions.Operation.Primary}.");
            }

            if (result.GetResult(_operation) is not null && OperationRequest.Parse(result.GetValue(_operation)!, null) is null)
            {
                result.AddError($"{CliOptions.Operation.Primary} takes a message and a table, such as \"Update account\".");
            }
            if (result.GetResult(_attributes) is not null && result.GetResult(_operation) is null)
            {
                result.AddError($"{CliOptions.Attributes.Primary} needs {CliOptions.Operation.Primary}.");
            }
        });
    }

    public AnalysisRequest Read(ParseResult parseResult)
    {
        var plugin = parseResult.GetValue(_plugin);
        var customApi = parseResult.GetValue(_customApi);
        var function = parseResult.GetValue(_function);
        var entry = parseResult.GetValue(_entry) ?? [];
        var operationText = parseResult.GetValue(_operation);
        var attributes = parseResult.GetValue(_attributes);
        var operation = operationText is null ? null : OperationRequest.Parse(operationText, attributes?.SelectMany(a => a.Split(',')).ToList());

        var scope = plugin is not null ? EntryPointSelector.Plugin(plugin)
            : customApi is not null ? EntryPointSelector.CustomApi(customApi)
            : function is not null ? EntryPointSelector.Function(function)
            : entry.Length > 0 ? EntryPointSelector.Methods(entry)
            : operation is not null ? EntryPointSelector.Operation(operationText!)
            : null;

        return new AnalysisRequest(
            parseResult.GetValue(_solution),
            parseResult.GetValue(_project),
            parseResult.GetValue(_metadata),
            parseResult.GetValue(_exclude) ?? [],
            parseResult.GetValue(_config),
            parseResult.GetValue(_format)!,
            parseResult.GetValue(_output),
            scope,
            entry,
            parseResult.GetValue(_role) ?? [],
            parseResult.GetValue(_callerRole) ?? [],
            operation,
            parseResult.GetValue(_pluginSolution) ?? []);
    }
}

/// <summary>
/// The command line of one <c>required</c> or <c>check</c> run.
/// </summary>
internal sealed record AnalysisRequest(
    string? Solution,
    string? Project,
    string? Metadata,
    IReadOnlyList<string> Exclude,
    string? Config,
    string Format,
    string? Output,
    EntryPointSelector? Scope,
    IReadOnlyList<string> EntryMethods,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> CallerRoles,
    OperationRequest? Operation = null,
    IReadOnlyList<string>? PluginSolutions = null);

/// <summary>
/// The user operation of <c>--operation</c>, with the columns of <c>--attributes</c> when given.
/// </summary>
internal sealed record OperationRequest(string Message, string Table, IReadOnlyList<string>? Columns)
{
    /// <summary>
    /// Reads <c>"&lt;message&gt; &lt;table&gt;"</c>; <c>null</c> when it isn't two words.
    /// </summary>
    public static OperationRequest? Parse(string text, IReadOnlyList<string>? columns)
    {
        var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 ? new OperationRequest(parts[0], parts[1], columns) : null;
    }
}
