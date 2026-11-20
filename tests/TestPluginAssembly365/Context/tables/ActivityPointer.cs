using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Task performed, or to be performed, by a user. An activity is any action for which an entry can be made on a calendar.</para>
/// <para>Display Name: Activity</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("activitypointer")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class ActivityPointer : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "activitypointer";
    public const int EntityTypeCode = 4200;

    public ActivityPointer() : base(EntityLogicalName) { }
    public ActivityPointer(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("subject");

    [AttributeLogicalName("activityid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("activityid", value);
        }
    }

    /// <summary>
    /// <para>Additional information provided by the external application as JSON. For internal use only.</para>
    /// <para>Display Name: Activity Additional Parameters</para>
    /// </summary>
    [AttributeLogicalName("activityadditionalparams")]
    [DisplayName("Activity Additional Parameters")]
    [MaxLength(8192)]
    public string? ActivityAdditionalParams
    {
        get => GetAttributeValue<string?>("activityadditionalparams");
        set => SetAttributeValue("activityadditionalparams", value);
    }

    /// <summary>
    /// <para>Display Name: Activity</para>
    /// </summary>
    [AttributeLogicalName("activityid")]
    [DisplayName("Activity")]
    public Guid? ActivityId
    {
        get => GetAttributeValue<Guid?>("activityid");
        set => SetId("activityid", value);
    }

    /// <summary>
    /// <para>Type of activity.</para>
    /// <para>Display Name: Activity Type</para>
    /// </summary>
    [AttributeLogicalName("activitytypecode")]
    [DisplayName("Activity Type")]
    [MaxLength()]
    public string? ActivityTypeCode
    {
        get => GetAttributeValue<string?>("activitytypecode");
        set => SetAttributeValue("activitytypecode", value);
    }

    /// <summary>
    /// <para>Actual duration of the activity in minutes.</para>
    /// <para>Display Name: Actual Duration</para>
    /// </summary>
    [AttributeLogicalName("actualdurationminutes")]
    [DisplayName("Actual Duration")]
    [Range(0, 2147483647)]
    public int? ActualDurationMinutes
    {
        get => GetAttributeValue<int?>("actualdurationminutes");
        set => SetAttributeValue("actualdurationminutes", value);
    }

    /// <summary>
    /// <para>Actual end time of the activity.</para>
    /// <para>Display Name: Actual End</para>
    /// </summary>
    [AttributeLogicalName("actualend")]
    [DisplayName("Actual End")]
    public DateTime? ActualEnd
    {
        get => GetAttributeValue<DateTime?>("actualend");
        set => SetAttributeValue("actualend", value);
    }

    /// <summary>
    /// <para>Actual start time of the activity.</para>
    /// <para>Display Name: Actual Start</para>
    /// </summary>
    [AttributeLogicalName("actualstart")]
    [DisplayName("Actual Start")]
    public DateTime? ActualStart
    {
        get => GetAttributeValue<DateTime?>("actualstart");
        set => SetAttributeValue("actualstart", value);
    }

    /// <summary>
    /// <para>All activity parties associated with this activity.</para>
    /// <para>Display Name: All Activity Parties</para>
    /// </summary>
    [AttributeLogicalName("allparties")]
    [DisplayName("All Activity Parties")]
    public IEnumerable<ActivityParty> allparties
    {
        get => GetEntityCollection<ActivityParty>("allparties");
        set => SetEntityCollection("allparties", value);
    }

    /// <summary>
    /// <para>Shows how contact about the social activity originated, such as from Twitter or Facebook. This field is read-only.</para>
    /// <para>Display Name: Social Channel</para>
    /// </summary>
    [AttributeLogicalName("community")]
    [DisplayName("Social Channel")]
    public socialprofile_community? Community
    {
        get => this.GetOptionSetValue<socialprofile_community>("community");
        set => this.SetOptionSetValue("community", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the activity.</para>
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
    /// <para>Date and time when the activity was created.</para>
    /// <para>Display Name: Date Created</para>
    /// </summary>
    [AttributeLogicalName("createdon")]
    [DisplayName("Date Created")]
    public DateTime? CreatedOn
    {
        get => GetAttributeValue<DateTime?>("createdon");
        set => SetAttributeValue("createdon", value);
    }

    /// <summary>
    /// <para>Unique identifier of the delegate user who created the activitypointer.</para>
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
    /// <para>Date and time when the delivery of the activity was last attempted.</para>
    /// <para>Display Name: Date Delivery Last Attempted</para>
    /// </summary>
    [AttributeLogicalName("deliverylastattemptedon")]
    [DisplayName("Date Delivery Last Attempted")]
    public DateTime? DeliveryLastAttemptedOn
    {
        get => GetAttributeValue<DateTime?>("deliverylastattemptedon");
        set => SetAttributeValue("deliverylastattemptedon", value);
    }

    /// <summary>
    /// <para>Priority of delivery of the activity to the email server.</para>
    /// <para>Display Name: Delivery Priority</para>
    /// </summary>
    [AttributeLogicalName("deliveryprioritycode")]
    [DisplayName("Delivery Priority")]
    public activitypointer_deliveryprioritycode? DeliveryPriorityCode
    {
        get => this.GetOptionSetValue<activitypointer_deliveryprioritycode>("deliveryprioritycode");
        set => this.SetOptionSetValue("deliveryprioritycode", value);
    }

    /// <summary>
    /// <para>Description of the activity.</para>
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
    /// <para>File that contains description content.</para>
    /// <para>Display Name: Description File Id</para>
    /// </summary>
    [AttributeLogicalName("descriptionblobid")]
    [DisplayName("Description File Id")]
    public byte[] DescriptionBlobId
    {
        get => GetAttributeValue<byte[]>("descriptionblobid");
        set => SetAttributeValue("descriptionblobid", value);
    }

    /// <summary>
    /// <para>The message id of activity which is returned from Exchange Server.</para>
    /// <para>Display Name: Exchange Item ID</para>
    /// </summary>
    [AttributeLogicalName("exchangeitemid")]
    [DisplayName("Exchange Item ID")]
    [MaxLength(200)]
    public string? ExchangeItemId
    {
        get => GetAttributeValue<string?>("exchangeitemid");
        set => SetAttributeValue("exchangeitemid", value);
    }

    /// <summary>
    /// <para>Exchange rate for the currency associated with the activitypointer with respect to the base currency.</para>
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
    /// <para>Shows the web link of Activity of type email.</para>
    /// <para>Display Name: Exchange WebLink</para>
    /// </summary>
    [AttributeLogicalName("exchangeweblink")]
    [DisplayName("Exchange WebLink")]
    [MaxLength(1250)]
    public string? ExchangeWebLink
    {
        get => GetAttributeValue<string?>("exchangeweblink");
        set => SetAttributeValue("exchangeweblink", value);
    }

    /// <summary>
    /// <para>Formatted scheduled end time of the activity.</para>
    /// <para>Display Name: Formatted End Date</para>
    /// </summary>
    [AttributeLogicalName("formattedscheduledend")]
    [DisplayName("Formatted End Date")]
    public DateTime? FormattedScheduledEnd
    {
        get => GetAttributeValue<DateTime?>("formattedscheduledend");
        set => SetAttributeValue("formattedscheduledend", value);
    }

    /// <summary>
    /// <para>Formatted scheduled start time of the activity.</para>
    /// <para>Display Name: Formatted Start Date</para>
    /// </summary>
    [AttributeLogicalName("formattedscheduledstart")]
    [DisplayName("Formatted Start Date")]
    public DateTime? FormattedScheduledStart
    {
        get => GetAttributeValue<DateTime?>("formattedscheduledstart");
        set => SetAttributeValue("formattedscheduledstart", value);
    }

    /// <summary>
    /// <para>Type of instance of a recurring series.</para>
    /// <para>Display Name: Recurring Instance Type</para>
    /// </summary>
    [AttributeLogicalName("instancetypecode")]
    [DisplayName("Recurring Instance Type")]
    public activitypointer_instancetypecode? InstanceTypeCode
    {
        get => this.GetOptionSetValue<activitypointer_instancetypecode>("instancetypecode");
        set => this.SetOptionSetValue("instancetypecode", value);
    }

    /// <summary>
    /// <para>Information regarding whether the activity was billed as part of resolving a case.</para>
    /// <para>Display Name: Is Billed</para>
    /// </summary>
    [AttributeLogicalName("isbilled")]
    [DisplayName("Is Billed")]
    public bool? IsBilled
    {
        get => GetAttributeValue<bool?>("isbilled");
        set => SetAttributeValue("isbilled", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Is Private</para>
    /// </summary>
    [AttributeLogicalName("ismapiprivate")]
    [DisplayName("Is Private")]
    public bool? IsMapiPrivate
    {
        get => GetAttributeValue<bool?>("ismapiprivate");
        set => SetAttributeValue("ismapiprivate", value);
    }

    /// <summary>
    /// <para>Information regarding whether the activity is a regular activity type or event type.</para>
    /// <para>Display Name: Is Regular Activity</para>
    /// </summary>
    [AttributeLogicalName("isregularactivity")]
    [DisplayName("Is Regular Activity")]
    public bool? IsRegularActivity
    {
        get => GetAttributeValue<bool?>("isregularactivity");
        set => SetAttributeValue("isregularactivity", value);
    }

    /// <summary>
    /// <para>Information regarding whether the activity was created from a workflow rule.</para>
    /// <para>Display Name: Is Workflow Created</para>
    /// </summary>
    [AttributeLogicalName("isworkflowcreated")]
    [DisplayName("Is Workflow Created")]
    public bool? IsWorkflowCreated
    {
        get => GetAttributeValue<bool?>("isworkflowcreated");
        set => SetAttributeValue("isworkflowcreated", value);
    }

    /// <summary>
    /// <para>Contains the date and time stamp of the last on hold time.</para>
    /// <para>Display Name: Last On Hold Time</para>
    /// </summary>
    [AttributeLogicalName("lastonholdtime")]
    [DisplayName("Last On Hold Time")]
    public DateTime? LastOnHoldTime
    {
        get => GetAttributeValue<DateTime?>("lastonholdtime");
        set => SetAttributeValue("lastonholdtime", value);
    }

    /// <summary>
    /// <para>Left the voice mail</para>
    /// <para>Display Name: Left Voice Mail</para>
    /// </summary>
    [AttributeLogicalName("leftvoicemail")]
    [DisplayName("Left Voice Mail")]
    public bool? LeftVoiceMail
    {
        get => GetAttributeValue<bool?>("leftvoicemail");
        set => SetAttributeValue("leftvoicemail", value);
    }

    /// <summary>
    /// <para>Unique identifier of user who last modified the activity.</para>
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
    /// <para>Date and time when activity was last modified.</para>
    /// <para>Display Name: Last Updated</para>
    /// </summary>
    [AttributeLogicalName("modifiedon")]
    [DisplayName("Last Updated")]
    public DateTime? ModifiedOn
    {
        get => GetAttributeValue<DateTime?>("modifiedon");
        set => SetAttributeValue("modifiedon", value);
    }

    /// <summary>
    /// <para>Unique identifier of the delegate user who last modified the activitypointer.</para>
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
    /// <para>Shows how long, in minutes, that the record was on hold.</para>
    /// <para>Display Name: On Hold Time (Minutes)</para>
    /// </summary>
    [AttributeLogicalName("onholdtime")]
    [DisplayName("On Hold Time (Minutes)")]
    [Range(-2147483648, 2147483647)]
    public int? OnHoldTime
    {
        get => GetAttributeValue<int?>("onholdtime");
        set => SetAttributeValue("onholdtime", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user or team who owns the activity.</para>
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
    /// <para>Unique identifier of the business unit that owns the activity.</para>
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
    /// <para>Unique identifier of the team that owns the activity.</para>
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
    /// <para>Unique identifier of the user that owns the activity.</para>
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
    /// <para>For internal use only.</para>
    /// <para>Display Name: Delay activity processing until</para>
    /// </summary>
    [AttributeLogicalName("postponeactivityprocessinguntil")]
    [DisplayName("Delay activity processing until")]
    public DateTime? PostponeActivityProcessingUntil
    {
        get => GetAttributeValue<DateTime?>("postponeactivityprocessinguntil");
        set => SetAttributeValue("postponeactivityprocessinguntil", value);
    }

    /// <summary>
    /// <para>Priority of the activity.</para>
    /// <para>Display Name: Priority</para>
    /// </summary>
    [AttributeLogicalName("prioritycode")]
    [DisplayName("Priority")]
    public activitypointer_prioritycode? PriorityCode
    {
        get => this.GetOptionSetValue<activitypointer_prioritycode>("prioritycode");
        set => this.SetOptionSetValue("prioritycode", value);
    }

    /// <summary>
    /// <para>Unique identifier of the Process.</para>
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
    /// <para>Unique identifier of the object with which the activity is associated.</para>
    /// <para>Display Name: Regarding</para>
    /// </summary>
    [AttributeLogicalName("regardingobjectid")]
    [DisplayName("Regarding")]
    public EntityReference? RegardingObjectId
    {
        get => GetAttributeValue<EntityReference?>("regardingobjectid");
        set => SetAttributeValue("regardingobjectid", value);
    }

    /// <summary>
    /// <para>Scheduled duration of the activity, specified in minutes.</para>
    /// <para>Display Name: Scheduled Duration</para>
    /// </summary>
    [AttributeLogicalName("scheduleddurationminutes")]
    [DisplayName("Scheduled Duration")]
    [Range(0, 2147483647)]
    public int? ScheduledDurationMinutes
    {
        get => GetAttributeValue<int?>("scheduleddurationminutes");
        set => SetAttributeValue("scheduleddurationminutes", value);
    }

    /// <summary>
    /// <para>Scheduled end time of the activity.</para>
    /// <para>Display Name: Due Date</para>
    /// </summary>
    [AttributeLogicalName("scheduledend")]
    [DisplayName("Due Date")]
    public DateTime? ScheduledEnd
    {
        get => GetAttributeValue<DateTime?>("scheduledend");
        set => SetAttributeValue("scheduledend", value);
    }

    /// <summary>
    /// <para>Scheduled start time of the activity.</para>
    /// <para>Display Name: Start Date</para>
    /// </summary>
    [AttributeLogicalName("scheduledstart")]
    [DisplayName("Start Date")]
    public DateTime? ScheduledStart
    {
        get => GetAttributeValue<DateTime?>("scheduledstart");
        set => SetAttributeValue("scheduledstart", value);
    }

    /// <summary>
    /// <para>Unique identifier of the mailbox associated with the sender of the email message.</para>
    /// <para>Display Name: Sender's Mailbox</para>
    /// </summary>
    [AttributeLogicalName("sendermailboxid")]
    [DisplayName("Sender's Mailbox")]
    public EntityReference? SenderMailboxId
    {
        get => GetAttributeValue<EntityReference?>("sendermailboxid");
        set => SetAttributeValue("sendermailboxid", value);
    }

    /// <summary>
    /// <para>Date and time when the activity was sent.</para>
    /// <para>Display Name: Date Sent</para>
    /// </summary>
    [AttributeLogicalName("senton")]
    [DisplayName("Date Sent")]
    public DateTime? SentOn
    {
        get => GetAttributeValue<DateTime?>("senton");
        set => SetAttributeValue("senton", value);
    }

    /// <summary>
    /// <para>Uniqueidentifier specifying the id of recurring series of an instance.</para>
    /// <para>Display Name: Series Id</para>
    /// </summary>
    [AttributeLogicalName("seriesid")]
    [DisplayName("Series Id")]
    public Guid? SeriesId
    {
        get => GetAttributeValue<Guid?>("seriesid");
        set => SetAttributeValue("seriesid", value);
    }

    /// <summary>
    /// <para>Choose the service level agreement (SLA) that you want to apply to the case record.</para>
    /// <para>Display Name: SLA</para>
    /// </summary>
    [AttributeLogicalName("slaid")]
    [DisplayName("SLA")]
    public EntityReference? SLAId
    {
        get => GetAttributeValue<EntityReference?>("slaid");
        set => SetAttributeValue("slaid", value);
    }

    /// <summary>
    /// <para>Last SLA that was applied to this case. This field is for internal use only.</para>
    /// <para>Display Name: Last SLA applied</para>
    /// </summary>
    [AttributeLogicalName("slainvokedid")]
    [DisplayName("Last SLA applied")]
    public EntityReference? SLAInvokedId
    {
        get => GetAttributeValue<EntityReference?>("slainvokedid");
        set => SetAttributeValue("slainvokedid", value);
    }

    /// <summary>
    /// <para>Shows the date and time by which the activities are sorted.</para>
    /// <para>Display Name: Sort Date</para>
    /// </summary>
    [AttributeLogicalName("sortdate")]
    [DisplayName("Sort Date")]
    public DateTime? SortDate
    {
        get => GetAttributeValue<DateTime?>("sortdate");
        set => SetAttributeValue("sortdate", value);
    }

    /// <summary>
    /// <para>Unique identifier of the Stage.</para>
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
    /// <para>Status of the activity.</para>
    /// <para>Display Name: Activity Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Activity Status")]
    public activitypointer_statecode? StateCode
    {
        get => this.GetOptionSetValue<activitypointer_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Reason for the status of the activity.</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public activitypointer_statuscode? StatusCode
    {
        get => this.GetOptionSetValue<activitypointer_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>Subject associated with the activity.</para>
    /// <para>Display Name: Subject</para>
    /// </summary>
    [AttributeLogicalName("subject")]
    [DisplayName("Subject")]
    [MaxLength(400)]
    public string? Subject
    {
        get => GetAttributeValue<string?>("subject");
        set => SetAttributeValue("subject", value);
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
    /// <para>Unique identifier of the currency associated with the activitypointer.</para>
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
    /// <para>Version number of the activity.</para>
    /// <para>Display Name: Version Number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version Number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    [AttributeLogicalName("regardingobjectid")]
    [RelationshipSchemaName("Account_ActivityPointers")]
    [RelationshipMetadata("ManyToOne", "regardingobjectid", "account", "accountid", "Referencing")]
    public Account Account_ActivityPointers
    {
        get => GetRelatedEntity<Account>("Account_ActivityPointers", null);
        set => SetRelatedEntity("Account_ActivityPointers", null, value);
    }

    [RelationshipSchemaName("activity_pointer_email")]
    [RelationshipMetadata("OneToMany", "activityid", "email", "activityid", "Referenced")]
    public IEnumerable<Email> activity_pointer_email
    {
        get => GetRelatedEntities<Email>("activity_pointer_email", null);
        set => SetRelatedEntities("activity_pointer_email", null, value);
    }

    [RelationshipSchemaName("activity_pointer_fax")]
    [RelationshipMetadata("OneToMany", "activityid", "fax", "activityid", "Referenced")]
    public IEnumerable<Fax> activity_pointer_fax
    {
        get => GetRelatedEntities<Fax>("activity_pointer_fax", null);
        set => SetRelatedEntities("activity_pointer_fax", null, value);
    }

    [RelationshipSchemaName("activity_pointer_task")]
    [RelationshipMetadata("OneToMany", "activityid", "task", "activityid", "Referenced")]
    public IEnumerable<Task> activity_pointer_task
    {
        get => GetRelatedEntities<Task>("activity_pointer_task", null);
        set => SetRelatedEntities("activity_pointer_task", null, value);
    }

    [RelationshipSchemaName("activitypointer_activity_parties")]
    [RelationshipMetadata("OneToMany", "activityid", "activityparty", "activityid", "Referenced")]
    public IEnumerable<ActivityParty> activitypointer_activity_parties
    {
        get => GetRelatedEntities<ActivityParty>("activitypointer_activity_parties", null);
        set => SetRelatedEntities("activitypointer_activity_parties", null, value);
    }

    [RelationshipSchemaName("activitypointer_FileAttachments")]
    [RelationshipMetadata("OneToMany", "activityid", "fileattachment", "objectid", "Referenced")]
    public IEnumerable<FileAttachment> activitypointer_FileAttachments
    {
        get => GetRelatedEntities<FileAttachment>("activitypointer_FileAttachments", null);
        set => SetRelatedEntities("activitypointer_FileAttachments", null, value);
    }

    [AttributeLogicalName("owningbusinessunit")]
    [RelationshipSchemaName("business_unit_activitypointer")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_activitypointer
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_activitypointer", null);
        set => SetRelatedEntity("business_unit_activitypointer", null, value);
    }

    [AttributeLogicalName("regardingobjectid")]
    [RelationshipSchemaName("Contact_ActivityPointers")]
    [RelationshipMetadata("ManyToOne", "regardingobjectid", "contact", "contactid", "Referencing")]
    public Contact Contact_ActivityPointers
    {
        get => GetRelatedEntity<Contact>("Contact_ActivityPointers", null);
        set => SetRelatedEntity("Contact_ActivityPointers", null, value);
    }

    [AttributeLogicalName("descriptionblobid")]
    [RelationshipSchemaName("FileAttachment_ActivityPointer_DescriptionBlobId")]
    [RelationshipMetadata("ManyToOne", "descriptionblobid", "fileattachment", "fileattachmentid", "Referencing")]
    public FileAttachment FileAttachment_ActivityPointer_DescriptionBlobId
    {
        get => GetRelatedEntity<FileAttachment>("FileAttachment_ActivityPointer_DescriptionBlobId", null);
        set => SetRelatedEntity("FileAttachment_ActivityPointer_DescriptionBlobId", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_activitypointer_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_activitypointer_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_activitypointer_createdby", null);
        set => SetRelatedEntity("lk_activitypointer_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_activitypointer_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_activitypointer_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_activitypointer_createdonbehalfby", null);
        set => SetRelatedEntity("lk_activitypointer_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_activitypointer_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_activitypointer_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_activitypointer_modifiedby", null);
        set => SetRelatedEntity("lk_activitypointer_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_activitypointer_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_activitypointer_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_activitypointer_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_activitypointer_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_activity")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_activity
    {
        get => GetRelatedEntity<Team>("team_activity", null);
        set => SetRelatedEntity("team_activity", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_ActivityPointer")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_ActivityPointer
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_ActivityPointer", null);
        set => SetRelatedEntity("TransactionCurrency_ActivityPointer", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("user_activity")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser user_activity
    {
        get => GetRelatedEntity<SystemUser>("user_activity", null);
        set => SetRelatedEntity("user_activity", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the ActivityPointer entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<ActivityPointer, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the ActivityPointer with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of ActivityPointer to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved ActivityPointer</returns>
    public static ActivityPointer Retrieve(IOrganizationService service, Guid id, params Expression<Func<ActivityPointer, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
