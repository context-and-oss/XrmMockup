using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class CloseQuoteRequestHandler : RequestHandler
    {
        internal CloseQuoteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CloseQuote") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CloseQuoteRequest>(orgRequest);
            // default status to Lost if set to -1 as standard by Dynamics
            var status = request.Status.Value == -1 ? new OptionSetValue((int)Quote_StatusCode.Lost) : request.Status;
            Utility.CloseQuote(core, QuoteState.Closed, status, request.QuoteClose, userRef);
            return new CloseQuoteResponse();
        }
    }
}