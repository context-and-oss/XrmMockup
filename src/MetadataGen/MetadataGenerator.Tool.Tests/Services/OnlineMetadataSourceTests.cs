using DG.Tools.XrmMockup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using NSubstitute;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Models;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Core.Services;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Services;

/// <summary>
/// Tests for OnlineMetadataSource.
/// Tests verify the service correctly aggregates data from multiple readers
/// and properly combines default entities with configured entities.
/// </summary>
public class OnlineMetadataSourceTests
{
    private readonly IEntityMetadataReader _entityMetadataReader;
    private readonly IPluginReader _pluginReader;
    private readonly IWorkflowReader _workflowReader;
    private readonly ISecurityRoleReader _securityRoleReader;
    private readonly IOptionSetReader _optionSetReader;
    private readonly ICurrencyReader _currencyReader;
    private readonly IOrganizationReader _organizationReader;
    private readonly IPrivilegeReader _privilegeReader;
    private readonly ILogger<OnlineMetadataSource> _logger;

    public OnlineMetadataSourceTests()
    {
        _entityMetadataReader = Substitute.For<IEntityMetadataReader>();
        _pluginReader = Substitute.For<IPluginReader>();
        _workflowReader = Substitute.For<IWorkflowReader>();
        _securityRoleReader = Substitute.For<ISecurityRoleReader>();
        _optionSetReader = Substitute.For<IOptionSetReader>();
        _currencyReader = Substitute.For<ICurrencyReader>();
        _organizationReader = Substitute.For<IOrganizationReader>();
        _privilegeReader = Substitute.For<IPrivilegeReader>();
        _logger = Substitute.For<ILogger<OnlineMetadataSource>>();

        // Setup default mock returns
        _entityMetadataReader.GetEntityMetadataAsync(Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _pluginReader.GetPluginsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetWorkflowsAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _securityRoleReader.GetSecurityRolesAsync(Arg.Any<Guid>(), Arg.Any<string[]>(), Arg.Any<string[]?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _optionSetReader.GetOptionSetsAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _currencyReader.GetCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _organizationReader.GetBaseOrganizationAsync(Arg.Any<CancellationToken>())
            .Returns(new Entity("organization", Guid.NewGuid()));
        _organizationReader.GetRootBusinessUnitAsync(Arg.Any<CancellationToken>())
            .Returns(new Entity("businessunit", Guid.NewGuid()));
        _organizationReader.GetDefaultStateStatusAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private OnlineMetadataSource CreateSource(GeneratorOptions options)
    {
        return new OnlineMetadataSource(
            _entityMetadataReader,
            _pluginReader,
            _workflowReader,
            _securityRoleReader,
            _optionSetReader,
            _currencyReader,
            _organizationReader,
            _privilegeReader,
            Options.Create(options),
            _logger);
    }

    [Fact]
    public async Task GetMetadataAsync_WithNoConfiguredEntities_PassesDefaultEntitiesToReader()
    {
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = [],
            Entities = []
        };
        var source = CreateSource(options);

        await source.GetMetadataAsync();

        await _entityMetadataReader.Received(1).GetEntityMetadataAsync(
            Arg.Any<string[]>(),
            Arg.Is<string[]>(entities => entities.SequenceEqual(GeneratorOptions.DefaultEntities)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_WithConfiguredEntities_CombinesDefaultAndConfiguredEntities()
    {
        var customEntities = new[] { "account", "contact" };
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = [],
            Entities = customEntities
        };
        var source = CreateSource(options);

        await source.GetMetadataAsync();

        await _entityMetadataReader.Received(1).GetEntityMetadataAsync(
            Arg.Any<string[]>(),
            Arg.Is<string[]>(entities =>
                GeneratorOptions.DefaultEntities.All(e => entities.Contains(e)) &&
                customEntities.All(e => entities.Contains(e))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_WithDuplicateEntities_DeduplicatesBeforeCallingReader()
    {
        // "businessunit" is already in DefaultEntities
        var customEntities = new[] { "businessunit", "account" };
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = [],
            Entities = customEntities
        };
        var source = CreateSource(options);

        await source.GetMetadataAsync();

        await _entityMetadataReader.Received(1).GetEntityMetadataAsync(
            Arg.Any<string[]>(),
            Arg.Is<string[]>(entities =>
                entities.Count(e => e == "businessunit") == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_PassesSolutionsToEntityMetadataReader()
    {
        var solutions = new[] { "MySolution", "OtherSolution" };
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = solutions,
            Entities = []
        };
        var source = CreateSource(options);

        await source.GetMetadataAsync();

        await _entityMetadataReader.Received(1).GetEntityMetadataAsync(
            Arg.Is<string[]>(s => s.SequenceEqual(solutions)),
            Arg.Any<string[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_PassesSolutionsToPluginReader()
    {
        var solutions = new[] { "MySolution" };
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = solutions,
            Entities = []
        };
        var source = CreateSource(options);

        await source.GetMetadataAsync();

        await _pluginReader.Received(1).GetPluginsAsync(
            Arg.Is<string[]>(s => s.SequenceEqual(solutions)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_CallsAllReaders()
    {
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = [],
            Entities = []
        };
        var source = CreateSource(options);

        await source.GetMetadataAsync();

        await _entityMetadataReader.Received(1).GetEntityMetadataAsync(Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        await _pluginReader.Received(1).GetPluginsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        await _currencyReader.Received(1).GetCurrenciesAsync(Arg.Any<CancellationToken>());
        await _organizationReader.Received(1).GetBaseOrganizationAsync(Arg.Any<CancellationToken>());
        await _organizationReader.Received(1).GetRootBusinessUnitAsync(Arg.Any<CancellationToken>());
        await _optionSetReader.Received(1).GetOptionSetsAsync(Arg.Any<CancellationToken>());
        await _organizationReader.Received(1).GetDefaultStateStatusAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsMetadataSkeletonWithAllReaderResults()
    {
        var entityMetadata = new Dictionary<string, EntityMetadata>
        {
            { "account", new EntityMetadata { LogicalName = "account" } }
        };
        var currencies = new List<Entity> { new("transactioncurrency", Guid.NewGuid()) };
        var plugins = new List<MetaPlugin> { new() { Name = "TestPlugin" } };
        var optionSets = new OptionSetMetadataBase[] { new OptionSetMetadata { Name = "TestOptionSet" } };
        var defaultStateStatus = new Dictionary<string, Dictionary<int, int>>
        {
            { "account", new Dictionary<int, int> { { 0, 1 } } }
        };
        var baseOrg = new Entity("organization", Guid.NewGuid());
        var rootBu = new Entity("businessunit", Guid.NewGuid());

        _entityMetadataReader.GetEntityMetadataAsync(Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(entityMetadata);
        _currencyReader.GetCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(currencies);
        _pluginReader.GetPluginsAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(plugins);
        _optionSetReader.GetOptionSetsAsync(Arg.Any<CancellationToken>())
            .Returns(optionSets);
        _organizationReader.GetDefaultStateStatusAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(defaultStateStatus);
        _organizationReader.GetBaseOrganizationAsync(Arg.Any<CancellationToken>())
            .Returns(baseOrg);
        _organizationReader.GetRootBusinessUnitAsync(Arg.Any<CancellationToken>())
            .Returns(rootBu);

        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var source = CreateSource(options);

        var result = await source.GetMetadataAsync();

        Assert.Same(entityMetadata, result.EntityMetadata);
        Assert.Same(currencies, result.Currencies);
        Assert.Same(plugins, result.Plugins);
        Assert.Same(optionSets, result.OptionSets);
        Assert.Same(defaultStateStatus, result.DefaultStateStatus);
        Assert.Same(baseOrg, result.BaseOrganization);
        Assert.Same(rootBu, result.RootBusinessUnit);
    }

    [Fact]
    public async Task GetWorkflowsAsync_DelegatesToWorkflowReader()
    {
        var workflows = new List<Entity> { new("workflow", Guid.NewGuid()) };
        _workflowReader.GetWorkflowsAsync(Arg.Any<CancellationToken>())
            .Returns(workflows);

        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var source = CreateSource(options);

        var result = await source.GetWorkflowsAsync();

        Assert.Same(workflows, result);
        await _workflowReader.Received(1).GetWorkflowsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecurityRolesAsync_PassesRootBusinessUnitIdToReader()
    {
        var rootBuId = Guid.NewGuid();
        var roles = new Dictionary<Guid, SecurityRole>();
        _securityRoleReader.GetSecurityRolesAsync(Arg.Any<Guid>(), Arg.Any<string[]>(), Arg.Any<string[]?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(roles);

        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var source = CreateSource(options);

        var result = await source.GetSecurityRolesAsync(rootBuId);

        Assert.Same(roles, result);
        await _securityRoleReader.Received(1).GetSecurityRolesAsync(
            Arg.Is<Guid>(id => id == rootBuId),
            Arg.Any<string[]>(),
            Arg.Any<string[]?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecurityRolesAsync_PassesSolutionsToReader()
    {
        var rootBuId = Guid.NewGuid();
        var solutions = new[] { "MySolution", "OtherSolution" };
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = solutions,
            Entities = []
        };
        var source = CreateSource(options);

        await source.GetSecurityRolesAsync(rootBuId);

        await _securityRoleReader.Received(1).GetSecurityRolesAsync(
            Arg.Any<Guid>(),
            Arg.Is<string[]>(s => s.SequenceEqual(solutions)),
            Arg.Any<string[]?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecurityRolesAsync_PassesSecurityRolesToReader()
    {
        var rootBuId = Guid.NewGuid();
        var securityRoles = new[] { "System Administrator", "Basic User" };
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = [],
            Entities = [],
            SecurityRoles = securityRoles
        };
        var source = CreateSource(options);

        await source.GetSecurityRolesAsync(rootBuId);

        await _securityRoleReader.Received(1).GetSecurityRolesAsync(
            Arg.Any<Guid>(),
            Arg.Any<string[]>(),
            Arg.Is<string[]?>(r => r != null && r.SequenceEqual(securityRoles)),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecurityRolesAsync_WithSolutionsAndSecurityRoles_PassesBothToReader()
    {
        var rootBuId = Guid.NewGuid();
        var solutions = new[] { "MySolution" };
        var securityRoles = new[] { "System Administrator" };
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = solutions,
            Entities = [],
            SecurityRoles = securityRoles
        };
        var source = CreateSource(options);

        await source.GetSecurityRolesAsync(rootBuId);

        await _securityRoleReader.Received(1).GetSecurityRolesAsync(
            Arg.Is<Guid>(id => id == rootBuId),
            Arg.Is<string[]>(s => s.SequenceEqual(solutions)),
            Arg.Is<string[]?>(r => r != null && r.SequenceEqual(securityRoles)),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecurityRolesAsync_WithEmptySolutionsAndSecurityRoles_PassesEmptyArrays()
    {
        var rootBuId = Guid.NewGuid();
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            Solutions = [],
            Entities = [],
            SecurityRoles = []
        };
        var source = CreateSource(options);

        await source.GetSecurityRolesAsync(rootBuId);

        await _securityRoleReader.Received(1).GetSecurityRolesAsync(
            Arg.Is<Guid>(id => id == rootBuId),
            Arg.Is<string[]>(s => s.Length == 0),
            Arg.Is<string[]?>(r => r != null && r.Length == 0),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var options = new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] };
        var source = CreateSource(options);

        await source.GetMetadataAsync(cts.Token);

        await _entityMetadataReader.Received(1).GetEntityMetadataAsync(
            Arg.Any<string[]>(),
            Arg.Any<string[]>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task GetPrivilegeCatalogAsync_ReturnsCatalogFromReader()
    {
        var catalog = new PrivilegeCatalog { Privileges = [], CustomApis = [] };
        _privilegeReader.GetPrivilegeCatalogAsync(Arg.Any<CancellationToken>()).Returns(catalog);
        var source = CreateSource(new GeneratorOptions { OutputDirectory = "/test", Solutions = [], Entities = [] });

        var result = await source.GetPrivilegeCatalogAsync();

        Assert.Same(catalog, result);
    }
}
