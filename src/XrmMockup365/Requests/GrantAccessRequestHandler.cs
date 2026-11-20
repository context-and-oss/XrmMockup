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

namespace DG.Tools.XrmMockup {
    internal class GrantAccessRequestHandler : RequestHandler {
        internal GrantAccessRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "GrantAccess") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<GrantAccessRequest>(orgRequest);
            security.GrantAccess(request.Target, request.PrincipalAccess, userRef);
            return new GrantAccessResponse();
        }
    }
}
