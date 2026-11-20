using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_accessmode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Read-Write", 1033)]
    [OptionSetMetadata("Læse-skrive", 1030)]
    ReadWrite = 0,

    [EnumMember]
    [OptionSetMetadata("Administrative", 1033)]
    [OptionSetMetadata("Administrativ", 1030)]
    Administrative = 1,

    [EnumMember]
    [OptionSetMetadata("Read", 1033)]
    [OptionSetMetadata("Læse", 1030)]
    Read = 2,

    [EnumMember]
    [OptionSetMetadata("Support User", 1033)]
    [OptionSetMetadata("Supportbruger", 1030)]
    SupportUser = 3,

    [EnumMember]
    [OptionSetMetadata("Non-interactive", 1033)]
    [OptionSetMetadata("Ikke-interaktiv", 1030)]
    Noninteractive = 4,

    [EnumMember]
    [OptionSetMetadata("Delegated Admin", 1033)]
    [OptionSetMetadata("Stedfortræderadministrator", 1030)]
    DelegatedAdmin = 5,
}
