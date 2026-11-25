using System.CommandLine;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// The options of the command: what to analyze, the scope, the roles, when to fail and the output.
/// </summary>
internal sealed class AnalysisOptions
{
    private readonly Option<bool> _explore = new(CliOptions.Explore.Primary) { Description = CliOptions.Explore.Description };
    private readonly Option<int> _port = new(CliOptions.Port.Primary) { Description = CliOptions.Port.Description, DefaultValueFactory = _ => 8765 };
    private readonly Option<string?> _solution = new(CliOptions.Solution.Primary, CliOptions.Solution.Alias) { Description = CliOptions.Solution.Description };
    private readonly Option<string?> _snapshot = new(CliOptions.Snapshot.Primary) { Description = CliOptions.Snapshot.Description };
    private readonly Option<string?> _saveSnapshot = new(CliOptions.SaveSnapshot.Primary) { Description = CliOptions.SaveSnapshot.Description };
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
    private readonly Option<bool> _noRoles = new(CliOptions.NoRoles.Primary) { Description = CliOptions.NoRoles.Description };
    private readonly Option<bool> _failOnMissing = new(CliOptions.FailOnMissing.Primary) { Description = CliOptions.FailOnMissing.Description };
    private readonly Option<string> _user = new(CliOptions.User.Primary) { Description = CliOptions.User.Description };
    private readonly Option<string> _callerUser = new(CliOptions.CallerUser.Primary) { Description = CliOptions.CallerUser.Description };
    private readonly Option<string> _dataverseUrl = new(CliOptions.DataverseUrl.Primary, CliOptions.DataverseUrl.Alias) { Description = CliOptions.DataverseUrl.Description };
    private readonly Option<string> _credentialType = new(CliOptions.CredentialType.Primary, CliOptions.CredentialType.Alias) { Description = CliOptions.CredentialType.Description };
    private readonly Option<bool> _strict = new(CliOptions.Strict.Primary) { Description = CliOptions.Strict.Description };
    private readonly Option<string[]> _table = new(CliOptions.Table.Primary) { Description = CliOptions.Table.Description };
    private readonly Option<string[]> _privilege = new(CliOptions.Privilege.Primary) { Description = CliOptions.Privilege.Description };
    private readonly Option<string[]> _runsAs = new(CliOptions.RunsAs.Primary) { Description = CliOptions.RunsAs.Description };
    private readonly Option<string[]> _basis = new(CliOptions.Basis.Primary) { Description = CliOptions.Basis.Description };
    private readonly Option<string> _format;
    private readonly Option<string> _detail;

    public AnalysisOptions()
    {
        _credentialType.AcceptOnlyFromAmong(CliOptions.CredentialType.AllowedValues);
        _format = new Option<string>(CliOptions.Format.Primary, CliOptions.Format.Alias)
        {
            Description = CliOptions.Format.Description,
            HelpName = string.Join("|", CliOptions.Format.AllowedValues),
            DefaultValueFactory = _ => CliOptions.Format.AllowedValues[0]
        };
        _format.AcceptOnlyFromAmong(CliOptions.Format.AllowedValues);
        _detail = new Option<string>(CliOptions.Detail.Primary, CliOptions.Detail.Alias)
        {
            Description = CliOptions.Detail.Description,
            HelpName = string.Join("|", CliOptions.Detail.AllowedValues),
            DefaultValueFactory = _ => CliOptions.Detail.AllowedValues[0]
        };
        _detail.AcceptOnlyFromAmong(CliOptions.Detail.AllowedValues);
    }

    /// <summary>
    /// Adds the options and their validation to <paramref name="command"/>, in the order the README lists them: input,
    /// output, filters, scope, roles and failing.
    /// </summary>
    public void AddTo(Command command)
    {
        foreach (var option in new Option[] { _explore, _port, _solution, _snapshot, _saveSnapshot, _metadata, _exclude, _pluginSolution, _config, _format, _output, _detail, _table, _privilege, _runsAs, _basis, _plugin, _customApi, _function, _entry, _operation, _attributes, _role, _callerRole, _user, _callerUser, _dataverseUrl, _credentialType, _noRoles, _failOnMissing, _strict })
        {
            command.Options.Add(option);
        }

        command.Validators.Add(result =>
        {
            if (result.GetResult(_port) is not null && result.GetValue(_port) is < 1 or > 65535)
                result.AddError("--port must be between 1 and 65535.");
            if (result.GetResult(_port) is not null && !result.GetValue(_explore))
                result.AddError($"{CliOptions.Port.Primary} requires {CliOptions.Explore.Primary}.");
            if (result.GetValue(_explore))
            {
                // Interactive queries choose their own scope/filter. Do not silently ignore report switches.
                foreach (var exploreConflict in new Option[] { _format, _output, _detail, _table, _privilege, _runsAs, _basis,
                    _plugin, _customApi, _function, _entry, _operation, _attributes, _failOnMissing, _strict }
                    .Where(o => result.GetResult(o) is not null))
                    result.AddError($"{CliOptions.Explore.Primary} can't be used with {exploreConflict.Name}; choose query options in the UI.");
            }
            if (result.GetResult(_snapshot) is not null)
            {
                foreach (var sourceConflict in new Option[] { _solution, _exclude, _pluginSolution, _saveSnapshot }.Where(o => result.GetResult(o) is not null))
                    result.AddError($"{CliOptions.Snapshot.Primary} can't be used with {sourceConflict.Name}; source inputs are frozen in the snapshot.");
            }
            foreach (var option in new[] { _snapshot, _saveSnapshot })
                if (result.GetResult(option) is not null && string.IsNullOrWhiteSpace(result.GetValue(option)))
                    result.AddError($"{option.Name} needs a file path.");
            foreach (var option in new[] { _snapshot, _saveSnapshot })
                if (result.GetValue(option) is { } artifact && !string.IsNullOrWhiteSpace(artifact) &&
                    result.GetValue(_output) is { } output && !string.IsNullOrWhiteSpace(output))
                {
                    try
                    {
                        if (string.Equals(Path.GetFullPath(artifact), Path.GetFullPath(output),
                            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                            result.AddError($"{CliOptions.Output.Primary} must not overwrite the snapshot file.");
                    }
                    catch (ArgumentException) { result.AddError("Invalid snapshot or report file path."); }
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
            if (result.GetValue(_noRoles) && new Option[] { _role, _callerRole, _user, _callerUser, _failOnMissing, _strict }.FirstOrDefault(o => result.GetResult(o) is not null) is { } conflict)
            {
                result.AddError($"{CliOptions.NoRoles.Primary} can't be used with {conflict.Name}, which needs roles.");
            }

            if (result.GetResult(_user) is not null && result.GetResult(_role) is not null)
                result.AddError($"{CliOptions.User.Primary} can't be used with {CliOptions.Role.Primary}.");
            if (result.GetResult(_callerUser) is not null && result.GetResult(_callerRole) is not null)
                result.AddError($"{CliOptions.CallerUser.Primary} can't be used with {CliOptions.CallerRole.Primary}.");
            foreach (var option in new[] { _user, _callerUser })
                if (result.GetResult(option) is not null && string.IsNullOrWhiteSpace(result.GetValue(option)))
                    result.AddError($"{option.Name} needs a UPN or email.");
            if (!result.GetValue(_explore) && result.GetResult(_user) is null && result.GetResult(_callerUser) is null &&
                new Option[] { _dataverseUrl, _credentialType }.FirstOrDefault(o => result.GetResult(o) is not null) is { } connectionOption)
                result.AddError($"{connectionOption.Name} needs {CliOptions.User.Primary} or {CliOptions.CallerUser.Primary}.");

            foreach (var name in Values(result.GetValue(_runsAs)).Where(n => ParseRunsAs(n) is null))
            {
                result.AddError($"Unknown identity '{name}' in {CliOptions.RunsAs.Primary}. Use {string.Join(", ", Enum.GetNames<RunsAsKind>())}.");
            }
            foreach (var name in Values(result.GetValue(_basis)).Where(n => BasisNames.Parse(n) is null))
            {
                result.AddError($"Unknown basis '{name}' in {CliOptions.Basis.Primary}. Use {string.Join(", ", BasisNames.All)}.");
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
            : operation is not null ? EntryPointSelector.Operation(operation.Message, operation.Table)
            : null;

        // Input/capture switches alone save the graph without evaluating a query or writing a report.
        // Check explicit presence so even --format text or --detail normal requests a report.
        var captureOnly = parseResult.GetValue(_saveSnapshot) is not null && !parseResult.GetValue(_explore)
            && !new Option[] { _format, _output, _detail, _table, _privilege, _runsAs, _basis,
                _plugin, _customApi, _function, _entry, _operation, _attributes,
                _role, _callerRole, _user, _callerUser, _failOnMissing, _strict }
                .Any(o => parseResult.GetResult(o) is { Implicit: false });

        return new AnalysisRequest(
            parseResult.GetValue(_solution),
            parseResult.GetValue(_metadata),
            parseResult.GetValue(_exclude) ?? [],
            parseResult.GetValue(_config),
            Enum.Parse<ReportFormat>(parseResult.GetValue(_format)!, ignoreCase: true),
            parseResult.GetValue(_output),
            scope,
            entry,
            parseResult.GetValue(_role) ?? [],
            parseResult.GetValue(_callerRole) ?? [],
            operation,
            parseResult.GetValue(_pluginSolution) ?? [],
            parseResult.GetValue(_noRoles),
            parseResult.GetValue(_failOnMissing) || parseResult.GetValue(_strict),
            parseResult.GetValue(_strict),
            new ReportOptions(
                Enum.Parse<Detail>(parseResult.GetValue(_detail)!, ignoreCase: true),
                new ReportFilter(
                    Values(parseResult.GetValue(_table)),
                    Values(parseResult.GetValue(_privilege)),
                    Values(parseResult.GetValue(_runsAs)).Select(n => ParseRunsAs(n)!.Value).Distinct().ToList(),
                    Values(parseResult.GetValue(_basis)).Select(n => BasisNames.Parse(n)!.Value).Distinct().ToList())),
            parseResult.GetValue(_user),
            parseResult.GetValue(_callerUser),
            parseResult.GetValue(_dataverseUrl),
            parseResult.GetValue(_credentialType),
            parseResult.GetValue(_snapshot),
            parseResult.GetValue(_saveSnapshot),
            parseResult.GetValue(_explore),
            parseResult.GetResult(_port) is null ? 8765 : parseResult.GetValue(_port),
            captureOnly);
    }

    /// <summary>
    /// The values of a repeatable option whose values may also be comma-separated.
    /// </summary>
    private static IReadOnlyList<string> Values(string[]? values) =>
        (values ?? []).SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>
    /// The identity by name, ignoring case; <c>null</c> for any other text, numbers included.
    /// </summary>
    private static RunsAsKind? ParseRunsAs(string name) =>
        Enum.GetValues<RunsAsKind>().Cast<RunsAsKind?>().FirstOrDefault(k => string.Equals(k.ToString(), name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// The command line of one run. <see cref="FailOnMissing"/> is also set by <c>--strict</c>.
/// <see cref="CaptureOnly"/> stops after source extraction and snapshot saving, without evaluating a report.
/// </summary>
internal sealed record AnalysisRequest(
    string? Solution,
    string? Metadata,
    IReadOnlyList<string> Exclude,
    string? Config,
    ReportFormat Format,
    string? Output,
    EntryPointSelector? Scope,
    IReadOnlyList<string> EntryMethods,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> CallerRoles,
    OperationRequest? Operation,
    IReadOnlyList<string> PluginSolutions,
    bool NoRoles,
    bool FailOnMissing,
    bool Strict,
    ReportOptions Report,
    string? User = null,
    string? CallerUser = null,
    string? DataverseUrl = null,
    string? CredentialType = null,
    string? Snapshot = null,
    string? SaveSnapshot = null,
    bool Web = false,
    int Port = 8765,
    bool CaptureOnly = false)
{
    // Capture is analysis-first. Configured coverage must not require --no-roles just to save a graph.
    // Explicit coverage/failure options still opt in, including configured roles with --strict.
    public bool RequirementsOnly => NoRoles || SaveSnapshot is not null && Roles.Count == 0 && CallerRoles.Count == 0
        && User is null && CallerUser is null && !FailOnMissing && !Strict;
}

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
