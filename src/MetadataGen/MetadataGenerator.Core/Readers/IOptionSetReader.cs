using Microsoft.Xrm.Sdk.Metadata;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads global option sets from Dataverse.
/// </summary>
public interface IOptionSetReader
{
    /// <summary>
    /// Gets all global option sets.
    /// </summary>
    Task<OptionSetMetadataBase[]> GetOptionSetsAsync(CancellationToken ct = default);
}
