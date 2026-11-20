using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockup.MetadataGenerator.Tool.Context;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads organization and business unit data from Dataverse.
/// </summary>
internal sealed class OrganizationReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<OrganizationReader> logger) : IOrganizationReader
{
    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<Entity> GetBaseOrganizationAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Getting base organization");

        return await Task.Run(() =>
        {
            var baseOrganizationId = serviceProvider.ConnectedOrgId;

            var query = new QueryExpression("organization")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("organizationid", ConditionOperator.Equal, baseOrganizationId)
                    }
                }
            };

            return _service.RetrieveMultiple(query).Entities.First();
        }, ct);
    }

    public async Task<Entity> GetRootBusinessUnitAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Getting root business unit");

        return await Task.Run(() =>
        {
            using var xrm = new Xrm(_service);
            return xrm.BusinessUnitSet.First(bu => bu.ParentBusinessUnitId == null).ToEntity<Entity>();
        }, ct);
    }

    public async Task<Dictionary<string, Dictionary<int, int>>> GetDefaultStateStatusAsync(
        IEnumerable<string> entityLogicalNames,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting default state and status mappings");

        return await Task.Run(() =>
        {
            var dict = new Dictionary<string, Dictionary<int, int>>();
            var entityNames = entityLogicalNames.ToList();

            if (entityNames.Count == 0)
            {
                return dict;
            }

            var query = new QueryExpression("statusmap")
            {
                ColumnSet = new ColumnSet("objecttypecode", "state", "status", "isdefault"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("isdefault", ConditionOperator.Equal, true),
                        new ConditionExpression("objecttypecode", ConditionOperator.In, [.. entityNames])
                    }
                }
            };
            query.PageInfo.PageNumber = 1;

            var statusmaps = new List<Entity>();
            var resp = _service.RetrieveMultiple(query);
            statusmaps.AddRange(resp.Entities);

            while (resp.MoreRecords)
            {
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = resp.PagingCookie;
                resp = _service.RetrieveMultiple(query);
                statusmaps.AddRange(resp.Entities);
            }

            foreach (var e in statusmaps)
            {
                var logicalName = e.GetAttributeValue<string>("objecttypecode");
                if (!dict.TryGetValue(logicalName, out Dictionary<int, int>? value))
                {
                    value = [];
                    dict.Add(logicalName, value);
                }

                var state = e.GetAttributeValue<int>("state");
                if (!value.ContainsKey(state))
                {
                    value.Add(state, e.GetAttributeValue<int>("status"));
                }
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved state/status mappings for {Count} entities", dict.Count);
            }

            return dict;
        }, ct);
    }
}
