namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A data operation whose table could not be determined statically. It is reported instead of dropped,
/// because a privilege can't be named without a table.
/// </summary>
public sealed class UnresolvedOperation(string operation, IReadOnlyList<Privilege> privileges, string reason, RunsAs runsAs, RequirementSource source)
{
    /// <summary>
    /// The service operation, e.g. <c>Create</c> or <c>RetrieveMultiple</c>.
    /// </summary>
    public string Operation { get; } = operation;

    /// <summary>
    /// The privileges the operation needs on the unresolved table.
    /// </summary>
    public IReadOnlyList<Privilege> Privileges { get; } = privileges;

    /// <summary>
    /// Why the table could not be resolved.
    /// </summary>
    public string Reason { get; } = reason;

    public RunsAs RunsAs { get; } = runsAs;

    public RequirementSource Source { get; } = source;
}
