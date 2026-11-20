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
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup {
    internal class MergeRequestHandler : RequestHandler {
        internal MergeRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Merge") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<MergeRequest>(orgRequest);
            var mainEntity = db.GetEntity(request.Target);
            var casSelection = new CascadeSelection() { merge = true };
            var subordinateReference = new EntityReference { LogicalName = mainEntity.LogicalName, Id = request.SubordinateId };
            var subordinateEntity = core.GetDbEntityWithRelatedEntities(subordinateReference, EntityRole.Referencing, userRef, casSelection);

            foreach (var attr in request.UpdateContent.Attributes) {
                if (attr.Value != null) {
                    mainEntity[attr.Key] = attr.Value;
                }
            }

            foreach (var relatedEntities in subordinateEntity.RelatedEntities) {
                var relationshipMeta = metadata.EntityMetadata.GetMetadata(subordinateEntity.LogicalName).ManyToOneRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                if (relationshipMeta.CascadeConfiguration.Merge == CascadeType.Cascade) {
                    var entitiesToSwap = relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList();

                    var disassocHandler = core.RequestHandlers.Find(x => x is DisassociateRequestHandler);
                    disassocHandler.Execute(new DisassociateRequest {
                        RelatedEntities = new EntityReferenceCollection(entitiesToSwap),
                        Relationship = relatedEntities.Key,
                        Target = subordinateEntity.ToEntityReference()
                    }, userRef);

                    var assocHandler = core.RequestHandlers.Find(x => x is AssociateRequestHandler);
                    assocHandler.Execute(new AssociateRequest {
                        RelatedEntities = new EntityReferenceCollection(entitiesToSwap),
                        Relationship = relatedEntities.Key,
                        Target = mainEntity.ToEntityReference()
                    }, userRef);
                }
            }

            subordinateEntity["merged"] = true;
            db.Update(subordinateEntity);
            db.Update(mainEntity);
            var setStateHandler = core.RequestHandlers.Find(x => x is SetStateRequestHandler);
            var req = new SetStateRequest() {
                EntityMoniker = subordinateReference,
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            };
            setStateHandler.Execute(req, userRef);

            return new MergeResponse();
        }
    }
}
