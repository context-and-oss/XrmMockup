using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum solution_sourcecontrolsyncstatus
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Not started", 1033)]
    Notstarted = 0,

    [EnumMember]
    [OptionSetMetadata("Initial sync in progress", 1033)]
    Initialsyncinprogress = 1,

    [EnumMember]
    [OptionSetMetadata("Errors in initial sync", 1033)]
    Errorsininitialsync = 2,

    [EnumMember]
    [OptionSetMetadata("Pending changes to be committed", 1033)]
    Pendingchangestobecommitted = 3,

    [EnumMember]
    [OptionSetMetadata("Committed", 1033)]
    Committed = 4,
}
