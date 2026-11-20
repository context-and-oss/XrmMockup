using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal class UpdateRequestHandler : RequestHandler
    {
        internal UpdateRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core,
            db, metadata, security, "Update")
        {
        }

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UpdateRequest>(orgRequest);
            var settings = MockupExecutionContext.GetSettings(request);

            var entRef = request.Target.ToEntityReferenceWithKeyAttributes();
            var entity = request.Target;
            var row = db.GetDbRow(entRef);
            var xrmEntity = row.ToEntity();

            if (!security.HasPermission(xrmEntity, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException($"Trying to update entity '{row.Table.TableName}'" +
                                         $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }

            if (core.GetMockupSettings().AppendAndAppendToPrivilegeCheck.GetValueOrDefault(true))
            {
                var references = entity.Attributes
                    .Where(x => x.Value is EntityReference && x.Key != "ownerid")
                    .ToArray();

                if (references.Any())
                {
                    if (!security.HasPermission(xrmEntity, AccessRights.AppendAccess, userRef))
                    {
                        throw new FaultException($"Trying to update entity '{xrmEntity.LogicalName}' with references" +
                            $", but the calling user with id '{userRef.Id}' does not have Append access for that entity");
                    }
                }

                foreach (var attr in references)
                {
                    var existingRef = xrmEntity.GetAttributeValue<EntityReference>(attr.Key);
                    var newRef = attr.Value as EntityReference;
                    if (existingRef?.Id == newRef.Id) continue;

                    if (settings.ServiceRole == MockupServiceSettings.Role.UI && !security.HasPermission(newRef, AccessRights.ReadAccess, userRef))
                    {
                        throw new FaultException($"Trying to update entity '{xrmEntity.LogicalName}'" +
                            $", but the calling user with id '{userRef.Id}' does not have read access for referenced entity '{newRef.LogicalName}' on attribute '{attr.Key}'");
                    }
                    if (!security.HasPermission(newRef, AccessRights.AppendToAccess, userRef))
                    {
                        throw new FaultException($"Trying to update entity '{xrmEntity.LogicalName}'" +
                            $", but the calling user with id '{userRef.Id}' does not have AppendTo access for referenced entity '{newRef.LogicalName}' on attribute '{attr.Key}'");
                    }
                }
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UpdateRequest>(orgRequest);
            var settings = MockupExecutionContext.GetSettings(request);

            if (request.Target.LogicalName is "incident"
                && !request.Parameters.ContainsKey("CloseIncidentRequestHandler")
                && request.Target.TryGetAttributeValue<OptionSetValue>("statecode", out var stateCode)
                && stateCode.Value is 1)
            {
                throw new FaultException("This message can not be used to set the state of incident to Resolved. In order to set state of incident to Resolved, use the CloseIncidentRequest message instead.");
            }

            var entRef = request.Target.ToEntityReferenceWithKeyAttributes();
            var row = db.GetDbRow(entRef);

            var entityMetadata = metadata.EntityMetadata.GetMetadata(entRef.LogicalName);

            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName != LogicalNames.Opportunity &&
                row.Table.TableName != LogicalNames.SystemUser &&
                row.GetColumn<int?>("statecode") == 1)
            {
                throw new MockupException($"Trying to update inactive '{row.Table.TableName}', which is impossible in UI");
            }

            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName == LogicalNames.Opportunity &&
                row.GetColumn<int?>("statecode") == 1)
            {
                throw new MockupException($"Trying to update closed opportunity '{row.Id}', which is impossible in UI");
            }


            if (settings.ServiceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName == LogicalNames.SystemUser &&
                row.GetColumn<bool?>("isdisabled") == true)
            {
                throw new MockupException($"Trying to update inactive systemuser '{row.Id}', which is impossible in UI");
            }

            // modify for all activites
            //if (entity.LogicalName == "activity" && dbEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 1) return;
            var xrmEntity = row.ToEntity();

            var ownerRef = request.Target.GetAttributeValue<EntityReference>("ownerid");
            if (ownerRef != null)
            {
                if (xrmEntity.Contains("ownerid") && request.Target.Contains("ownerid")
                    && xrmEntity.GetAttributeValue<EntityReference>("ownerid").Id
                    != request.Target.GetAttributeValue<EntityReference>("ownerid").Id)
                {
                    security.CheckAssignPermission(xrmEntity, ownerRef, userRef);
                }
            }

            var updEntity = request.Target.CloneEntity(row.Metadata, new ColumnSet(true));

            // TODO: The following code seems to allow illegal status transitions if statecode is not included in the update
            if (updEntity.Contains("statecode") || updEntity.Contains("statuscode"))
            {
                var defaultStateStatus = metadata.DefaultStateStatus[updEntity.LogicalName];
                var statusmeta =
                    (metadata.EntityMetadata.GetMetadata(updEntity.LogicalName)
                        .Attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as StatusAttributeMetadata)
                    ?.OptionSet.Options
                    .Cast<StatusOptionMetadata>()
                    .FirstOrDefault(o => o.Value == updEntity.GetAttributeValue<OptionSetValue>("statuscode")?.Value);

                if ((!updEntity.Contains("statecode") ||
                     updEntity.GetAttributeValue<OptionSetValue>("statecode") == null)
                    && statusmeta != null)
                {
                    updEntity["statecode"] = new OptionSetValue(statusmeta.State.Value);
                }
                else if (!updEntity.Contains("statuscode") ||
                         updEntity.GetAttributeValue<OptionSetValue>("statuscode") == null)
                {
                    var state = updEntity.GetAttributeValue<OptionSetValue>("statecode").Value;
                    updEntity["statuscode"] = new OptionSetValue(defaultStateStatus[state]);
                }
            }

            Utility.CheckStatusTransitions(row.Metadata, updEntity, xrmEntity);

            if (Utility.HasCircularReference(metadata.EntityMetadata, updEntity))
            {
                throw new FaultException(
                    $"Trying to create entity '{xrmEntity.LogicalName}', but the attributes had a circular reference");
            }

            if (updEntity.LogicalName == LogicalNames.Contact || updEntity.LogicalName == LogicalNames.Lead ||
                updEntity.LogicalName == LogicalNames.SystemUser)
            {
                foreach (var nameField in new[] { "firstname", "middlename", "lastname" })
                {
                    if (!updEntity.Contains(nameField) && xrmEntity.Contains(nameField))
                        updEntity[nameField] = xrmEntity[nameField];
                }
                Utility.SetFullName(metadata, updEntity);
            }

            updEntity.Attributes
                .Where(x => x.Value is string && x.Value != null && string.IsNullOrEmpty((string)x.Value))
                .ToList()
                .ForEach(x => updEntity[x.Key] = null);

            xrmEntity.SetAttributes(updEntity.Attributes, metadata.EntityMetadata[updEntity.LogicalName]);

            var transactioncurrencyId = "transactioncurrencyid";
            if (updEntity.LogicalName != LogicalNames.TransactionCurrency &&
                (updEntity.Attributes.ContainsKey(transactioncurrencyId) ||
                 updEntity.Attributes.Any(a =>
                     row.Metadata.Attributes.Any(m => m.LogicalName == a.Key && m is MoneyAttributeMetadata))))
            {
                if (!xrmEntity.Attributes.ContainsKey(transactioncurrencyId))
                {
                    var user = db.GetEntity(userRef);
                    if (user.Attributes.ContainsKey(transactioncurrencyId))
                    {
                        xrmEntity[transactioncurrencyId] = user[transactioncurrencyId];
                    }
                    else
                    {
                        xrmEntity[transactioncurrencyId] = core.BaseCurrency;
                    }
                }

                var currencyId = xrmEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db.GetEntity(LogicalNames.TransactionCurrency, currencyId.Id);
                xrmEntity["exchangerate"] = currency.GetAttributeValue<decimal?>("exchangerate");
                Utility.HandleCurrencies(metadata, db, xrmEntity);
            }

            if (updEntity.Attributes.ContainsKey("statecode") || updEntity.Attributes.ContainsKey("statuscode"))
            {
                Utility.HandleCurrencies(metadata, db, xrmEntity);
            }

            if (ownerRef != null)
            {
                Utility.SetOwner(db, security, metadata, xrmEntity, ownerRef);
                security.CascadeOwnerUpdate(xrmEntity, userRef, ownerRef);
            }

            if (entityMetadata.IsActivity.GetValueOrDefault())
            {
                xrmEntity["activitytypecode"] = new OptionSetValue(entityMetadata.ObjectTypeCode.GetValueOrDefault());

                var req = new UpdateRequest
                {
                    Target = xrmEntity.ToActivityPointer(entityMetadata)
                };
                core.Execute(req, userRef);
            }

            Utility.Touch(xrmEntity, row.Metadata, core.TimeOffset, userRef);

            if (request.Target.RelatedEntities.Count > 0)
            {
                foreach (var relatedEntities in request.Target.RelatedEntities)
                {
                    if (Utility.GetRelationshipMetadataDefaultNull(metadata.EntityMetadata,
                        relatedEntities.Key.SchemaName, Guid.Empty, userRef) == null)
                    {
                        throw new FaultException(
                            $"Relationship with schemaname '{relatedEntities.Key.SchemaName}' does not exist in metadata");
                    }

                    if (relatedEntities.Value.Entities.Any(e => e.Id == Guid.Empty))
                    {
                        // MS Error = System.ServiceModel.FaultException`1[[Microsoft.Xrm.Sdk.OrganizationServiceFault, Microsoft.Xrm.Sdk, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]] : Entity Id must be specified for Operation
                        throw new FaultException($"Entity Id must be specified for Operation");
                    }

                    var associateReq = new AssociateRequest
                    {
                        Target = request.Target.ToEntityReference(),
                        Relationship = relatedEntities.Key,
                        RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities
                            .Select(e => e.ToEntityReference()).ToList())
                    };
                    core.Execute(associateReq, userRef);
                }
            }

            db.Update(xrmEntity);

            return new UpdateResponse();
        }
    }
}