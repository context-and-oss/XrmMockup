using Microsoft.Xrm.Sdk;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using DG.Tools.XrmMockup.Database;
using WorkflowExecuter;
using DG.Tools.XrmMockup.Internal;
using Utility = DG.Tools.XrmMockup.Internal.Utility;

namespace DG.Tools.XrmMockup {
    internal class CalculateRollupFieldRequestHandler : RequestHandler {
        internal CalculateRollupFieldRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CalculateRollupField") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<CalculateRollupFieldRequest>(orgRequest);

            var dbEntity = db.GetEntity(request.Target);
            var metadata = this.metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName);
            var field = metadata.Attributes.FirstOrDefault(a => a.LogicalName == request.FieldName);
            if (field == null) {
                throw new FaultException($"Couldn't find the field '{request.FieldName}' on the entity '{dbEntity.LogicalName}'");
            } else
            {
                var definition = Utility.GetFormulaDefinition(field, SourceType.RollupAttribute);
                if (definition == null)
                {
                    throw new FaultException($"Field '{request.FieldName}' on entity '{dbEntity.LogicalName}' is not a rollup field");
                }
                else
                {
                    var tree = WorkflowConstructor.ParseRollUp(definition, $"{dbEntity.LogicalName}.{field.LogicalName}");
                    var factory = core.ServiceFactory;
                    var resultTree = tree.Execute(dbEntity, core.TimeOffset, core.GetWorkflowService(), factory, factory.GetService<ITracingService>());
                    var resultLocaltion = ((resultTree.StartActivity as RollUp).Aggregation[1] as Aggregate).VariableName;
                    var result = resultTree.Variables[resultLocaltion];
                    if (result != null)
                    {
                        dbEntity[request.FieldName] = result;
                    }
                }
            }
            Utility.HandleCurrencies(this.metadata, db, dbEntity);
            db.Update(dbEntity);

            var resp = new CalculateRollupFieldResponse();
            resp.Results["Entity"] = db.GetEntity(request.Target);
            return resp;
        }
    }
}
