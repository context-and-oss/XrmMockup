using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum contact_paymenttermscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Net 30", 1033)]
    [OptionSetMetadata("Netto 30 dage", 1030)]
    Net30 = 1,

    [EnumMember]
    [OptionSetMetadata("2% 10, Net 30", 1033)]
    _210Net30 = 2,

    [EnumMember]
    [OptionSetMetadata("Net 45", 1033)]
    Net45 = 3,

    [EnumMember]
    [OptionSetMetadata("Net 60", 1033)]
    Net60 = 4,
}
