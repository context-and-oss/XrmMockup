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
}
