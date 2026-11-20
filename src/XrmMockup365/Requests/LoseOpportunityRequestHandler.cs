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
    internal class LoseOpportunityRequestHandler : RequestHandler {
        internal LoseOpportunityRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "LoseOpportunity") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<LoseOpportunityRequest>(orgRequest);
            Utility.CloseOpportunity(core, OpportunityState.Lost, request.Status, request.OpportunityClose, userRef);
            return new LoseOpportunityResponse();
        }
    }
}
