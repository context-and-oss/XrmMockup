using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_throttlingbehaviortype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("None", 1033)]
    [OptionSetMetadata("Ingen", 1030)]
    None = 0,

    [EnumMember]
    [OptionSetMetadata("TenantPool", 1033)]
    [OptionSetMetadata("LejerPulje", 1030)]
    TenantPool = 1,

    [EnumMember]
    [OptionSetMetadata("CopilotStudio", 1033)]
    [OptionSetMetadata("CopilotStudio", 1030)]
    CopilotStudio = 2,
}
