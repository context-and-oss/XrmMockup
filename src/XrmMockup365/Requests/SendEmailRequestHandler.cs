using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class SendEmailRequestHandler : RequestHandler
    {
        const int EMAIL_STATE_COMPLETED = 1;
        const int EMAIL_STATUS_DRAFT = 1;
        const int EMAIL_STATUS_FAILED = 8;
        const int EMAIL_STATUS_PENDING_SEND = 6;
        const int EMAIL_STATUS_SENT = 3;

        public SendEmailRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "SendEmail") { }

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailRequest>(orgRequest);
            if (request.EmailId == Guid.Empty)
            {
                throw new FaultException("Required field 'EmailId' is missing");
            }

            var emailRef = new EntityReference("email", request.EmailId);

            if (!security.HasPermission(emailRef, AccessRights.ReadAccess, userRef))
            {
                throw new FaultException($"Principal user (Id={userRef.Id}) is missing Read privilege for email (Id={emailRef.Id})");
            }

            if (!security.HasPermission(emailRef, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException($"Principal user (Id={userRef.Id}) is missing Write privilege for email (Id={emailRef.Id})");
            }

            var email = db.GetEntityOrNull(emailRef) ?? throw new FaultException($"email with Id = {request.EmailId} does not exist");

            if (email.Contains("regardingobjectid"))
            {
                var regardingObject = email.GetAttributeValue<EntityReference>("regardingobjectid");

                if (!security.HasPermission(regardingObject, AccessRights.ReadAccess, userRef))
                {
                    throw new FaultException($"Principal user (Id={userRef.Id}) is missing Read privilege for {regardingObject.LogicalName} (Id={regardingObject.Id})");
                }
            }

            // Remaining security checks have been omitted to reduce complexity
        }

        private static string GetEmailAddress(Entity entity)
        {
            if (entity.LogicalName == "systemuser")
            {
                return entity.GetAttributeValue<string>("internalemailaddress");
            }
            else if (entity.LogicalName == "queue")
            {
                return entity.GetAttributeValue<string>("emailaddress");
            }
            else
            {
                return entity.GetAttributeValue<string>("emailaddress1");
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<SendEmailRequest>(orgRequest);

            var email = db.GetEntity(new EntityReference("email", request.EmailId));

            var statusCode = email.GetAttributeValue<OptionSetValue>("statuscode").Value;
            if (statusCode != EMAIL_STATUS_DRAFT && statusCode != EMAIL_STATUS_FAILED)
            {
                throw new FaultException("Email must be in Draft or Failed status to send");
            }

            if (email.GetAttributeValue<bool>("directioncode") is false)
            {
                throw new FaultException("Cannot send incoming email messages");
            }

            if (email.Contains("from"))
            {
                var from = email.GetAttributeValue<EntityCollection>("from");
                if (from.Entities.Count != 1)
                {
                    throw new FaultException("The email must have one and only one sender");
                }

                var activityParty = from.Entities[0];
                if (activityParty.Contains("partyid"))
                {
                    var partyRef = activityParty.GetAttributeValue<EntityReference>("partyid");
                    if (db.GetEntityOrNull(partyRef) is null)
                    {
                        throw new FaultException($"{partyRef.LogicalName} with Id = {partyRef.Id} does not exist");
                    }
                }
                else
                {
                    throw new FaultException("Sender cannot be unresolved");
                }
            }
            else
            {
                throw new FaultException("The email must have a sender");
            }

            if (email.Contains("to"))
            {
                var to = email.GetAttributeValue<EntityCollection>("to");
                if (to.Entities.Count is 0)
                {
                    throw new FaultException("The email must have at least one recipient before it can be sent");
                }

                foreach (Entity activityParty in to.Entities)
                {
                    if (activityParty.Contains("partyid"))
                    {
                        var partyRef = activityParty.GetAttributeValue<EntityReference>("partyid");
                        var partyEntity = db.GetEntityOrNull(partyRef);
                        if (partyEntity is null)
                        {
                            throw new FaultException($"{partyRef.LogicalName} with Id = {partyRef.Id} does not exist");
                        }
                        if (string.IsNullOrEmpty(GetEmailAddress(partyEntity)))
                        {
                            throw new FaultException($"{partyRef.LogicalName} with Id = {partyRef.Id} does not have an email address");
                        }
                    }
                    else if (!activityParty.Contains("addressused"))
                    {
                        throw new FaultException("Invalid ActivityParty");
                    }
                }
            }
            else
            {
                throw new FaultException("The email must have at least one recipient before it can be sent");
            }

            email["statecode"] = new OptionSetValue(EMAIL_STATE_COMPLETED);
            email["statuscode"] = new OptionSetValue(request.IssueSend ? EMAIL_STATUS_PENDING_SEND : EMAIL_STATUS_SENT);

            db.Update(email);

            return new SendEmailResponse
            {
                Results = new ParameterCollection
                {
                    { "Subject", email.Contains("subject") ? email.GetAttributeValue<string>("subject") : null }
                }
            };
        }
    }
}
