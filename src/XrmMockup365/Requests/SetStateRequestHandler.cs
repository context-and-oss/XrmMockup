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
    internal class SetStateRequestHandler : RequestHandler {
        internal SetStateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SetState") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<SetStateRequest>(orgRequest);

            var row = db.GetDbRow(request.EntityMoniker);
            var record = row.ToEntity();
            if (Utility.IsValidAttribute("statecode", row.Metadata) &&
                Utility.IsValidAttribute("statuscode", row.Metadata)) {
                var prevEntity = record.CloneEntity();
                record["statecode"] = request.State;
                record["statuscode"] = request.Status;
                Utility.CheckStatusTransitions(row.Metadata, record, prevEntity);
                Utility.HandleCurrencies(metadata, db, record);
                Utility.Touch(record, row.Metadata, core.TimeOffset, userRef);

                db.Update(record);
            }
            return new SetStateResponse();
        }
    }
}
