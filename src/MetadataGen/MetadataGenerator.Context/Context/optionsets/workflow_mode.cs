using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_mode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Background", 1033)]
    [OptionSetMetadata("Baggrund", 1030)]
    Background = 0,

    [EnumMember]
    [OptionSetMetadata("Real-time", 1033)]
    [OptionSetMetadata("Realtid", 1030)]
    Realtime = 1,
}
