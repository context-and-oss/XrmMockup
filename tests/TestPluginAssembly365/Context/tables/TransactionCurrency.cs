using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Currency in which a financial transaction is carried out.</para>
/// <para>Display Name: Currency</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("transactioncurrency")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class TransactionCurrency : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "transactioncurrency";
    public const int EntityTypeCode = 9105;

    public TransactionCurrency() : base(EntityLogicalName) { }
    public TransactionCurrency(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("currencyname");

    [AttributeLogicalName("transactioncurrencyid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("transactioncurrencyid", value);
        }
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the transaction currency.</para>
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
    /// <para>Date and time when the transaction currency was created.</para>
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
    /// <para>Unique identifier of the delegate user who created the transactioncurrency.</para>
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
    /// <para>Name of the transaction currency.</para>
    /// <para>Display Name: Currency Name</para>
    /// </summary>
    [AttributeLogicalName("currencyname")]
    [DisplayName("Currency Name")]
    [MaxLength(100)]
    public string? CurrencyName
    {
        get => GetAttributeValue<string?>("currencyname");
        set => SetAttributeValue("currencyname", value);
    }

    /// <summary>
    /// <para>Number of decimal places that can be used for currency.</para>
    /// <para>Display Name: Currency Precision</para>
    /// </summary>
    [AttributeLogicalName("currencyprecision")]
    [DisplayName("Currency Precision")]
    [Range(0, 10)]
    public int? CurrencyPrecision
    {
        get => GetAttributeValue<int?>("currencyprecision");
        set => SetAttributeValue("currencyprecision", value);
    }

    /// <summary>
    /// <para>Symbol for the transaction currency.</para>
    /// <para>Display Name: Currency Symbol</para>
    /// </summary>
    [AttributeLogicalName("currencysymbol")]
    [DisplayName("Currency Symbol")]
    [MaxLength(10)]
    public string? CurrencySymbol
    {
        get => GetAttributeValue<string?>("currencysymbol");
        set => SetAttributeValue("currencysymbol", value);
    }

    /// <summary>
    /// <para>Currency type that can be used for new currency.</para>
    /// <para>Display Name: Currency Type</para>
    /// </summary>
    [AttributeLogicalName("currencytype")]
    [DisplayName("Currency Type")]
    public transactioncurrency_currencytype? CurrencyType
    {
        get => this.GetOptionSetValue<transactioncurrency_currencytype>("currencytype");
        set => this.SetOptionSetValue("currencytype", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Entity Image Id</para>
    /// </summary>
    [AttributeLogicalName("entityimageid")]
    [DisplayName("Entity Image Id")]
    public Guid? EntityImageId
    {
        get => GetAttributeValue<Guid?>("entityimageid");
        set => SetAttributeValue("entityimageid", value);
    }

    /// <summary>
    /// <para>Exchange rate between the transaction currency and the base currency.</para>
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
    /// <para>ISO currency code for the transaction currency.</para>
    /// <para>Display Name: Currency Code</para>
    /// </summary>
    [AttributeLogicalName("isocurrencycode")]
    [DisplayName("Currency Code")]
    [MaxLength(5)]
    public string? ISOCurrencyCode
    {
        get => GetAttributeValue<string?>("isocurrencycode");
        set => SetAttributeValue("isocurrencycode", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who last modified the transaction currency.</para>
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
    /// <para>Date and time when the transaction currency was last modified.</para>
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
    /// <para>Unique identifier of the delegate user who last modified the transactioncurrency.</para>
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
    /// <para>Unique identifier of the organization associated with the transaction currency.</para>
    /// <para>Display Name: Organization</para>
    /// </summary>
    [AttributeLogicalName("organizationid")]
    [DisplayName("Organization")]
    public EntityReference? OrganizationId
    {
        get => GetAttributeValue<EntityReference?>("organizationid");
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
    /// <para>Status of the transaction currency.</para>
    /// <para>Display Name: Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Status")]
    public transactioncurrency_statecode? StateCode
    {
        get => this.GetOptionSetValue<transactioncurrency_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Reason for the status of the transaction currency.</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public transactioncurrency_statuscode? StatusCode
    {
        get => this.GetOptionSetValue<transactioncurrency_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>Display Name: Transaction Currency</para>
    /// </summary>
    [AttributeLogicalName("transactioncurrencyid")]
    [DisplayName("Transaction Currency")]
    public Guid? TransactionCurrencyId
    {
        get => GetAttributeValue<Guid?>("transactioncurrencyid");
        set => SetId("transactioncurrencyid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: UniqueDscId</para>
    /// </summary>
    [AttributeLogicalName("uniquedscid")]
    [DisplayName("UniqueDscId")]
    public Guid? UniqueDscId
    {
        get => GetAttributeValue<Guid?>("uniquedscid");
        set => SetAttributeValue("uniquedscid", value);
    }

    /// <summary>
    /// <para>Version number of the transaction currency.</para>
    /// <para>Display Name: Version Number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version Number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_transactioncurrency_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_transactioncurrency_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_transactioncurrency_createdonbehalfby", null);
        set => SetRelatedEntity("lk_transactioncurrency_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_transactioncurrency_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_transactioncurrency_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_transactioncurrency_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_transactioncurrency_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_transactioncurrencybase_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_transactioncurrencybase_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_transactioncurrencybase_createdby", null);
        set => SetRelatedEntity("lk_transactioncurrencybase_createdby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_transactioncurrencybase_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_transactioncurrencybase_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_transactioncurrencybase_modifiedby", null);
        set => SetRelatedEntity("lk_transactioncurrencybase_modifiedby", null, value);
    }

    [RelationshipSchemaName("transactioncurrency_account")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "account", "transactioncurrencyid", "Referenced")]
    public IEnumerable<Account> transactioncurrency_account
    {
        get => GetRelatedEntities<Account>("transactioncurrency_account", null);
        set => SetRelatedEntities("transactioncurrency_account", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_ActivityPointer")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "activitypointer", "transactioncurrencyid", "Referenced")]
    public IEnumerable<ActivityPointer> TransactionCurrency_ActivityPointer
    {
        get => GetRelatedEntities<ActivityPointer>("TransactionCurrency_ActivityPointer", null);
        set => SetRelatedEntities("TransactionCurrency_ActivityPointer", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_BusinessUnit")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "businessunit", "transactioncurrencyid", "Referenced")]
    public IEnumerable<BusinessUnit> TransactionCurrency_BusinessUnit
    {
        get => GetRelatedEntities<BusinessUnit>("TransactionCurrency_BusinessUnit", null);
        set => SetRelatedEntities("TransactionCurrency_BusinessUnit", null, value);
    }

    [RelationshipSchemaName("transactioncurrency_contact")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "contact", "transactioncurrencyid", "Referenced")]
    public IEnumerable<Contact> transactioncurrency_contact
    {
        get => GetRelatedEntities<Contact>("transactioncurrency_contact", null);
        set => SetRelatedEntities("transactioncurrency_contact", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_ctx_child")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "ctx_child", "transactioncurrencyid", "Referenced")]
    public IEnumerable<ctx_child> TransactionCurrency_ctx_child
    {
        get => GetRelatedEntities<ctx_child>("TransactionCurrency_ctx_child", null);
        set => SetRelatedEntities("TransactionCurrency_ctx_child", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_ctx_parent")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "ctx_parent", "transactioncurrencyid", "Referenced")]
    public IEnumerable<ctx_parent> TransactionCurrency_ctx_parent
    {
        get => GetRelatedEntities<ctx_parent>("TransactionCurrency_ctx_parent", null);
        set => SetRelatedEntities("TransactionCurrency_ctx_parent", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_Email")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "email", "transactioncurrencyid", "Referenced")]
    public IEnumerable<Email> TransactionCurrency_Email
    {
        get => GetRelatedEntities<Email>("TransactionCurrency_Email", null);
        set => SetRelatedEntities("TransactionCurrency_Email", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_Fax")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "fax", "transactioncurrencyid", "Referenced")]
    public IEnumerable<Fax> TransactionCurrency_Fax
    {
        get => GetRelatedEntities<Fax>("TransactionCurrency_Fax", null);
        set => SetRelatedEntities("TransactionCurrency_Fax", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_SystemUser")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "systemuser", "transactioncurrencyid", "Referenced")]
    public IEnumerable<SystemUser> TransactionCurrency_SystemUser
    {
        get => GetRelatedEntities<SystemUser>("TransactionCurrency_SystemUser", null);
        set => SetRelatedEntities("TransactionCurrency_SystemUser", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_Task")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "task", "transactioncurrencyid", "Referenced")]
    public IEnumerable<Task> TransactionCurrency_Task
    {
        get => GetRelatedEntities<Task>("TransactionCurrency_Task", null);
        set => SetRelatedEntities("TransactionCurrency_Task", null, value);
    }

    [RelationshipSchemaName("TransactionCurrency_Team")]
    [RelationshipMetadata("OneToMany", "transactioncurrencyid", "team", "transactioncurrencyid", "Referenced")]
    public IEnumerable<Team> TransactionCurrency_Team
    {
        get => GetRelatedEntities<Team>("TransactionCurrency_Team", null);
        set => SetRelatedEntities("TransactionCurrency_Team", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the TransactionCurrency entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<TransactionCurrency, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the TransactionCurrency with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of TransactionCurrency to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved TransactionCurrency</returns>
    public static TransactionCurrency Retrieve(IOrganizationService service, Guid id, params Expression<Func<TransactionCurrency, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
