using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class ExecuteTransactionRequestHandler : RequestHandler
    {
        internal ExecuteTransactionRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "ExecuteTransaction") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<ExecuteTransactionRequest>(orgRequest);
            var toReturn = new ExecuteTransactionResponse();
            toReturn.Results["Responses"] = new OrganizationResponseCollection();

            var snapshot = core.TakeJsonSnapshot();
            // Customization handlers replace metadata objects instead of mutating them. A shallow
            // snapshot therefore also makes choice/status edits transactional without copying the cache.
            var originalEntities = new Dictionary<string, Microsoft.Xrm.Sdk.Metadata.EntityMetadata>(metadata.EntityMetadata);
            var originalOptionSets = metadata.OptionSets;

            try
            {
                foreach (var req in request.Requests)
                {
                    toReturn.Responses.Add(core.Execute(req, userRef));
                }

                return toReturn;
            }
            catch (Exception)
            {
                foreach (var entity in originalEntities.Values)
                {
                    metadata.EntityMetadata[entity.LogicalName] = entity;
                    if (db.HasTable(entity.LogicalName)) db[entity.LogicalName].ReplaceMetadata(entity);
                }
                metadata.OptionSets = originalOptionSets;
                core.RestoreJsonSnapshot(snapshot);
                throw;
            }
        }
    }
}
