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
using XrmPluginCore.Enums;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup {
    internal class UpsertRequestHandler : RequestHandler {
        internal UpsertRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, nameof(EventOperation.Upsert)) {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<UpsertRequest>(orgRequest);
            var resp = new UpsertResponse();
            var target = request.Target;
            var entityId = db.GetEntityOrNull(target.ToEntityReferenceWithKeyAttributes())?.Id;
            if (entityId.HasValue) {
                var req = new UpdateRequest();
                target.Id = entityId.Value;
                req.Target = target;
                core.Execute(req, userRef);
                resp.Results["RecordCreated"] = false;
                resp.Results["Target"] = target.ToEntityReferenceWithKeyAttributes();
            } else {
                var req = new CreateRequest {
                    Target = target
                };
                target.Id = (core.Execute(req, userRef) as CreateResponse).id;
                resp.Results["RecordCreated"] = true;
                resp.Results["Target"] = target.ToEntityReferenceWithKeyAttributes();
            }
            return resp;
        }
    }
}
