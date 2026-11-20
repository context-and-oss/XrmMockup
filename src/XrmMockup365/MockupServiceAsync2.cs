using System;
using Microsoft.Xrm.Sdk;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;
using System.Threading;
#if DATAVERSE_SERVICE_CLIENT
using Microsoft.PowerPlatform.Dataverse.Client;
#endif

namespace DG.Tools.XrmMockup
{
#if DATAVERSE_SERVICE_CLIENT
    /// <summary>
    /// A class for Mocking the IOrganizationServiceAsync when exposing through the Dataverse Client
    /// </summary>
    internal partial class MockupService : IOrganizationServiceAsync2
    {
        public Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, CancellationToken cancellation)
        {
            Associate(entityName, entityId, relationship, relatedEntities);
            return Task.CompletedTask;
        }

        public Task<Guid> CreateAsync(Entity entity, CancellationToken cancellation)
        {
            return Task.FromResult(Create(entity));
        }

        public async Task<Entity> CreateAndReturnAsync(Entity entity, CancellationToken cancellationToken)
        {
            var entityId = await CreateAsync(entity, cancellationToken);
            return await RetrieveAsync(entity.LogicalName, entityId, new ColumnSet(true), cancellationToken);
        }

        public Task DeleteAsync(string entityName, Guid id, CancellationToken cancellation)
        {
            Delete(entityName, id);
            return Task.CompletedTask;
        }

        public Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, CancellationToken cancellation)
        {
            Disassociate(entityName, entityId, relationship, relatedEntities);
            return Task.CompletedTask;
        }

        public Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, CancellationToken cancellation)
        {
            return Task.FromResult(Execute(request));
        }

        public Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet, CancellationToken cancellation)
        {
            return Task.FromResult(Retrieve(entityName, id, columnSet));
        }

        public Task<EntityCollection> RetrieveMultipleAsync(QueryBase query, CancellationToken cancellation)
        {
            return Task.FromResult(RetrieveMultiple(query));
        }

        public Task UpdateAsync(Entity entity, CancellationToken cancellation)
        {
            Update(entity);
            return Task.CompletedTask;
        }
    }
#endif
}
