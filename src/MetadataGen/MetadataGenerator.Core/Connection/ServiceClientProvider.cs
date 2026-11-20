using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace XrmMockup.MetadataGenerator.Core.Connection;

/// <summary>
/// Production implementation of IOrganizationServiceProvider using ServiceClient.
/// </summary>
internal sealed class ServiceClientProvider(ServiceClient serviceClient) : IOrganizationServiceProvider
{
    public IOrganizationService Service => serviceClient;

    public string ConnectedHost => serviceClient.ConnectedOrgUriActual.GetLeftPart(UriPartial.Authority);

    public Guid ConnectedOrgId => serviceClient.ConnectedOrgId;
}
