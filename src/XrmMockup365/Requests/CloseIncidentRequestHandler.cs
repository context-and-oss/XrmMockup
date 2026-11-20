using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup
{
    internal class CloseIncidentRequestHandler : RequestHandler
    {
        internal CloseIncidentRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "CloseIncident") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {

            var request = MakeRequest<CloseIncidentRequest>(orgRequest);

            if (request.Status == null)
            {
                throw new FaultException("Required field 'Status' is missing");
            }

            if (request.IncidentResolution == null)
            {
                throw new FaultException("Incident resolution is missing");
            }

            if (request.IncidentResolution.LogicalName == null)
            {
                throw new FaultException("Required member 'LogicalName' missing for field 'IncidentResolution'");
            }

            if (request.IncidentResolution.LogicalName != "incidentresolution")
            {
                throw new FaultException("An unexpected error occurred.");
            }

            if (!metadata.EntityMetadata.ContainsKey(request.IncidentResolution.LogicalName))
            {
                throw new FaultException($"The entity with a name = '{request.IncidentResolution.LogicalName}' was not found in the MetadataCache.");
            }

            var incidentRef = request.IncidentResolution.GetAttributeValue<EntityReference>("incidentid");

            if (incidentRef == null)
            {
                throw new FaultException("The incident id is missing.");
            }

            var entityMetadata = metadata.EntityMetadata.GetMetadata("incident");
            var statusOptionMeta = Utility.GetStatusOptionMetadata(entityMetadata);

            if (!statusOptionMeta.Any(o => (o as StatusOptionMetadata).Value == request.Status.Value
                && ((o as StatusOptionMetadata).State == 1 || (o as StatusOptionMetadata).State == 2)))
            {
                throw new FaultException($"{request.Status.Value} is not a valid status code on incident with Id {incidentRef.Id}.");
            }

            var incident = db.GetEntity(incidentRef);

            if (incident == null)
            {
                throw new FaultException($"incident With Id = {incidentRef.Id} Does Not Exist");
            }
            var incidentState = incident["statecode"] as OptionSetValue;

            if (incidentState.Value == 1)
            {
                throw new FaultException($"incident With Id = {incidentRef.Id} is resolved and connot be closed");
            }

            if (incidentState.Value == 2)
            {
                throw new FaultException($"incident With Id = {incidentRef.Id} is allready cancelled and connot be closed");
            }

            var incidentResolution = db.GetDbRowOrNull(request.IncidentResolution.ToEntityReference());

            if (incidentResolution != null)
            {
                throw new FaultException("Cannot insert duplicate key.");
            }

            db.Add(request.IncidentResolution);

            var incidentStatecode = statusOptionMeta.Where(o => (o as StatusOptionMetadata).Value == request.Status.Value).Select(o => (o as StatusOptionMetadata).State).FirstOrDefault() ?? 1;

            incident["statecode"] = new OptionSetValue(incidentStatecode);
            incident["statuscode"] = request.Status;

            var updateRequest = new UpdateRequest
            {
                Target = incident,
                ["CloseIncidentRequestHandler"] = true,
            };

            core.Execute(updateRequest, userRef);
            
            return new CloseIncidentResponse();
        }
    }
}
