using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for PrivilegeReader.
/// XrmMockup doesn't hold privilege records, so the service returns canned records per table.
/// </summary>
public class PrivilegeReaderTests
{
    private static readonly Guid ReadOrder = Guid.NewGuid();
    private static readonly Guid AppendActivity = Guid.NewGuid();
    private static readonly Guid ActOnBehalf = Guid.NewGuid();

    private readonly IOrganizationService _service = Substitute.For<IOrganizationService>();
    private readonly List<QueryExpression> _queries = [];

    public PrivilegeReaderTests()
    {
        Returns("privilege",
            Privilege(ReadOrder, "prvReadctx_Order", AccessRights.ReadAccess),
            Privilege(AppendActivity, "prvAppendActivity", AccessRights.AppendAccess),
            Privilege(ActOnBehalf, "prvActOnBehalfOfAnotherUser", AccessRights.None),
            Privilege(Guid.NewGuid(), null, AccessRights.ReadAccess));
        Returns("privilegeobjecttypecodes",
            Table(ReadOrder, "ctx_order"),
            Table(AppendActivity, "task"),
            Table(AppendActivity, "email"),
            Table(AppendActivity, "task"),
            Table(ActOnBehalf, PrivilegeReader.NoTable));
        Returns("customapi",
            CustomApi("ctx_Submit", "prvReadctx_Order"),
            CustomApi("ctx_Approve", "prvActOnBehalfOfAnotherUser"),
            CustomApi("ctx_Ping", null));
    }

    private PrivilegeReader CreateReader() =>
        new(new MockOrganizationServiceProvider(_service), Substitute.For<ILogger<PrivilegeReader>>());

    [Fact]
    public async Task GetPrivilegeCatalogAsync_ReturnsNamedPrivilegesSortedByName()
    {
        var catalog = await CreateReader().GetPrivilegeCatalogAsync();

        Assert.Equal(["prvActOnBehalfOfAnotherUser", "prvAppendActivity", "prvReadctx_Order"], catalog.Privileges.Select(p => p.Name));
        Assert.Equal(AccessRights.AppendAccess, catalog.Privileges[1].AccessRight);
        Assert.Equal(AppendActivity, catalog.Privileges[1].PrivilegeId);
    }

    [Fact]
    public async Task GetPrivilegeCatalogAsync_ListsTablesSortedAndDistinct()
    {
        var catalog = await CreateReader().GetPrivilegeCatalogAsync();

        Assert.Equal(["email", "task"], catalog.Privileges.Single(p => p.Name == "prvAppendActivity").Tables);
        Assert.Equal(["ctx_order"], catalog.Privileges.Single(p => p.Name == "prvReadctx_Order").Tables);
    }

    [Fact]
    public async Task GetPrivilegeCatalogAsync_LeavesTablesEmptyForGlobalPrivileges()
    {
        var catalog = await CreateReader().GetPrivilegeCatalogAsync();

        Assert.Empty(catalog.Privileges.Single(p => p.Name == "prvActOnBehalfOfAnotherUser").Tables);
    }

    [Fact]
    public async Task GetPrivilegeCatalogAsync_ReturnsCustomApisSortedByName()
    {
        var catalog = await CreateReader().GetPrivilegeCatalogAsync();

        Assert.Equal(["ctx_Approve", "ctx_Ping", "ctx_Submit"], catalog.CustomApis.Select(a => a.UniqueName));
        Assert.Equal("prvReadctx_Order", catalog.CustomApis[2].ExecutePrivilegeName);
    }

    [Fact]
    public async Task GetPrivilegeCatalogAsync_ReturnsCustomApisWithoutAnExecutePrivilege()
    {
        var catalog = await CreateReader().GetPrivilegeCatalogAsync();

        Assert.Null(catalog.CustomApis.Single(a => a.UniqueName == "ctx_Ping").ExecutePrivilegeName);
        Assert.Empty(_queries.Single(q => q.EntityName == "customapi").Criteria.Conditions);
    }

    private void Returns(string table, params Entity[] entities)
    {
        _service.RetrieveMultiple(Arg.Is<QueryExpression>(q => q.EntityName == table))
            .Returns(call =>
            {
                _queries.Add(call.Arg<QueryExpression>());
                return new EntityCollection([.. entities]);
            });
    }

    private static Entity Privilege(Guid id, string? name, AccessRights accessRight) =>
        new("privilege", id)
        {
            ["name"] = name,
            ["accessright"] = (int)accessRight
        };

    private static Entity Table(Guid privilegeId, string table) =>
        new("privilegeobjecttypecodes", Guid.NewGuid())
        {
            ["privilegeid"] = new EntityReference("privilege", privilegeId),
            ["objecttypecode"] = table
        };

    private static Entity CustomApi(string uniqueName, string? executePrivilegeName) =>
        new("customapi", Guid.NewGuid())
        {
            ["uniquename"] = uniqueName,
            ["executeprivilegename"] = executePrivilegeName
        };
}
