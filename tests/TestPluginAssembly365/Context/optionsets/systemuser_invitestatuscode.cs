using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_invitestatuscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Invitation Not Sent", 1033)]
    [OptionSetMetadata("Invitation er ikke sendt", 1030)]
    InvitationNotSent = 0,

    [EnumMember]
    [OptionSetMetadata("Invited", 1033)]
    [OptionSetMetadata("Inviteret", 1030)]
    Invited = 1,

    [EnumMember]
    [OptionSetMetadata("Invitation Near Expired", 1033)]
    [OptionSetMetadata("Invitationen er næsten udløbet", 1030)]
    InvitationNearExpired = 2,

    [EnumMember]
    [OptionSetMetadata("Invitation Expired", 1033)]
    [OptionSetMetadata("Invitationen er udløbet", 1030)]
    InvitationExpired = 3,

    [EnumMember]
    [OptionSetMetadata("Invitation Accepted", 1033)]
    [OptionSetMetadata("Invitationen er accepteret", 1030)]
    InvitationAccepted = 4,

    [EnumMember]
    [OptionSetMetadata("Invitation Rejected", 1033)]
    [OptionSetMetadata("Invitationen er afvist", 1030)]
    InvitationRejected = 5,

    [EnumMember]
    [OptionSetMetadata("Invitation Revoked", 1033)]
    [OptionSetMetadata("Invitationen er tilbagekaldt", 1030)]
    InvitationRevoked = 6,
}
