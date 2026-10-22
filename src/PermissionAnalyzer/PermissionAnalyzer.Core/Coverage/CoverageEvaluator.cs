using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Coverage;

/// <summary>
/// A missing privilege that is accepted, from <c>XrmMockup:Permissions:Suppress</c>.
/// </summary>
/// <param name="Table">Table logical name.</param>
/// <param name="Privilege">The privilege, or <c>null</c> for every privilege on the table.</param>
/// <param name="Reason">Why it is accepted, shown in the output.</param>
public sealed record Suppression(string Table, Privilege? Privilege, string? Reason)
{
    public bool Matches(Requirement requirement) =>
        string.Equals(Table, requirement.Table, StringComparison.OrdinalIgnoreCase) && (Privilege is null || Privilege == requirement.Privilege);
}

/// <summary>
/// The roles to check each identity against.
/// </summary>
public sealed class CoverageOptions
{
    /// <summary>
    /// <c>--role</c>: for the caller and the application user. Replaces <see cref="Principals"/> when set.
    /// </summary>
    public RoleSet? Roles { get; init; }

    /// <summary>
    /// <c>--caller-role</c>: for the user that app code acts for by setting <c>CallerId</c>.
    /// </summary>
    public RoleSet? CallerRoles { get; init; }

    /// <summary>
    /// The application user's roles, by assembly (<c>Contoso.Functions</c>) or by assembly and function
    /// (<c>Contoso.Functions/ProcessOrder</c>), which wins. Keys ignore case.
    /// </summary>
    public IReadOnlyDictionary<string, RoleSet> Principals { get; init; } = new Dictionary<string, RoleSet>();

    /// <summary>
    /// Roles of specific users, such as the impersonating user of a plugin step, by user id. Keys ignore case.
    /// </summary>
    public IReadOnlyDictionary<string, RoleSet> Users { get; init; } = new Dictionary<string, RoleSet>();

    public IReadOnlyList<Suppression> Suppress { get; init; } = [];
}

/// <summary>
/// Checks a manifest's requirements against security roles.
/// </summary>
public static class CoverageEvaluator
{
    private const string PrincipalsSetting = "XrmMockup:Permissions:Principals";
    private const string UsersSetting = "XrmMockup:Permissions:Users";

    public static CoverageReport Evaluate(Manifest manifest, CoverageOptions options)
    {
        var notGoverned = new HashSet<string>(manifest.NotRoleGoverned ?? [], StringComparer.Ordinal);
        var principals = new Dictionary<string, RoleSet>(StringComparer.OrdinalIgnoreCase);
        foreach (var principal in options.Principals) principals[principal.Key] = principal.Value;
        var users = new Dictionary<string, RoleSet>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in options.Users) users[user.Key] = user.Value;

        var usesConfiguredRoles = false;
        var sections = new List<SectionCoverage>();
        var missing = new SortedDictionary<(string Table, Privilege Privilege, RunsAs RunsAs, string Roles), (RoleSet Roles, SortedSet<string> In)>(MissingOrder);

        SectionCoverage Section(string id, EntryPoint? entryPoint, IReadOnlyList<Requirement> requirements, IReadOnlyList<UnresolvedOperation> unresolved)
        {
            var applicationUser = ApplicationUserRoles(entryPoint, options, principals);
            var identities = new SortedDictionary<RunsAs, IdentityRoles>();
            var coverage = new Dictionary<Requirement, RequirementCoverage>();

            foreach (var requirement in requirements)
            {
                var identity = requirement.RunsAs with { Assumed = false };
                if (!identities.TryGetValue(identity, out var roles))
                {
                    roles = Roles(identity, entryPoint, options, applicationUser, users);
                    if (roles.Roles is { } set && set != options.Roles && set != options.CallerRoles) usesConfiguredRoles = true;
                    identities.Add(identity, roles);
                }
                var result = Check(requirement, roles, notGoverned, options.Suppress);
                coverage.Add(requirement, result);
                if (result.Status == CoverageStatus.Missing)
                {
                    var key = (requirement.Table, requirement.Privilege, identity, roles.Roles!.ToString());
                    if (!missing.TryGetValue(key, out var found))
                    {
                        found = (roles.Roles, new SortedSet<string>(StringComparer.Ordinal));
                        missing.Add(key, found);
                    }
                    found.In.Add(id);
                }
            }

            var impersonation = requirements.Any(r => r.RunsAs.Kind == RunsAsKind.ImpersonatedCaller) ? new ImpersonationCheck(Impersonator(entryPoint)) : null;
            var needsRole = unresolved.Where(u => u.RunsAs.Kind != RunsAsKind.System).ToList();
            var section = new SectionCoverage(
                coverage,
                identities.Values.Where(i => i.RunsAs.Kind != RunsAsKind.System).ToList(),
                impersonation,
                needsRole.Count,
                needsRole.Count(u => u.Operation == "Execute"));
            sections.Add(section);
            return section;
        }

        var entryPoints = new Dictionary<string, SectionCoverage>(StringComparer.Ordinal);
        foreach (var entryPoint in manifest.EntryPoints)
        {
            entryPoints[entryPoint.Id] = Section(entryPoint.Id, entryPoint, entryPoint.Requirements, entryPoint.Unresolved);
        }

        var unreached = new Dictionary<string, SectionCoverage>(StringComparer.Ordinal);
        foreach (var method in manifest.Unreached)
        {
            unreached[method.Id] = Section(method.Id, null, method.Requirements, method.Unresolved);
        }

        var all = sections.SelectMany(s => s.All).ToList();
        int Count(CoverageStatus status) => all.Count(c => c.Status == status);
        var summary = new CoverageSummary(
            Count(CoverageStatus.Granted),
            Count(CoverageStatus.NotGoverned),
            Count(CoverageStatus.Missing),
            Count(CoverageStatus.Suppressed),
            Count(CoverageStatus.NotChecked),
            Count(CoverageStatus.System),
            sections.Sum(s => s.Unresolved),
            sections.Sum(s => s.UnresolvedExecute),
            sections.Count(s => s.Impersonation is not null));

        var missingPrivileges = missing.Select(m => new MissingPrivilege(m.Key.Table, m.Key.Privilege, m.Key.RunsAs, m.Value.Roles, m.Value.In.ToList())).ToList();
        return new CoverageReport(entryPoints, unreached, summary, missingPrivileges, usesConfiguredRoles);
    }

    private static readonly IComparer<(string Table, Privilege Privilege, RunsAs RunsAs, string Roles)> MissingOrder =
        Comparer<(string Table, Privilege Privilege, RunsAs RunsAs, string Roles)>.Create((x, y) =>
        {
            var result = string.CompareOrdinal(x.Table, y.Table);
            if (result == 0) result = x.Privilege.CompareTo(y.Privilege);
            if (result == 0) result = x.RunsAs.CompareTo(y.RunsAs);
            return result != 0 ? result : string.CompareOrdinal(x.Roles, y.Roles);
        });

    private static RequirementCoverage Check(Requirement requirement, IdentityRoles identity, HashSet<string> notGoverned, IReadOnlyList<Suppression> suppress)
    {
        if (requirement.RunsAs.Kind == RunsAsKind.System) return new RequirementCoverage(CoverageStatus.System);
        if (identity.Roles is not { } roles) return new RequirementCoverage(CoverageStatus.NotChecked);
        if (roles.Depth(requirement.Table, requirement.Privilege) is { } depth) return new RequirementCoverage(CoverageStatus.Granted, depth);
        if (notGoverned.Contains(requirement.Table)) return new RequirementCoverage(CoverageStatus.NotGoverned);

        return suppress.FirstOrDefault(s => s.Matches(requirement)) is { } suppression
            ? new RequirementCoverage(CoverageStatus.Suppressed, Reason: suppression.Reason)
            : new RequirementCoverage(CoverageStatus.Missing);
    }

    private static IdentityRoles Roles(RunsAs identity, EntryPoint? entryPoint, CoverageOptions options, RoleSet? applicationUser, Dictionary<string, RoleSet> users) =>
        identity.Kind switch
        {
            RunsAsKind.Caller => new IdentityRoles(identity, options.Roles, options.Roles is null ? "not checked: pass --role" : null),
            RunsAsKind.ApplicationUser => new IdentityRoles(identity, applicationUser, applicationUser is null
                ? entryPoint?.Assembly is { } assembly
                    ? $"not checked: pass --role, or map {assembly} in {PrincipalsSetting}"
                    : "not checked: pass --role"
                : null),
            RunsAsKind.User => users.TryGetValue(identity.UserId ?? "", out var roles)
                ? new IdentityRoles(identity, roles)
                : new IdentityRoles(identity, null, $"not checked: runs as user {identity.UserId}; map it in {UsersSetting}"),
            RunsAsKind.ImpersonatedCaller => new IdentityRoles(identity, options.CallerRoles, options.CallerRoles is null
                ? "not checked: acts for the user in CallerId; pass --caller-role with that user's roles"
                : null),
            _ => new IdentityRoles(identity, null),
        };

    /// <summary>
    /// <c>--role</c>, else the entry point's function or assembly in <see cref="CoverageOptions.Principals"/>.
    /// </summary>
    private static RoleSet? ApplicationUserRoles(EntryPoint? entryPoint, CoverageOptions options, Dictionary<string, RoleSet> principals)
    {
        if (options.Roles is not null) return options.Roles;
        if (entryPoint?.Assembly is not { } assembly) return null;

        const string functionPrefix = "function:";
        if (entryPoint.Kind == EntryPointKind.Function && entryPoint.Id.StartsWith(functionPrefix, StringComparison.Ordinal)
            && principals.TryGetValue($"{assembly}/{entryPoint.Id.Substring(functionPrefix.Length)}", out var function))
        {
            return function;
        }
        return principals.TryGetValue(assembly, out var app) ? app : null;
    }

    /// <summary>
    /// Who sets <c>CallerId</c>: the application user of app code, else the caller.
    /// </summary>
    private static RunsAs Impersonator(EntryPoint? entryPoint) =>
        entryPoint?.Kind switch
        {
            EntryPointKind.Function => RunsAs.ApplicationUser,
            EntryPointKind.Method or null => RunsAs.ApplicationUser with { Assumed = true },
            _ => RunsAs.Caller,
        };
}
