using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class WinQuoteRequestHandler : RequestHandler
    {
        internal WinQuoteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "WinQuote") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<WinQuoteRequest>(orgRequest);
            // default status to Won if set to -1 as standard by Dynamics
            var status = request.Status.Value == -1 ? new OptionSetValue((int)Quote_StatusCode.Won) : request.Status;
            Utility.CloseQuote(core, QuoteState.Won, status, request.QuoteClose, userRef);
            return new WinQuoteResponse();
        }
    }
}