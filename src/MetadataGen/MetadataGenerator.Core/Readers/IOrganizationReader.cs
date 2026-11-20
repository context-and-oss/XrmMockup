using Microsoft.Xrm.Sdk;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads organization and business unit data from Dataverse.
/// </summary>
public interface IOrganizationReader
{
    /// <summary>
    /// Gets the base organization entity.
    /// </summary>
    Task<Entity> GetBaseOrganizationAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the root business unit.
    /// </summary>
    Task<Entity> GetRootBusinessUnitAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the default state/status mappings for the specified entities.
    /// </summary>
    /// <param name="entityLogicalNames">The entity logical names to get mappings for.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Dictionary<string, Dictionary<int, int>>> GetDefaultStateStatusAsync(
        IEnumerable<string> entityLogicalNames,
        CancellationToken ct = default);
}
