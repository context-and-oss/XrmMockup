using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum account_ownershipcode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Public", 1033)]
    [OptionSetMetadata("Offentligt", 1030)]
    @Public = 1,

    [EnumMember]
    [OptionSetMetadata("Private", 1033)]
    [OptionSetMetadata("Privat", 1030)]
    @Private = 2,

    [EnumMember]
    [OptionSetMetadata("Subsidiary", 1033)]
    [OptionSetMetadata("Datterselskab", 1030)]
    Subsidiary = 3,

    [EnumMember]
    [OptionSetMetadata("Other", 1033)]
    [OptionSetMetadata("Andet", 1030)]
    Other = 4,
}
