extern alias XrmMockupLib;
using Microsoft.Extensions.Logging;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for PluginReader using XrmMockup.
/// Note: XrmMockup has limitations with plugin system entities and solution components.
/// These tests focus on what XrmMockup can support from metadata.
/// </summary>
public class PluginReaderTests : ReaderTestBase
{
    private readonly PluginReader _reader;
    private readonly ILogger<PluginReader> _logger;

    public PluginReaderTests()
    {
        _logger = CreateLogger<PluginReader>();
        _reader = new PluginReader(ServiceProvider, _logger);
    }

    [Fact]
    public async Task GetPluginsAsync_WithNoSolutions_ReturnsEmptyList()
    {
        // Act
        var result = await _reader.GetPluginsAsync([]);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPluginsAsync_WithNonExistentSolution_ReturnsEmptyList()
    {
        // Arrange
        var solutions = new[] { "NonExistentSolution" };

        // Act
        var result = await _reader.GetPluginsAsync(solutions);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPluginsAsync_ReturnsListOfMetaPlugins()
    {
        var result = await _reader.GetPluginsAsync([]);

        Assert.NotNull(result);
        Assert.True(result.GetType().IsGenericType);
        Assert.Contains("MetaPlugin", result.GetType().GetGenericArguments()[0].Name);
    }
}
