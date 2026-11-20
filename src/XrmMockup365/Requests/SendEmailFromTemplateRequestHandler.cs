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
        private readonly EmailTemplateInstantiator instantiator;

        public SendEmailFromTemplateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SendEmailFromTemplate")
        {
            instantiator = new EmailTemplateInstantiator(db, metadata, security);
        }

        // Dataverse leaves regardingobjectid empty when the lookup cannot target the regarding
        // type, as with a systemuser template.
        private bool CanBeRegarding(string logicalName)
        {
            if (!metadata.EntityMetadata.TryGetValue(LogicalNames.Email, out var emailMetadata))
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

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailFromTemplateRequest>(orgRequest);

            // Messages and their order follow Dataverse.
            if (request.TemplateId == Guid.Empty)
                throw new FaultException("Template id should be set.");

            if (request.Target == null)
                throw new FaultException("Required field 'Target' is missing for RequestName='SendEmailFromTemplate'");

            if (request.Target.LogicalName != LogicalNames.Email)
                throw new FaultException($"Cannot merge 2 Business entities of different types. Current Entity Type: {request.Target.LogicalName}, Entity To Merge Type: email");

            if (request.RegardingId == Guid.Empty)
                throw new FaultException("Object id should be set.");

            if (request.RegardingType == null)
                throw new FaultException("Required field 'RegardingType' is missing for RequestName='SendEmailFromTemplate'");

            if (request.RegardingType.Length == 0)
                throw new FaultException("Expected non-empty string.");

            var merged = instantiator.Instantiate(request.TemplateId, request.RegardingType, request.RegardingId, userRef);

            // Dataverse works on its own copy and leaves the caller's Target untouched.
            var email = request.Target.CloneEntity();

            if (CanBeRegarding(request.RegardingType))
                email["regardingobjectid"] = new EntityReference(request.RegardingType, request.RegardingId);

            // Dataverse sends from the caller when the e-mail names no sender.
            if (!HasSender(email))
            {
                email["from"] = new EntityCollection(new List<Entity>
                {
                    new Entity("activityparty") { ["partyid"] = userRef }
                });
            }

            email["subject"] = merged["subject"];
            email["description"] = merged["description"];

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
