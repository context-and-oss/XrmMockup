using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum account_customertypecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Competitor", 1033)]
    [OptionSetMetadata("Konkurrent", 1030)]
    Competitor = 1,

    [EnumMember]
    [OptionSetMetadata("Consultant", 1033)]
    [OptionSetMetadata("Konsulent", 1030)]
    Consultant = 2,

    [EnumMember]
    [OptionSetMetadata("Customer", 1033)]
    [OptionSetMetadata("Kunde", 1030)]
    Customer = 3,

    [EnumMember]
    [OptionSetMetadata("Investor", 1033)]
    [OptionSetMetadata("Investor", 1030)]
    Investor = 4,

    [EnumMember]
    [OptionSetMetadata("Partner", 1033)]
    [OptionSetMetadata("Partner", 1030)]
    Partner = 5,

    [EnumMember]
    [OptionSetMetadata("Influencer", 1033)]
    [OptionSetMetadata("Person, der øver indflydelse", 1030)]
    Influencer = 6,

    [EnumMember]
    [OptionSetMetadata("Press", 1033)]
    [OptionSetMetadata("Tryk på", 1030)]
    Press = 7,

    [EnumMember]
    [OptionSetMetadata("Prospect", 1033)]
    [OptionSetMetadata("Kundeemne", 1030)]
    Prospect = 8,

    [EnumMember]
    [OptionSetMetadata("Reseller", 1033)]
    [OptionSetMetadata("Forhandler", 1030)]
    Reseller = 9,

    [EnumMember]
    [OptionSetMetadata("Supplier", 1033)]
    [OptionSetMetadata("Kreditor", 1030)]
    Supplier = 10,

    [EnumMember]
    [OptionSetMetadata("Vendor", 1033)]
    [OptionSetMetadata("Leverandør", 1030)]
    Vendor = 11,

    [EnumMember]
    [OptionSetMetadata("Other", 1033)]
    [OptionSetMetadata("Andet", 1030)]
    Other = 12,
}
