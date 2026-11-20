namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class RelationshipMetadataAttribute : Attribute
{
    public string RelationshipType { get; }
    public string? ThisEntityAttribute { get; }
    public string? RelatedEntity { get; }
    public string? RelatedEntityAttribute { get; }
    public string? ThisEntityRole { get; }

    public RelationshipMetadataAttribute(
        string relationshipType,
        string? thisEntityAttribute = null,
        string? relatedEntity = null,
        string? relatedEntityAttribute = null,
        string? thisEntityRole = null)
    {
        RelationshipType = relationshipType;
        ThisEntityAttribute = thisEntityAttribute;
        RelatedEntity = relatedEntity;
        RelatedEntityAttribute = relatedEntityAttribute;
        ThisEntityRole = thisEntityRole;
    }
}
