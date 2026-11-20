using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum contact_statecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Active", 1033)]
    [OptionSetMetadata("Aktiv", 1030)]
    Active = 0,

    [EnumMember]
    [OptionSetMetadata("Inactive", 1033)]
    [OptionSetMetadata("Inaktiv", 1030)]
    Inactive = 1,
}
