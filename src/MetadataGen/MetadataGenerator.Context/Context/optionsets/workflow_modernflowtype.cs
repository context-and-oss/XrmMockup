using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_modernflowtype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("PowerAutomateFlow", 1033)]
    [OptionSetMetadata("PowerAutomateFlow", 1030)]
    PowerAutomateFlow = 0,

    [EnumMember]
    [OptionSetMetadata("CopilotStudioFlow", 1033)]
    [OptionSetMetadata("CopilotStudioFlow", 1030)]
    CopilotStudioFlow = 1,

    [EnumMember]
    [OptionSetMetadata("M365CopilotAgentFlow", 1033)]
    [OptionSetMetadata("M365CopilotAgentFlow", 1030)]
    M365CopilotAgentFlow = 2,
}
