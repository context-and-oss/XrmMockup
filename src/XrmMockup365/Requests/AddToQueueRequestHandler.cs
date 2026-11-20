using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class AddToQueueRequestHandler : ServiceRequestHandler
    {
        internal AddToQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "AddToQueue") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<AddToQueueRequest>(orgRequest);
            RequireId(request.DestinationQueueId, "DestinationQueueId");
            if (request.Target == null) throw new FaultException("Target is required.");
            var target = Retrieve(request.Target, userRef);
            var destination = new EntityReference("queue", request.DestinationQueueId);
            Retrieve(destination, userRef);
            if (metadata.EntityMetadata[target.LogicalName].IsValidForQueue?.Value != true)
                throw new FaultException($"Records of type '{target.LogicalName}' cannot be added to a queue.");
            if (request.QueueItemProperties != null && request.QueueItemProperties.LogicalName != "queueitem")
                throw new FaultException("QueueItemProperties must be a queueitem.");

            var query = new QueryExpression("queueitem") { ColumnSet = new ColumnSet(true) };
            query.Criteria.AddCondition("objectid", ConditionOperator.Equal, target.Id);
            var items = RetrieveMultiple(query, userRef).Entities
                .Where(e => e.GetAttributeValue<EntityReference>("objectid")?.LogicalName == target.LogicalName).ToList();
            Entity existing;
            if (orgRequest.Parameters.Contains("SourceQueueId") && orgRequest["SourceQueueId"] != null)
            {
                RequireId(request.SourceQueueId, "SourceQueueId");
                Retrieve(new EntityReference("queue", request.SourceQueueId), userRef);
                existing = items.FirstOrDefault(e => e.GetAttributeValue<EntityReference>("queueid")?.Id == request.SourceQueueId);
                if (existing == null) throw new FaultException("The target is not in the source queue.");
                if (items.Any(e => e.Id != existing.Id && e.GetAttributeValue<EntityReference>("queueid")?.Id == destination.Id))
                    throw new FaultException("The target is already in the destination queue.");
            }
            else
            {
                existing = items.FirstOrDefault(e => e.GetAttributeValue<EntityReference>("queueid")?.Id == destination.Id);
                // Received email is the only record allowed in more than one queue.
                if (existing == null && !(target.LogicalName == "email" && target.GetAttributeValue<OptionSetValue>("statuscode")?.Value == 4))
                    existing = items.FirstOrDefault();
            }
            var item = new Entity("queueitem", existing?.Id ?? Guid.Empty);
            if (existing == null && request.QueueItemProperties != null)
                foreach (var attribute in request.QueueItemProperties.Attributes)
                    if (attribute.Key != "queueitemid") item[attribute.Key] = attribute.Value;
            item["queueid"] = destination;
            item["objectid"] = target.ToEntityReference();
            Guid id;
            if (existing == null)
                id = ((CreateResponse)core.Execute(new CreateRequest { Target = item }, userRef)).id;
            else
            {
                core.Execute(new UpdateRequest { Target = item }, userRef);
                id = existing.Id;
            }
            var response = new AddToQueueResponse();
            response.Results["QueueItemId"] = id;
            return response;
        }
    }
}
