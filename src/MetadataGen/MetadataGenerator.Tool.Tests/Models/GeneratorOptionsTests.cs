using Xunit;
using XrmMockup.MetadataGenerator.Core.Models;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Models;

/// <summary>
/// Tests for GeneratorOptions configuration model.
/// Verifies that the SecurityRoles property is correctly defined and accessible.
/// </summary>
public class GeneratorOptionsTests
{
    [Fact]
    public void SecurityRoles_DefaultsToNull()
    {
        // Arrange & Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test"
        };

        // Assert
        Assert.Null(options.SecurityRoles);
    }

    [Fact]
    public void SecurityRoles_CanBeSetToValues()
    {
        // Arrange
        var securityRoles = new[] { "System Administrator", "Basic User" };

        // Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            SecurityRoles = securityRoles
        };

        // Assert
        Assert.Equal(securityRoles, options.SecurityRoles);
        Assert.Equal(2, options.SecurityRoles.Length);
        Assert.Contains("System Administrator", options.SecurityRoles);
        Assert.Contains("Basic User", options.SecurityRoles);
    }

    [Fact]
    public void SecurityRoles_CanBeSetToSingleValue()
    {
        // Arrange
        var securityRoles = new[] { "Custom Role" };

        // Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test",
            SecurityRoles = securityRoles
        };

        // Assert
        Assert.Single(options.SecurityRoles);
        Assert.Equal("Custom Role", options.SecurityRoles[0]);
    }

    [Fact]
    public void AllPropertiesCanBeSetTogether()
    {
        // Arrange
        var solutions = new[] { "Solution1", "Solution2" };
        var entities = new[] { "account", "contact" };
        var securityRoles = new[] { "System Administrator" };

        // Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test/output",
            Solutions = solutions,
            Entities = entities,
            SecurityRoles = securityRoles,
            PrettyPrint = true
        };

        // Assert
        Assert.Equal("/test/output", options.OutputDirectory);
        Assert.Equal(solutions, options.Solutions);
        Assert.Equal(entities, options.Entities);
        Assert.Equal(securityRoles, options.SecurityRoles);
        Assert.True(options.PrettyPrint);
    }

    [Fact]
    public void DefaultEntities_ContainsExpectedEntities()
    {
        // Assert - verify default entities are defined
        Assert.NotEmpty(GeneratorOptions.DefaultEntities);
        Assert.Contains("businessunit", GeneratorOptions.DefaultEntities);
        Assert.Contains("systemuser", GeneratorOptions.DefaultEntities);
        Assert.Contains("role", GeneratorOptions.DefaultEntities);
    }

    [Fact]
    public void Solutions_DefaultsToEmptyArray()
    {
        // Arrange & Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test"
        };

        // Assert
        Assert.NotNull(options.Solutions);
        Assert.Empty(options.Solutions);
    }

    [Fact]
    public void Entities_DefaultsToEmptyArray()
    {
        // Arrange & Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test"
        };

        // Assert
        Assert.NotNull(options.Entities);
        Assert.Empty(options.Entities);
    }

    [Fact]
    public void PrettyPrint_DefaultsToFalse()
    {
        // Arrange & Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/test"
        };

        // Assert
        Assert.False(options.PrettyPrint);
    }
}
