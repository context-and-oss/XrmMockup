using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_incomingemaildeliverymethod
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("None", 1033)]
    [OptionSetMetadata("Ingen", 1030)]
    None = 0,

    [EnumMember]
    [OptionSetMetadata("Microsoft Dynamics 365 for Outlook", 1033)]
    [OptionSetMetadata("Microsoft Dynamics 365 til Outlook", 1030)]
    MicrosoftDynamics365forOutlook = 1,

    [EnumMember]
    [OptionSetMetadata("Server-Side Synchronization or Email Router", 1033)]
    [OptionSetMetadata("Synkronisering på serversiden eller E-mail Router", 1030)]
    ServerSideSynchronizationorEmailRouter = 2,

    [EnumMember]
    [OptionSetMetadata("Forward Mailbox", 1033)]
    [OptionSetMetadata("Postkasse til videresendelse", 1030)]
    ForwardMailbox = 3,
}
