using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;
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

            // Merge the template content into the e-mail, mirroring Dataverse: the template's
            // subject/body are XSLT stylesheets rendered against the regarding record and the
            // sending user. This is guarded on metadata because looking up an entity that was
            // not generated throws - in that case the caller-supplied subject/body are used.
            if (metadata.EntityMetadata.ContainsKey("template"))
            {
                var template = db.GetEntityOrNull(new EntityReference("template", request.TemplateId));
                if (template != null)
                {
                    var entities = new Dictionary<string, Entity>();

                    var regarding = db.GetEntityOrNull(new EntityReference(request.RegardingType, request.RegardingId));
                    if (regarding != null)
                        entities[request.RegardingType] = regarding;

                    var sender = db.GetEntityOrNull(userRef);
                    if (sender != null)
                        entities[sender.LogicalName] = sender;

                    var subject = EmailTemplateRenderer.Render(template.GetAttributeValue<string>("subject"), entities);
                    if (!string.IsNullOrEmpty(subject))
                        email["subject"] = subject;

                    var body = EmailTemplateRenderer.Render(template.GetAttributeValue<string>("body"), entities);
                    if (!string.IsNullOrEmpty(body))
                        email["description"] = body;
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
