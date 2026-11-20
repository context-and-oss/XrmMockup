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
    internal class DisassociateRequestHandler : RequestHandler {
        internal DisassociateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "Disassociate") {}

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<DisassociateRequest>(orgRequest);
            var relatedLogicalName = request.RelatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as ManyToManyRelationshipMetadata;

            var targetEntity = db.GetEntity(request.Target);
            if (!security.HasPermission(targetEntity, AccessRights.ReadAccess, userRef))
            {
                throw new FaultException($"Trying to unappend to entity '{request.Target.LogicalName}'" +
                    ", but the calling user does not have read access for that entity");
            }

            if (!security.HasPermission(targetEntity, AccessRights.AppendToAccess, userRef))
            {
                throw new FaultException($"Trying to unappend to entity '{request.Target.LogicalName}'" +
                    ", but the calling user does not have append to access for that entity");
            }

            if (request.RelatedEntities.Any(r => !security.HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef)))
            {
                var firstError = request.RelatedEntities.First(r => !security.HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef));
                throw new FaultException($"Trying to unappend entity '{firstError.LogicalName}'" +
                    $" to '{request.Target.LogicalName}', but the calling user does not have read access for that entity");
            }

            if (request.RelatedEntities.Any(r => !security.HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef)))
            {
                var firstError = request.RelatedEntities.First(r => !security.HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef));
                throw new FaultException($"Trying to unappend entity '{firstError.LogicalName}'" +
                    $" to '{request.Target.LogicalName}', but the calling user does not have append access for that entity");
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<DisassociateRequest>(orgRequest);
            var relatedLogicalName = request.RelatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata, relatedLogicalName, request.Relationship.SchemaName) as ManyToManyRelationshipMetadata;

            if (manyToMany != null) {
                foreach (var relatedEntity in request.RelatedEntities) {
                    if (request.Target.LogicalName == manyToMany.Entity1LogicalName) {
                        var link = db[manyToMany.IntersectEntityName]
                            .First(row =>
                                row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == request.Target.Id &&
                                row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == relatedEntity.Id
                            );

                        db[manyToMany.IntersectEntityName].Remove(link.Id);
                    } else {
                        var link = db[manyToMany.IntersectEntityName]
                            .First(row =>
                                row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == relatedEntity.Id &&
                                row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == request.Target.Id
                            );
                        db[manyToMany.IntersectEntityName].Remove(link.Id);
                    }
                }
            } else {
                if (oneToMany.ReferencedEntity == request.Target.LogicalName) {
                    foreach (var relatedEntity in request.RelatedEntities) {
                        var dbEntity = db.GetEntity(relatedEntity);
                        Utility.RemoveAttribute(dbEntity, oneToMany.ReferencingAttribute);
                        Utility.Touch(dbEntity, metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName), core.TimeOffset, userRef);
                        db.Update(dbEntity);
                    }
                } else {
                    if (request.RelatedEntities.Count > 1) {
                        throw new FaultException("Only one related entity is supported for Many to One relationship association.");
                    }
                    if (request.RelatedEntities.Count == 1) {
                        var related = request.RelatedEntities.First();
                        var dbEntity = db.GetEntity(request.Target);
                        Utility.RemoveAttribute(dbEntity, oneToMany.ReferencingAttribute);
                        Utility.Touch(dbEntity, metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName), core.TimeOffset, userRef);
                        db.Update(dbEntity);
                    }
                }
            }


            return new DisassociateResponse();
        }
    }
}
