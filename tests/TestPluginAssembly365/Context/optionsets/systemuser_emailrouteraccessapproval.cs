using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_emailrouteraccessapproval
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Empty", 1033)]
    [OptionSetMetadata("Tom", 1030)]
    Empty = 0,

    [EnumMember]
    [OptionSetMetadata("Approved", 1033)]
    [OptionSetMetadata("Godkendt", 1030)]
    Approved = 1,

    [EnumMember]
    [OptionSetMetadata("Pending Approval", 1033)]
    [OptionSetMetadata("Afventer godkendelse", 1030)]
    PendingApproval = 2,

    [EnumMember]
    [OptionSetMetadata("Rejected", 1033)]
    [OptionSetMetadata("Afvist", 1030)]
    Rejected = 3,
}
