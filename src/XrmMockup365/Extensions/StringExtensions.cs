using XrmPluginCore.Enums;

namespace DG.Tools.XrmMockup.Extensions
{
    internal static class StringExtensions
    {
        public static bool Matches(this string value, EventOperation operation) => value.Equals(operation.ToString());
    }
}
