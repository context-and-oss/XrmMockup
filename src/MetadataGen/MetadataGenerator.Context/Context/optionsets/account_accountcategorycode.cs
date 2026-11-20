using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum account_accountcategorycode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Preferred Customer", 1033)]
    [OptionSetMetadata("Foretrukken kunde", 1030)]
    PreferredCustomer = 1,

    [EnumMember]
    [OptionSetMetadata("Standard", 1033)]
    [OptionSetMetadata("Standard", 1030)]
    Standard = 2,
}
