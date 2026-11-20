using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum task_statuscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Not Started", 1033)]
    [OptionSetMetadata("Ikke startet", 1030)]
    NotStarted = 2,

    [EnumMember]
    [OptionSetMetadata("In Progress", 1033)]
    [OptionSetMetadata("I gang", 1030)]
    InProgress = 3,

    [EnumMember]
    [OptionSetMetadata("Waiting on someone else", 1033)]
    [OptionSetMetadata("Venter på en anden", 1030)]
    Waitingonsomeoneelse = 4,

    [EnumMember]
    [OptionSetMetadata("Completed", 1033)]
    [OptionSetMetadata("Fuldført", 1030)]
    Completed = 5,

    [EnumMember]
    [OptionSetMetadata("Canceled", 1033)]
    [OptionSetMetadata("Annulleret", 1030)]
    Canceled = 6,

    [EnumMember]
    [OptionSetMetadata("Deferred", 1033)]
    [OptionSetMetadata("Udskudt", 1030)]
    Deferred = 7,
}
