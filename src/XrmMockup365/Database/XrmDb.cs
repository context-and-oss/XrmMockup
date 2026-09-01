using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Threading;
using DG.Tools.XrmMockup.Serialization;
using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Online;

namespace DG.Tools.XrmMockup.Database {

    internal class XrmDb {
        // Using ConcurrentDictionary for thread-safe table access in parallel test scenarios
        private ConcurrentDictionary<string, DbTable> TableDict = new ConcurrentDictionary<string, DbTable>();
        private readonly Dictionary<string, EntityMetadata> EntityMetadata;
        private readonly IOnlineDataService OnlineDataService;
        private int sequence;

        public XrmDb(Dictionary<string, EntityMetadata> entityMetadata, IOnlineDataService onlineDataService) {
            this.EntityMetadata = entityMetadata;
            this.OnlineDataService = onlineDataService;
            sequence = 0;
        }

        public DbTable this[string tableName] {
            get {
                return TableDict.GetOrAdd(tableName, name => {
                    if (!EntityMetadata.TryGetValue(name, out EntityMetadata entityMetadata)) {
                        throw new MockupException($"No EntityMetadata found for entity with logical name '{name}'.");
                    }
                    return new DbTable(entityMetadata);
                });
            }
        }

        public void Add(Entity xrmEntity, bool withReferenceChecks = true)
        {
            int nextSequence = Interlocked.Increment(ref sequence);
            var dbEntity = ToDbRow(xrmEntity,nextSequence, withReferenceChecks);
            this[dbEntity.Table.TableName][dbEntity.Id] = dbEntity;
        }

        public DbRow ToDbRow(Entity xrmEntity, int sequence, bool withReferenceChecks = true)
        {
            var primaryIdKey = this[xrmEntity.LogicalName].Metadata.PrimaryIdAttribute;
            if (!xrmEntity.Attributes.ContainsKey(primaryIdKey))
            {
                xrmEntity[primaryIdKey] = xrmEntity.Id;
            }

            var dbEntity = DbRow.FromEntity(xrmEntity, this, withReferenceChecks);
            dbEntity.Sequence = sequence;
            if (dbEntity.Id != Guid.Empty)
            {
                if (this[dbEntity.Table.TableName][dbEntity.Id] != null)
                {
                    throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}' and id '{xrmEntity.Id}', but a record already exists with that Id.");
                }
            }
            else
            {
                dbEntity.Id = Guid.NewGuid();
            }

            return dbEntity;
        }

        public void AddRange(IEnumerable<Entity> xrmEntities, bool withReferenceChecks = true) {
            foreach (var xrmEntity in xrmEntities) Add(xrmEntity, withReferenceChecks);
        }

        internal IEnumerable<DbRow> GetDBEntityRows(string EntityLogicalName)
        {
            return this[EntityLogicalName];
        }

        public void Update(Entity xrmEntity, bool withReferenceChecks = true) {
            var currentDbRow = GetDbRow(xrmEntity);

            var dbEntity = DbRow.FromEntity(xrmEntity, withReferenceChecks ? this : null);
            this[dbEntity.Table.TableName][dbEntity.Id] = dbEntity;
        }

        public void Delete(Entity xrmEntity) {
            this[xrmEntity.LogicalName].Remove(xrmEntity.Id);
        }

        internal bool HasRow(EntityReference reference) {
            return this[reference.LogicalName][reference.Id] != null;
        }

        internal bool HasTable(string tableName)
        {
            return TableDict.ContainsKey(tableName);
        }

        internal bool IsValidEntity(string entityLogicalName)
        {
            return EntityMetadata.TryGetValue(entityLogicalName, out EntityMetadata entityMetadata);
        }

        internal void PrefillDBWithOnlineData(QueryExpression queryExpr)
        {
            if (OnlineDataService != null)
            {
                var onlineEntities = OnlineDataService.RetrieveMultiple(queryExpr).Entities;
                foreach (var onlineEntity in onlineEntities)
                {
                    if (this[onlineEntity.LogicalName][onlineEntity.Id] == null)
                    {
                        Add(onlineEntity, true);
                    }
                }
            }
        }

        internal DbRow GetDbRow(EntityReference reference, bool withReferenceCheck = true)
        {
            DbRow currentDbRow = null;

            if (reference != null && reference.Id != Guid.Empty)
            {
                currentDbRow = this[reference.LogicalName][reference.Id];
                if (currentDbRow == null && OnlineDataService != null)
                {
                    if (!withReferenceCheck)
                    {
                        currentDbRow = DbRow.MakeDBRowRef(reference, this);
                    }
                    else
                    {
                        var onlineEntity = OnlineDataService.Retrieve(reference.LogicalName, reference.Id, new ColumnSet(true));
                        Add(onlineEntity, withReferenceCheck);
                        currentDbRow = this[reference.LogicalName][reference.Id];
                    }
                }
                if (currentDbRow == null)
                {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with id '{reference.Id}' " +
                        "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
                }
            }

            // Try fetching with key attributes if any
            else if (reference?.KeyAttributes?.Count > 0) {
                currentDbRow = this[reference.LogicalName].FirstOrDefault(row => MatchesKeyAttributes(row, reference.KeyAttributes));

                if (currentDbRow == null) {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with key attributes '{reference.KeyAttributes.ToPrettyString()}' " +
                        "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
                }
            }

            // No identification given for the entity, throw error
            else
            {
                throw new FaultException("Missing a form of identification for the desired record in order to retrieve it.");
            }

            return currentDbRow;
        }

        // Lookups are stored as DbRow, so convert before matching.
        private static bool MatchesKeyAttributes(DbRow row, KeyAttributeCollection keyAttributes) {
            return keyAttributes.All(kv => {
                var rowValue = row[kv.Key];
                if (rowValue is DbRow related) rowValue = related.ToXrmEntityReference();
                return KeyValuesEqual(rowValue, kv.Value);
            });
        }

        private static bool KeyValuesEqual(object rowValue, object keyValue) {
            // Dataverse keys are only valid if every key field has a value
            if (rowValue == null || keyValue == null) {
                return false;
            }

            if (rowValue is EntityReference rowRef && keyValue is EntityReference keyRef) {
                return rowRef.Id == keyRef.Id && rowRef.LogicalName == keyRef.LogicalName;
            }

            return ValueConverter.AreEqual(rowValue, keyValue);
        }

        internal DbRow GetDbRow(Entity xrmEntity) {
            return GetDbRow(xrmEntity.ToEntityReferenceWithKeyAttributes());
        }

        internal DbRow GetDbRow(string logicalName, Guid id) {
            return GetDbRow(new EntityReference(logicalName, id));
        }

        public Entity GetEntity(string logicalName, Guid id) {
            return GetDbRow(logicalName, id).ToEntity();
        }

        public Entity GetEntity(EntityReference reference) {
            return GetDbRow(reference).ToEntity();
        }

        #region GetOrNull

        internal bool TryGetDbRow(EntityReference reference, out DbRow dbRow)
        {
            DbRow currentDbRow = null;
            dbRow = null;

            if (reference?.Id != default && reference?.Id != Guid.Empty)
            {
                currentDbRow = this[reference.LogicalName][reference.Id];
                if (currentDbRow == null)
                {
                    return false;
                }
                else
                {
                    dbRow = currentDbRow;
                    return true;
                }
            }

            // Try fetching with key attributes if any
            else if (reference?.KeyAttributes?.Count > 0)
            {
                currentDbRow = this[reference.LogicalName].FirstOrDefault(row => MatchesKeyAttributes(row, reference.KeyAttributes));

                if (currentDbRow == null)
                {
                    return false;
                }
                else
                {
                    dbRow = currentDbRow;
                    return true;
                }
            }

            // No identification given for the entity, return false
            else
            {
                return false;
            }
        }

        internal DbRow GetDbRowOrNull(EntityReference reference)
        {
            if (TryGetDbRow(reference, out DbRow row))
            {
                return row;
            }
            else
            {
                return null;
            }
        }

        internal Entity GetEntityOrNull(EntityReference reference)
        {
            if (TryGetDbRow(reference, out DbRow row))
            {
                return row.ToEntity();
            }
            else
            {
                return null;
            }
        }

        #endregion

        public XrmDb Clone()
        {
            var clonedTables = this.TableDict.ToDictionary(x => x.Key, x => x.Value.Clone());
            var clonedDB = new XrmDb(this.EntityMetadata, this.OnlineDataService)
            {
                TableDict = new ConcurrentDictionary<string, DbTable>(clonedTables)
            };

            return clonedDB;
        }
        public DbDTO ToSerializableDTO()
        {
            var jsonObj = new DbDTO
            {
                Tables = this.TableDict.ToDictionary(x => x.Key, x => x.Value.ToSerializableDTO())
            };
            return jsonObj;
        }
        public static XrmDb RestoreSerializableDTO(XrmDb current, DbDTO model)
        {
            var clonedTables = model.Tables.ToDictionary(x => x.Key, x => DbTable.RestoreSerializableDTO(new DbTable(current.EntityMetadata[x.Key]), x.Value));
            var clonedDB = new XrmDb(current.EntityMetadata, current.OnlineDataService)
            {
                TableDict = new ConcurrentDictionary<string, DbTable>(clonedTables)
            };

            foreach (var table in clonedTables)
            {
                table.Value.RestoreFromDTOPostProcess(clonedDB);
            }

            return clonedDB;
        }

        internal void ResetTable(string tableName)
        {
            TableDict[tableName] = new DbTable(this.EntityMetadata[tableName]);
        }
    }
}
