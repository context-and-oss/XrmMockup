using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Linq;

namespace DG.Tools.XrmMockup
{
    internal class ReviseQuoteRequestHandler : RequestHandler
    {
        internal ReviseQuoteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "ReviseQuote") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<ReviseQuoteRequest>(orgRequest);
            var quote = db.GetEntity(LogicalNames.Quote, request.QuoteId);
            // if you are wondering why its allowed to revise a quote in Active standard Sales: - Client Side JS closes it before calling this request.
            if (quote.GetAttributeValue<OptionSetValue>("statecode").Value != (int)QuoteState.Closed) throw new MockupException("Only quotes in the closed state can be revised.");

            // create quote revision
            var revisedQuote = Utility.CloneEntity(quote);
            revisedQuote.Id = Guid.Empty;
            var req = new CreateRequest() { Target = revisedQuote };
            revisedQuote.Id = (core.Execute(req, userRef) as CreateResponse).id;

            // override readonly revisionnumber field
            var revisionNumber = (quote.GetAttributeValue<int?>("revisionnumber") ?? 0) + 1;
            var dbRow = db.GetDbRow(revisedQuote);
            var record = dbRow.ToEntity();
            record["revisionnumber"] = revisionNumber;
            record["statecode"] = new OptionSetValue((int)QuoteState.Draft);
            record["statuscode"] = new OptionSetValue((int)Quote_StatusCode.InProgress_2);
            db.Update(record);

            // clone alle quotedetails to new quote revision
            var relatedQuoteLines = db.GetDBEntityRows(LogicalNames.QuoteDetail)
                .Select(e => e.ToEntity())
                .Where(e => e.GetAttributeValue<EntityReference>("quoteid").Id == quote.Id);
            foreach (var relatedQuoteLine in relatedQuoteLines)
            {
                var relatedQuoteLineClone = Utility.CloneEntity(relatedQuoteLine);
                relatedQuoteLineClone.Id = Guid.Empty;
                relatedQuoteLineClone.Attributes["quoteid"] = revisedQuote.ToEntityReference();
                req = new CreateRequest() { Target = relatedQuoteLineClone };
                core.Execute(req, userRef);
            }

            var resp = new ReviseQuoteResponse();
            resp.Results["Entity"] = revisedQuote;
            return resp;
        }
    }
}