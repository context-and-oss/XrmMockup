namespace XrmMockup.PermissionAnalyzer.Tool.Options;

/// <summary>
/// Configuration for permission analysis from appsettings.json.
/// </summary>
public record PermissionsConfiguration
{
    /// <summary>
    /// The configuration section path.
    /// </summary>
    public const string SectionPath = "XrmMockup:Permissions";

    /// <summary>
    /// Solution or project to analyze, relative to the configuration file.
    /// </summary>
    public string? Solution { get; init; }

    /// <summary>
    /// XrmMockup metadata folder, relative to the configuration file. When not set, the metadata
    /// tool's <c>XrmMockup:Metadata:OutputDirectory</c> is used.
    /// </summary>
    public string? MetadataDirectory { get; init; }

    /// <summary>
    /// Projects to leave out, such as test projects. Wildcard patterns matched against the project name
    /// (<c>*.Tests</c>), or against the path relative to the solution when they contain a slash (<c>tests/*</c>).
    /// </summary>
    public string[] ExcludeProjects { get; init; } = [];

    /// <summary>
    /// Solutions or projects with the plugin code of the environment, relative to the configuration file, for code that
    /// doesn't include it, such as an Azure Function app. Their plugin steps and custom APIs run when the analyzed code
    /// fires them, but their own entry points aren't reported.
    /// </summary>
    public string[] PluginSolutions { get; init; } = [];

    /// <summary>
    /// Methods to analyze as entry points besides the discovered ones, such as <c>Contoso.Sync.Importer.Run</c>.
    /// </summary>
    public string[] EntryPoints { get; init; } = [];

    /// <summary>
    /// Roles of the application user of app code, such as an Azure Function app, by assembly name
    /// (<c>Contoso.Functions</c>) or by assembly and function name (<c>Contoso.Functions/ProcessOrder</c>), which wins.
    /// </summary>
    public Dictionary<string, PrincipalConfiguration> Principals { get; init; } = [];

    /// <summary>
    /// Roles of specific users, such as the impersonating user of a plugin step, by user id.
    /// </summary>
    public Dictionary<string, UserConfiguration> Users { get; init; } = [];

    /// <summary>
    /// Missing privileges that are accepted. They are reported as suppressed and don't fail <c>check</c>.
    /// </summary>
    public SuppressionConfiguration[] Suppress { get; init; } = [];

    /// <summary>
    /// Rules for requests passed to <c>Execute</c>, which replace the built-in rule for their message or add one.
    /// </summary>
    public RuleConfiguration[] Rules { get; init; } = [];
}

public record PrincipalConfiguration
{
    public string[] Roles { get; init; } = [];
}

public record UserConfiguration
{
    /// <summary>
    /// A name for the user in the output, such as <c>svc-plugins</c>.
    /// </summary>
    public string? Name { get; init; }

    public string[] Roles { get; init; } = [];
}

public record SuppressionConfiguration
{
    /// <summary>
    /// Table logical name.
    /// </summary>
    public string? Table { get; init; }

    /// <summary>
    /// The privilege, such as <c>Create</c>. Every privilege on the table when not set.
    /// </summary>
    public string? Privilege { get; init; }

    public string? Reason { get; init; }
}

public record RuleConfiguration
{
    /// <summary>
    /// The message, such as <c>Assign</c>, or the request type name, such as <c>AssignRequest</c>.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// The member that holds the target of <see cref="Privileges"/>, such as <c>Target</c>.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// The privileges needed on <see cref="Target"/>. <c>$Member</c>, such as <c>$PrincipalAccess.AccessMask</c>,
    /// adds the rights that member grants.
    /// </summary>
    public string[] Privileges { get; init; } = [];

    /// <summary>
    /// Further checks, on other members or on fixed tables.
    /// </summary>
    public RuleCheckConfiguration[] Checks { get; init; } = [];

    /// <summary>
    /// Messages whose checks this request makes as well, such as <c>Create</c> and <c>Update</c> for <c>Upsert</c>.
    /// </summary>
    public string[] As { get; init; } = [];

    /// <summary>
    /// The member that holds several targets, which the checks of <see cref="As"/> on <c>Target</c> apply to.
    /// </summary>
    public string? Targets { get; init; }

    /// <summary>
    /// The member that holds requests run as part of this one.
    /// </summary>
    public string? Requests { get; init; }

    /// <summary>
    /// Whether XrmMockup makes the checks. <c>true</c> when not set.
    /// </summary>
    public bool? Enforced { get; init; }
}

public record RuleCheckConfiguration
{
    /// <summary>
    /// The request member that refers to the table, such as <c>Target</c>.
    /// </summary>
    public string? Member { get; init; }

    /// <summary>
    /// A fixed table logical name, when no <see cref="Member"/> is set.
    /// </summary>
    public string? Table { get; init; }

    /// <summary>
    /// The privileges, as for <see cref="RuleConfiguration.Privileges"/>.
    /// </summary>
    public string[] Privileges { get; init; } = [];

    /// <summary>
    /// A boolean member that must be set for the check to apply.
    /// </summary>
    public string? When { get; init; }
}
