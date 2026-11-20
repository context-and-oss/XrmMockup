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
using System.Reflection;
using System.Diagnostics;

namespace DG.Tools.XrmMockup {
    internal class RetrieveVersionRequestHandler : RequestHandler {
        internal RetrieveVersionRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveVersion") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveVersionRequest>(orgRequest);

            Assembly sdk = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.ManifestModule.Name == "Microsoft.Xrm.Sdk.dll").First();

            var resp = new RetrieveVersionResponse();
            resp.Results["Version"] = FileVersionInfo.GetVersionInfo(sdk.Location).FileVersion;
            return resp;
        }
    }
}
