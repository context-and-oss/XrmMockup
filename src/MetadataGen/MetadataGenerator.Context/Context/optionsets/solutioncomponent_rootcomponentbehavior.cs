using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum solutioncomponent_rootcomponentbehavior
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Include Subcomponents", 1033)]
    [OptionSetMetadata("Inkluder underkomponenter", 1030)]
    IncludeSubcomponents = 0,

    [EnumMember]
    [OptionSetMetadata("Do not include subcomponents", 1033)]
    [OptionSetMetadata("Inkluder ikke underkomponenter", 1030)]
    Donotincludesubcomponents = 1,

    [EnumMember]
    [OptionSetMetadata("Include As Shell Only", 1033)]
    [OptionSetMetadata("Inkluder kun som skal", 1030)]
    IncludeAsShellOnly = 2,
}
