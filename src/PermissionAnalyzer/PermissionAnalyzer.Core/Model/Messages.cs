namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// The Dataverse messages the analysis handles itself. A misspelled constant fails to compile, where a misspelled
/// string never matches.
/// </summary>
internal static class Messages
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Upsert = "Upsert";
    public const string Delete = "Delete";
    public const string Retrieve = "Retrieve";
    public const string RetrieveMultiple = "RetrieveMultiple";
    public const string Associate = "Associate";
    public const string Disassociate = "Disassociate";
    public const string Execute = "Execute";
}
