using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmMockup.MetadataGenerator.Core.Readers;

internal static class QueryExtensions
{
    /// <summary>
    /// Every record the query matches, page by page.
    /// </summary>
    public static List<Entity> RetrieveAll(this IOrganizationService service, QueryExpression query)
    {
        query.PageInfo.PageNumber = 1;
        var entities = new List<Entity>();
        var resp = service.RetrieveMultiple(query);
        entities.AddRange(resp.Entities);

        while (resp.MoreRecords)
        {
            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = resp.PagingCookie;
            resp = service.RetrieveMultiple(query);
            entities.AddRange(resp.Entities);
        }

        return entities;
    }
}
