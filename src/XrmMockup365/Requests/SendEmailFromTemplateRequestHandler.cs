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

        // Dataverse throws when a referenced record does not exist; mirror that rather than
        // silently sending an empty/unmerged e-mail.
        private Entity RetrieveOrThrow(EntityReference reference)
        {
            return db.GetEntityOrNull(reference)
                ?? throw new FaultException($"{reference.LogicalName} With Id = {reference.Id} Does Not Exist");
        }

        // A template is bound to an entity type via templatetypecode (an object type code).
        // Dataverse rejects a regarding record of a different type.
        private void ValidateTemplateType(Entity template, string regardingType)
        {
            var templateTypeCode = (template.GetAttributeValue<OptionSetValue>("templatetypecode"))?.Value
                ?? template.GetAttributeValue<int?>("templatetypecode");
            metadata.EntityMetadata.TryGetValue(regardingType, out var regardingMetadata);
            var regardingTypeCode = regardingMetadata?.ObjectTypeCode;

            if (templateTypeCode.HasValue && regardingTypeCode.HasValue &&
                templateTypeCode.Value != regardingTypeCode.Value)
            {
                throw new FaultException(
                    $"The template type does not match the regarding object type '{regardingType}'.");
            }
        }

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

            var template = RetrieveOrThrow(new EntityReference("template", request.TemplateId));
            var regardingRef = new EntityReference(request.RegardingType, request.RegardingId);
            var regarding = RetrieveOrThrow(regardingRef);

            ValidateTemplateType(template, request.RegardingType);

            // The template's subject/body are XSLT stylesheets rendered against the regarding
            // record and the sending user. Verified against a live org: the merged values (and
            // XSLT whitespace handling) match the platform.
            var entities = new Dictionary<string, Entity> { [request.RegardingType] = regarding };
            var sender = db.GetEntityOrNull(userRef);
            if (sender != null)
                entities[sender.LogicalName] = sender;

            var email = request.Target;
            email["regardingobjectid"] = regardingRef;
            email["subject"] = EmailTemplateRenderer.Render(template.GetAttributeValue<string>("subject"), entities);
            email["description"] = EmailTemplateRenderer.Render(template.GetAttributeValue<string>("body"), entities);

            // Delegate to the existing Create and SendEmail handlers so plugins, security
            // and status transitions are applied consistently.
            var emailId = ((CreateResponse)core.Execute(new CreateRequest { Target = email }, userRef)).id;
            core.Execute(new SendEmailRequest { EmailId = emailId, IssueSend = true }, userRef);

            return new SendEmailFromTemplateResponse
            {
                Results = new ParameterCollection { { "Id", emailId } }
            };
        }
    }
}
