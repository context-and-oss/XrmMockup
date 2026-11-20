using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.MetadataGenerator.Core.Connection;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads the privileges in Dataverse and the execute privileges of custom APIs.
/// </summary>
internal sealed class PrivilegeReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<PrivilegeReader> logger) : IPrivilegeReader
{
    /// <summary>
    /// The object type code of privileges that aren't tied to a table.
    /// </summary>
    internal const string NoTable = "none";

    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<PrivilegeCatalog> GetPrivilegeCatalogAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Getting privileges");

        return await Task.Run(() =>
        {
            var privileges = _service.RetrieveAll(new QueryExpression("privilege")
            {
                ColumnSet = new ColumnSet("privilegeid", "name", "accessright")
            });

            var tables = _service.RetrieveAll(new QueryExpression("privilegeobjecttypecodes")
                {
                    ColumnSet = new ColumnSet("privilegeid", "objecttypecode")
                })
                .Where(e => e.GetAttributeValue<EntityReference>("privilegeid") is not null && e.GetAttributeValue<string>("objecttypecode") is { } table && table != NoTable)
                .ToLookup(e => e.GetAttributeValue<EntityReference>("privilegeid").Id, e => e.GetAttributeValue<string>("objecttypecode"));

            // Every custom API, also those without an execute privilege, so a call to one is known to need none
            var customApis = _service.RetrieveAll(new QueryExpression("customapi")
            {
                ColumnSet = new ColumnSet("uniquename", "executeprivilegename")
            });

            var catalog = new PrivilegeCatalog
            {
                Privileges = privileges
                    .Where(p => !string.IsNullOrEmpty(p.GetAttributeValue<string>("name")))
                    .Select(p => new MetaPrivilege
                    {
                        Name = p.GetAttributeValue<string>("name"),
                        PrivilegeId = p.Id,
                        AccessRight = p.GetAttributeValue<AccessRights>("accessright"),
                        Tables = [.. tables[p.Id].Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]
                    })
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ThenBy(p => p.PrivilegeId)
                    .ToList(),
                CustomApis = customApis
                    .Where(a => !string.IsNullOrEmpty(a.GetAttributeValue<string>("uniquename")))
                    .Select(a => new MetaCustomApi
                    {
                        UniqueName = a.GetAttributeValue<string>("uniquename"),
                        ExecutePrivilegeName = a.GetAttributeValue<string>("executeprivilegename") is { Length: > 0 } privilege ? privilege : null
                    })
                    .OrderBy(a => a.UniqueName, StringComparer.Ordinal)
                    .ToList()
            };

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} privileges and {CustomApis} custom APIs", catalog.Privileges.Count, catalog.CustomApis.Count);
            }
            return catalog;
        }, ct);
    }
}
