using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Serialization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DG.Tools.XrmMockup.Database
{

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class DbRow {

        public DbTable Table { get; }
        public Guid Id { get; set; }
        public bool IsDeleted { get; private set; } = false;

        public int Sequence { get; set; }

        private Dictionary<string, object> Columns = new Dictionary<string, object>();
        private Dictionary<string, object> Keys = new Dictionary<string, object>();

        public EntityMetadata Metadata {
            get {
                return Table.Metadata;
            }
        }
        
        public Dictionary<string, AttributeMetadata> AttributeMetadata {
            get {
                return Table.AttributeMetadata;
            }
        }

        protected string DebuggerDisplay {
            get {
                string display = Table.TableName;

                var name = GetColumn<string>(Metadata.PrimaryNameAttribute);
                if (!string.IsNullOrEmpty(name)) display += $" ({name})";
                if (Id != Guid.Empty) display += $" [{Id}]";

                return display;
            }
        }

        public AttributeMetadata GetAttributeMetadata(string key) {
            if (!AttributeMetadata.TryGetValue(key, out AttributeMetadata attrMetadata))
                throw new MockupException($"'{Table.TableName}' entity doesn't contain attribute with name '{key}'");
            return attrMetadata;
        }

        public DbRow(DbTable table, Guid id, IEnumerable<KeyValuePair<string, object>> columns) {
            this.Table = table;
            this.Id = id;

            if (columns != null) {
                foreach (var col in columns) this[col.Key] = col.Value;
            }
        }

        public object this[string colName] {
            get {
                // check that attribute exists
                GetAttributeMetadata(colName);

                Columns.TryGetValue(colName, out object val);
                if (val is DbRow related && related.IsDeleted) {
                    Columns.Remove(colName);
                    return null;
                }
                return val;
            }
            set {
                if (ValidKeyAndValue(colName, value)) {
                    Columns[colName] = value;
                }
            }
        }

        public T GetColumn<T>(string colName) {
            return (T)this[colName];
        }

        public object GetColumn(string colName) {
            return this[colName];
        }

        public bool ColumnIsSet(string colName) {
            return this[colName] != null;
        }

        public bool ValidKeyAndValue(string key, object value) {
            if (value == null) return true;
            return DbAttributeTypeHelper.IsValidType(GetAttributeMetadata(key), value);
        }

        public void ApplyUpdates(IEnumerable<KeyValuePair<string, object>> columns) {
            foreach (var column in columns) this[column.Key] = column.Value;
        }

        private void PruneDeletedReferences() {
            foreach (var key in Columns.Keys.ToList()) {
                if (Columns[key] is DbRow related && related.IsDeleted) Columns.Remove(key);
            }
        }

        public void MarkAsDeleted() {
            this.IsDeleted = true;
        }

        public Entity ToEntity() {
            var xrmEntity = new Entity(Table.TableName) {
                Id = Id
            };

            // Prune deleted references
            this.PruneDeletedReferences();

            var attributes = Columns.Select(ConvertToXrmKeyValue);
            xrmEntity.Attributes.AddRange(attributes);

            var keys = Keys.Select(ConvertToXrmKeyValue);
            xrmEntity.KeyAttributes.AddRange(keys);

            return xrmEntity;
        }

        internal static DbRow MakeDBRowRef(EntityReference reference, XrmDb db)
        {
            return new DbRow(db[reference.LogicalName], reference.Id, new List<KeyValuePair<string, object>>());
        }

        internal static DbRow FromEntity(Entity xrmEntity, XrmDb db, bool withReferenceChecks = true) {
            IEnumerable<KeyValuePair<string, object>> columns = xrmEntity.Attributes;

            if (withReferenceChecks == true) { // Convert EntityReferences to actual references if db is given
                columns = columns.Select(a => ConvertToDbKeyValue(a, db));
            }

            return new DbRow(db[xrmEntity.LogicalName], xrmEntity.Id, columns);
        }

        internal static KeyValuePair<string, object> ConvertToDbKeyValue(KeyValuePair<string, object> xrmAttribute, XrmDb db) {
            return new KeyValuePair<string, object>(xrmAttribute.Key, ConvertToDbValue(xrmAttribute.Value, db));
        }

        internal static object ConvertToDbValue(object value, XrmDb db) {
            if (value is OptionSetValue osv) {
                return osv.Value;
            }
            if (value is Money money) {
                return money.Value;
            }
            if (value is EntityReference reference && db.IsValidEntity(reference.LogicalName)) {
                return db.GetDbRow(reference, false);
            }
            if (value is IEnumerable<Entity> entities) {
                return entities
                    .Where(e => db.IsValidEntity(e.LogicalName))
                    .Select(e => db.GetDbRow(e))
                    .ToArray();
            }
            if (value is OptionSetValueCollection optionsets)
            {
                return new OptionSetValueCollection(optionsets);
            }
            return value;
        }

        internal KeyValuePair<string, object> ConvertToXrmKeyValue(KeyValuePair<string, object> col) {
            return new KeyValuePair<string, object>(col.Key, ConvertToXrmValue(col.Value, GetAttributeMetadata(col.Key)));
        }

        internal static object ConvertToXrmValue(object dbValue, AttributeMetadata attrMetadata) {
            if (attrMetadata is EnumAttributeMetadata && dbValue is int dbValueInt) {
                return new OptionSetValue(dbValueInt);
            }
            if (attrMetadata is LookupAttributeMetadata && dbValue is DbRow dbValueRow) {
                return dbValueRow.ToXrmEntityReference();
            }
            if (attrMetadata is MoneyAttributeMetadata && dbValue is decimal dbValueDecimal) {
                return new Money(dbValueDecimal);
            }
            if (attrMetadata is IntegerAttributeMetadata)
            {
                return (int?)dbValue;
            }
            if (attrMetadata is BigIntAttributeMetadata)
            {
                return (long?)dbValue;
            }
            return dbValue;
        }


        public EntityReference ToXrmEntityReference() {
            return new EntityReference(Table.TableName, Id);
        }

        public DbRow Clone(DbTable table)
        {
            var clonedColumns = this.Columns.ToDictionary(x => x.Key, x => x.Value);
            var clonedDBRow = new DbRow(table, this.Id, clonedColumns)
            {
                IsDeleted = this.IsDeleted
            };
            return clonedDBRow;
        }
        public TableRowDTO ToSerializableDTO()
        {
            var jsonObj = new TableRowDTO
            {
                Id = this.Id,
                IsDeleted = this.IsDeleted,
                Columns = this.Columns.ToDictionary(x => x.Key, x => Utility.ConvertValueToSerializableDTO(x.Value))
            };
            return jsonObj;
        }

        public static DbRow RestoreSerializableDTO(DbTable table, TableRowDTO model)
        {
            var clonedColumns = model.Columns.ToDictionary(x => x.Key, x => Utility.ConvertValueFromSerializableDTO(x.Value));
            var clonedDBRow = new DbRow(table, model.Id, clonedColumns)
            {
                IsDeleted = model.IsDeleted
            };
            return clonedDBRow;
        }

        internal void RestoreFromDTOPostProcess(XrmDb clonedDB)
        {
            //Since there is no guarantee that data is recreated in a usefull order, we have to set the db row in a postprocess step.
            var keyesToIterate = new List<string>(this.Columns.Keys);
            foreach (var columnKey in keyesToIterate)
            {
                if (this.Columns[columnKey] is DbRow)
                {
                    var tmpRef = (DbRow)this.Columns[columnKey];
                    var dbRow = clonedDB[tmpRef.Metadata.LogicalName][tmpRef.Id];
                    this.Columns[columnKey] = dbRow;
                }
            }
        }
    }
}
