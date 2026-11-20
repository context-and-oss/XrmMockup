#if DATAVERSE_SERVICE_CLIENT
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tools.XrmMockup.Online;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest.Online
{
    /// <summary>
    /// Mock implementation of IOnlineDataService for unit testing XrmMockup's online data integration.
    /// Tracks all calls made to verify correct behavior.
    /// </summary>
    internal class MockOnlineDataService : IOnlineDataService
    {
        private readonly Dictionary<(string LogicalName, Guid Id), Entity> _entities = new Dictionary<(string, Guid), Entity>();

        /// <summary>
        /// List of all Retrieve calls made to this service.
        /// </summary>
        public List<(string EntityName, Guid Id, ColumnSet ColumnSet)> RetrieveCalls { get; } = new List<(string, Guid, ColumnSet)>();

        /// <summary>
        /// List of all RetrieveMultiple calls made to this service.
        /// </summary>
        public List<QueryExpression> RetrieveMultipleCalls { get; } = new List<QueryExpression>();

        /// <summary>
        /// Configures the mock to return the specified entity when retrieved.
        /// </summary>
        public void SetupEntity(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _entities[(entity.LogicalName, entity.Id)] = entity;
        }

        /// <summary>
        /// Configures the mock to return multiple entities.
        /// </summary>
        public void SetupEntities(IEnumerable<Entity> entities)
        {
            foreach (var entity in entities)
            {
                SetupEntity(entity);
            }
        }

        /// <summary>
        /// Clears all configured entities.
        /// </summary>
        public void ClearEntities()
        {
            _entities.Clear();
        }

        /// <summary>
        /// Clears all recorded calls.
        /// </summary>
        public void ClearCalls()
        {
            RetrieveCalls.Clear();
            RetrieveMultipleCalls.Clear();
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            RetrieveCalls.Add((entityName, id, columnSet));

            if (_entities.TryGetValue((entityName, id), out var entity))
            {
                return CloneEntity(entity, columnSet);
            }

            throw new Exception($"Entity {entityName} with id {id} not found in mock online data service");
        }

        public EntityCollection RetrieveMultiple(QueryExpression query)
        {
            RetrieveMultipleCalls.Add(query);

            var matches = _entities.Values
                .Where(e => e.LogicalName == query.EntityName)
                .Select(e => CloneEntity(e, query.ColumnSet))
                .ToList();

            return new EntityCollection(matches) { EntityName = query.EntityName };
        }

        public bool IsConnected => true;

        public void Dispose()
        {
            // Nothing to dispose
        }

        private static Entity CloneEntity(Entity entity, ColumnSet columnSet)
        {
            var clone = new Entity(entity.LogicalName, entity.Id);

            if (columnSet.AllColumns)
            {
                foreach (var attr in entity.Attributes)
                {
                    clone[attr.Key] = attr.Value;
                }
            }
            else
            {
                foreach (var column in columnSet.Columns)
                {
                    if (entity.Contains(column))
                    {
                        clone[column] = entity[column];
                    }
                }
            }

            return clone;
        }
    }
}
#endif
