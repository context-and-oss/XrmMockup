using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest for reading in a terminal: requirements per entry point, the methods no entry point reaches,
/// then the privileges per table.
/// </summary>
internal sealed class TextManifestWriter : IManifestWriter
{
    public void Write(Manifest manifest, TextWriter writer)
    {
        var requirements = manifest.AllRequirements.ToList();
        var tableWidth = requirements.Select(r => r.Table.Length).DefaultIfEmpty(0).Max();
        var privilegeWidth = Enum.GetNames<Privilege>().Max(n => n.Length);
        var runsAsWidth = requirements.Select(r => r.RunsAs.ToString().Length).DefaultIfEmpty(0).Max();

        void Section(string header, IReadOnlyList<Requirement> sectionRequirements, IReadOnlyList<UnresolvedOperation> unresolved, IReadOnlyList<Assumption> assumptions)
        {
            writer.Write(header + "\n");
            foreach (var requirement in sectionRequirements)
            {
                var basis = requirement.Basis == RequirementBasis.Direct ? "" : $"  ({Kebab(requirement.Basis)})";
                var sources = string.Join(", ", requirement.Sources.Select(Source));
                writer.Write(
                    $"  {requirement.Table.PadRight(tableWidth)}  {requirement.Privilege.ToString().PadRight(privilegeWidth)}  " +
                    $"{requirement.RunsAs.ToString().PadRight(runsAsWidth)}  {sources}{basis}\n");
            }

            foreach (var operation in unresolved)
            {
                var privileges = operation.Privileges.Count > 0 ? $" [{string.Join(", ", operation.Privileges)}]" : "";
                writer.Write($"  ? {operation.Operation}{privileges} as {operation.RunsAs}: {operation.Reason}  {Source(operation.Source)}\n");
            }

            foreach (var assumption in assumptions)
            {
                writer.Write($"  ~ {assumption.Reason}  {Source(assumption.Source)}\n");
            }
            writer.Write('\n');
        }

        foreach (var entryPoint in manifest.EntryPoints)
        {
            var header = entryPoint.Declaration is { } declaration ? $"{entryPoint.Id}  {declaration}" : entryPoint.Id;
            if (entryPoint.ImpersonatingUserId is { } user) header += $"  (runs as user {user})";
            Section(header, entryPoint.Requirements, entryPoint.Unresolved, entryPoint.Assumptions);
        }

        if (manifest.Unreached.Count > 0)
        {
            writer.Write("Not reached from any entry point\n\n");
            foreach (var method in manifest.Unreached)
            {
                var header = method.Declaration is { } declaration ? $"{method.Id}  {declaration}" : method.Id;
                Section(header, method.Requirements, method.Unresolved, method.Assumptions);
            }
        }

        var byTable = requirements
            .GroupBy(r => r.Table)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();
        if (byTable.Count > 0)
        {
            writer.Write("Privileges per table\n");
            foreach (var table in byTable)
            {
                var privileges = table.Select(r => r.Privilege).Distinct().OrderBy(p => p);
                writer.Write($"  {table.Key.PadRight(tableWidth)}  {string.Join(", ", privileges)}\n");
            }
            writer.Write('\n');
        }

        if (manifest.NotRoleGoverned is { Count: > 0 } notGoverned)
        {
            writer.Write($"Not in any security role, so XrmMockup allows every operation: {string.Join(", ", notGoverned)}\n\n");
        }

        var unresolvedCount = manifest.EntryPoints.Sum(e => e.Unresolved.Count) + manifest.Unreached.Sum(m => m.Unresolved.Count);
        writer.Write(
            $"{Count(manifest.EntryPoints.Count, "entry point")}, {manifest.Unreached.Count} unreached, {Count(byTable.Count, "table")}, " +
            $"{Count(requirements.Count, "requirement")}, {unresolvedCount} unresolved\n");
    }

    /// <summary>
    /// A source location, with the call path when the operation is in a called method.
    /// </summary>
    private static string Source(RequirementSource source) =>
        source.Path.Count > 1 ? $"{source.Location} ({string.Join(" → ", source.Path)})" : source.Location.ToString();

    private static string Kebab(RequirementBasis basis) => basis switch
    {
        RequirementBasis.AllImplementations => "all-implementations",
        _ => basis.ToString().ToLowerInvariant(),
    };

    private static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
