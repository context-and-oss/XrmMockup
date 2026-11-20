using Microsoft.Xrm.Sdk.Metadata;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for OptionSetReader.
/// Tests verify the reader correctly retrieves global option sets from the organization.
/// </summary>
public class OptionSetReaderTests : ReaderTestBase
{
    private readonly OptionSetReader _reader;

    public OptionSetReaderTests()
    {
        var logger = CreateLogger<OptionSetReader>();
        _reader = new OptionSetReader(ServiceProvider, logger);
    }

    [Fact]
    public async Task GetOptionSetsAsync_ReturnsOptionSetMetadataBaseArray()
    {
        var result = await _reader.GetOptionSetsAsync();

        Assert.NotNull(result);
        Assert.IsType<OptionSetMetadataBase[]>(result);
    }
}
