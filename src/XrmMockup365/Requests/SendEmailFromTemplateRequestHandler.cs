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

        // Dataverse throws when a referenced record does not exist or the caller cannot read it;
        // mirror that rather than silently sending an empty or unmerged e-mail.
        private Entity RetrieveOrThrow(EntityReference reference, EntityReference userRef)
        {
            var entity = db.GetEntityOrNull(reference)
                ?? throw new FaultException($"{reference.LogicalName} With Id = {reference.Id} Does Not Exist");

            if (!security.HasPermission(entity, AccessRights.ReadAccess, userRef))
                throw new FaultException($"Calling user with id '{userRef.Id}' does not have permission to read entity '{reference.LogicalName}'");

            return entity;
        }

        // A template is bound to an entity type via templatetypecode; Dataverse rejects a regarding
        // record of a different type. templatetypecode is an EntityName attribute, stored as the
        // integer object type code when its metadata carries an option set and as the logical name
        // otherwise (see DbAttributeTypeMap), so both shapes are accepted.
        private void ValidateTemplateType(Entity template, string regardingType)
        {
            if (!template.Attributes.TryGetValue("templatetypecode", out var rawTypeCode) || rawTypeCode == null)
                return;

            metadata.EntityMetadata.TryGetValue(regardingType, out var regardingMetadata);
            var regardingTypeCode = regardingMetadata?.ObjectTypeCode;
            if (regardingTypeCode == null)
                return;

            bool matches;
            if (rawTypeCode is OptionSetValue optionSet)
                matches = optionSet.Value == regardingTypeCode.Value;
            else if (rawTypeCode is int typeCode)
                matches = typeCode == regardingTypeCode.Value;
            else if (rawTypeCode is string logicalName)
                matches = logicalName == regardingType;
            else
                return;

            if (!matches)
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

            var template = RetrieveOrThrow(new EntityReference("template", request.TemplateId), userRef);
            var regardingRef = new EntityReference(request.RegardingType, request.RegardingId);
            var regarding = RetrieveOrThrow(regardingRef, userRef);

            ValidateTemplateType(template, request.RegardingType);

            var entities = new Dictionary<string, Entity> { [request.RegardingType] = regarding };

            // The sending user is a second merge source, but never at the regarding record's
            // expense: a template regarding a systemuser must merge that user, not the caller.
            var sender = db.GetEntityOrNull(userRef);
            if (sender != null && !entities.ContainsKey(sender.LogicalName))
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
