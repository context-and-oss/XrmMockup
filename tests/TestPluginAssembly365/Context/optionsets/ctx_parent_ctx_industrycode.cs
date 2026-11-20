using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum ctx_parent_ctx_industrycode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Accounting", 1033)]
    Accounting = 1,

    [EnumMember]
    [OptionSetMetadata("Agriculture", 1033)]
    Agriculture = 2,

    [EnumMember]
    [OptionSetMetadata("Consulting", 1033)]
    Consulting = 3,
}
