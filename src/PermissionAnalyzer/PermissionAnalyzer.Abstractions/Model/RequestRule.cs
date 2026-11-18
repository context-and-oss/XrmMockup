namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// The privileges executing a request needs, such as Assign and Write on the <c>Target</c> of <c>AssignRequest</c>,
/// or prvBulkDelete for <c>BulkDeleteRequest</c>. A rule with no checks, global privileges, as, or requests marks a
/// request that needs no privilege.
/// </summary>
/// <param name="Message">
/// The message, such as <c>Assign</c>. The request type name (<c>AssignRequest</c>) is accepted too.
/// </param>
public sealed record RequestRule(string Message)
{
    public IReadOnlyList<RuleCheck> Checks { get; init; } = [];

    /// <summary>
    /// Privileges the request needs by name, such as <c>prvBulkDelete</c>. Each is looked up in the privilege
    /// catalog, where it is a privilege not tied to a table or one on the tables it applies to.
    /// </summary>
    public IReadOnlyList<string> GlobalPrivileges { get; init; } = [];

    /// <summary>
    /// What the analysis assumes about the request, such as that an email is sent as its sender, when what it needs
    /// depends on data only known at runtime.
    /// </summary>
    public string? Assumption { get; init; }

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

    /// <summary>
    /// The table the request's plugin steps fire on, when it isn't that of its first check on a member: such as
    /// <c>opportunity</c> for <c>WinOpportunityRequest</c>, whose first check is on its <c>OpportunityClose</c>.
    /// </summary>
    public string? FiresOn { get; init; }

    /// <summary>
    /// The message the request's plugin steps are registered on, when it isn't the request's own: such as
    /// <c>Win</c> for <c>WinOpportunityRequest</c>.
    /// </summary>
    public string? StepMessage { get; init; }
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
