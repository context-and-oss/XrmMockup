using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.MetadataGenerator.Core.Connection;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads global option sets from Dataverse.
/// </summary>
internal sealed class OptionSetReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<OptionSetReader> logger) : IOptionSetReader
{
    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<OptionSetMetadataBase[]> GetOptionSetsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Getting all option sets");

        return await Task.Run(() =>
        {
            var optionSetRequest = new RetrieveAllOptionSetsRequest
            {
                RetrieveAsIfPublished = true
            };

            var results = (RetrieveAllOptionSetsResponse)_service.Execute(optionSetRequest);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} option sets", results.OptionSetMetadata.Length);
            }

            return results.OptionSetMetadata;
        }, ct);
    }
}
