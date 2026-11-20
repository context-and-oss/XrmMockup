using DG.Tools.XrmMockup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using XrmMockup.MetadataGenerator.Core.Models;
using XrmMockup.MetadataGenerator.Core.Readers;

namespace XrmMockup.MetadataGenerator.Core.Services;

/// <summary>
/// Fetches metadata from an online Dataverse environment.
/// </summary>
internal sealed class OnlineMetadataSource(
    IEntityMetadataReader entityMetadataReader,
    IPluginReader pluginReader,
    IWorkflowReader workflowReader,
    ISecurityRoleReader securityRoleReader,
    IOptionSetReader optionSetReader,
    ICurrencyReader currencyReader,
    IOrganizationReader organizationReader,
    IPrivilegeReader privilegeReader,
    IOptions<GeneratorOptions> options,
    ILogger<OnlineMetadataSource> logger) : IMetadataSource
{
    private readonly GeneratorOptions _options = options.Value;

    public async Task<MetadataSkeleton> GetMetadataAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching metadata from Dataverse");

        var skeleton = new MetadataSkeleton();

        // Combine default entities with configured entities
        var allEntities = GeneratorOptions.DefaultEntities
            .Concat(_options.Entities)
            .Distinct()
            .ToArray();

        // Get entity metadata
        skeleton.EntityMetadata = await entityMetadataReader.GetEntityMetadataAsync(
            _options.Solutions,
            allEntities,
            ct);

        // Get currencies
        skeleton.Currencies = await currencyReader.GetCurrenciesAsync(ct);

        // Get plugins
        skeleton.Plugins = await pluginReader.GetPluginsAsync(_options.Solutions, ct);

        // Get organization
        skeleton.BaseOrganization = await organizationReader.GetBaseOrganizationAsync(ct);

        // Get root business unit
        skeleton.RootBusinessUnit = await organizationReader.GetRootBusinessUnitAsync(ct);

        // Get option sets
        skeleton.OptionSets = await optionSetReader.GetOptionSetsAsync(ct);

        // Get default state/status for only the entities we have metadata for
        skeleton.DefaultStateStatus = await organizationReader.GetDefaultStateStatusAsync(
            skeleton.EntityMetadata.Keys,
            ct);

        logger.LogInformation("Metadata fetching complete");
        return skeleton;
    }

    public async Task<IEnumerable<Entity>> GetWorkflowsAsync(CancellationToken ct = default)
    {
        return await workflowReader.GetWorkflowsAsync(ct);
    }

    public async Task<Dictionary<Guid, SecurityRole>> GetSecurityRolesAsync(
        Guid rootBusinessUnitId,
        CancellationToken ct = default)
    {
        return await securityRoleReader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            _options.Solutions,
            _options.SecurityRoles,
            _options.AllSecurityRoles,
            ct);
    }

    public async Task<PrivilegeCatalog> GetPrivilegeCatalogAsync(CancellationToken ct = default)
    {
        return await privilegeReader.GetPrivilegeCatalogAsync(ct);
    }
}
