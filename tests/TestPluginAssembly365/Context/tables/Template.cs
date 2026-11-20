using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Template for an email message that contains the standard attributes of an email message.</para>
/// <para>Display Name: Email Template</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("template")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Template : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "template";
    public const int EntityTypeCode = 2010;

    public Template() : base(EntityLogicalName) { }
    public Template(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("title");

    [AttributeLogicalName("templateid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("templateid", value);
        }
    }

    /// <summary>
    /// <para>Body text of the email template.</para>
    /// <para>Display Name: Body</para>
    /// </summary>
    [AttributeLogicalName("body")]
    [DisplayName("Body")]
    [MaxLength(1073741823)]
    public string? Body
    {
        get => GetAttributeValue<string?>("body");
        set => SetAttributeValue("body", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Component State</para>
    /// </summary>
    [AttributeLogicalName("componentstate")]
    [DisplayName("Component State")]
    public componentstate? ComponentState
    {
        get => this.GetOptionSetValue<componentstate>("componentstate");
        set => this.SetOptionSetValue("componentstate", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the email template.</para>
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
    /// <para>Date and time when the email template was created.</para>
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
    /// <para>Unique identifier of the delegate user who created the template.</para>
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
    /// <para>Description of the email template.</para>
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
    /// <para>Display Name: entityimageid</para>
    /// </summary>
    [AttributeLogicalName("entityimageid")]
    [DisplayName("entityimageid")]
    public Guid? EntityImageId
    {
        get => GetAttributeValue<Guid?>("entityimageid");
        set => SetAttributeValue("entityimageid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Generation Type Code</para>
    /// </summary>
    [AttributeLogicalName("generationtypecode")]
    [DisplayName("Generation Type Code")]
    [Range(0, 2147483647)]
    public int? GenerationTypeCode
    {
        get => GetAttributeValue<int?>("generationtypecode");
        set => SetAttributeValue("generationtypecode", value);
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
    /// <para>Version in which the form is introduced.</para>
    /// <para>Display Name: Introduced Version</para>
    /// </summary>
    [AttributeLogicalName("introducedversion")]
    [DisplayName("Introduced Version")]
    [MaxLength(48)]
    public string? IntroducedVersion
    {
        get => GetAttributeValue<string?>("introducedversion");
        set => SetAttributeValue("introducedversion", value);
    }

    /// <summary>
    /// <para>Information that specifies whether this component can be customized.</para>
    /// <para>Display Name: Customizable</para>
    /// </summary>
    [AttributeLogicalName("iscustomizable")]
    [DisplayName("Customizable")]
    public BooleanManagedProperty IsCustomizable
    {
        get => GetAttributeValue<BooleanManagedProperty>("iscustomizable");
        set => SetAttributeValue("iscustomizable", value);
    }

    /// <summary>
    /// <para>Indicates whether the solution component is part of a managed solution.</para>
    /// <para>Display Name: Is Managed</para>
    /// </summary>
    [AttributeLogicalName("ismanaged")]
    [DisplayName("Is Managed")]
    public bool? IsManaged
    {
        get => GetAttributeValue<bool?>("ismanaged");
        set => SetAttributeValue("ismanaged", value);
    }

    /// <summary>
    /// <para>Information about whether the template is personal or is available to all users.</para>
    /// <para>Display Name: Viewable By</para>
    /// </summary>
    [AttributeLogicalName("ispersonal")]
    [DisplayName("Viewable By")]
    public bool? IsPersonal
    {
        get => GetAttributeValue<bool?>("ispersonal");
        set => SetAttributeValue("ispersonal", value);
    }

    /// <summary>
    /// <para>Indicates if a template is recommended by Dynamics 365.</para>
    /// <para>Display Name: Recommended</para>
    /// </summary>
    [AttributeLogicalName("isrecommended")]
    [DisplayName("Recommended")]
    public bool? IsRecommended
    {
        get => GetAttributeValue<bool?>("isrecommended");
        set => SetAttributeValue("isrecommended", value);
    }

    /// <summary>
    /// <para>Language of the email template.</para>
    /// <para>Display Name: Language</para>
    /// </summary>
    [AttributeLogicalName("languagecode")]
    [DisplayName("Language")]
    [Range(0, 2147483647)]
    public int? LanguageCode
    {
        get => GetAttributeValue<int?>("languagecode");
        set => SetAttributeValue("languagecode", value);
    }

    /// <summary>
    /// <para>MIME type of the email template.</para>
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
    /// <para>Unique identifier of the user who last modified the template.</para>
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
    /// <para>Date and time when the email template was last modified.</para>
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
    /// <para>Unique identifier of the delegate user who last modified the template.</para>
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
    /// <para>For internal use only. Shows the number of times emails that use this template have been opened.</para>
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
    /// <para>Shows the open rate of this template. This is based on number of opens on followed emails that use this template.</para>
    /// <para>Display Name: Open Rate</para>
    /// </summary>
    [AttributeLogicalName("openrate")]
    [DisplayName("Open Rate")]
    [Range(0, 2147483647)]
    public int? OpenRate
    {
        get => GetAttributeValue<int?>("openrate");
        set => SetAttributeValue("openrate", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Record Overwrite Time</para>
    /// </summary>
    [AttributeLogicalName("overwritetime")]
    [DisplayName("Record Overwrite Time")]
    public DateTime? OverwriteTime
    {
        get => GetAttributeValue<DateTime?>("overwritetime");
        set => SetAttributeValue("overwritetime", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user or team who owns the template for the email activity.</para>
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
    /// <para>Unique identifier of the business unit that owns the template.</para>
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
    /// <para>Unique identifier of the team who owns the template.</para>
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
    /// <para>Unique identifier of the user who owns the template.</para>
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
    /// <para>XML data for the body of the email template.</para>
    /// <para>Display Name: Presentation XML</para>
    /// </summary>
    [AttributeLogicalName("presentationxml")]
    [DisplayName("Presentation XML")]
    [MaxLength(1073741823)]
    public string? PresentationXml
    {
        get => GetAttributeValue<string?>("presentationxml");
        set => SetAttributeValue("presentationxml", value);
    }

    /// <summary>
    /// <para>For internal use only. Shows the number of times emails that use this template have received replies.</para>
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
    /// <para>Shows the reply rate for this template. This is based on number of replies received on followed emails that use this template.</para>
    /// <para>Display Name: Reply Rate</para>
    /// </summary>
    [AttributeLogicalName("replyrate")]
    [DisplayName("Reply Rate")]
    [Range(0, 2147483647)]
    public int? ReplyRate
    {
        get => GetAttributeValue<int?>("replyrate");
        set => SetAttributeValue("replyrate", value);
    }

    /// <summary>
    /// <para>Safe html of email template.</para>
    /// <para>Display Name: Safe HTML of email template</para>
    /// </summary>
    [AttributeLogicalName("safehtml")]
    [DisplayName("Safe HTML of email template")]
    [MaxLength(1073741823)]
    public string? SafeHtml
    {
        get => GetAttributeValue<string?>("safehtml");
        set => SetAttributeValue("safehtml", value);
    }

    /// <summary>
    /// <para>Unique identifier of the associated solution.</para>
    /// <para>Display Name: Solution</para>
    /// </summary>
    [AttributeLogicalName("solutionid")]
    [DisplayName("Solution")]
    public Guid? SolutionId
    {
        get => GetAttributeValue<Guid?>("solutionid");
        set => SetAttributeValue("solutionid", value);
    }

    /// <summary>
    /// <para>Subject associated with the email template.</para>
    /// <para>Display Name: Subject</para>
    /// </summary>
    [AttributeLogicalName("subject")]
    [DisplayName("Subject")]
    [MaxLength(5000)]
    public string? Subject
    {
        get => GetAttributeValue<string?>("subject");
        set => SetAttributeValue("subject", value);
    }

    /// <summary>
    /// <para>XML data for the subject of the email template.</para>
    /// <para>Display Name: Subject XML</para>
    /// </summary>
    [AttributeLogicalName("subjectpresentationxml")]
    [DisplayName("Subject XML")]
    [MaxLength(1073741823)]
    public string? SubjectPresentationXml
    {
        get => GetAttributeValue<string?>("subjectpresentationxml");
        set => SetAttributeValue("subjectpresentationxml", value);
    }

    /// <summary>
    /// <para>Safe html of email template subject.</para>
    /// <para>Display Name: Safe HTML of email template subject</para>
    /// </summary>
    [AttributeLogicalName("subjectsafehtml")]
    [DisplayName("Safe HTML of email template subject")]
    [MaxLength(1073741823)]
    public string? SubjectSafeHtml
    {
        get => GetAttributeValue<string?>("subjectsafehtml");
        set => SetAttributeValue("subjectsafehtml", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: Solution</para>
    /// </summary>
    [AttributeLogicalName("supportingsolutionid")]
    [DisplayName("Solution")]
    public Guid? SupportingSolutionId
    {
        get => GetAttributeValue<Guid?>("supportingsolutionid");
        set => SetAttributeValue("supportingsolutionid", value);
    }

    /// <summary>
    /// <para>Display Name: Email Template</para>
    /// </summary>
    [AttributeLogicalName("templateid")]
    [DisplayName("Email Template")]
    public Guid? TemplateId
    {
        get => GetAttributeValue<Guid?>("templateid");
        set => SetId("templateid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: templateidunique</para>
    /// </summary>
    [AttributeLogicalName("templateidunique")]
    [DisplayName("templateidunique")]
    public Guid? TemplateIdUnique
    {
        get => GetAttributeValue<Guid?>("templateidunique");
        set => SetAttributeValue("templateidunique", value);
    }

    /// <summary>
    /// <para>Type of email template.</para>
    /// <para>Display Name: Template Type</para>
    /// </summary>
    [AttributeLogicalName("templatetypecode")]
    [DisplayName("Template Type")]
    [MaxLength()]
    public string? TemplateTypeCode
    {
        get => GetAttributeValue<string?>("templatetypecode");
        set => SetAttributeValue("templatetypecode", value);
    }

    /// <summary>
    /// <para>Title of the template.</para>
    /// <para>Display Name: Title</para>
    /// </summary>
    [AttributeLogicalName("title")]
    [DisplayName("Title")]
    [MaxLength(200)]
    public string? Title
    {
        get => GetAttributeValue<string?>("title");
        set => SetAttributeValue("title", value);
    }

    /// <summary>
    /// <para>Shows the number of sent emails that use this template.</para>
    /// <para>Display Name: Sent email count</para>
    /// </summary>
    [AttributeLogicalName("usedcount")]
    [DisplayName("Sent email count")]
    [Range(0, 2147483647)]
    public int? UsedCount
    {
        get => GetAttributeValue<int?>("usedcount");
        set => SetAttributeValue("usedcount", value);
    }

    /// <summary>
    /// <para>Version number of the template.</para>
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
    [RelationshipSchemaName("business_unit_templates")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_templates
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_templates", null);
        set => SetRelatedEntity("business_unit_templates", null, value);
    }

    [RelationshipSchemaName("Email_EmailTemplate")]
    [RelationshipMetadata("OneToMany", "templateid", "email", "templateid", "Referenced")]
    public IEnumerable<Email> Email_EmailTemplate
    {
        get => GetRelatedEntities<Email>("Email_EmailTemplate", null);
        set => SetRelatedEntities("Email_EmailTemplate", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_templatebase_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_templatebase_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_templatebase_createdby", null);
        set => SetRelatedEntity("lk_templatebase_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_templatebase_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_templatebase_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_templatebase_createdonbehalfby", null);
        set => SetRelatedEntity("lk_templatebase_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_templatebase_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_templatebase_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_templatebase_modifiedby", null);
        set => SetRelatedEntity("lk_templatebase_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_templatebase_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_templatebase_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_templatebase_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_templatebase_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("system_user_email_templates")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser system_user_email_templates
    {
        get => GetRelatedEntity<SystemUser>("system_user_email_templates", null);
        set => SetRelatedEntity("system_user_email_templates", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_email_templates")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_email_templates
    {
        get => GetRelatedEntity<Team>("team_email_templates", null);
        set => SetRelatedEntity("team_email_templates", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Template entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Template, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Template with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Template to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Template</returns>
    public static Template Retrieve(IOrganizationService service, Guid id, params Expression<Func<Template, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
