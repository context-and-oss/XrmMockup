// Private wire contracts retain the metadata generator's DataContract names without exporting
// duplicate DG.Tools.XrmMockup types into SDK consumers that also reference Core.
#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
namespace XrmMockup.PermissionAnalyzer.Workspace.Serialization;

[DataContract(Name = "MetadataSkeleton", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class MetadataSkeleton
{
    [DataMember] public SortedDictionary<string, EntityMetadata> EntityMetadata;
    [DataMember] public List<Entity> Currencies;
    [DataMember] public Entity BaseOrganization;
    [DataMember] public Entity RootBusinessUnit;
    [DataMember] public List<MetaPlugin> Plugins;
    [DataMember] public OptionSetMetadataBase[] OptionSets;
    [DataMember] public Dictionary<string, Dictionary<int,int>> DefaultStateStatus;
}

[DataContract(Name = "MetaPlugin", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class MetaPlugin
{
    [DataMember] public string FilteredAttributes;
    [DataMember] public int Mode;
    [DataMember] public string Name;
    [DataMember] public int Rank;
    [DataMember] public int Stage;
    [DataMember] public string MessageName;
    [DataMember] public string AssemblyName;
    [DataMember] public string PluginTypeAssemblyName;
    [DataMember] public string PrimaryEntity;
    [DataMember] public bool AsyncAutoDelete;
    [DataMember] public List<MetaImage> Images;
    [DataMember] public Guid? ImpersonatingUserId;
}

[DataContract(Name = "MetaImage", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class MetaImage
{
    [DataMember] public string Attributes;
    [DataMember] public string EntityAlias;
    [DataMember] public string Name;
    [DataMember] public int ImageType;
}

[DataContract(Name = "RolePrivilege", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class RolePrivilege
{
    [DataMember] public bool CanBeGlobal;
    [DataMember] public bool CanBeDeep;
    [DataMember] public bool CanBeLocal;
    [DataMember] public bool CanBeBasic;
    [DataMember] public AccessRights AccessRight;
    [DataMember] public PrivilegeDepth PrivilegeDepth;
}

[DataContract(Name = "SecurityRole", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class SecurityRole
{
    [DataMember] public Dictionary<string, Dictionary<AccessRights, RolePrivilege>> Privileges;
    [DataMember] public Dictionary<string, RolePrivilege> GlobalPrivileges;
    [DataMember] public string Name;
    [DataMember] public EntityReference BusinessUnitId;
    [DataMember] public Guid RoleId;
    [DataMember] public Guid RoleTemplateId;
}

[DataContract(Name = "PrivilegeCatalog", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class PrivilegeCatalog
{
    [DataMember] public List<MetaPrivilege> Privileges;
    [DataMember] public List<MetaCustomApi> CustomApis;
}

[DataContract(Name = "MetaPrivilege", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class MetaPrivilege
{
    [DataMember] public string Name;
    [DataMember] public Guid PrivilegeId;
    [DataMember] public AccessRights AccessRight;
    [DataMember] public List<string> Tables;
}

[DataContract(Name = "MetaCustomApi", Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup")]
internal sealed class MetaCustomApi
{
    [DataMember] public string UniqueName;
    [DataMember] public string ExecutePrivilegeName;
}
