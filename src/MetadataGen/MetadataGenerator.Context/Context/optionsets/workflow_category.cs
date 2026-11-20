using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_category
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Workflow", 1033)]
    [OptionSetMetadata("Arbejdsproces", 1030)]
    Workflow = 0,

    [EnumMember]
    [OptionSetMetadata("Dialog", 1033)]
    [OptionSetMetadata("Dialogboks", 1030)]
    Dialog = 1,

    [EnumMember]
    [OptionSetMetadata("Business Rule", 1033)]
    [OptionSetMetadata("Forretningsregel", 1030)]
    BusinessRule = 2,

    [EnumMember]
    [OptionSetMetadata("Action", 1033)]
    [OptionSetMetadata("Handling", 1030)]
    Action = 3,

    [EnumMember]
    [OptionSetMetadata("Business Process Flow", 1033)]
    [OptionSetMetadata("Forretningsprocesforløb", 1030)]
    BusinessProcessFlow = 4,

    [EnumMember]
    [OptionSetMetadata("Modern Flow", 1033)]
    [OptionSetMetadata("Moderne forløb", 1030)]
    ModernFlow = 5,

    [EnumMember]
    [OptionSetMetadata("Desktop Flow", 1033)]
    [OptionSetMetadata("Skrivebordsflow", 1030)]
    DesktopFlow = 6,

    [EnumMember]
    [OptionSetMetadata("AI Flow", 1033)]
    [OptionSetMetadata("AI-flow", 1030)]
    AIFlow = 7,
}
