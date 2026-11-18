using Microsoft.Xrm.Sdk.Metadata;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads entity metadata from Dataverse.
/// </summary>
public interface IEntityMetadataReader
{
    /// <summary>
    /// Gets entity metadata for the specified solutions and entities.
    /// </summary>
    Task<SortedDictionary<string, EntityMetadata>> GetEntityMetadataAsync(
        string[] solutions,
        string[] entities,
        CancellationToken ct = default);
}
