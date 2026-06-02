using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class SendEmailFromTemplateRequestHandler : RequestHandler
    {
        public SendEmailFromTemplateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SendEmailFromTemplate") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailFromTemplateRequest>(orgRequest);

            if (request.TemplateId == Guid.Empty)
                throw new FaultException("Template id should be set.");

            if (request.Target == null)
                throw new FaultException("Target email is missing.");

            if (request.Target.LogicalName != "email")
                throw new FaultException("Target must be an email entity.");

            if (request.RegardingId == Guid.Empty)
                throw new FaultException("Regarding id should be set.");

            if (string.IsNullOrEmpty(request.RegardingType))
                throw new FaultException("Regarding type should be set.");

            var email = request.Target;
            email["regardingobjectid"] = new EntityReference(request.RegardingType, request.RegardingId);

            // Best-effort template merge: when the 'template' entity is present in metadata
            // and the record exists, use its subject/body for fields the caller did not
            // supply on the Target email. Token substitution (e.g. {!contact.firstname;})
            // is not modelled - the template content is copied verbatim. Guarding on
            // metadata keeps this safe in environments where 'template' was not generated
            // (looking up an entity with no metadata throws).
            if (metadata.EntityMetadata.ContainsKey("template"))
            {
                var template = db.GetEntityOrNull(new EntityReference("template", request.TemplateId));
                if (template != null)
                {
                    if (!email.Contains("subject") && template.Contains("subject"))
                        email["subject"] = template.GetAttributeValue<string>("subject");

                    if (!email.Contains("description") && template.Contains("body"))
                        email["description"] = template.GetAttributeValue<string>("body");
                }
            }

            // Delegate to the existing Create and SendEmail handlers so plugins, security
            // and status transitions are applied consistently.
            var createResponse = (CreateResponse)core.Execute(new CreateRequest { Target = email }, userRef);
            var emailId = createResponse.id;

            core.Execute(new SendEmailRequest { EmailId = emailId, IssueSend = true }, userRef);

            return new SendEmailFromTemplateResponse
            {
                Results = new ParameterCollection
                {
                    { "Id", emailId }
                }
            };
        }
    }
}
