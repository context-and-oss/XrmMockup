using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Coverage;

/// <summary>
/// Whether the roles of a requirement's identity grant it.
/// </summary>
public enum CoverageStatus
{
    /// <summary>
    /// A role grants the privilege, at <see cref="RequirementCoverage.Depth"/>. Any depth counts.
    /// </summary>
    Granted,

    /// <summary>
    /// No security role in the metadata mentions the table, so XrmMockup allows every operation on it.
    /// </summary>
    NotGoverned,

    /// <summary>
    /// None of the roles grants the privilege.
    /// </summary>
    Missing,

    /// <summary>
    /// Missing, but suppressed in <c>XrmMockup:Permissions:Suppress</c>.
    /// </summary>
    Suppressed,

    /// <summary>
    /// No roles are known for the identity, so the requirement isn't checked.
    /// </summary>
    NotChecked,

    /// <summary>
    /// Runs as SYSTEM, which needs no role.
    /// </summary>
    System,

    /// <summary>
    /// A privilege not tied to a table, which the role metadata doesn't have: it was generated before those privileges
    /// were downloaded.
    /// </summary>
    Unverifiable,
}

/// <param name="Status">Whether the requirement is covered.</param>
/// <param name="Depth">The depth the roles grant, when <see cref="CoverageStatus.Granted"/>.</param>
/// <param name="Reason">Why it is suppressed, when <see cref="CoverageStatus.Suppressed"/>.</param>
public sealed record RequirementCoverage(CoverageStatus Status, PrivilegeDepth? Depth = null, string? Reason = null);

/// <summary>
/// The roles one identity of an entry point is checked against, or why it isn't checked.
/// </summary>
/// <param name="RunsAs">The identity, without <see cref="RunsAs.Assumed"/>.</param>
/// <param name="Roles">The roles, or <c>null</c> when none are known.</param>
/// <param name="Note">Why the identity isn't checked, when <paramref name="Roles"/> is <c>null</c>.</param>
public sealed record IdentityRoles(RunsAs RunsAs, RoleSet? Roles, string? Note = null);

/// <summary>
/// Role coverage for the requirements of one entry point or unreached method.
/// </summary>
public sealed class SectionCoverage(
    IReadOnlyDictionary<Requirement, RequirementCoverage> requirements,
    IReadOnlyDictionary<GlobalRequirement, RequirementCoverage> globalRequirements,
    IReadOnlyList<IdentityRoles> identities,
    int unresolved,
    int unresolvedNoRule,
    int unresolvedCustomApi,
    int unresolvedWorkflow)
{
    /// <summary>
    /// The identities of the section's requirements, with their roles, sorted by identity.
    /// </summary>
    public IReadOnlyList<IdentityRoles> Identities { get; } = identities;

    /// <summary>
    /// Unresolved operations that would need a role, so not those running as SYSTEM.
    /// </summary>
    public int Unresolved { get; } = unresolved;

    /// <summary>
    /// The part of <see cref="Unresolved"/> that are requests without a rule.
    /// </summary>
    public int UnresolvedNoRule { get; } = unresolvedNoRule;

    /// <summary>
    /// The part of <see cref="Unresolved"/> that are custom API calls, whose code or execute privilege isn't known.
    /// </summary>
    public int UnresolvedCustomApi { get; } = unresolvedCustomApi;

    /// <summary>
    /// The part of <see cref="Unresolved"/> that are workflows from the metadata, whose steps aren't analyzed.
    /// </summary>
    public int UnresolvedWorkflow { get; } = unresolvedWorkflow;

    public RequirementCoverage For(Requirement requirement) => requirements[requirement];

    public RequirementCoverage For(GlobalRequirement requirement) => globalRequirements[requirement];

    internal IEnumerable<RequirementCoverage> All => requirements.Values.Concat(globalRequirements.Values);
}

/// <summary>
/// Counts over every requirement of a <see cref="CoverageReport"/>, including privileges not tied to a table. <see cref="UnresolvedNoRule"/>,
/// <see cref="UnresolvedCustomApi"/> and <see cref="UnresolvedWorkflow"/> are the parts of <see cref="Unresolved"/> that are requests
/// without a rule, custom API calls and fired workflows.
/// </summary>
public sealed record CoverageSummary(
    int Granted,
    int NotGoverned,
    int Missing,
    int Suppressed,
    int NotChecked,
    int System,
    int Unresolved,
    int UnresolvedNoRule,
    int UnresolvedCustomApi,
    int UnresolvedWorkflow,
    int Unverifiable)
{
    /// <summary>
    /// Something the result doesn't cover: unresolved operations, identities without roles or privileges that
    /// can't be checked.
    /// </summary>
    public bool Incomplete => Unresolved + NotChecked + Unverifiable > 0;

    /// <summary>
    /// Nothing is missing. With <paramref name="strict"/>, everything was also checked: no unresolved operations,
    /// unchecked identities or unverifiable privileges.
    /// </summary>
    public bool Passed(bool strict) => Missing == 0 && (!strict || !Incomplete);
}

/// <summary>
/// A privilege the roles of an identity don't grant, with every entry point and unreached method that needs it.
/// </summary>
/// <param name="RunsAs">The identity, without <see cref="RunsAs.Assumed"/>.</param>
/// <param name="Roles">The roles that don't grant it.</param>
/// <param name="In">Ids of the entry points and unreached methods that need it, sorted.</param>
public sealed record MissingPrivilege(string Table, Privilege Privilege, RunsAs RunsAs, RoleSet Roles, IReadOnlyList<string> In);

/// <summary>
/// A privilege not tied to a table that the roles of an identity don't grant, with every entry point and unreached
/// method that needs it.
/// </summary>
/// <param name="Privilege">The privilege name, such as <c>prvActOnBehalfOfAnotherUser</c>.</param>
/// <param name="RunsAs">The identity, without <see cref="RunsAs.Assumed"/>.</param>
/// <param name="Roles">The roles that don't grant it.</param>
/// <param name="In">Ids of the entry points and unreached methods that need it, sorted.</param>
public sealed record MissingGlobalPrivilege(string Privilege, RunsAs RunsAs, RoleSet Roles, IReadOnlyList<string> In);

/// <summary>
/// Role coverage for a whole manifest.
/// </summary>
public sealed class CoverageReport(
    IReadOnlyDictionary<string, SectionCoverage> entryPoints,
    IReadOnlyDictionary<string, SectionCoverage> unreached,
    CoverageSummary summary,
    IReadOnlyList<MissingPrivilege> missing,
    IReadOnlyList<MissingGlobalPrivilege> missingGlobal,
    bool usesConfiguredRoles)
{
    public CoverageSummary Summary { get; } = summary;

    /// <summary>
    /// The missing requirements merged across entry points, sorted by table, privilege, identity and roles.
    /// </summary>
    public IReadOnlyList<MissingPrivilege> Missing { get; } = missing;

    /// <summary>
    /// The missing privileges not tied to a table merged across entry points, sorted by privilege, identity and roles.
    /// </summary>
    public IReadOnlyList<MissingGlobalPrivilege> MissingGlobal { get; } = missingGlobal;

    /// <summary>
    /// Some requirement was checked against roles from <c>Principals</c> or <c>Users</c>.
    /// </summary>
    public bool UsesConfiguredRoles { get; } = usesConfiguredRoles;

    public SectionCoverage For(IManifestSection section) => section is EntryPoint ? entryPoints[section.Id] : unreached[section.Id];
}
