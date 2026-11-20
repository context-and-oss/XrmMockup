using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Models;
using XrmMockup.MetadataGenerator.Core.Services;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Services;

/// <summary>
/// Tests for MetadataGeneratorService.
/// Tests verify the service correctly orchestrates the generation workflow
/// by calling source and serializer in the correct sequence.
/// </summary>
public class MetadataGeneratorServiceTests
{
    private readonly IMetadataSource _metadataSource;
    private readonly IMetadataSerializer _serializer;
    private readonly ILogger<MetadataGeneratorService> _logger;
    private readonly MetadataSkeleton _skeleton;
    private readonly List<Entity> _workflows;
    private readonly Dictionary<Guid, SecurityRole> _securityRoles;
    private readonly PrivilegeCatalog _privilegeCatalog;

    public MetadataGeneratorServiceTests()
    {
        _metadataSource = Substitute.For<IMetadataSource>();
        _serializer = Substitute.For<IMetadataSerializer>();
        _logger = Substitute.For<ILogger<MetadataGeneratorService>>();

        // Setup default test data
        var rootBuId = Guid.NewGuid();
        _skeleton = new MetadataSkeleton
        {
            EntityMetadata = [],
            Currencies = [],
            Plugins = [],
            OptionSets = [],
            DefaultStateStatus = [],
            BaseOrganization = new Entity("organization", Guid.NewGuid()),
            RootBusinessUnit = new Entity("businessunit", rootBuId)
        };
        _workflows = [new Entity("workflow", Guid.NewGuid())];
        _securityRoles = new Dictionary<Guid, SecurityRole>
        {
            { Guid.NewGuid(), new SecurityRole { Name = "Test Role", RoleId = Guid.NewGuid(), Privileges = [] } }
        };
        _privilegeCatalog = new PrivilegeCatalog { Privileges = [], CustomApis = [] };

        // Setup mocks
        _metadataSource.GetMetadataAsync(Arg.Any<CancellationToken>())
            .Returns(_skeleton);
        _metadataSource.GetWorkflowsAsync(Arg.Any<CancellationToken>())
            .Returns(_workflows);
        _metadataSource.GetSecurityRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_securityRoles);
        _metadataSource.GetPrivilegeCatalogAsync(Arg.Any<CancellationToken>())
            .Returns(_privilegeCatalog);
    }

    private MetadataGeneratorService CreateService(GeneratorOptions options)
    {
        return new MetadataGeneratorService(
            _metadataSource,
            _serializer,
            Options.Create(options),
            _logger);
    }

    [Fact]
    public async Task GenerateAsync_CallsGetMetadataFirst()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        await _metadataSource.Received(1).GetMetadataAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_CallsGetWorkflowsAfterMetadata()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        Received.InOrder(() =>
        {
            _metadataSource.GetMetadataAsync(Arg.Any<CancellationToken>());
            _metadataSource.GetWorkflowsAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task GenerateAsync_PassesRootBusinessUnitIdToGetSecurityRoles()
    {
        var rootBuId = Guid.NewGuid();
        _skeleton.RootBusinessUnit = new Entity("businessunit", rootBuId);

        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        await _metadataSource.Received(1).GetSecurityRolesAsync(
            Arg.Is<Guid>(id => id == rootBuId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SerializesMetadata()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        await _serializer.Received(1).SerializeMetadataAsync(
            Arg.Is<MetadataSkeleton>(s => s == _skeleton),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SerializesWorkflows()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        await _serializer.Received(1).SerializeWorkflowsAsync(
            Arg.Is<IEnumerable<Entity>>(w => w == _workflows),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SerializesSecurityRoles()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        await _serializer.Received(1).SerializeSecurityRolesAsync(
            Arg.Is<Dictionary<Guid, SecurityRole>>(r => r == _securityRoles),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SerializesPrivilegeCatalog()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        await _serializer.Received(1).SerializePrivilegeCatalogAsync(
            Arg.Is<PrivilegeCatalog>(c => c == _privilegeCatalog),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_GeneratesTypeDeclarations()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        await _serializer.Received(1).GenerateTypeDeclarationsAsync(
            Arg.Is<Dictionary<Guid, SecurityRole>>(r => r == _securityRoles),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_CallsSerializersAfterFetchingAllData()
    {
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync();

        Received.InOrder(() =>
        {
            _metadataSource.GetMetadataAsync(Arg.Any<CancellationToken>());
            _metadataSource.GetWorkflowsAsync(Arg.Any<CancellationToken>());
            _metadataSource.GetSecurityRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            _metadataSource.GetPrivilegeCatalogAsync(Arg.Any<CancellationToken>());
            _serializer.SerializeMetadataAsync(Arg.Any<MetadataSkeleton>(), Arg.Any<CancellationToken>());
            _serializer.SerializeWorkflowsAsync(Arg.Any<IEnumerable<Entity>>(), Arg.Any<CancellationToken>());
            _serializer.SerializeSecurityRolesAsync(Arg.Any<Dictionary<Guid, SecurityRole>>(), Arg.Any<CancellationToken>());
            _serializer.SerializePrivilegeCatalogAsync(Arg.Any<PrivilegeCatalog>(), Arg.Any<CancellationToken>());
            _serializer.GenerateTypeDeclarationsAsync(Arg.Any<Dictionary<Guid, SecurityRole>>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task GenerateAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var service = CreateService(options);

        await service.GenerateAsync(cts.Token);

        await _metadataSource.Received(1).GetMetadataAsync(
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
        await _metadataSource.Received(1).GetWorkflowsAsync(
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
        await _serializer.Received(1).SerializeMetadataAsync(
            Arg.Any<MetadataSkeleton>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }
}
