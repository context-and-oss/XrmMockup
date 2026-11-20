using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_stage
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Pre-operation", 1033)]
    [OptionSetMetadata("Starthandling", 1030)]
    Preoperation = 20,

    [EnumMember]
    [OptionSetMetadata("Post-operation", 1033)]
    [OptionSetMetadata("Efterfølgende handling", 1030)]
    Postoperation = 40,
}
