using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_preferredaddresscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Mailing Address", 1033)]
    [OptionSetMetadata("Postadresse", 1030)]
    MailingAddress = 1,

    [EnumMember]
    [OptionSetMetadata("Other Address", 1033)]
    [OptionSetMetadata("Anden adresse", 1030)]
    OtherAddress = 2,
}
