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
