namespace XrmMockup.PermissionAnalyzer.Core.Model;

internal static class RequirementBases
{
    /// <summary>
    /// The basis of a requirement reached through both: the later of the two in <see cref="RequirementBasis"/>.
    /// </summary>
    public static RequirementBasis Max(RequirementBasis a, RequirementBasis b) => a > b ? a : b;
}
