using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum account_preferredcontactmethodcode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Any", 1033)]
    [OptionSetMetadata("Ethvert", 1030)]
    Any = 1,

    [EnumMember]
    [OptionSetMetadata("Email", 1033)]
    [OptionSetMetadata("Mail", 1030)]
    Email = 2,

    [EnumMember]
    [OptionSetMetadata("Phone", 1033)]
    [OptionSetMetadata("Telefon", 1030)]
    Phone = 3,

    [EnumMember]
    [OptionSetMetadata("Fax", 1033)]
    [OptionSetMetadata("Fax", 1030)]
    Fax = 4,

    [EnumMember]
    [OptionSetMetadata("Mail", 1033)]
    [OptionSetMetadata("Post", 1030)]
    Mail = 5,
}
