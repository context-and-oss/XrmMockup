using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum socialprofile_community
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Other", 1033)]
    [OptionSetMetadata("Andet", 1030)]
    Other = 0,

    [EnumMember]
    [OptionSetMetadata("Facebook", 1033)]
    [OptionSetMetadata("Facebook", 1030)]
    Facebook = 1,

    [EnumMember]
    [OptionSetMetadata("Twitter", 1033)]
    [OptionSetMetadata("Twitter", 1030)]
    Twitter = 2,
}
