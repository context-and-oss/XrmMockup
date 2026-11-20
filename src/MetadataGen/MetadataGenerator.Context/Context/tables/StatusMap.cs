using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace XrmMockup.MetadataGenerator.Tool.Context;

/// <summary>
/// <para>Mapping between statuses.</para>
/// <para>Display Name: Status Map</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("statusmap")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class StatusMap : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "statusmap";
    public const int EntityTypeCode = 1075;

    public StatusMap() : base(EntityLogicalName) { }
    public StatusMap(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("");

    [AttributeLogicalName("statusmapid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("statusmapid", value);
        }
    }

    /// <summary>
    /// <para>Display Name: isdefault</para>
    /// </summary>
    [AttributeLogicalName("isdefault")]
    [DisplayName("isdefault")]
    public bool? IsDefault
    {
        get => GetAttributeValue<bool?>("isdefault");
        set => SetAttributeValue("isdefault", value);
    }

    /// <summary>
    /// <para>Display Name: objecttypecode</para>
    /// </summary>
    [AttributeLogicalName("objecttypecode")]
    [DisplayName("objecttypecode")]
    [MaxLength()]
    public string? ObjectTypeCode
    {
        get => GetAttributeValue<string?>("objecttypecode");
        set => SetAttributeValue("objecttypecode", value);
    }

    /// <summary>
    /// <para>Display Name: organizationid</para>
    /// </summary>
    [AttributeLogicalName("organizationid")]
    [DisplayName("organizationid")]
    public Guid? OrganizationId
    {
        get => GetAttributeValue<Guid?>("organizationid");
        set => SetAttributeValue("organizationid", value);
    }

    /// <summary>
    /// <para>Display Name: state</para>
    /// </summary>
    [AttributeLogicalName("state")]
    [DisplayName("state")]
    [Range(-2147483648, 2147483647)]
    public int? State
    {
        get => GetAttributeValue<int?>("state");
        set => SetAttributeValue("state", value);
    }

    /// <summary>
    /// <para>Display Name: status</para>
    /// </summary>
    [AttributeLogicalName("status")]
    [DisplayName("status")]
    [Range(-2147483648, 2147483647)]
    public int? Status
    {
        get => GetAttributeValue<int?>("status");
        set => SetAttributeValue("status", value);
    }

    /// <summary>
    /// <para>Display Name: statusmapid</para>
    /// </summary>
    [AttributeLogicalName("statusmapid")]
    [DisplayName("statusmapid")]
    public Guid? StatusMapId
    {
        get => GetAttributeValue<Guid?>("statusmapid");
        set => SetId("statusmapid", value);
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
    /// Gets the logical column name for a property on the StatusMap entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<StatusMap, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the StatusMap with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of StatusMap to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved StatusMap</returns>
    public static StatusMap Retrieve(IOrganizationService service, Guid id, params Expression<Func<StatusMap, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
