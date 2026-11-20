using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum contact_gendercode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Male", 1033)]
    [OptionSetMetadata("Mand", 1030)]
    Male = 1,

    [EnumMember]
    [OptionSetMetadata("Female", 1033)]
    [OptionSetMetadata("Kvinde", 1030)]
    Female = 2,
}
