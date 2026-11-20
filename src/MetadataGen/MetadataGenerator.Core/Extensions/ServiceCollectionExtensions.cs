using DataverseConnection;
using Microsoft.Extensions.DependencyInjection;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Core.Services;

namespace XrmMockup.MetadataGenerator.Core.Extensions;

/// <summary>
/// Extension methods for registering MetadataGenerator services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Dataverse connection and all metadata generator services.
    /// </summary>
    public static IServiceCollection AddMetadataGenerator(this IServiceCollection services)
    {
        services.AddDataverse();
        services.AddSingleton<IOrganizationServiceProvider, ServiceClientProvider>();
        return services.AddMetadataGeneratorServices();
    }

    /// <summary>
    /// Registers metadata generator services without Dataverse connection.
    /// Use this when providing your own IOrganizationServiceProvider.
    /// </summary>
    public static IServiceCollection AddMetadataGeneratorServices(this IServiceCollection services)
    {
        // Readers
        services.AddSingleton<IEntityMetadataReader, EntityMetadataReader>();
        services.AddSingleton<IPluginReader, PluginReader>();
        services.AddSingleton<IWorkflowReader, WorkflowReader>();
        services.AddSingleton<ISecurityRoleReader, SecurityRoleReader>();
        services.AddSingleton<IPrivilegeReader, PrivilegeReader>();
        services.AddSingleton<IOptionSetReader, OptionSetReader>();
        services.AddSingleton<ICurrencyReader, CurrencyReader>();
        services.AddSingleton<IOrganizationReader, OrganizationReader>();

        // Services
        services.AddSingleton<IMetadataSerializer, DataContractMetadataSerializer>();
        services.AddSingleton<IMetadataSource, OnlineMetadataSource>();
        services.AddSingleton<IMetadataGeneratorService, MetadataGeneratorService>();

        return services;
    }
}
