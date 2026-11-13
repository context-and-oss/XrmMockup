using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk.Metadata;

namespace DG.Tools.XrmMockup
{
    internal class InitializeFromRequestHandler : ServiceRequestHandler
    {
        internal InitializeFromRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "InitializeFrom") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InitializeFromRequest>(orgRequest);
            if (request.EntityMoniker == null) throw new FaultException("EntityMoniker is required.");
            if (string.IsNullOrWhiteSpace(request.TargetEntityName) || !metadata.EntityMetadata.TryGetValue(request.TargetEntityName, out var targetMetadata))
                throw new FaultException("A valid TargetEntityName is required.");
            if (!Enum.IsDefined(typeof(TargetFieldType), request.TargetFieldType)) throw new FaultException("Invalid TargetFieldType.");
            var source = Retrieve(request.EntityMoniker, userRef);
            var target = new Entity(request.TargetEntityName);
            var maps = new QueryExpression("entitymap") { ColumnSet = new ColumnSet(true) };
            maps.Criteria.AddCondition("sourceentityname", ConditionOperator.Equal, source.LogicalName);
            maps.Criteria.AddCondition("targetentityname", ConditionOperator.Equal, target.LogicalName);
            foreach (var map in RetrieveMultiple(maps, userRef).Entities)
            {
                var attributes = new QueryExpression("attributemap") { ColumnSet = new ColumnSet(true) };
                attributes.Criteria.AddCondition("entitymapid", ConditionOperator.Equal, map.Id);
                foreach (var mapping in RetrieveMultiple(attributes, userRef).Entities)
                {
                    var sourceName = mapping.GetAttributeValue<string>("sourceattributename");
                    var targetName = mapping.GetAttributeValue<string>("targetattributename");
                    var attribute = targetMetadata.Attributes.FirstOrDefault(a => a.LogicalName == targetName);
                    if (attribute == null || targetName == targetMetadata.PrimaryIdAttribute || !source.Attributes.Contains(sourceName)) continue;
                    if (IsValidTargetField(attribute, request.TargetFieldType)) target[targetName] = source[sourceName];
                }
            }
            if (!request.SkipParentalRelationshipMapping)
                foreach (var relationship in targetMetadata.ManyToOneRelationships ?? Array.Empty<OneToManyRelationshipMetadata>())
                    if (relationship.ReferencedEntity == source.LogicalName &&
                        relationship.CascadeConfiguration?.Reparent != null && relationship.CascadeConfiguration.Reparent != CascadeType.NoCascade)
                    {
                        var attribute = targetMetadata.Attributes.FirstOrDefault(a => a.LogicalName == relationship.ReferencingAttribute);
                        if (attribute != null && IsValidTargetField(attribute, request.TargetFieldType))
                            target[relationship.ReferencingAttribute] = source.ToEntityReference();
                    }
            var response = new InitializeFromResponse();
            response.Results["Entity"] = target;
            return response;
        }

        private static bool IsValidTargetField(AttributeMetadata attribute, TargetFieldType type)
        {
            switch (type)
            {
                case TargetFieldType.All: return true;
                case TargetFieldType.ValidForCreate: return attribute.IsValidForCreate == true;
                case TargetFieldType.ValidForUpdate: return attribute.IsValidForUpdate == true;
                case TargetFieldType.ValidForRead: return attribute.IsValidForRead == true;
                default: return false;
            }
        }
    }
}
