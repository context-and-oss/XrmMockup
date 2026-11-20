using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum team_membershiptype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Members and guests", 1033)]
    Membersandguests = 0,

    [EnumMember]
    [OptionSetMetadata("Members", 1033)]
    Members = 1,

    [EnumMember]
    [OptionSetMetadata("Owners", 1033)]
    Owners = 2,

    [EnumMember]
    [OptionSetMetadata("Guests", 1033)]
    Guests = 3,
}
