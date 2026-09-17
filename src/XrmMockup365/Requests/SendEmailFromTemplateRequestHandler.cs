using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class SendEmailFromTemplateRequestHandler : RequestHandler
    {
        public SendEmailFromTemplateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SendEmailFromTemplate") { }

        // Dataverse fails the send rather than merging an unreadable record as blanks.
        private Entity RetrieveOrThrow(EntityReference reference, EntityReference userRef)
        {
            var entity = db.GetEntityOrNull(reference)
                ?? throw new FaultException($"{reference.LogicalName} With Id = {reference.Id} Does Not Exist");

            if (!security.HasPermission(entity, AccessRights.ReadAccess, userRef))
                throw new FaultException($"Calling user with id '{userRef.Id}' does not have permission to read entity '{reference.LogicalName}'");

            return entity;
        }

        // Depending on the attribute's metadata (see DbAttributeTypeMap), templatetypecode is stored
        // as an OptionSetValue, an int or the logical name. Dataverse itself returns the logical name.
        private int? GetTemplateTypeCode(Entity template)
        {
            switch (template.GetAttributeValue<object>("templatetypecode"))
            {
                case OptionSetValue optionSet:
                    return optionSet.Value;
                case int typeCode:
                    return typeCode;
                case string logicalName:
                    metadata.EntityMetadata.TryGetValue(logicalName, out var typeMetadata);
                    return typeMetadata?.ObjectTypeCode;
                default:
                    return null;
            }
        }

        // Dataverse rejects a regarding record whose type differs from the template's, and does so
        // before it looks the regarding record up.
        private void ValidateTemplateType(Entity template, string regardingType)
        {
            var templateTypeCode = GetTemplateTypeCode(template);
            metadata.EntityMetadata.TryGetValue(regardingType, out var regardingMetadata);
            var regardingTypeCode = regardingMetadata?.ObjectTypeCode;

            if (templateTypeCode == null || regardingTypeCode == null)
                return;

            if (templateTypeCode != regardingTypeCode)
            {
                throw new FaultException(
                    $"Template type is incorrect for given objectType {regardingTypeCode} != {templateTypeCode} template.templatetypecode");
            }
        }

        // Dataverse leaves regardingobjectid empty when the lookup cannot target the regarding
        // type, as with a systemuser template.
        private bool CanBeRegarding(string logicalName)
        {
            if (!metadata.EntityMetadata.TryGetValue("email", out var emailMetadata))
                return true;

            var regarding = emailMetadata.Attributes?
                .OfType<LookupAttributeMetadata>()
                .FirstOrDefault(a => a.LogicalName == "regardingobjectid");

            return regarding?.Targets == null || regarding.Targets.Contains(logicalName);
        }

        private static bool HasSender(Entity email)
        {
            return email.GetAttributeValue<EntityCollection>("from")?.Entities.Count > 0;
        }

        // Dataverse returns the merged body wrapped in a minimal HTML document, with LF line
        // breaks and a trailing newline, rather than as bare text. It also re-serialises the
        // markup inside (lower-cased tags, line breaks around block elements), which the mock
        // leaves as the stylesheet produced it.
        private static string WrapInHtmlEnvelope(string body)
        {
            if (body == null)
                return null;

            return "<html>\n<head>\n" +
                   "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\n" +
                   "</head>\n<body>\n" + body + "\n</body>\n</html>\n";
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailFromTemplateRequest>(orgRequest);

            // Messages and their order follow Dataverse.
            if (request.TemplateId == Guid.Empty)
                throw new FaultException("Template id should be set.");

            if (request.Target == null)
                throw new FaultException("Required field 'Target' is missing for RequestName='SendEmailFromTemplate'");

            if (request.Target.LogicalName != "email")
                throw new FaultException($"Cannot merge 2 Business entities of different types. Current Entity Type: {request.Target.LogicalName}, Entity To Merge Type: email");

            if (request.RegardingId == Guid.Empty)
                throw new FaultException("Object id should be set.");

            if (request.RegardingType == null)
                throw new FaultException("Required field 'RegardingType' is missing for RequestName='SendEmailFromTemplate'");

            if (request.RegardingType.Length == 0)
                throw new FaultException("Expected non-empty string.");

            var template = RetrieveOrThrow(new EntityReference("template", request.TemplateId), userRef);
            ValidateTemplateType(template, request.RegardingType);

            var regardingRef = new EntityReference(request.RegardingType, request.RegardingId);
            var regarding = RetrieveOrThrow(regardingRef, userRef);

            // The stylesheet addresses the regarding record by its logical name and the sending
            // user as "systemuser". When the regarding record is itself a systemuser, Dataverse
            // still merges the sender, so the sender is added last and wins the key.
            var entities = new Dictionary<string, Entity> { [request.RegardingType] = regarding };
            var sender = db.GetEntityOrNull(userRef);
            if (sender != null)
                entities[sender.LogicalName] = sender;

            // Dataverse works on its own copy and leaves the caller's Target untouched.
            var email = request.Target.CloneEntity();

            if (CanBeRegarding(request.RegardingType))
                email["regardingobjectid"] = regardingRef;

            // Dataverse sends from the caller when the e-mail names no sender.
            if (!HasSender(email))
            {
                email["from"] = new EntityCollection(new List<Entity>
                {
                    new Entity("activityparty") { ["partyid"] = userRef }
                });
            }

            email["subject"] = EmailTemplateRenderer.Render(template.GetAttributeValue<string>("subject"), entities);
            email["description"] = WrapInHtmlEnvelope(
                EmailTemplateRenderer.Render(template.GetAttributeValue<string>("body"), entities));

            // Going through Create and SendEmail keeps plugins, security and status consistent.
            var emailId = ((CreateResponse)core.Execute(new CreateRequest { Target = email }, userRef)).id;
            core.Execute(new SendEmailRequest { EmailId = emailId, IssueSend = true }, userRef);

            return new SendEmailFromTemplateResponse
            {
                Results = new ParameterCollection { { "Id", emailId } }
            };
        }
    }
}
