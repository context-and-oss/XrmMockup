using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Context;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for EntityMetadataReader.
/// Tests verify the reader correctly retrieves and filters entity metadata.
/// </summary>
public class EntityMetadataReaderTests : ReaderTestBase
{
    private readonly EntityMetadataReader _reader;

    public EntityMetadataReaderTests()
    {
        var logger = CreateLogger<EntityMetadataReader>();
        _reader = new EntityMetadataReader(ServiceProvider, logger);
    }

    [Fact]
    public async Task GetEntityMetadataAsync_WithNoSolutionsAndNoEntities_ReturnsEmptyDictionary()
    {
        var result = await _reader.GetEntityMetadataAsync([], []);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEntityMetadataAsync_WithEntityList_ReturnsRequestedEntities()
    {
        var result = await _reader.GetEntityMetadataAsync([], [Account.EntityLogicalName]);

        Assert.Single(result);
        Assert.True(result.ContainsKey(Account.EntityLogicalName));
    }

    [Fact]
    public async Task GetEntityMetadataAsync_WithMultipleEntities_ReturnsExactRequestedEntities()
    {
        var result = await _reader.GetEntityMetadataAsync([], [Account.EntityLogicalName, Contact.EntityLogicalName, BusinessUnit.EntityLogicalName]);

        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey(Account.EntityLogicalName));
        Assert.True(result.ContainsKey(Contact.EntityLogicalName));
        Assert.True(result.ContainsKey(BusinessUnit.EntityLogicalName));
    }

    [Fact]
    public async Task GetEntityMetadataAsync_WithDuplicateEntities_DeduplicatesByLogicalName()
    {
        var result = await _reader.GetEntityMetadataAsync([], [Account.EntityLogicalName, Account.EntityLogicalName]);

        Assert.Single(result);
        Assert.True(result.ContainsKey(Account.EntityLogicalName));
    }

    [Fact]
    public async Task GetEntityMetadataAsync_WithSolutionName_AndNoComponents_ReturnsEmpty()
    {
        var publisherId = Guid.NewGuid();
        var publisher = new Entity("publisher", publisherId);
        publisher["uniquename"] = "testsolutionpublisher";
        publisher["friendlyname"] = "Test Solution Publisher";
        publisher["customizationprefix"] = "test";
        Service.Create(publisher);

        var solutionId = Guid.NewGuid();
        var solution = new Solution(solutionId)
        {
            UniqueName = "TestSolution",
            FriendlyName = "Test Solution",
            Version = "1.0.0.0",
            IsManaged = false,
            PublisherId = new EntityReference("publisher", publisherId)
        };
        Service.Create(solution);

        var result = await _reader.GetEntityMetadataAsync(["TestSolution"], []);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that GetEntityMetadataAsync correctly uses ObjectId (not Id) when querying SolutionComponents.
    /// This test verifies the fix for a bug where the reader was querying by SolutionComponent.Id
    /// instead of SolutionComponent.ObjectId to get entity metadata IDs.
    /// </summary>
    /// <remarks>
    /// SKIP REASON: XrmMockup does not persist the 'objectid' attribute on solutioncomponent entities.
    /// The attribute exists in metadata but XrmMockup filters it out during Create operations.
    /// This test would pass against a real Dataverse environment.
    /// The bug was: using c.Id instead of c.ObjectId in the LINQ query in GetEntityComponentIdsFromSolution.
    /// </remarks>
    [Fact(Skip = "XrmMockup does not support storing objectid attribute on solutioncomponent entities")]
    public async Task GetEntityMetadataAsync_WithSolutionName_AndEntityComponents_ReturnsEntitiesFromSolution()
    {
        // Arrange: Create publisher
        var publisherId = Guid.NewGuid();
        var publisher = new Entity("publisher", publisherId);
        publisher["uniquename"] = "testsolutionpublisher2";
        publisher["friendlyname"] = "Test Solution Publisher 2";
        publisher["customizationprefix"] = "test";
        Service.Create(publisher);

        // Arrange: Create solution
        var solutionId = Guid.NewGuid();
        var solution = new Solution(solutionId)
        {
            UniqueName = "TestSolutionWithEntities",
            FriendlyName = "Test Solution With Entities",
            Version = "1.0.0.0",
            IsManaged = false,
            PublisherId = new EntityReference("publisher", publisherId)
        };
        Service.Create(solution);

        // Arrange: Get Account entity metadata ID
        var accountMetadataResponse = (RetrieveEntityResponse)Service.Execute(new RetrieveEntityRequest
        {
            LogicalName = Account.EntityLogicalName,
            EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity
        });
        var accountMetadataId = accountMetadataResponse.EntityMetadata.MetadataId!.Value;

        // Arrange: Create SolutionComponent linking the entity to the solution
        // This tests that ObjectId (entity metadata ID) is used, not the SolutionComponent's own Id
        // Use late-bound to ensure all attributes are set correctly
        var solutionComponentId = Guid.NewGuid();
        var solutionComponent = new Entity(SolutionComponent.EntityLogicalName, solutionComponentId);
        solutionComponent["solutionid"] = new EntityReference(Solution.EntityLogicalName, solutionId);
        solutionComponent["componenttype"] = new OptionSetValue((int)componenttype.Entity);
        solutionComponent["objectid"] = accountMetadataId;
        Service.Create(solutionComponent);

        // Verify: Check the component was created and can be queried
        var retrievedComponent = Service.Retrieve(
            SolutionComponent.EntityLogicalName,
            solutionComponentId,
            new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
        Assert.NotNull(retrievedComponent);
        Assert.Equal(accountMetadataId, retrievedComponent.GetAttributeValue<Guid?>("objectid"));

        // Act
        var result = await _reader.GetEntityMetadataAsync(["TestSolutionWithEntities"], []);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey(Account.EntityLogicalName));
    }
}
