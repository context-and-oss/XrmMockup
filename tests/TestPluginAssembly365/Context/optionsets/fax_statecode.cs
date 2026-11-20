using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum fax_statecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Open", 1033)]
    [OptionSetMetadata("Åben", 1030)]
    Open = 0,

    [EnumMember]
    [OptionSetMetadata("Completed", 1033)]
    [OptionSetMetadata("Fuldført", 1030)]
    Completed = 1,

    [EnumMember]
    [OptionSetMetadata("Canceled", 1033)]
    [OptionSetMetadata("Annulleret", 1030)]
    Canceled = 2,
}
