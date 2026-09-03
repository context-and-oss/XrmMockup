using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockup.MetadataGenerator.Tool.Context;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads workflow definitions from Dataverse.
/// </summary>
internal sealed class WorkflowReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<WorkflowReader> logger) : IWorkflowReader
{
    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<IEnumerable<Entity>> GetWorkflowsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Getting workflows");

        return await Task.Run(() =>
        {
            var activeSolutionId = GetActiveSolution();

            if (!activeSolutionId.HasValue)
            {
                logger.LogWarning("No active solution found, skipping workflow extraction");
                return [];
            }

            var query = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
            };

            query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, activeSolutionId);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, (int)workflow_statecode.Activated);

            var category = new FilterExpression(LogicalOperator.Or);
            category.AddCondition("category", ConditionOperator.Equal, (int)workflow_category.Workflow);
            category.AddCondition("category", ConditionOperator.Equal, (int)workflow_category.Action);
            query.Criteria.AddFilter(category);

            var workflows = _service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>())
                .ToList();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} workflows", workflows.Count);
            }
            return workflows.AsEnumerable();
        }, ct);
    }

    private Guid? GetActiveSolution()
    {
        var query = new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet(),
            Criteria = new FilterExpression()
        };
        query.Criteria.AddCondition(new ConditionExpression("uniquename", ConditionOperator.Equal, "active"));

        return _service.RetrieveMultiple(query).Entities
            .Select(e => e.Id)
            .FirstOrDefault();
    }
}
