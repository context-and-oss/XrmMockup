using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum activityparty_participationtypemask
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Sender", 1033)]
    [OptionSetMetadata("Afsender", 1030)]
    Sender = 1,

    [EnumMember]
    [OptionSetMetadata("To Recipient", 1033)]
    [OptionSetMetadata("Til-modtager", 1030)]
    ToRecipient = 2,

    [EnumMember]
    [OptionSetMetadata("CC Recipient", 1033)]
    [OptionSetMetadata("CC-modtager", 1030)]
    CCRecipient = 3,

    [EnumMember]
    [OptionSetMetadata("BCC Recipient", 1033)]
    [OptionSetMetadata("BCC-modtager", 1030)]
    BCCRecipient = 4,

    [EnumMember]
    [OptionSetMetadata("Required attendee", 1033)]
    [OptionSetMetadata("Nødvendig deltager", 1030)]
    Requiredattendee = 5,

    [EnumMember]
    [OptionSetMetadata("Optional attendee", 1033)]
    [OptionSetMetadata("Valgfri deltager", 1030)]
    Optionalattendee = 6,

    [EnumMember]
    [OptionSetMetadata("Organizer", 1033)]
    [OptionSetMetadata("Arrangør", 1030)]
    Organizer = 7,

    [EnumMember]
    [OptionSetMetadata("Regarding", 1033)]
    [OptionSetMetadata("Angående", 1030)]
    Regarding = 8,

    [EnumMember]
    [OptionSetMetadata("Owner", 1033)]
    [OptionSetMetadata("Ejer", 1030)]
    Owner = 9,

    [EnumMember]
    [OptionSetMetadata("Resource", 1033)]
    [OptionSetMetadata("Ressource", 1030)]
    Resource = 10,

    [EnumMember]
    [OptionSetMetadata("Customer", 1033)]
    [OptionSetMetadata("Kunde", 1030)]
    Customer = 11,

    [EnumMember]
    [OptionSetMetadata("Chat Participant", 1033)]
    [OptionSetMetadata("Chatdeltager", 1030)]
    ChatParticipant = 12,

    [EnumMember]
    [OptionSetMetadata("Related", 1033)]
    [OptionSetMetadata("Relaterede", 1030)]
    Related = 13,
}
