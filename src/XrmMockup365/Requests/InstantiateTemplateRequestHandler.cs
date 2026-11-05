using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class InstantiateTemplateRequestHandler : RequestHandler
    {
        private readonly EmailTemplateInstantiator instantiator;

        public InstantiateTemplateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "InstantiateTemplate")
        {
            instantiator = new EmailTemplateInstantiator(db, metadata, security);
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InstantiateTemplateRequest>(orgRequest);

            // Messages and their order follow Dataverse.
            if (request.TemplateId == Guid.Empty)
                throw new FaultException("Template id should be set.");

            if (request.ObjectId == Guid.Empty)
                throw new FaultException("Object id should be set.");

            if (request.ObjectType == null)
                throw new FaultException("Required field 'ObjectType' is missing for RequestName='InstantiateTemplate'");

            if (request.ObjectType.Length == 0)
                throw new FaultException("Expected non-empty string.");

            // Nothing is persisted: the merged content is the whole response.
            var email = instantiator.Instantiate(request.TemplateId, request.ObjectType, request.ObjectId, userRef);

            return new InstantiateTemplateResponse
            {
                Results = new ParameterCollection
                {
                    { "EntityCollection", new EntityCollection(new List<Entity> { email }) }
                },
                ResponseName = "InstantiateTemplate",
            };
        }
    }
}
