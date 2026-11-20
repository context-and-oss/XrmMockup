using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace XrmMockup.MetadataGenerator.Tool.Context;

/// <summary>
/// <para>A component of a CRM solution.</para>
/// <para>Display Name: Solution Component</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("solutioncomponent")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class SolutionComponent : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "solutioncomponent";
    public const int EntityTypeCode = 7103;

    public SolutionComponent() : base(EntityLogicalName) { }
    public SolutionComponent(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("");

    [AttributeLogicalName("solutioncomponentid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("solutioncomponentid", value);
        }
    }

    /// <summary>
    /// <para>The object type code of the component.</para>
    /// <para>Display Name: Object Type Code</para>
    /// </summary>
    [AttributeLogicalName("componenttype")]
    [DisplayName("Object Type Code")]
    public componenttype? ComponentType
    {
        get => this.GetOptionSetValue<componenttype>("componenttype");
        set => this.SetOptionSetValue("componenttype", value);
    }

    /// <summary>
    /// <para>Unique identifier of the user who created the solution</para>
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
    /// <para>Indicates whether this component is metadata or data.</para>
    /// <para>Display Name: Is this component metadata</para>
    /// </summary>
    [AttributeLogicalName("ismetadata")]
    [DisplayName("Is this component metadata")]
    public bool? IsMetadata
    {
        get => GetAttributeValue<bool?>("ismetadata");
        set => SetAttributeValue("ismetadata", value);
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
    /// <para>Unique identifier of the object with which the component is associated.</para>
    /// <para>Display Name: Regarding</para>
    /// </summary>
    [AttributeLogicalName("objectid")]
    [DisplayName("Regarding")]
    public Guid? ObjectId
    {
        get => GetAttributeValue<Guid?>("objectid");
        set => SetAttributeValue("objectid", value);
    }

    /// <summary>
    /// <para>Indicates the include behavior of the root component.</para>
    /// <para>Display Name: Root Component Behavior</para>
    /// </summary>
    [AttributeLogicalName("rootcomponentbehavior")]
    [DisplayName("Root Component Behavior")]
    public solutioncomponent_rootcomponentbehavior? RootComponentBehavior
    {
        get => this.GetOptionSetValue<solutioncomponent_rootcomponentbehavior>("rootcomponentbehavior");
        set => this.SetOptionSetValue("rootcomponentbehavior", value);
    }

    /// <summary>
    /// <para>The parent ID of the subcomponent, which will be a root</para>
    /// <para>Display Name: Root Solution Component ID</para>
    /// </summary>
    [AttributeLogicalName("rootsolutioncomponentid")]
    [DisplayName("Root Solution Component ID")]
    public Guid? RootSolutionComponentId
    {
        get => GetAttributeValue<Guid?>("rootsolutioncomponentid");
        set => SetAttributeValue("rootsolutioncomponentid", value);
    }

    /// <summary>
    /// <para>Display Name: Solution Component Identifier</para>
    /// </summary>
    [AttributeLogicalName("solutioncomponentid")]
    [DisplayName("Solution Component Identifier")]
    public Guid? SolutionComponentId
    {
        get => GetAttributeValue<Guid?>("solutioncomponentid");
        set => SetId("solutioncomponentid", value);
    }

    /// <summary>
    /// <para>Unique identifier of the solution.</para>
    /// <para>Display Name: Solution</para>
    /// </summary>
    [AttributeLogicalName("solutionid")]
    [DisplayName("Solution")]
    public EntityReference? SolutionId
    {
        get => GetAttributeValue<EntityReference?>("solutionid");
        set => SetAttributeValue("solutionid", value);
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

    [AttributeLogicalName("createdonbehalfby")]
    [RelationshipSchemaName("lk_solutioncomponentbase_createdonbehalfby")]
    [RelationshipMetadata("ManyToOne", "createdonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_solutioncomponentbase_createdonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_solutioncomponentbase_createdonbehalfby", null);
        set => SetRelatedEntity("lk_solutioncomponentbase_createdonbehalfby", null, value);
    }

    [AttributeLogicalName("modifiedonbehalfby")]
    [RelationshipSchemaName("lk_solutioncomponentbase_modifiedonbehalfby")]
    [RelationshipMetadata("ManyToOne", "modifiedonbehalfby", "systemuser", "systemuserid", "Referencing")]
    public SystemUser lk_solutioncomponentbase_modifiedonbehalfby
    {
        get => GetRelatedEntity<SystemUser>("lk_solutioncomponentbase_modifiedonbehalfby", null);
        set => SetRelatedEntity("lk_solutioncomponentbase_modifiedonbehalfby", null, value);
    }

    [AttributeLogicalName("solutionid")]
    [RelationshipSchemaName("solution_solutioncomponent")]
    [RelationshipMetadata("ManyToOne", "solutionid", "solution", "solutionid", "Referencing")]
    public Solution solution_solutioncomponent
    {
        get => GetRelatedEntity<Solution>("solution_solutioncomponent", null);
        set => SetRelatedEntity("solution_solutioncomponent", null, value);
    }

    [AttributeLogicalName("rootsolutioncomponentid")]
    [RelationshipSchemaName("solutioncomponent_parent_solutioncomponent")]
    [RelationshipMetadata("ManyToOne", "rootsolutioncomponentid", "solutioncomponent", "solutioncomponentid", "Referencing")]
    public SolutionComponent solutioncomponent_parent_solutioncomponent
    {
        get => GetRelatedEntity<SolutionComponent>("solutioncomponent_parent_solutioncomponent", null);
        set => SetRelatedEntity("solutioncomponent_parent_solutioncomponent", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the SolutionComponent entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<SolutionComponent, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the SolutionComponent with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of SolutionComponent to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved SolutionComponent</returns>
    public static SolutionComponent Retrieve(IOrganizationService service, Guid id, params Expression<Func<SolutionComponent, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
