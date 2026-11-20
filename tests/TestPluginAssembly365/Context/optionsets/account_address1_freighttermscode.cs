using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum account_address1_freighttermscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("FOB", 1033)]
    [OptionSetMetadata("FOB", 1030)]
    FOB = 1,

    [EnumMember]
    [OptionSetMetadata("No Charge", 1033)]
    [OptionSetMetadata("Uden beregning", 1030)]
    NoCharge = 2,
}
