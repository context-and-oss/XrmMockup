using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum account_address1_shippingmethodcode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Airborne", 1033)]
    [OptionSetMetadata("Luftfragt", 1030)]
    Airborne = 1,

    [EnumMember]
    [OptionSetMetadata("DHL", 1033)]
    [OptionSetMetadata("DHL", 1030)]
    DHL = 2,

    [EnumMember]
    [OptionSetMetadata("FedEx", 1033)]
    [OptionSetMetadata("FedEx", 1030)]
    FedEx = 3,

    [EnumMember]
    [OptionSetMetadata("UPS", 1033)]
    [OptionSetMetadata("UPS", 1030)]
    UPS = 4,

    [EnumMember]
    [OptionSetMetadata("Postal Mail", 1033)]
    [OptionSetMetadata("Alm. post", 1030)]
    PostalMail = 5,

    [EnumMember]
    [OptionSetMetadata("Full Load", 1033)]
    FullLoad = 6,

    [EnumMember]
    [OptionSetMetadata("Will Call", 1033)]
    WillCall = 7,
}
