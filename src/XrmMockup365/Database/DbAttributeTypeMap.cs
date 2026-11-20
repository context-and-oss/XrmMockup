using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Database
{
    internal static class DbAttributeTypeHelper
    {
        private static Dictionary<AttributeTypeCode, Type> _metadataTypeMap = new Dictionary<AttributeTypeCode, Type>() {
            { AttributeTypeCode.BigInt, typeof(long) },
            { AttributeTypeCode.Boolean, typeof(bool) },
            { AttributeTypeCode.DateTime, typeof(DateTime) },
            { AttributeTypeCode.Decimal, typeof(decimal) },
            { AttributeTypeCode.Double, typeof(double) },
            
            { AttributeTypeCode.EntityName, typeof(string) },
            { AttributeTypeCode.Picklist, typeof(int) },
            { AttributeTypeCode.State, typeof(int) },
            { AttributeTypeCode.Status, typeof(int) },

            { AttributeTypeCode.Integer, typeof(int) },
            { AttributeTypeCode.Lookup,  typeof(DbRow) },
            { AttributeTypeCode.ManagedProperty, typeof(BooleanManagedProperty) },
            { AttributeTypeCode.Memo, typeof(string) },
            { AttributeTypeCode.Money, typeof(decimal) },
            { AttributeTypeCode.Owner, typeof(DbRow) },
            { AttributeTypeCode.Customer, typeof(DbRow) },
            { AttributeTypeCode.PartyList, typeof(EntityCollection) },
            { AttributeTypeCode.String, typeof(string) },
            { AttributeTypeCode.Uniqueidentifier, typeof(Guid) },
            { AttributeTypeCode.Virtual, typeof(DbRow[]) }
        };

        public static bool IsValidType(AttributeMetadata attrMetadata, object value) {
            if (value == null) return true;
            _metadataTypeMap.TryGetValue(attrMetadata.AttributeType.Value, out Type expectedType);
            if (expectedType == null) {
                throw new NotImplementedException($"Attribute of type '{attrMetadata.AttributeType.Value}' is not implemented in XrmMockup yet.");
            }
            if (expectedType.Name == "DbRow[]" && attrMetadata.AttributeType.Value == AttributeTypeCode.Virtual)
                return true;

            // EntityName attributes with an OptionSet (like activitytypecode) store int values
            // EntityName attributes without an OptionSet store string values (entity logical names)
            if (attrMetadata.AttributeType.Value == AttributeTypeCode.EntityName)
            {
                if (attrMetadata is EnumAttributeMetadata enumAttr && enumAttr.OptionSet != null)
                {
                    return value is int;
                }
                return value is string;
            }

            return expectedType == value.GetType();
        }
    }
}
