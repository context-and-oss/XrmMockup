using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace XrmMockup.MetadataGenerator.Tool.Context;

/// <summary>
/// <para>Set of logical rules that define the steps necessary to automate a specific business process, task, or set of actions to be performed.</para>
/// <para>Display Name: Process</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("workflow")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Workflow : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "workflow";
    public const int EntityTypeCode = 4703;

    public Workflow() : base(EntityLogicalName) { }
    public Workflow(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("name");

    [AttributeLogicalName("workflowid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("workflowid", value);
        }
    }

    /// <summary>
    /// <para>Unique identifier of the latest activation record for the process.</para>
    /// <para>Display Name: Active Process ID</para>
    /// </summary>
    [AttributeLogicalName("activeworkflowid")]
    [DisplayName("Active Process ID")]
    public EntityReference? ActiveWorkflowId
    {
        get => GetAttributeValue<EntityReference?>("activeworkflowid");
        set => SetAttributeValue("activeworkflowid", value);
    }

    /// <summary>
    /// <para>Indicates whether the asynchronous system job is automatically deleted on completion.</para>
    /// <para>Display Name: Delete Job On Completion</para>
    /// </summary>
    [AttributeLogicalName("asyncautodelete")]
    [DisplayName("Delete Job On Completion")]
    public bool? AsyncAutoDelete
    {
        get => GetAttributeValue<bool?>("asyncautodelete");
        set => SetAttributeValue("asyncautodelete", value);
    }

    /// <summary>
    /// <para>Billing context this flow is in.</para>
    /// <para>Display Name: BillingContext</para>
    /// </summary>
    [AttributeLogicalName("billingcontext")]
    [DisplayName("BillingContext")]
    [MaxLength(100000)]
    public string? BillingContext
    {
        get => GetAttributeValue<string?>("billingcontext");
        set => SetAttributeValue("billingcontext", value);
    }

    /// <summary>
    /// <para>Business Process Type.</para>
    /// <para>Display Name: Business Process Type</para>
    /// </summary>
    [AttributeLogicalName("businessprocesstype")]
    [DisplayName("Business Process Type")]
    public workflow_businessprocesstype? BusinessProcessType
    {
        get => this.GetOptionSetValue<workflow_businessprocesstype>("businessprocesstype");
        set => this.SetOptionSetValue("businessprocesstype", value);
    }

    /// <summary>
    /// <para>Category of the process.</para>
    /// <para>Display Name: Category</para>
    /// </summary>
    [AttributeLogicalName("category")]
    [DisplayName("Category")]
    public workflow_category? Category
    {
        get => this.GetOptionSetValue<workflow_category>("category");
        set => this.SetOptionSetValue("category", value);
    }

    /// <summary>
    /// <para>Claims related to this workflow.</para>
    /// <para>Display Name: Claims</para>
    /// </summary>
    [AttributeLogicalName("claims")]
    [DisplayName("Claims")]
    [MaxLength(100000)]
    public string? Claims
    {
        get => GetAttributeValue<string?>("claims");
        set => SetAttributeValue("claims", value);
    }

    /// <summary>
    /// <para>Business logic converted into client data</para>
    /// <para>Display Name: Client Data</para>
    /// </summary>
    [AttributeLogicalName("clientdata")]
    [DisplayName("Client Data")]
    [MaxLength(1073741823)]
    public string? ClientData
    {
        get => GetAttributeValue<string?>("clientdata");
        set => SetAttributeValue("clientdata", value);
    }

    /// <summary>
    /// <para>For Internal Use Only.</para>
    /// <para>Display Name: Client Data Is Compressed</para>
    /// </summary>
    [AttributeLogicalName("clientdataiscompressed")]
    [DisplayName("Client Data Is Compressed")]
    public bool? ClientDataIsCompressed
    {
        get => GetAttributeValue<bool?>("clientdataiscompressed");
        set => SetAttributeValue("clientdataiscompressed", value);
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
    /// <para>Connection References related to this workflow.</para>
    /// <para>Display Name: Connection references</para>
    /// </summary>
    [AttributeLogicalName("connectionreferences")]
    [DisplayName("Connection references")]
    [MaxLength(100000)]
    public string? ConnectionReferences
    {
        get => GetAttributeValue<string?>("connectionreferences");
        set => SetAttributeValue("connectionreferences", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the process.</para>
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
    /// <para>Date and time when the process was created.</para>
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
    /// <para>Unique identifier of the delegate user who created the process.</para>
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
    /// <para>Create metadata for this workflow.</para>
    /// <para>Display Name: Create metadata</para>
    /// </summary>
    [AttributeLogicalName("createmetadata")]
    [DisplayName("Create metadata")]
    [MaxLength(100000)]
    public string? CreateMetadata
    {
        get => GetAttributeValue<string?>("createmetadata");
        set => SetAttributeValue("createmetadata", value);
    }

    /// <summary>
    /// <para>Stage of the process when triggered on Create.</para>
    /// <para>Display Name: Create Stage</para>
    /// </summary>
    [AttributeLogicalName("createstage")]
    [DisplayName("Create Stage")]
    public workflow_stage? CreateStage
    {
        get => this.GetOptionSetValue<workflow_stage>("createstage");
        set => this.SetOptionSetValue("createstage", value);
    }

    /// <summary>
    /// <para>Credentials related to this workflow.</para>
    /// <para>Display Name: Credentials</para>
    /// </summary>
    [AttributeLogicalName("credentials")]
    [DisplayName("Credentials")]
    [MaxLength(100000)]
    public string? Credentials
    {
        get => GetAttributeValue<string?>("credentials");
        set => SetAttributeValue("credentials", value);
    }

    /// <summary>
    /// <para>Definition of the business logic of this workflow instance.</para>
    /// <para>Display Name: Definition</para>
    /// </summary>
    [AttributeLogicalName("definition")]
    [DisplayName("Definition")]
    [MaxLength(16777216)]
    public string? Definition
    {
        get => GetAttributeValue<string?>("definition");
        set => SetAttributeValue("definition", value);
    }

    /// <summary>
    /// <para>Stage of the process when triggered on Delete.</para>
    /// <para>Display Name: Delete stage</para>
    /// </summary>
    [AttributeLogicalName("deletestage")]
    [DisplayName("Delete stage")]
    public workflow_stage? DeleteStage
    {
        get => this.GetOptionSetValue<workflow_stage>("deletestage");
        set => this.SetOptionSetValue("deletestage", value);
    }

    /// <summary>
    /// <para>Soft dependencies of this workflow instance.</para>
    /// <para>Display Name: Dependencies</para>
    /// </summary>
    [AttributeLogicalName("dependencies")]
    [DisplayName("Dependencies")]
    [MaxLength(100000)]
    public string? Dependencies
    {
        get => GetAttributeValue<string?>("dependencies");
        set => SetAttributeValue("dependencies", value);
    }

    /// <summary>
    /// <para>Description of the process.</para>
    /// <para>Display Name: Description</para>
    /// </summary>
    [AttributeLogicalName("description")]
    [DisplayName("Description")]
    [MaxLength(100000)]
    public string? Description
    {
        get => GetAttributeValue<string?>("description");
        set => SetAttributeValue("description", value);
    }

    /// <summary>
    /// <para>Desktop flow modules related to this workflow.</para>
    /// <para>Display Name: Desktop flow modules</para>
    /// </summary>
    [AttributeLogicalName("desktopflowmodules")]
    [DisplayName("Desktop flow modules")]
    [MaxLength(100000)]
    public string? DesktopFlowModules
    {
        get => GetAttributeValue<string?>("desktopflowmodules");
        set => SetAttributeValue("desktopflowmodules", value);
    }

    /// <summary>
    /// <para>comma separated list of one or more Dynamics First Party Solution Unique names that this workflow is in context of.</para>
    /// <para>Display Name: DynamicsSolutionContext</para>
    /// </summary>
    [AttributeLogicalName("dynamicssolutioncontext")]
    [DisplayName("DynamicsSolutionContext")]
    [MaxLength(10000)]
    public string? DynamicsSolutionContext
    {
        get => GetAttributeValue<string?>("dynamicssolutioncontext");
        set => SetAttributeValue("dynamicssolutioncontext", value);
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
    /// <para>Flow group the flow is associated with.</para>
    /// <para>Display Name: Flow Group</para>
    /// </summary>
    [AttributeLogicalName("flowgroup")]
    [DisplayName("Flow Group")]
    public EntityReference? FlowGroup
    {
        get => GetAttributeValue<EntityReference?>("flowgroup");
        set => SetAttributeValue("flowgroup", value);
    }

    /// <summary>
    /// <para>Unique identifier of the associated form.</para>
    /// <para>Display Name: Form ID</para>
    /// </summary>
    [AttributeLogicalName("formid")]
    [DisplayName("Form ID")]
    public Guid? FormId
    {
        get => GetAttributeValue<Guid?>("formid");
        set => SetAttributeValue("formid", value);
    }

    /// <summary>
    /// <para>Input parameters to the process.</para>
    /// <para>Display Name: Input Parameters</para>
    /// </summary>
    [AttributeLogicalName("inputparameters")]
    [DisplayName("Input Parameters")]
    [MaxLength(1073741823)]
    public string? InputParameters
    {
        get => GetAttributeValue<string?>("inputparameters");
        set => SetAttributeValue("inputparameters", value);
    }

    /// <summary>
    /// <para>Inputs definition for this workflow.</para>
    /// <para>Display Name: Inputs</para>
    /// </summary>
    [AttributeLogicalName("inputs")]
    [DisplayName("Inputs")]
    [MaxLength(1048576)]
    public string? Inputs
    {
        get => GetAttributeValue<string?>("inputs");
        set => SetAttributeValue("inputs", value);
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
    /// <para>Indicates whether the process was created using the Microsoft Dynamics 365 Web application.</para>
    /// <para>Display Name: Is CRM Process</para>
    /// </summary>
    [AttributeLogicalName("iscrmuiworkflow")]
    [DisplayName("Is CRM Process")]
    public bool? IsCrmUIWorkflow
    {
        get => GetAttributeValue<bool?>("iscrmuiworkflow");
        set => SetAttributeValue("iscrmuiworkflow", value);
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
    /// <para>Defines whether other publishers can attach custom processing steps to this action</para>
    /// <para>Display Name: Allow custom processing step for other publishers</para>
    /// </summary>
    [AttributeLogicalName("iscustomprocessingstepallowedforotherpublishers")]
    [DisplayName("Allow custom processing step for other publishers")]
    public BooleanManagedProperty IsCustomProcessingStepAllowedForOtherPublishers
    {
        get => GetAttributeValue<BooleanManagedProperty>("iscustomprocessingstepallowedforotherpublishers");
        set => SetAttributeValue("iscustomprocessingstepallowedforotherpublishers", value);
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
    /// <para>Whether or not the steps in the process are executed in a single transaction.</para>
    /// <para>Display Name: Is Transacted</para>
    /// </summary>
    [AttributeLogicalName("istransacted")]
    [DisplayName("Is Transacted")]
    public bool? IsTransacted
    {
        get => GetAttributeValue<bool?>("istransacted");
        set => SetAttributeValue("istransacted", value);
    }

    /// <summary>
    /// <para>Language of the process.</para>
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
    /// <para>The user object that should be used to establish the license the flow should operate under.</para>
    /// <para>Display Name: Licensee</para>
    /// </summary>
    [AttributeLogicalName("licensee")]
    [DisplayName("Licensee")]
    public EntityReference? Licensee
    {
        get => GetAttributeValue<EntityReference?>("licensee");
        set => SetAttributeValue("licensee", value);
    }

    /// <summary>
    /// <para>The source of the license entitlements.</para>
    /// <para>Display Name: License entitled by</para>
    /// </summary>
    [AttributeLogicalName("licenseentitledby")]
    [DisplayName("License entitled by")]
    public EntityReference? LicenseEntitledBy
    {
        get => GetAttributeValue<EntityReference?>("licenseentitledby");
        set => SetAttributeValue("licenseentitledby", value);
    }

    /// <summary>
    /// <para>Additional metadata for this workflow.</para>
    /// <para>Display Name: Metadata</para>
    /// </summary>
    [AttributeLogicalName("metadata")]
    [DisplayName("Metadata")]
    [MaxLength(100000)]
    public string? Metadata
    {
        get => GetAttributeValue<string?>("metadata");
        set => SetAttributeValue("metadata", value);
    }

    /// <summary>
    /// <para>Shows the mode of the process.</para>
    /// <para>Display Name: Mode</para>
    /// </summary>
    [AttributeLogicalName("mode")]
    [DisplayName("Mode")]
    public workflow_mode? Mode
    {
        get => this.GetOptionSetValue<workflow_mode>("mode");
        set => this.SetOptionSetValue("mode", value);
    }

    /// <summary>
    /// <para>Type of the Modern Flow.</para>
    /// <para>Display Name: Modern Flow Type</para>
    /// </summary>
    [AttributeLogicalName("modernflowtype")]
    [DisplayName("Modern Flow Type")]
    public workflow_modernflowtype? ModernFlowType
    {
        get => this.GetOptionSetValue<workflow_modernflowtype>("modernflowtype");
        set => this.SetOptionSetValue("modernflowtype", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who last modified the process.</para>
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
    /// <para>Date and time when the process was last modified.</para>
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
    /// <para>Unique identifier of the delegate user who last modified the process.</para>
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
    /// <para>Flow modify metadata used for telemetry, etc.</para>
    /// <para>Display Name: ModifyMetadata</para>
    /// </summary>
    [AttributeLogicalName("modifymetadata")]
    [DisplayName("ModifyMetadata")]
    [MaxLength(100000)]
    public string? ModifyMetadata
    {
        get => GetAttributeValue<string?>("modifymetadata");
        set => SetAttributeValue("modifymetadata", value);
    }

    /// <summary>
    /// <para>Name of the process.</para>
    /// <para>Display Name: Process Name</para>
    /// </summary>
    [AttributeLogicalName("name")]
    [DisplayName("Process Name")]
    [MaxLength(100)]
    public string? Name
    {
        get => GetAttributeValue<string?>("name");
        set => SetAttributeValue("name", value);
    }

    /// <summary>
    /// <para>Indicates whether the process is able to run as an on-demand process.</para>
    /// <para>Display Name: Run as On Demand</para>
    /// </summary>
    [AttributeLogicalName("ondemand")]
    [DisplayName("Run as On Demand")]
    public bool? OnDemand
    {
        get => GetAttributeValue<bool?>("ondemand");
        set => SetAttributeValue("ondemand", value);
    }

    /// <summary>
    /// <para>Outputs definition for this workflow.</para>
    /// <para>Display Name: Outputs</para>
    /// </summary>
    [AttributeLogicalName("outputs")]
    [DisplayName("Outputs")]
    [MaxLength(1048576)]
    public string? Outputs
    {
        get => GetAttributeValue<string?>("outputs");
        set => SetAttributeValue("outputs", value);
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
    /// <para>Unique identifier of the user or team who owns the process.</para>
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
    /// <para>Unique identifier of the business unit that owns the process.</para>
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
    /// <para>Unique identifier of the team who owns the process.</para>
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
    /// <para>Unique identifier of the user who owns the process.</para>
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
    /// <para>Unique identifier of the definition for process activation.</para>
    /// <para>Display Name: Parent Process ID</para>
    /// </summary>
    [AttributeLogicalName("parentworkflowid")]
    [DisplayName("Parent Process ID")]
    public EntityReference? ParentWorkflowId
    {
        get => GetAttributeValue<EntityReference?>("parentworkflowid");
        set => SetAttributeValue("parentworkflowid", value);
    }

    /// <summary>
    /// <para>For Internal Use Only.</para>
    /// <para>Display Name: Plan Verified</para>
    /// </summary>
    [AttributeLogicalName("planverified")]
    [DisplayName("Plan Verified")]
    public bool? PlanVerified
    {
        get => GetAttributeValue<bool?>("planverified");
        set => SetAttributeValue("planverified", value);
    }

    /// <summary>
    /// <para>Unique identifier of the plug-in type.</para>
    /// <para>Display Name: plugintypeid</para>
    /// </summary>
    [AttributeLogicalName("plugintypeid")]
    [DisplayName("plugintypeid")]
    public EntityReference? PluginTypeId
    {
        get => GetAttributeValue<EntityReference?>("plugintypeid");
        set => SetAttributeValue("plugintypeid", value);
    }

    /// <summary>
    /// <para>Primary entity for the process. The process can be associated for one or more SDK operations defined on the primary entity.</para>
    /// <para>Display Name: Primary Entity</para>
    /// </summary>
    [AttributeLogicalName("primaryentity")]
    [DisplayName("Primary Entity")]
    [MaxLength()]
    public string? PrimaryEntity
    {
        get => GetAttributeValue<string?>("primaryentity");
        set => SetAttributeValue("primaryentity", value);
    }

    /// <summary>
    /// <para>Type the business process flow order.</para>
    /// <para>Display Name: Process Order</para>
    /// </summary>
    [AttributeLogicalName("processorder")]
    [DisplayName("Process Order")]
    [Range(0, 2147483647)]
    public int? ProcessOrder
    {
        get => GetAttributeValue<int?>("processorder");
        set => SetAttributeValue("processorder", value);
    }

    /// <summary>
    /// <para>Contains the role assignment for the process.</para>
    /// <para>Display Name: Role assignment for Process</para>
    /// </summary>
    [AttributeLogicalName("processroleassignment")]
    [DisplayName("Role assignment for Process")]
    [MaxLength(1048576)]
    public string? ProcessRoleAssignment
    {
        get => GetAttributeValue<string?>("processroleassignment");
        set => SetAttributeValue("processroleassignment", value);
    }

    /// <summary>
    /// <para>Unique identifier of the associated form for process trigger.</para>
    /// <para>Display Name: ProcessTriggerFormId</para>
    /// </summary>
    [AttributeLogicalName("processtriggerformid")]
    [DisplayName("ProcessTriggerFormId")]
    public Guid? ProcessTriggerFormId
    {
        get => GetAttributeValue<Guid?>("processtriggerformid");
        set => SetAttributeValue("processtriggerformid", value);
    }

    /// <summary>
    /// <para>Scope of the process trigger.</para>
    /// <para>Display Name: ProcessTriggerScope</para>
    /// </summary>
    [AttributeLogicalName("processtriggerscope")]
    [DisplayName("ProcessTriggerScope")]
    public processtrigger_scope? ProcessTriggerScope
    {
        get => this.GetOptionSetValue<processtrigger_scope>("processtriggerscope");
        set => this.SetOptionSetValue("processtriggerscope", value);
    }

    /// <summary>
    /// <para>Indicates the rank for order of execution for the synchronous workflow.</para>
    /// <para>Display Name: Rank</para>
    /// </summary>
    [AttributeLogicalName("rank")]
    [DisplayName("Rank")]
    [Range(-2147483648, 2147483647)]
    public int? Rank
    {
        get => GetAttributeValue<int?>("rank");
        set => SetAttributeValue("rank", value);
    }

    /// <summary>
    /// <para>The renderer type of Workflow</para>
    /// <para>Display Name: Renderer Type</para>
    /// </summary>
    [AttributeLogicalName("rendererobjecttypecode")]
    [DisplayName("Renderer Type")]
    [MaxLength()]
    public string? RendererObjectTypeCode
    {
        get => GetAttributeValue<string?>("rendererobjecttypecode");
        set => SetAttributeValue("rendererobjecttypecode", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: ResourceContainer</para>
    /// </summary>
    [AttributeLogicalName("resourcecontainer")]
    [DisplayName("ResourceContainer")]
    [MaxLength(10000)]
    public string? ResourceContainer
    {
        get => GetAttributeValue<string?>("resourcecontainer");
        set => SetAttributeValue("resourcecontainer", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: ResourceId</para>
    /// </summary>
    [AttributeLogicalName("resourceid")]
    [DisplayName("ResourceId")]
    public Guid? ResourceId
    {
        get => GetAttributeValue<Guid?>("resourceid");
        set => SetAttributeValue("resourceid", value);
    }

    /// <summary>
    /// <para>Specifies the system user account under which a workflow executes.</para>
    /// <para>Display Name: Run As User</para>
    /// </summary>
    [AttributeLogicalName("runas")]
    [DisplayName("Run As User")]
    public workflow_runas? RunAs
    {
        get => this.GetOptionSetValue<workflow_runas>("runas");
        set => this.SetOptionSetValue("runas", value);
    }

    /// <summary>
    /// <para>Schema version for this workflow.</para>
    /// <para>Display Name: Schema Version</para>
    /// </summary>
    [AttributeLogicalName("schemaversion")]
    [DisplayName("Schema Version")]
    [MaxLength(100)]
    public string? SchemaVersion
    {
        get => GetAttributeValue<string?>("schemaversion");
        set => SetAttributeValue("schemaversion", value);
    }

    /// <summary>
    /// <para>Scope of the process.</para>
    /// <para>Display Name: Scope</para>
    /// </summary>
    [AttributeLogicalName("scope")]
    [DisplayName("Scope")]
    public workflow_scope? Scope
    {
        get => this.GetOptionSetValue<workflow_scope>("scope");
        set => this.SetOptionSetValue("scope", value);
    }

    /// <summary>
    /// <para>Unique identifier of the SDK Message associated with this workflow.</para>
    /// <para>Display Name: SDK Message</para>
    /// </summary>
    [AttributeLogicalName("sdkmessageid")]
    [DisplayName("SDK Message")]
    public EntityReference? SdkMessageId
    {
        get => GetAttributeValue<EntityReference?>("sdkmessageid");
        set => SetAttributeValue("sdkmessageid", value);
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
    /// <para>Status of the workflow</para>
    /// <para>Display Name: Status</para>
    /// </summary>
    [AttributeLogicalName("statecode")]
    [DisplayName("Status")]
    public workflow_statecode? StateCode
    {
        get => this.GetOptionSetValue<workflow_statecode>("statecode");
        set => this.SetOptionSetValue("statecode", value);
    }

    /// <summary>
    /// <para>Reason for the status of the workflow</para>
    /// <para>Display Name: Status Reason</para>
    /// </summary>
    [AttributeLogicalName("statuscode")]
    [DisplayName("Status Reason")]
    public workflow_statuscode? StatusCode
    {
        get => this.GetOptionSetValue<workflow_statuscode>("statuscode");
        set => this.SetOptionSetValue("statuscode", value);
    }

    /// <summary>
    /// <para>Indicates whether the process can be included in other processes as a child process.</para>
    /// <para>Display Name: Is Child Process</para>
    /// </summary>
    [AttributeLogicalName("subprocess")]
    [DisplayName("Is Child Process")]
    public bool? Subprocess
    {
        get => GetAttributeValue<bool?>("subprocess");
        set => SetAttributeValue("subprocess", value);
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
    /// <para>Display Name: suspensionreasondetails</para>
    /// </summary>
    [AttributeLogicalName("suspensionreasondetails")]
    [DisplayName("suspensionreasondetails")]
    [MaxLength(500)]
    public string? SuspensionReasonDetails
    {
        get => GetAttributeValue<string?>("suspensionreasondetails");
        set => SetAttributeValue("suspensionreasondetails", value);
    }

    /// <summary>
    /// <para>Select whether synchronous workflow failures will be saved to log files.</para>
    /// <para>Display Name: Log upon Failure</para>
    /// </summary>
    [AttributeLogicalName("syncworkflowlogonfailure")]
    [DisplayName("Log upon Failure")]
    public bool? SyncWorkflowLogOnFailure
    {
        get => GetAttributeValue<bool?>("syncworkflowlogonfailure");
        set => SetAttributeValue("syncworkflowlogonfailure", value);
    }

    /// <summary>
    /// <para>The throttling behavior type.</para>
    /// <para>Display Name: Throttling behavior type</para>
    /// </summary>
    [AttributeLogicalName("throttlingbehavior")]
    [DisplayName("Throttling behavior type")]
    public workflow_throttlingbehaviortype? ThrottlingBehavior
    {
        get => this.GetOptionSetValue<workflow_throttlingbehaviortype>("throttlingbehavior");
        set => this.SetOptionSetValue("throttlingbehavior", value);
    }

    /// <summary>
    /// <para>Indicates whether the process will be triggered when the primary entity is created.</para>
    /// <para>Display Name: Trigger On Create</para>
    /// </summary>
    [AttributeLogicalName("triggeroncreate")]
    [DisplayName("Trigger On Create")]
    public bool? TriggerOnCreate
    {
        get => GetAttributeValue<bool?>("triggeroncreate");
        set => SetAttributeValue("triggeroncreate", value);
    }

    /// <summary>
    /// <para>Indicates whether the process will be triggered on deletion of the primary entity.</para>
    /// <para>Display Name: Trigger On Delete</para>
    /// </summary>
    [AttributeLogicalName("triggerondelete")]
    [DisplayName("Trigger On Delete")]
    public bool? TriggerOnDelete
    {
        get => GetAttributeValue<bool?>("triggerondelete");
        set => SetAttributeValue("triggerondelete", value);
    }

    /// <summary>
    /// <para>Attributes that trigger the process when updated.</para>
    /// <para>Display Name: Trigger On Update Attribute List</para>
    /// </summary>
    [AttributeLogicalName("triggeronupdateattributelist")]
    [DisplayName("Trigger On Update Attribute List")]
    [MaxLength(1073741823)]
    public string? TriggerOnUpdateAttributeList
    {
        get => GetAttributeValue<string?>("triggeronupdateattributelist");
        set => SetAttributeValue("triggeronupdateattributelist", value);
    }

    /// <summary>
    /// <para>For Internal Use Only.</para>
    /// <para>Display Name: Trusted Access</para>
    /// </summary>
    [AttributeLogicalName("trustedaccess")]
    [DisplayName("Trusted Access")]
    public bool? TrustedAccess
    {
        get => GetAttributeValue<bool?>("trustedaccess");
        set => SetAttributeValue("trustedaccess", value);
    }

    /// <summary>
    /// <para>Type of the process.</para>
    /// <para>Display Name: Type</para>
    /// </summary>
    [AttributeLogicalName("type")]
    [DisplayName("Type")]
    public workflow_type? Type
    {
        get => this.GetOptionSetValue<workflow_type>("type");
        set => this.SetOptionSetValue("type", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: UI Data</para>
    /// </summary>
    [AttributeLogicalName("uidata")]
    [DisplayName("UI Data")]
    [MaxLength(1073741823)]
    public string? UIData
    {
        get => GetAttributeValue<string?>("uidata");
        set => SetAttributeValue("uidata", value);
    }

    /// <summary>
    /// <para>Type of the UI Flow process.</para>
    /// <para>Display Name: UI Flow Type</para>
    /// </summary>
    [AttributeLogicalName("uiflowtype")]
    [DisplayName("UI Flow Type")]
    public workflow_uiflowtype? UIFlowType
    {
        get => this.GetOptionSetValue<workflow_uiflowtype>("uiflowtype");
        set => this.SetOptionSetValue("uiflowtype", value);
    }

    /// <summary>
    /// <para>Unique name of the process</para>
    /// <para>Display Name: Unique Name</para>
    /// </summary>
    [AttributeLogicalName("uniquename")]
    [DisplayName("Unique Name")]
    [MaxLength(256)]
    public string? UniqueName
    {
        get => GetAttributeValue<string?>("uniquename");
        set => SetAttributeValue("uniquename", value);
    }

    /// <summary>
    /// <para>Select the stage a process will be triggered on update.</para>
    /// <para>Display Name: Update Stage</para>
    /// </summary>
    [AttributeLogicalName("updatestage")]
    [DisplayName("Update Stage")]
    public workflow_stage? UpdateStage
    {
        get => this.GetOptionSetValue<workflow_stage>("updatestage");
        set => this.SetOptionSetValue("updatestage", value);
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

    /// <summary>
    /// <para>Display Name: Process</para>
    /// </summary>
    [AttributeLogicalName("workflowid")]
    [DisplayName("Process")]
    public Guid? WorkflowId
    {
        get => GetAttributeValue<Guid?>("workflowid");
        set => SetId("workflowid", value);
    }

    /// <summary>
    /// <para>For internal use only.</para>
    /// <para>Display Name: workflowidunique</para>
    /// </summary>
    [AttributeLogicalName("workflowidunique")]
    [DisplayName("workflowidunique")]
    public Guid? WorkflowIdUnique
    {
        get => GetAttributeValue<Guid?>("workflowidunique");
        set => SetAttributeValue("workflowidunique", value);
    }

    /// <summary>
    /// <para>XAML that defines the process.</para>
    /// <para>Display Name: xaml</para>
    /// </summary>
    [AttributeLogicalName("xaml")]
    [DisplayName("xaml")]
    [MaxLength(1073741823)]
    public string? Xaml
    {
        get => GetAttributeValue<string?>("xaml");
        set => SetAttributeValue("xaml", value);
    }

    [AttributeLogicalName("owningbusinessunit")]
    [RelationshipSchemaName("business_unit_workflow")]
    [RelationshipMetadata("ManyToOne", "owningbusinessunit", "businessunit", "businessunitid", "Referencing")]
    public BusinessUnit business_unit_workflow
    {
        get => GetRelatedEntity<BusinessUnit>("business_unit_workflow", null);
        set => SetRelatedEntity("business_unit_workflow", null, value);
    }

    [AttributeLogicalName("owninguser")]
    [RelationshipSchemaName("system_user_workflow")]
    [RelationshipMetadata("ManyToOne", "owninguser", "systemuser", "systemuserid", "Referencing")]
    public SystemUser system_user_workflow
    {
        get => GetRelatedEntity<SystemUser>("system_user_workflow", null);
        set => SetRelatedEntity("system_user_workflow", null, value);
    }

    [AttributeLogicalName("owningteam")]
    [RelationshipSchemaName("team_workflow")]
    [RelationshipMetadata("ManyToOne", "owningteam", "team", "teamid", "Referencing")]
    public Team team_workflow
    {
        get => GetRelatedEntity<Team>("team_workflow", null);
        set => SetRelatedEntity("team_workflow", null, value);
    }

    [AttributeLogicalName("activeworkflowid")]
    [RelationshipSchemaName("workflow_active_workflow")]
    [RelationshipMetadata("ManyToOne", "activeworkflowid", "workflow", "workflowid", "Referencing")]
    public Workflow workflow_active_workflow
    {
        get => GetRelatedEntity<Workflow>("workflow_active_workflow", null);
        set => SetRelatedEntity("workflow_active_workflow", null, value);
    }

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("workflow_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser workflow_createdby
    {
        get => GetRelatedEntity<SystemUser>("workflow_createdby", null);
        set => SetRelatedEntity("workflow_createdby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("workflow_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser workflow_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("workflow_createdonbehalfby", null);
        set => SetRelatedEntity("workflow_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("licensee")]
    [RelationshipSchemaName("Workflow_licensee")]
    [RelationshipMetadata("ManyToOne", "licensee", "systemuser", "systemuserid", "Referencing")]
    public SystemUser Workflow_licensee
    {
        get => GetRelatedEntity<SystemUser>("Workflow_licensee", null);
        set => SetRelatedEntity("Workflow_licensee", null, value);
    }

    [AttributeLogicalName("licenseentitledby")]
    [RelationshipSchemaName("Workflow_licenseentitledby")]
    [RelationshipMetadata("ManyToOne", "licenseentitledby", "workflow", "workflowid", "Referencing")]
    public Workflow Workflow_licenseentitledby
    {
        get => GetRelatedEntity<Workflow>("Workflow_licenseentitledby", null);
        set => SetRelatedEntity("Workflow_licenseentitledby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("workflow_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser workflow_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("workflow_modifiedby", null);
        set => SetRelatedEntity("workflow_modifiedby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("workflow_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser workflow_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("workflow_modifiedonbehalfby", null);
        set => SetRelatedEntity("workflow_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("parentworkflowid")]
    [RelationshipSchemaName("workflow_parent_workflow")]
    [RelationshipMetadata("ManyToOne", "parentworkflowid", "workflow", "workflowid", "Referencing")]
    public Workflow workflow_parent_workflow
    {
        get => GetRelatedEntity<Workflow>("workflow_parent_workflow", null);
        set => SetRelatedEntity("workflow_parent_workflow", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Workflow entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Workflow, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Workflow with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Workflow to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Workflow</returns>
    public static Workflow Retrieve(IOrganizationService service, Guid id, params Expression<Func<Workflow, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
