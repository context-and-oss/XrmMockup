using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// Merges an e-mail template against a record. This is everything <c>InstantiateTemplate</c>
    /// does, and the first half of <c>SendEmailFromTemplate</c>.
    /// </summary>
    internal class EmailTemplateInstantiator
    {
        private readonly XrmDb db;
        private readonly MetadataSkeleton metadata;
        private readonly Security security;

        public EmailTemplateInstantiator(XrmDb db, MetadataSkeleton metadata, Security security)
        {
            this.db = db;
            this.metadata = metadata;
            this.security = security;
        }

        /// <summary>
        /// Returns an unsaved e-mail carrying the merged subject and description, and nothing else.
        /// </summary>
        public Entity Instantiate(Guid templateId, string objectType, Guid objectId, EntityReference userRef)
        {
            var template = RetrieveOrThrow(new EntityReference(LogicalNames.Template, templateId), userRef);
            ValidateTemplateType(template, objectType);

            var record = RetrieveOrThrow(new EntityReference(objectType, objectId), userRef);

            // The stylesheet addresses the merged record by its logical name and the calling user
            // as "systemuser". When the record is itself a systemuser, Dataverse still merges the
            // caller, so the caller is added last and wins the key.
            var entities = new Dictionary<string, Entity> { [objectType] = record };
            var caller = db.GetEntityOrNull(userRef);
            if (caller != null)
                entities[caller.LogicalName] = caller;

            return new Entity(LogicalNames.Email)
            {
                ["subject"] = EmailTemplateRenderer.Render(template.GetAttributeValue<string>("subject"), entities),
                ["description"] = WrapInHtmlEnvelope(
                    EmailTemplateRenderer.Render(template.GetAttributeValue<string>("body"), entities))
            };
        }

        // Dataverse fails the merge rather than rendering an unreadable record as blanks.
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

        // Dataverse rejects a record whose type differs from the template's, and does so before it
        // looks that record up.
        private void ValidateTemplateType(Entity template, string objectType)
        {
            var templateTypeCode = GetTemplateTypeCode(template);
            metadata.EntityMetadata.TryGetValue(objectType, out var objectMetadata);
            var objectTypeCode = objectMetadata?.ObjectTypeCode;

            if (templateTypeCode == null || objectTypeCode == null)
                return;

            if (templateTypeCode != objectTypeCode)
            {
                throw new FaultException(
                    $"Template type is incorrect for given objectType {objectTypeCode} != {templateTypeCode} template.templatetypecode");
            }
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
    }
}
