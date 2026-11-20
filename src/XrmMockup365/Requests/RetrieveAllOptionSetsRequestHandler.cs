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
    internal class RetrieveAllOptionSetsRequestHandler : RequestHandler {
        internal RetrieveAllOptionSetsRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveAllOptionSets") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveAllOptionSetsRequest>(orgRequest);
            var resp = new RetrieveAllOptionSetsResponse();
            resp.Results["OptionSetMetadata"] = metadata.OptionSets;
            return resp;
        }
    }
}
