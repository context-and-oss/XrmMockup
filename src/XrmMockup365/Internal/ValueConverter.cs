using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup.Internal
{
    internal static class ValueConverter
    {
        public static object ConvertToComparableObject(object obj)
        {
            switch (obj)
            {
                case EntityReference entityReference:
                    return entityReference.Id;
                case Money money:
                    return money.Value;
                case AliasedValue aliasedValue:
                    return ConvertToComparableObject(aliasedValue.Value);
                case OptionSetValue optionSetValue:
                    return optionSetValue.Value;
                case Enum _:
                    return (int)obj;
                default:
                    return obj;
            }
        }

        public static bool AreEqual(object stored, object value)
        {
            stored = ConvertToComparableObject(stored);
            value = ConvertToComparableObject(value);

            if (stored == null || value == null) {
                return false;
            }
            if (stored is string storedString) {
                return storedString.Equals((string)ConvertTo(value, typeof(string)), StringComparison.OrdinalIgnoreCase);
            }
            if (stored is DateTime storedDate) {
                var valueDate = (DateTime)ConvertTo(value, typeof(DateTime));
                return DateTime.Equals(storedDate.ToUniversalTime(), valueDate.ToUniversalTime());
            }

            // Widen first, so an int key matches a decimal column
            return Equals(stored, ConvertTo(value, stored.GetType()));
        }

        public static object ConvertTo(object value, Type targetType)
        {
            // If the value, or target type, are null, nothing to convert, return the value
            if (targetType is null || value is null)
            {
                return value;
            }

            // Boxed values never report Nullable<T>, and Convert.ChangeType rejects it as a target
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            var valueType = value.GetType();
            if (valueType == targetType)
            {
                // If the types match, just return the object
                return value;
            }

            // We might be trying to convert a string 0, or 1 to a bool
            if (targetType == typeof(bool) && value is string str && decimal.TryParse(str, out var numericValue))
            {
                return numericValue != 0;
            }

            // Can we convert from the value's type converter to the target type?
            var valueConverter = System.ComponentModel.TypeDescriptor.GetConverter(valueType);
            if (valueConverter.CanConvertTo(targetType))
            {
                return valueConverter.ConvertTo(value, targetType);
            }

            // Can we convert to the target's type using the target type converter?
            var targetConverter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
            if (targetConverter.CanConvertFrom(valueType))
            {
                return targetConverter.ConvertFrom(value);
            }

            // Fallback to Convert.ChangeType which handles most IConvertible types
            return Convert.ChangeType(value, targetType);
        }
    }
}