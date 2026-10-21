namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// The privileges needed by the data operations written directly in one method.
/// </summary>
public sealed class MethodManifest(
    string id,
    SourceLocation? declaration,
    IReadOnlyList<Requirement> requirements,
    IReadOnlyList<UnresolvedOperation> unresolved)
{
    /// <summary>
    /// Fully qualified method signature, e.g. <c>Contoso.Plugins.AccountPostPlugin.Execute(LocalPluginContext)</c>.
    /// </summary>
    public string Id { get; } = id;

    public SourceLocation? Declaration { get; } = declaration;

    /// <summary>
    /// Requirements sorted by table, then privilege.
    /// </summary>
    public IReadOnlyList<Requirement> Requirements { get; } = requirements;

    /// <summary>
    /// Operations whose table could not be resolved, sorted by source location.
    /// </summary>
    public IReadOnlyList<UnresolvedOperation> Unresolved { get; } = unresolved;
}
