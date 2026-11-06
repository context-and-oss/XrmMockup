namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The writer of each output format.
/// </summary>
internal static class ManifestWriters
{
    /// <param name="root">The analysis root, which SARIF paths are made relative to the repository from.</param>
    /// <param name="version">The tool version SARIF output names.</param>
    public static IManifestWriter Create(ReportFormat format, ReportOptions report, string root, string? version) => format switch
    {
        ReportFormat.Json => new JsonManifestWriter(report.Filter),
        ReportFormat.Markdown => new MarkdownManifestWriter(report.Filter),
        ReportFormat.Sarif => new SarifManifestWriter(report.Filter, SarifManifestWriter.RepositoryPath(root), version),
        _ => new TextManifestWriter(report),
    };
}
