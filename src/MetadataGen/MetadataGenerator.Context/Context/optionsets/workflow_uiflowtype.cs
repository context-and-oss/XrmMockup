using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_uiflowtype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Windows recorder (V1)", 1033)]
    [OptionSetMetadata("Windows-optager (V1)", 1030)]
    WindowsrecorderV1 = 0,

    [EnumMember]
    [OptionSetMetadata("Selenium IDE", 1033)]
    [OptionSetMetadata("Selenium IDE", 1030)]
    SeleniumIDE = 1,

    [EnumMember]
    [OptionSetMetadata("Power Automate Desktop", 1033)]
    [OptionSetMetadata("Power Automate Desktop", 1030)]
    PowerAutomateDesktop = 2,

    [EnumMember]
    [OptionSetMetadata("Test", 1033)]
    [OptionSetMetadata("Test", 1030)]
    Test = 3,

    [EnumMember]
    [OptionSetMetadata("Recording", 1033)]
    [OptionSetMetadata("Optagelse", 1030)]
    Recording = 101,
}
