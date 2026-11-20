using Xunit;
using CR = XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Context;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers.OrganizationReader;

/// <summary>
/// Tests for OrganizationReader.GetDefaultStateStatusAsync.
/// Note: XrmMockup doesn't properly support the statusmap entity.
/// When creating statusmap records, the objecttypecode attribute is not returned on queries.
/// These tests would require a real Dataverse instance.
/// </summary>
public class GetDefaultStateStatusTests : ReaderTestBase
{
    private readonly CR.OrganizationReader _reader;

    public GetDefaultStateStatusTests()
    {
        var logger = CreateLogger<CR.OrganizationReader>();
        _reader = new CR.OrganizationReader(ServiceProvider, logger);
    }

    [Fact(Skip = "XrmMockup doesn't return objecttypecode from statusmap entities")]
    public async Task GetDefaultStateStatusAsync_ReturnsDefaultStatuses()
    {
        // Arrange - Create status map entities in XrmMockup
        var statusMap1 = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = Account.EntityLogicalName,
            State = 0,
            Status = 1,
            IsDefault = true
        };
        Service.Create(statusMap1);

        var statusMap2 = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = Contact.EntityLogicalName,
            State = 0,
            Status = 1,
            IsDefault = true
        };
        Service.Create(statusMap2);

        // Act
        var result = await _reader.GetDefaultStateStatusAsync([Account.EntityLogicalName, Contact.EntityLogicalName]);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(Account.EntityLogicalName));
        Assert.True(result.ContainsKey(Contact.EntityLogicalName));
        Assert.Equal(1, result[Account.EntityLogicalName][0]);
        Assert.Equal(1, result[Contact.EntityLogicalName][0]);
    }

    [Fact(Skip = "XrmMockup doesn't return objecttypecode from statusmap entities")]
    public async Task GetDefaultStateStatusAsync_WithMultipleStates_GroupsByEntity()
    {
        // Arrange - Create status map entities with multiple states using entity in our metadata
        var statusMapActive = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = SystemUser.EntityLogicalName,
            State = 0, // Active
            Status = 1,
            IsDefault = true
        };
        Service.Create(statusMapActive);

        var statusMapInactive = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = SystemUser.EntityLogicalName,
            State = 1, // Inactive
            Status = 2,
            IsDefault = true
        };
        Service.Create(statusMapInactive);

        // Act
        var result = await _reader.GetDefaultStateStatusAsync([SystemUser.EntityLogicalName]);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(SystemUser.EntityLogicalName));
        Assert.Equal(2, result[SystemUser.EntityLogicalName].Count);
        Assert.Equal(1, result[SystemUser.EntityLogicalName][0]); // Active state's default status
        Assert.Equal(2, result[SystemUser.EntityLogicalName][1]); // Inactive state's default status
    }

    [Fact(Skip = "XrmMockup doesn't return objecttypecode from statusmap entities")]
    public async Task GetDefaultStateStatusAsync_WithNonDefaultStatus_ExcludesNonDefault()
    {
        // Arrange - Create status map entities with default and non-default using entity in our metadata
        var defaultStatus = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = Team.EntityLogicalName,
            State = 0,
            Status = 1,
            IsDefault = true
        };
        Service.Create(defaultStatus);

        var nonDefaultStatus = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = Team.EntityLogicalName,
            State = 0,
            Status = 2,
            IsDefault = false
        };
        Service.Create(nonDefaultStatus);

        // Act
        var result = await _reader.GetDefaultStateStatusAsync([Team.EntityLogicalName]);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(Team.EntityLogicalName));
        Assert.Single(result[Team.EntityLogicalName]);
        Assert.Equal(1, result[Team.EntityLogicalName][0]);
    }

    [Fact]
    public async Task GetDefaultStateStatusAsync_ReturnsValidDictionary()
    {
        var result = await _reader.GetDefaultStateStatusAsync([]);

        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, Dictionary<int, int>>>(result);
    }

    [Fact(Skip = "XrmMockup doesn't return objecttypecode from statusmap entities")]
    public async Task GetDefaultStateStatusAsync_WithDuplicateStateForEntity_UsesFirstDefault()
    {
        // Arrange - Two defaults for the same state (should only keep first one)
        var statusMap1 = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = Solution.EntityLogicalName,
            State = 0,
            Status = 1,
            IsDefault = true
        };
        Service.Create(statusMap1);

        var statusMap2 = new StatusMap(Guid.NewGuid())
        {
            ObjectTypeCode = Solution.EntityLogicalName,
            State = 0,
            Status = 99, // Different status for same state
            IsDefault = true
        };
        Service.Create(statusMap2);

        // Act
        var result = await _reader.GetDefaultStateStatusAsync([Solution.EntityLogicalName]);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(Solution.EntityLogicalName));
        Assert.Single(result[Solution.EntityLogicalName]);
        Assert.Equal(1, result[Solution.EntityLogicalName][0]); // First one should win
    }

    [Fact(Skip = "XrmMockup doesn't return objecttypecode from statusmap entities")]
    public async Task GetDefaultStateStatusAsync_WithManyEntities_GroupsCorrectly()
    {
        // Arrange - Create status maps for multiple entities using entities in our metadata
        var entities = new[] { Account.EntityLogicalName, Contact.EntityLogicalName, BusinessUnit.EntityLogicalName, SystemUser.EntityLogicalName, Team.EntityLogicalName };
        var statusIndex = 10;
        foreach (var entity in entities)
        {
            var statusMap = new StatusMap(Guid.NewGuid())
            {
                ObjectTypeCode = entity,
                State = 0,
                Status = statusIndex++,
                IsDefault = true
            };
            Service.Create(statusMap);
        }

        // Act
        var result = await _reader.GetDefaultStateStatusAsync(entities);

        // Assert
        Assert.NotNull(result);
        foreach (var entity in entities)
        {
            Assert.True(result.ContainsKey(entity));
        }
    }
}
