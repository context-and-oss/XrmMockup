using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XrmMockup.MetadataGenerator.Core.Models;

namespace XrmMockup.MetadataGenerator.Core.Services;

/// <summary>
/// Orchestrates the metadata generation workflow.
/// </summary>
internal sealed class MetadataGeneratorService(
    IMetadataSource metadataSource,
    IMetadataSerializer serializer,
    IOptions<GeneratorOptions> options,
    ILogger<MetadataGeneratorService> logger) : IMetadataGeneratorService
{
    private readonly GeneratorOptions _options = options.Value;

    public async Task GenerateAsync(CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting metadata generation");
            logger.LogInformation("Output directory: {OutputDirectory}", _options.OutputDirectory);
            logger.LogInformation("Solutions: {Solutions}", string.Join(", ", _options.Solutions));
        }

        // Get metadata
        var skeleton = await metadataSource.GetMetadataAsync(ct);

        // Get workflows
        var workflows = await metadataSource.GetWorkflowsAsync(ct);

        // Get security roles
        var securityRoles = await metadataSource.GetSecurityRolesAsync(
            skeleton.RootBusinessUnit.Id,
            ct);

        // Get the privilege catalog
        var privilegeCatalog = await metadataSource.GetPrivilegeCatalogAsync(ct);

        // Serialize all outputs
        await serializer.SerializeMetadataAsync(skeleton, ct);
        await serializer.SerializeWorkflowsAsync(workflows, ct);
        await serializer.SerializeSecurityRolesAsync(securityRoles, ct);
        await serializer.SerializePrivilegeCatalogAsync(privilegeCatalog, ct);
        await serializer.GenerateTypeDeclarationsAsync(securityRoles, ct);

        logger.LogInformation("Metadata generation completed successfully");
    }
}
