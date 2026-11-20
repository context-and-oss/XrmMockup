using Microsoft.Xrm.Sdk;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads transaction currencies from Dataverse.
/// </summary>
public interface ICurrencyReader
{
    /// <summary>
    /// Gets all transaction currencies.
    /// </summary>
    Task<List<Entity>> GetCurrenciesAsync(CancellationToken ct = default);
}
