using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class NavigateToNextEntityRequestHandler : ServiceRequestHandler
    {
        internal NavigateToNextEntityRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "NavigateToNextEntity") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var processId = Required<Guid>(orgRequest, "ProcessId");
            var stageId = Required<Guid>(orgRequest, "NewActiveStageId");
            var currentName = Required<string>(orgRequest, "CurrentEntityLogicalName");
            var nextName = Required<string>(orgRequest, "NextEntityLogicalName");
            var currentId = Required<Guid>(orgRequest, "CurrentEntityId");
            var nextId = Required<Guid>(orgRequest, "NextEntityId");
            var path = Required<string>(orgRequest, "NewTraversedPath");
            var process = Retrieve(new EntityReference("workflow", processId), userRef);
            var stage = Retrieve(new EntityReference("processstage", stageId), userRef);
            if (process.GetAttributeValue<OptionSetValue>("category")?.Value != 4)
                throw new FaultException("ProcessId must refer to a business process flow.");
            if (stage.GetAttributeValue<EntityReference>("processid")?.Id != processId)
                throw new FaultException("The new stage does not belong to the specified process.");
            if (stage.GetAttributeValue<string>("primaryentitytypecode") != nextName)
                throw new FaultException("The new stage does not belong to the next entity type.");
            var stages = path.Split(',');
            if (stages.Any(s => !Guid.TryParse(s, out var id) || id == Guid.Empty) || Guid.Parse(stages.Last()) != stageId)
                throw new FaultException("NewTraversedPath must end with the new active stage.");
            foreach (var id in stages.Select(Guid.Parse))
                if (Retrieve(new EntityReference("processstage", id), userRef).GetAttributeValue<EntityReference>("processid")?.Id != processId)
                    throw new FaultException("All traversed stages must belong to the specified process.");
            var current = new EntityReference(currentName, currentId);
            var next = new EntityReference(nextName, nextId);
            Retrieve(current, userRef);
            Retrieve(next, userRef);
            var requests = new OrganizationRequestCollection();
            foreach (var record in new[] { current, next }.Distinct())
                requests.Add(new UpdateRequest { Target = new Entity(record.LogicalName, record.Id)
                {
                    ["processid"] = processId, ["stageid"] = stageId, ["traversedpath"] = path
                } });
            core.Execute(new ExecuteTransactionRequest { Requests = requests }, userRef);
            var response = new OrganizationResponse { ResponseName = "NavigateToNextEntity" };
            response.Results["TraversedPath"] = path;
            return response;
        }

        private static T Required<T>(OrganizationRequest request, string name)
        {
            if (!request.Parameters.TryGetValue(name, out var value) || !(value is T typed) ||
                (value is Guid guid && guid == Guid.Empty) || (value is string text && string.IsNullOrWhiteSpace(text)))
                throw new FaultException(name + " is required and must have a valid value.");
            return typed;
        }
    }
}
