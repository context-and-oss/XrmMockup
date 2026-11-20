using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_scope
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("User", 1033)]
    [OptionSetMetadata("Bruger", 1030)]
    User = 1,

    [EnumMember]
    [OptionSetMetadata("Business Unit", 1033)]
    [OptionSetMetadata("Afdeling", 1030)]
    BusinessUnit = 2,

    [EnumMember]
    [OptionSetMetadata("Parent: Child Business Units", 1033)]
    [OptionSetMetadata("Overordnet: underordnede afdelinger", 1030)]
    ParentChildBusinessUnits = 3,

    [EnumMember]
    [OptionSetMetadata("Organization", 1033)]
    [OptionSetMetadata("Organisation", 1030)]
    Organization = 4,
}
