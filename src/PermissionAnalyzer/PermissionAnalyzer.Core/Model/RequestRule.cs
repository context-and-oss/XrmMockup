namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// The table privileges executing a request needs, such as Assign and Write on the <c>Target</c> of
/// <c>AssignRequest</c>. A rule with no checks, as, or requests marks a request that needs no table privilege.
/// </summary>
/// <param name="Message">
/// The message, such as <c>Assign</c>. The request type name (<c>AssignRequest</c>) is accepted too.
/// </param>
public sealed record RequestRule(string Message)
{
    public IReadOnlyList<RuleCheck> Checks { get; init; } = [];

    /// <summary>
    /// Messages whose checks this request makes as well, such as <c>Create</c> and <c>Update</c> for <c>Upsert</c>.
    /// </summary>
    public IReadOnlyList<string> As { get; init; } = [];

    /// <summary>
    /// The member that holds several targets, such as <c>Targets</c> of <c>CreateMultipleRequest</c>. The checks of
    /// the <see cref="As"/> messages on <c>Target</c> apply to it instead.
    /// </summary>
    public string? Targets { get; init; }

    /// <summary>
    /// The member that holds requests run as part of this one, such as <c>Requests</c> of <c>ExecuteMultipleRequest</c>.
    /// </summary>
    public string? Requests { get; init; }

    /// <summary>
    /// Whether XrmMockup's handler for the message makes the checks. Dataverse makes them either way.
    /// </summary>
    public bool Enforced { get; init; } = true;
}

/// <summary>
/// Privileges on the tables a request member refers to, or on a fixed table.
/// </summary>
/// <param name="Privileges">The privileges needed on the table.</param>
public sealed record RuleCheck(IReadOnlyList<Privilege> Privileges)
{
    /// <summary>
    /// The request member that refers to the table, such as <c>Target</c>.
    /// </summary>
    public string? Member { get; init; }

    /// <summary>
    /// A fixed table, such as <c>email</c> for <c>SendEmailRequest</c>, when no <see cref="Member"/> is set.
    /// </summary>
    public string? Table { get; init; }

    /// <summary>
    /// A member that holds access rights, such as <c>PrincipalAccess.AccessMask</c>; each right it grants is needed
    /// as well.
    /// </summary>
    public string? AccessMask { get; init; }

    /// <summary>
    /// A boolean member that must be set for the check to apply, such as <c>CreateAccount</c> of
    /// <c>QualifyLeadRequest</c>.
    /// </summary>
    public string? When { get; init; }
}
