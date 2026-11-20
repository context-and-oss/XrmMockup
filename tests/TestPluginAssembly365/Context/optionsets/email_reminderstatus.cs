using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum email_reminderstatus
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("NotSet", 1033)]
    [OptionSetMetadata("NotSet", 1030)]
    NotSet = 0,

    [EnumMember]
    [OptionSetMetadata("ReminderSet", 1033)]
    [OptionSetMetadata("ReminderSet", 1030)]
    ReminderSet = 1,

    [EnumMember]
    [OptionSetMetadata("ReminderExpired", 1033)]
    [OptionSetMetadata("ReminderExpired", 1030)]
    ReminderExpired = 2,

    [EnumMember]
    [OptionSetMetadata("ReminderInvalid", 1033)]
    [OptionSetMetadata("ReminderInvalid", 1030)]
    ReminderInvalid = 3,
}
