using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Display Name: Parent</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("ctx_parent")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class ctx_parent : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "ctx_parent";
    public const int EntityTypeCode = 10750;

    public ctx_parent() : base(EntityLogicalName) { }
    public ctx_parent(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("ctx_name");

    [AttributeLogicalName("ctx_parentid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("ctx_parentid", value);
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
    /// <para>Display Name: Account</para>
    /// </summary>
    [AttributeLogicalName("ctx_accountid")]
    [DisplayName("Account")]
    public EntityReference? ctx_AccountId
    {
        get => GetAttributeValue<EntityReference?>("ctx_accountid");
        set => SetAttributeValue("ctx_accountid", value);
    }

    /// <summary>
    /// <para>Display Name: Amount</para>
    /// </summary>
    [AttributeLogicalName("ctx_amount")]
    [DisplayName("Amount")]
    public decimal? ctx_Amount
    {
        get => this.GetMoneyValue("ctx_amount");
        set => this.SetMoneyValue("ctx_amount", value);
    }

    /// <summary>
    /// <para>Value of the Amount in base currency.</para>
    /// <para>Display Name: Amount (Base)</para>
    /// </summary>
    [AttributeLogicalName("ctx_amount_base")]
    [DisplayName("Amount (Base)")]
    public decimal? ctx_amount_Base
    {
        get => this.GetMoneyValue("ctx_amount_base");
        set => this.SetMoneyValue("ctx_amount_base", value);
    }

    /// <summary>
    /// <para>Display Name: Amount calculated</para>
    /// </summary>
    [AttributeLogicalName("ctx_amountcalc")]
    [DisplayName("Amount calculated")]
    public decimal? ctx_AmountCalc
    {
        get => GetAttributeValue<decimal?>("ctx_amountcalc");
        set => SetAttributeValue("ctx_amountcalc", value);
    }

    /// <summary>
    /// <para>Display Name: Amount calculated (classic)</para>
    /// </summary>
    [AttributeLogicalName("ctx_amountcalcclassic")]
    [DisplayName("Amount calculated (classic)")]
    public decimal? ctx_AmountCalcClassic
    {
        get => this.GetMoneyValue("ctx_amountcalcclassic");
        set => this.SetMoneyValue("ctx_amountcalcclassic", value);
    }

    /// <summary>
    /// <para>Value of the Amount calculated (classic) in base currency.</para>
    /// <para>Display Name: Amount calculated (classic) (Base)</para>
    /// </summary>
    [AttributeLogicalName("ctx_amountcalcclassic_base")]
    [DisplayName("Amount calculated (classic) (Base)")]
    public decimal? ctx_amountcalcclassic_Base
    {
        get => this.GetMoneyValue("ctx_amountcalcclassic_base");
        set => this.SetMoneyValue("ctx_amountcalcclassic_base", value);
    }

    /// <summary>
    /// <para>Display Name: Avg allowance</para>
    /// </summary>
    [AttributeLogicalName("ctx_avgallowance")]
    [DisplayName("Avg allowance")]
    public decimal? ctx_AvgAllowance
    {
        get => this.GetMoneyValue("ctx_avgallowance");
        set => this.SetMoneyValue("ctx_avgallowance", value);
    }

    /// <summary>
    /// <para>Value of the Avg allowance in base currency.</para>
    /// <para>Display Name: Avg allowance (Base)</para>
    /// </summary>
    [AttributeLogicalName("ctx_avgallowance_base")]
    [DisplayName("Avg allowance (Base)")]
    public decimal? ctx_avgallowance_Base
    {
        get => this.GetMoneyValue("ctx_avgallowance_base");
        set => this.SetMoneyValue("ctx_avgallowance_base", value);
    }

    /// <summary>
    /// <para>ctx_AvgAllowance (CountAverageDescription)</para>
    /// <para>Display Name: ctx_AvgAllowance (CountAverage)</para>
    /// </summary>
    [AttributeLogicalName("ctx_avgallowance_count")]
    [DisplayName("ctx_AvgAllowance (CountAverage)")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_AvgAllowance_Count
    {
        get => GetAttributeValue<int?>("ctx_avgallowance_count");
        set => SetAttributeValue("ctx_avgallowance_count", value);
    }

    /// <summary>
    /// <para>Last Updated time of rollup field Avg allowance.</para>
    /// <para>Display Name: Avg allowance (Last Updated On)</para>
    /// </summary>
    [AttributeLogicalName("ctx_avgallowance_date")]
    [DisplayName("Avg allowance (Last Updated On)")]
    public DateTime? ctx_AvgAllowance_Date
    {
        get => GetAttributeValue<DateTime?>("ctx_avgallowance_date");
        set => SetAttributeValue("ctx_avgallowance_date", value);
    }

    /// <summary>
    /// <para>State of rollup field Avg allowance.</para>
    /// <para>Display Name: Avg allowance (State)</para>
    /// </summary>
    [AttributeLogicalName("ctx_avgallowance_state")]
    [DisplayName("Avg allowance (State)")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_AvgAllowance_State
    {
        get => GetAttributeValue<int?>("ctx_avgallowance_state");
        set => SetAttributeValue("ctx_avgallowance_state", value);
    }

    /// <summary>
    /// <para>ctx_AvgAllowance (SumAverageDescription)</para>
    /// <para>Display Name: ctx_AvgAllowance (SumAverage)</para>
    /// </summary>
    [AttributeLogicalName("ctx_avgallowance_sum")]
    [DisplayName("ctx_AvgAllowance (SumAverage)")]
    public decimal? ctx_AvgAllowance_Sum
    {
        get => this.GetMoneyValue("ctx_avgallowance_sum");
        set => this.SetMoneyValue("ctx_avgallowance_sum", value);
    }

    /// <summary>
    /// <para>Display Name: Contact</para>
    /// </summary>
    [AttributeLogicalName("ctx_contactid")]
    [DisplayName("Contact")]
    public EntityReference? ctx_ContactId
    {
        get => GetAttributeValue<EntityReference?>("ctx_contactid");
        set => SetAttributeValue("ctx_contactid", value);
    }

    /// <summary>
    /// <para>Display Name: Date calculated</para>
    /// </summary>
    [AttributeLogicalName("ctx_datecalc")]
    [DisplayName("Date calculated")]
    public DateTime? ctx_DateCalc
    {
        get => GetAttributeValue<DateTime?>("ctx_datecalc");
        set => SetAttributeValue("ctx_datecalc", value);
    }

    /// <summary>
    /// <para>Display Name: Date value</para>
    /// </summary>
    [AttributeLogicalName("ctx_datevalue")]
    [DisplayName("Date value")]
    public DateTime? ctx_DateValue
    {
        get => GetAttributeValue<DateTime?>("ctx_datevalue");
        set => SetAttributeValue("ctx_datevalue", value);
    }

    /// <summary>
    /// <para>Display Name: Document types</para>
    /// </summary>
    [AttributeLogicalName("ctx_documenttypes")]
    [DisplayName("Document types")]
    public IEnumerable<ctx_parent_ctx_documenttypes> ctx_Documenttypes
    {
        get => this.GetOptionSetCollectionValue<ctx_parent_ctx_documenttypes>("ctx_documenttypes");
        set => this.SetOptionSetCollectionValue("ctx_documenttypes", value);
    }

    /// <summary>
    /// <para>Display Name: Industry</para>
    /// </summary>
    [AttributeLogicalName("ctx_industrycode")]
    [DisplayName("Industry")]
    public ctx_parent_ctx_industrycode? ctx_Industrycode
    {
        get => this.GetOptionSetValue<ctx_parent_ctx_industrycode>("ctx_industrycode");
        set => this.SetOptionSetValue("ctx_industrycode", value);
    }

    /// <summary>
    /// <para>Display Name: Max allowance</para>
    /// </summary>
    [AttributeLogicalName("ctx_maxallowance")]
    [DisplayName("Max allowance")]
    public decimal? ctx_MaxAllowance
    {
        get => this.GetMoneyValue("ctx_maxallowance");
        set => this.SetMoneyValue("ctx_maxallowance", value);
    }

    /// <summary>
    /// <para>Value of the Max allowance in base currency.</para>
    /// <para>Display Name: Max allowance (Base)</para>
    /// </summary>
    [AttributeLogicalName("ctx_maxallowance_base")]
    [DisplayName("Max allowance (Base)")]
    public decimal? ctx_maxallowance_Base
    {
        get => this.GetMoneyValue("ctx_maxallowance_base");
        set => this.SetMoneyValue("ctx_maxallowance_base", value);
    }

    /// <summary>
    /// <para>Last Updated time of rollup field Max allowance.</para>
    /// <para>Display Name: Max allowance (Last Updated On)</para>
    /// </summary>
    [AttributeLogicalName("ctx_maxallowance_date")]
    [DisplayName("Max allowance (Last Updated On)")]
    public DateTime? ctx_MaxAllowance_Date
    {
        get => GetAttributeValue<DateTime?>("ctx_maxallowance_date");
        set => SetAttributeValue("ctx_maxallowance_date", value);
    }

    /// <summary>
    /// <para>State of rollup field Max allowance.</para>
    /// <para>Display Name: Max allowance (State)</para>
    /// </summary>
    [AttributeLogicalName("ctx_maxallowance_state")]
    [DisplayName("Max allowance (State)")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_MaxAllowance_State
    {
        get => GetAttributeValue<int?>("ctx_maxallowance_state");
        set => SetAttributeValue("ctx_maxallowance_state", value);
    }

    /// <summary>
    /// <para>Display Name: Min allowance</para>
    /// </summary>
    [AttributeLogicalName("ctx_minallowance")]
    [DisplayName("Min allowance")]
    public decimal? ctx_MinAllowance
    {
        get => this.GetMoneyValue("ctx_minallowance");
        set => this.SetMoneyValue("ctx_minallowance", value);
    }

    /// <summary>
    /// <para>Value of the Min allowance in base currency.</para>
    /// <para>Display Name: Min allowance (Base)</para>
    /// </summary>
    [AttributeLogicalName("ctx_minallowance_base")]
    [DisplayName("Min allowance (Base)")]
    public decimal? ctx_minallowance_Base
    {
        get => this.GetMoneyValue("ctx_minallowance_base");
        set => this.SetMoneyValue("ctx_minallowance_base", value);
    }

    /// <summary>
    /// <para>Last Updated time of rollup field Min allowance.</para>
    /// <para>Display Name: Min allowance (Last Updated On)</para>
    /// </summary>
    [AttributeLogicalName("ctx_minallowance_date")]
    [DisplayName("Min allowance (Last Updated On)")]
    public DateTime? ctx_MinAllowance_Date
    {
        get => GetAttributeValue<DateTime?>("ctx_minallowance_date");
        set => SetAttributeValue("ctx_minallowance_date", value);
    }

    /// <summary>
    /// <para>State of rollup field Min allowance.</para>
    /// <para>Display Name: Min allowance (State)</para>
    /// </summary>
    [AttributeLogicalName("ctx_minallowance_state")]
    [DisplayName("Min allowance (State)")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_MinAllowance_State
    {
        get => GetAttributeValue<int?>("ctx_minallowance_state");
        set => SetAttributeValue("ctx_minallowance_state", value);
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
    public Guid? ctx_parentId
    {
        get => GetAttributeValue<Guid?>("ctx_parentid");
        set => SetId("ctx_parentid", value);
    }

    /// <summary>
    /// <para>Display Name: Postal code</para>
    /// </summary>
    [AttributeLogicalName("ctx_postalcode")]
    [DisplayName("Postal code")]
    [MaxLength(40)]
    public string? ctx_Postalcode
    {
        get => GetAttributeValue<string?>("ctx_postalcode");
        set => SetAttributeValue("ctx_postalcode", value);
    }

    /// <summary>
    /// <para>Display Name: Score</para>
    /// </summary>
    [AttributeLogicalName("ctx_score")]
    [DisplayName("Score")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_Score
    {
        get => GetAttributeValue<int?>("ctx_score");
        set => SetAttributeValue("ctx_score", value);
    }

    /// <summary>
    /// <para>Display Name: Total allowance</para>
    /// </summary>
    [AttributeLogicalName("ctx_totalallowance")]
    [DisplayName("Total allowance")]
    public decimal? ctx_TotalAllowance
    {
        get => this.GetMoneyValue("ctx_totalallowance");
        set => this.SetMoneyValue("ctx_totalallowance", value);
    }

    /// <summary>
    /// <para>Value of the Total allowance in base currency.</para>
    /// <para>Display Name: Total allowance (Base)</para>
    /// </summary>
    [AttributeLogicalName("ctx_totalallowance_base")]
    [DisplayName("Total allowance (Base)")]
    public decimal? ctx_totalallowance_Base
    {
        get => this.GetMoneyValue("ctx_totalallowance_base");
        set => this.SetMoneyValue("ctx_totalallowance_base", value);
    }

    /// <summary>
    /// <para>Last Updated time of rollup field Total allowance.</para>
    /// <para>Display Name: Total allowance (Last Updated On)</para>
    /// </summary>
    [AttributeLogicalName("ctx_totalallowance_date")]
    [DisplayName("Total allowance (Last Updated On)")]
    public DateTime? ctx_TotalAllowance_Date
    {
        get => GetAttributeValue<DateTime?>("ctx_totalallowance_date");
        set => SetAttributeValue("ctx_totalallowance_date", value);
    }

    /// <summary>
    /// <para>State of rollup field Total allowance.</para>
    /// <para>Display Name: Total allowance (State)</para>
    /// </summary>
    [AttributeLogicalName("ctx_totalallowance_state")]
    [DisplayName("Total allowance (State)")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_TotalAllowance_State
    {
        get => GetAttributeValue<int?>("ctx_totalallowance_state");
        set => SetAttributeValue("ctx_totalallowance_state", value);
    }

    /// <summary>
    /// <para>Display Name: Trim left</para>
    /// </summary>
    [AttributeLogicalName("ctx_trimleft")]
    [DisplayName("Trim left")]
    [MaxLength(4000)]
    public string? ctx_TrimLeft
    {
        get => GetAttributeValue<string?>("ctx_trimleft");
        set => SetAttributeValue("ctx_trimleft", value);
    }

    /// <summary>
    /// <para>Display Name: Whole number</para>
    /// </summary>
    [AttributeLogicalName("ctx_wholenumber")]
    [DisplayName("Whole number")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_WholeNumber
    {
        get => GetAttributeValue<int?>("ctx_wholenumber");
        set => SetAttributeValue("ctx_wholenumber", value);
    }

    /// <summary>
    /// <para>Display Name: Whole number calculated</para>
    /// </summary>
    [AttributeLogicalName("ctx_wholenumbercalc")]
    [DisplayName("Whole number calculated")]
    [Range(-2147483648, 2147483647)]
    public int? ctx_WholeNumberCalc
    {
        get => GetAttributeValue<int?>("ctx_wholenumbercalc");
        set => SetAttributeValue("ctx_wholenumbercalc", value);
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
    /// <para>Status of the Parent</para>
    /// <para>Display Name: Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Status")]
    public ctx_parent_statecode? statecode
    {
        get => this.GetOptionSetValue<ctx_parent_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Reason for the status of the Parent</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public ctx_parent_statuscode? statuscode
    {
        get => this.GetOptionSetValue<ctx_parent_statuscode>("statuscode");
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
    [RelationshipSchemaName("business_unit_ctx_parent")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_ctx_parent
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_ctx_parent", null);
        set => SetRelatedEntity("business_unit_ctx_parent", null, value);
    }

    [AttributeLogicalName("ctx_accountid")]
    [RelationshipSchemaName("ctx_account_ctx_parent")]
    [RelationshipMetadata("ManyToOne", "ctx_accountid", "account", "accountid", "Referencing")]
    public Account ctx_account_ctx_parent
    {
        get => GetRelatedEntity<Account>("ctx_account_ctx_parent", null);
        set => SetRelatedEntity("ctx_account_ctx_parent", null, value);
    }

    [AttributeLogicalName("ctx_contactid")]
    [RelationshipSchemaName("ctx_contact_ctx_parent")]
    [RelationshipMetadata("ManyToOne", "ctx_contactid", "contact", "contactid", "Referencing")]
    public Contact ctx_contact_ctx_parent
    {
        get => GetRelatedEntity<Contact>("ctx_contact_ctx_parent", null);
        set => SetRelatedEntity("ctx_contact_ctx_parent", null, value);
    }

    [RelationshipSchemaName("ctx_parent_child")]
    [RelationshipMetadata("ManyToMany", "ctx_parentid", "ctx_child", "ctx_childid", "Entity1")]
    public IEnumerable<ctx_child> ctx_parent_child
    {
        get => GetRelatedEntities<ctx_child>("ctx_parent_child", null);
        set => SetRelatedEntities("ctx_parent_child", null, value);
    }

    [RelationshipSchemaName("ctx_parent_ctx_child_cascade")]
    [RelationshipMetadata("OneToMany", "ctx_parentid", "ctx_child", "ctx_parentid", "Referenced")]
    public IEnumerable<ctx_child> ctx_parent_ctx_child_cascade
    {
        get => GetRelatedEntities<ctx_child>("ctx_parent_ctx_child_cascade", null);
        set => SetRelatedEntities("ctx_parent_ctx_child_cascade", null, value);
    }

    [RelationshipSchemaName("ctx_parent_ctx_child_rollup")]
    [RelationshipMetadata("OneToMany", "ctx_parentid", "ctx_child", "ctx_rollupparentid", "Referenced")]
    public IEnumerable<ctx_child> ctx_parent_ctx_child_rollup
    {
        get => GetRelatedEntities<ctx_child>("ctx_parent_ctx_child_rollup", null);
        set => SetRelatedEntities("ctx_parent_ctx_child_rollup", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_ctx_parent_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_parent_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_parent_createdby", null);
        set => SetRelatedEntity("lk_ctx_parent_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_ctx_parent_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_parent_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_parent_createdonbehalfby", null);
        set => SetRelatedEntity("lk_ctx_parent_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_ctx_parent_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_parent_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_parent_modifiedby", null);
        set => SetRelatedEntity("lk_ctx_parent_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_ctx_parent_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_ctx_parent_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_ctx_parent_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_ctx_parent_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_ctx_parent")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_ctx_parent
    {
        get => GetRelatedEntity<Team>("team_ctx_parent", null);
        set => SetRelatedEntity("team_ctx_parent", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_ctx_parent")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_ctx_parent
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_ctx_parent", null);
        set => SetRelatedEntity("TransactionCurrency_ctx_parent", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("user_ctx_parent")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser user_ctx_parent
    {
        get => GetRelatedEntity<SystemUser>("user_ctx_parent", null);
        set => SetRelatedEntity("user_ctx_parent", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the ctx_parent entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<ctx_parent, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the ctx_parent with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of ctx_parent to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved ctx_parent</returns>
    public static ctx_parent Retrieve(IOrganizationService service, Guid id, params Expression<Func<ctx_parent, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }

    /// <summary>
    /// Retrieves the ctx_parent using the Name key alternate key.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="ctx_Name">ctx_Name key value</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved ctx_parent</returns>
    public static ctx_parent Retrieve_ctx_NameKey(IOrganizationService service, string ctx_Name, params Expression<Func<ctx_parent, object>>[] columns)
    {
        var keyedEntityReference = new EntityReference(EntityLogicalName, new KeyAttributeCollection
        {
            ["ctx_name"] = ctx_Name,
        });

        return service.Retrieve(keyedEntityReference, columns);
    }
}
