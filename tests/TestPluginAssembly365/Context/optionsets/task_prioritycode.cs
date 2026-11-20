using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum task_prioritycode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Low", 1033)]
    [OptionSetMetadata("Lav", 1030)]
    Low = 0,

    [EnumMember]
    [OptionSetMetadata("Normal", 1033)]
    [OptionSetMetadata("Normal", 1030)]
    Normal = 1,

    [EnumMember]
    [OptionSetMetadata("High", 1033)]
    [OptionSetMetadata("Høj", 1030)]
    High = 2,
}
