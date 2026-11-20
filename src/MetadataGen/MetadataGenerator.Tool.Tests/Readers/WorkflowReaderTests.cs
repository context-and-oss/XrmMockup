using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Context;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for WorkflowReader.
/// Tests verify the reader correctly queries workflows from the active solution.
/// </summary>
public class WorkflowReaderTests : ReaderTestBase
{
    private readonly WorkflowReader _reader;

    public WorkflowReaderTests()
    {
        var logger = CreateLogger<WorkflowReader>();
        _reader = new WorkflowReader(ServiceProvider, logger);
    }

    [Fact]
    public async Task GetWorkflowsAsync_ReturnsEnumerableOfEntities()
    {
        var result = await _reader.GetWorkflowsAsync();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<Entity>>(result);
    }

    [Fact]
    public async Task GetWorkflowsAsync_WithNoActiveSolution_ReturnsEmptyCollection()
    {
        // Arrange - Delete any existing "active" solution
        using (var xrm = new Xrm(Service))
        {
            var solutions = xrm.SolutionSet.Where(s => s.UniqueName == "active").ToList();
            foreach (var solution in solutions)
            {

            }
        }
            var existingSolutions = Service.RetrieveMultiple(new QueryExpression(Solution.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(Solution.GetColumnName(s => s.UniqueName)),
                Criteria = new FilterExpression
                {
                    Conditions =
                {
                    new ConditionExpression(
                        Solution.GetColumnName(s => s.UniqueName),
                        ConditionOperator.Equal,
                        "active")
                }
                }
            }).Entities;

        foreach (var solution in existingSolutions)
        {
            Service.Delete(Solution.EntityLogicalName, solution.Id);
        }

        // Act
        var result = await _reader.GetWorkflowsAsync();

        // Assert - Should return empty, not throw
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
