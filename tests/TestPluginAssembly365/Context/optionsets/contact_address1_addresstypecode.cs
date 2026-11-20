using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum contact_address1_addresstypecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Bill To", 1033)]
    [OptionSetMetadata("Faktura til", 1030)]
    BillTo = 1,

    [EnumMember]
    [OptionSetMetadata("Ship To", 1033)]
    [OptionSetMetadata("Lever til", 1030)]
    ShipTo = 2,

    [EnumMember]
    [OptionSetMetadata("Primary", 1033)]
    [OptionSetMetadata("Primær", 1030)]
    Primary = 3,

    [EnumMember]
    [OptionSetMetadata("Other", 1033)]
    [OptionSetMetadata("Andet", 1030)]
    Other = 4,
}
