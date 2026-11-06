namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// How far a granted privilege reaches, as shown in the security role editor.
/// </summary>
public enum PrivilegeDepth
{
    User,
    BusinessUnit,
    ParentChildBusinessUnit,
    Organization
}
