using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockup.MetadataGenerator.Tool.Context;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads entity metadata from Dataverse.
/// </summary>
internal sealed class EntityMetadataReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<EntityMetadataReader> logger) : IEntityMetadataReader
{
    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<SortedDictionary<string, EntityMetadata>> GetEntityMetadataAsync(
        string[] solutions,
        string[] entities,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting entity metadata");

        // Get entity IDs from solutions
        var entityComponentIds = solutions
            .SelectMany(GetEntityComponentIdsFromSolution)
            .Distinct()
            .Where(id => id != Guid.Empty)
            .ToArray();

        // Fetch entity metadata using RetrieveMetadataChangesRequest
        var solutionEntities = await Task.Run(
            () => GetEntityMetadataByIds(entityComponentIds),
            ct);

        var specificEntities = await Task.Run(
            () => GetEntityMetadataByLogicalNames(entities),
            ct);

        var entityMetadata = solutionEntities.Concat(specificEntities).ToArray();

        var logicalNamesSet = new HashSet<string>(entityMetadata.Select(x => x.LogicalName));
        var relationMetadata = GetRelationMetadata(entityMetadata, logicalNamesSet);
        var allMetadata = entityMetadata.Concat(relationMetadata);

        // Add activityparty if needed
        if (!logicalNamesSet.Contains("activityparty") && NeedActivityParty(allMetadata))
        {
            var activityPartyMetadata = GetEntityMetadataByLogicalNames(["activityparty"]);
            allMetadata = allMetadata.Concat(activityPartyMetadata);
        }

        // Deduplicate by logical name
        var result = new SortedDictionary<string, EntityMetadata>(
            allMetadata
                .GroupBy(m => m.LogicalName)
                .ToDictionary(g => g.Key, g => g.First()));
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Retrieved entity metadata for: {Entities}", string.Join(", ", result.Keys));
        }

        return result;
    }

    private IEnumerable<Guid> GetEntityComponentIdsFromSolution(string solutionName)
    {
        using var xrm = new Xrm(_service);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var components = (
            from c in xrm.SolutionComponentSet
            join s in xrm.SolutionSet on c.SolutionId.Id equals s.SolutionId
            where s.UniqueName == solutionName && c.ComponentType == componenttype.Entity
            select c.ObjectId
        ).ToList();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return components.Select(c => c ?? Guid.Empty);
    }

    private EntityMetadata[] GetRelationMetadata(
        IEnumerable<EntityMetadata> entityMetadata,
        HashSet<string> logicalNamesSet)
    {
        var relationalEntityLogicalNames = FindAllRelationsEntities(logicalNamesSet, entityMetadata);
        var missingLogicalNames = relationalEntityLogicalNames
            .Where(relation => !logicalNamesSet.Contains(relation))
            .ToArray();
        return GetEntityMetadataByLogicalNames(missingLogicalNames);
    }

    private static IEnumerable<string> FindAllRelationsEntities(
        HashSet<string> allLogicalNames,
        IEnumerable<EntityMetadata> metadata)
    {
        return metadata
            .SelectMany(md =>
                md.ManyToManyRelationships
                    .Where(m2m => m2m.Entity1LogicalName == md.LogicalName
                        && allLogicalNames.Contains(m2m.Entity2LogicalName)
                        && !allLogicalNames.Contains(m2m.IntersectEntityName))
                    .Select(m2m => m2m.IntersectEntityName));
    }

    private static bool NeedActivityParty(IEnumerable<EntityMetadata> metadata)
    {
        return metadata.Any(entity =>
            entity.Attributes.Any(attribute => attribute.AttributeType == AttributeTypeCode.PartyList));
    }

    /// <summary>
    /// Retrieves entity metadata by logical names using a single RetrieveMetadataChangesRequest.
    /// </summary>
    private EntityMetadata[] GetEntityMetadataByLogicalNames(string[] logicalNames)
    {
        if (logicalNames.Length == 0)
        {
            return [];
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Fetching metadata for {Count} entities by logical name", logicalNames.Length);
        }

        var query = new EntityQueryExpression
        {
            Properties = new MetadataPropertiesExpression { AllProperties = true },
            Criteria = new MetadataFilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new MetadataConditionExpression(
                        "LogicalName",
                        MetadataConditionOperator.In,
                        logicalNames)
                }
            },
            AttributeQuery = new AttributeQueryExpression
            {
                Properties = new MetadataPropertiesExpression { AllProperties = true }
            },
            RelationshipQuery = new RelationshipQueryExpression
            {
                Properties = new MetadataPropertiesExpression { AllProperties = true }
            }
        };

        var request = new RetrieveMetadataChangesRequest { Query = query };
        var response = (RetrieveMetadataChangesResponse)_service.Execute(request);
        return [.. response.EntityMetadata];
    }

    /// <summary>
    /// Retrieves entity metadata by MetadataIds using a single RetrieveMetadataChangesRequest.
    /// </summary>
    private EntityMetadata[] GetEntityMetadataByIds(Guid[] metadataIds)
    {
        if (metadataIds.Length == 0)
        {
            return [];
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Fetching metadata for {Count} entities by MetadataId", metadataIds.Length);
        }

        var query = new EntityQueryExpression
        {
            Properties = new MetadataPropertiesExpression { AllProperties = true },
            Criteria = new MetadataFilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new MetadataConditionExpression(
                        "MetadataId",
                        MetadataConditionOperator.In,
                        metadataIds)
                }
            },
            AttributeQuery = new AttributeQueryExpression
            {
                Properties = new MetadataPropertiesExpression { AllProperties = true }
            },
            RelationshipQuery = new RelationshipQueryExpression
            {
                Properties = new MetadataPropertiesExpression { AllProperties = true }
            }
        };

        var request = new RetrieveMetadataChangesRequest { Query = query };
        var response = (RetrieveMetadataChangesResponse)_service.Execute(request);
        return [.. response.EntityMetadata];
    }
}
