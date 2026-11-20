using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Serialization;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DG.Tools.XrmMockup.Internal
{
    internal static class Utility
    {
        public static IPlugin CreatePluginInstance(Type baseType)
        {
            try
            {
                return Activator.CreateInstance(baseType) as IPlugin;
            }
            catch (Exception ex) when (ex.Source == "mscorlib" || ex.Source == "System.Private.CoreLib")
            {
            }

            return null;
        }

        public static Entity CloneEntity(this Entity entity)
        {
            if (entity == null) return null;
            return entity.CloneEntity(null, null);
        }

        public static KeyAttributeCollection CloneKeyAttributes(this Entity entity)
        {
            var kac = new KeyAttributeCollection();
            foreach (var keyAttr in entity.KeyAttributes)
            {
                kac.Add(new KeyValuePair<string, object>(keyAttr.Key, CloneAttribute(keyAttr)));
            }
            return kac;
        }

        public static Entity CloneEntity(this Entity entity, EntityMetadata metadata, ColumnSet cols)
        {
            var clone = new Entity(entity.LogicalName)
            {
                Id = entity.Id
            };

            if (metadata?.PrimaryIdAttribute != null && entity.Id != Guid.Empty)
            {
                clone[metadata.PrimaryIdAttribute] = entity.Id;
            }
            clone.EntityState = entity.EntityState;
            clone.KeyAttributes = entity.CloneKeyAttributes();

            return clone.SetAttributes(entity.Attributes, metadata, cols);
        }

        public static bool IsValidAttribute(string attrName, EntityMetadata metadata)
        {
            if (metadata == null || attrName.Contains("."))
            {
                return true;
            }

            return metadata.Attributes.Any(a => a.LogicalName == attrName);
        }

        public static T SetAttributes<T>(this T entity, AttributeCollection attributes, EntityMetadata metadata) where T : Entity
        {
            return entity.SetAttributes(attributes, metadata, null);
        }

        public static T SetAttributes<T>(this T entity, AttributeCollection attributes, EntityMetadata metadata,
            ColumnSet colsToKeep) where T : Entity
        {
            if (colsToKeep != null && !colsToKeep.AllColumns)
            {
                foreach (var col in colsToKeep.Columns)
                {
                    if (!IsValidAttribute(col, metadata))
                    {
                        throw new MockupException($"'{entity.LogicalName}' entity doesn't contain attribute with Name = '{col}'");
                    }
                }

                // Can't this be optimized to not go through all attributes in the entity?
                HashSet<string> keep = new HashSet<string>(colsToKeep.Columns);
                foreach (var attr in attributes)
                {
                    if (!IsValidAttribute(attr.Key, metadata))
                    {
                        throw new MockupException($"'{entity.LogicalName}' entity doesn't contain attribute with Name = '{attr.Key}'");
                    }
                    if (keep.Contains(attr.Key)) entity.Attributes[attr.Key] = CloneAttribute(attr);
                }
            }
            else
            {
                foreach (var attr in attributes)
                {
                    if (!IsValidAttribute(attr.Key, metadata))
                    {
                        throw new FaultException($"'{entity.LogicalName}' entity doesn't contain attribute with Name = '{attr.Key}'");
                    }
                    entity.Attributes[attr.Key] = CloneAttribute(attr);
                }
            }
            return entity;
        }

        internal static void RemoveUnsettableAttributes(string actionType, EntityMetadata metadata, Entity entity)
        {
            if (entity == null) return;

            switch (actionType)
            {
                case "Create":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForCreate == false)).Select(a => a.Key).ToArray());
                    break;
                case "Retrieve":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForRead == false)).Select(a => a.Key).ToArray());
                    break;
                case "Update":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForUpdate == false)).Select(a => a.Key).ToArray());
                    break;
            }
        }

        internal static void RemoveAttribute(Entity entity, params string[] attrNames)
        {
            foreach (var attrName in attrNames)
            {
                if (entity.Attributes.ContainsKey(attrName)) entity.Attributes.Remove(attrName);
            }
        }

        internal static void CloseOpportunity(Core core, OpportunityState state, OptionSetValue status, Entity opportunityClose, EntityReference userRef)
        {
            var req = new SetStateRequest()
            {
                EntityMoniker = opportunityClose.GetAttributeValue<EntityReference>("opportunityid"),
                State = new OptionSetValue((int)state),
                Status = status
            };
            core.Execute(req, userRef);

            var create = new CreateRequest
            {
                Target = opportunityClose
            };
            core.Execute(create as OrganizationRequest, userRef);
        }

        internal static void CloseQuote(Core core, QuoteState state, OptionSetValue status, Entity quoteClose, EntityReference userRef)
        {
            var req = new SetStateRequest()
            {
                EntityMoniker = quoteClose.GetAttributeValue<EntityReference>("quoteid"),
                State = new OptionSetValue((int)state),
                Status = status
            };
            core.Execute(req, userRef);

            var create = new CreateRequest
            {
                Target = quoteClose
            };
            core.Execute(create as OrganizationRequest, userRef);
        }


        public static EntityMetadata GetMetadata(this Dictionary<string, EntityMetadata> metadata, string logicalName)
        {
            if (!metadata.ContainsKey(logicalName))
            {
                throw new FaultException($"Couldn't find metadata for the logicalname '{logicalName}'. Run the MetadataGenerator again, and check that the logicalname is specified in the config file.");
            }
            return metadata[logicalName];
        }

        internal static void SetFullName(MetadataSkeleton metadata, Entity dbEntity)
        {
            var fullnameFormat =
                (FullNameConventionCode)metadata.BaseOrganization.GetAttributeValue<OptionSetValue>("fullnameconventioncode").Value;
            var first = dbEntity.GetAttributeValue<string>("firstname");
            if (first == null) first = "";
            var middle = dbEntity.GetAttributeValue<string>("middlename");
            if (middle == null) middle = "";
            var last = dbEntity.GetAttributeValue<string>("lastname");
            if (last == null) last = "";

            string fullname = string.Empty;

            switch (fullnameFormat)
            {
                case FullNameConventionCode.FirstLast:
                    fullname = first != "" ? first + " " + last : last;
                    break;
                case FullNameConventionCode.LastFirst:
                    fullname = first != "" ? last + ", " + first : last;
                    break;
                case FullNameConventionCode.LastNoSpaceFirst:
                    fullname = first != "" ? last + first : last;
                    break;
                case FullNameConventionCode.LastSpaceFirst:
                    fullname = first != "" ? last + " " + first : last;
                    break;
                case FullNameConventionCode.FirstMiddleLast:
                    fullname = first;
                    if (middle != "") fullname += " " + middle;
                    fullname += fullname != "" ? " " + last : last;
                    break;
                case FullNameConventionCode.FirstMiddleInitialLast:
                    fullname = first;
                    if (middle != "") fullname += " " + middle[0] + ".";
                    fullname += fullname != "" ? " " + last : last;
                    break;
                case FullNameConventionCode.LastFirstMiddle:
                    fullname = last;
                    if (first != "") fullname += ", " + first;
                    if (middle != "") fullname += fullname == last ? ", " + middle : " " + middle;
                    break;
                case FullNameConventionCode.LastFirstMiddleInitial:
                    fullname = last;
                    if (first != "") fullname += ", " + first;
                    if (middle != "") fullname += fullname == last ? ", " + middle[0] + "." : " " + middle[0] + ".";
                    break;
            }

            if (string.IsNullOrEmpty(fullname))
            {
                dbEntity["fullname"] = null;
            }
            else
            {
                dbEntity["fullname"] = fullname.Trim();
            }
        }

        internal static RelationshipMetadataBase GetRelationshipMetadataDefaultNull(Dictionary<string, EntityMetadata> entityMetadata, string name, Guid metadataId, EntityReference userRef)
        {
            if (name == null && metadataId == Guid.Empty)
            {
                return null;
            }
            RelationshipMetadataBase relationshipBase;
            foreach (var meta in entityMetadata)
            {
                var manyToMany = meta.Value.ManyToManyRelationships ?? Array.Empty<ManyToManyRelationshipMetadata>();
                relationshipBase = manyToMany.FirstOrDefault(rel => rel.MetadataId == metadataId);
                if (relationshipBase != null)
                {
                    return relationshipBase;
                }
                relationshipBase = manyToMany.FirstOrDefault(rel => rel.SchemaName == name);
                if (relationshipBase != null)
                {
                    return relationshipBase;
                }
                var manyToOne = meta.Value.ManyToOneRelationships ?? Array.Empty<OneToManyRelationshipMetadata>();
                var oneToMany = meta.Value.OneToManyRelationships ?? Array.Empty<OneToManyRelationshipMetadata>();
                var oneToManyBases = manyToOne.Concat(oneToMany);
                relationshipBase = oneToManyBases.FirstOrDefault(rel => rel.MetadataId == metadataId);
                if (relationshipBase != null)
                {
                    return relationshipBase;
                }
                relationshipBase = oneToManyBases.FirstOrDefault(rel => rel.SchemaName == name);
                if (relationshipBase != null)
                {
                    return relationshipBase;
                }
            }
            return null;
        }

        internal static void CheckStatusTransitions(EntityMetadata metadata, Entity newEntity, Entity prevEntity)
        {
            if (newEntity == null || prevEntity == null) return;
            if (!newEntity.Attributes.ContainsKey("statuscode") || !prevEntity.Attributes.ContainsKey("statuscode")) return;
            if (newEntity.LogicalName != prevEntity.LogicalName || newEntity.Id != prevEntity.Id) return;

            var newValue = newEntity["statuscode"] as OptionSetValue;
            var prevValue = prevEntity["statuscode"] as OptionSetValue;

            if (metadata.EnforceStateTransitions != true) return;

            OptionMetadataCollection optionsMeta = GetStatusOptionMetadata(metadata);
            if (!optionsMeta.Any(o => o.Value == newValue.Value)) return;

            var prevValueOptionMeta = optionsMeta.FirstOrDefault(o => o.Value == prevValue.Value) as StatusOptionMetadata;
            if (prevValueOptionMeta == null) return;

            var transitions = prevValueOptionMeta.TransitionData;
            if (transitions != null && transitions != "" &&
                IsValidStatusTransition(transitions, newValue.Value)) return;

            throw new FaultException($"Trying to switch {newEntity.LogicalName} from status {prevValue.Value} to {newValue.Value}");
        }

        internal static OptionMetadata GetStateOptionMetadataFromInvariantName(string stateInvariantName, EntityMetadata entityMetadata)
        {
            var stateOptionMeta = (entityMetadata.Attributes
                .FirstOrDefault(a => a is StateAttributeMetadata) as StateAttributeMetadata)
                .OptionSet
                .Options;

            return stateOptionMeta.FirstOrDefault(o => (o as StateOptionMetadata).InvariantName == stateInvariantName);
        }

        internal static bool IsValidStatusTransition(string transitionData, int newStatusCode)
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/crm/2009/WebServices");
            var doc = XDocument.Parse(transitionData).Element(ns + "allowedtransitions");
            if (doc.Descendants(ns + "allowedtransition")
                .Where(x => x.Attribute("tostatusid").Value == newStatusCode.ToString())
                .Any())
            {
                return true;
            }
            return false;
        }

        internal static OptionMetadataCollection GetStatusOptionMetadata(EntityMetadata metadata)
        {
            return (metadata.Attributes
                .FirstOrDefault(a => a is StatusAttributeMetadata) as StatusAttributeMetadata)
                .OptionSet.Options;
        }

        internal static EntityReference GetBaseCurrency(MetadataSkeleton metadata)
        {
            return metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid");
        }

        private static int GetBaseCurrencyPrecision(MetadataSkeleton metadata)
        {
            return metadata.BaseOrganization.GetAttributeValue<int>("pricingdecimalprecision");
        }

        private static void HandleBaseCurrencies(MetadataSkeleton metadata, XrmDb db, Entity entity)
        {
            if (entity.LogicalName == LogicalNames.TransactionCurrency) return;
            var transAttr = "transactioncurrencyid";
            if (!entity.Attributes.ContainsKey(transAttr))
            {
                return;
            }
            var currency = db.GetEntity(LogicalNames.TransactionCurrency, entity.GetAttributeValue<EntityReference>(transAttr).Id);
            var attributesMetadata = metadata.EntityMetadata.GetMetadata(entity.LogicalName).Attributes.Where(a => a is MoneyAttributeMetadata);
            if (!currency.GetAttributeValue<decimal?>("exchangerate").HasValue)
            {
                throw new FaultException($"No exchangerate specified for transactioncurrency '{entity.GetAttributeValue<EntityReference>(transAttr)}'");
            }

            var baseCurrency = GetBaseCurrency(metadata);
            foreach (var attr in entity.Attributes.ToList())
            {
                if (attributesMetadata.Any(a => a.LogicalName == attr.Key) && !attr.Key.EndsWith("_base"))
                {
                    if (entity.GetAttributeValue<EntityReference>(transAttr) == baseCurrency)
                    {
                        entity[attr.Key + "_base"] = attr.Value;
                    }
                    else
                    {
                        if (attr.Value is Money money)
                        {
                            var value = money.Value / currency.GetAttributeValue<decimal?>("exchangerate").Value;
                            entity[attr.Key + "_base"] = new Money(value);
                        }
                    }
                }
            }
        }

        internal static void HandlePrecision(MetadataSkeleton metadata, XrmDb db, Entity entity)
        {
            if (entity.LogicalName == LogicalNames.TransactionCurrency) return;
            var transAttr = "transactioncurrencyid";
            if (!entity.Attributes.ContainsKey(transAttr))
            {
                return;
            }
            var currency = db.GetEntity(LogicalNames.TransactionCurrency, entity.GetAttributeValue<EntityReference>(transAttr).Id);
            var attributesMetadata = metadata.EntityMetadata.GetMetadata(entity.LogicalName).Attributes.Where(a => a is MoneyAttributeMetadata);
            var baseCurrencyPrecision = GetBaseCurrencyPrecision(metadata);
            foreach (var attr in entity.Attributes.ToList())
            {
                if (attributesMetadata.Any(a => a.LogicalName == attr.Key) && attr.Value != null)
                {
                    var attributeMetadata = attributesMetadata.First(m => m.LogicalName == attr.Key) as MoneyAttributeMetadata;
                    int? precision = null;
                    switch (attributeMetadata.PrecisionSource)
                    {
                        case 0:
                            precision = attributeMetadata.Precision;
                            break;
                        case 1:
                            precision = baseCurrencyPrecision;
                            break;
                        case 2:
                            precision = currency.GetAttributeValue<int?>("currencyprecision");
                            break;
                    }

                    if (!precision.HasValue)
                    {
                        switch (attributeMetadata.PrecisionSource)
                        {
                            case 0:
                                throw new MockupException($"No precision set for field '{attr.Key}' on entity '{entity.LogicalName}'");
                            case 1:
                                throw new MockupException($"No precision set for organization. Please check you have the correct metadata");
                            case 2:
                                throw new MockupException($"No precision set for currency. Make sure you set the precision for your own currencies");
                        }
                    }

                    var rounded = Math.Round(((Money)attr.Value).Value, precision.Value);
                    if (rounded < (decimal)attributeMetadata.MinValue.Value || rounded > (decimal)attributeMetadata.MaxValue.Value)
                    {
                        throw new FaultException($"'{attr.Key}' was outside the ranges '{attributeMetadata.MinValue}','{attributeMetadata.MaxValue}' with value '{rounded}' ");
                    }
                    entity[attr.Key] = new Money(rounded);
                }
            }
        }

        internal static void HandleCurrencies(MetadataSkeleton metadata, XrmDb db, Entity entity)
        {
            HandleBaseCurrencies(metadata, db, entity);
            HandlePrecision(metadata, db, entity);
        }

        internal static bool HasCircularReference(Dictionary<string, EntityMetadata> metadata, Entity entity)
        {
            if (!metadata.ContainsKey(entity.LogicalName)) return false;
            var attributeMetadata = metadata.GetMetadata(entity.LogicalName).Attributes;
            var references = entity.Attributes.Where(a => attributeMetadata.FirstOrDefault(m => m.LogicalName == a.Key) is LookupAttributeMetadata).Select(a => a.Value);
            foreach (var r in references)
            {
                if (r == null) continue;
                Guid guid;
                if (r is EntityReference)
                {
                    guid = (r as EntityReference).Id;
                }
                else if (r is Guid)
                {
                    guid = (Guid)r;
                }
                else if (r is EntityCollection)
                {
                    continue;
                }
                else
                {
                    throw new NotImplementedException($"{r.GetType()} not implemented in HasCircularReference");
                }
                if (guid == entity.Id) return true;
            }
            return false;
        }

        internal static void SetOwner(XrmDb db, Security dataMethods, MetadataSkeleton metadata, Entity entity, EntityReference owner)
        {
            var ownershipType = metadata.EntityMetadata.GetMetadata(entity.LogicalName).OwnershipType;

            if (!ownershipType.HasValue)
            {
                throw new MockupException($"No ownership type set for '{entity.LogicalName}'");
            }

            if (ownershipType.Value.HasFlag(OwnershipTypes.UserOwned) || ownershipType.Value.HasFlag(OwnershipTypes.TeamOwned))
            {
                if (db.GetDbRowOrNull(owner) == null)
                {
                    throw new FaultException($"Owner referenced with id '{owner.Id}' does not exist");
                }

                var prevOwner = entity.Attributes.ContainsKey("ownerid") ? entity["ownerid"] : null;
                entity["ownerid"] = owner;

                if (!dataMethods.HasPermission(entity, AccessRights.ReadAccess, owner))
                {
                    entity["ownerid"] = prevOwner;
                    throw new FaultException($"Trying to assign '{entity.LogicalName}' with id '{entity.Id}'" +
                        $" to '{owner.LogicalName}' with id '{owner.Id}', but owner does not have read access for that entity");
                }

                entity["owningbusinessunit"] = null;
                entity["owninguser"] = null;
                entity["owningteam"] = null;


                if (entity.LogicalName != LogicalNames.SystemUser && entity.LogicalName != LogicalNames.Team)
                {
                    if (owner.LogicalName == LogicalNames.SystemUser && ownershipType.Value.HasFlag(OwnershipTypes.UserOwned))
                    {
                        entity["owninguser"] = owner;
                    }
                    else if (owner.LogicalName == "team")
                    {
                        entity["owningteam"] = owner;
                    }
                    else
                    {
                        throw new MockupException($"Trying to give owner to {owner.LogicalName} but ownershiptype is {ownershipType.ToString()}");
                    }
                    entity["owningbusinessunit"] = GetBusinessUnit(db, owner);
                }
            }
        }

        internal static EntityReference GetBusinessUnit(XrmDb db, EntityReference owner)
        {
            var user = db.GetEntityOrNull(owner);
            if (user == null)
            {
                return null;
            }
            var buRef = user.GetAttributeValue<EntityReference>("businessunitid");
            var bu = db.GetEntityOrNull(buRef);
            if (bu == null)
            {
                return null;
            }
            buRef.Name = bu.GetAttributeValue<string>("name");
            return buRef;
        }

        /*
         * Possible error with cast to Entity here, was (T) before
         * 
         * */
        public static Entity MakeProxyTypeEntity(Entity entity, Type ty)
        {
            var method = typeof(Entity).GetMethod("ToEntity").MakeGenericMethod(ty);
            return (Entity)method.Invoke(entity, null);
        }

        public static IEnumerable<KeyValuePair<string, object>> GetProperties(object obj)
        {
            var ty = obj.GetType();
            var props = ty.GetProperties().Where(x => x.DeclaringType.UnderlyingSystemType == ty).ToList();
            return props.Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(obj)));
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        public static T? GetOptionSetValue<T>(this Entity entity, string attributeName) where T : struct, IComparable, IConvertible, IFormattable
        {
            var optionSet = entity.GetAttributeValue<OptionSetValue>(attributeName);
            if (optionSet != null)
            {
                return (T)Enum.ToObject(typeof(T), optionSet.Value);
            }
            else
            {
                return null;
            }
        }

        public static List<AccessRights> GetAccessRights(this AccessRights mask)
        {
            var rights = new List<AccessRights>();
            foreach (AccessRights right in Enum.GetValues(typeof(AccessRights)))
            {
                if (right == AccessRights.None) continue;
                if (mask.HasFlag(right))
                {
                    rights.Add(right);
                }
            }
            return rights;
        }

        internal static void Touch(Entity dbEntity, EntityMetadata metadata, TimeSpan timeOffset, EntityReference user)
        {
            if (IsValidAttribute("modifiedon", metadata) && IsValidAttribute("modifiedby", metadata))
            {
                dbEntity["modifiedon"] = DateTime.UtcNow.Add(timeOffset);
                dbEntity["modifiedby"] = user;
            }
        }

        internal static void PopulateEntityReferenceNames(Entity entity, XrmDb db)
        {
            foreach (var attr in entity.Attributes)
            {
                if (attr.Value is EntityReference eRef)
                {
                    var row = db.GetDbRowOrNull(eRef);
                    if (row != null)
                    {
                        var nameAttr = row.Metadata.PrimaryNameAttribute;
                        eRef.Name = row.GetColumn<string>(nameAttr);
                    }
                }
                else if (attr.Value is AliasedValue aliasedValue && aliasedValue.Value is EntityReference aliasedRef)
                {
                    var row = db.GetDbRowOrNull(aliasedRef);
                    if (row != null)
                    {
                        var nameAttr = row.Metadata.PrimaryNameAttribute;
                        aliasedRef.Name = row.GetColumn<string>(nameAttr);
                    }
                }
            }
        }

        internal static object GetComparableAttribute(object attribute)
        {
            if (attribute is Money money)
            {
                return money.Value;
            }
            if (attribute is EntityReference eRef)
            {
                return eRef.Name;
            }
            if (attribute is OptionSetValue osv)
            {
                return osv.Value;
            }
            return attribute;
        }

        internal static RelationshipMetadataBase GetRelatedEntityMetadata(Dictionary<string, EntityMetadata> metadata, string entityType, string relationshipName)
        {
            if (!metadata.ContainsKey(entityType))
            {
                return null;
            }
            var oneToMany = metadata.GetMetadata(entityType).OneToManyRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (oneToMany != null)
            {
                return oneToMany;
            }

            var manyToOne = metadata.GetMetadata(entityType).ManyToOneRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (manyToOne != null)
            {
                return manyToOne;
            }

            var manyToMany = metadata.GetMetadata(entityType).ManyToManyRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (manyToMany != null)
            {
                return manyToMany;
            }

            return null;
        }


        internal static MetadataSkeleton GetMetadata(string folderLocation)
        {
            var pathToMetadata = Path.Combine(folderLocation, "Metadata.xml");
            if (!File.Exists(pathToMetadata))
            {
                throw new ArgumentException($"Could not find metadata file at '{pathToMetadata}'." +
                    " Be sure to run Metadata/GetMetadata.cmd to generate it after setting it up in Metadata/Config.fsx.");
            }

            //check for any additional metadata files
            var metaDataFiles = Directory.GetFiles(folderLocation, "*Metadata.xml");

            var master = new MetadataSkeleton();
            var serializer = new DataContractSerializer(typeof(MetadataSkeleton));
            using (var stream = new FileStream(pathToMetadata, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                master = (MetadataSkeleton)serializer.ReadObject(stream);
            }

            foreach (var file in metaDataFiles.Where(x => Path.GetFileName(x) != Path.GetFileName(pathToMetadata)))
            {
                serializer = new DataContractSerializer(typeof(MetadataSkeleton));
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    master.Merge((MetadataSkeleton)serializer.ReadObject(stream));
                }
            }

            return master;
        }

        internal static List<Entity> GetWorkflows(string folderLocation)
        {
            var pathToWorkflows = Path.Combine(folderLocation, "Workflows");
            if (!Directory.Exists(pathToWorkflows))
            {
                return new List<Entity>();
            }

            var files = Directory.GetFiles(pathToWorkflows, "*.xml");
            return files.Select(GetWorkflow).ToList();
        }

        internal static Entity GetWorkflow(string path)
        {
            var serializer = new DataContractSerializer(typeof(Entity));
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (Entity)serializer.ReadObject(stream);
            }
        }

        internal static List<SecurityRole> GetSecurityRoles(string folderLocation)
        {
            var pathToSecurity = Path.Combine(folderLocation, "SecurityRoles");

            if (!Directory.Exists(pathToSecurity))
            {
                return new List<SecurityRole>();
            }

            var files = Directory.GetFiles(pathToSecurity, "*.xml");

            var serializer = new DataContractSerializer(typeof(SecurityRole));
            return files.Select(file =>
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return (SecurityRole)serializer.ReadObject(stream);
                }
            }).ToList();
        }

        internal static Guid GetGuidFromReference(object reference)
        {
            return reference is EntityReference ? (reference as EntityReference).Id : (Guid)reference;
        }

        internal static EntityReference ToEntityReferenceWithKeyAttributes(this Entity entity)
        {
            var reference = entity.ToEntityReference();
            reference.KeyAttributes = entity.KeyAttributes;
            return reference;
        }

        internal static string ToPrettyString(this KeyAttributeCollection keys)
        {
            return "(" + string.Join(", ", keys.Select(x => $"{x.Key}:{x.Value}")) + ")";
        }

        internal static Entity ToActivityPointer(this Entity entity, EntityMetadata entityMetadata)
        {
            if (!entityMetadata.IsActivity.GetValueOrDefault()) return null;

            var pointer = new Entity("activitypointer")
            {
                Id = entity.Id
            };
            pointer["activityid"] = entity.Id;
            pointer["ownerid"] = entity.GetAttributeValue<EntityReference>("ownerid");
            pointer["activitytypecode"] = entity.GetAttributeValue<OptionSetValue>("activitytypecode");
            pointer["actualdurationminutes"] = entity.GetAttributeValue<int>("actualdurationminutes");
            pointer["actualend"] = entity.GetAttributeValue<DateTime>("actualend");
            pointer["actualstart"] = entity.GetAttributeValue<DateTime>("actualstart");
            pointer["description"] = entity.GetAttributeValue<string>("description");
            pointer["isbilled"] = entity.GetAttributeValue<bool>("isbilled");
            pointer["isregularactivity"] = entity.GetAttributeValue<bool>("isregularactivity");
            pointer["isworkflowcreated"] = entity.GetAttributeValue<bool>("isworkflowcreated");
            pointer["prioritycode"] = entity.GetAttributeValue<OptionSetValue>("prioritycode");
            pointer["regardingobjectid"] = entity.GetAttributeValue<EntityReference>("regardingobjectid");
            pointer["scheduleddurationminutes"] = entity.GetAttributeValue<int>("scheduleddurationminutes");
            pointer["scheduledend"] = entity.GetAttributeValue<DateTime>("scheduledend");
            pointer["scheduledstart"] = entity.GetAttributeValue<DateTime>("scheduledstart");
            pointer["subject"] = entity.GetAttributeValue<string>("subject");
            pointer["senton"] = entity.GetAttributeValue<DateTime>("senton");
            pointer["deliveryprioritycode"] = entity.GetAttributeValue<OptionSetValue>("deliveryprioritycode");


            switch (entity.GetAttributeValue<OptionSetValue>("statecode").Value)
            {
                case 1:
                    pointer["statecode"] = new OptionSetValue(1);
                    pointer["statuscode"] = new OptionSetValue(2);
                    break;

                case 2:
                    pointer["statecode"] = new OptionSetValue(2);
                    pointer["statuscode"] = new OptionSetValue(3);
                    break;

                case 3:
                    pointer["statecode"] = new OptionSetValue(3);
                    pointer["statuscode"] = new OptionSetValue(4);
                    break;

                default:
                    pointer["statecode"] = new OptionSetValue(0);
                    pointer["statuscode"] = new OptionSetValue(1);
                    break;
            }
            return pointer;
        }

        private static bool IsValidForFormattedValues(AttributeMetadata attributeMetadata)
        {
            return
                attributeMetadata is PicklistAttributeMetadata ||
                attributeMetadata is BooleanAttributeMetadata ||
                attributeMetadata is MoneyAttributeMetadata ||
                attributeMetadata is LookupAttributeMetadata ||
                attributeMetadata is IntegerAttributeMetadata ||
                attributeMetadata is DateTimeAttributeMetadata ||
                attributeMetadata is MemoAttributeMetadata ||
                attributeMetadata is DoubleAttributeMetadata ||
                attributeMetadata is DecimalAttributeMetadata;
        }

        private static string GetFormattedValueLabel(XrmDb db, AttributeMetadata metadataAtt, object value, Entity entity, string attrName)
        {
            if (metadataAtt is PicklistAttributeMetadata picklistAttributeMetadata)
            {
                var optionset = picklistAttributeMetadata.OptionSet.Options
                    .FirstOrDefault(opt => opt.Value == (value as OptionSetValue).Value);

                return optionset == null
                    ? throw new MockupException($"Value '{(value as OptionSetValue).Value}' for attribute '{attrName}' on entity '{entity.LogicalName}' not found in OptionSet '{picklistAttributeMetadata.OptionSet.Name}'")
                    : optionset.Label.UserLocalizedLabel.Label;
            }

            if (metadataAtt is BooleanAttributeMetadata booleanAttributeMetadata)
            {
                var booleanOptions = booleanAttributeMetadata.OptionSet;
                var label = (bool)value ? booleanOptions.TrueOption.Label : booleanOptions.FalseOption.Label;
                return label.UserLocalizedLabel.Label;
            }

            if (metadataAtt is MoneyAttributeMetadata)
            {
                var currencysymbol =
                    db.GetEntity(
                        db.GetEntity(entity.ToEntityReference())
                        .GetAttributeValue<EntityReference>("transactioncurrencyid"))
                    .GetAttributeValue<string>("currencysymbol");

                return currencysymbol + (value as Money).Value.ToString();
            }

            if (metadataAtt is LookupAttributeMetadata)
            {
                try
                {
                    return (value as EntityReference).Name;
                }
                catch (NullReferenceException e)
                {
                    Console.WriteLine("No lookup entity exists: " + e.Message);
                }
            }

            if (metadataAtt is IntegerAttributeMetadata ||
                metadataAtt is DateTimeAttributeMetadata ||
                metadataAtt is MemoAttributeMetadata ||
                metadataAtt is DoubleAttributeMetadata ||
                metadataAtt is DecimalAttributeMetadata)
            {
                return value.ToString();
            }

            return null;
        }

        internal static void SetFormattedValues(XrmDb db, Entity entity, EntityMetadata metadata)
        {
            var validMetadata = metadata.Attributes.Where(a => IsValidForFormattedValues(a));

            var formattedValues = new ConcurrentBag<KeyValuePair<string, string>>();

            Parallel.ForEach(entity.Attributes.Where(x => x.Value != null), a =>
             {
                 var metadataAtt = validMetadata.Where(m => m.LogicalName == a.Key).FirstOrDefault();
                 var formattedValuePair = new KeyValuePair<string, string>(a.Key, GetFormattedValueLabel(db, metadataAtt, a.Value, entity, a.Key));
                 if (formattedValuePair.Value != null)
                 {
                     formattedValues.Add(formattedValuePair);
                 }
             });

            if (formattedValues.Count > 0)
            {
                entity.FormattedValues.AddRange(formattedValues);
            }
        }

        public static QueryExpression QueryByAttributeToQueryExpression(QueryByAttribute query)
        {
            var ret = new QueryExpression
            {
                EntityName = query.EntityName,
                TopCount = query.TopCount,
                PageInfo = query.PageInfo,
                ColumnSet = query.ColumnSet,
            };

            for (var i = 0; i <= query.Attributes.Count - 1; i++)
            {
                ret.Criteria.Conditions.Add(new ConditionExpression(query.Attributes[i], ConditionOperator.Equal, query.Values[i]));
            }

            return ret;
        }

        public static Entity CreateDefaultTeam(Entity rootBusinessUnit, EntityReference useReference)
        {
            var defaultTeam = new Entity(LogicalNames.Team);
            defaultTeam["name"] = rootBusinessUnit.Attributes["name"];
            defaultTeam["teamtype"] = new OptionSetValue(0);
            defaultTeam["isdefault"] = true;
            defaultTeam["description"] = "Default team for the parent business unit. The name and membership for default team are inherited from their parent business unit.";
            defaultTeam["administratorid"] = useReference;
            defaultTeam["businessunitid"] = rootBusinessUnit.ToEntityReference();

            return defaultTeam;
        }

        private static object CloneAttribute(KeyValuePair<string, object> attribute)
        {
            if (attribute.Value == null) return null;

            switch (attribute.Value.GetType().Name)
            {
                case "Money":
                    var m = attribute.Value as Money;
                    return new Money(m.Value);
                case "EntityReference":
                    var er = attribute.Value as EntityReference;
                    var newEr = new EntityReference(er.LogicalName, er.Id);
                    newEr.Name = er.Name;
                    return newEr;
                case "OptionSetValue":
                    var os = attribute.Value as OptionSetValue;
                    return new OptionSetValue(os.Value);
                case "OptionSetValueCollection":
                    var osc = attribute.Value as OptionSetValueCollection;
                    return new OptionSetValueCollection(osc);
                default:
                    return attribute.Value;
            }
        }

        public static TableColumnDTO ConvertValueToSerializableDTO(object colToSerialize)
        {
            var jsonColObj = new TableColumnDTO
            {
                Type = colToSerialize?.GetType().AssemblyQualifiedName,
            };

            if (jsonColObj.Type == null)
            {
                return null;
            }

            if (colToSerialize is DbRow)
            {
                //To avoid recursion we serialize dbrows as references
                var dbRow = (DbRow)colToSerialize;
                var refObj = new EntityReferenceDTO
                {
                    Id = dbRow.Id,
                    LogicalName = dbRow.Table.TableName
                };
                jsonColObj.Value = JsonSerializer.Serialize(refObj);
            }
            else if (colToSerialize is OptionSetValueCollection)
            {
                var typedCollection = (OptionSetValueCollection)colToSerialize;
                var dto = new OptionSetCollectionDTO
                {
                    Values = typedCollection.Select(x => x.Value).ToList(),
                };
                jsonColObj.Value = JsonSerializer.Serialize(dto);
            }
            else if (colToSerialize is BooleanManagedProperty)
            {
                // BooleanManagedProperty has ExtensionDataObject that can't be serialized
                // Serialize just the essential properties
                var mp = (BooleanManagedProperty)colToSerialize;
                var dto = new BooleanManagedPropertyDTO
                {
                    Value = mp.Value,
                    CanBeChanged = mp.CanBeChanged,
                    ManagedPropertyLogicalName = mp.ManagedPropertyLogicalName
                };
                jsonColObj.Value = JsonSerializer.Serialize(dto);
            }
            else
            {
                jsonColObj.Value = JsonSerializer.Serialize(colToSerialize);
            }
            return jsonColObj;
        }

        public static object ConvertValueFromSerializableDTO(TableColumnDTO colToSerialize)
        {
            if (colToSerialize == null) return null;
            var type = Type.GetType(colToSerialize.Type);
            if (type == typeof(DbRow))
            {
                var node = JsonNode.Parse(colToSerialize.Value);
                var typed = (EntityReferenceDTO)node.Deserialize(typeof(EntityReferenceDTO));
                var tmpTable = new DbTable(new EntityMetadata { LogicalName = typed.LogicalName });
                return new DbRow(tmpTable, typed.Id, null);
            }
            else if (type == typeof(OptionSetValueCollection))
            {
                var node = JsonNode.Parse(colToSerialize.Value);
                var typed = (OptionSetCollectionDTO)node.Deserialize(typeof(OptionSetCollectionDTO));
                var newCollection = new OptionSetValueCollection(typed.Values.Select(x => new OptionSetValue(x)).ToList());
                return newCollection;
            }
            else if (type == typeof(BooleanManagedProperty))
            {
                var node = JsonNode.Parse(colToSerialize.Value);
                var typed = (BooleanManagedPropertyDTO)node.Deserialize(typeof(BooleanManagedPropertyDTO));
                // ManagedPropertyLogicalName is read-only and not settable, but Value and CanBeChanged are what matters
                return new BooleanManagedProperty(typed.Value)
                {
                    CanBeChanged = typed.CanBeChanged
                };
            }
            else
            {
                var node = JsonNode.Parse(colToSerialize.Value);
                var typed = node.Deserialize(type);
                return typed;
            }
        }

        private static readonly string ZIP_STRING_FILENAME = "data.txt";
        public static void ZipCompressString(string filenameWithoutExtenstion, string json)
        {
            var zipName = Path.ChangeExtension(filenameWithoutExtenstion, ".zip");

            if (File.Exists(zipName)) File.Delete(zipName);

            using (var zip = ZipFile.Open(zipName, ZipArchiveMode.Create))
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json ?? "")))
            {
                var zipEntry = zip.CreateEntry(ZIP_STRING_FILENAME);
                zipEntry.LastWriteTime = DateTimeOffset.Now;
                using (var entryStream = zipEntry.Open())
                    ms.CopyTo(entryStream);
            }
        }

        public static string ZipUncompressString(string filenameWithoutExtenstion)
        {
            var zipName = Path.ChangeExtension(filenameWithoutExtenstion, ".zip");
            if (!File.Exists(zipName)) throw new FileNotFoundException(zipName);

            using (var zip = ZipFile.Open(zipName, ZipArchiveMode.Read))
            using (var ms = new MemoryStream())
            {
                var zipEntry = zip.GetEntry(ZIP_STRING_FILENAME);
                using (var entryStream = zipEntry.Open())
                    entryStream.CopyTo(ms);
                var json = Encoding.UTF8.GetString(ms.ToArray());
                return json;
            }
        }

        public static string GetFormulaDefinition(AttributeMetadata field, SourceType sourceType)
        {
            if (field.SourceType != (int)sourceType)
            {
                return null;
            }

            switch (field)
            {
                case BooleanAttributeMetadata booleanField:
                    return booleanField.FormulaDefinition;
                case DateTimeAttributeMetadata dateTimeField:
                    return dateTimeField.FormulaDefinition;
                case DecimalAttributeMetadata decimalField:
                    return decimalField.FormulaDefinition;
                case IntegerAttributeMetadata integerField:
                    return integerField.FormulaDefinition;
                case MoneyAttributeMetadata moneyField:
                    return moneyField.FormulaDefinition;
                case PicklistAttributeMetadata picklistField:
                    return picklistField.FormulaDefinition;
                case StringAttributeMetadata stringField:
                    return stringField.FormulaDefinition;
            }

            return null;
        }
    }

    internal static class LogicalNames
    {
        public const string TransactionCurrency = "transactioncurrency";
        public const string BusinessUnit = "businessunit";
        public const string SystemUser = "systemuser";
        public const string Workflow = "workflow";
        public const string Team = "team";
        public const string Contact = "contact";
        public const string Lead = "lead";
        public const string Opportunity = "opportunity";
        public const string TeamMembership = "teammembership";
        public const string Incident = "incident";
        public const string Annotation = "annotation";
        public const string Quote = "quote";
        public const string QuoteDetail = "quotedetail";
        public const string QuoteClose = "quoteclose";
    }

    [DataContract()]
    internal enum workflow_runas
    {

        [EnumMember()]
        Owner = 0,

        [EnumMember()]
        CallingUser = 1,
    }

    [DataContract()]
    internal enum workflow_stage
    {

        [EnumMember()]
        Preoperation = 20,

        [EnumMember()]
        Postoperation = 40,
    }

    [DataContract()]
    internal enum Workflow_Type
    {

        [EnumMember()]
        Definition = 1,

        [EnumMember()]
        Activation = 2,

        [EnumMember()]
        Template = 3,
    }

    [DataContract()]
    internal enum componentstate
    {

        [EnumMember()]
        Published = 0,

        [EnumMember()]
        Unpublished = 1,

        [EnumMember()]
        Deleted = 2,

        [EnumMember()]
        DeletedUnpublished = 3,
    }

    [DataContract()]
    internal enum Workflow_Scope
    {

        [EnumMember()]
        User = 1,

        [EnumMember()]
        BusinessUnit = 2,

        [EnumMember()]
        ParentChildBusinessUnits = 3,

        [EnumMember()]
        Organization = 4,
    }

    [DataContract()]
    internal enum Workflow_Mode
    {

        [EnumMember()]
        Background = 0,

        [EnumMember()]
        Realtime = 1,
    }

    [DataContract()]
    internal enum Workflow_BusinessProcessType
    {

        [EnumMember()]
        BusinessFlow = 0,

        [EnumMember()]
        TaskFlow = 1,
    }

    [DataContract()]
    internal enum Workflow_Category
    {

        [EnumMember()]
        Workflow = 0,

        [EnumMember()]
        Dialog = 1,

        [EnumMember()]
        BusinessRule = 2,

        [EnumMember()]
        Action = 3,

        [EnumMember()]
        BusinessProcessFlow = 4,
    }

    [DataContract()]
    internal enum WorkflowState
    {

        [EnumMember()]
        Draft = 0,

        [EnumMember()]
        Activated = 1,
    }

    [DataContract()]
    internal enum Workflow_StatusCode
    {

        [EnumMember()]
        Draft = 1,

        [EnumMember()]
        Activated = 2,
    }

    [DataContract()]
    internal enum OpportunityState
    {

        [EnumMember()]
        Open = 0,

        [EnumMember()]
        Won = 1,

        [EnumMember()]
        Lost = 2,
    }

    [DataContract()]
    internal enum Opportunity_StatusCode
    {

        [EnumMember()]
        InProgress = 1,

        [EnumMember()]
        OnHold = 2,

        [EnumMember()]
        Won = 3,

        [EnumMember()]
        Canceled = 4,

        [EnumMember()]
        OutSold = 5,
    }

    [DataContract()]
    internal enum QuoteState
    {

        [EnumMember()]
        Draft = 0,

        [EnumMember()]
        Active = 1,

        [EnumMember()]
        Won = 2,

        [EnumMember()]
        Closed = 3,
    }

    [DataContract()]
    public enum Quote_StatusCode
    {

        [EnumMember()]
        InProgress_2 = 1,

        [EnumMember()]
        InProgress = 2,

        [EnumMember()]
        Open = 3,

        [EnumMember()]
        Won = 4,

        [EnumMember()]
        Lost = 5,

        [EnumMember()]
        Canceled = 6,

        [EnumMember()]
        Revised = 7,
    }

}
