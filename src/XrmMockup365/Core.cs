using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Logging;
using DG.Tools.XrmMockup.Serialization;
using DG.Tools.XrmMockup.Online;
using XrmPluginCore.Enums;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using WorkflowExecuter;
using Utility = DG.Tools.XrmMockup.Internal.Utility;

[assembly: InternalsVisibleTo("SharedTests")]

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// Class for handling all requests to the database
    /// </summary>
    internal class Core : IXrmMockupExtension, ICoreOperations
    {
        public TimeSpan TimeOffset { get; private set; }
        public MockupServiceProviderAndFactory ServiceFactory { get; private set; }
        public ITracingServiceFactory TracingServiceFactory { get; private set; }
        public EntityReference AdminUserRef { get; private set; }
        public EntityReference RootBusinessUnitRef { get; private set; }
        public List<RequestHandler> RequestHandlers { get; private set; }

        /// <summary>
        /// Organization id for the Mockup instance
        /// </summary>
        public Guid OrganizationId { get; private set; }

        /// <summary>
        /// Organization name for the Mockup instance
        /// </summary>
        public string OrganizationName { get; private set; }

        internal OrganizationDetail OrganizationDetail { get; private set; }
        internal EntityReference BaseCurrency { get; private set; }

        private RequestExecutionPipeline pipeline;
        private PluginManager pluginManager;
        private CustomApiManager customApiManager;
        private WorkflowManager workflowManager;
        private Security security;
        private XrmMockupSettings settings;
        private MetadataSkeleton metadata;
        private XrmDb db;
        private Dictionary<string, Snapshot> snapshots;
        private Dictionary<string, Type> entityTypeMap = new Dictionary<string, Type>();
        private IOnlineDataService OnlineDataService;
        private int baseCurrencyPrecision;
        private FormulaFieldEvaluator FormulaFieldEvaluator { get; set; }
        private List<string> systemAttributeNames;
        internal FileBlockStore FileBlockStore { get; private set; }

        /// <summary>Logger scoped to the mockup engine; used by request handlers to surface warnings.</summary>
        internal ILogger Logger { get; private set; }

        /// <summary>
        /// Creates a new instance of Core
        /// </summary>
        public Core(XrmMockupSettings Settings, MetadataSkeleton metadata, List<Entity> Workflows,
            List<SecurityRole> SecurityRoles)
        {
            var initData = new CoreInitializationData
            {
                Settings = Settings,
                Metadata = metadata,
                Workflows = Workflows,
                SecurityRoles = SecurityRoles,
                BaseCurrency = metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid"),
                BaseCurrencyPrecision = metadata.BaseOrganization.GetAttributeValue<int>("pricingdecimalprecision"),
                OnlineDataService = null,
                EntityTypeMap = new Dictionary<string, Type>(),
                LoggerFactory = ResolveLoggerFactory(Settings)
            };

            InitializeCore(initData);
        }

        /// <summary>
        /// Creates a new instance of Core using cached static metadata
        /// </summary>
        public Core(XrmMockupSettings Settings, StaticMetadataCache staticCache)
        {
            var initData = new CoreInitializationData
            {
                Settings = Settings,
                Metadata = staticCache.Metadata,
                Workflows = staticCache.Workflows,
                SecurityRoles = staticCache.SecurityRoles,
                BaseCurrency = staticCache.BaseCurrency,
                BaseCurrencyPrecision = staticCache.BaseCurrencyPrecision,
                OnlineDataService = staticCache.OnlineDataService,
                EntityTypeMap = staticCache.EntityTypeMap,
                LoggerFactory = staticCache.LoggerFactory
            };

            InitializeCore(initData);
        }

        /// <summary>
        /// Common initialization logic for both constructors
        /// </summary>
        private void InitializeCore(CoreInitializationData initData)
        {
            var sw = Stopwatch.StartNew();
            var loggerFactory = initData.LoggerFactory ?? NullLoggerFactory.Instance;
            var coreLogger = loggerFactory.CreateLogger(typeof(Core).FullName);
            Logger = coreLogger;

            TimeOffset = new TimeSpan();
            settings = initData.Settings;
            metadata = initData.Metadata;
            BaseCurrency = initData.BaseCurrency;
            baseCurrencyPrecision = initData.BaseCurrencyPrecision;
            OnlineDataService = initData.OnlineDataService;
            db = new XrmDb(initData.Metadata.EntityMetadata, initData.OnlineDataService);
            entityTypeMap = initData.EntityTypeMap;
            FileBlockStore = new FileBlockStore();
            snapshots = new Dictionary<string, Snapshot>();
            security = new Security(this, initData.Metadata, initData.SecurityRoles, db);
            TracingServiceFactory = initData.Settings.TracingServiceFactory ?? new TracingServiceFactory();
            ServiceFactory = new MockupServiceProviderAndFactory(this);

            // Handle plugin metadata - create new list for non-cached scenarios or merge with cached
            var allPlugins = new List<MetaPlugin>(initData.Metadata.Plugins);
            if (initData.Settings.IPluginMetadata != null)
            {
                allPlugins.AddRange(initData.Settings.IPluginMetadata);
            }

            var pluginLogger = loggerFactory.CreateLogger(typeof(PluginManager).FullName);
            pluginManager = new PluginManager(this, initData.Settings.BasePluginTypes, initData.Metadata.EntityMetadata, allPlugins, pluginLogger);

            var workflowLogger = loggerFactory.CreateLogger(typeof(WorkflowManager).FullName);
            workflowManager = new WorkflowManager(this, initData.Settings.CodeActivityInstanceTypes, initData.Settings.IncludeAllWorkflows,
                initData.Workflows, initData.Metadata.EntityMetadata, workflowLogger);

            var apiLogger = loggerFactory.CreateLogger(typeof(CustomApiManager).FullName);
            customApiManager = new CustomApiManager(this, initData.Settings.BaseCustomApiTypes, apiLogger);

            var typesMissingRegistration = pluginManager.missingRegistrations
                .Intersect(customApiManager.missingRegistration)
                .ToList();
            if (typesMissingRegistration.Count > 0)
            {
                var typeNames = string.Join(", ", typesMissingRegistration.Select(t => t.FullName));
                throw new Exception($"The following plugin types are missing plugin or custom api registrations: {typeNames}");
            }

            sw.Stop();
            coreLogger.LogInformation("XrmMockup initialized in {ElapsedMs}ms - Plugins: {PluginCount}, Workflows: sync={SyncCount} async={AsyncCount}, Custom APIs: {ApiCount}",
                sw.ElapsedMilliseconds,
                pluginManager.PluginRegistrations.Count,
                workflowManager.SynchronousWorkflowCount,
                workflowManager.AsynchronousWorkflowCount,
                customApiManager.RegisteredApiCount);

            // System-managed attributes that Dataverse resolves during the main operation. They are
            // not present in the Target during the pre-stages, but are copied back onto the Target for
            // post-operation plugins (and are always available via the post-image).
            systemAttributeNames = new List<string>()
            {
                "createdon", "createdby", "modifiedon", "modifiedby",
                "ownerid", "owningbusinessunit", "owninguser", "owningteam",
                "statecode", "statuscode"
            };

            RequestHandlers = GetRequestHandlers(db);
            InitializeDB();

            // Add workflow entities to database so they can be queried
            // Only add if workflow entity metadata exists (for backwards compatibility with older metadata)
            if (initData.Workflows != null && metadata.EntityMetadata.ContainsKey("workflow"))
            {
                foreach (var workflow in initData.Workflows)
                {
                    var workflowCopy = CloneEntity(workflow);
                    this.db.Add(workflowCopy, false);
                }
            }

            security.InitializeSecurityRoles(db);
            OrganizationDetail = initData.Settings.OrganizationDetail;

            pipeline = new RequestExecutionPipeline(this, pluginManager, workflowManager);

            FormulaFieldEvaluator = new FormulaFieldEvaluator(ServiceFactory);
        }

        /// <summary>
        /// Builds a static metadata cache from settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static StaticMetadataCache BuildStaticMetadataCache(XrmMockupSettings settings)
        {
            var metadataDirectory = settings.MetadataDirectoryPath ?? "../../Metadata/";
            var metadata = Utility.GetMetadata(metadataDirectory);
            var workflows = Utility.GetWorkflows(metadataDirectory);
            var securityRoles = Utility.GetSecurityRoles(metadataDirectory);

            var baseCurrency = metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid");
            var baseCurrencyPrecision = metadata.BaseOrganization.GetAttributeValue<int>("pricingdecimalprecision");

            var onlineDataService = BuildOnlineDataService(settings);
            var entityTypeMap = new Dictionary<string, Type>();

            // Build entity type map for proxy types if enabled
            if (settings.EnableProxyTypes == true)
            {
                BuildEntityTypeMap(settings, entityTypeMap);
            }

            // Note: IPluginMetadata is handled per-instance in the Core constructor
            // to avoid modifying the shared cache

            var loggerFactory = ResolveLoggerFactory(settings);

            return new StaticMetadataCache(metadata, workflows, securityRoles, entityTypeMap,
                baseCurrency, baseCurrencyPrecision, onlineDataService, loggerFactory);
        }

        private static ILoggerFactory ResolveLoggerFactory(XrmMockupSettings settings)
        {
            if (settings.LoggerFactory != null)
                return settings.LoggerFactory;

            if (!string.IsNullOrEmpty(settings.LogFilePath))
                return new FileLoggerFactory(settings.LogFilePath, settings.MinLogLevel);

            return NullLoggerFactory.Instance;
        }

        private static IOnlineDataService BuildOnlineDataService(XrmMockupSettings settings)
        {
#if DATAVERSE_SERVICE_CLIENT
            // Allow injection for testing
            if (settings.OnlineDataServiceFactory != null)
            {
                return settings.OnlineDataServiceFactory();
            }

            if (settings.OnlineEnvironment.HasValue)
            {
                var env = settings.OnlineEnvironment.Value;
                return new OnlineDataService(env.Url);
            }
#endif
            return null;
        }

        private static void BuildEntityTypeMap(XrmMockupSettings settings, Dictionary<string, Type> entityTypeMap)
        {
            if (settings.Assemblies?.Any() ?? false)
            {
                foreach (var assembly in settings.Assemblies)
                {
                    EnableProxyTypes(assembly, entityTypeMap);
                }
            }
            else
            {
                List<string> exclude = new List<string> {
                    "Microsoft.Xrm.Sdk.dll",
                    "Microsoft.Crm.Sdk.Proxy.dll"
                };

                var regex = new Regex("^XrmMockup.*\\.dll$");
                var assemblies = new List<Assembly>();
                var addedAssemblies = new HashSet<string>();

                var exeAsm = AppDomain.CurrentDomain.GetAssemblies();
                assemblies.AddRange(exeAsm);
                foreach (var name in exeAsm.Select(x => x.FullName))
                {
                    addedAssemblies.Add(name);
                }

                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                foreach (string dll in Directory.GetFiles(path, "*.dll"))
                {
                    var asm = Assembly.LoadFrom(dll);
                    if (addedAssemblies.Contains(asm.FullName)) continue;

                    assemblies.Add(asm);
                    addedAssemblies.Add(asm.FullName);
                }

                var useableAssemblies =
                    assemblies
                    .Where(asm => asm.CustomAttributes.Any(attr => attr.AttributeType.Name.Equals("ProxyTypesAssemblyAttribute")))
                    .Where(asm => !exclude.Contains(asm.ManifestModule.Name) && !regex.IsMatch(asm.ManifestModule.Name))
                    .ToList();

                if (useableAssemblies?.Count > 0)
                {
                    foreach (var asm in useableAssemblies)
                    {
                        EnableProxyTypes(asm, entityTypeMap);
                    }
                }
            }
        }

        private static void EnableProxyTypes(Assembly assembly, Dictionary<string, Type> entityTypeMap)
        {
            foreach (var type in assembly.GetLoadableTypes())
            {
                if (type.CustomAttributes
                    .FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute")
                    ?.ConstructorArguments
                    ?.FirstOrDefault()
                    .Value is string logicalName)
                {
                    entityTypeMap.Add(logicalName, type);
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of an Entity to avoid modifying shared cached entities
        /// </summary>
        private Entity CloneEntity(Entity original)
        {
            var clone = new Entity(original.LogicalName, original.Id);
            foreach (var attr in original.Attributes)
            {
                clone[attr.Key] = attr.Value;
            }
            return clone;
        }

        private void InitializeDB()
        {
            this.OrganizationId = Guid.NewGuid();
            this.OrganizationName = "MockupOrganization";

            // Add organization entity to database so it can be queried
            if (metadata.BaseOrganization != null && metadata.EntityMetadata.ContainsKey(metadata.BaseOrganization.LogicalName))
            {
                var orgEntity = CloneEntity(metadata.BaseOrganization);
                orgEntity.Id = this.OrganizationId;
                orgEntity["organizationid"] = this.OrganizationId;
                this.db.Add(orgEntity, false);
            }

            // Setup currencies - create copies to avoid modifying shared cached entities
            var currencies = new List<Entity>();
            foreach (var entity in metadata.Currencies)
            {
                var currencyCopy = CloneEntity(entity);
                Utility.RemoveAttribute(currencyCopy, "createdby", "modifiedby", "organizationid", "modifiedonbehalfby",
                    "createdonbehalfby");
                currencies.Add(currencyCopy);
            }

            this.db.AddRange(currencies);

            // Setup root business unit - create a copy to avoid modifying the shared cached entity
            var rootBu = CloneEntity(metadata.RootBusinessUnit);
            rootBu["name"] = "RootBusinessUnit";
            rootBu.Attributes.Remove("organizationid");
            this.db.Add(rootBu, false);
            this.RootBusinessUnitRef = rootBu.ToEntityReference();

            // Setup admin user
            var admin = new Entity(LogicalNames.SystemUser)
            {
                Id = Guid.NewGuid()
            };
            this.AdminUserRef = admin.ToEntityReference();

            admin["firstname"] = "";
            admin["lastname"] = "SYSTEM";
            admin["businessunitid"] = RootBusinessUnitRef;
            this.db.Add(admin);

            // Setup default team for root business unit
            var defaultTeam = Utility.CreateDefaultTeam(rootBu, AdminUserRef);
            this.db.Add(defaultTeam);

            // Adding admin user to root business unit default team
            var teamMembership = new Entity(LogicalNames.TeamMembership);
            teamMembership["teamid"] = defaultTeam.Id;
            teamMembership["systemuserid"] = admin.Id;
            this.db.Add(teamMembership);
        }

        private List<RequestHandler> GetRequestHandlers(XrmDb db) => new List<RequestHandler>
        {
            new CreateRequestHandler(this, db, metadata, security),
            new UpdateRequestHandler(this, db, metadata, security),
            new RetrieveMultipleRequestHandler(this, db, metadata, security),
            new RetrieveRequestHandler(this, db, metadata, security),
            new DeleteRequestHandler(this, db, metadata, security),
            new SetStateRequestHandler(this, db, metadata, security),
            new AssignRequestHandler(this, db, metadata, security),
            new AssociateRequestHandler(this, db, metadata, security),
            new DisassociateRequestHandler(this, db, metadata, security),
            new MergeRequestHandler(this, db, metadata, security),
            new RetrieveVersionRequestHandler(this, db, metadata, security),
            new FetchXmlToQueryExpressionRequestHandler(this, db, metadata, security),
            new ExecuteMultipleRequestHandler(this, db, metadata, security),
            new RetrieveEntityRequestHandler(this, db, metadata, security),
            new RetrieveRelationshipRequestHandler(this, db, metadata, security),
            new GrantAccessRequestHandler(this, db, metadata, security),
            new ModifyAccessRequestHandler(this, db, metadata, security),
            new RevokeAccessRequestHandler(this, db, metadata, security),
            new WinOpportunityRequestHandler(this, db, metadata, security),
            new LoseOpportunityRequestHandler(this, db, metadata, security),
            new QualifyLeadRequestHandler(this, db, metadata, security),
            new RetrieveAllOptionSetsRequestHandler(this, db, metadata, security),
            new RetrieveOptionSetRequestHandler(this, db, metadata, security),
            new RetrieveExchangeRateRequestHandler(this, db, metadata, security),
            new CloseIncidentRequestHandler(this, db, metadata, security),
            new AddMembersTeamRequestHandler(this, db, metadata, security),
            new RemoveMembersTeamRequestHandler(this, db, metadata, security),
            new SendEmailRequestHandler(this, db, metadata, security),
            new IsValidStateTransitionRequestHandler(this, db, metadata, security),
            new CalculateRollupFieldRequestHandler(this, db, metadata, security),
            new AddUserToRecordTeamRequestHandler(this, db, metadata, security),
            new RemoveUserFromRecordTeamRequestHandler(this, db, metadata, security),
            new RetrieveCurrentOrganizationRequestHandler(this, db, metadata, security),
            new UpsertRequestHandler(this, db, metadata, security),
            new RetrieveAttributeRequestHandler(this, db, metadata, security),
            new WhoAmIRequestHandler(this, db, metadata, security),
            new RetrieveAllEntitiesRequestHandler(this, db, metadata, security),
            new RetrievePrincipalAccessRequestHandler(this, db, metadata, security),
            new RetrieveMetadataChangesRequestHandler(this, db, metadata, security),
            new InitializeFileBlocksUploadRequestHandler(this, db, metadata, security),
            new UploadBlockRequestHandler(this, db, metadata, security),
            new CommitFileBlocksUploadRequestHandler(this, db, metadata, security),
            new InitializeFileBlocksDownloadRequestHandler(this, db, metadata, security),
            new DownloadBlockRequestHandler(this, db, metadata, security),
            new InstantiateTemplateRequestHandler(this, db, metadata, security),
            new SendEmailFromTemplateRequestHandler(this, db, metadata, security),
            new CreateMultipleRequestHandler(this, db, metadata, security),
            new UpdateMultipleRequestHandler(this, db, metadata, security),
            new DeleteMultipleRequestHandler(this, db, metadata, security),
            new UpsertMultipleRequestHandler(this, db, metadata, security),
            new ExecuteTransactionRequestHandler(this, db, metadata, security),
            new WinQuoteRequestHandler(this, db, metadata, security),
            new CloseQuoteRequestHandler(this, db, metadata, security),
            new ReviseQuoteRequestHandler(this, db, metadata, security)
        };

        internal void EnableProxyTypes(Assembly assembly)
        {
            foreach (var type in assembly.GetLoadableTypes())
            {
                if (type.CustomAttributes
                    .FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute")
                    ?.ConstructorArguments
                    ?.FirstOrDefault()
                    .Value is string logicalName)
                {
                    entityTypeMap.Add(logicalName, type);
                }
            }
        }

        internal IOrganizationService GetWorkflowService()
        {
            return ServiceFactory.CreateOrganizationService(null,
                new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
        }

        internal bool HasType(string entityType)
        {
            return entityTypeMap.ContainsKey(entityType);
        }

        private Entity GetEntity(string entityType)
        {
            if (HasType(entityType))
            {
                return (Entity)Activator.CreateInstance(entityTypeMap[entityType]);
            }

            return null;
        }

        public DbRow GetDbRow(EntityReference entityReference)
        {
            return db.GetDbRow(entityReference);
        }

        internal DbRow GetDbRowOrNull(EntityReference entityReference)
        {
            return db.GetDbRowOrNull(entityReference);
        }

        internal DbTable GetDbTable(string tableName)
        {
            return db[tableName];
        }

        internal Entity GetStronglyTypedEntity(Entity entity, EntityMetadata metadata, ColumnSet colsToKeep)
        {
            Entity toReturn;

            if (HasType(entity.LogicalName))
            {
                toReturn = GetEntity(entity.LogicalName);
                toReturn.SetAttributes(entity.Attributes, metadata, colsToKeep);
                toReturn.RelatedEntities = entity.RelatedEntities;

                toReturn.Id = entity.Id;
                toReturn.EntityState = entity.EntityState;
            }
            else
            {
                toReturn = entity.CloneEntity(metadata, colsToKeep);
            }
            toReturn.KeyAttributes = entity.CloneKeyAttributes();
            Utility.PopulateEntityReferenceNames(toReturn, db);
            return toReturn;
        }

        internal void AddRelatedEntities(Entity entity, RelationshipQueryCollection relatedEntityQuery,
            EntityReference userRef)
        {
            foreach (var relQuery in relatedEntityQuery)
            {
                var relationship = relQuery.Key;
                if (!(relQuery.Value is QueryExpression queryExpr))
                {
                    queryExpr = XmlHandling.FetchXmlToQueryExpression(((FetchExpression)relQuery.Value).Query);
                }

                var relationshipMetadata = Utility.GetRelatedEntityMetadata(metadata.EntityMetadata,
                    queryExpr.EntityName, relationship.SchemaName);


                var oneToMany = relationshipMetadata as OneToManyRelationshipMetadata;
                var manyToMany = relationshipMetadata as ManyToManyRelationshipMetadata;

                if (oneToMany != null)
                {
                    if (relationship.PrimaryEntityRole == EntityRole.Referencing)
                    {
                        var entityAttributes = db.GetEntityOrNull(entity.ToEntityReference()).Attributes;
                        if (entityAttributes.ContainsKey(oneToMany.ReferencingAttribute) &&
                            entityAttributes[oneToMany.ReferencingAttribute] != null)
                        {
                            var referencingGuid =
                                Utility.GetGuidFromReference(entityAttributes[oneToMany.ReferencingAttribute]);
                            queryExpr.Criteria.AddCondition(
                                new ConditionExpression(oneToMany.ReferencedAttribute, ConditionOperator.Equal,
                                    referencingGuid));
                        }
                    }
                    else
                    {
                        queryExpr.Criteria.AddCondition(
                            new ConditionExpression(oneToMany.ReferencingAttribute, ConditionOperator.Equal,
                                entity.Id));
                    }
                }

                if (manyToMany != null)
                {
                    if (db[manyToMany.IntersectEntityName].Count() > 0)
                    {
                        var conditions = new FilterExpression(LogicalOperator.Or);
                        if (entity.LogicalName == manyToMany.Entity1LogicalName)
                        {
                            queryExpr.EntityName = manyToMany.Entity2LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute));

                            foreach (var id in relatedIds)
                            {
                                conditions.AddCondition(
                                    new ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        }
                        else
                        {
                            queryExpr.EntityName = manyToMany.Entity1LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute));

                            foreach (var id in relatedIds)
                            {
                                conditions.AddCondition(
                                    new ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        }

                        queryExpr.Criteria = conditions;
                    }
                }

                var entities = new EntityCollection();

                if ((oneToMany != null || manyToMany != null) && queryExpr.Criteria.Conditions.Count > 0)
                {
                    var handler = RequestHandlers.Find(x => x is RetrieveMultipleRequestHandler);
                    var req = new RetrieveMultipleRequest
                    {
                        Query = queryExpr
                    };
                    var resp = handler.Execute(req, userRef) as RetrieveMultipleResponse;
                    entities = resp.EntityCollection;
                }

                if (entities.Entities.Count() > 0)
                {
                    entity.RelatedEntities.Add(relationship, entities);
                }
            }
        }

        private bool HasCascadeBehaviour(CascadeType? cascadeType)
        {
            return cascadeType != null && cascadeType != CascadeType.NoCascade;
        }

        private bool CascadeCompare(CascadeConfiguration cascadeConfiguration, CascadeSelection selection)
        {
            if (selection == null)
                return true;

            if (selection.assign && HasCascadeBehaviour(cascadeConfiguration.Assign))
                return true;

            if (selection.delete && HasCascadeBehaviour(cascadeConfiguration.Delete))
                return true;

            if (selection.merge && HasCascadeBehaviour(cascadeConfiguration.Merge))
                return true;

            if (selection.reparent && HasCascadeBehaviour(cascadeConfiguration.Reparent))
                return true;

            if (selection.share && HasCascadeBehaviour(cascadeConfiguration.Share))
                return true;

            if (selection.unshare && HasCascadeBehaviour(cascadeConfiguration.Unshare))
                return true;

            if (selection.rollup && HasCascadeBehaviour(cascadeConfiguration.RollupView))
                return true;

            return false;
        }

        //TODO: update to also take in cascading filtering on Assign, Delete, Merge, reparent, rollup
        internal Entity GetDbEntityWithRelatedEntities(EntityReference reference, EntityRole primaryEntityRole,
            EntityReference userRef, CascadeSelection cascadeSelection = null, params Relationship[] relations)
        {
            var entity = db.GetEntityOrNull(reference);
            if (entity == null)
            {
                return null;
            }

            var metadata = this.metadata.EntityMetadata.GetMetadata(entity.LogicalName);

            if (entity.RelatedEntities.Count() > 0)
            {
                var clone = entity.CloneEntity(metadata, new ColumnSet(true));
                db.Update(clone);
                entity = clone;
            }

            var relationQuery = new RelationshipQueryCollection();
            var relationsMetadata =
                primaryEntityRole == EntityRole.Referenced
                    ? metadata.OneToManyRelationships
                    : metadata.ManyToOneRelationships;

            if (cascadeSelection != null)
            {
                relationsMetadata.Where(x => CascadeCompare(x.CascadeConfiguration, cascadeSelection));
            }

            if (relations.Any())
            {
                relationsMetadata = relationsMetadata
                    .Join(relations, x => x.SchemaName, y => y.SchemaName, (r1, r2) => r1).ToArray();
            }

            relationQuery.AddRange(
                relationsMetadata
                    .Select(relationshipMeta =>
                    {
                        var rel = new Relationship()
                        {
                            SchemaName = relationshipMeta.SchemaName,
                            PrimaryEntityRole = primaryEntityRole
                        };
                        var query = new QueryExpression()
                        {
                            EntityName =
                                primaryEntityRole == EntityRole.Referenced
                                    ? relationshipMeta.ReferencingEntity
                                    : relationshipMeta.ReferencedEntity,
                            ColumnSet = new ColumnSet(true)
                        };
                        return new KeyValuePair<Relationship, QueryBase>(rel, query);
                    }));


            foreach (var relationshipMeta in relationsMetadata)
            {
            }

            if (cascadeSelection == null)
            {
                var relationShipManyMetadata = metadata.ManyToManyRelationships;
                if (relations.Any())
                {
                    relationShipManyMetadata = relationShipManyMetadata
                        .Join(relations, x => x.SchemaName, y => y.SchemaName, (r1, r2) => r1).ToArray();
                }

                relationQuery.AddRange(relationShipManyMetadata
                    .Select(relationshipMeta =>
                    {
                        var rel = new Relationship() { SchemaName = relationshipMeta.SchemaName };
                        var query = new QueryExpression(relationshipMeta.IntersectEntityName)
                        {
                            ColumnSet = new ColumnSet(true)
                        };
                        return new KeyValuePair<Relationship, QueryBase>(rel, query);
                    }));
            }

            AddRelatedEntities(entity, relationQuery, userRef);
            return entity;
        }


        internal void Initialize(params Entity[] entities)
        {
            foreach (var entity in entities)
            {
                var createHandler = RequestHandlers.Find(x => x is CreateRequestHandler);
                createHandler.Execute(new CreateRequest { Target = entity }, null);
            }
        }

        /// <summary>
        /// Execute the request and trigger plugins if needed
        /// </summary>
        public OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef)
        {
            return Execute(request, userRef, null);
        }

        public OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef,
            PluginContext parentPluginContext)
        {
            return pipeline.Execute(request, userRef, parentPluginContext);
        }

        // ── ICoreOperations — infrastructure methods the pipeline calls back into Core for ──

        public bool HasMockupExtensions => settings.MockUpExtensions.Count != 0;

        public EntityReference GetBusinessUnit(EntityReference owner)
        {
            return Utility.GetBusinessUnit(db, owner);
        }

        public ISet<string> CopySystemAttributes(Entity postImage, Entity target)
        {
            var injected = new HashSet<string>();
            if (target == null) return injected;

            foreach (var systemAttributeName in systemAttributeNames)
            {
                if (postImage.Contains(systemAttributeName))
                {
                    // Only attributes this copy *adds* are reported. One the caller (or a
                    // pre-operation plugin) already put in the Target is a genuine part of the
                    // update and must keep counting towards plugin/workflow attribute filters.
                    var wasAbsent = !target.Contains(systemAttributeName);

                    if (postImage[systemAttributeName] is EntityReference reference)
                    {
                        target[systemAttributeName] = new EntityReference(reference.LogicalName, reference.Id);
                    }
                    else if (postImage[systemAttributeName] is DateTime dateTime)
                    {
                        target[systemAttributeName] = dateTime;
                    }
                    else if (postImage[systemAttributeName] is OptionSetValue optionSet)
                    {
                        target[systemAttributeName] = new OptionSetValue(optionSet.Value);
                    }
                    else
                    {
                        continue;
                    }

                    if (wasAbsent) injected.Add(systemAttributeName);
                }
            }

            return injected;
        }

        public void HandleInternalPreOperations(OrganizationRequest request, EntityReference userRef)
        {
            if (request.RequestName == "Create")
            {
                var entity = request["Target"] as Entity;
                if (entity.Id == Guid.Empty)
                    entity.Id = Guid.NewGuid();

                // Note: ownerid is intentionally NOT defaulted here. In Dataverse the owner is
                // resolved during the main operation (stage 30), so it must not be visible in the
                // Target during the PreValidation/PreOperation stages. The persisted default (calling
                // user) is applied by CreateRequestHandler.Execute, and the resolved value surfaces
                // afterwards via the post-image and the post-operation Target.
            }
        }

        public OrganizationResponse ExecuteAction(OrganizationRequest request)
        {
            return ExecuteActionInternal(request);
        }

        public bool HandlesCustomApi(string requestName) => customApiManager.HandlesRequest(requestName);

        public OrganizationResponse ExecuteCustomApi(OrganizationRequest request, PluginContext pluginContext)
        {
            return customApiManager.Execute(request, pluginContext);
        }

        public bool IsExceptionFreeRequest(string requestName)
        {
            return settings.ExceptionFreeRequests?.Contains(requestName) ?? false;
        }

        public IOrganizationService CreateMockupService(Guid? userId, PluginContext pluginContext)
        {
            return new MockupService(this, userId, pluginContext);
        }

        public MockupServiceProviderAndFactory CreateServiceProviderAndFactory(PluginContext pluginContext)
        {
            return new MockupServiceProviderAndFactory(this, pluginContext, TracingServiceFactory);
        }

        private readonly List<PluginTraceLog> _pluginTraceLogs = new List<PluginTraceLog>();
        private readonly object _pluginTraceLogLock = new object();

        public void RecordPluginTrace(PluginTraceLog log)
        {
            lock (_pluginTraceLogLock)
            {
                _pluginTraceLogs.Add(log);
            }
        }

        internal IReadOnlyList<PluginTraceLog> PluginTraceLogs
        {
            get
            {
                lock (_pluginTraceLogLock)
                {
                    return _pluginTraceLogs.ToList();
                }
            }
        }

        internal void ClearPluginTraceLogs()
        {
            lock (_pluginTraceLogLock)
            {
                _pluginTraceLogs.Clear();
            }
        }

        // ──────────────────────────────────────────────────────────────────────────

        internal void AddTime(TimeSpan offset)
        {
            this.TimeOffset = this.TimeOffset.Add(offset);
            TriggerWaitingWorkflows();
        }

        internal void TriggerWaitingWorkflows()
        {
            workflowManager.ExecuteWaitingWorkflows(null);
        }

        internal void AddWorkflow(Entity workflow)
        {
            workflowManager.AddWorkflow(workflow);
        }

        internal bool ContainsEntity(Entity entity)
        {
            var dbentity = db.GetEntityOrNull(entity.ToEntityReference());
            if (dbentity == null) return false;
            return entity.Attributes.All(a =>
                dbentity.Attributes.ContainsKey(a.Key) && dbentity.Attributes[a.Key].Equals(a.Value));
        }

        internal void PopulateWith(Entity[] entities)
        {
            foreach (var entity in entities)
            {
                if (entity.Id == Guid.Empty)
                {
                    var id = Guid.NewGuid();
                    entity.Id = id;
                    entity[entity.LogicalName + "id"] = id;
                }

                db.Add(entity);
            }
        }

        /// <summary>
        /// Prefills the local database with data from the online service based on the query.
        /// </summary>
        internal void PrefillDBWithOnlineData(QueryExpression query)
        {
            db.PrefillDBWithOnlineData(query);
        }

        internal Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> GetPrivilege(Guid principleId)
        {
            return security.GetPrincipalPrivilege(principleId);
        }

        internal void AddPrivileges(EntityReference entRef,
            Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> privileges)
        {
            security.AddPrinciplePrivileges(entRef.Id, privileges);
        }

        internal void SetSecurityRoles(EntityReference entRef, Guid[] securityRoles)
        {
            security.SetSecurityRole(entRef, securityRoles);
        }

        internal bool HasPermission(EntityReference entityRef, AccessRights access, EntityReference principleRef)
        {
            return security.HasPermission(entityRef, access, principleRef);
        }

        private OrganizationResponse ExecuteActionInternal(OrganizationRequest request)
        {
            var action = workflowManager.GetActionDefaultNull(request.RequestName);

            var workflow = workflowManager.ParseWorkflow(action);
            if (workflow.Input.Where(a => a.Required).Any(required => !request.Parameters.ContainsKey(required.Name)))
            {
                throw new FaultException(
                    $"Call to action '{request.RequestName}' but no all required input arguments were provided");
            }

            var entity = db.GetEntityOrNull(request.Parameters["Target"] as EntityReference).CloneEntity();

            var inputs = workflow.Input.Where(a => request.Parameters.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, request.Parameters[a.Name]));
            foreach (var input in inputs)
            {
                var argumentName = "{" + input.Key + "(Arguments)}";
                workflow.Variables.Add(argumentName, input.Value);
            }

            var postExecution = workflowManager.ExecuteWorkflow(workflow, entity, null);

            var outputs = workflow.Output.Where(a => postExecution.Variables.ContainsKey(a.Name))
                .Select(a => new KeyValuePair<string, object>(a.Name, postExecution.Variables[a.Name]));

            var resp = new OrganizationResponse();
            foreach (var output in outputs)
            {
                resp.Results[output.Key] = output.Value;
            }

            postExecution.Variables = new Dictionary<string, object>();

            return resp;
        }

        #region EntityImage helpers

        private Tuple<object, string, Guid> GetEntityInfo(OrganizationRequest request)
        {
            Mappings.EntityImageProperty.TryGetValue(request.GetType(), out string key);
            object obj = null;
            if (key != null)
            {
                obj = request.Parameters[key];
            }

            if (request is WinOpportunityRequest || request is LoseOpportunityRequest)
            {
                var close = request is WinOpportunityRequest
                    ? (request as WinOpportunityRequest).OpportunityClose
                    : (request as LoseOpportunityRequest).OpportunityClose;
                obj = close.GetAttributeValue<EntityReference>("opportunityid");
            }
            else if (request is WinQuoteRequest || request is CloseQuoteRequest)
            {
                var close = request is WinQuoteRequest
                    ? (request as WinQuoteRequest).QuoteClose
                    : (request as CloseQuoteRequest).QuoteClose;
                obj = close.GetAttributeValue<EntityReference>("quoteid");
            }
            else if (request is CloseIncidentRequest closeIncidentRequest)
            {
                obj = closeIncidentRequest.IncidentResolution?.GetAttributeValue<EntityReference>("incidentid");
            }
            else if (request is RetrieveMultipleRequest)
            {
                var retrieve = request as RetrieveMultipleRequest;

                string entityName = null;
                switch (retrieve.Query)
                {
                    case FetchExpression fe:
                        var qe = XmlHandling.FetchXmlToQueryExpression(fe.Query);
                        entityName = qe.EntityName;
                        break;
                    case QueryExpression query:
                        entityName = query.EntityName;
                        break;
                    case QueryByAttribute qba:
                        entityName = qba.EntityName;
                        break;
                }

                if (entityName != null)
                {
                    return new Tuple<object, string, Guid>(new EntityReference
                    {
                        LogicalName = entityName,
                        Id = Guid.Empty
                    }, entityName, Guid.Empty);
                }
            }

            if (obj is Entity entity)
            {
                return new Tuple<object, string, Guid>(obj, entity.LogicalName, entity.Id);
            }

            if (obj is EntityReference entityRef)
            {
                return new Tuple<object, string, Guid>(obj, entityRef.LogicalName, entityRef.Id);
            }

            if (obj is EntityCollection entityCollection)
            {
                return new Tuple<object, string, Guid>(obj, entityCollection.EntityName, Guid.Empty);
            }

            return null;
        }


        public Entity TryRetrieve(EntityReference reference)
        {
            return db.GetEntityOrNull(reference)?.CloneEntity();
        }

        #endregion

        internal void DisableRegisteredPlugins(bool include)
        {
            pluginManager.DisableRegisteredPlugins(include);
        }

        internal List<string> PluginRegistrations => pluginManager.PluginRegistrations;
        internal List<string> TemporaryPluginRegistrations => pluginManager.TemporaryPluginRegistrations;
        internal List<string> SystemPluginRegistrations => pluginManager.SystemPluginRegistrations;

        public XrmMockupSettings GetMockupSettings()
        {
            return settings;
        }

        internal void RegisterAdditionalPlugins(IEnumerable<Type> basePluginTypes, PluginRegistrationScope scope)
        {
            foreach (var type in basePluginTypes)
                pluginManager.RegisterAdditionalPlugin(type, metadata.EntityMetadata, metadata.Plugins, scope);
        }

        internal void TakeSnapshot(string snapshotName)
        {
            if (snapshots.ContainsKey(snapshotName))
                snapshots.Remove(snapshotName);
            var snapshot = new Snapshot()
            {
                db = this.db.Clone(),
                AdminUserRef = this.AdminUserRef,
                OrganizationId = this.OrganizationId,
                RootBusinessUnitRef = this.RootBusinessUnitRef,
                TimeOffset = this.TimeOffset,
                security = this.security.Clone(),
            };
            snapshots.Add(snapshotName, snapshot);
        }
        internal string TakeJsonSnapshot()
        {
            var jsonObj = new SnapshotDTO
            {
                Db = this.db.ToSerializableDTO(),
                AdminUserId = this.AdminUserRef.Id,
                OrganizationId = this.OrganizationId,
                RootBusinessUnitId = this.RootBusinessUnitRef.Id,
                TimeOffset = this.TimeOffset.Ticks,
                Security = this.security.ToSerializableDTO()
            };
            string jsonString = JsonSerializer.Serialize(jsonObj);
            return jsonString;
        }

        internal void RestoreJsonSnapshot(string json)
        {
            var jsonObj = JsonSerializer.Deserialize<SnapshotDTO>(json);
            this.db = XrmDb.RestoreSerializableDTO(this.db, jsonObj.Db);
            this.security = Security.RestoreSerializableDTO(this.security, jsonObj.Security);
            this.AdminUserRef = new EntityReference(this.AdminUserRef.LogicalName, jsonObj.AdminUserId);
            this.RootBusinessUnitRef = new EntityReference(this.RootBusinessUnitRef.LogicalName, jsonObj.RootBusinessUnitId);
            this.TimeOffset = new TimeSpan(jsonObj.TimeOffset);
            this.OrganizationId = jsonObj.OrganizationId;
            this.RequestHandlers = GetRequestHandlers(this.db);
        }

        internal void RestoreToSnapshot(string snapshotName)
        {
            if (!snapshots.ContainsKey(snapshotName))
                throw new KeyNotFoundException("A Snapshot with that name does not exist");
            var snapshot = snapshots[snapshotName];
            this.db = snapshot.db.Clone();
            this.security = snapshot.security.Clone();
            this.AdminUserRef = snapshot.AdminUserRef;
            this.RootBusinessUnitRef = snapshot.RootBusinessUnitRef;
            this.TimeOffset = snapshot.TimeOffset;
            this.OrganizationId = snapshot.OrganizationId;
            this.RequestHandlers = GetRequestHandlers(this.db);
        }

        internal void DeleteSnapshot(string snapshotName)
        {
            if (!snapshots.ContainsKey(snapshotName))
                throw new KeyNotFoundException("A Snapshot with that name does not exist");
            snapshots.Remove(snapshotName);
        }

        internal void ResetEnvironment()
        {
            this.TimeOffset = new TimeSpan();
            workflowManager.ResetWorkflows(settings.IncludeAllWorkflows);

            ClearPluginTraceLogs();
            pluginManager.ResetPlugins();
            this.db = new XrmDb(metadata.EntityMetadata, OnlineDataService);
            this.RequestHandlers = GetRequestHandlers(db);
            InitializeDB();
            security.ResetEnvironment(db);
        }

        public EntityMetadata GetEntityMetadata(string entityLogicalName)
        {
            if (!metadata.EntityMetadata.TryGetValue(entityLogicalName, out var entityMetadata))
            {
                throw new MockupException($"No EntityMetadata found for entity with logical name '{entityLogicalName}'");
            }

            return entityMetadata;
        }

        internal void ExecuteCalculatedFields(EntityMetadata entityMetadata, Entity entity, ISet<string> requestedAttributes = null)
        {
            // requestedAttributes == null means "all columns" (e.g. ColumnSet(true)); only filter when
            // the caller has supplied an explicit list. Skipping unrequested calculated columns avoids
            // running their formulas, which is both faster and prevents a broken/unused calculated
            // field from blocking unrelated reads (the original cast bug was surfaced this way: a
            // mixed-type Concat in one calc field blew up every Retrieve, even when the column was
            // never selected).
            var attributes = entityMetadata.Attributes.Where(
                m => m.SourceType == (int)SourceType.CalculatedAttribute && !(m is MoneyAttributeMetadata && m.LogicalName.EndsWith("_base")));
            if (requestedAttributes != null)
            {
                attributes = attributes.Where(m => requestedAttributes.Contains(m.LogicalName));
            }

            foreach (var attr in attributes)
            {
                var definition = Utility.GetFormulaDefinition(attr, SourceType.CalculatedAttribute);

                if (definition == null)
                {
                    var trace = ServiceFactory.GetService<ITracingService>();
                    trace.Trace($"Calculated field on {attr.EntityLogicalName} field {attr.LogicalName} is empty");
                    continue;
                }

                var tree = WorkflowConstructor.ParseCalculated(definition, $"{attr.EntityLogicalName}.{attr.LogicalName}");
                var factory = ServiceFactory;
                // Calculated fields are evaluated on demand and projected onto the entity being
                // returned. Unlike rollup fields they are never persisted to the database, so we run
                // the calculation workflow against a clone and copy the computed value back onto the
                // returned entity (the workflow leaves its result in the "primaryEntity" variable).
                var resultTree = tree.Execute(entity.CloneEntity(entityMetadata, new ColumnSet(true)), TimeOffset, GetWorkflowService(),
                    factory, factory.GetService<ITracingService>(), suppressWrites: true);

                if (resultTree.Variables.TryGetValue("InputEntities(\"primaryEntity\")", out var resultObj)
                    && resultObj is Entity result && result.Contains(attr.LogicalName))
                {
                    entity[attr.LogicalName] = result[attr.LogicalName];
                }
            }
        }

        internal async System.Threading.Tasks.Task ExecuteFormulaFields(EntityMetadata entityMetadata, Entity entity, ISet<string> requestedAttributes = null)
        {
            if (!settings.EnablePowerFxFields)
                return;

            var attributes = entityMetadata.Attributes.Where(m => m.SourceType == (int)SourceType.FormulaAttribute);
            if (requestedAttributes != null)
            {
                attributes = attributes.Where(m => requestedAttributes.Contains(m.LogicalName));
            }
            foreach (var attr in attributes)
            {
                var definition = Utility.GetFormulaDefinition(attr, SourceType.FormulaAttribute);

                if (definition == null)
                {
                    var trace = ServiceFactory.GetService<ITracingService>();
                    trace.Trace($"Formula field on {attr.EntityLogicalName} field {attr.LogicalName} is empty");
                    continue;
                }

                try
                {
                    entity[attr.LogicalName] = CoerceFormulaValue(await FormulaFieldEvaluator.Evaluate(definition, entity, TimeOffset), attr);
                }
                catch (Exception ex)
                {
                    // In Dataverse a formula field whose expression errors at runtime (e.g. referencing
                    // a blank source, like Mid(name, 3) on an empty name) surfaces as a blank value on
                    // that field — it does not fail the whole Retrieve. Mirror that: trace and leave the
                    // field unset rather than letting one formula column break reading the record.
                    var trace = ServiceFactory.GetService<ITracingService>();
                    trace.Trace($"Formula field on {attr.EntityLogicalName} field {attr.LogicalName} could not be evaluated: {ex.Message}");
                }
            }
        }

        // PowerFx evaluates numeric expressions to decimal. Coerce the result to the formula
        // attribute's declared CLR type so the strongly-typed accessors (e.g. int? for a whole-number
        // formula column) don't fail casting decimal to the target type.
        private static object CoerceFormulaValue(object value, AttributeMetadata attr)
        {
            if (value == null) return null;

            switch (attr.AttributeType)
            {
                case AttributeTypeCode.Integer:
                    return Convert.ToInt32(value);
                case AttributeTypeCode.BigInt:
                    return Convert.ToInt64(value);
                case AttributeTypeCode.Double:
                    return Convert.ToDouble(value);
                case AttributeTypeCode.Decimal:
                    return value is decimal ? value : Convert.ToDecimal(value);
                case AttributeTypeCode.Money:
                    return value is Money ? value : new Money(Convert.ToDecimal(value));
                default:
                    return value;
            }
        }

        internal void ResetTable(string tableName)
        {
            db.ResetTable(tableName);
        }

        internal SecurityRole GetSecurityRole(string roleName)
        {
            return security.GetSecurityRole(roleName);
        }

        internal void AddSecurityRole(SecurityRole role)
        {
            security.AddSecurityRole(role);
        }

        public void TriggerExtension(IOrganizationService service, OrganizationRequest request, Entity currentEntity,
            Entity preEntity, EntityReference userRef)
        {
            foreach (var mockUpExtension in settings.MockUpExtensions)
            {
                mockUpExtension.TriggerExtension(service, request, currentEntity, preEntity, userRef);
            }
        }
    }
}
