using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Coverage;

/// <summary>
/// A missing privilege that is accepted, from <c>XrmMockup:Permissions:Suppress</c>.
/// </summary>
/// <param name="Table">Table logical name, or empty for a privilege not tied to a table.</param>
/// <param name="Privilege">The privilege, or <c>null</c> for every privilege on the table.</param>
/// <param name="Reason">Why it is accepted, shown in the output.</param>
/// <param name="GlobalPrivilege">
/// The name of a privilege not tied to a table, such as <c>prvActOnBehalfOfAnotherUser</c>, when
/// <paramref name="Table"/> is empty.
/// </param>
public sealed record Suppression(string Table, Privilege? Privilege, string? Reason, string? GlobalPrivilege = null)
{
    public bool Matches(Requirement requirement) =>
        Table.Length > 0 && string.Equals(Table, requirement.Table, StringComparison.OrdinalIgnoreCase) && (Privilege is null || Privilege == requirement.Privilege);

    public bool Matches(GlobalRequirement requirement) =>
        string.Equals(GlobalPrivilege, requirement.Privilege, StringComparison.OrdinalIgnoreCase);
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
        var missingGlobal = new SortedDictionary<(string Privilege, RunsAs RunsAs, string Roles), (RoleSet Roles, SortedSet<string> In)>(MissingGlobalOrder);

        SectionCoverage Section(IManifestSection manifestSection)
        {
            var id = manifestSection.Id;
            var entryPoint = manifestSection as EntryPoint;
            var applicationUser = ApplicationUserRoles(entryPoint, options, principals);
            var identities = new SortedDictionary<RunsAs, IdentityRoles>();
            var coverage = new Dictionary<Requirement, RequirementCoverage>();
            var globalCoverage = new Dictionary<GlobalRequirement, RequirementCoverage>();

            IdentityRoles IdentityOf(RunsAs identity)
            {
                if (!identities.TryGetValue(identity, out var roles))
                {
                    roles = Roles(identity, entryPoint, options, applicationUser, users);
                    if (roles.Roles is { } set && set != options.Roles && set != options.CallerRoles) usesConfiguredRoles = true;
                    identities.Add(identity, roles);
                }
                return roles;
            }

            foreach (var requirement in manifestSection.Requirements)
            {
                var identity = requirement.RunsAs with { Assumed = false };
                var roles = IdentityOf(identity);
                var result = Check(requirement, roles, notGoverned, options.Suppress);
                coverage.Add(requirement, result);
                if (result.Status == CoverageStatus.Missing) AddMissing(missing, (requirement.Table, requirement.Privilege, identity, roles.Roles!.ToString()), roles.Roles, id);
            }

            foreach (var requirement in manifestSection.GlobalRequirements)
            {
                var identity = requirement.RunsAs with { Assumed = false };
                var roles = IdentityOf(identity);
                var result = Check(requirement, roles, options.Suppress);
                globalCoverage.Add(requirement, result);
                if (result.Status == CoverageStatus.Missing) AddMissing(missingGlobal, (requirement.Privilege, identity, roles.Roles!.ToString()), roles.Roles, id);
            }

            var needsRole = manifestSection.Unresolved.Where(u => u.RunsAs.Kind != RunsAsKind.System).ToList();
            var section = new SectionCoverage(
                coverage,
                globalCoverage,
                identities.Values.Where(i => i.RunsAs.Kind != RunsAsKind.System).ToList(),
                needsRole.Count,
                needsRole.Count(u => RequestReasons.NoRuleMessage(u.Reason) is not null),
                needsRole.Count(u => RequestReasons.IsCustomApi(u.Reason)));
            sections.Add(section);
            return section;
        }

        var entryPoints = new Dictionary<string, SectionCoverage>(StringComparer.Ordinal);
        var unreached = new Dictionary<string, SectionCoverage>(StringComparer.Ordinal);
        foreach (var section in manifest.Sections)
        {
            (section is EntryPoint ? entryPoints : unreached)[section.Id] = Section(section);
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
            sections.Sum(s => s.UnresolvedNoRule),
            sections.Sum(s => s.UnresolvedCustomApi),
            Count(CoverageStatus.Unverifiable));

        var missingPrivileges = missing.Select(m => new MissingPrivilege(m.Key.Table, m.Key.Privilege, m.Key.RunsAs, m.Value.Roles, m.Value.In.ToList())).ToList();
        var missingGlobalPrivileges = missingGlobal.Select(m => new MissingGlobalPrivilege(m.Key.Privilege, m.Key.RunsAs, m.Value.Roles, m.Value.In.ToList())).ToList();
        return new CoverageReport(entryPoints, unreached, summary, missingPrivileges, missingGlobalPrivileges, usesConfiguredRoles);
    }

    private static readonly IComparer<(string Table, Privilege Privilege, RunsAs RunsAs, string Roles)> MissingOrder =
        Comparer<(string Table, Privilege Privilege, RunsAs RunsAs, string Roles)>.Create((x, y) =>
        {
            var result = string.CompareOrdinal(x.Table, y.Table);
            if (result == 0) result = x.Privilege.CompareTo(y.Privilege);
            if (result == 0) result = x.RunsAs.CompareTo(y.RunsAs);
            return result != 0 ? result : string.CompareOrdinal(x.Roles, y.Roles);
        });

    private static readonly IComparer<(string Privilege, RunsAs RunsAs, string Roles)> MissingGlobalOrder =
        Comparer<(string Privilege, RunsAs RunsAs, string Roles)>.Create((x, y) =>
        {
            var result = string.CompareOrdinal(x.Privilege, y.Privilege);
            if (result == 0) result = x.RunsAs.CompareTo(y.RunsAs);
            return result != 0 ? result : string.CompareOrdinal(x.Roles, y.Roles);
        });

    /// <summary>
    /// Records that <paramref name="id"/> needs a privilege <paramref name="roles"/> lack.
    /// </summary>
    private static void AddMissing<TKey>(SortedDictionary<TKey, (RoleSet Roles, SortedSet<string> In)> missing, TKey key, RoleSet roles, string id)
    {
        if (!missing.TryGetValue(key, out var found)) missing.Add(key, found = (roles, new SortedSet<string>(StringComparer.Ordinal)));
        found.In.Add(id);
    }

    private static RequirementCoverage Check(Requirement requirement, IdentityRoles identity, HashSet<string> notGoverned, IReadOnlyList<Suppression> suppress)
    {
        if (requirement.RunsAs.Kind == RunsAsKind.System) return new RequirementCoverage(CoverageStatus.System);
        // No role can grant a table no role governs, whatever roles the identity has
        if (notGoverned.Contains(requirement.Table)) return new RequirementCoverage(CoverageStatus.NotGoverned);
        if (identity.Roles is not { } roles) return new RequirementCoverage(CoverageStatus.NotChecked);
        if (roles.Depth(requirement.Table, requirement.Privilege) is { } depth) return new RequirementCoverage(CoverageStatus.Granted, depth);

        return suppress.FirstOrDefault(s => s.Matches(requirement)) is { } suppression
            ? new RequirementCoverage(CoverageStatus.Suppressed, Reason: suppression.Reason)
            : new RequirementCoverage(CoverageStatus.Missing);
    }

    private static RequirementCoverage Check(GlobalRequirement requirement, IdentityRoles identity, IReadOnlyList<Suppression> suppress)
    {
        if (requirement.RunsAs.Kind == RunsAsKind.System) return new RequirementCoverage(CoverageStatus.System);
        if (identity.Roles is not { } roles) return new RequirementCoverage(CoverageStatus.NotChecked);
        if (!roles.KnowsGlobalPrivileges) return new RequirementCoverage(CoverageStatus.Unverifiable);
        if (roles.GlobalDepth(requirement.Privilege) is { } depth) return new RequirementCoverage(CoverageStatus.Granted, depth);

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

        if (entryPoint.Kind == EntryPointKind.Function && EntryPointIds.FunctionName(entryPoint.Id) is { } name
            && principals.TryGetValue($"{assembly}/{name}", out var function))
        {
            return function;
        }
        return principals.TryGetValue(assembly, out var app) ? app : null;
    }
}
