using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using DG.Tools.XrmMockup.Serialization;

namespace DG.Tools.XrmMockup.Database {
    internal class DbTable : IEnumerable<DbRow> {
        private ConcurrentDictionary<Guid, DbRow> MainDict = new ConcurrentDictionary<Guid, DbRow>();

        public string TableName { get; set; }
        public EntityMetadata Metadata { get; set; }

        private Dictionary<string, AttributeMetadata> _attributeMetadata;
        public Dictionary<string, AttributeMetadata> AttributeMetadata {
            get {
                if (_attributeMetadata == null) _attributeMetadata = Metadata.Attributes.ToDictionary(x => x.LogicalName);
                return _attributeMetadata;
            }
        }

        internal void ReplaceMetadata(EntityMetadata metadata)
        {
            Metadata = metadata;
            _attributeMetadata = null;
        }

        public DbTable(EntityMetadata entityMetadata) {
            this.Metadata = entityMetadata;
            this.TableName = Metadata.LogicalName;
        }

        public DbRow this[Guid guid] {
            get {
                MainDict.TryGetValue(guid, out DbRow entity);
                return entity;
            }
            set {
                MainDict[guid] = value;
            }
        }

        public void Remove(Guid guid) {
            if (MainDict.TryRemove(guid, out DbRow row)) {
                row.MarkAsDeleted();
            } else {
                throw new InvalidOperationException($"No record of type '{TableName}' exists with GUID={guid}");
            }
        }

        public IEnumerator<DbRow> GetEnumerator() {
            // Return a snapshot to avoid collection modification exceptions during enumeration
            return MainDict.Values.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            // Return a snapshot to avoid collection modification exceptions during enumeration
            return MainDict.Values.ToList().GetEnumerator();
        }

        public DbTable Clone()
        {
            //var attributeMetadata = this._attributeMetadata.ToDictionary(x => x.Key, x => x.Value);
            var clonedTable = new DbTable(this.Metadata)
            {
                TableName = this.TableName,
                //_attributeMetadata = attributeMetadata,                
            };
            var clonedMainDict = new ConcurrentDictionary<Guid, DbRow>(this.MainDict.ToDictionary(x => x.Key, x => x.Value.Clone(clonedTable)));
            clonedTable.MainDict = clonedMainDict;
            return clonedTable;
        }
        public TableDTO ToSerializableDTO()
        {
            var jsonObj = new TableDTO
            {
                Name = this.TableName,
                Rows = this.MainDict.ToDictionary(x => x.Key, x => x.Value.ToSerializableDTO())
            };
            return jsonObj;
        }
        public static DbTable RestoreSerializableDTO(DbTable current, TableDTO model)
        {
            var clonedTable = new DbTable(current.Metadata)
            {
                TableName = current.TableName,
                MainDict = new ConcurrentDictionary<Guid, DbRow>(model.Rows.ToDictionary(x => x.Key, x => DbRow.RestoreSerializableDTO(current, x.Value)))
            };
            return clonedTable;
        }

        internal void RestoreFromDTOPostProcess(XrmDb clonedDB)
        {
            foreach (var row in this.MainDict.Values)
            {
                row.RestoreFromDTOPostProcess(clonedDB);
            }
        }
    }
}

