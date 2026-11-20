using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum email_correlationmethod
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("None", 1033)]
    [OptionSetMetadata("Ingen", 1030)]
    None = 0,

    [EnumMember]
    [OptionSetMetadata("Skipped", 1033)]
    [OptionSetMetadata("Sprunget over", 1030)]
    Skipped = 1,

    [EnumMember]
    [OptionSetMetadata("XHeader", 1033)]
    [OptionSetMetadata("XHeader", 1030)]
    XHeader = 2,

    [EnumMember]
    [OptionSetMetadata("InReplyTo", 1033)]
    [OptionSetMetadata("InReplyTo", 1030)]
    InReplyTo = 3,

    [EnumMember]
    [OptionSetMetadata("TrackingToken", 1033)]
    [OptionSetMetadata("TrackingToken", 1030)]
    TrackingToken = 4,

    [EnumMember]
    [OptionSetMetadata("ConversationIndex", 1033)]
    [OptionSetMetadata("ConversationIndex", 1030)]
    ConversationIndex = 5,

    [EnumMember]
    [OptionSetMetadata("SmartMatching", 1033)]
    [OptionSetMetadata("SmartMatching", 1030)]
    SmartMatching = 6,

    [EnumMember]
    [OptionSetMetadata("CustomCorrelation", 1033)]
    [OptionSetMetadata("CustomCorrelation", 1030)]
    CustomCorrelation = 7,
}
