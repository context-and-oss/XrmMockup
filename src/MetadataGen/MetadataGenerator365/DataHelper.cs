using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

namespace DG.Tools.XrmMockup.Metadata
{
    internal class DataHelper
    {
        internal const string PRIVILEGE = "privilege";
        internal const string ROLEPRIVILEGES = "roleprivileges";
        internal const string ROLE = "role";
        internal const string PRIVILEGE_OTC = "privilegeobjecttypecodes";
        internal const string PRIVILEGE_ROLEPRIVILEGE_ALIAS = "privrp";
        internal const string ROLEPRIVILEGE_ROLE_ALIAS = "rpr";
        internal const string PRIVILEGE_POTCH = "privpotch";

        private IOrganizationService service;
        private HashSet<string> EntityLogicalNames;
        private string[] SolutionNames;

        public DataHelper(IOrganizationService service, string entitiesString, string solutionsString, bool fetchFromAssemblies)
        {
            this.service = service;
            this.SolutionNames = solutionsString.Split(',').Select(x => x.Trim()).ToArray();
            this.EntityLogicalNames = new HashSet<string>();

            // Add entites from assemblies
            if (fetchFromAssemblies)
                this.EntityLogicalNames = GetLogicalNames(AssemblyGetter.GetAssembliesInBuildPath());

            // Add default entities
            var defaultEntities = new string[] { "businessunit", "systemuser", "transactioncurrency", "role", "systemuserroles", "team", "teamroles", "activitypointer", "roletemplate", "fileattachment" };
            foreach (var logicalName in defaultEntities)
            {
                this.EntityLogicalNames.Add(logicalName);
            }

            // Add specified entities
            if (!String.IsNullOrWhiteSpace(entitiesString))
            {
                foreach (var logicalName in entitiesString.Split(','))
                {
                    this.EntityLogicalNames.Add(logicalName.Trim());
                }
            }
        }

        public MetadataSkeleton GetMetadata()
        {
            var skeleton = new MetadataSkeleton();

            Console.WriteLine($"Getting entity metadata");
            skeleton.EntityMetadata = new SortedDictionary<string, EntityMetadata>(GetEntityMetadata(this.SolutionNames, this.EntityLogicalNames.ToArray()).ToDictionary(m => m.LogicalName, m => m));

            Console.WriteLine("Getting currencies");
            skeleton.Currencies = GetCurrencies();

            Console.WriteLine("Getting plugins");
            skeleton.Plugins = GetPlugins(this.SolutionNames);

            Console.WriteLine("Getting organization");
            skeleton.BaseOrganization = GetBaseOrganization();

            Console.WriteLine("Getting root businessunit");
            var rootBU = GetBusinessUnits().First(bu => !bu.Attributes.ContainsKey("parentbusinessunitid"));
            skeleton.RootBusinessUnit = rootBU.ToEntity<Entity>();

            Console.WriteLine("Getting All Optionsets");
            skeleton.OptionSets = RetrieveAllOptionSets();

            Console.WriteLine("Getting Default state and status");
            skeleton.DefaultStateStatus = GetDefaultStateAndStatus();

            return skeleton;
        }

        public OptionSetMetadataBase[] RetrieveAllOptionSets()
        {
            var optionSetRequest = new RetrieveAllOptionSetsRequest
            {
                RetrieveAsIfPublished = true
            };
            var results = service.Execute(optionSetRequest) as RetrieveAllOptionSetsResponse;
            return results.OptionSetMetadata;
        }

        private IEnumerable<EntityMetadata> GetEntityMetadata(string[] solutions, string[] entities)
        {
            var entityComponentId = solutions.SelectMany(GetEntityComponentIdsFromSolution).Distinct();
            var solutionRequests = entityComponentId.Select(GetEntityMetadataFromIdRequest);
            var specificEntityRequest = entities.Select(lname => GetEntityMetadataRequest(lname));

            var entitymetadata = GetEntityMetadataBulk(solutionRequests.Concat(specificEntityRequest));
            var logicalNamesSet = new HashSet<string>(entitymetadata.Select(x => x.LogicalName));
            var relationMetadata = GetRelationMetadata(entitymetadata);
            var entityMetadata = entitymetadata.Concat(relationMetadata);

            if (logicalNamesSet.Contains("activityparty") && NeedActivityParty(entityMetadata))
            {
                var resp = (RetrieveEntityResponse)service.Execute(GetEntityMetadataRequest("activityparty"));
                entityMetadata = entityMetadata.Concat(new EntityMetadata[] { resp.EntityMetadata });
            }

            entityMetadata = entityMetadata.GroupBy(x => x.LogicalName).Select(x => x.First());

            this.EntityLogicalNames = new HashSet<string>(entityMetadata.Select(meta => meta.LogicalName));
            Console.WriteLine($"Retrieved entity metadata for: {String.Join(", ", this.EntityLogicalNames.ToArray())}");

            return entityMetadata;
        }

        private List<MetaPlugin> GetPlugins(string[] solutions)
        {
            var plugins = new List<MetaPlugin>();

            if (solutions == null || solutions.Length == 0)
            {
                return plugins;
            }

            var pluginSteps = GetPluginSteps(solutions);

            //get the images
            var images = GetImages(solutions);

            foreach (var pluginStep in pluginSteps.Entities)
            {
                var metaPlugin = new MetaPlugin()
                {
                    Name = pluginStep.GetAttributeValue<string>("name"),
                    Rank = pluginStep.GetAttributeValue<int>("rank"),
                    FilteredAttributes = pluginStep.GetAttributeValue<string>("filteringattributes"),
                    Mode = pluginStep.GetAttributeValue<OptionSetValue>("mode").Value,
                    Stage = pluginStep.GetAttributeValue<OptionSetValue>("stage").Value,
                    MessageName = pluginStep.GetAttributeValue<EntityReference>("sdkmessageid").Name,
                    AssemblyName = pluginStep.GetAttributeValue<EntityReference>("eventhandler").Name,
                    PluginTypeAssemblyName = pluginStep.GetAttributeValue<AliasedValue>("plugintype.assemblyname").Value.ToString(),
                    ImpersonatingUserId = pluginStep.Contains("impersonatinguserid") ? pluginStep.GetAttributeValue<EntityReference>("impersonatinguserid").Id : (Guid?)null,
                    PrimaryEntity = pluginStep.GetAttributeValue<AliasedValue>("sdkmessagefilter.primaryobjecttypecode")?.Value as string ?? "",  // In case of AnyEntity use ""
                    AsyncAutoDelete = pluginStep.Contains("asyncautodelete") && pluginStep.GetAttributeValue<bool>("asyncautodelete"),
                    Images = images.Entities
                        .Where(x => x.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid").Id == pluginStep.Id)
                        .Select(x => new MetaImage
                        {
                            Attributes = x.GetAttributeValue<string>("attributes"),
                            EntityAlias = x.GetAttributeValue<string>("entityalias"),
                            ImageType = x.GetAttributeValue<OptionSetValue>("imagetype").Value,
                            Name = x.GetAttributeValue<string>("name")
                        })
                        .ToList()
                };
                if (metaPlugin.PrimaryEntity == "none")
                {
                    metaPlugin.PrimaryEntity = "";
                }
                plugins.Add(metaPlugin);
            }
            return plugins;
        }

        private EntityCollection GetPluginSteps(string[] solutions)
        {
            var pluginQuery = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("eventhandler", "stage", "mode", "rank", "sdkmessageid", "filteringattributes", "name", "impersonatinguserid", "sdkmessageprocessingstepid"),
                Criteria = new FilterExpression(),
                Distinct = true,
            };
            pluginQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var sdkMessageFilterQuery = new LinkEntity("sdkmessageprocessingstep", "sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter)
            {
                Columns = new ColumnSet("primaryobjecttypecode"),
                EntityAlias = "sdkmessagefilter",
                LinkCriteria = new FilterExpression()
            };
            pluginQuery.LinkEntities.Add(sdkMessageFilterQuery);

            var pluginTypeFilterQuery = new LinkEntity("sdkmessageprocessingstep", "plugintype", "plugintypeid", "plugintypeid", JoinOperator.LeftOuter)
            {
                Columns = new ColumnSet("assemblyname"),
                EntityAlias = "plugintype",
                LinkCriteria = new FilterExpression()
            };
            pluginQuery.LinkEntities.Add(pluginTypeFilterQuery);

            var solutionComponentQuery = new LinkEntity("sdkmessageprocessingstep", "solutioncomponent", "sdkmessageprocessingstepid", "objectid", JoinOperator.Inner)
            {
                Columns = new ColumnSet(),
                LinkCriteria = new FilterExpression()
            };
            pluginQuery.LinkEntities.Add(solutionComponentQuery);

            var solutionQuery = new LinkEntity("solutioncomponent", "solution", "solutionid", "solutionid", JoinOperator.Inner)
            {
                Columns = new ColumnSet(),
                LinkCriteria = new FilterExpression()
            };
            solutionQuery.LinkCriteria.AddCondition("uniquename", ConditionOperator.In, solutions);
            solutionComponentQuery.LinkEntities.Add(solutionQuery);

            return service.RetrieveMultiple(pluginQuery);
        }

        private EntityCollection GetImages(string[] solutions)
        {
            var imagesQuery = new QueryExpression
            {
                EntityName = "sdkmessageprocessingstepimage",
                ColumnSet = new ColumnSet("attributes", "entityalias", "name", "imagetype", "sdkmessageprocessingstepid"),
                LinkEntities =
                {
                    new LinkEntity("sdkmessageprocessingstepimage", "sdkmessageprocessingstep", "sdkmessageprocessingstepid", "sdkmessageprocessingstepid", JoinOperator.Inner)
                    {
                        LinkEntities =
                        {
                            new LinkEntity("sdkmessageprocessingstep", "solutioncomponent", "sdkmessageprocessingstepid", "objectid", JoinOperator.Inner)
                            {
                                LinkEntities =
                                {
                                    new LinkEntity("solutioncomponent", "solution", "solutionid", "solutionid", JoinOperator.Inner)
                                    {
                                        LinkCriteria =
                                        {
                                            Conditions =
                                            {
                                                new ConditionExpression("uniquename", ConditionOperator.In, solutions)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var images = service.RetrieveMultiple(imagesQuery);
            return images;
        }

        private List<Entity> GetCurrencies()
        {
            var query = new QueryExpression("transactioncurrency")
            {
                ColumnSet = new ColumnSet(true)
            };
            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>()).ToList();
        }

        private Entity GetBaseOrganization()
        {
            var baseOrganizationId = (service.Execute(new WhoAmIRequest()) as WhoAmIResponse).OrganizationId;
            var query = new QueryExpression("organization")
            {
                ColumnSet = new ColumnSet(true)
            };
            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>())
                .First(e => e.Id.Equals(baseOrganizationId));
        }

        internal IEnumerable<Entity> GetBusinessUnits()
        {
            var query = new QueryExpression("businessunit")
            {
                ColumnSet = new ColumnSet(true)
            };
            return service.RetrieveMultiple(query).Entities;
        }

        private IEnumerable<Guid> GetEntityComponentIdsFromSolution(string solutionName)
        {
            var solutionFilter = new Dictionary<string, object>() { { "uniquename", solutionName } };
            var solutionColumns = new string[] { "solutionid", "uniquename" };
            var solutions = GetEntities("solution", solutionColumns, solutionFilter);

            return solutions.SelectMany(solution => GetEntityComponentIds(solution));
        }

        private IEnumerable<EntityMetadata> GetRelationMetadata(IEnumerable<EntityMetadata> entitieMetadata)
        {
            var logicalNamesSet = new HashSet<string>(entitieMetadata.Select(x => x.LogicalName));
            var relationalEntityLogicalNames = FindAllRelationsEntities(logicalNamesSet, entitieMetadata);
            var missingLogicalNames = relationalEntityLogicalNames.Where(relation => !logicalNamesSet.Contains(relation));
            var relationEntityMetadata = GetEntityMetadataBulk(missingLogicalNames.Select(lname => GetEntityMetadataRequest(lname)));
            return relationEntityMetadata;
        }

        private IEnumerable<Guid> GetEntityComponentIds(Entity solution)
        {
            var filter = new Dictionary<string, object>() {
                        { "solutionid", solution.Id },
                        { "componenttype", 1 }};
            var columns = new string[] { "solutionid", "objectid", "componenttype" };
            return GetEntities("solutioncomponent", columns, filter).Select(component => (Guid)component["objectid"]);
        }

        private bool NeedActivityParty(IEnumerable<EntityMetadata> metadata)
        {
            return metadata.Any(entity => entity.Attributes.Any(attribute => attribute.AttributeType == AttributeTypeCode.PartyList));
        }

        private IEnumerable<Entity> GetEntities(string logicalName, string[] columns, Dictionary<string, object> filter)
        {
            var f = new FilterExpression();
            foreach (var item in filter)
                f.AddCondition(item.Key, ConditionOperator.Equal, item.Value);
            var query = new QueryExpression(logicalName);
            if (columns.Count() == 0)
                query.ColumnSet = new ColumnSet(true);
            else
                query.ColumnSet = new ColumnSet(columns);
            query.Criteria = f;

            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>());
        }

        private IEnumerable<string> FindAllRelationsEntities(HashSet<string> allLogicalNames, IEnumerable<EntityMetadata> metadata)
        {
            return metadata
                .SelectMany(md =>
                    md.ManyToManyRelationships
                    .Where(m2m => m2m.Entity1LogicalName == md.LogicalName
                        && allLogicalNames.Contains(m2m.Entity2LogicalName)
                        && !allLogicalNames.Contains(m2m.IntersectEntityName))
                    .Select(m2m => m2m.IntersectEntityName));
        }

        private HashSet<string> GetLogicalNames(AssemblyName[] assemblies)
        {
            Console.WriteLine("Retriving logical names from local assemblies");
            var exclude = new string[] { "Microsoft.Xrm.Sdk.dll", "Microsoft.Crm.Sdk.Proxy.dll" };
            var correctAssemblies = assemblies
                .Select(dll => Assembly.Load(dll))
                .Where(asm => asm.CustomAttributes.Any(attr => attr.AttributeType.Name.Equals("ProxyTypesAssemblyAttribute")))
                .Where(asm => !exclude.Contains(asm.ManifestModule.Name));

            var logicalNames = new HashSet<string>();
            foreach (var asm in correctAssemblies)
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        var logicalNameAttribute =
                            type.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute");
                        var logicalName = logicalNameAttribute?.ConstructorArguments?.FirstOrDefault().Value as string;

                        if (logicalName != null)
                        {
                            logicalNames.Add(logicalName);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return logicalNames;
        }

        //The solutionid for Workflowentities points to the active solution  
        //By getting the workflows from the active solution all the workflows from the targeted solution are included.
        internal Guid? GetActiveSolution()
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet(),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition(new ConditionExpression("uniquename", ConditionOperator.Equal, "active"));

            return service.RetrieveMultiple(query).Entities
                .Select(e => e.Id).FirstOrDefault();
        }

        internal IEnumerable<Entity> GetWorkflows()
        {
            var activeSolutionId = GetActiveSolution();

            var query = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
            };

            if (!activeSolutionId.HasValue) return new List<Entity>();

            query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, activeSolutionId);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1);

            var category = new FilterExpression(LogicalOperator.Or);
            category.AddCondition("category", ConditionOperator.Equal, 0);
            category.AddCondition("category", ConditionOperator.Equal, 3);

            query.Criteria.AddFilter(category);

            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>());
        }

        internal Dictionary<Guid, SecurityRole> GetSecurityRoles(Guid rootBUId, bool mitigateDuplicateNames)
        {
            // Queries
            var privQuery = new QueryExpression(PRIVILEGE)
            {
                ColumnSet = new ColumnSet("privilegeid", "accessright", "canbeglobal", "canbedeep", "canbelocal", "canbebasic")
            };
            var privileges = QueryPaging(privQuery);

            var privOTCQuery = new QueryExpression(PRIVILEGE_OTC)
            {
                ColumnSet = new ColumnSet("privilegeid", "objecttypecode")
            };
            var privilegeOTCs = QueryPaging(privOTCQuery);

            var rolePrivQuery = new QueryExpression(ROLEPRIVILEGES)
            {
                ColumnSet = new ColumnSet("privilegeid", "roleid", "privilegedepthmask")
            };
            var rolePrivileges = QueryPaging(rolePrivQuery);

            var roleQuery = new QueryExpression(ROLE)
            {
                ColumnSet = new ColumnSet("parentrootroleid", "name", "roleid", "roletemplateid", "businessunitid")
            };
            var rolelist = QueryPaging(roleQuery);
            // Joins
            // rpr <- roleprivileges inner join roles
            var roleprivilegeIJrole =
                from roleprivilege in rolePrivileges
                join role in rolelist on ((Guid)roleprivilege["roleid"]) equals ((EntityReference)role["parentrootroleid"]).Id
                where ((EntityReference)role["businessunitid"]).Id.Equals(rootBUId) &&
                    (int)roleprivilege["privilegedepthmask"] != 0
                select new { roleprivilege, role };

            // pp <- privileges left outer join privilegeOTCs
            var privilegesLOJprivilegeOTCs =
                from privilege in privileges
                join privilegeOTC in privilegeOTCs on ((Guid)privilege["privilegeid"]) equals ((EntityReference)privilegeOTC["privilegeid"]).Id into res
                from privilegeOTC in res.DefaultIfEmpty()
                select new { privilege, privilegeOTC };

            // entities <- pp left outer join rpr
            var entities =
                from pp in privilegesLOJprivilegeOTCs
                join rpr in roleprivilegeIJrole on ((Guid)pp.privilege["privilegeid"]) equals ((Guid)rpr.roleprivilege["privilegeid"]) into res
                from rpr in res.DefaultIfEmpty()
                select new { pp, rpr };

            // Role generation
            var roles = new Dictionary<Guid, SecurityRole>();

            var validSecurityRoles = entities.Where(e =>
                e.pp?.privilege != null &&
                (e.pp?.privilegeOTC?.Contains("objecttypecode")).GetValueOrDefault() &&
                (e.rpr?.roleprivilege?.Contains("roleid")).GetValueOrDefault() &&
                (e.rpr?.role?.Contains("name")).GetValueOrDefault());

            var roleNameCounters = new Dictionary<string, int>();

            foreach (var e in validSecurityRoles)
            {
                var entityName = (string)e.pp.privilegeOTC["objecttypecode"];

                if (entityName == "none") continue;

                var rp = ToRolePrivilege(e.pp.privilege, e.rpr.roleprivilege);
                if (rp.AccessRight == AccessRights.None) continue;
                var roleId = (Guid)e.rpr.roleprivilege["roleid"];
                if (!roles.ContainsKey(roleId))
                {
                    var name = (string)e.rpr.role["name"];

                    if (mitigateDuplicateNames) {
                        if (roleNameCounters.TryGetValue(name, out var count))
                        {
                            // Role name has been seen before, warn, add and increment counter
                            Console.WriteLine("*** WARNING: DUPLICATE SECURITY ROLE NAME DETECTED: \"{0}\" ***", name);
                            Console.WriteLine("*** IF THIS IS NO ON PURPOSE - REACH OUT TO MICROSOFT SUPPORT ***");
                            name += $"_{++count}";
                            roleNameCounters[name] = count;
                        }
                        else
                        {
                            roleNameCounters[name] = 1;
                        }
                    }

                    roles[roleId] = new SecurityRole()
                    {
                        Name = name,
                        RoleId = roleId
                    };

                    if (e.rpr.role.Attributes.ContainsKey("roletemplateid"))
                    {
                        roles[roleId].RoleTemplateId = ((EntityReference)e.rpr.role["roletemplateid"]).Id;
                    }

                    roles[roleId].Privileges = new Dictionary<string, Dictionary<AccessRights, RolePrivilege>>();
                }

                if (!roles[roleId].Privileges.ContainsKey(entityName))
                {
                    roles[roleId].Privileges.Add(entityName, new Dictionary<AccessRights, RolePrivilege>());
                }

                if(!roles[roleId].Privileges[entityName].ContainsKey(rp.AccessRight))
                {
                    roles[roleId].Privileges[entityName].Add(rp.AccessRight, rp);
                }                
            }
            return roles;
        }

        private List<Entity> QueryPaging(QueryExpression query)
        {
            query.PageInfo.PageNumber = 1;
            var entities = new List<Entity>();
            var resp = service.RetrieveMultiple(query);
            entities.AddRange(resp.Entities);
            while (resp.MoreRecords)
            {
                query.PageInfo.PageNumber = query.PageInfo.PageNumber + 1;
                query.PageInfo.PagingCookie = resp.PagingCookie;
                resp = service.RetrieveMultiple(query);
                entities.AddRange(resp.Entities);
            }
            return entities;
        }

        private PrivilegeDepth PrivilegeDepthToEnum(int privilegeDepth)
        {
            switch (privilegeDepth)
            {
                case 1: return PrivilegeDepth.Basic;
                case 2: return PrivilegeDepth.Local;
                case 4: return PrivilegeDepth.Deep;
                case 8: return PrivilegeDepth.Global;
                case 16: return PrivilegeDepth.RecordFilter;
                default:
                    throw new NotImplementedException($"Unknown privilege depth mask: {privilegeDepth}");
            }
        }

        private RolePrivilege ToRolePrivilege(Entity e1, Entity e2)
        {
            var rp = new RolePrivilege()
            {
                CanBeGlobal = e1.GetAttributeValue<bool>("canbeglobal"),
                CanBeDeep = e1.GetAttributeValue<bool>("canbedeep"),
                CanBeLocal = e1.GetAttributeValue<bool>("canbelocal"),
                CanBeBasic = e1.GetAttributeValue<bool>("canbebasic"),
                AccessRight = e1.GetAttributeValue<AccessRights>("accessright"),
                PrivilegeDepth = PrivilegeDepthToEnum((int)e2["privilegedepthmask"])
            };
            return rp;

        }
        private RetrieveEntityRequest GetEntityMetadataRequest(string logicalName)
        {
            var request = new RetrieveEntityRequest()
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = true
            };
            return request;
        }

        private RetrieveEntityRequest GetEntityMetadataFromIdRequest(Guid id)
        {
            var request = new RetrieveEntityRequest()
            {
                MetadataId = id,
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = true
            };
            return request;
        }

        private EntityMetadata[] GetEntityMetadataBulk(IEnumerable<OrganizationRequest> requests)
        {
            var request = new ExecuteMultipleRequest
            {
                Requests = new OrganizationRequestCollection()
            };
            request.Requests.AddRange(requests);
            request.Settings = new ExecuteMultipleSettings()
            {
                ContinueOnError = false,
                ReturnResponses = true
            };

            var bulkResp = (service.Execute(request) as ExecuteMultipleResponse).Responses;
            if (bulkResp.Any(resp => resp.Fault != null))
            {
                var resp = bulkResp.First(r => r.Fault != null);
                var faultedReq = (RetrieveEntityRequest)request.Requests[resp.RequestIndex];
                throw new FaultException($"Error while retrieving entity metadata for entity {faultedReq.LogicalName}: {resp.Fault.Message}");
            }

            return bulkResp
                .Select(resp => (resp.Response as RetrieveEntityResponse).EntityMetadata)
                .ToArray();
        }

        private Dictionary<string, Dictionary<int, int>> GetDefaultStateAndStatus()
        {
            var dict = new Dictionary<string, Dictionary<int, int>>();

            var query = new QueryExpression("statusmap")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.PageInfo.PageNumber = 1;

            var statusmaps = new List<Entity>();
            var resp = service.RetrieveMultiple(query);
            statusmaps.AddRange(resp.Entities);
            while (resp.MoreRecords)
            {
                query.PageInfo.PageNumber = query.PageInfo.PageNumber + 1;
                query.PageInfo.PagingCookie = resp.PagingCookie;
                resp = service.RetrieveMultiple(query);
                statusmaps.AddRange(resp.Entities);
            }

            foreach (var e in statusmaps.Where(x => x.GetAttributeValue<bool>("isdefault")))
            {
                var logicalName = e.GetAttributeValue<string>("objecttypecode");
                if (!dict.ContainsKey(logicalName))
                {
                    dict.Add(logicalName, new Dictionary<int, int>());
                }
                var state = e.GetAttributeValue<int>("state");
                if (!dict[logicalName].ContainsKey(state))
                {
                    dict[logicalName].Add(state, e.GetAttributeValue<int>("status"));
                }
            }

            return dict;
        }
    }
}
