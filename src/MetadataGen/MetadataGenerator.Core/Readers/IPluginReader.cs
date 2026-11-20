using DG.Tools.XrmMockup;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads plugin registrations from Dataverse.
/// </summary>
public interface IPluginReader
{
    /// <summary>
    /// Gets plugin metadata for the specified solutions.
    /// </summary>
    Task<List<MetaPlugin>> GetPluginsAsync(string[] solutions, CancellationToken ct = default);
}
