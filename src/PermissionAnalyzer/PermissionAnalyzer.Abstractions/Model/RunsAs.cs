namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// Who an operation runs as. This decides which security roles have to grant its privileges.
/// </summary>
public enum RunsAsKind
{
    /// <summary>
    /// The user that triggered the plugin step, custom API or workflow.
    /// </summary>
    Caller,

    /// <summary>
    /// A specific user, by <see cref="RunsAs.UserId"/>: a constant id passed to <c>CreateOrganizationService</c>,
    /// or the impersonating user of a plugin step.
    /// </summary>
    User,

    /// <summary>
    /// The application user of app code, such as an Azure Function using <c>ServiceClient</c>.
    /// </summary>
    ApplicationUser,

    /// <summary>
    /// App code acting for a user by setting <c>CallerId</c>. The user needs the privilege; the application user
    /// needs to be allowed to act on behalf of other users.
    /// </summary>
    ImpersonatedCaller,

    /// <summary>
    /// SYSTEM (<c>CreateOrganizationService(null)</c>). No role is needed; listed for information.
    /// </summary>
    System,
}

/// <summary>
/// The identity an operation runs as. <see cref="Assumed"/> is set when the source of the service couldn't be
/// determined, and the analysis fell back to the caller.
/// </summary>
public sealed record RunsAs(RunsAsKind Kind, string? UserId = null, bool Assumed = false) : IComparable<RunsAs>
{
    public static RunsAs Caller { get; } = new(RunsAsKind.Caller);
    public static RunsAs System { get; } = new(RunsAsKind.System);
    public static RunsAs ApplicationUser { get; } = new(RunsAsKind.ApplicationUser);
    public static RunsAs ImpersonatedCaller { get; } = new(RunsAsKind.ImpersonatedCaller);

    public static RunsAs User(string userId) => new(RunsAsKind.User, userId.ToLowerInvariant());

    public int CompareTo(RunsAs? other)
    {
        if (other is null) return 1;
        var result = Kind.CompareTo(other.Kind);
        if (result == 0) result = string.CompareOrdinal(UserId, other.UserId);
        return result != 0 ? result : Assumed.CompareTo(other.Assumed);
    }

    public override string ToString()
    {
        var text = UserId is null ? Kind.ToString() : $"{Kind} {UserId}";
        return Assumed ? text + " (assumed)" : text;
    }
}
