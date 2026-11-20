using Xunit;
using XrmMockup.MetadataGenerator.Tool.Context;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;
using CR = XrmMockup.MetadataGenerator.Core.Readers;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers.OrganizationReader;

/// <summary>
/// Tests for OrganizationReader.GetRootBusinessUnitAsync.
/// </summary>
public class GetRootBusinessUnitTests : ReaderTestBase
{
    private readonly CR.OrganizationReader _reader;

    public GetRootBusinessUnitTests()
    {
        var logger = CreateLogger<CR.OrganizationReader>();
        _reader = new CR.OrganizationReader(ServiceProvider, logger);
    }

    [Fact]
    public async Task GetRootBusinessUnitAsync_ReturnsBusinessUnitWithNoParent()
    {
        // Act
        var result = await _reader.GetRootBusinessUnitAsync();

        // Assert
        Assert.NotNull(result);
        var businessUnit = result.ToEntity<BusinessUnit>();
        Assert.NotEqual(Guid.Empty, businessUnit.Id);
        Assert.NotEmpty(businessUnit.Attributes);
        // Root business unit should not have a parent
        Assert.Null(businessUnit.ParentBusinessUnitId);

    }
}
