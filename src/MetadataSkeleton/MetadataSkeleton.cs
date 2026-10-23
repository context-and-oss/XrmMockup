using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup {

    public class MetadataSkeleton {
        public Dictionary<string, EntityMetadata> EntityMetadata;
        public List<Entity> Currencies;
        public Entity BaseOrganization;
        public Entity RootBusinessUnit;
        public List<MetaPlugin> Plugins;
        public OptionSetMetadataBase[] OptionSets;
        public Dictionary<string, Dictionary<int,int>> DefaultStateStatus;

        public void Merge(MetadataSkeleton metadata)
        {
            foreach (var kvp in metadata.EntityMetadata)
            {
                if (this.EntityMetadata.ContainsKey(kvp.Key)) continue;
                
                this.EntityMetadata.Add(kvp.Key, kvp.Value);

                //also need to merge the default state and status for this entity
                if (metadata.DefaultStateStatus.ContainsKey(kvp.Key)) { 
                    var defaultSS = metadata.DefaultStateStatus[kvp.Key];
                    var dict = new Dictionary<int, int>();
                    foreach (var kvp2 in defaultSS)
                    {
                        dict.Add(kvp2.Key, kvp2.Value);
                    }

                    this.DefaultStateStatus.Add(kvp.Key, dict);
                }

                if (metadata.DefaultStateStatus.ContainsKey(kvp.Key))
                {
                    if (!this.DefaultStateStatus.ContainsKey(kvp.Key))
                    {
                        var defaultSS = metadata.DefaultStateStatus[kvp.Key];
                        var dict = new Dictionary<int, int>();
                        foreach (var kvp2 in defaultSS)
                        {
                            dict.Add(kvp2.Key, kvp2.Value);
                        }

                        this.DefaultStateStatus.Add(kvp.Key, dict);
                    }
                }
            }
        }
    }

    public class MetaPlugin
    {
        public string FilteredAttributes;
        public int Mode;
        public string Name;
        public int Rank;
        public int Stage;
        public string MessageName;
        public string AssemblyName;
        public string PluginTypeAssemblyName;
        public string PrimaryEntity;
        public bool AsyncAutoDelete;
        public List<MetaImage> Images;
        public Guid? ImpersonatingUserId;
    }

    public class MetaImage
    {
        public string Attributes;
        public string EntityAlias;
        public string Name;
        public int ImageType;
    }


    public class RolePrivilege {
        public bool CanBeGlobal;
        public bool CanBeDeep;
        public bool CanBeLocal;
        public bool CanBeBasic;
        public AccessRights AccessRight;
        public PrivilegeDepth PrivilegeDepth;

        public RolePrivilege Clone()
        {
            var clone = new RolePrivilege();
            clone.CanBeGlobal = this.CanBeGlobal;
            clone.CanBeDeep = this.CanBeDeep;
            clone.CanBeLocal = this.CanBeLocal;
            clone.CanBeBasic = this.CanBeBasic;
            clone.AccessRight = this.AccessRight;
            clone.PrivilegeDepth = this.PrivilegeDepth;

            return clone;
        }
    }

    public class SecurityRole {
        public Dictionary<string, Dictionary<AccessRights, RolePrivilege>> Privileges;

        /// <summary>
        /// Privileges that aren't tied to a table, such as prvActOnBehalfOfAnotherUser, by privilege name.
        /// Null in metadata generated before they were downloaded.
        /// </summary>
        public Dictionary<string, RolePrivilege> GlobalPrivileges;
        public string Name;
        public EntityReference BusinessUnitId;
        public Guid RoleId;
        public Guid RoleTemplateId;

        public SecurityRole Clone()
        {
            var clone = new SecurityRole();
            clone.Privileges = new Dictionary<string, Dictionary<AccessRights, RolePrivilege>>();
            foreach (var priv in this.Privileges)
            {
                var newV = new Dictionary<AccessRights, RolePrivilege>();
                foreach (var v in priv.Value)
                {
                    newV.Add(v.Key, new RolePrivilege() { AccessRight = v.Value.AccessRight, CanBeBasic = v.Value.CanBeBasic, CanBeDeep = v.Value.CanBeDeep, CanBeGlobal = v.Value.CanBeGlobal, CanBeLocal = v.Value.CanBeLocal, PrivilegeDepth = v.Value.PrivilegeDepth });
                }

                var p = new KeyValuePair<string, Dictionary<AccessRights, RolePrivilege>>(priv.Key, newV);
                clone.Privileges.Add(p.Key, p.Value);
            }

            if (this.GlobalPrivileges != null)
            {
                clone.GlobalPrivileges = new Dictionary<string, RolePrivilege>();
                foreach (var priv in this.GlobalPrivileges)
                {
                    clone.GlobalPrivileges.Add(priv.Key, priv.Value.Clone());
                }
            }

            clone.Name = "Clone of " + this.Name;
            clone.BusinessUnitId = this.BusinessUnitId;
            clone.RoleId = Guid.NewGuid();
            clone.RoleTemplateId = this.RoleTemplateId;

            return clone;
        }
    }

    /// <summary>
    /// Every privilege in the environment and every custom API with its execute privilege, so a privilege name can be
    /// looked up as a table and access right. Written to PrivilegeCatalog.xml next to Metadata.xml.
    /// </summary>
    public class PrivilegeCatalog {
        public List<MetaPrivilege> Privileges;
        public List<MetaCustomApi> CustomApis;
    }

    public class MetaPrivilege {
        public string Name;
        public Guid PrivilegeId;
        public AccessRights AccessRight;

        /// <summary>
        /// Logical names of the tables the privilege applies to, sorted. Empty for a privilege that isn't tied to a
        /// table.
        /// </summary>
        public List<string> Tables;
    }

    /// <summary>
    /// A custom API and the privilege it needs to be called.
    /// </summary>
    public class MetaCustomApi {
        public string UniqueName;

        /// <summary>
        /// Null when the custom API needs no privilege.
        /// </summary>
        public string ExecutePrivilegeName;
    }
}
