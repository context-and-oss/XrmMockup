using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_type
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Definition", 1033)]
    [OptionSetMetadata("Definition", 1030)]
    Definition = 1,

    [EnumMember]
    [OptionSetMetadata("Activation", 1033)]
    [OptionSetMetadata("Aktivering", 1030)]
    Activation = 2,

    [EnumMember]
    [OptionSetMetadata("Template", 1033)]
    [OptionSetMetadata("Skabelon", 1030)]
    Template = 3,
}
