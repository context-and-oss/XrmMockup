using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;
using WorkflowExecuter;
using DG.Tools.XrmMockup.Internal;
using Utility = DG.Tools.XrmMockup.Internal.Utility;

namespace DG.Tools.XrmMockup {
    internal class RetrieveRequestHandler : RequestHandler {
        internal RetrieveRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Retrieve") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveRequest>(orgRequest);
            var settings = MockupExecutionContext.GetSettings(request);
            

            if (request.Target.LogicalName == null) {
                throw new FaultException("You must provide a LogicalName");
            }


            if (request.ColumnSet == null && request.Target.KeyAttributes.Count == 0) {
                throw new FaultException("The columnset parameter must not be null when no KeyAttributes are provided");
            }

            var row = db.GetDbRow(request.Target);

            if (!security.HasPermission(row.ToEntity(), AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Calling user with id '{userRef.Id}' does not have permission to read entity '{row.Table.TableName}'");
            }

            // Calculate the (on-demand, never-persisted) calculated and formula fields onto the
            // entity we are about to return, before we filter the fetched columns. Only compute the
            // columns the caller actually asked for: an unselected calculated/formula column has no
            // observable effect on the response, and skipping it avoids paying the per-row workflow
            // cost (and prevents a broken calc field on an unrelated column from breaking this read).
            var looseEntity = row.ToEntity();
            var requested = GetRequestedComputedAttributes(request.ColumnSet);
            core.ExecuteCalculatedFields(row.Metadata, looseEntity, requested);
            core.ExecuteFormulaFields(row.Metadata, looseEntity, requested).GetAwaiter().GetResult();

            var entity = core.GetStronglyTypedEntity(looseEntity, row.Metadata, request.ColumnSet);

            Utility.SetFormattedValues(db, entity, row.Metadata);

            if (!settings.SetUnsettableFields) {
                Utility.RemoveUnsettableAttributes("Retrieve", row.Metadata, entity);
            }

            Utility.HandlePrecision(metadata, db, entity);
            if (request.RelatedEntitiesQuery != null) {
                core.AddRelatedEntities(entity, request.RelatedEntitiesQuery, userRef);
            }

            var resp = new RetrieveResponse();
            resp.Results["Entity"] = entity;
            return resp;
        }

        // Returns null when the caller asked for AllColumns (treat as "everything"); otherwise the
        // explicit column names. Calculated/formula attributes whose LogicalName is not in this set
        // can be skipped entirely.
        private static ISet<string> GetRequestedComputedAttributes(ColumnSet columnSet)
        {
            if (columnSet == null || columnSet.AllColumns) return null;
            return new HashSet<string>(columnSet.Columns, StringComparer.OrdinalIgnoreCase);
        }
    }
}
