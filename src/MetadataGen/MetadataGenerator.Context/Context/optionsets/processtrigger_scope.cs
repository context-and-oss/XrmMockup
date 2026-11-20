using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum processtrigger_scope
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Form", 1033)]
    [OptionSetMetadata("Formular", 1030)]
    Form = 1,

    [EnumMember]
    [OptionSetMetadata("Entity", 1033)]
    [OptionSetMetadata("Objekt", 1030)]
    Entity = 2,
}
