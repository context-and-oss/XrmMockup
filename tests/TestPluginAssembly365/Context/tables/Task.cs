using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Generic activity representing work needed to be done.</para>
/// <para>Display Name: Task</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("task")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Task : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "task";
    public const int EntityTypeCode = 4212;

    public Task() : base(EntityLogicalName) { }
    public Task(Guid id) : base(EntityLogicalName, id) { }

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
    /// <para>For internal use only.</para>
    /// <para>Display Name: Additional Parameters</para>
    /// </summary>
    [AttributeLogicalName("activityadditionalparams")]
    [DisplayName("Additional Parameters")]
    [MaxLength(8192)]
    public string? ActivityAdditionalParams
    {
        get => GetAttributeValue<string?>("activityadditionalparams");
        set => SetAttributeValue("activityadditionalparams", value);
    }

    /// <summary>
    /// <para>Display Name: Task</para>
    /// </summary>
    [AttributeLogicalName("activityid")]
    [DisplayName("Task")]
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
    /// <para>Type the number of minutes spent on the task. The duration is used in reporting.</para>
    /// <para>Display Name: Duration</para>
    /// </summary>
    [AttributeLogicalName("actualdurationminutes")]
    [DisplayName("Duration")]
    [Range(0, 2147483647)]
    public int? ActualDurationMinutes
    {
        get => GetAttributeValue<int?>("actualdurationminutes");
        set => SetAttributeValue("actualdurationminutes", value);
    }

    /// <summary>
    /// <para>Enter the actual end date and time of the task. By default, it displays when the activity was completed or canceled.</para>
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
    /// <para>Enter the actual start date and time for the task. By default, it displays when the task was created.</para>
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
    /// <para>Type a category to identify the task type, such as lead gathering or customer follow up, to tie the task to a business group or function.</para>
    /// <para>Display Name: Category</para>
    /// </summary>
    [AttributeLogicalName("category")]
    [DisplayName("Category")]
    [MaxLength(250)]
    public string? Category
    {
        get => GetAttributeValue<string?>("category");
        set => SetAttributeValue("category", value);
    }

    /// <summary>
    /// <para>Shows who created the record.</para>
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
    /// <para>Shows the date and time when the record was created. The date and time are displayed in the time zone selected in Microsoft Dynamics 365 options.</para>
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
    /// <para>Shows who created the record on behalf of another user.</para>
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
    /// <para>Assigned Task Unique Id</para>
    /// <para>Display Name: Assigned Task Unique Id</para>
    /// </summary>
    [AttributeLogicalName("crmtaskassigneduniqueid")]
    [DisplayName("Assigned Task Unique Id")]
    public Guid? CrmTaskAssignedUniqueId
    {
        get => GetAttributeValue<Guid?>("crmtaskassigneduniqueid");
        set => SetAttributeValue("crmtaskassigneduniqueid", value);
    }

    /// <summary>
    /// <para>Type additional information to describe the task.</para>
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
    /// <para>Shows the conversion rate of the record's currency. The exchange rate is used to convert all money fields in the record from the local currency to the system's default currency.</para>
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
    /// <para>Information which specifies whether the task was billed as part of resolving a case.</para>
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
    /// <para>Information which specifies if the task was created from a workflow rule.</para>
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
    /// <para>Shows who last updated the record.</para>
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
    /// <para>Shows the date and time when the record was last updated. The date and time are displayed in the time zone selected in Microsoft Dynamics 365 options.</para>
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
    /// <para>Shows who last updated the record on behalf of another user.</para>
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
    /// <para>Enter the user or team who is assigned to manage the record. This field is updated every time the record is assigned to a different user.</para>
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
    /// <para>Shows the record owner's business unit.</para>
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
    /// <para>Unique identifier of the team that owns the task.</para>
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
    /// <para>Unique identifier of the user that owns the task.</para>
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
    /// <para>Type the percentage complete value for the task to track tasks to completion.</para>
    /// <para>Display Name: Percent Complete</para>
    /// </summary>
    [AttributeLogicalName("percentcomplete")]
    [DisplayName("Percent Complete")]
    [Range(0, 100)]
    public int? PercentComplete
    {
        get => GetAttributeValue<int?>("percentcomplete");
        set => SetAttributeValue("percentcomplete", value);
    }

    /// <summary>
    /// <para>Select the priority so that preferred customers or critical issues are handled quickly.</para>
    /// <para>Display Name: Priority</para>
    /// </summary>
    [AttributeLogicalName("prioritycode")]
    [DisplayName("Priority")]
    public task_prioritycode? PriorityCode
    {
        get => this.GetOptionSetValue<task_prioritycode>("prioritycode");
        set => this.SetOptionSetValue("prioritycode", value);
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
    /// <para>Choose the record that the task relates to.</para>
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
    /// <para>Scheduled duration of the task, specified in minutes.</para>
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
    /// <para>Enter the expected due date and time.</para>
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
    /// <para>Enter the expected due date and time.</para>
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
    /// <para>Choose the service level agreement (SLA) that you want to apply to the Task record.</para>
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
    /// <para>Last SLA that was applied to this Task. This field is for internal use only.</para>
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
    /// <para>Shows whether the task is open, completed, or canceled. Completed and canceled tasks are read-only and can't be edited.</para>
    /// <para>Display Name: Activity Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Activity Status")]
    public task_statecode? StateCode
    {
        get => this.GetOptionSetValue<task_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Select the task's status.</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public task_statuscode? StatusCode
    {
        get => this.GetOptionSetValue<task_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>Type a subcategory to identify the task type and relate the activity to a specific product, sales region, business group, or other function.</para>
    /// <para>Display Name: Sub-Category</para>
    /// </summary>
    [AttributeLogicalName("subcategory")]
    [DisplayName("Sub-Category")]
    [MaxLength(250)]
    public string? Subcategory
    {
        get => GetAttributeValue<string?>("subcategory");
        set => SetAttributeValue("subcategory", value);
    }

    /// <summary>
    /// <para>Type a short description about the objective or primary topic of the task.</para>
    /// <para>Display Name: Subject</para>
    /// </summary>
    [AttributeLogicalName("subject")]
    [DisplayName("Subject")]
    [MaxLength(200)]
    public string? Subject
    {
        get => GetAttributeValue<string?>("subject");
        set => SetAttributeValue("subject", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Subscription</para>
    /// </summary>
    [AttributeLogicalName("subscriptionid")]
    [DisplayName("Subscription")]
    public Guid? SubscriptionId
    {
        get => GetAttributeValue<Guid?>("subscriptionid");
        set => SetAttributeValue("subscriptionid", value);
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
    /// <para>Choose the local currency for the record to make sure budgets are reported in the correct currency.</para>
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
    /// <para>Version number of the task.</para>
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
    [RelationshipSchemaName("Account_Tasks")]
    [RelationshipMetadata("ManyToOne", "regardingobjectid", "account", "accountid", "Referencing")]
    public Account Account_Tasks
    {
        get => GetRelatedEntity<Account>("Account_Tasks", null);
        set => SetRelatedEntity("Account_Tasks", null, value);
    }

    [AttributeLogicalName("activityid")]
    [RelationshipSchemaName("activity_pointer_task")]
    [RelationshipMetadata("ManyToOne", "activityid", "activitypointer", "activityid", "Referencing")]
    public ActivityPointer activity_pointer_task
    {
        get => GetRelatedEntity<ActivityPointer>("activity_pointer_task", null);
        set => SetRelatedEntity("activity_pointer_task", null, value);
    }

    [AttributeLogicalName("owningbusinessunit")]
    [RelationshipSchemaName("business_unit_task_activities")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_task_activities
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_task_activities", null);
        set => SetRelatedEntity("business_unit_task_activities", null, value);
    }

    [AttributeLogicalName("regardingobjectid")]
    [RelationshipSchemaName("Contact_Tasks")]
    [RelationshipMetadata("ManyToOne", "regardingobjectid", "contact", "contactid", "Referencing")]
    public Contact Contact_Tasks
    {
        get => GetRelatedEntity<Contact>("Contact_Tasks", null);
        set => SetRelatedEntity("Contact_Tasks", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_task_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_task_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_task_createdby", null);
        set => SetRelatedEntity("lk_task_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_task_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_task_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_task_createdonbehalfby", null);
        set => SetRelatedEntity("lk_task_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_task_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_task_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_task_modifiedby", null);
        set => SetRelatedEntity("lk_task_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_task_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_task_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_task_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_task_modifiedonbehalfby", null, value);
    }

    [RelationshipSchemaName("task_activity_parties")]
    [RelationshipMetadata("OneToMany", "activityid", "activityparty", "activityid", "Referenced")]
    public IEnumerable<ActivityParty> task_activity_parties
    {
        get => GetRelatedEntities<ActivityParty>("task_activity_parties", null);
        set => SetRelatedEntities("task_activity_parties", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_task")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_task
    {
        get => GetRelatedEntity<Team>("team_task", null);
        set => SetRelatedEntity("team_task", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_Task")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_Task
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_Task", null);
        set => SetRelatedEntity("TransactionCurrency_Task", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("user_task")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser user_task
    {
        get => GetRelatedEntity<SystemUser>("user_task", null);
        set => SetRelatedEntity("user_task", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Task entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Task, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Task with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Task to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Task</returns>
    public static Task Retrieve(IOrganizationService service, Guid id, params Expression<Func<Task, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
