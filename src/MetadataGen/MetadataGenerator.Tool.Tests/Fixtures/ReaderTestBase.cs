extern alias XrmMockupLib;

using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockupLib::DG.Tools.XrmMockup;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

public abstract class ReaderTestBase
{
    protected XrmMockup365 Crm { get; }
    protected IOrganizationService Service { get; }
    protected IOrganizationServiceProvider ServiceProvider { get; }

    protected ReaderTestBase()
    {
        Crm = XrmMockupFactory.CreateMockup();
        Service = Crm.GetAdminService();
        ServiceProvider = new MockOrganizationServiceProvider(Service);
    }

    protected static ILogger<T> CreateLogger<T>() => Substitute.For<ILogger<T>>();
}
