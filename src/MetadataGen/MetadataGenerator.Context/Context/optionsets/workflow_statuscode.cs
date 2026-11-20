using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_statuscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Draft", 1033)]
    [OptionSetMetadata("Kladde", 1030)]
    Draft = 1,

    [EnumMember]
    [OptionSetMetadata("Activated", 1033)]
    [OptionSetMetadata("Aktiveret", 1030)]
    Activated = 2,

    [EnumMember]
    [OptionSetMetadata("CompanyDLPViolation", 1033)]
    [OptionSetMetadata("CompanyDLPViolation", 1030)]
    CompanyDLPViolation = 3,
}
