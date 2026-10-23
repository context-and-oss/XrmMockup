namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// The privileges needed by the data operations written directly in one method that no entry point reaches.
/// </summary>
public sealed class MethodManifest(
    string id,
    SourceLocation? declaration,
    IReadOnlyList<Requirement> requirements,
    IReadOnlyList<UnresolvedOperation> unresolved,
    IReadOnlyList<Assumption> assumptions,
    IReadOnlyList<GlobalRequirement>? globalRequirements = null) : IManifestSection
{
    /// <summary>
    /// Fully qualified method signature, e.g. <c>Contoso.Plugins.AccountPostPlugin.Execute(LocalPluginContext)</c>.
    /// </summary>
    public string Id { get; } = id;

    public SourceLocation? Declaration { get; } = declaration;

    /// <summary>
    /// Requirements sorted by table, privilege, identity and basis.
    /// </summary>
    public IReadOnlyList<Requirement> Requirements { get; } = requirements;

    /// <summary>
    /// Privileges not tied to a table, sorted by name, identity and basis.
    /// </summary>
    public IReadOnlyList<GlobalRequirement> GlobalRequirements { get; } = globalRequirements ?? [];

    /// <summary>
    /// Operations whose table could not be resolved, sorted by source location.
    /// </summary>
    public IReadOnlyList<UnresolvedOperation> Unresolved { get; } = unresolved;

    /// <summary>
    /// Operations counted as running as the caller because their identity is unknown, sorted by source location.
    /// </summary>
    public IReadOnlyList<Assumption> Assumptions { get; } = assumptions;
}
