using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum contact_preferredappointmenttimecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Morning", 1033)]
    [OptionSetMetadata("Morgen", 1030)]
    Morning = 1,

    [EnumMember]
    [OptionSetMetadata("Afternoon", 1033)]
    [OptionSetMetadata("Eftermiddag", 1030)]
    Afternoon = 2,

    [EnumMember]
    [OptionSetMetadata("Evening", 1033)]
    [OptionSetMetadata("Aften", 1030)]
    Evening = 3,
}
