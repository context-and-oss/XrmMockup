using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum account_preferredappointmentdaycode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Sunday", 1033)]
    [OptionSetMetadata("Søndag", 1030)]
    Sunday = 0,

    [EnumMember]
    [OptionSetMetadata("Monday", 1033)]
    [OptionSetMetadata("Mandag", 1030)]
    Monday = 1,

    [EnumMember]
    [OptionSetMetadata("Tuesday", 1033)]
    [OptionSetMetadata("Tirsdag", 1030)]
    Tuesday = 2,

    [EnumMember]
    [OptionSetMetadata("Wednesday", 1033)]
    [OptionSetMetadata("Onsdag", 1030)]
    Wednesday = 3,

    [EnumMember]
    [OptionSetMetadata("Thursday", 1033)]
    [OptionSetMetadata("Torsdag", 1030)]
    Thursday = 4,

    [EnumMember]
    [OptionSetMetadata("Friday", 1033)]
    [OptionSetMetadata("Fredag", 1030)]
    Friday = 5,

    [EnumMember]
    [OptionSetMetadata("Saturday", 1033)]
    [OptionSetMetadata("Lørdag", 1030)]
    Saturday = 6,
}
