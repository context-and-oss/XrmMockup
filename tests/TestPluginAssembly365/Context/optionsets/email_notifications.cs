using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum email_notifications
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("None", 1033)]
    [OptionSetMetadata("Ingen", 1030)]
    None = 0,

    [EnumMember]
    [OptionSetMetadata("The message was saved as a Microsoft Dynamics 365 email record, but not all the attachments could be saved with it. An attachment cannot be saved if it is blocked or if its file type is invalid.", 1033)]
    [OptionSetMetadata("Meddelelsen blev gemt som en Microsoft Dynamics 365-mailpost, men det var ikke alle de vedhæftede filer, der blev gemt med den. En vedhæftet fil kan ikke gemmes, hvis den er blokeret, eller hvis filtypen er ugyldig.", 1030)]
    ThemessagewassavedasaMicrosoftDynamics365emailrecordbutnotalltheattachmentscouldbesavedwithitAnattachmentcannotbesavedifitisblockedorifitsfiletypeisinvalid = 1,

    [EnumMember]
    [OptionSetMetadata("Truncated body.", 1033)]
    [OptionSetMetadata("Afkortet brødtekst.", 1030)]
    Truncatedbody = 2,
}
