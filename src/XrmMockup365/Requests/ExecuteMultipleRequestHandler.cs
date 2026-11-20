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
    internal class ExecuteMultipleRequestHandler : RequestHandler {
        internal ExecuteMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "ExecuteMultiple") {}

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<ExecuteMultipleRequest>(orgRequest);
            var toReturn = new ExecuteMultipleResponse();
            var responses = new ExecuteMultipleResponseItemCollection();
            for (var i = 0; i < request.Requests.Count; i++) {
                var resp = new ExecuteMultipleResponseItem {
                    RequestIndex = i
                };
                var r = request.Requests[i];
                try {
                    var orgResp = core.Execute(r, userRef);
                    if (request.Settings.ReturnResponses) {
                        resp.Response = orgResp;
                        responses.Add(resp);
                    }

                } catch (Exception e) {
                    resp.Fault = new OrganizationServiceFault {
                        Message = e.Message,
                        Timestamp = DateTime.Now
                    };
                    responses.Add(resp);
                    if (!request.Settings.ContinueOnError) {
                        toReturn.Results["Responses"] = responses;
                        toReturn.Results["IsFaulted"] = true;
                        return toReturn;
                    }
                }
            }
            toReturn.Results["Responses"] = responses;
            toReturn.Results["IsFaulted"] = responses.Any(x => x.Fault != null);
            return toReturn;
        }
    }
}
