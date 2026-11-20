namespace XrmMockup.MetadataGenerator.Core.Services;

/// <summary>
/// Orchestrates the metadata generation workflow.
/// </summary>
public interface IMetadataGeneratorService
{
    /// <summary>
    /// Generates all metadata and serializes to the configured output directory.
    /// </summary>
    Task GenerateAsync(CancellationToken ct = default);
}
