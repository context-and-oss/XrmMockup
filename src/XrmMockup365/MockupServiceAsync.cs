using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;
#if DATAVERSE_SERVICE_CLIENT
using Microsoft.PowerPlatform.Dataverse.Client;
#endif

namespace DG.Tools.XrmMockup
{
#if DATAVERSE_SERVICE_CLIENT
    /// <summary>
    /// A class for Mocking the IOrganizationServiceAsync when exposing through the Dataverse Client
    /// </summary>
    internal partial class MockupService : IOrganizationServiceAsync
    {
        public Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Associate(entityName, entityId, relationship, relatedEntities);
            return Task.CompletedTask;
        }

        public Task<Guid> CreateAsync(Entity entity)
        {
            return Task.FromResult(Create(entity));
        }

        public Task DeleteAsync(string entityName, Guid id)
        {
            Delete(entityName, id);
            return Task.CompletedTask;
        }

        public Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Disassociate(entityName, entityId, relationship, relatedEntities);
            return Task.CompletedTask;
        }

        public Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request)
        {
            return Task.FromResult(Execute(request));
        }

        public Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet)
        {
            return Task.FromResult(Retrieve(entityName, id, columnSet));
        }

        public Task<EntityCollection> RetrieveMultipleAsync(QueryBase query)
        {
            return Task.FromResult(RetrieveMultiple(query));
        }

        public Task UpdateAsync(Entity entity)
        {
            Update(entity);
            return Task.CompletedTask;
        }
    }
#endif
}
