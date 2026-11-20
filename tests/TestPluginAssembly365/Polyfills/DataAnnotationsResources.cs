#if !NET8_0_OR_GREATER

using System.Activities.Validation;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Web.UI.WebControls;

namespace System.ComponentModel.DataAnnotations.Resources
{
    internal static class DataAnnotationsResources
    {
        public const string ValidationAttribute_Cannot_Set_ErrorMessage_And_Resource = "Either ErrorMessageString or ErrorMessageResourceName must be set, but not both.";
        public const string ValidationAttribute_NeedBothResourceTypeAndResourceName = "Both ErrorMessageResourceType and ErrorMessageResourceName need to be set on this attribute.";
        public const string ValidationAttribute_ResourcePropertyNotStringType = "The property '{0}' on resource type '{1}' is not a string type.";
        public const string ValidationAttribute_ResourceTypeDoesNotHaveProperty = "The resource type '{0}' does not have an accessible static property named '{1}'.";
        public const string ValidationAttribute_ValidationError = "The field {0} is invalid.";
        public const string ValidationAttribute_IsValid_NotImplemented = "IsValid(object value) has not been implemented by this class.  The preferred entry point is GetValidationResult() and classes should override IsValid(object value, ValidationContext context).";

        public const string MaxLengthAttribute_InvalidMaxLength = "MaxLengthAttribute must have a Length value that is greater than zero. Use MaxLength() without parameters to indicate that the string or array can have the maximum allowable length.";
        public const string MaxLengthAttribute_ValidationError = "The field {0} must be a string or array type with a maximum length of '{1}'.";

        public const string RangeAttribute_ArbitraryTypeNotIComparable = "The type {0} must implement {1}.";
        public const string RangeAttribute_MinGreaterThanMax = "The maximum value '{0}' must be greater than or equal to the minimum value '{1}'.";
        public const string RangeAttribute_Must_Set_Min_And_Max = "The minimum and maximum values must be set.";
        public const string RangeAttribute_Must_Set_Operand_Type = "The OperandType must be set when strings are used for minimum and maximum values.";
        public const string RangeAttribute_ValidationError = "The field {0} must be between {1} and {2}.";

        public const string AttributeStore_Unknown_Property = "The type '{0}' does not contain a public property named '{1}'.";
        public const string ValidationContextServiceContainer_ItemAlreadyExists = "A service of type '{0}' already exists in the container.";

        public const string DisplayAttribute_PropertyNotSet = "The {0} property has not been set.  Use the {1} method to get the value.";
        public const string LocalizableString_LocalizationFailed = "Cannot retrieve property '{0}' because localization failed.  Type '{1}' is not public or does not contain a public static string property with the name '{2}'.";
    }
}

#endif