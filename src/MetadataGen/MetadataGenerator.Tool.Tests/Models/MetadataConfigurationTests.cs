extern alias MetadataGenTool;
using MetadataGenTool::XrmMockup.MetadataGenerator.Tool.Options;
using Xunit;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Models;

/// <summary>
/// Tests for MetadataConfiguration model (appsettings.json binding).
/// Verifies that the SecurityRoles property is correctly defined and accessible.
/// </summary>
public class MetadataConfigurationTests
{
    [Fact]
    public void SectionPath_IsCorrectlyDefined()
    {
        // Assert
        Assert.Equal("XrmMockup:Metadata", MetadataConfiguration.SectionPath);
    }

    [Fact]
    public void SecurityRoles_DefaultsToNull()
    {
        // Arrange & Act
        var config = new MetadataConfiguration();

        // Assert
        Assert.Null(config.SecurityRoles);
    }

    [Fact]
    public void SecurityRoles_CanBeSetToValues()
    {
        // Arrange
        var securityRoles = new[] { "System Administrator", "Basic User" };

        // Act
        var config = new MetadataConfiguration
        {
            SecurityRoles = securityRoles
        };

        // Assert
        Assert.Equal(securityRoles, config.SecurityRoles);
        Assert.Equal(2, config.SecurityRoles.Length);
        Assert.Contains("System Administrator", config.SecurityRoles);
        Assert.Contains("Basic User", config.SecurityRoles);
    }

    [Fact]
    public void SecurityRoles_CanBeSetToSingleValue()
    {
        // Arrange
        var securityRoles = new[] { "Custom Role" };

        // Act
        var config = new MetadataConfiguration
        {
            SecurityRoles = securityRoles
        };

        // Assert
        Assert.Single(config.SecurityRoles);
        Assert.Equal("Custom Role", config.SecurityRoles[0]);
    }

    [Fact]
    public void AllPropertiesCanBeSetTogether()
    {
        // Arrange
        var solutions = new[] { "Solution1", "Solution2" };
        var entities = new[] { "account", "contact" };
        var securityRoles = new[] { "System Administrator" };

        // Act
        var config = new MetadataConfiguration
        {
            OutputDirectory = "/test/output",
            Solutions = solutions,
            Entities = entities,
            SecurityRoles = securityRoles,
            PrettyPrint = true
        };

        // Assert
        Assert.Equal("/test/output", config.OutputDirectory);
        Assert.Equal(solutions, config.Solutions);
        Assert.Equal(entities, config.Entities);
        Assert.Equal(securityRoles, config.SecurityRoles);
        Assert.True(config.PrettyPrint);
    }

    [Fact]
    public void OutputDirectory_DefaultsToMetadataFolder()
    {
        // Arrange & Act
        var config = new MetadataConfiguration();

        // Assert
        Assert.Equal("./Metadata", config.OutputDirectory);
    }

    [Fact]
    public void Solutions_DefaultsToEmptyArray()
    {
        // Arrange & Act
        var config = new MetadataConfiguration();

        // Assert
        Assert.NotNull(config.Solutions);
        Assert.Empty(config.Solutions);
    }

    [Fact]
    public void Entities_DefaultsToEmptyArray()
    {
        // Arrange & Act
        var config = new MetadataConfiguration();

        // Assert
        Assert.NotNull(config.Entities);
        Assert.Empty(config.Entities);
    }

    [Fact]
    public void PrettyPrint_DefaultsToFalse()
    {
        // Arrange & Act
        var config = new MetadataConfiguration();

        // Assert
        Assert.False(config.PrettyPrint);
    }

    [Fact]
    public void SecurityRoles_PropertyExistsAndIsArray()
    {
        // Arrange
        var config = new MetadataConfiguration();

        // Act
        var property = typeof(MetadataConfiguration).GetProperty(nameof(MetadataConfiguration.SecurityRoles));

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(string[]), property.PropertyType);
    }
}
