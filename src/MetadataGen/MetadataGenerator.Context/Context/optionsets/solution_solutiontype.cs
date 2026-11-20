using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum solution_solutiontype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("None", 1033)]
    [OptionSetMetadata("Ingen", 1030)]
    None = 0,

    [EnumMember]
    [OptionSetMetadata("Snapshot", 1033)]
    [OptionSetMetadata("Øjebliksbillede", 1030)]
    Snapshot = 1,

    [EnumMember]
    [OptionSetMetadata("Internal", 1033)]
    [OptionSetMetadata("Intern", 1030)]
    @Internal = 2,
}
