using System.Text.RegularExpressions;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The report as GitHub-flavored Markdown, for a pull request comment or a job summary: the result, the privileges to
/// change per role set (<see cref="RoleChanges"/>) as a table, with the entry points that need them and the
/// unresolved operations folded away, then the counts. A filter narrows the changes and the unresolved operations; the
/// counts and the result stay those of the whole analysis. There are no verbosity levels: the folded sections hold
/// what <see cref="Verbosity.Compact"/> adds to the text report.
/// </summary>
internal sealed partial class MarkdownManifestWriter(ReportFilter? filter = null) : IManifestWriter
{
    private readonly ReportFilter _filter = filter ?? ReportFilter.None;

    public void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer)
    {
        if (coverage is null)
        {
            writer.Write("### Required privileges\n\n");
        }
        else
        {
            var missing = coverage.Summary.Missing;
            writer.Write(missing > 0 ? $"### ✘ {ReportText.Count(missing, "missing privilege")}\n\n" : "### ✔ No missing privileges\n\n");
            writer.Write(ReportText.Result(coverage.Summary) + "\n\n");
            if (ReportText.NotChecked(coverage.Summary) is { } notChecked) writer.Write($"> [!WARNING]\n> {Text(notChecked)}\n\n");
        }

        foreach (var block in RoleChanges.Of(manifest, coverage, _filter))
        {
            writer.Write($"#### {Text(block.Heading)}\n\n");
            Tables(block.Tables, writer);
            writer.Write("<details><summary>By entry point</summary>\n\n");
            foreach (var group in block.EntryPoints)
            {
                foreach (var id in group.Ids)
                {
                    writer.Write($"- {Code(id)}\n");
                }
                writer.Write('\n');
                Tables(group.Tables, writer);
            }
            writer.Write("</details>\n\n");
        }

        var unresolved = UnresolvedSites.Of(manifest, _filter);
        if (unresolved.Count > 0)
        {
            writer.Write($"<details><summary>{ReportText.Count(unresolved.Count, "unresolved operation")}</summary>\n\n");
            writer.Write("| Operation | Runs as | Reason | Source |\n| --- | --- | --- | --- |\n");
            foreach (var site in unresolved)
            {
                var operation = site.Operation;
                writer.Write($"| {Code(operation.Operation)}{UnresolvedSites.PrivilegeSuffix(operation)} | {operation.RunsAs} | {Text(operation.Reason)} | {Code(operation.Source.Location.ToString())} |\n");
            }
            writer.Write("\n</details>\n\n");
        }

        writer.Write(ReportText.Totals(manifest) + "\n");
        if (!_filter.IsEmpty) writer.Write($"\n{Text(ReportText.Filtered(manifest, _filter))}\n");
    }

    /// <summary>
    /// A row per table, privileges not tied to a table last.
    /// </summary>
    private static void Tables(IReadOnlyList<TableChange> tables, TextWriter writer)
    {
        writer.Write("| Table | Privileges |\n| --- | --- |\n");
        foreach (var table in tables)
        {
            var privileges = table.IsGlobal ? "not tied to a table" : string.Join(", ", table.Privileges);
            writer.Write($"| {Code(table.Table)} | {privileges} |\n");
        }
        writer.Write('\n');
    }

    /// <summary>
    /// Text with the characters Markdown would format escaped, so a role or reason shows as written, also in a table.
    /// </summary>
    private static string Text(string text) => MarkdownCharacters().Replace(text, @"\$0");

    /// <summary>
    /// A code span that also works in a table cell.
    /// </summary>
    private static string Code(string text)
    {
        var escaped = text.Replace("|", @"\|");
        return text.Contains('`') ? $"`` {escaped} ``" : $"`{escaped}`";
    }

    [GeneratedRegex(@"[\\`*_{}\[\]<>#|~]")]
    private static partial Regex MarkdownCharacters();
}
