using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.25")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_caltype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Professional", 1033)]
    [OptionSetMetadata("Professional", 1030)]
    Professional = 0,

    [EnumMember]
    [OptionSetMetadata("Administrative", 1033)]
    [OptionSetMetadata("Administrativ", 1030)]
    Administrative = 1,

    [EnumMember]
    [OptionSetMetadata("Basic", 1033)]
    [OptionSetMetadata("Grundlæggende", 1030)]
    Basic = 2,

    [EnumMember]
    [OptionSetMetadata("Device Professional", 1033)]
    [OptionSetMetadata("Enhed Professional", 1030)]
    DeviceProfessional = 3,

    [EnumMember]
    [OptionSetMetadata("Device Basic", 1033)]
    [OptionSetMetadata("Device Basic", 1030)]
    DeviceBasic = 4,

    [EnumMember]
    [OptionSetMetadata("Essential", 1033)]
    [OptionSetMetadata("Essential", 1030)]
    Essential = 5,

    [EnumMember]
    [OptionSetMetadata("Device Essential", 1033)]
    [OptionSetMetadata("Device Essential", 1030)]
    DeviceEssential = 6,

    [EnumMember]
    [OptionSetMetadata("Enterprise", 1033)]
    [OptionSetMetadata("Enterprise", 1030)]
    Enterprise = 7,

    [EnumMember]
    [OptionSetMetadata("Device Enterprise", 1033)]
    [OptionSetMetadata("Enheden Enterprise", 1030)]
    DeviceEnterprise = 8,

    [EnumMember]
    [OptionSetMetadata("Sales", 1033)]
    [OptionSetMetadata("Salg", 1030)]
    Sales = 9,

    [EnumMember]
    [OptionSetMetadata("Service", 1033)]
    [OptionSetMetadata("Service", 1030)]
    Service = 10,

    [EnumMember]
    [OptionSetMetadata("Field Service", 1033)]
    [OptionSetMetadata("Field Service", 1030)]
    FieldService = 11,

    [EnumMember]
    [OptionSetMetadata("Project Service", 1033)]
    [OptionSetMetadata("Project Service", 1030)]
    ProjectService = 12,
}
