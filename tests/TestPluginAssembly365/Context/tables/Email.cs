using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Activity that is delivered using email protocols.</para>
/// <para>Display Name: Email</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("email")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Email : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "email";
    public const int EntityTypeCode = 4202;

    public Email() : base(EntityLogicalName) { }
    public Email(Guid id) : base(EntityLogicalName, id) { }

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
    /// <para>The Entity that Accepted the Email</para>
    /// <para>Display Name: Accepting Entity</para>
    /// </summary>
    [AttributeLogicalName("acceptingentityid")]
    [DisplayName("Accepting Entity")]
    public EntityReference? AcceptingEntityId
    {
        get => GetAttributeValue<EntityReference?>("acceptingentityid");
        set => SetAttributeValue("acceptingentityid", value);
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
    /// <para>Display Name: Email Message</para>
    /// </summary>
    [AttributeLogicalName("activityid")]
    [DisplayName("Email Message")]
    public Guid? ActivityId
    {
        get => GetAttributeValue<Guid?>("activityid");
        set => SetId("activityid", value);
    }

    /// <summary>
    /// <para>Shows the type of activity.</para>
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
    /// <para>Type the number of minutes spent creating and sending the email. The duration is used in reporting.</para>
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
    /// <para>Enter the actual end date and time of the email. By default, it displays the date and time when the activity was completed or canceled, but can be edited to capture the actual time to create and send the email.</para>
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
    /// <para>Enter the actual start date and time for the email. By default, it displays the date and time when the activity was created, but can be edited to capture the actual time to create and send the email.</para>
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
    /// <para>Shows the umber of attachments of the email message.</para>
    /// <para>Display Name: Attachment Count</para>
    /// </summary>
    [AttributeLogicalName("attachmentcount")]
    [DisplayName("Attachment Count")]
    [Range(0, 2147483647)]
    public int? AttachmentCount
    {
        get => GetAttributeValue<int?>("attachmentcount");
        set => SetAttributeValue("attachmentcount", value);
    }

    /// <summary>
    /// <para>Shows the number of times an email attachment has been viewed.</para>
    /// <para>Display Name: Attachment Open Count</para>
    /// </summary>
    [AttributeLogicalName("attachmentopencount")]
    [DisplayName("Attachment Open Count")]
    [Range(0, 2147483647)]
    public int? AttachmentOpenCount
    {
        get => GetAttributeValue<int?>("attachmentopencount");
        set => SetAttributeValue("attachmentopencount", value);
    }

    /// <summary>
    /// <para>Hash of base of conversation index.</para>
    /// <para>Display Name: Conversation Index (Hash)</para>
    /// </summary>
    [AttributeLogicalName("baseconversationindexhash")]
    [DisplayName("Conversation Index (Hash)")]
    [Range(-2147483648, 2147483647)]
    public int? BaseConversationIndexHash
    {
        get => GetAttributeValue<int?>("baseconversationindexhash");
        set => SetAttributeValue("baseconversationindexhash", value);
    }

    /// <summary>
    /// <para>Enter the recipients that are included on the email distribution, but are not displayed to other recipients.</para>
    /// <para>Display Name: Bcc</para>
    /// </summary>
    [AttributeLogicalName("bcc")]
    [DisplayName("Bcc")]
    public IEnumerable<ActivityParty> bcc
    {
        get => GetEntityCollection<ActivityParty>("bcc");
        set => SetEntityCollection("bcc", value);
    }

    /// <summary>
    /// <para>Type a category to identify the email type, such as lead outreach, customer follow-up, or service alert, to tie the email to a business group or function.</para>
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
    /// <para>Enter the recipients that should be copied on the email.</para>
    /// <para>Display Name: Cc</para>
    /// </summary>
    [AttributeLogicalName("cc")]
    [DisplayName("Cc")]
    public IEnumerable<ActivityParty> cc
    {
        get => GetEntityCollection<ActivityParty>("cc");
        set => SetEntityCollection("cc", value);
    }

    /// <summary>
    /// <para>Indicates if the body is compressed.</para>
    /// <para>Display Name: Compression</para>
    /// </summary>
    [AttributeLogicalName("compressed")]
    [DisplayName("Compression")]
    public bool? Compressed
    {
        get => GetAttributeValue<bool?>("compressed");
        set => SetAttributeValue("compressed", value);
    }

    /// <summary>
    /// <para>Identifier for all the email responses for this conversation.</para>
    /// <para>Display Name: Conversation Index</para>
    /// </summary>
    [AttributeLogicalName("conversationindex")]
    [DisplayName("Conversation Index")]
    [MaxLength(2048)]
    public string? ConversationIndex
    {
        get => GetAttributeValue<string?>("conversationindex");
        set => SetAttributeValue("conversationindex", value);
    }

    /// <summary>
    /// <para>Conversation Tracking Id.</para>
    /// <para>Display Name: Conversation Tracking Id</para>
    /// </summary>
    [AttributeLogicalName("conversationtrackingid")]
    [DisplayName("Conversation Tracking Id")]
    public Guid? ConversationTrackingId
    {
        get => GetAttributeValue<Guid?>("conversationtrackingid");
        set => SetAttributeValue("conversationtrackingid", value);
    }

    /// <summary>
    /// <para>Correlated Activity Id</para>
    /// <para>Display Name: Correlated Activity Id</para>
    /// </summary>
    [AttributeLogicalName("correlatedactivityid")]
    [DisplayName("Correlated Activity Id")]
    public EntityReference? CorrelatedActivityId
    {
        get => GetAttributeValue<EntityReference?>("correlatedactivityid");
        set => SetAttributeValue("correlatedactivityid", value);
    }

    /// <summary>
    /// <para>Indicates if the subject changed compared to the subject of the correlated email</para>
    /// <para>Display Name: Correlated subject changed</para>
    /// </summary>
    [AttributeLogicalName("correlatedsubjectchanged")]
    [DisplayName("Correlated subject changed")]
    public bool? correlatedsubjectchanged
    {
        get => GetAttributeValue<bool?>("correlatedsubjectchanged");
        set => SetAttributeValue("correlatedsubjectchanged", value);
    }

    /// <summary>
    /// <para>Shows how an email is correlated to an existing email in Microsoft Dynamics 365. XHeader and CustomCorrelation are not used. For system use only.</para>
    /// <para>Display Name: Correlation Method</para>
    /// </summary>
    [AttributeLogicalName("correlationmethod")]
    [DisplayName("Correlation Method")]
    public email_correlationmethod? CorrelationMethod
    {
        get => this.GetOptionSetValue<email_correlationmethod>("correlationmethod");
        set => this.SetOptionSetValue("correlationmethod", value);
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
    /// <para>Enter the expected date and time when email will be sent.</para>
    /// <para>Display Name: Send Later</para>
    /// </summary>
    [AttributeLogicalName("delayedemailsendtime")]
    [DisplayName("Send Later")]
    public DateTime? DelayedEmailSendTime
    {
        get => GetAttributeValue<DateTime?>("delayedemailsendtime");
        set => SetAttributeValue("delayedemailsendtime", value);
    }

    /// <summary>
    /// <para>Shows the count of the number of attempts made to send the email. The count is used as an indicator of email routing issues.</para>
    /// <para>Display Name: No. of Delivery Attempts</para>
    /// </summary>
    [AttributeLogicalName("deliveryattempts")]
    [DisplayName("No. of Delivery Attempts")]
    [Range(0, 1000000000)]
    public int? DeliveryAttempts
    {
        get => GetAttributeValue<int?>("deliveryattempts");
        set => SetAttributeValue("deliveryattempts", value);
    }

    /// <summary>
    /// <para>Select the priority of delivery of the email to the email server.</para>
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
    /// <para>Select whether the sender should receive confirmation that the email was delivered.</para>
    /// <para>Display Name: Delivery Receipt Requested</para>
    /// </summary>
    [AttributeLogicalName("deliveryreceiptrequested")]
    [DisplayName("Delivery Receipt Requested")]
    public bool? DeliveryReceiptRequested
    {
        get => GetAttributeValue<bool?>("deliveryreceiptrequested");
        set => SetAttributeValue("deliveryreceiptrequested", value);
    }

    /// <summary>
    /// <para>Type the greeting and message text of the email.</para>
    /// <para>Display Name: Description</para>
    /// </summary>
    [AttributeLogicalName("description")]
    [DisplayName("Description")]
    [MaxLength(1073741823)]
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
    /// <para>Select the direction of the email as incoming or outbound.</para>
    /// <para>Display Name: Direction</para>
    /// </summary>
    [AttributeLogicalName("directioncode")]
    [DisplayName("Direction")]
    public bool? DirectionCode
    {
        get => GetAttributeValue<bool?>("directioncode");
        set => SetAttributeValue("directioncode", value);
    }

    /// <summary>
    /// <para>Shows the date and time when an email reminder expires.</para>
    /// <para>Display Name: Email Reminder Expiry Time</para>
    /// </summary>
    [AttributeLogicalName("emailreminderexpirytime")]
    [DisplayName("Email Reminder Expiry Time")]
    public DateTime? EmailReminderExpiryTime
    {
        get => GetAttributeValue<DateTime?>("emailreminderexpirytime");
        set => SetAttributeValue("emailreminderexpirytime", value);
    }

    /// <summary>
    /// <para>Shows the status of the email reminder.</para>
    /// <para>Display Name: Email Reminder Status</para>
    /// </summary>
    [AttributeLogicalName("emailreminderstatus")]
    [DisplayName("Email Reminder Status")]
    public email_reminderstatus? EmailReminderStatus
    {
        get => this.GetOptionSetValue<email_reminderstatus>("emailreminderstatus");
        set => this.SetOptionSetValue("emailreminderstatus", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Email Reminder Text</para>
    /// </summary>
    [AttributeLogicalName("emailremindertext")]
    [DisplayName("Email Reminder Text")]
    [MaxLength(1250)]
    public string? EmailReminderText
    {
        get => GetAttributeValue<string?>("emailremindertext");
        set => SetAttributeValue("emailremindertext", value);
    }

    /// <summary>
    /// <para>Shows the type of the email reminder.</para>
    /// <para>Display Name: Email Reminder Type</para>
    /// </summary>
    [AttributeLogicalName("emailremindertype")]
    [DisplayName("Email Reminder Type")]
    public email_remindertype? EmailReminderType
    {
        get => this.GetOptionSetValue<email_remindertype>("emailremindertype");
        set => this.SetOptionSetValue("emailremindertype", value);
    }

    /// <summary>
    /// <para>Shows the sender of the email.</para>
    /// <para>Display Name: Sender</para>
    /// </summary>
    [AttributeLogicalName("emailsender")]
    [DisplayName("Sender")]
    public EntityReference? EmailSender
    {
        get => GetAttributeValue<EntityReference?>("emailsender");
        set => SetAttributeValue("emailsender", value);
    }

    /// <summary>
    /// <para>Email Tracking Id.</para>
    /// <para>Display Name: Email Tracking Id</para>
    /// </summary>
    [AttributeLogicalName("emailtrackingid")]
    [DisplayName("Email Tracking Id")]
    public Guid? EmailTrackingId
    {
        get => GetAttributeValue<Guid?>("emailtrackingid");
        set => SetAttributeValue("emailtrackingid", value);
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
    /// <para>Select whether the email allows following recipient activities sent from Microsoft Dynamics 365.This is user preference state which can be overridden by system evaluated state.</para>
    /// <para>Display Name: Following</para>
    /// </summary>
    [AttributeLogicalName("followemailuserpreference")]
    [DisplayName("Following")]
    public bool? FollowEmailUserPreference
    {
        get => GetAttributeValue<bool?>("followemailuserpreference");
        set => SetAttributeValue("followemailuserpreference", value);
    }

    /// <summary>
    /// <para>Enter the sender of the email.</para>
    /// <para>Display Name: From</para>
    /// </summary>
    [AttributeLogicalName("from")]
    [DisplayName("From")]
    public IEnumerable<ActivityParty> from
    {
        get => GetEntityCollection<ActivityParty>("from");
        set => SetEntityCollection("from", value);
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
    /// <para>Type the ID of the email message that this email activity is a response to.</para>
    /// <para>Display Name: In Reply To Message</para>
    /// </summary>
    [AttributeLogicalName("inreplyto")]
    [DisplayName("In Reply To Message")]
    [MaxLength(320)]
    public string? InReplyTo
    {
        get => GetAttributeValue<string?>("inreplyto");
        set => SetAttributeValue("inreplyto", value);
    }

    /// <summary>
    /// <para>Contains a set of internet headers associated to the email message in json format</para>
    /// <para>Display Name: Internet message headers</para>
    /// </summary>
    [AttributeLogicalName("internetmessageheaders")]
    [DisplayName("Internet message headers")]
    [MaxLength(500)]
    public string? InternetMessageHeaders
    {
        get => GetAttributeValue<string?>("internetmessageheaders");
        set => SetAttributeValue("internetmessageheaders", value);
    }

    /// <summary>
    /// <para>Information regarding whether the email activity was billed as part of resolving a case.</para>
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
    /// <para>Indicates if the sender of the email is unresolved in case of multiple match</para>
    /// <para>Display Name: Is Duplicate Sender Unresolved</para>
    /// </summary>
    [AttributeLogicalName("isduplicatesenderunresolved")]
    [DisplayName("Is Duplicate Sender Unresolved")]
    public bool? IsDuplicateSenderUnresolved
    {
        get => GetAttributeValue<bool?>("isduplicatesenderunresolved");
        set => SetAttributeValue("isduplicatesenderunresolved", value);
    }

    /// <summary>
    /// <para>For internal use only. Shows whether this email is followed. This is evaluated state which overrides user selection of follow email.</para>
    /// <para>Display Name: Followed</para>
    /// </summary>
    [AttributeLogicalName("isemailfollowed")]
    [DisplayName("Followed")]
    public bool? IsEmailFollowed
    {
        get => GetAttributeValue<bool?>("isemailfollowed");
        set => SetAttributeValue("isemailfollowed", value);
    }

    /// <summary>
    /// <para>For internal use only. Shows whether this email Reminder is Set.</para>
    /// <para>Display Name: Reminder Set</para>
    /// </summary>
    [AttributeLogicalName("isemailreminderset")]
    [DisplayName("Reminder Set")]
    public bool? IsEmailReminderSet
    {
        get => GetAttributeValue<bool?>("isemailreminderset");
        set => SetAttributeValue("isemailreminderset", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Is Protected</para>
    /// </summary>
    [AttributeLogicalName("isprotected")]
    [DisplayName("Is Protected")]
    public bool? IsProtected
    {
        get => GetAttributeValue<bool?>("isprotected");
        set => SetAttributeValue("isprotected", value);
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
    /// <para>For internal use only.</para>
    /// <para>Display Name: IsSafeDescriptionTruncated</para>
    /// </summary>
    [AttributeLogicalName("issafedescriptiontruncated")]
    [DisplayName("IsSafeDescriptionTruncated")]
    [Range(0, 2147483647)]
    public int? IsSafeDescriptionTruncated
    {
        get => GetAttributeValue<int?>("issafedescriptiontruncated");
        set => SetAttributeValue("issafedescriptiontruncated", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: IsUnsafe</para>
    /// </summary>
    [AttributeLogicalName("isunsafe")]
    [DisplayName("IsUnsafe")]
    [Range(0, 2147483647)]
    public int? IsUnsafe
    {
        get => GetAttributeValue<int?>("isunsafe");
        set => SetAttributeValue("isunsafe", value);
    }

    /// <summary>
    /// <para>Indication if the email was created by a workflow rule.</para>
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
    /// <para>Shows the latest date and time when email was opened.</para>
    /// <para>Display Name: Last Opened Time</para>
    /// </summary>
    [AttributeLogicalName("lastopenedtime")]
    [DisplayName("Last Opened Time")]
    public DateTime? LastOpenedTime
    {
        get => GetAttributeValue<DateTime?>("lastopenedtime");
        set => SetAttributeValue("lastopenedtime", value);
    }

    /// <summary>
    /// <para>Shows the number of times a link in an email has been clicked.</para>
    /// <para>Display Name: Links Clicked Count</para>
    /// </summary>
    [AttributeLogicalName("linksclickedcount")]
    [DisplayName("Links Clicked Count")]
    [Range(0, 2147483647)]
    public int? LinksClickedCount
    {
        get => GetAttributeValue<int?>("linksclickedcount");
        set => SetAttributeValue("linksclickedcount", value);
    }

    /// <summary>
    /// <para>Unique identifier of the email message. Used only for email that is received.</para>
    /// <para>Display Name: Message ID</para>
    /// </summary>
    [AttributeLogicalName("messageid")]
    [DisplayName("Message ID")]
    [MaxLength(320)]
    public string? MessageId
    {
        get => GetAttributeValue<string?>("messageid");
        set => SetAttributeValue("messageid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Message ID Dup Check</para>
    /// </summary>
    [AttributeLogicalName("messageiddupcheck")]
    [DisplayName("Message ID Dup Check")]
    public Guid? MessageIdDupCheck
    {
        get => GetAttributeValue<Guid?>("messageiddupcheck");
        set => SetAttributeValue("messageiddupcheck", value);
    }

    /// <summary>
    /// <para>MIME type of the email message data.</para>
    /// <para>Display Name: Mime Type</para>
    /// </summary>
    [AttributeLogicalName("mimetype")]
    [DisplayName("Mime Type")]
    [MaxLength(256)]
    public string? MimeType
    {
        get => GetAttributeValue<string?>("mimetype");
        set => SetAttributeValue("mimetype", value);
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
    /// <para>Name of the agent which associated the activity.</para>
    /// <para>Display Name: Associated Agent Name</para>
    /// </summary>
    [AttributeLogicalName("msdyn_associatedagentname")]
    [DisplayName("Associated Agent Name")]
    [MaxLength(100)]
    public string? msdyn_associatedagentname
    {
        get => GetAttributeValue<string?>("msdyn_associatedagentname");
        set => SetAttributeValue("msdyn_associatedagentname", value);
    }

    /// <summary>
    /// <para>Select the notification code to identify issues with the email recipients or attachments, such as blocked attachments.</para>
    /// <para>Display Name: Notifications</para>
    /// </summary>
    [AttributeLogicalName("notifications")]
    [DisplayName("Notifications")]
    public email_notifications? Notifications
    {
        get => this.GetOptionSetValue<email_notifications>("notifications");
        set => this.SetOptionSetValue("notifications", value);
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
    /// <para>Shows the number of times an email has been opened.</para>
    /// <para>Display Name: Open Count</para>
    /// </summary>
    [AttributeLogicalName("opencount")]
    [DisplayName("Open Count")]
    [Range(0, 2147483647)]
    public int? OpenCount
    {
        get => GetAttributeValue<int?>("opencount");
        set => SetAttributeValue("opencount", value);
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
    /// <para>Unique identifier of the business unit that owns the email activity.</para>
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
    /// <para>Unique identifier of the team who owns the email activity.</para>
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
    /// <para>Unique identifier of the user who owns the email activity.</para>
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
    /// <para>Select the activity that the email is associated with.</para>
    /// <para>Display Name: Parent Activity Id</para>
    /// </summary>
    [AttributeLogicalName("parentactivityid")]
    [DisplayName("Parent Activity Id")]
    public EntityReference? ParentActivityId
    {
        get => GetAttributeValue<EntityReference?>("parentactivityid");
        set => SetAttributeValue("parentactivityid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Parent Sensitivity Label Id</para>
    /// </summary>
    [AttributeLogicalName("parentsensitivitylabelid")]
    [DisplayName("Parent Sensitivity Label Id")]
    public Guid? ParentSensitivityLabelId
    {
        get => GetAttributeValue<Guid?>("parentsensitivitylabelid");
        set => SetAttributeValue("parentsensitivitylabelid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Delay email processing until</para>
    /// </summary>
    [AttributeLogicalName("postponeemailprocessinguntil")]
    [DisplayName("Delay email processing until")]
    public DateTime? PostponeEmailProcessingUntil
    {
        get => GetAttributeValue<DateTime?>("postponeemailprocessinguntil");
        set => SetAttributeValue("postponeemailprocessinguntil", value);
    }

    /// <summary>
    /// <para>Select the priority so that preferred customers or critical issues are handled quickly.</para>
    /// <para>Display Name: Priority</para>
    /// </summary>
    [AttributeLogicalName("prioritycode")]
    [DisplayName("Priority")]
    public email_prioritycode? PriorityCode
    {
        get => this.GetOptionSetValue<email_prioritycode>("prioritycode");
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
    /// <para>Purview Rights</para>
    /// <para>Display Name: Purview Rights</para>
    /// </summary>
    [AttributeLogicalName("purviewrights")]
    [DisplayName("Purview Rights")]
    [MaxLength(500)]
    public string? PurviewRights
    {
        get => GetAttributeValue<string?>("purviewrights");
        set => SetAttributeValue("purviewrights", value);
    }

    /// <summary>
    /// <para>Indicates that a read receipt is requested.</para>
    /// <para>Display Name: Read Receipt Requested</para>
    /// </summary>
    [AttributeLogicalName("readreceiptrequested")]
    [DisplayName("Read Receipt Requested")]
    public bool? ReadReceiptRequested
    {
        get => GetAttributeValue<bool?>("readreceiptrequested");
        set => SetAttributeValue("readreceiptrequested", value);
    }

    /// <summary>
    /// <para>The Mailbox that Received the Email.</para>
    /// <para>Display Name: Receiving Mailbox</para>
    /// </summary>
    [AttributeLogicalName("receivingmailboxid")]
    [DisplayName("Receiving Mailbox")]
    public EntityReference? ReceivingMailboxId
    {
        get => GetAttributeValue<EntityReference?>("receivingmailboxid");
        set => SetAttributeValue("receivingmailboxid", value);
    }

    /// <summary>
    /// <para>Choose the record that the email relates to.</para>
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
    /// <para>Enter the related records for the email.</para>
    /// <para>Display Name: Related</para>
    /// </summary>
    [AttributeLogicalName("related")]
    [DisplayName("Related")]
    public IEnumerable<ActivityParty> related
    {
        get => GetEntityCollection<ActivityParty>("related");
        set => SetEntityCollection("related", value);
    }

    /// <summary>
    /// <para>Reminder Action Card Id.</para>
    /// <para>Display Name: Reminder Action Card Id.</para>
    /// </summary>
    [AttributeLogicalName("reminderactioncardid")]
    [DisplayName("Reminder Action Card Id.")]
    public Guid? ReminderActionCardId
    {
        get => GetAttributeValue<Guid?>("reminderactioncardid");
        set => SetAttributeValue("reminderactioncardid", value);
    }

    /// <summary>
    /// <para>Shows the number of replies received for an email.</para>
    /// <para>Display Name: Reply Count</para>
    /// </summary>
    [AttributeLogicalName("replycount")]
    [DisplayName("Reply Count")]
    [Range(0, 2147483647)]
    public int? ReplyCount
    {
        get => GetAttributeValue<int?>("replycount");
        set => SetAttributeValue("replycount", value);
    }

    /// <summary>
    /// <para>For internal use only</para>
    /// <para>Display Name: Reserved for internal use</para>
    /// </summary>
    [AttributeLogicalName("reservedforinternaluse")]
    [DisplayName("Reserved for internal use")]
    [MaxLength(40000)]
    public string? ReservedForInternalUse
    {
        get => GetAttributeValue<string?>("reservedforinternaluse");
        set => SetAttributeValue("reservedforinternaluse", value);
    }

    /// <summary>
    /// <para>Scheduled duration of the email activity, specified in minutes.</para>
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
    /// <para>Enter the expected due date and time for the activity to be completed to provide details about when the email will be sent.</para>
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
    /// <para>Enter the expected start date and time for the activity to provide details about the tentative time when the email activity must be initiated.</para>
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
    /// <para>Sender of the email.</para>
    /// <para>Display Name: From</para>
    /// </summary>
    [AttributeLogicalName("sender")]
    [DisplayName("From")]
    [MaxLength(250)]
    public string? Sender
    {
        get => GetAttributeValue<string?>("sender");
        set => SetAttributeValue("sender", value);
    }

    /// <summary>
    /// <para>Select the mailbox associated with the sender of the email message.</para>
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
    /// <para>Shows the parent account of the sender of the email.</para>
    /// <para>Display Name: Senders Account</para>
    /// </summary>
    [AttributeLogicalName("sendersaccount")]
    [DisplayName("Senders Account")]
    public EntityReference? SendersAccount
    {
        get => GetAttributeValue<EntityReference?>("sendersaccount");
        set => SetAttributeValue("sendersaccount", value);
    }

    /// <summary>
    /// <para>The sensitivity label assigned to the Email.</para>
    /// <para>Display Name: Sensitivity Label</para>
    /// </summary>
    [AttributeLogicalName("sensitivitylabelid")]
    [DisplayName("Sensitivity Label")]
    public EntityReference? SensitivityLabelId
    {
        get => GetAttributeValue<EntityReference?>("sensitivitylabelid");
        set => SetAttributeValue("sensitivitylabelid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Sensitivity Label Info</para>
    /// </summary>
    [AttributeLogicalName("sensitivitylabelinfo")]
    [DisplayName("Sensitivity Label Info")]
    [MaxLength(1073741823)]
    public string? SensitivityLabelInfo
    {
        get => GetAttributeValue<string?>("sensitivitylabelinfo");
        set => SetAttributeValue("sensitivitylabelinfo", value);
    }

    /// <summary>
    /// <para>Shows the date and time that the email was sent.</para>
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
    /// <para>Choose the service level agreement (SLA) that you want to apply to the email record.</para>
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
    /// <para>Last SLA that was applied to this email. This field is for internal use only.</para>
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
    /// <para>Shows whether the email is open, completed, or canceled. Completed and canceled email is read-only and can't be edited.</para>
    /// <para>Display Name: Activity Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Activity Status")]
    public email_statecode? StateCode
    {
        get => this.GetOptionSetValue<email_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Select the email's status.</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public email_statuscode? StatusCode
    {
        get => this.GetOptionSetValue<email_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>Type a subcategory to identify the email type and relate the activity to a specific product, sales region, business group, or other function.</para>
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
    /// <para>Type a short description about the objective or primary topic of the email.</para>
    /// <para>Display Name: Subject</para>
    /// </summary>
    [AttributeLogicalName("subject")]
    [DisplayName("Subject")]
    [MaxLength(800)]
    public string? Subject
    {
        get => GetAttributeValue<string?>("subject");
        set => SetAttributeValue("subject", value);
    }

    /// <summary>
    /// <para>Shows the Microsoft Office Outlook account for the user who submitted the email to Microsoft Dynamics 365.</para>
    /// <para>Display Name: Submitted By</para>
    /// </summary>
    [AttributeLogicalName("submittedby")]
    [DisplayName("Submitted By")]
    [MaxLength(250)]
    public string? SubmittedBy
    {
        get => GetAttributeValue<string?>("submittedby");
        set => SetAttributeValue("submittedby", value);
    }

    /// <summary>
    /// <para>For internal use only. ID for template used in email.</para>
    /// <para>Display Name: ID for template used.</para>
    /// </summary>
    [AttributeLogicalName("templateid")]
    [DisplayName("ID for template used.")]
    public EntityReference? TemplateId
    {
        get => GetAttributeValue<EntityReference?>("templateid");
        set => SetAttributeValue("templateid", value);
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
    /// <para>Enter the account, contact, lead, queue, or user recipients for the email.</para>
    /// <para>Display Name: To</para>
    /// </summary>
    [AttributeLogicalName("to")]
    [DisplayName("To")]
    public IEnumerable<ActivityParty> to
    {
        get => GetEntityCollection<ActivityParty>("to");
        set => SetEntityCollection("to", value);
    }

    /// <summary>
    /// <para>Shows the email addresses corresponding to the recipients.</para>
    /// <para>Display Name: To Recipients</para>
    /// </summary>
    [AttributeLogicalName("torecipients")]
    [DisplayName("To Recipients")]
    [MaxLength(500)]
    public string? ToRecipients
    {
        get => GetAttributeValue<string?>("torecipients");
        set => SetAttributeValue("torecipients", value);
    }

    /// <summary>
    /// <para>Shows the tracking token assigned to the email to make sure responses are automatically tracked in Microsoft Dynamics 365.</para>
    /// <para>Display Name: Tracking Token</para>
    /// </summary>
    [AttributeLogicalName("trackingtoken")]
    [DisplayName("Tracking Token")]
    [MaxLength(50)]
    public string? TrackingToken
    {
        get => GetAttributeValue<string?>("trackingtoken");
        set => SetAttributeValue("trackingtoken", value);
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
    /// <para>Version number of the email message.</para>
    /// <para>Display Name: Version Number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version Number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    [AttributeLogicalName("emailsender")]
    [RelationshipSchemaName("Account_Email_EmailSender")]
    [RelationshipMetadata("ManyToOne", "emailsender", "account", "accountid", "Referencing")]
    public Account Account_Email_EmailSender
    {
        get => GetRelatedEntity<Account>("Account_Email_EmailSender", null);
        set => SetRelatedEntity("Account_Email_EmailSender", null, value);
    }

    [AttributeLogicalName("sendersaccount")]
    [RelationshipSchemaName("Account_Email_SendersAccount")]
    [RelationshipMetadata("ManyToOne", "sendersaccount", "account", "accountid", "Referencing")]
    public Account Account_Email_SendersAccount
    {
        get => GetRelatedEntity<Account>("Account_Email_SendersAccount", null);
        set => SetRelatedEntity("Account_Email_SendersAccount", null, value);
    }

    [AttributeLogicalName("regardingobjectid")]
    [RelationshipSchemaName("Account_Emails")]
    [RelationshipMetadata("ManyToOne", "regardingobjectid", "account", "accountid", "Referencing")]
    public Account Account_Emails
    {
        get => GetRelatedEntity<Account>("Account_Emails", null);
        set => SetRelatedEntity("Account_Emails", null, value);
    }

    [AttributeLogicalName("activityid")]
    [RelationshipSchemaName("activity_pointer_email")]
    [RelationshipMetadata("ManyToOne", "activityid", "activitypointer", "activityid", "Referencing")]
    public ActivityPointer activity_pointer_email
    {
        get => GetRelatedEntity<ActivityPointer>("activity_pointer_email", null);
        set => SetRelatedEntity("activity_pointer_email", null, value);
    }

    [AttributeLogicalName("owningbusinessunit")]
    [RelationshipSchemaName("business_unit_email_activities")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_email_activities
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_email_activities", null);
        set => SetRelatedEntity("business_unit_email_activities", null, value);
    }

    [AttributeLogicalName("emailsender")]
    [RelationshipSchemaName("Contact_Email_EmailSender")]
    [RelationshipMetadata("ManyToOne", "emailsender", "contact", "contactid", "Referencing")]
    public Contact Contact_Email_EmailSender
    {
        get => GetRelatedEntity<Contact>("Contact_Email_EmailSender", null);
        set => SetRelatedEntity("Contact_Email_EmailSender", null, value);
    }

    [AttributeLogicalName("regardingobjectid")]
    [RelationshipSchemaName("Contact_Emails")]
    [RelationshipMetadata("ManyToOne", "regardingobjectid", "contact", "contactid", "Referencing")]
    public Contact Contact_Emails
    {
        get => GetRelatedEntity<Contact>("Contact_Emails", null);
        set => SetRelatedEntity("Contact_Emails", null, value);
    }

    [AttributeLogicalName("acceptingentityid")]
    [RelationshipSchemaName("email_acceptingentity_systemuser")]
    [RelationshipMetadata("ManyToOne", "acceptingentityid", "systemuser", "systemuserid", "Referencing")]
    public SystemUser email_acceptingentity_systemuser
    {
        get => GetRelatedEntity<SystemUser>("email_acceptingentity_systemuser", null);
        set => SetRelatedEntity("email_acceptingentity_systemuser", null, value);
    }

    [RelationshipSchemaName("email_activity_parties")]
    [RelationshipMetadata("OneToMany", "activityid", "activityparty", "activityid", "Referenced")]
    public IEnumerable<ActivityParty> email_activity_parties
    {
        get => GetRelatedEntities<ActivityParty>("email_activity_parties", null);
        set => SetRelatedEntities("email_activity_parties", null, value);
    }

    [AttributeLogicalName("correlatedactivityid")]
    [RelationshipSchemaName("email_email_CorrelatedActivityId")]
    [RelationshipMetadata("ManyToOne", "correlatedactivityid", "email", "activityid", "Referencing")]
    public Email email_email_CorrelatedActivityId
    {
        get => GetRelatedEntity<Email>("email_email_CorrelatedActivityId", null);
        set => SetRelatedEntity("email_email_CorrelatedActivityId", null, value);
    }

    [AttributeLogicalName("parentactivityid")]
    [RelationshipSchemaName("email_email_parentactivityid")]
    [RelationshipMetadata("ManyToOne", "parentactivityid", "email", "activityid", "Referencing")]
    public Email email_email_parentactivityid
    {
        get => GetRelatedEntity<Email>("email_email_parentactivityid", null);
        set => SetRelatedEntity("email_email_parentactivityid", null, value);
    }

    [AttributeLogicalName("templateid")]
    [RelationshipSchemaName("Email_EmailTemplate")]
    [RelationshipMetadata("ManyToOne", "templateid", "template", "templateid", "Referencing")]
    public Template Email_EmailTemplate
    {
        get => GetRelatedEntity<Template>("Email_EmailTemplate", null);
        set => SetRelatedEntity("Email_EmailTemplate", null, value);
    }

    [RelationshipSchemaName("email_FileAttachments")]
    [RelationshipMetadata("OneToMany", "activityid", "fileattachment", "objectid", "Referenced")]
    public IEnumerable<FileAttachment> email_FileAttachments
    {
        get => GetRelatedEntities<FileAttachment>("email_FileAttachments", null);
        set => SetRelatedEntities("email_FileAttachments", null, value);
    }

    [AttributeLogicalName("descriptionblobid")]
    [RelationshipSchemaName("FileAttachment_Email_DescriptionBlobId")]
    [RelationshipMetadata("ManyToOne", "descriptionblobid", "fileattachment", "fileattachmentid", "Referencing")]
    public FileAttachment FileAttachment_Email_DescriptionBlobId
    {
        get => GetRelatedEntity<FileAttachment>("FileAttachment_Email_DescriptionBlobId", null);
        set => SetRelatedEntity("FileAttachment_Email_DescriptionBlobId", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_email_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_email_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_email_createdby", null);
        set => SetRelatedEntity("lk_email_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_email_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_email_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_email_createdonbehalfby", null);
        set => SetRelatedEntity("lk_email_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_email_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_email_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_email_modifiedby", null);
        set => SetRelatedEntity("lk_email_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_email_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_email_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_email_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_email_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("emailsender")]
    [RelationshipSchemaName("SystemUser_Email_EmailSender")]
    [RelationshipMetadata("ManyToOne", "emailsender", "systemuser", "systemuserid", "Referencing")]
    public SystemUser SystemUser_Email_EmailSender
    {
        get => GetRelatedEntity<SystemUser>("SystemUser_Email_EmailSender", null);
        set => SetRelatedEntity("SystemUser_Email_EmailSender", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_email")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_email
    {
        get => GetRelatedEntity<Team>("team_email", null);
        set => SetRelatedEntity("team_email", null, value);
    }

    [AttributeLogicalName("transactioncurrencyid")]
    [RelationshipSchemaName("TransactionCurrency_Email")]
    [RelationshipMetadata("ManyToOne", "transactioncurrencyid", "transactioncurrency", "transactioncurrencyid", "Referencing")]
    public TransactionCurrency TransactionCurrency_Email
    {
        get => GetRelatedEntity<TransactionCurrency>("TransactionCurrency_Email", null);
        set => SetRelatedEntity("TransactionCurrency_Email", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("user_email")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser user_email
    {
        get => GetRelatedEntity<SystemUser>("user_email", null);
        set => SetRelatedEntity("user_email", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Email entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Email, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Email with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Email to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Email</returns>
    public static Email Retrieve(IOrganizationService service, Guid id, params Expression<Func<Email, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
