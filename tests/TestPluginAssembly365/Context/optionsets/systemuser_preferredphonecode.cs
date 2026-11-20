using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_preferredphonecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Main Phone", 1033)]
    [OptionSetMetadata("Primær telefon", 1030)]
    MainPhone = 1,

    [EnumMember]
    [OptionSetMetadata("Other Phone", 1033)]
    [OptionSetMetadata("Anden telefon", 1030)]
    OtherPhone = 2,

    [EnumMember]
    [OptionSetMetadata("Home Phone", 1033)]
    [OptionSetMetadata("Telefon (privat)", 1030)]
    HomePhone = 3,

    [EnumMember]
    [OptionSetMetadata("Mobile Phone", 1033)]
    [OptionSetMetadata("Mobiltelefon", 1030)]
    MobilePhone = 4,
}
