using System;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;

namespace DG.Tools.XrmMockup.Serialization
{
    public class SecurityModelDTO
    {
        public Dictionary<Guid, Guid> SecurityRoleMapping { get; set; }
        public List<Guid> AddedRoles { get; set; }
        public Dictionary<EntityReferenceDTO, Dictionary<EntityReferenceDTO, AccessRights>> Shares { get; set; }
        public Dictionary<Guid, Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>> PrinciplePrivilages { get; set; }
    }
}
