using System.Runtime.Serialization;

namespace DG.XrmFramework.BusinessDomain.ServiceContext;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum activitypointer_instancetypecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Not Recurring", 1033)]
    [OptionSetMetadata("Skal ikke gentages", 1030)]
    NotRecurring = 0,

    [EnumMember]
    [OptionSetMetadata("Recurring Master", 1033)]
    [OptionSetMetadata("Gentaget master", 1030)]
    RecurringMaster = 1,

    [EnumMember]
    [OptionSetMetadata("Recurring Instance", 1033)]
    [OptionSetMetadata("Gentaget forekomst", 1030)]
    RecurringInstance = 2,

    [EnumMember]
    [OptionSetMetadata("Recurring Exception", 1033)]
    [OptionSetMetadata("Gentaget undtagelse", 1030)]
    RecurringException = 3,

    [EnumMember]
    [OptionSetMetadata("Recurring Future Exception", 1033)]
    [OptionSetMetadata("Gentaget fremtidig undtagelse", 1030)]
    RecurringFutureException = 4,
}
