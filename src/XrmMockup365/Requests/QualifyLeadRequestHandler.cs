using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup
{
    internal class QualifyLeadRequestHandler : RequestHandler
    {
        // Lead state codes (statecode option values).
        private const int LeadStateOpen = 0;
        private const int LeadStateQualified = 1;

        // Maps a source attribute on the lead to the destination attribute on the created record.
        // Only attributes present on both the lead and the target's metadata are copied.
        private static readonly Dictionary<string, string> LeadToAccountAttributeMap = new Dictionary<string, string>
        {
            { "sic", "sic" },
            { "emailaddress1", "emailaddress1" },
            { "companyname", "name" },
            { "fax", "fax" },
            { "websiteurl", "websiteurl" },
            { "address1_country", "address1_country" },
            { "address1_city", "address1_city" },
            { "address1_line1", "address1_line1" },
            { "address1_line2", "address1_line2" },
            { "address1_line3", "address1_line3" },
            { "address1_postalcode", "address1_postalcode" },
            { "address1_stateorprovince", "address1_stateorprovince" },
            { "telephone2", "telephone2" },
            { "donotpostalmail", "donotpostalmail" },
            { "donotphone", "donotphone" },
            { "donotfax", "donotfax" },
            { "donotsendmm", "donotsendmm" },
            { "description", "description" },
            { "donotemail", "donotemail" },
            { "yomicompanyname", "yominame" },
            { "donotbulkemail", "donotbulkemail" },
        };

        private static readonly Dictionary<string, string> LeadToContactAttributeMap = new Dictionary<string, string>
        {
            { "mobilephone", "mobilephone" },
            { "emailaddress1", "emailaddress1" },
            { "emailaddress2", "emailaddress2" },
            { "emailaddress3", "emailaddress3" },
            { "websiteurl", "websiteurl" },
            { "yomifirstname", "yomifirstname" },
            { "yomimiddlename", "yomimiddlename" },
            { "yomilastname", "yomilastname" },
            { "firstname", "firstname" },
            { "lastname", "lastname" },
            { "jobtitle", "jobtitle" },
            { "pager", "pager" },
            { "fax", "fax" },
            { "telephone2", "telephone2" },
            { "telephone3", "telephone3" },
            { "description", "description" },
            { "donotpostalmail", "donotpostalmail" },
            { "donotphone", "donotphone" },
            { "donotfax", "donotfax" },
            { "donotemail", "donotemail" },
            { "donotsendmm", "donotsendmm" },
            { "donotbulkemail", "donotbulkemail" },
            { "address1_country", "address1_country" },
            { "address1_city", "address1_city" },
            { "address1_line1", "address1_line1" },
            { "address1_line2", "address1_line2" },
            { "address1_line3", "address1_line3" },
            { "address1_postalcode", "address1_postalcode" },
            { "address1_stateorprovince", "address1_stateorprovince" },
        };

        private static readonly Dictionary<string, string> LeadToOpportunityAttributeMap = new Dictionary<string, string>
        {
            { "subject", "name" },
            { "qualificationcomments", "qualificationcomments" },
            { "description", "description" },
        };

        internal QualifyLeadRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "QualifyLead") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<QualifyLeadRequest>(orgRequest);

            if (request.LeadId == null)
            {
                throw new FaultException("Required field 'LeadId' is missing");
            }

            // GetMetadata throws a descriptive fault if the entity is unknown to the metadata cache.
            var leadMetadata = metadata.EntityMetadata.GetMetadata(request.LeadId.LogicalName);
            if (request.OpportunityCurrencyId != null &&
                request.OpportunityCurrencyId.LogicalName != LogicalNames.TransactionCurrency)
            {
                throw new FaultException($"OpportunityCurrencyId must reference a '{LogicalNames.TransactionCurrency}'.");
            }
            if (request.OpportunityCustomerId != null)
            {
                metadata.EntityMetadata.GetMetadata(request.OpportunityCustomerId.LogicalName);
            }

            var leadRow = db.GetDbRowOrNull(request.LeadId);
            if (leadRow == null || request.LeadId.LogicalName != LogicalNames.Lead)
            {
                throw new FaultException($"{LogicalNames.Lead} With Id = {request.LeadId.Id} Does Not Exist");
            }

            var lead = leadRow.ToEntity();

            // A lead can only be qualified/disqualified while it is still open.
            var currentState = lead.GetAttributeValue<OptionSetValue>("statecode");
            if (currentState != null && currentState.Value != LeadStateOpen)
            {
                throw new FaultException($"The {LogicalNames.Lead} is already closed.");
            }

            if (request.Status == null)
            {
                throw new FaultException("Required field 'Status' is missing");
            }
            var status = request.Status.Value;

            var statusOption = Utility.GetStatusOptionMetadata(leadMetadata)
                .FirstOrDefault(s => s.Value == status) as StatusOptionMetadata;
            if (statusOption == null)
            {
                throw new FaultException($"{status} is not a valid status code on {LogicalNames.Lead} with Id {request.LeadId.Id}");
            }

            // The requested status determines the resulting state (Qualified/Disqualified/...).
            var state = statusOption.State ?? LeadStateQualified;

            var createdEntities = new EntityReferenceCollection();

            if (request.CreateAccount)
            {
                createdEntities.Add(CreateFromLead(lead, LogicalNames.Account, LeadToAccountAttributeMap, userRef));
            }
            if (request.CreateContact)
            {
                createdEntities.Add(CreateFromLead(lead, LogicalNames.Contact, LeadToContactAttributeMap, userRef));
            }
            if (request.CreateOpportunity)
            {
                createdEntities.Add(CreateOpportunityFromLead(lead, request.OpportunityCustomerId, request.OpportunityCurrencyId, userRef));
            }

            // Close the lead in the requested state (mirrors SetStateRequestHandler).
            if (Utility.IsValidAttribute("statecode", leadMetadata) && Utility.IsValidAttribute("statuscode", leadMetadata))
            {
                var previous = lead.CloneEntity();
                lead["statecode"] = new OptionSetValue(state);
                lead["statuscode"] = new OptionSetValue(status);
                Utility.CheckStatusTransitions(leadMetadata, lead, previous);
                Utility.HandleCurrencies(metadata, db, lead);
                Utility.Touch(lead, leadMetadata, core.TimeOffset, userRef);
                db.Update(lead);
            }

            var response = new QualifyLeadResponse();
            response.Results["CreatedEntities"] = createdEntities;
            return response;
        }

        private EntityReference CreateFromLead(Entity lead, string targetLogicalName, IDictionary<string, string> attributeMap, EntityReference userRef)
        {
            var target = new Entity(targetLogicalName);
            MapAttributesFromLead(lead, target, attributeMap);
            SetIfValid(target, "originatingleadid", lead.ToEntityReference());
            target.Id = CreateEntity(target, userRef);
            return target.ToEntityReference();
        }

        private EntityReference CreateOpportunityFromLead(Entity lead, EntityReference customer, EntityReference currency, EntityReference userRef)
        {
            if (customer != null &&
                customer.LogicalName != LogicalNames.Account &&
                customer.LogicalName != LogicalNames.Contact)
            {
                throw new FaultException($"CustomerIdType for {LogicalNames.Opportunity} can either be an {LogicalNames.Account} or {LogicalNames.Contact}");
            }

            var opportunity = new Entity(LogicalNames.Opportunity);
            MapAttributesFromLead(lead, opportunity, LeadToOpportunityAttributeMap);
            SetIfValid(opportunity, "originatingleadid", lead.ToEntityReference());
            if (customer != null)
            {
                SetIfValid(opportunity, "customerid", customer);
            }
            if (currency != null)
            {
                SetIfValid(opportunity, "transactioncurrencyid", currency);
            }
            opportunity.Id = CreateEntity(opportunity, userRef);
            return opportunity.ToEntityReference();
        }

        private void MapAttributesFromLead(Entity lead, Entity target, IDictionary<string, string> attributeMap)
        {
            var targetMetadata = metadata.EntityMetadata.GetMetadata(target.LogicalName);
            foreach (var mapping in attributeMap)
            {
                if (!lead.Attributes.Contains(mapping.Key))
                {
                    continue;
                }
                if (!Utility.IsValidAttribute(mapping.Value, targetMetadata))
                {
                    core.Logger?.LogWarning(
                        "QualifyLead: skipped copying lead.{Source} to {Target}.{Destination} because the target entity's metadata does not define that attribute.",
                        mapping.Key, target.LogicalName, mapping.Value);
                    continue;
                }
                target[mapping.Value] = lead[mapping.Key];
            }
        }

        private void SetIfValid(Entity target, string attribute, object value)
        {
            var targetMetadata = metadata.EntityMetadata.GetMetadata(target.LogicalName);
            if (Utility.IsValidAttribute(attribute, targetMetadata))
            {
                target[attribute] = value;
            }
            else
            {
                core.Logger?.LogWarning(
                    "QualifyLead: skipped setting {Target}.{Attribute} because the target entity's metadata does not define that attribute.",
                    target.LogicalName, attribute);
            }
        }

        private Guid CreateEntity(Entity entity, EntityReference userRef)
        {
            var response = core.Execute(new CreateRequest { Target = entity }, userRef) as CreateResponse;
            return response.id;
        }
    }
}
