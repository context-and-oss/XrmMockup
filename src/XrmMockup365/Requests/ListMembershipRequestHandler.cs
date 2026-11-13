using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    // AddMemberList and AddListMembersList share the same membership rules and persistence.
    internal abstract class ListMembershipRequestHandler : ServiceRequestHandler
    {
        protected ListMembershipRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security, string requestName)
            : base(core, db, metadata, security, requestName) { }

        protected Guid[] AddListMembers(Guid listId, Guid[] memberIds, EntityReference caller)
        {
            RequireId(listId, "ListId");
            if (memberIds == null) throw new FaultException("MemberIds is required.");
            var list = Retrieve(new EntityReference("list", listId), caller);
            if (list.GetAttributeValue<bool>("lockstatus")) throw new FaultException("The marketing list is locked.");
            if (list.GetAttributeValue<bool>("type")) throw new FaultException("Members cannot be manually added to a dynamic marketing list.");
            var code = list.GetAttributeValue<OptionSetValue>("createdfromcode")?.Value;
            var entityName = code == 1 ? "account" : code == 2 ? "contact" : code == 4 ? "lead" : null;
            if (entityName == null) throw new FaultException("The marketing list must specify account, contact or lead as its member type.");
            var uniqueIds = memberIds.Distinct().ToArray();
            // Validate every member before changing membership.
            foreach (var id in uniqueIds)
            {
                RequireId(id, "MemberId");
                Retrieve(new EntityReference(entityName, id), caller);
            }
            var query = new QueryExpression("listmember") { ColumnSet = new ColumnSet(true) };
            query.Criteria.AddCondition("listid", ConditionOperator.Equal, listId);
            var existing = RetrieveMultiple(query, caller).Entities.ToDictionary(e => MemberId(e["entityid"]), e => e.Id);
            var snapshot = core.TakeJsonSnapshot();
            try
            {
                foreach (var id in uniqueIds)
                    if (!existing.ContainsKey(id))
                    {
                        var member = new Entity("listmember") { ["listid"] = new EntityReference("list", listId) };
                        // Some CRM versions expose the polymorphic intersect column as Uniqueidentifier.
                        var attribute = metadata.EntityMetadata["listmember"].Attributes.First(a => a.LogicalName == "entityid");
                        member["entityid"] = attribute.AttributeType == AttributeTypeCode.Uniqueidentifier
                            ? (object)id : new EntityReference(entityName, id);
                        existing[id] = ((CreateResponse)core.Execute(new CreateRequest { Target = member }, caller)).id;
                    }
                return memberIds.Select(id => existing[id]).ToArray();
            }
            catch
            {
                core.RestoreJsonSnapshot(snapshot);
                throw;
            }
        }

        private static Guid MemberId(object value) => value is EntityReference reference ? reference.Id : (Guid)value;
    }
}
