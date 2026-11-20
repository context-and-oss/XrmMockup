using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum team_type
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Owner", 1033)]
    [OptionSetMetadata("Ejer", 1030)]
    Owner = 0,

    [EnumMember]
    [OptionSetMetadata("Access", 1033)]
    [OptionSetMetadata("Adgang", 1030)]
    Access = 1,

    [EnumMember]
    [OptionSetMetadata("Security Group", 1033)]
    [OptionSetMetadata("AAD-sikkerhedsgruppe", 1030)]
    SecurityGroup = 2,

    [EnumMember]
    [OptionSetMetadata("Office Group", 1033)]
    [OptionSetMetadata("AAD Office-gruppe", 1030)]
    OfficeGroup = 3,
}
