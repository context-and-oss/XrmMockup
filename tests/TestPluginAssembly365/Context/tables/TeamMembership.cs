using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>Display Name: teammembership</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("teammembership")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class TeamMembership : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "teammembership";
    public const int EntityTypeCode = 23;

    public TeamMembership() : base(EntityLogicalName) { }
    public TeamMembership(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("");

    [AttributeLogicalName("teammembershipid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("teammembershipid", value);
        }
    }

    /// <summary>
    /// <para>Display Name: systemuserid</para>
    /// </summary>
    [AttributeLogicalName("systemuserid")]
    [DisplayName("systemuserid")]
    public Guid? SystemUserId
    {
        get => GetAttributeValue<Guid?>("systemuserid");
        set => SetAttributeValue("systemuserid", value);
    }

    /// <summary>
    /// <para>Display Name: teamid</para>
    /// </summary>
    [AttributeLogicalName("teamid")]
    [DisplayName("teamid")]
    public Guid? TeamId
    {
        get => GetAttributeValue<Guid?>("teamid");
        set => SetAttributeValue("teamid", value);
    }

    /// <summary>
    /// <para>Display Name: teammembershipid</para>
    /// </summary>
    [AttributeLogicalName("teammembershipid")]
    [DisplayName("teammembershipid")]
    public Guid? TeamMembershipId
    {
        get => GetAttributeValue<Guid?>("teammembershipid");
        set => SetId("teammembershipid", value);
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
    /// Gets the logical column name for a property on the TeamMembership entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<TeamMembership, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the TeamMembership with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of TeamMembership to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved TeamMembership</returns>
    public static TeamMembership Retrieve(IOrganizationService service, Guid id, params Expression<Func<TeamMembership, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
