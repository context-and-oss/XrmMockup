using Microsoft.Xrm.Sdk;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

/// <summary>
/// <para>File Attachment</para>
/// <para>Display Name: FileAttachment</para>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[EntityLogicalName("fileattachment")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract]
#pragma warning disable CS8981 // Allows: Only lowercase characters
public partial class FileAttachment : ExtendedEntity
#pragma warning restore CS8981
{
    public const string EntityLogicalName = "fileattachment";
    public const int EntityTypeCode = 55;

    public FileAttachment() : base(EntityLogicalName) { }
    public FileAttachment(Guid id) : base(EntityLogicalName, id) { }

    private string DebuggerDisplay => GetDebuggerDisplay("filename");

    [AttributeLogicalName("fileattachmentid")]
    public override Guid Id {
        get {
            return base.Id;
        }
        set {
            SetId("fileattachmentid", value);
        }
    }

    /// <summary>
    /// <para>Body</para>
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
    /// <para>Date and time when the attachment was created.</para>
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
    /// <para>Display Name: FileAttachmentId</para>
    /// </summary>
    [AttributeLogicalName("fileattachmentid")]
    [DisplayName("FileAttachmentId")]
    public Guid? FileAttachmentId
    {
        get => GetAttributeValue<Guid?>("fileattachmentid");
        set => SetId("fileattachmentid", value);
    }

    /// <summary>
    /// <para>File name of the attachment.</para>
    /// <para>Display Name: File Name</para>
    /// </summary>
    [AttributeLogicalName("filename")]
    [DisplayName("File Name")]
    [MaxLength(255)]
    public string? FileName
    {
        get => GetAttributeValue<string?>("filename");
        set => SetAttributeValue("filename", value);
    }

    /// <summary>
    /// <para>File pointer of the attachment.</para>
    /// <para>Display Name: File Pointer</para>
    /// </summary>
    [AttributeLogicalName("filepointer")]
    [DisplayName("File Pointer")]
    [MaxLength(255)]
    public string? FilePointer
    {
        get => GetAttributeValue<string?>("filepointer");
        set => SetAttributeValue("filepointer", value);
    }

    /// <summary>
    /// <para>File size of the attachment in bytes.</para>
    /// <para>Display Name: File Size (Bytes)</para>
    /// </summary>
    [AttributeLogicalName("filesizeinbytes")]
    [DisplayName("File Size (Bytes)")]
    public long? FileSizeInBytes
    {
        get => GetAttributeValue<long?>("filesizeinbytes");
        set => SetAttributeValue("filesizeinbytes", value);
    }

    /// <summary>
    /// <para>IsCommitted</para>
    /// <para>Display Name: IsCommitted</para>
    /// </summary>
    [AttributeLogicalName("iscommitted")]
    [DisplayName("IsCommitted")]
    public bool? IsCommitted
    {
        get => GetAttributeValue<bool?>("iscommitted");
        set => SetAttributeValue("iscommitted", value);
    }

    /// <summary>
    /// <para>Indicates if file is compressed in the storage</para>
    /// <para>Display Name: Is Compressed</para>
    /// </summary>
    [AttributeLogicalName("iscompressed")]
    [DisplayName("Is Compressed")]
    public bool? IsCompressed
    {
        get => GetAttributeValue<bool?>("iscompressed");
        set => SetAttributeValue("iscompressed", value);
    }

    /// <summary>
    /// <para>MIME type of the attachment.</para>
    /// <para>Display Name: MIME Type</para>
    /// </summary>
    [AttributeLogicalName("mimetype")]
    [DisplayName("MIME Type")]
    [MaxLength(256)]
    public string? MimeType
    {
        get => GetAttributeValue<string?>("mimetype");
        set => SetAttributeValue("mimetype", value);
    }

    /// <summary>
    /// <para>Unique identifier of the object with which the attachment is associated.</para>
    /// <para>Display Name: Regarding</para>
    /// </summary>
    [AttributeLogicalName("objectid")]
    [DisplayName("Regarding")]
    public EntityReference? ObjectId
    {
        get => GetAttributeValue<EntityReference?>("objectid");
        set => SetAttributeValue("objectid", value);
    }

    /// <summary>
    /// <para>Type of entity with which the file attachment is associated.</para>
    /// <para>Display Name: Object Type </para>
    /// </summary>
    [AttributeLogicalName("objecttypecode")]
    [DisplayName("Object Type ")]
    [MaxLength()]
    public string? ObjectTypeCode
    {
        get => GetAttributeValue<string?>("objecttypecode");
        set => SetAttributeValue("objecttypecode", value);
    }

    /// <summary>
    /// <para>Prefix of the file pointer in blob storage.</para>
    /// <para>Display Name: Prefix</para>
    /// </summary>
    [AttributeLogicalName("prefix")]
    [DisplayName("Prefix")]
    [MaxLength(10)]
    public string? Prefix
    {
        get => GetAttributeValue<string?>("prefix");
        set => SetAttributeValue("prefix", value);
    }

    /// <summary>
    /// <para>Regarding attribute schema name of the attachment.</para>
    /// <para>Display Name: Regarding Attribute Schema Name</para>
    /// </summary>
    [AttributeLogicalName("regardingfieldname")]
    [DisplayName("Regarding Attribute Schema Name")]
    [MaxLength(255)]
    public string? RegardingFieldName
    {
        get => GetAttributeValue<string?>("regardingfieldname");
        set => SetAttributeValue("regardingfieldname", value);
    }

    /// <summary>
    /// <para>Storage pointer.</para>
    /// <para>Display Name: Storage Pointer</para>
    /// </summary>
    [AttributeLogicalName("storagepointer")]
    [DisplayName("Storage Pointer")]
    [MaxLength(10)]
    public string? StoragePointer
    {
        get => GetAttributeValue<string?>("storagepointer");
        set => SetAttributeValue("storagepointer", value);
    }

    /// <summary>
    /// <para>Version number of the file attachment.</para>
    /// <para>Display Name: Version Number</para>
    /// </summary>
    [AttributeLogicalName("versionnumber")]
    [DisplayName("Version Number")]
    public long? VersionNumber
    {
        get => GetAttributeValue<long?>("versionnumber");
        set => SetAttributeValue("versionnumber", value);
    }

    [AttributeLogicalName("objectid")]
    [RelationshipSchemaName("activitypointer_FileAttachments")]
    [RelationshipMetadata("ManyToOne", "objectid", "activitypointer", "activityid", "Referencing")]
    public ActivityPointer activitypointer_FileAttachments
    {
        get => GetRelatedEntity<ActivityPointer>("activitypointer_FileAttachments", null);
        set => SetRelatedEntity("activitypointer_FileAttachments", null, value);
    }

    [AttributeLogicalName("objectid")]
    [RelationshipSchemaName("email_FileAttachments")]
    [RelationshipMetadata("ManyToOne", "objectid", "email", "activityid", "Referencing")]
    public Email email_FileAttachments
    {
        get => GetRelatedEntity<Email>("email_FileAttachments", null);
        set => SetRelatedEntity("email_FileAttachments", null, value);
    }

    [RelationshipSchemaName("FileAttachment_ActivityPointer_DescriptionBlobId")]
    [RelationshipMetadata("OneToMany", "fileattachmentid", "activitypointer", "descriptionblobid", "Referenced")]
    public IEnumerable<ActivityPointer> FileAttachment_ActivityPointer_DescriptionBlobId
    {
        get => GetRelatedEntities<ActivityPointer>("FileAttachment_ActivityPointer_DescriptionBlobId", null);
        set => SetRelatedEntities("FileAttachment_ActivityPointer_DescriptionBlobId", null, value);
    }

    [RelationshipSchemaName("FileAttachment_Email_DescriptionBlobId")]
    [RelationshipMetadata("OneToMany", "fileattachmentid", "email", "descriptionblobid", "Referenced")]
    public IEnumerable<Email> FileAttachment_Email_DescriptionBlobId
    {
        get => GetRelatedEntities<Email>("FileAttachment_Email_DescriptionBlobId", null);
        set => SetRelatedEntities("FileAttachment_Email_DescriptionBlobId", null, value);
    }

    /// <summary>
    /// Gets the logical column name for a property on the FileAttachment entity, using the AttributeLogicalNameAttribute if present.
    /// </summary>
    /// <param name="column">Expression to pick the column</param>
    /// <returns>Name of column</returns>
    /// <exception cref="ArgumentNullException">If no expression is provided</exception>
    /// <exception cref="ArgumentException">If the expression is not x => x.column</exception>
    public static string GetColumnName(Expression<Func<FileAttachment, object?>> column)
    {
        return TableAttributeHelpers.GetColumnName(column);
    }

    /// <summary>
    /// Retrieves the FileAttachment with the specified columns.
    /// </summary>
    /// <param name="service">Organization service</param>
    /// <param name="id">Id of FileAttachment to retrieve</param>
    /// <param name="columns">Expressions that specify columns to retrieve</param>
    /// <returns>The retrieved FileAttachment</returns>
    public static FileAttachment Retrieve(IOrganizationService service, Guid id, params Expression<Func<FileAttachment, object>>[] columns)
    {
        return service.Retrieve(id, columns);
    }
}
