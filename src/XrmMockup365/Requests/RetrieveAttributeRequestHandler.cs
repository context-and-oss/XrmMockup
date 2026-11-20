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

namespace DG.Tools.XrmMockup
{
    internal class RetrieveAttributeRequestHandler : RequestHandler
    {
        internal RetrieveAttributeRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveAttribute") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveAttributeRequest>(orgRequest);

            if ((request.EntityLogicalName == null || request.LogicalName == null) && request.MetadataId == Guid.Empty)
            {
                throw new FaultException("Entity and attribute logical name is required when MetadataId is not specified");
            }

            AttributeMetadata attributeMetadata = null;

            if (request.LogicalName != null && metadata.EntityMetadata.ContainsKey(request.EntityLogicalName))
            {
                attributeMetadata = metadata.EntityMetadata[request.EntityLogicalName].Attributes.FirstOrDefault(a => a.LogicalName == request.LogicalName);
            }
            else if (request.MetadataId != Guid.Empty)
            {
                attributeMetadata = metadata.EntityMetadata.SelectMany(e => e.Value.Attributes, (e, a) => a).FirstOrDefault(a => a.MetadataId == request.MetadataId);
            }
            else
            {
                throw new FaultException($"Could not find entity with logicalname {request.LogicalName} or metadataid {request.MetadataId}");
            }

            var resp = new RetrieveAttributeResponse();
            resp.Results["AttributeMetadata"] = attributeMetadata;
            return resp;
        }
    }
}
