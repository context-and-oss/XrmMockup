namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A place where code starts running, with every privilege needed by the operations it reaches.
/// </summary>
public sealed class EntryPoint(
    string id,
    EntryPointKind kind,
    string type,
    string? assembly,
    Trigger? trigger,
    string? impersonatingUserId,
    SourceLocation? declaration,
    IReadOnlyList<Requirement> requirements,
    IReadOnlyList<UnresolvedOperation> unresolved,
    IReadOnlyList<Assumption> assumptions,
    IReadOnlyList<string>? triggers = null)
{
    /// <summary>
    /// A stable id such as <c>plugin:Contoso.AccountPostPlugin:Update:account:PostOperation</c>.
    /// </summary>
    public string Id { get; } = id;

    public EntryPointKind Kind { get; } = kind;

    /// <summary>
    /// The full name of the plugin, activity or class that declares the entry point.
    /// </summary>
    public string Type { get; } = type;

    /// <summary>
    /// The assembly that declares the entry point, such as the function app.
    /// </summary>
    public string? Assembly { get; } = assembly;

    public Trigger? Trigger { get; } = trigger;

    /// <summary>
    /// The user a plugin step runs as instead of the caller.
    /// </summary>
    public string? ImpersonatingUserId { get; } = impersonatingUserId;

    /// <summary>
    /// Where the handler is declared.
    /// </summary>
    public SourceLocation? Declaration { get; } = declaration;

    /// <summary>
    /// Requirements sorted by table, privilege, identity and basis.
    /// </summary>
    public IReadOnlyList<Requirement> Requirements { get; } = requirements;

    public IReadOnlyList<UnresolvedOperation> Unresolved { get; } = unresolved;

    public IReadOnlyList<Assumption> Assumptions { get; } = assumptions;

    /// <summary>
    /// The ids of the plugin steps and custom APIs the entry point's operations fire, directly or through other
    /// steps, sorted. Their requirements are part of <see cref="Requirements"/>.
    /// </summary>
    public IReadOnlyList<string> Triggers { get; } = triggers ?? [];
}
