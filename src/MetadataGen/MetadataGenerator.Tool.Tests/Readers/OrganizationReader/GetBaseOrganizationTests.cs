using Xunit;
using CR = XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers.OrganizationReader;

/// <summary>
/// Tests for OrganizationReader.GetBaseOrganizationAsync.
/// </summary>
public class GetBaseOrganizationTests : ReaderTestBase
{
    private readonly CR.OrganizationReader _reader;

    public GetBaseOrganizationTests()
    {
        var logger = CreateLogger<CR.OrganizationReader>();
        _reader = new CR.OrganizationReader(ServiceProvider, logger);
    }

    [Fact(Skip = "XrmMockup fails to format organization attribute values due to missing metadata")]
    public async Task GetBaseOrganizationAsync_ReturnsOrganization()
    {
        var result = await _reader.GetBaseOrganizationAsync();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("organization", result.LogicalName);
    }

    [Fact(Skip = "XrmMockup fails to format organization attribute values due to missing metadata")]
    public async Task GetBaseOrganizationAsync_ReturnsOrganizationWithAttributes()
    {
        var result = await _reader.GetBaseOrganizationAsync();

        Assert.NotNull(result);
        Assert.NotEmpty(result.Attributes);
    }
}
