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
    internal class RetrieveRelationshipRequestHandler : RequestHandler {
        internal RetrieveRelationshipRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveRelationship") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveRelationshipRequest>(orgRequest);

            if (request.Name == null && request.MetadataId == Guid.Empty) {
                throw new FaultException("Relationship name is required when MetadataId is not specified");
            }
            var metadata = Utility.GetRelationshipMetadataDefaultNull(this.metadata.EntityMetadata, request.Name, request.MetadataId, userRef);
            if (metadata == null) {
                throw new FaultException($"Could not find relationship with name {request.Name} or metadataid {request.MetadataId}");
            }

            var resp = new RetrieveRelationshipResponse();
            resp.Results["RelationshipMetadata"] = metadata;
            return resp;
        }
    }
}
