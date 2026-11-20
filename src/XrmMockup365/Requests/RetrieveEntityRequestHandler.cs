using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveEntityRequestHandler : RequestHandler
    {
        internal RetrieveEntityRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveEntity") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveEntityRequest>(orgRequest);

            if (request.LogicalName == null && request.MetadataId == Guid.Empty)
            {
                throw new FaultException("Entity logical name is required when MetadataId is not specified");
            }

            EntityMetadata entityMetadata = null;

            if (request.LogicalName != null && metadata.EntityMetadata.ContainsKey(request.LogicalName))
            {
                entityMetadata = metadata.EntityMetadata[request.LogicalName];
            }
            else if (request.MetadataId != Guid.Empty)
            {
                entityMetadata = metadata.EntityMetadata.FirstOrDefault(x => x.Value.MetadataId == request.MetadataId).Value;
            }
            else
            {
                throw new FaultException($"Could not find entity with logicalname {request.LogicalName} or metadataid {request.MetadataId}");
            }

            var resp = new RetrieveEntityResponse();
            resp.Results["EntityMetadata"] = FilterEntityMetadataProperties(entityMetadata, request.EntityFilters);
            return resp;
        }

        public static EntityMetadata FilterEntityMetadataProperties(EntityMetadata metadata, EntityFilters entityFilters)
        {
            var propsToRemove = new List<string>();
            var privilegesProps = new List<string>() { "Privileges" };
            var attributesProps = new List<string>() { "Attributes" };
            var relationshipsProps = new List<string>() { "OneToManyRelationships", "ManyToOneRelationships", "ManyToManyRelationships" };

            switch (entityFilters)
            {
                case EntityFilters.All:
                    return metadata;
                case EntityFilters.Attributes:
                    propsToRemove.AddRange(privilegesProps);
                    propsToRemove.AddRange(relationshipsProps);
                    break;
                case EntityFilters.Privileges:
                    propsToRemove.AddRange(attributesProps);
                    propsToRemove.AddRange(relationshipsProps);
                    break;
                case EntityFilters.Relationships:
                    propsToRemove.AddRange(attributesProps);
                    propsToRemove.AddRange(privilegesProps);
                    break;
                case EntityFilters.Entity:
                // Default case covers EntityFilters.Entity and EntityFiltes.Default as they are equal
                default:
                    propsToRemove.AddRange(attributesProps);
                    propsToRemove.AddRange(privilegesProps);
                    propsToRemove.AddRange(relationshipsProps);
                    break;
            }

            return CopyFilteredEntityMetadata(metadata, propsToRemove);
        }

        private static EntityMetadata CopyFilteredEntityMetadata(EntityMetadata source, ICollection<string> filteredAttributes)
        {
            var sourceProps = source.GetType().GetProperties()
                .Where(x => x.CanRead && !filteredAttributes.Contains(x.Name))
                .ToList();

            var dest = new EntityMetadata();
            var destProps = dest.GetType().GetProperties()
                .Where(x => x.CanWrite && !filteredAttributes.Contains(x.Name))
                .ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (destProps.Any(x => x.Name == sourceProp.Name))
                {
                    var p = destProps.First(x => x.Name == sourceProp.Name);
                    if (p.CanWrite)
                    {
                        p.SetValue(dest, sourceProp.GetValue(source, null), null);
                    }
                }
            }
            return dest;
        }
    }
}
