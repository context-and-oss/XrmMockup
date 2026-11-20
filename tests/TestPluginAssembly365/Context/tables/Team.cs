using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Collection of system users that routinely collaborate. Teams can be used to simplify record sharing and provide team members with common access to organization data when team members belong to different Business Units.</para>
/// <para>Display Name: Team</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("team")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Team : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "team";
    public const int EntityTypeCode = 9;

    public Team() : base(EntityLogicalName) { }
    public Team(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("name");

    [AttributeLogicalName("teamid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("teamid", value);
        }
    }

    /// <summary>
    /// <para>Unique identifier of the user primary responsible for the team.</para>
    /// <para>Display Name: Administrator</para>
    /// </summary>
    [AttributeLogicalName("administratorid")]
    [DisplayName("Administrator")]
    public EntityReference? AdministratorId
    {
        get => GetAttributeValue<EntityReference?>("administratorid");
        set => SetAttributeValue("administratorid", value);
    }

    /// <summary>
    /// <para>The object Id for a group.</para>
    /// <para>Display Name: Object Id for a group</para>
    /// </summary>
    [AttributeLogicalName("azureactivedirectoryobjectid")]
    [DisplayName("Object Id for a group")]
    public Guid? AzureActiveDirectoryObjectId
    {
        get => GetAttributeValue<Guid?>("azureactivedirectoryobjectid");
        set => SetAttributeValue("azureactivedirectoryobjectid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the business unit with which the team is associated.</para>
    /// <para>Display Name: Business Unit</para>
    /// </summary>
    [AttributeLogicalName("businessunitid")]
    [DisplayName("Business Unit")]
    public EntityReference? BusinessUnitId
    {
        get => GetAttributeValue<EntityReference?>("businessunitid");
        set => SetAttributeValue("businessunitid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the team.</para>
    /// <para>Display Name: Created By</para>
    /// </summary>
    [AttributeLogicalName("createdby")]
    [DisplayName("Created By")]
    public EntityReference? CreatedBy
    {
        get => GetAttributeValue<EntityReference?>("createdby");
        set => SetAttributeValue("createdby", value);
    }

    /// <summary>
    /// <para>Date and time when the team was created.</para>
    /// <para>Display Name: Created On</para>
    /// </summary>
    [AttributeLogicalName("createdon")]
    [DisplayName("Created On")]
    public DateTime? CreatedOn
    {
        get => GetAttributeValue<DateTime?>("createdon");
        set => SetAttributeValue("createdon", value);
    }

    /// <summary>
    /// <para>Unique identifier of the delegate user who created the team.</para>
    /// <para>Display Name: Created By (Delegate)</para>
    /// </summary>
    [AttributeLogicalName("createdonbehalfby")]
    [DisplayName("Created By (Delegate)")]
    public EntityReference? CreatedOnBehalfBy
    {
        get => GetAttributeValue<EntityReference?>("createdonbehalfby");
        set => SetAttributeValue("createdonbehalfby", value);
    }

    /// <summary>
    /// <para>The delegated authorization context for the team.</para>
    /// <para>Display Name: Delegated authorization</para>
    /// </summary>
    [AttributeLogicalName("delegatedauthorizationid")]
    [DisplayName("Delegated authorization")]
    public EntityReference? DelegatedAuthorizationId
    {
        get => GetAttributeValue<EntityReference?>("delegatedauthorizationid");
        set => SetAttributeValue("delegatedauthorizationid", value);
    }

    /// <summary>
    /// <para>Description of the team.</para>
    /// <para>Display Name: Description</para>
    /// </summary>
    [AttributeLogicalName("description")]
    [DisplayName("Description")]
    [MaxLength(2000)]
    public string? Description
    {
        get => GetAttributeValue<string?>("description");
        set => SetAttributeValue("description", value);
    }

    /// <summary>
    /// <para>Email address for the team.</para>
    /// <para>Display Name: Email</para>
    /// </summary>
    [AttributeLogicalName("emailaddress")]
    [DisplayName("Email")]
    [MaxLength(100)]
    public string? EMailAddress
    {
        get => GetAttributeValue<string?>("emailaddress");
        set => SetAttributeValue("emailaddress", value);
    }

    /// <summary>
    /// <para>Exchange rate for the currency associated with the team with respect to the base currency.</para>
    /// <para>Display Name: Exchange Rate</para>
    /// </summary>
    [AttributeLogicalName("exchangerate")]
    [DisplayName("Exchange Rate")]
    public decimal? ExchangeRate
    {
        get => GetAttributeValue<decimal?>("exchangerate");
        set => SetAttributeValue("exchangerate", value);
    }

    /// <summary>
    /// <para>Unique identifier of the data import or data migration that created this record.</para>
    /// <para>Display Name: Import Sequence Number</para>
    /// </summary>
    [AttributeLogicalName("importsequencenumber")]
    [DisplayName("Import Sequence Number")]
    [Range(-2147483648, 2147483647)]
    public int? ImportSequenceNumber
    {
        get => GetAttributeValue<int?>("importsequencenumber");
        set => SetAttributeValue("importsequencenumber", value);
    }

    /// <summary>
    /// <para>Information about whether the team is a default business unit team.</para>
    /// <para>Display Name: Is Default</para>
    /// </summary>
    [AttributeLogicalName("isdefault")]
    [DisplayName("Is Default")]
    public bool? IsDefault
    {
        get => GetAttributeValue<bool?>("isdefault");
        set => SetAttributeValue("isdefault", value);
    }

    /// <summary>
    /// <para>Display Name: issastokenset</para>
    /// </summary>
    [AttributeLogicalName("issastokenset")]
    [DisplayName("issastokenset")]
    public bool? IsSasTokenSet
    {
        get => GetAttributeValue<bool?>("issastokenset");
        set => SetAttributeValue("issastokenset", value);
    }

    /// <summary>
    /// <para>Display Name: Membership Type</para>
    /// </summary>
    [AttributeLogicalName("membershiptype")]
    [DisplayName("Membership Type")]
    public team_membershiptype? MembershipType
    {
        get => this.GetOptionSetValue<team_membershiptype>("membershiptype");
        set => this.SetOptionSetValue("membershiptype", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who last modified the team.</para>
    /// <para>Display Name: Modified By</para>
    /// </summary>
    [AttributeLogicalName("modifiedby")]
    [DisplayName("Modified By")]
    public EntityReference? ModifiedBy
    {
        get => GetAttributeValue<EntityReference?>("modifiedby");
        set => SetAttributeValue("modifiedby", value);
    }

    /// <summary>
    /// <para>Date and time when the team was last modified.</para>
    /// <para>Display Name: Modified On</para>
    /// </summary>
    [AttributeLogicalName("modifiedon")]
    [DisplayName("Modified On")]
    public DateTime? ModifiedOn
    {
        get => GetAttributeValue<DateTime?>("modifiedon");
        set => SetAttributeValue("modifiedon", value);
    }

    /// <summary>
    /// <para>Unique identifier of the delegate user who last modified the team.</para>
    /// <para>Display Name: Modified By (Delegate)</para>
    /// </summary>
    [AttributeLogicalName("modifiedonbehalfby")]
    [DisplayName("Modified By (Delegate)")]
    public EntityReference? ModifiedOnBehalfBy
    {
        get => GetAttributeValue<EntityReference?>("modifiedonbehalfby");
        set => SetAttributeValue("modifiedonbehalfby", value);
    }

    /// <summary>
    /// <para>Name of the team.</para>
    /// <para>Display Name: Team Name</para>
    /// </summary>
    [AttributeLogicalName("name")]
    [DisplayName("Team Name")]
    [MaxLength(160)]
    public string? Name
    {
        get => GetAttributeValue<string?>("name");
        set => SetAttributeValue("name", value);
    }

    /// <summary>
    /// <para>Unique identifier of the organization associated with the team.</para>
    /// <para>Display Name: Organization </para>
    /// </summary>
    [AttributeLogicalName("organizationid")]
    [DisplayName("Organization ")]
    public Guid? OrganizationId
    {
        get => GetAttributeValue<Guid?>("organizationid");
        set => SetAttributeValue("organizationid", value);
    }

    /// <summary>
    /// <para>Date and time that the record was migrated.</para>
    /// <para>Display Name: Record Created On</para>
    /// </summary>
    [AttributeLogicalName("overriddencreatedon")]
    [DisplayName("Record Created On")]
    public DateTime? OverriddenCreatedOn
    {
        get => GetAttributeValue<DateTime?>("overriddencreatedon");
        set => SetAttributeValue("overriddencreatedon", value);
    }

    /// <summary>
    /// <para>Shows the ID of the process.</para>
    /// <para>Display Name: Process</para>
    /// </summary>
    [AttributeLogicalName("processid")]
    [DisplayName("Process")]
    public Guid? ProcessId
    {
        get => GetAttributeValue<Guid?>("processid");
        set => SetAttributeValue("processid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the default queue for the team.</para>
    /// <para>Display Name: Default Queue</para>
    /// </summary>
    [AttributeLogicalName("queueid")]
    [DisplayName("Default Queue")]
    public EntityReference? QueueId
    {
        get => GetAttributeValue<EntityReference?>("queueid");
        set => SetAttributeValue("queueid", value);
    }

    /// <summary>
    /// <para>Choose the record that the team relates to.</para>
    /// <para>Display Name: Regarding Object Id</para>
    /// </summary>
    [AttributeLogicalName("regardingobjectid")]
    [DisplayName("Regarding Object Id")]
    public EntityReference? RegardingObjectId
    {
        get => GetAttributeValue<EntityReference?>("regardingobjectid");
        set => SetAttributeValue("regardingobjectid", value);
    }

    /// <summary>
    /// <para>Sas Token for Team.</para>
    /// <para>Display Name: Sas Token</para>
    /// </summary>
    [AttributeLogicalName("sastoken")]
    [DisplayName("Sas Token")]
    [MaxLength(50)]
    public string? SasToken
    {
        get => GetAttributeValue<string?>("sastoken");
        set => SetAttributeValue("sastoken", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Share Link Qualifier</para>
    /// </summary>
    [AttributeLogicalName("sharelinkqualifier")]
    [DisplayName("Share Link Qualifier")]
    [MaxLength(1250)]
    public string? ShareLinkQualifier
    {
        get => GetAttributeValue<string?>("sharelinkqualifier");
        set => SetAttributeValue("sharelinkqualifier", value);
    }

    /// <summary>
    /// <para>Shows the ID of the stage.</para>
    /// <para>Display Name: (Deprecated) Process Stage</para>
    /// </summary>
    [AttributeLogicalName("stageid")]
    [DisplayName("(Deprecated) Process Stage")]
    public Guid? StageId
    {
        get => GetAttributeValue<Guid?>("stageid");
        set => SetAttributeValue("stageid", value);
    }

    /// <summary>
    /// <para>Select whether the team will be managed by the system.</para>
    /// <para>Display Name: Is System Managed</para>
    /// </summary>
    [AttributeLogicalName("systemmanaged")]
    [DisplayName("Is System Managed")]
    public bool? SystemManaged
    {
        get => GetAttributeValue<bool?>("systemmanaged");
        set => SetAttributeValue("systemmanaged", value);
    }

    /// <summary>
    /// <para>Display Name: Team</para>
    /// </summary>
    [AttributeLogicalName("teamid")]
    [DisplayName("Team")]
    public Guid? TeamId
    {
        get => GetAttributeValue<Guid?>("teamid");
        set => SetId("teamid", value);
    }

    /// <summary>
    /// <para>Shows the team template that is associated with the team.</para>
    /// <para>Display Name: Team Template Identifier</para>
    /// </summary>
    [AttributeLogicalName("teamtemplateid")]
    [DisplayName("Team Template Identifier")]
    public EntityReference? TeamTemplateId
    {
        get => GetAttributeValue<EntityReference?>("teamtemplateid");
        set => SetAttributeValue("teamtemplateid", value);
    }

    /// <summary>
    /// <para>Select the team type.</para>
    /// <para>Display Name: Team Type</para>
    /// </summary>
    [AttributeLogicalName("teamtype")]
    [DisplayName("Team Type")]
    public team_type? TeamType
    {
        get => this.GetOptionSetValue<team_type>("teamtype");
        set => this.SetOptionSetValue("teamtype", value);
    }

    /// <summary>
    /// <para>Unique identifier of the currency associated with the team.</para>
    /// <para>Display Name: Currency</para>
    /// </summary>
    [AttributeLogicalName("transactioncurrencyid")]
    [DisplayName("Currency")]
    public EntityReference? TransactionCurrencyId
    {
        get => GetAttributeValue<EntityReference?>("transactioncurrencyid");
        set => SetAttributeValue("transactioncurrencyid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: (Deprecated) Traversed Path</para>
    /// </summary>
    [AttributeLogicalName("traversedpath")]
    [DisplayName("(Deprecated) Traversed Path")]
    [MaxLength(1250)]
    public string? TraversedPath
    {
        get => GetAttributeValue<string?>("traversedpath");
        set => SetAttributeValue("traversedpath", value);
    }

    /// <summary>
    /// <para>Version number of the team.</para>
    /// <para>Display Name: Version number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    /// <summary>
    /// <para>Pronunciation of the full name of the team, written in phonetic hiragana or katakana characters.</para>
    /// <para>Display Name: Yomi Name</para>
    /// </summary>
    [AttributeLogicalName("yominame")]
    [DisplayName("Yomi Name")]
    [MaxLength(160)]
    public string? YomiName
    {
        get => GetAttributeValue<string?>("yominame");
        set => SetAttributeValue("yominame", value);
    }

    [AttributeLogicalName("businessunitid")]
    [RelationshipSchemaName("business_unit_teams")]
    [RelationshipMetadata("ManyToOne", "businessunitid", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_teams
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_teams", null);
        set => SetRelatedEntity("business_unit_teams", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_team_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_team_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_team_createdonbehalfby", null);
        set => SetRelatedEntity("lk_team_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_team_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_team_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_team_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_team_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("administratorid")]
    [RelationshipSchemaName("lk_teambase_administratorid")]
    [RelationshipMetadata("ManyToOne", "administratorid", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_teambase_administratorid
    {
        get => GetRelatedEntity<SystemUser>("lk_teambase_administratorid", null);
        set => SetRelatedEntity("lk_teambase_administratorid", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_teambase_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_teambase_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_teambase_createdby", null);
        set => SetRelatedEntity("lk_teambase_createdby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_teambase_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_teambase_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_teambase_modifiedby", null);
        set => SetRelatedEntity("lk_teambase_modifiedby", null, value);
    }

    [RelationshipSchemaName("team_accounts")]
    [RelationshipMetadata("OneToMany", "teamid", "account", "owningteam", "Referenced")]
    public IEnumerable<Account> team_accounts
    {
        get => GetRelatedEntities<Account>("team_accounts", null);
        set => SetRelatedEntities("team_accounts", null, value);
    }

    [RelationshipSchemaName("team_activity")]
    [RelationshipMetadata("OneToMany", "teamid", "activitypointer", "owningteam", "Referenced")]
    public IEnumerable<ActivityPointer> team_activity
    {
        get => GetRelatedEntities<ActivityPointer>("team_activity", null);
        set => SetRelatedEntities("team_activity", null, value);
    }

    [RelationshipSchemaName("team_contacts")]
    [RelationshipMetadata("OneToMany", "teamid", "contact", "owningteam", "Referenced")]
    public IEnumerable<Contact> team_contacts
    {
        get => GetRelatedEntities<Contact>("team_contacts", null);
        set => SetRelatedEntities("team_contacts", null, value);
    }

    [RelationshipSchemaName("team_ctx_child")]
    [RelationshipMetadata("OneToMany", "teamid", "ctx_child", "owningteam", "Referenced")]
    public IEnumerable<ctx_child> team_ctx_child
    {
        get => GetRelatedEntities<ctx_child>("team_ctx_child", null);
        set => SetRelatedEntities("team_ctx_child", null, value);
    }

    [RelationshipSchemaName("team_ctx_parent")]
    [RelationshipMetadata("OneToMany", "teamid", "ctx_parent", "owningteam", "Referenced")]
    public IEnumerable<ctx_parent> team_ctx_parent
    {
        get => GetRelatedEntities<ctx_parent>("team_ctx_parent", null);
        set => SetRelatedEntities("team_ctx_parent", null, value);
    }

    [RelationshipSchemaName("team_email")]
    [RelationshipMetadata("OneToMany", "teamid", "email", "owningteam", "Referenced")]
    public IEnumerable<Email> team_email
    {
        get => GetRelatedEntities<Email>("team_email", null);
        set => SetRelatedEntities("team_email", null, value);
    }

    [RelationshipSchemaName("team_email_templates")]
    [RelationshipMetadata("OneToMany", "teamid", "template", "owningteam", "Referenced")]
    public IEnumerable<Template> team_email_templates
    {
        get => GetRelatedEntities<Template>("team_email_templates", null);
        set => SetRelatedEntities("team_email_templates", null, value);
    }

    [RelationshipSchemaName("team_fax")]
    [RelationshipMetadata("OneToMany", "teamid", "fax", "owningteam", "Referenced")]
    public IEnumerable<Fax> team_fax
    {
        get => GetRelatedEntities<Fax>("team_fax", null);
        set => SetRelatedEntities("team_fax", null, value);
    }

    [RelationshipSchemaName("team_task")]
    [RelationshipMetadata("OneToMany", "teamid", "task", "owningteam", "Referenced")]
    public IEnumerable<Task> team_task
    {
        get => GetRelatedEntities<Task>("team_task", null);
        set => SetRelatedEntities("team_task", null, value);
    }

    [RelationshipSchemaName("teammembership_association")]
    [RelationshipMetadata("ManyToMany", "teamid", "systemuser", "systemuserid", "Entity1")]
    public IEnumerable<SystemUser> teammembership_association
    {
        get => GetRelatedEntities<SystemUser>("teammembership_association", null);
        set => SetRelatedEntities("teammembership_association", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_Team")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_Team
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_Team", null);
        set => SetRelatedEntity("TransactionCurrency_Team", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Team entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Team, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Team with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Team to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Team</returns>
    public static Team Retrieve(IOrganizationService service, Guid id, params Expression<Func<Team, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }

    /// <summary>
    /// Retrieves the Team using the ObjectId with MembershipType alternate key.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="AzureActiveDirectoryObjectId">AzureActiveDirectoryObjectId key value</param>
    /// <param name="MembershipType">MembershipType key value</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Team</returns>
    public static Team Retrieve_aadobjectid_membershiptype(IOrganizationService service, Guid AzureActiveDirectoryObjectId, team_membershiptype MembershipType, params Expression<Func<Team, object>>[] columns)
    {
        var keyedEntityReference = new EntityReference(EntityLogicalName, new KeyAttributeCollection
        {
            ["azureactivedirectoryobjectid"] = AzureActiveDirectoryObjectId,
            ["membershiptype"] = MembershipType,
        });

        return service.Retrieve(keyedEntityReference, columns);
    }
}
