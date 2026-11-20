using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace XrmMockup.MetadataGenerator.Tool.Context;

/// <summary>
/// <para>A solution which contains CRM customizations.</para>
/// <para>Display Name: Solution</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("solution")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class Solution : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "solution";
    public const int EntityTypeCode = 7100;

    public Solution() : base(EntityLogicalName) { }
    public Solution(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("friendlyname");

    [AttributeLogicalName("solutionid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("solutionid", value);
        }
    }

    /// <summary>
    /// <para>A link to an optional configuration page for this solution.</para>
    /// <para>Display Name: Configuration Page</para>
    /// </summary>
    [AttributeLogicalName("configurationpageid")]
    [DisplayName("Configuration Page")]
    public EntityReference? ConfigurationPageId
    {
        get => GetAttributeValue<EntityReference?>("configurationpageid");
        set => SetAttributeValue("configurationpageid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the solution.</para>
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
    /// <para>Date and time when the solution was created.</para>
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
    /// <para>Unique identifier of the delegate user who created the solution.</para>
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
    /// <para>Description of the solution.</para>
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
    /// <para>Indicates if solution is enabled for source control integration</para>
    /// <para>Display Name: Enabled for Source Control Integration</para>
    /// </summary>
    [AttributeLogicalName("enabledforsourcecontrolintegration")]
    [DisplayName("Enabled for Source Control Integration")]
    public bool? EnabledForSourceControlIntegration
    {
        get => GetAttributeValue<bool?>("enabledforsourcecontrolintegration");
        set => SetAttributeValue("enabledforsourcecontrolintegration", value);
    }

    /// <summary>
    /// <para>File Id for the blob url used for file storage.</para>
    /// <para>Display Name: File Id</para>
    /// </summary>
    [AttributeLogicalName("fileid")]
    [DisplayName("File Id")]
    public byte[] FileId
    {
        get => GetAttributeValue<byte[]>("fileid");
        set => SetAttributeValue("fileid", value);
    }

    /// <summary>
    /// <para>User display name for the solution.</para>
    /// <para>Display Name: Display Name</para>
    /// </summary>
    [AttributeLogicalName("friendlyname")]
    [DisplayName("Display Name")]
    [MaxLength(256)]
    public string? FriendlyName
    {
        get => GetAttributeValue<string?>("friendlyname");
        set => SetAttributeValue("friendlyname", value);
    }

    /// <summary>
    /// <para>Date and time when the solution was installed/upgraded.</para>
    /// <para>Display Name: Installed On</para>
    /// </summary>
    [AttributeLogicalName("installedon")]
    [DisplayName("Installed On")]
    public DateTime? InstalledOn
    {
        get => GetAttributeValue<DateTime?>("installedon");
        set => SetAttributeValue("installedon", value);
    }

    /// <summary>
    /// <para>Information about whether the solution is api managed.</para>
    /// <para>Display Name: Is Api Managed Solution</para>
    /// </summary>
    [AttributeLogicalName("isapimanaged")]
    [DisplayName("Is Api Managed Solution")]
    public bool? IsApiManaged
    {
        get => GetAttributeValue<bool?>("isapimanaged");
        set => SetAttributeValue("isapimanaged", value);
    }

    /// <summary>
    /// <para>Indicates whether the solution is internal or not.</para>
    /// <para>Display Name: Is internal solution</para>
    /// </summary>
    [AttributeLogicalName("isinternal")]
    [DisplayName("Is internal solution")]
    public bool? IsInternal
    {
        get => GetAttributeValue<bool?>("isinternal");
        set => SetAttributeValue("isinternal", value);
    }

    /// <summary>
    /// <para>Indicates whether the solution is managed or unmanaged.</para>
    /// <para>Display Name: Package Type</para>
    /// </summary>
    [AttributeLogicalName("ismanaged")]
    [DisplayName("Package Type")]
    public bool? IsManaged
    {
        get => GetAttributeValue<bool?>("ismanaged");
        set => SetAttributeValue("ismanaged", value);
    }

    /// <summary>
    /// <para>Indicates whether the solution is visible outside of the platform.</para>
    /// <para>Display Name: Is Visible Outside Platform</para>
    /// </summary>
    [AttributeLogicalName("isvisible")]
    [DisplayName("Is Visible Outside Platform")]
    public bool? IsVisible
    {
        get => GetAttributeValue<bool?>("isvisible");
        set => SetAttributeValue("isvisible", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who last modified the solution.</para>
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
    /// <para>Date and time when the solution was last modified.</para>
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
    /// <para>Unique identifier of the delegate user who modified the solution.</para>
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
    /// <para>Unique identifier of the organization associated with the solution.</para>
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
    /// <para>Unique identifier of the parent solution. Should only be non-null if this solution is a patch.</para>
    /// <para>Display Name: Parent Solution</para>
    /// </summary>
    [AttributeLogicalName("parentsolutionid")]
    [DisplayName("Parent Solution")]
    public EntityReference? ParentSolutionId
    {
        get => GetAttributeValue<EntityReference?>("parentsolutionid");
        set => SetAttributeValue("parentsolutionid", value);
    }

    /// <summary>
    /// <para>Display Name: pinpointassetid</para>
    /// </summary>
    [AttributeLogicalName("pinpointassetid")]
    [DisplayName("pinpointassetid")]
    [MaxLength(255)]
    public string? PinpointAssetId
    {
        get => GetAttributeValue<string?>("pinpointassetid");
        set => SetAttributeValue("pinpointassetid", value);
    }

    /// <summary>
    /// <para>Identifier of the publisher of this solution in Microsoft Pinpoint.</para>
    /// <para>Display Name: pinpointpublisherid</para>
    /// </summary>
    [AttributeLogicalName("pinpointpublisherid")]
    [DisplayName("pinpointpublisherid")]
    public long? PinpointPublisherId
    {
        get => GetAttributeValue<long?>("pinpointpublisherid");
        set => SetAttributeValue("pinpointpublisherid", value);
    }

    /// <summary>
    /// <para>Default locale of the solution in Microsoft Pinpoint.</para>
    /// <para>Display Name: pinpointsolutiondefaultlocale</para>
    /// </summary>
    [AttributeLogicalName("pinpointsolutiondefaultlocale")]
    [DisplayName("pinpointsolutiondefaultlocale")]
    [MaxLength(16)]
    public string? PinpointSolutionDefaultLocale
    {
        get => GetAttributeValue<string?>("pinpointsolutiondefaultlocale");
        set => SetAttributeValue("pinpointsolutiondefaultlocale", value);
    }

    /// <summary>
    /// <para>Identifier of the solution in Microsoft Pinpoint.</para>
    /// <para>Display Name: pinpointsolutionid</para>
    /// </summary>
    [AttributeLogicalName("pinpointsolutionid")]
    [DisplayName("pinpointsolutionid")]
    public long? PinpointSolutionId
    {
        get => GetAttributeValue<long?>("pinpointsolutionid");
        set => SetAttributeValue("pinpointsolutionid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the publisher.</para>
    /// <para>Display Name: Publisher</para>
    /// </summary>
    [AttributeLogicalName("publisherid")]
    [DisplayName("Publisher")]
    public EntityReference? PublisherId
    {
        get => GetAttributeValue<EntityReference?>("publisherid");
        set => SetAttributeValue("publisherid", value);
    }

    /// <summary>
    /// <para>Display Name: Solution Identifier</para>
    /// </summary>
    [AttributeLogicalName("solutionid")]
    [DisplayName("Solution Identifier")]
    public Guid? SolutionId
    {
        get => GetAttributeValue<Guid?>("solutionid");
        set => SetId("solutionid", value);
    }

    /// <summary>
    /// <para>Solution package source organization version</para>
    /// <para>Display Name: Solution Package Version</para>
    /// </summary>
    [AttributeLogicalName("solutionpackageversion")]
    [DisplayName("Solution Package Version")]
    [MaxLength(256)]
    public string? SolutionPackageVersion
    {
        get => GetAttributeValue<string?>("solutionpackageversion");
        set => SetAttributeValue("solutionpackageversion", value);
    }

    /// <summary>
    /// <para>Solution Type</para>
    /// <para>Display Name: Solution Type</para>
    /// </summary>
    [AttributeLogicalName("solutiontype")]
    [DisplayName("Solution Type")]
    public solution_solutiontype? SolutionType
    {
        get => this.GetOptionSetValue<solution_solutiontype>("solutiontype");
        set => this.SetOptionSetValue("solutiontype", value);
    }

    /// <summary>
    /// <para>Indicates the current status of source control integration</para>
    /// <para>Display Name: Source Control Sync Status</para>
    /// </summary>
    [AttributeLogicalName("sourcecontrolsyncstatus")]
    [DisplayName("Source Control Sync Status")]
    public solution_sourcecontrolsyncstatus? SourceControlSyncStatus
    {
        get => this.GetOptionSetValue<solution_sourcecontrolsyncstatus>("sourcecontrolsyncstatus");
        set => this.SetOptionSetValue("sourcecontrolsyncstatus", value);
    }

    /// <summary>
    /// <para>The template suffix of this solution</para>
    /// <para>Display Name: Suffix</para>
    /// </summary>
    [AttributeLogicalName("templatesuffix")]
    [DisplayName("Suffix")]
    [MaxLength(65)]
    public string? TemplateSuffix
    {
        get => GetAttributeValue<string?>("templatesuffix");
        set => SetAttributeValue("templatesuffix", value);
    }

    /// <summary>
    /// <para>thumbprint of the solution signature</para>
    /// <para>Display Name: Thumbprint</para>
    /// </summary>
    [AttributeLogicalName("thumbprint")]
    [DisplayName("Thumbprint")]
    [MaxLength(65)]
    public string? Thumbprint
    {
        get => GetAttributeValue<string?>("thumbprint");
        set => SetAttributeValue("thumbprint", value);
    }

    /// <summary>
    /// <para>The unique name of this solution</para>
    /// <para>Display Name: Name</para>
    /// </summary>
    [AttributeLogicalName("uniquename")]
    [DisplayName("Name")]
    [MaxLength(65)]
    public string? UniqueName
    {
        get => GetAttributeValue<string?>("uniquename");
        set => SetAttributeValue("uniquename", value);
    }

    /// <summary>
    /// <para>Date and time when the solution was updated.</para>
    /// <para>Display Name: Updated On</para>
    /// </summary>
    [AttributeLogicalName("updatedon")]
    [DisplayName("Updated On")]
    public DateTime? UpdatedOn
    {
        get => GetAttributeValue<DateTime?>("updatedon");
        set => SetAttributeValue("updatedon", value);
    }

    /// <summary>
    /// <para>Contains component info for the solution upgrade operation</para>
    /// <para>Display Name: upgradeinfo</para>
    /// </summary>
    [AttributeLogicalName("upgradeinfo")]
    [DisplayName("upgradeinfo")]
    [MaxLength(1073741823)]
    public string? UpgradeInfo
    {
        get => GetAttributeValue<string?>("upgradeinfo");
        set => SetAttributeValue("upgradeinfo", value);
    }

    /// <summary>
    /// <para>Solution version, used to identify a solution for upgrades and hotfixes.</para>
    /// <para>Display Name: Version</para>
    /// </summary>
    [AttributeLogicalName("version")]
    [DisplayName("Version")]
    [MaxLength(256)]
    public string? Version
    {
        get => GetAttributeValue<string?>("version");
        set => SetAttributeValue("version", value);
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

    [AttributeLogicalName("createdby")]
    [RelationshipSchemaName("lk_solution_createdby")]
    [RelationshipMetadata("ManyToOne", "createdby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_solution_createdby
    {
        get => GetRelatedEntity<SystemUser>("lk_solution_createdby", null);
        set => SetRelatedEntity("lk_solution_createdby", null, value);
    }

    [AttributeLogicalName("modifiedby")]
    [RelationshipSchemaName("lk_solution_modifiedby")]
    [RelationshipMetadata("ManyToOne", "modifiedby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_solution_modifiedby
    {
        get => GetRelatedEntity<SystemUser>("lk_solution_modifiedby", null);
        set => SetRelatedEntity("lk_solution_modifiedby", null, value);
    }

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_solutionbase_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_solutionbase_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_solutionbase_createdonbehalfby", null);
        set => SetRelatedEntity("lk_solutionbase_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_solutionbase_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_solutionbase_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_solutionbase_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_solutionbase_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("parentsolutionid")]
    [RelationshipSchemaName("solution_parent_solution")]
    [RelationshipMetadata("ManyToOne", "parentsolutionid", "solution", "solutionid", "Referencing")]
    public Solution solution_parent_solution
    {
        get => GetRelatedEntity<Solution>("solution_parent_solution", null);
        set => SetRelatedEntity("solution_parent_solution", null, value);
    }

    [RelationshipSchemaName("solution_solutioncomponent")]
    [RelationshipMetadata("OneToMany", "solutionid", "solutioncomponent", "solutionid", "Referenced")]
    public IEnumerable<SolutionComponent> solution_solutioncomponent
    {
        get => GetRelatedEntities<SolutionComponent>("solution_solutioncomponent", null);
        set => SetRelatedEntities("solution_solutioncomponent", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the Solution entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<Solution, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the Solution with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of Solution to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved Solution</returns>
    public static Solution Retrieve(IOrganizationService service, Guid id, params Expression<Func<Solution, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
