using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class CreateRequestHandler : RequestHandler
    {
        internal CreateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core,
            db, metadata, security, "Create")
        {
        }

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CreateRequest>(orgRequest);
            var settings = MockupExecutionContext.GetSettings(request);
            var entity = request.Target;
            if (entity.LogicalName == null) throw new MockupException("Entity needs a logical name");

            var entityMetadata = metadata.EntityMetadata.GetMetadata(entity.LogicalName);
            var clonedEntity = entity.CloneEntity(entityMetadata, new ColumnSet(true));
            var validAttributes = clonedEntity.Attributes.Where(x => x.Value != null);
            clonedEntity.Attributes = new AttributeCollection();
            clonedEntity.Attributes.AddRange(validAttributes);

            // Resolve the effective owner for the access checks only. The persisted default is applied in
            // Execute and the plugin-visible Target is intentionally left without an ownerid, matching
            // Dataverse. Ownership-based checks (e.g. user-level Append) need the effective owner here.
            if (!clonedEntity.Attributes.ContainsKey("ownerid") &&
                Utility.IsValidAttribute("ownerid", entityMetadata))
            {
                clonedEntity["ownerid"] = userRef;
            }

            if (userRef != null && userRef.Id != Guid.Empty)
            {
                if (!security.HasPermission(clonedEntity, AccessRights.CreateAccess, userRef))
                {
                    throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                        $", but the calling user with id '{userRef.Id}' does not have Create access for that entity (SecLib::AccessCheckEx2 failed)");
                }

                if (core.GetMockupSettings().AppendAndAppendToPrivilegeCheck.GetValueOrDefault(true))
                {
                    var references = clonedEntity.Attributes
                        .Where(x => x.Value is EntityReference && x.Key != "ownerid")
                        .ToArray();

                    if (references.Any())
                    {
                        if (!security.HasPermission(clonedEntity, AccessRights.AppendAccess, userRef))
                        {
                            throw new FaultException($"Trying to create entity '{entity.LogicalName}' with references" +
                                $", but the calling user with id '{userRef.Id}' does not have Append access for that entity (SecLib::AccessCheckEx2 failed)");
                        }
                    }

                    foreach (var attr in references)
                    {
                        var reference = attr.Value as EntityReference;
                        if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                            !security.HasPermission(reference, AccessRights.ReadAccess, userRef))
                        {
                            throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                                $", but the calling user with id '{userRef.Id}' does not have read access for referenced entity '{reference.LogicalName}' on attribute '{attr.Key}' (SecLib::AccessCheckEx2 failed)");
                        }

                        if (!security.HasPermission(reference, AccessRights.AppendToAccess, userRef))
                        {
                            throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                                                     $", but the calling user with id '{userRef.Id}' does not have AppendTo access for referenced entity '{reference.LogicalName}' on attribute '{attr.Key}'");
                        }
                    }
                }
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CreateRequest>(orgRequest);
            var resp = new CreateResponse();
            var settings = MockupExecutionContext.GetSettings(request);
            var entity = request.Target;
            if (entity.LogicalName == null) throw new MockupException("Entity needs a logical name");

            var entityMetadata = metadata.EntityMetadata.GetMetadata(entity.LogicalName);
            var clonedEntity = entity.CloneEntity(entityMetadata, new ColumnSet(true));
            var validAttributes = clonedEntity.Attributes.Where(x => x.Value != null);
            clonedEntity.Attributes = new AttributeCollection();
            clonedEntity.Attributes.AddRange(validAttributes);

            if (Utility.HasCircularReference(metadata.EntityMetadata, clonedEntity))
            {
                throw new FaultException(
                    $"Trying to create entity '{clonedEntity.LogicalName}', but the attributes had a circular reference");
            }

            // Stamp the created/modified system attributes on the record being persisted. This runs as
            // part of the main operation (not a pre-stage) so the values surface via the post-image and
            // the post-operation Target, matching Dataverse — they are absent from the pre-stage Target.
            var createdOn = DateTime.UtcNow.Add(core.TimeOffset);
            if (Utility.IsValidAttribute("createdon", entityMetadata))
            {
                if (Utility.IsValidAttribute("overriddencreatedon", entityMetadata) &&
                    clonedEntity.Attributes.ContainsKey("overriddencreatedon") &&
                    clonedEntity["overriddencreatedon"] is DateTime overriddencreatedon)
                {
                    if (overriddencreatedon > createdOn)
                    {
                        throw new FaultException(
                            $"Trying to create entity '{clonedEntity.LogicalName}', but overriddencreatedon cannot be set to a date in the future");
                    }

                    clonedEntity["overriddencreatedon"] = createdOn;
                    createdOn = overriddencreatedon;
                }

                clonedEntity["createdon"] = createdOn;
            }

            if (Utility.IsValidAttribute("createdby", entityMetadata))
            {
                clonedEntity["createdby"] = userRef;
            }

            if (Utility.IsValidAttribute("modifiedon", entityMetadata))
            {
                clonedEntity["modifiedon"] = createdOn;
            }

            if (Utility.IsValidAttribute("modifiedby", entityMetadata))
            {
                clonedEntity["modifiedby"] = userRef;
            }

            var transactioncurrencyId = "transactioncurrencyid";
            var exchangerate = "exchangerate";
            if (!clonedEntity.Attributes.ContainsKey(transactioncurrencyId) &&
                Utility.IsValidAttribute(transactioncurrencyId, entityMetadata) &&
                entityMetadata.Attributes.Any(m => m is MoneyAttributeMetadata) &&
                (settings.ServiceRole == MockupServiceSettings.Role.UI ||
                 (settings.ServiceRole == MockupServiceSettings.Role.SDK && clonedEntity.Attributes.Any(
                     attr => entityMetadata.Attributes.Where(a => a is MoneyAttributeMetadata)
                         .Any(m => m.LogicalName == attr.Key)))))
            {
                var user = db.GetEntityOrNull(userRef);
                if (user.Attributes.ContainsKey(transactioncurrencyId))
                {
                    clonedEntity.Attributes[transactioncurrencyId] = user[transactioncurrencyId];
                }
                else
                {
                    clonedEntity.Attributes[transactioncurrencyId] = Utility.GetBaseCurrency(metadata);
                }
            }

            if (!clonedEntity.Attributes.ContainsKey(exchangerate) &&
                Utility.IsValidAttribute(exchangerate, entityMetadata) &&
                clonedEntity.Attributes.ContainsKey(transactioncurrencyId))
            {
                var currencyId = clonedEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db.GetEntityOrNull(currencyId);
                clonedEntity.Attributes[exchangerate] = currency["exchangerate"];
                Utility.HandleCurrencies(metadata, db, clonedEntity);
            }

            if (Utility.IsValidAttribute("statecode", entityMetadata) &&
                Utility.IsValidAttribute("statuscode", entityMetadata))
            {
                var defaultState = 0;
                try
                {
                    var defaultStateStatus = metadata.DefaultStateStatus[clonedEntity.LogicalName];
                    if (!clonedEntity.Attributes.ContainsKey("statecode") &&
                        !clonedEntity.Attributes.ContainsKey("statuscode"))
                    {
                        clonedEntity["statecode"] = new OptionSetValue(defaultState);
                        clonedEntity["statuscode"] = new OptionSetValue(defaultStateStatus[defaultState]);
                    }
                    else
                    {
                        var statusmeta =
                            (entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as
                                StatusAttributeMetadata)
                            ?.OptionSet.Options
                            .Cast<StatusOptionMetadata>()
                            .FirstOrDefault(o =>
                                o.Value == clonedEntity.GetAttributeValue<OptionSetValue>("statuscode")?.Value);
                        if (clonedEntity.LogicalName != "opportunityclose" && // is allowed to be created inactive 
                            ((clonedEntity.Attributes.ContainsKey("statecode") &&
                              clonedEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value != defaultState) ||
                             (clonedEntity.Attributes.ContainsKey("statuscode") && statusmeta?.State != defaultState)))
                        {
                            clonedEntity["statecode"] = new OptionSetValue(defaultState);
                            clonedEntity["statuscode"] = new OptionSetValue(defaultStateStatus[defaultState]);
                        }
                        else if (!clonedEntity.Contains("statecode") ||
                                 clonedEntity.GetAttributeValue<OptionSetValue>("statecode") == null)
                        {
                            clonedEntity["statecode"] = new OptionSetValue(statusmeta.State.Value);
                        }
                        else if (!clonedEntity.Contains("statuscode") ||
                                 clonedEntity.GetAttributeValue<OptionSetValue>("statuscode") == null)
                        {
                            clonedEntity["statuscode"] = new OptionSetValue(defaultStateStatus[defaultState]);
                        }
                    }
                }
                catch (KeyNotFoundException)
                {
                    throw new KeyNotFoundException(
                        $"Unable to get default status reason for the state {defaultState.ToString()} in {clonedEntity.LogicalName} entity. " +
                        $"This might be due to unsaved default status reason changes. Please update, save, and publish the relevant status reason field on {clonedEntity.LogicalName} and generate new metadata");
                }
            }

            var owner = userRef;
            if (clonedEntity.Attributes.ContainsKey("ownerid"))
            {
                owner = clonedEntity.GetAttributeValue<EntityReference>("ownerid");
            }

            Utility.SetOwner(db, security, metadata, clonedEntity, owner);

            if (!clonedEntity.Attributes.ContainsKey("businessunitid") &&
                (clonedEntity.LogicalName == LogicalNames.SystemUser || clonedEntity.LogicalName == LogicalNames.Team))
            {
                clonedEntity["businessunitid"] = metadata.RootBusinessUnit.ToEntityReference();
            }

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit)
            {
                CheckBusinessUnitAttributes(clonedEntity, settings);
            }

            foreach (var attr in entityMetadata.Attributes
                .Where(a => (a as BooleanAttributeMetadata)?.DefaultValue != null).ToList())
            {
                if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName))
                {
                    clonedEntity[attr.LogicalName] = (attr as BooleanAttributeMetadata).DefaultValue;
                }
            }

            foreach (var attr in entityMetadata.Attributes.Where(a =>
                (a as PicklistAttributeMetadata)?.DefaultFormValue != null &&
                (a as PicklistAttributeMetadata)?.DefaultFormValue.Value != -1).ToList())
            {
                if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName))
                {
                    clonedEntity[attr.LogicalName] =
                        new OptionSetValue((attr as PicklistAttributeMetadata).DefaultFormValue.Value);
                }
            }

            if (clonedEntity.LogicalName == LogicalNames.Contact || clonedEntity.LogicalName == LogicalNames.Lead ||
                clonedEntity.LogicalName == LogicalNames.SystemUser)
            {
                Utility.SetFullName(metadata, clonedEntity);
            }

            clonedEntity.Attributes
                .Where(x => x.Value is string && x.Value != null && string.IsNullOrEmpty((string)x.Value))
                .ToList()
                .ForEach(x => clonedEntity[x.Key] = null);

            if (entityMetadata.IsActivity.GetValueOrDefault())
            {
                clonedEntity["activitytypecode"] = new OptionSetValue(entityMetadata.ObjectTypeCode.GetValueOrDefault());

                var req = new CreateRequest
                {
                    Target = clonedEntity.ToActivityPointer(entityMetadata),
                    Parameters = { [MockupExecutionContext.Key] = new MockupServiceSettings(true, settings.TriggerWorkflows, true, MockupServiceSettings.Role.SDK) }
                };
                core.Execute(req, userRef);
            }

            db.Add(clonedEntity);

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit)
            {
                security.AddRolesForBusinessUnit(db, clonedEntity.ToEntityReference());
                CreateDefaultTeamForBusinessUnit(clonedEntity, userRef);
            }

            if (entity.RelatedEntities.Count > 0)
            {
                foreach (var relatedEntities in entity.RelatedEntities)
                {
                    if (Utility.GetRelationshipMetadataDefaultNull(metadata.EntityMetadata,
                        relatedEntities.Key.SchemaName, Guid.Empty, userRef) == null)
                    {
                        throw new FaultException(
                            $"Relationship with schemaname '{relatedEntities.Key.SchemaName}' does not exist in metadata");
                    }

                    foreach (var relatedEntity in relatedEntities.Value.Entities)
                    {
                        var req = new CreateRequest()
                        {
                            Target = relatedEntity
                        };
                        core.Execute(req, userRef);
                    }

                    var associateReq = new AssociateRequest
                    {
                        Target = entity.ToEntityReference(),
                        Relationship = relatedEntities.Key,
                        RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities
                            .Select(e => e.ToEntityReference()).ToList())
                    };
                    core.Execute(associateReq, userRef);
                }
            }

            resp.Results.Add("id", clonedEntity.Id);

            return resp;
        }

        private void CheckBusinessUnitAttributes(Entity clonedEntity, MockupServiceSettings settings)
        {
            if (!clonedEntity.Attributes.ContainsKey("parentbusinessunitid"))
            {
                if (settings.ServiceRole == MockupServiceSettings.Role.UI)
                {
                    clonedEntity["parentbusinessunitid"] = metadata.RootBusinessUnit.ToEntityReference();
                }
                else
                {
                    throw new FaultException("Only one organization and one root business are allowed.");
                }
            }

            if (!clonedEntity.Attributes.ContainsKey("name"))
            {
                throw new FaultException("Business unit must have a name.");
                // The real error :  Condition for attribute 'businessunit.name': null is not a valid value for an attribute. Use 'Null' or 'NotNull' conditions instead.
            }
        }

        private void CreateDefaultTeamForBusinessUnit(Entity clonedEntity, EntityReference userRef)
        {
            var req = new CreateRequest()
            {
                Target = Utility.CreateDefaultTeam(clonedEntity, userRef)
            };
            var triggerWorkflows = core.GetMockupSettings().TriggerWorkflows ?? true;
            req.Parameters[MockupExecutionContext.Key] =
                new MockupServiceSettings(true, triggerWorkflows, true, MockupServiceSettings.Role.SDK);
            core.Execute(req, userRef);
        }
    }
}
