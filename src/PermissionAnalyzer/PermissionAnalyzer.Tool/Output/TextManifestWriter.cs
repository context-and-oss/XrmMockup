using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest for reading in a terminal: requirements per method, then the privileges per table.
/// </summary>
internal sealed class TextManifestWriter : IManifestWriter
{
    public void Write(Manifest manifest, TextWriter writer)
    {
        var requirements = manifest.Methods.SelectMany(m => m.Requirements).ToList();
        var tableWidth = requirements.Select(r => r.Table.Length).DefaultIfEmpty(0).Max();
        var privilegeWidth = Enum.GetNames<Privilege>().Max(n => n.Length);

        foreach (var method in manifest.Methods)
        {
            writer.Write(method.Declaration is { } declaration ? $"{method.Id}  {declaration}\n" : $"{method.Id}\n");

            foreach (var requirement in method.Requirements)
            {
                var sources = string.Join(", ", requirement.Sources);
                writer.Write($"  {requirement.Table.PadRight(tableWidth)}  {requirement.Privilege.ToString().PadRight(privilegeWidth)}  {sources}\n");
            }

            foreach (var unresolved in method.Unresolved)
            {
                var privileges = unresolved.Privileges.Count > 0 ? $" [{string.Join(", ", unresolved.Privileges)}]" : "";
                writer.Write($"  ? {unresolved.Operation}{privileges}: {unresolved.Reason}  {unresolved.Source}\n");
            }

            writer.Write('\n');
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

        var unresolvedCount = manifest.Methods.Sum(m => m.Unresolved.Count);
        writer.Write(
            $"{Count(manifest.Methods.Count, "method")}, {Count(byTable.Count, "table")}, " +
            $"{Count(requirements.Count, "requirement")}, {unresolvedCount} unresolved\n");
    }

    private static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
