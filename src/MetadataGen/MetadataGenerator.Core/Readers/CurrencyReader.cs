using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockup.MetadataGenerator.Tool.Context;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads transaction currencies from Dataverse.
/// </summary>
internal sealed class CurrencyReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<CurrencyReader> logger) : ICurrencyReader
{
    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<List<Entity>> GetCurrenciesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Getting currencies");

        return await Task.Run(() =>
        {
            using var xrm = new Xrm(_service);
            var currencies = xrm.TransactionCurrencySet.ToList();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} currencies", currencies.Count);
            }

            // We need to downcast to Entity to ensure serialization works correctly
            return currencies.ConvertAll(c => c.ToEntity<Entity>());
        }, ct);
    }
}
