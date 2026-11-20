using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk.Metadata;

namespace WorkflowExecuter
{
    static class Util
    {
        public static DataCollection<Entity> GetRelatedEntities(string relatedEntityName, Dictionary<string, object> variables,
            IOrganizationService orgService)
        {
            var primaryEntityKey = "InputEntities(\"primaryEntity\")";
            var relatedlinked = "relatedlinked";
            QueryExpression query = new QueryExpression();
            query.EntityName = relatedEntityName;
            query.ColumnSet = new ColumnSet(true);
            Relationship relationship = new Relationship();
            relationship.SchemaName = variables[relatedlinked + "_" + relatedEntityName] as string;
            RelationshipQueryCollection relatedEntity = new RelationshipQueryCollection();
            relatedEntity.Add(relationship, query);
            RetrieveRequest request = new RetrieveRequest();
            request.RelatedEntitiesQuery = relatedEntity;
            request.ColumnSet = new ColumnSet();
            var primaryEntity = variables[primaryEntityKey] as Entity;
            request.Target = primaryEntity.ToEntityReference();
            RetrieveResponse response = (RetrieveResponse)orgService.Execute(request);
            return response.Entity.RelatedEntities[relationship].Entities;
        }

        public static string GetPrimaryName(EntityReference entityReference, IOrganizationService orgService)
        {
            var primaryNameAttribute = ((RetrieveEntityResponse)orgService.Execute(new RetrieveEntityRequest
            {
                LogicalName = entityReference.LogicalName,
                EntityFilters = EntityFilters.Entity
            })).EntityMetadata.PrimaryNameAttribute;
            var referencedEntity = orgService.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(primaryNameAttribute));
            return referencedEntity.GetAttributeValue<string>(primaryNameAttribute);
        }

        public static string GetOptionSetValueLabel(string entityLogicalName, string attributeLogicalName, OptionSetValue optionSetValue, IOrganizationService orgService)
        {
            var optionSetMetadata = ((PicklistAttributeMetadata)((RetrieveAttributeResponse)orgService.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName
            })).AttributeMetadata).OptionSet;

            var option = optionSetMetadata.Options.SingleOrDefault(x => x.Value == optionSetValue.Value);
            return option.Label.UserLocalizedLabel.Label;
        }

        public static string GetBooleanLabel(string entityLogicalName, string attributeLogicalName, bool value, IOrganizationService orgService)
        {
            var booleanMetadata = ((BooleanAttributeMetadata)((RetrieveAttributeResponse)orgService.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName
            })).AttributeMetadata).OptionSet;
            var option = value ? booleanMetadata.TrueOption : booleanMetadata.FalseOption;
            return option.Label.UserLocalizedLabel.Label;
        }

        public static XrmWorkflowContext GetDefaultContext()
        {
            var userId = Guid.NewGuid();
            return new XrmWorkflowContext()
            {
                Depth = 1,
                IsExecutingOffline = false,
                MessageName = "Create",
                UserId = userId,
                InitiatingUserId = userId,
                InputParameters = new ParameterCollection(),
                OutputParameters = new ParameterCollection(),
                SharedVariables = new ParameterCollection(),
                PreEntityImages = new EntityImageCollection(),
                PostEntityImages = new EntityImageCollection()
            };
        }
    }
}
