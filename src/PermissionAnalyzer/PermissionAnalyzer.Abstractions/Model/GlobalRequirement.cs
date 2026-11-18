namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A privilege not tied to a table that the code needs under one identity, such as the execute privilege of a custom
/// API or <c>prvBulkDelete</c> for a <c>BulkDelete</c> request, with every call site that needs it.
/// </summary>
public sealed class GlobalRequirement(string privilege, RunsAs runsAs, RequirementBasis basis, IReadOnlyList<RequirementSource> sources)
{
    /// <summary>
    /// What setting <c>CallerId</c> needs on the identity that sets it.
    /// </summary>
    public const string ActOnBehalfOfAnotherUser = "prvActOnBehalfOfAnotherUser";

    /// <summary>
    /// What creating a record with <c>overriddencreatedon</c> set needs.
    /// </summary>
    public const string OverrideCreatedOnCreatedBy = "prvOverrideCreatedOnCreatedBy";

    /// <summary>
    /// What the <c>BypassCustomPluginExecution</c> parameter, or <c>BypassPluginExecution</c> on a <c>ServiceClient</c>,
    /// needs.
    /// </summary>
    public const string BypassCustomPlugins = "prvBypassCustomPlugins";

    /// <summary>
    /// What the <c>BypassBusinessLogicExecution</c> and <c>BypassBusinessLogicExecutionStepIds</c> parameters need.
    /// </summary>
    public const string BypassCustomBusinessLogic = "prvBypassCustomBusinessLogic";

    /// <summary>
    /// The privilege name, such as <c>prvActOnBehalfOfAnotherUser</c>.
    /// </summary>
    public string Privilege { get; } = privilege;

    public RunsAs RunsAs { get; } = runsAs;

    public RequirementBasis Basis { get; } = basis;

    /// <summary>
    /// Call sites that need this privilege, sorted by file and position.
    /// </summary>
    public IReadOnlyList<RequirementSource> Sources { get; } = sources;
}
