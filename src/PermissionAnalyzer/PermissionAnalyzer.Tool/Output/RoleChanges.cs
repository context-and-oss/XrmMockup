using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The privileges to change, in a block per set of roles: what the roles lack, merged over every entry point and
/// identity they serve, since a role is changed once whatever needs it. Requirements that can't be checked get a block
/// per identity, and without roles, a block is every requirement of an identity that needs a role. Each block also
/// has the entry points and methods that need its privileges, those that need the same ones together. The text
/// report shows the blocks below <see cref="Verbosity.Normal"/>, and Markdown always does.
/// </summary>
internal static class RoleChanges
{
    public static IReadOnlyList<RoleChange> Of(Manifest manifest, CoverageReport? coverage, ReportFilter filter)
    {
        var blocks = new Dictionary<(BlockKind Kind, string Key), Block>();
        // Entry points before unreached methods, each in manifest order
        var position = manifest.Sections.Select((s, i) => (s.Id, i)).ToDictionary(s => s.Id, s => s.i, StringComparer.Ordinal);
        foreach (var section in manifest.Sections)
        {
            var sectionCoverage = coverage?.For(section);
            foreach (var privilegeNeed in PrivilegeNeed.Of(section, sectionCoverage, filter))
            {
                var identity = privilegeNeed.RunsAs with { Assumed = false };
                if (BlockOf(identity, privilegeNeed.Coverage, sectionCoverage) is not { } key) continue;
                var need = new Need(privilegeNeed.Name, privilegeNeed.TablePrivilege);
                if (!blocks.TryGetValue(key, out var block)) blocks.Add(key, block = new Block());
                block.Identities.Add(identity);
                block.Needs.Add(need);
                if (!block.Sections.TryGetValue(section.Id, out var sectionNeeds)) block.Sections.Add(section.Id, sectionNeeds = []);
                sectionNeeds.Add(need);
            }
        }

        return blocks.OrderBy(b => b.Key.Kind).ThenBy(b => b.Key.Key, StringComparer.Ordinal).Select(b =>
        {
            var ((kind, key), block) = (b.Key, b.Value);
            var identities = string.Join(", ", block.Identities.Order());
            var heading = kind switch
            {
                BlockKind.Missing when coverage is null => $"{key} needs",
                BlockKind.Missing => $"Missing from {key}, for {identities}",
                BlockKind.NotChecked => key,
                _ => $"Can't be checked against {key}, for {identities} (the role metadata predates privileges not tied to a table; regenerate it)",
            };
            // Sections that need exactly the same privileges are one group, placed where its first section is
            var groups = block.Sections
                .GroupBy(s => string.Join("\n", s.Value.Select(n => $"{n.Name} {n.Privilege?.ToString() ?? ""}").Order(StringComparer.Ordinal)), StringComparer.Ordinal)
                .Select(g => new EntryPointChange(g.Select(s => s.Key).OrderBy(id => position[id]).ToList(), Tables(g.First().Value)))
                .OrderBy(g => position[g.Ids[0]])
                .ToList();
            return new RoleChange(heading, kind == BlockKind.Missing && coverage is not null, Tables(block.Needs), groups);
        }).ToList();
    }

    /// <summary>
    /// A line per table, so a role can be changed one table at a time, with the privileges in the order the role
    /// editor has them; privileges not tied to a table come last.
    /// </summary>
    private static IReadOnlyList<TableChange> Tables(IEnumerable<Need> needs) => needs
        .GroupBy(n => n.Name, StringComparer.Ordinal)
        .Select(t => new TableChange(t.Key, t.Where(n => n.Privilege is not null).Select(n => n.Privilege!.Value).Distinct().Order().Select(p => p.ToString()).ToList(), t.Any(n => n.Privilege is null)))
        .OrderBy(t => t.IsGlobal)
        .ThenBy(t => t.Table, StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// The block a requirement goes in, by kind and key: missing from a role set, or without roles needed by an
    /// identity, not checked for an identity, or unverifiable against a role set; <c>null</c> when it needs no change.
    /// </summary>
    private static (BlockKind Kind, string Key)? BlockOf(RunsAs identity, RequirementCoverage? result, SectionCoverage? coverage)
    {
        var roles = coverage?.Identities.FirstOrDefault(i => i.RunsAs == identity);
        return result?.Status switch
        {
            null => identity.Kind == RunsAsKind.System ? null : (BlockKind.Missing, identity.ToString()),
            CoverageStatus.Missing => (BlockKind.Missing, roles!.Roles!.ToString()),
            CoverageStatus.NotChecked => (BlockKind.NotChecked, $"{identity} needs ({roles?.Note ?? "not checked"})"),
            CoverageStatus.Unverifiable => (BlockKind.Unverifiable, roles!.Roles!.ToString()),
            _ => null,
        };
    }

    /// <summary>
    /// The kinds of block, in the order they are shown.
    /// </summary>
    private enum BlockKind
    {
        Missing,
        NotChecked,
        Unverifiable,
    }

    /// <summary>
    /// What one block lacks: in all, and per entry point or unreached method.
    /// </summary>
    private sealed class Block
    {
        public HashSet<RunsAs> Identities { get; } = [];

        public HashSet<Need> Needs { get; } = [];

        public Dictionary<string, HashSet<Need>> Sections { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// A privilege on a table, or a privilege not tied to a table by its name, with no <paramref name="Privilege"/>.
    /// </summary>
    private sealed record Need(string Name, Privilege? Privilege);
}

/// <param name="Heading">What the block is, such as <c>Missing from Salesperson (--role), for Caller</c>.</param>
/// <param name="Missing">The roles lack the privileges, rather than they weren't checked or are needed without roles.</param>
/// <param name="Tables">Every table of the block.</param>
/// <param name="EntryPoints">The entry points and unreached methods that need the privileges, grouped.</param>
internal sealed record RoleChange(string Heading, bool Missing, IReadOnlyList<TableChange> Tables, IReadOnlyList<EntryPointChange> EntryPoints);

/// <param name="Table">The table, or the name of a privilege not tied to a table.</param>
/// <param name="Privileges">In role editor order; empty for a privilege not tied to a table.</param>
/// <param name="IsGlobal"><paramref name="Table"/> is a privilege not tied to a table.</param>
internal sealed record TableChange(string Table, IReadOnlyList<string> Privileges, bool IsGlobal);

/// <param name="Ids">Entry points and unreached methods that need exactly <paramref name="Tables"/>.</param>
internal sealed record EntryPointChange(IReadOnlyList<string> Ids, IReadOnlyList<TableChange> Tables);
