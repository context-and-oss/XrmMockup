using Microsoft.Xrm.Sdk;
using XrmMockup.MetadataGenerator.Core.Connection;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

public sealed class MockOrganizationServiceProvider(IOrganizationService service) : IOrganizationServiceProvider
{
    public IOrganizationService Service { get; } = service;
    public string ConnectedHost => "test.crm.dynamics.com";
    public Guid ConnectedOrgId => new("22fe4299-c8bb-f011-89f5-000d3ab73c58");
}
