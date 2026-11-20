using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum componentstate
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Published", 1033)]
    [OptionSetMetadata("Udgivet", 1030)]
    Published = 0,

    [EnumMember]
    [OptionSetMetadata("Unpublished", 1033)]
    [OptionSetMetadata("Ikke-udgivet", 1030)]
    Unpublished = 1,

    [EnumMember]
    [OptionSetMetadata("Deleted", 1033)]
    [OptionSetMetadata("Slettet", 1030)]
    Deleted = 2,

    [EnumMember]
    [OptionSetMetadata("Deleted Unpublished", 1033)]
    [OptionSetMetadata("Ikke-publicerede blev slettet", 1030)]
    DeletedUnpublished = 3,
}
