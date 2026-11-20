using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum email_remindertype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("If I do not receive a reply by", 1033)]
    [OptionSetMetadata("Hvis jeg ikke modtager et svar inden", 1030)]
    IfIdonotreceiveareplyby = 0,

    [EnumMember]
    [OptionSetMetadata("If the email is not opened by", 1033)]
    [OptionSetMetadata("Hvis mailen ikke åbnes af", 1030)]
    Iftheemailisnotopenedby = 1,

    [EnumMember]
    [OptionSetMetadata("Remind me anyways at", 1033)]
    [OptionSetMetadata("Påmind mig uanset tidspunkt", 1030)]
    Remindmeanywaysat = 2,
}
