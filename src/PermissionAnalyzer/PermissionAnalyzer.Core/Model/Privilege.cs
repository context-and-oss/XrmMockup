namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A table privilege as granted by a Dataverse security role.
/// </summary>
public enum Privilege
{
    Create,
    Read,
    Write,
    Delete,
    Append,
    AppendTo,
    Assign,
    Share
}
