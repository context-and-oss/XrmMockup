using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class DeleteMultipleRequestHandler : RequestHandler
    {
        internal DeleteMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "DeleteMultiple")
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<DeleteMultipleRequest>(orgRequest);

            var invalidRef = request.Targets.FirstOrDefault(e => string.IsNullOrEmpty(e.LogicalName));
            if (invalidRef != null)
            {
                throw new FaultException("The required field 'EntityName' is missing.");
            }

            var distinctLogicalNames = request.Targets.Select(e => e.LogicalName).Distinct().ToList();
            if (distinctLogicalNames.Count > 1)
            {
                throw new FaultException("All entity references in a DeleteMultipleRequest must have the same entity logical name.");
            }

            foreach (var entityRef in request.Targets)
            {
                var deleteRequest = new DeleteRequest
                {
                    Target = entityRef
                };
                core.Execute(deleteRequest, userRef);
            }

            return new OrganizationResponse { ResponseName = "DeleteMultiple" };
        }
    }
}
