using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_systemmanagedusertype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Entra User", 1033)]
    EntraUser = 0,

    [EnumMember]
    [OptionSetMetadata("C2 User", 1033)]
    C2User = 1,

    [EnumMember]
    [OptionSetMetadata("Impersonable Stub User", 1033)]
    ImpersonableStubUser = 2,

    [EnumMember]
    [OptionSetMetadata("Agentic User", 1033)]
    AgenticUser = 3,
}
