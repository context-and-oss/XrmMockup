using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Person or group associated with an activity. An activity can have multiple activity parties.</para>
/// <para>Display Name: Activity Party</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("activityparty")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class ActivityParty : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "activityparty";
    public const int EntityTypeCode = 135;

    public ActivityParty() : base(EntityLogicalName) { }
    public ActivityParty(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("partyidname");

    [AttributeLogicalName("activitypartyid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("activitypartyid", value);
        }
    }

    /// <summary>
    /// <para>Unique identifier of the activity associated with the activity party. (A "party" is any person who is associated with an activity.)</para>
    /// <para>Display Name: Activity</para>
    /// </summary>
    [AttributeLogicalName("activityid")]
    [DisplayName("Activity")]
    public EntityReference? ActivityId
    {
        get => GetAttributeValue<EntityReference?>("activityid");
        set => SetAttributeValue("activityid", value);
    }

    /// <summary>
    /// <para>Display Name: Activity Party</para>
    /// </summary>
    [AttributeLogicalName("activitypartyid")]
    [DisplayName("Activity Party")]
    public Guid? ActivityPartyId
    {
        get => GetAttributeValue<Guid?>("activitypartyid");
        set => SetId("activitypartyid", value);
    }

    /// <summary>
    /// <para>Email address to which an email is delivered, and which is associated with the target entity.</para>
    /// <para>Display Name: Address </para>
    /// </summary>
    [AttributeLogicalName("addressused")]
    [DisplayName("Address ")]
    [MaxLength(320)]
    public string? AddressUsed
    {
        get => GetAttributeValue<string?>("addressused");
        set => SetAttributeValue("addressused", value);
    }

    /// <summary>
    /// <para>Email address column number from associated party.</para>
    /// <para>Display Name: Email column number of party</para>
    /// </summary>
    [AttributeLogicalName("addressusedemailcolumnnumber")]
    [DisplayName("Email column number of party")]
    [Range(1, 2147483647)]
    public int? AddressUsedEmailColumnNumber
    {
        get => GetAttributeValue<int?>("addressusedemailcolumnnumber");
        set => SetAttributeValue("addressusedemailcolumnnumber", value);
    }

    /// <summary>
    /// <para>Information about whether to allow sending email to the activity party.</para>
    /// <para>Display Name: Do not allow Emails</para>
    /// </summary>
    [AttributeLogicalName("donotemail")]
    [DisplayName("Do not allow Emails")]
    public bool? DoNotEmail
    {
        get => GetAttributeValue<bool?>("donotemail");
        set => SetAttributeValue("donotemail", value);
    }

    /// <summary>
    /// <para>Information about whether to allow sending faxes to the activity party.</para>
    /// <para>Display Name: Do not allow Faxes</para>
    /// </summary>
    [AttributeLogicalName("donotfax")]
    [DisplayName("Do not allow Faxes")]
    public bool? DoNotFax
    {
        get => GetAttributeValue<bool?>("donotfax");
        set => SetAttributeValue("donotfax", value);
    }

    /// <summary>
    /// <para>Information about whether to allow phone calls to the lead.</para>
    /// <para>Display Name: Do not allow Phone Calls</para>
    /// </summary>
    [AttributeLogicalName("donotphone")]
    [DisplayName("Do not allow Phone Calls")]
    public bool? DoNotPhone
    {
        get => GetAttributeValue<bool?>("donotphone");
        set => SetAttributeValue("donotphone", value);
    }

    /// <summary>
    /// <para>Information about whether to allow sending postal mail to the lead.</para>
    /// <para>Display Name: Do not allow Postal Mails</para>
    /// </summary>
    [AttributeLogicalName("donotpostalmail")]
    [DisplayName("Do not allow Postal Mails")]
    public bool? DoNotPostalMail
    {
        get => GetAttributeValue<bool?>("donotpostalmail");
        set => SetAttributeValue("donotpostalmail", value);
    }

    /// <summary>
    /// <para>Amount of effort used by the resource in a service appointment activity.</para>
    /// <para>Display Name: Effort</para>
    /// </summary>
    [AttributeLogicalName("effort")]
    [DisplayName("Effort")]
    public double? Effort
    {
        get => GetAttributeValue<double?>("effort");
        set => SetAttributeValue("effort", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Exchange Entry</para>
    /// </summary>
    [AttributeLogicalName("exchangeentryid")]
    [DisplayName("Exchange Entry")]
    [MaxLength(1024)]
    public string? ExchangeEntryId
    {
        get => GetAttributeValue<string?>("exchangeentryid");
        set => SetAttributeValue("exchangeentryid", value);
    }

    /// <summary>
    /// <para>The external id used when the party does not have an email address.</para>
    /// <para>Display Name: External Id</para>
    /// </summary>
    [AttributeLogicalName("externalid")]
    [DisplayName("External Id")]
    [MaxLength(200)]
    public string? ExternalId
    {
        get => GetAttributeValue<string?>("externalid");
        set => SetAttributeValue("externalid", value);
    }

    /// <summary>
    /// <para>The external id type used when the party does not have an email address.</para>
    /// <para>Display Name: External Id Type</para>
    /// </summary>
    [AttributeLogicalName("externalidtype")]
    [DisplayName("External Id Type")]
    [MaxLength(200)]
    public string? ExternalIdType
    {
        get => GetAttributeValue<string?>("externalidtype");
        set => SetAttributeValue("externalidtype", value);
    }

    /// <summary>
    /// <para>Type of instance of a recurring series.</para>
    /// <para>Display Name: Appointment Type</para>
    /// </summary>
    [AttributeLogicalName("instancetypecode")]
    [DisplayName("Appointment Type")]
    public activityparty_instancetypecode? InstanceTypeCode
    {
        get => this.GetOptionSetValue<activityparty_instancetypecode>("instancetypecode");
        set => this.SetOptionSetValue("instancetypecode", value);
    }

    /// <summary>
    /// <para>Information about whether the underlying entity record is deleted.</para>
    /// <para>Display Name: Is Party Deleted</para>
    /// </summary>
    [AttributeLogicalName("ispartydeleted")]
    [DisplayName("Is Party Deleted")]
    public bool? IsPartyDeleted
    {
        get => GetAttributeValue<bool?>("ispartydeleted");
        set => SetAttributeValue("ispartydeleted", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user or team who owns the activity_party.</para>
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
    /// <para>Display Name: owningbusinessunit</para>
    /// </summary>
    [AttributeLogicalName("owningbusinessunit")]
    [DisplayName("owningbusinessunit")]
    public Guid? OwningBusinessUnit
    {
        get => GetAttributeValue<Guid?>("owningbusinessunit");
        set => SetAttributeValue("owningbusinessunit", value);
    }

    /// <summary>
    /// <para>Display Name: owninguser</para>
    /// </summary>
    [AttributeLogicalName("owninguser")]
    [DisplayName("owninguser")]
    public Guid? OwningUser
    {
        get => GetAttributeValue<Guid?>("owninguser");
        set => SetAttributeValue("owninguser", value);
    }

    /// <summary>
    /// <para>Role of the person in the activity, such as sender, to, cc, bcc, required, optional, organizer, regarding, or owner.</para>
    /// <para>Display Name: Participation Type</para>
    /// </summary>
    [AttributeLogicalName("participationtypemask")]
    [DisplayName("Participation Type")]
    public activityparty_participationtypemask? ParticipationTypeMask
    {
        get => this.GetOptionSetValue<activityparty_participationtypemask>("participationtypemask");
        set => this.SetOptionSetValue("participationtypemask", value);
    }

    /// <summary>
    /// <para>Unique identifier of the party associated with the activity.</para>
    /// <para>Display Name: Party</para>
    /// </summary>
    [AttributeLogicalName("partyid")]
    [DisplayName("Party")]
    public EntityReference? PartyId
    {
        get => GetAttributeValue<EntityReference?>("partyid");
        set => SetAttributeValue("partyid", value);
    }

    /// <summary>
    /// <para>Scheduled end time of the activity.</para>
    /// <para>Display Name: Scheduled End</para>
    /// </summary>
    [AttributeLogicalName("scheduledend")]
    [DisplayName("Scheduled End")]
    public DateTime? ScheduledEnd
    {
        get => GetAttributeValue<DateTime?>("scheduledend");
        set => SetAttributeValue("scheduledend", value);
    }

    /// <summary>
    /// <para>Scheduled start time of the activity.</para>
    /// <para>Display Name: Scheduled Start</para>
    /// </summary>
    [AttributeLogicalName("scheduledstart")]
    [DisplayName("Scheduled Start")]
    public DateTime? ScheduledStart
    {
        get => GetAttributeValue<DateTime?>("scheduledstart");
        set => SetAttributeValue("scheduledstart", value);
    }

    /// <summary>
    /// <para>The name of the party to be used when the party is not resolved to an entity.</para>
    /// <para>Display Name: Unresolved Party Name</para>
    /// </summary>
    [AttributeLogicalName("unresolvedpartyname")]
    [DisplayName("Unresolved Party Name")]
    [MaxLength(200)]
    public string? UnresolvedPartyName
    {
        get => GetAttributeValue<string?>("unresolvedpartyname");
        set => SetAttributeValue("unresolvedpartyname", value);
    }

    /// <summary>
    /// <para>Display Name: versionnumber</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("versionnumber")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    [AttributeLogicalName("partyid")]
    [RelationshipSchemaName("account_activity_parties")]
    [RelationshipMetadata("ManyToOne", "partyid", "account", "accountid", "Referencing")]
    public Account account_activity_parties
    {
        get => GetRelatedEntity<Account>("account_activity_parties", null);
        set => SetRelatedEntity("account_activity_parties", null, value);
    }

    [AttributeLogicalName("activityid")]
    [RelationshipSchemaName("activitypointer_activity_parties")]
    [RelationshipMetadata("ManyToOne", "activityid", "activitypointer", "activityid", "Referencing")]
    public ActivityPointer activitypointer_activity_parties
    {
        get => GetRelatedEntity<ActivityPointer>("activitypointer_activity_parties", null);
        set => SetRelatedEntity("activitypointer_activity_parties", null, value);
    }

    [AttributeLogicalName("partyid")]
    [RelationshipSchemaName("contact_activity_parties")]
    [RelationshipMetadata("ManyToOne", "partyid", "contact", "contactid", "Referencing")]
    public Contact contact_activity_parties
    {
        get => GetRelatedEntity<Contact>("contact_activity_parties", null);
        set => SetRelatedEntity("contact_activity_parties", null, value);
    }

    [AttributeLogicalName("activityid")]
    [RelationshipSchemaName("email_activity_parties")]
    [RelationshipMetadata("ManyToOne", "activityid", "email", "activityid", "Referencing")]
    public Email email_activity_parties
    {
        get => GetRelatedEntity<Email>("email_activity_parties", null);
        set => SetRelatedEntity("email_activity_parties", null, value);
    }

    [AttributeLogicalName("activityid")]
    [RelationshipSchemaName("fax_activity_parties")]
    [RelationshipMetadata("ManyToOne", "activityid", "fax", "activityid", "Referencing")]
    public Fax fax_activity_parties
    {
        get => GetRelatedEntity<Fax>("fax_activity_parties", null);
        set => SetRelatedEntity("fax_activity_parties", null, value);
    }

    [AttributeLogicalName("partyid")]
    [RelationshipSchemaName("system_user_activity_parties")]
    [RelationshipMetadata("ManyToOne", "partyid", "systemuser", "systemuserid", "Referencing")]
    public SystemUser system_user_activity_parties
    {
        get => GetRelatedEntity<SystemUser>("system_user_activity_parties", null);
        set => SetRelatedEntity("system_user_activity_parties", null, value);
    }

    [AttributeLogicalName("activityid")]
    [RelationshipSchemaName("task_activity_parties")]
    [RelationshipMetadata("ManyToOne", "activityid", "task", "activityid", "Referencing")]
    public Task task_activity_parties
    {
        get => GetRelatedEntity<Task>("task_activity_parties", null);
        set => SetRelatedEntity("task_activity_parties", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the ActivityParty entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<ActivityParty, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the ActivityParty with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of ActivityParty to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved ActivityParty</returns>
    public static ActivityParty Retrieve(IOrganizationService service, Guid id, params Expression<Func<ActivityParty, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
