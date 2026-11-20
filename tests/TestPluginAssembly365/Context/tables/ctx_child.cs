using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Display Name: Child</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("ctx_child")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class ctx_child : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "ctx_child";
    public const int EntityTypeCode = 10751;

    public ctx_child() : base(EntityLogicalName) { }
    public ctx_child(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("ctx_name");

    [AttributeLogicalName("ctx_childid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("ctx_childid", value);
        }
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the record.</para>
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
    /// <para>Date and time when the record was created.</para>
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
    /// <para>Unique identifier of the delegate user who created the record.</para>
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
    /// <para>Display Name: Allowance</para>
    /// </summary>
    [AttributeLogicalName("ctx_allowance")]
    [DisplayName("Allowance")]
    public decimal? ctx_Allowance
    {
        get => this.GetMoneyValue("ctx_allowance");
        set => this.SetMoneyValue("ctx_allowance", value);
    }

    /// <summary>
    /// <para>Value of the Allowance in base currency.</para>
    /// <para>Display Name: Allowance (Base)</para>
    /// </summary>
    [AttributeLogicalName("ctx_allowance_base")]
    [DisplayName("Allowance (Base)")]
    public decimal? ctx_allowance_Base
    {
        get => this.GetMoneyValue("ctx_allowance_base");
        set => this.SetMoneyValue("ctx_allowance_base", value);
    }

    /// <summary>
    /// <para>Display Name: Child</para>
    /// </summary>
    [AttributeLogicalName("ctx_childid")]
    [DisplayName("Child")]
    public Guid? ctx_childId
    {
        get => GetAttributeValue<Guid?>("ctx_childid");
        set => SetId("ctx_childid", value);
    }

    /// <summary>
    /// <para>Display Name: Name</para>
    /// </summary>
    [AttributeLogicalName("ctx_name")]
    [DisplayName("Name")]
    [MaxLength(200)]
    public string? ctx_Name
    {
        get => GetAttributeValue<string?>("ctx_name");
        set => SetAttributeValue("ctx_name", value);
    }

    /// <summary>
    /// <para>Display Name: Parent</para>
    /// </summary>
    [AttributeLogicalName("ctx_parentid")]
    [DisplayName("Parent")]
    public EntityReference? ctx_ParentId
    {
        get => GetAttributeValue<EntityReference?>("ctx_parentid");
        set => SetAttributeValue("ctx_parentid", value);
    }

    /// <summary>
    /// <para>Display Name: Rollup parent</para>
    /// </summary>
    [AttributeLogicalName("ctx_rollupparentid")]
    [DisplayName("Rollup parent")]
    public EntityReference? ctx_RollupParentId
    {
        get => GetAttributeValue<EntityReference?>("ctx_rollupparentid");
        set => SetAttributeValue("ctx_rollupparentid", value);
    }

    /// <summary>
    /// <para>Exchange rate for the currency associated with the entity with respect to the base currency.</para>
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
    /// <para>Sequence number of the import that created this record.</para>
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
    /// <para>Unique identifier of the user who modified the record.</para>
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
    /// <para>Date and time when the record was modified.</para>
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
    /// <para>Unique identifier of the delegate user who modified the record.</para>
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
    /// <para>Owner Id</para>
    /// <para>Display Name: Owner</para>
    /// </summary>
    [AttributeLogicalName("ownerid")]
    [DisplayName("Owner")]
    public EntityReference? OwnerId
    {
        get => GetAttributeValue<EntityReference?>("ownerid");
        set => SetAttributeValue("ownerid", value);
    }

    /// <summary>
    /// <para>Unique identifier for the business unit that owns the record</para>
    /// <para>Display Name: Owning Business Unit</para>
    /// </summary>
    [AttributeLogicalName("owningbusinessunit")]
    [DisplayName("Owning Business Unit")]
    public EntityReference? OwningBusinessUnit
    {
        get => GetAttributeValue<EntityReference?>("owningbusinessunit");
        set => SetAttributeValue("owningbusinessunit", value);
    }

    /// <summary>
    /// <para>Unique identifier for the team that owns the record.</para>
    /// <para>Display Name: Owning Team</para>
    /// </summary>
    [AttributeLogicalName("owningteam")]
    [DisplayName("Owning Team")]
    public EntityReference? OwningTeam
    {
        get => GetAttributeValue<EntityReference?>("owningteam");
        set => SetAttributeValue("owningteam", value);
    }

    /// <summary>
    /// <para>Unique identifier for the user that owns the record.</para>
    /// <para>Display Name: Owning User</para>
    /// </summary>
    [AttributeLogicalName("owninguser")]
    [DisplayName("Owning User")]
    public EntityReference? OwningUser
    {
        get => GetAttributeValue<EntityReference?>("owninguser");
        set => SetAttributeValue("owninguser", value);
    }

    /// <summary>
    /// <para>Status of the Child</para>
    /// <para>Display Name: Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Status")]
    public ctx_child_statecode? statecode
    {
        get => this.GetOptionSetValue<ctx_child_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Reason for the status of the Child</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public ctx_child_statuscode? statuscode
    {
        get => this.GetOptionSetValue<ctx_child_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Time Zone Rule Version Number</para>
    /// </summary>
    [AttributeLogicalName("timezoneruleversionnumber")]
    [DisplayName("Time Zone Rule Version Number")]
    [Range(-1, 2147483647)]
    public int? TimeZoneRuleVersionNumber
    {
        get => GetAttributeValue<int?>("timezoneruleversionnumber");
        set => SetAttributeValue("timezoneruleversionnumber", value);
    }

    /// <summary>
    /// <para>Unique identifier of the currency associated with the entity.</para>
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
    /// <para>Time zone code that was in use when the record was created.</para>
    /// <para>Display Name: UTC Conversion Time Zone Code</para>
    /// </summary>
    [AttributeLogicalName("utcconversiontimezonecode")]
    [DisplayName("UTC Conversion Time Zone Code")]
    [Range(-1, 2147483647)]
    public int? UTCConversionTimeZoneCode
    {
        get => GetAttributeValue<int?>("utcconversiontimezonecode");
        set => SetAttributeValue("utcconversiontimezonecode", value);
    }

    /// <summary>
    /// <para>Version Number</para>
    /// <para>Display Name: Version Number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version Number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    [AttributeLogicalName("owningbusinessunit")]
    [RelationshipSchemaName("business_unit_ctx_child")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_ctx_child
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_ctx_child", null);
        set => SetRelatedEntity("business_unit_ctx_child", null, value);
    }

    [RelationshipSchemaName("ctx_parent_child")]
    [RelationshipMetadata("ManyToMany", "ctx_childid", "ctx_parent", "ctx_parentid", "Entity2")]
    public IEnumerable<ctx_parent> ctx_parent_child
    {
        get => GetRelatedEntities<ctx_parent>("ctx_parent_child", null);
        set => SetRelatedEntities("ctx_parent_child", null, value);
    }

    [AttributeLogicalName("ctx_parentid")]
    [RelationshipSchemaName("ctx_parent_ctx_child_cascade")]
    [RelationshipMetadata("ManyToOne", "ctx_parentid", "ctx_parent", "ctx_parentid", "Referencing")]
    public ctx_parent ctx_parent_ctx_child_cascade
    {
        get => GetRelatedEntity<ctx_parent>("ctx_parent_ctx_child_cascade", null);
        set => SetRelatedEntity("ctx_parent_ctx_child_cascade", null, value);
    }

    [AttributeLogicalName("ctx_rollupparentid")]
    [RelationshipSchemaName("ctx_parent_ctx_child_rollup")]
    [RelationshipMetadata("ManyToOne", "ctx_rollupparentid", "ctx_parent", "ctx_parentid", "Referencing")]
    public ctx_parent ctx_parent_ctx_child_rollup
    {
        get => GetRelatedEntity<ctx_parent>("ctx_parent_ctx_child_rollup", null);
        set => SetRelatedEntity("ctx_parent_ctx_child_rollup", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_ctx_child_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_child_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_child_createdby", null);
        set => SetRelatedEntity("lk_ctx_child_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_ctx_child_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_child_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_child_createdonbehalfby", null);
        set => SetRelatedEntity("lk_ctx_child_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_ctx_child_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_child_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_child_modifiedby", null);
        set => SetRelatedEntity("lk_ctx_child_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_ctx_child_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_child_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_child_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_ctx_child_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_ctx_child")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_ctx_child
    {
        get => GetRelatedEntity<Team>("team_ctx_child", null);
        set => SetRelatedEntity("team_ctx_child", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_ctx_child")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_ctx_child
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_ctx_child", null);
        set => SetRelatedEntity("TransactionCurrency_ctx_child", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("user_ctx_child")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser user_ctx_child
    {
        get => GetRelatedEntity<SystemUser>("user_ctx_child", null);
        set => SetRelatedEntity("user_ctx_child", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the ctx_child entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<ctx_child, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the ctx_child with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of ctx_child to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved ctx_child</returns>
    public static ctx_child Retrieve(IOrganizationService service, Guid id, params Expression<Func<ctx_child, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
