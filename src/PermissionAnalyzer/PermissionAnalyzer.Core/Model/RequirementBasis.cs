namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// Why a requirement is part of the requirement set. A basis is a fact about the code, not a likelihood.
/// </summary>
public enum RequirementBasis
{
    /// <summary>
    /// Table and privilege resolved from the call itself: types, constants or local data flow.
    /// </summary>
    Direct
}
