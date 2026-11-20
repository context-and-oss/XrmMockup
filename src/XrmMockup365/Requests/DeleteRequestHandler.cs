using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup {
    internal class DeleteRequestHandler : RequestHandler {
        internal DeleteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Delete") {}

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<DeleteRequest>(orgRequest);
            var casSelection = new CascadeSelection() { delete = true };
            var entity = core.GetDbEntityWithRelatedEntities(request.Target, EntityRole.Referenced, userRef, casSelection);
            if (entity == null)
            {
                throw new FaultException($"{request.Target.LogicalName} with Id '{request.Target.Id}' does not exist");
            }
            if (!security.HasPermission(entity, AccessRights.DeleteAccess, userRef))
            {
                throw new FaultException($"You do not have permission to access entity '{request.Target.LogicalName}' for delete");
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<DeleteRequest>(orgRequest);
            var casSelection = new CascadeSelection() { delete = true };
            var entity = core.GetDbEntityWithRelatedEntities(request.Target, EntityRole.Referenced, userRef, casSelection);
            if (entity == null) {
                throw new FaultException($"{request.Target.LogicalName} with Id '{request.Target.Id}' does not exist");
            }

            if (entity != null) {
                foreach (var relatedEntities in entity.RelatedEntities) {
                    var relationshipMeta = metadata.EntityMetadata.GetMetadata(entity.LogicalName).OneToManyRelationships.FirstOrDefault(r => r.SchemaName == relatedEntities.Key.SchemaName);

                    if (relationshipMeta == null)
                    {
                        continue;
                    }
                    switch (relationshipMeta.CascadeConfiguration.Delete) {
                        case CascadeType.Cascade:
                            foreach (var relatedEntity in relatedEntities.Value.Entities) {
                                var req = new DeleteRequest {
                                    Target = new EntityReference(relatedEntity.LogicalName, relatedEntity.Id)
                                };
                                core.Execute(req, userRef, null);
                            }
                            break;
                        case CascadeType.RemoveLink:
                            var disassocHandler = core.RequestHandlers.Find(x => x is DisassociateRequestHandler);
                            disassocHandler.Execute(new DisassociateRequest {
                                RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList()),
                                Relationship = relatedEntities.Key,
                                Target = entity.ToEntityReference()
                            }, userRef);
                            break;
                        case CascadeType.Restrict:
                            var trace = core.ServiceFactory.GetService<ITracingService>();
                            trace.Trace($"Delete restricted for {relatedEntities.Key.SchemaName} when trying to delete {entity.LogicalName} with id {entity.Id}");
                            return new DeleteResponse();
                    }
                }

                db.Delete(entity);
            }

            return new DeleteResponse();
        }
    }
}
