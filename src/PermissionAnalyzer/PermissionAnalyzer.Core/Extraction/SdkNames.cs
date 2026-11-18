namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The names of the Dataverse SDK members the analysis reads in code.
/// </summary>
internal static class SdkNames
{
    // Entity, EntityReference and EntityCollection
    public const string LogicalName = "LogicalName";
    public const string Attributes = "Attributes";
    public const string ToEntity = "ToEntity";
    public const string ToEntityReference = "ToEntityReference";
    public const string EntityName = "EntityName";
    public const string Entities = "Entities";

    // Queries
    public const string Query = "Query";
    public const string AddLink = "AddLink";
    public const string LinkEntities = "LinkEntities";

    // Requests
    public const string Target = "Target";
    public const string RequestName = "RequestName";
    public const string Parameters = "Parameters";

    // Plugin execution context and service clients
    public const string PrimaryEntityName = "PrimaryEntityName";
    public const string UserId = "UserId";
    public const string InitiatingUserId = "InitiatingUserId";
    public const string CallerId = "CallerId";
    public const string CallerAADObjectId = "CallerAADObjectId";
    public const string CreateOrganizationService = "CreateOrganizationService";

    /// <summary>
    /// <c>IPlugin.Execute</c> and <c>CodeActivity.Execute</c>.
    /// </summary>
    public const string Execute = "Execute";
}
