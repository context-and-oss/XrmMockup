namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// An operation whose identity couldn't be determined, so it is counted as running as the caller.
/// </summary>
public sealed class Assumption(string reason, RequirementSource source)
{
    /// <summary>
    /// Why the identity is unknown, such as <c>service comes from parameter 'service'</c>.
    /// </summary>
    public string Reason { get; } = reason;

    public RequirementSource Source { get; } = source;
}
