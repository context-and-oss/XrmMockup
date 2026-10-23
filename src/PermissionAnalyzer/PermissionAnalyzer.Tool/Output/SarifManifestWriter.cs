using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The findings as SARIF 2.1.0, for GitHub code scanning and other tools that annotate code: a result per call site
/// that needs a privilege the roles lack (an error; suppressed ones carry their reason as a suppression), can't be
/// checked (a note), or whose table or request isn't known (a warning, not for SYSTEM). The entry points and methods
/// that reach a call site are its related locations. Without roles, only unresolved operations are findings. A filter
/// leaves out what it doesn't match.
/// </summary>
/// <param name="sourceRoot">The analysis root relative to <c>%SRCROOT%</c>, the repository root, such as
/// <c>src/Functions/</c>, so that locations, which are relative to the analysis root, are relative to the
/// repository too. No absolute path is written, so the output is the same on every machine.</param>
/// <param name="version">The tool version, or <c>null</c> to leave it out.</param>
internal sealed class SarifManifestWriter(ReportFilter? filter = null, string sourceRoot = "", string? version = null) : IManifestWriter
{
    private const string SourceRootId = "%SRCROOT%";

    /// <summary>
    /// The entry points named in a message; the related locations have all of them.
    /// </summary>
    private const int NamedSections = 3;

    private readonly ReportFilter _filter = filter ?? ReportFilter.None;

    // The rules by their index in Rules
    private const int MissingRule = 0, UnresolvedRule = 1, NotCheckedRule = 2, UnverifiableRule = 3;

    private static readonly IReadOnlyList<RuleDocument> Rules =
    [
        new("missing-privilege", "MissingPrivilege",
            new("A security role lacks a privilege the code needs"),
            new("None of the roles the identity is checked against grants a privilege an operation needs, so the operation fails with an access error. Add the privilege to one of the roles, or suppress it in XrmMockup:Permissions:Suppress with a reason."),
            new("error")),
        new("unresolved-operation", "UnresolvedOperation",
            new("The privilege an operation needs isn't known"),
            new("The table of the operation can't be determined statically, or the request has no rule, so the privilege it needs isn't checked. Name the table with a constant or an early-bound type, or add a rule in XrmMockup:Permissions:Rules."),
            new("warning")),
        new("not-checked", "NotChecked",
            new("A privilege isn't checked, as no roles are known for the identity"),
            new("The identity the operation runs as has no roles to check against. Map it to roles with --role, --caller-role, XrmMockup:Permissions:Principals or XrmMockup:Permissions:Users."),
            new("note")),
        new("unverifiable", "Unverifiable",
            new("A privilege not tied to a table can't be checked"),
            new("The role metadata was generated before privileges not tied to a table were downloaded. Regenerate it."),
            new("note")),
    ];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        NewLine = "\n",
    };

    public void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer)
    {
        var results = Findings(manifest, coverage).Concat(Unresolved(manifest))
            .OrderBy(r => r.RuleIndex)
            .ThenBy(r => r.Locations?[0].PhysicalLocation.ArtifactLocation.Uri, StringComparer.Ordinal)
            .ThenBy(r => r.Locations?[0].PhysicalLocation.Region?.StartLine)
            .ThenBy(r => r.Locations?[0].PhysicalLocation.Region?.StartColumn)
            .ThenBy(r => r.Message.Text, StringComparer.Ordinal)
            .ToList();
        var document = new SarifDocument(
            "https://json.schemastore.org/sarif-2.1.0.json",
            "2.1.0",
            [new RunDocument(new ToolDocument(new DriverDocument("xrmmockup-permissions", version, "https://github.com/delegateas/XrmMockup", Rules)), results)]);

        writer.Write(JsonSerializer.Serialize(document, Options));
        writer.Write('\n');
    }

    /// <summary>
    /// The analysis root relative to the repository it is in, the nearest directory up with a <c>.git</c> folder or
    /// file, with forward slashes and a trailing one; empty at the repository root or outside a repository.
    /// </summary>
    public static string RepositoryPath(string root)
    {
        var full = Path.GetFullPath(root);
        for (var directory = new DirectoryInfo(full); directory is not null; directory = directory.Parent)
        {
            var git = Path.Combine(directory.FullName, ".git");
            if (!Directory.Exists(git) && !File.Exists(git)) continue;

            var relative = Path.GetRelativePath(directory.FullName, full).Replace('\\', '/');
            return relative == "." ? "" : relative + "/";
        }
        return "";
    }

    /// <summary>
    /// A result per call site and privilege the roles lack, or can't check, merged over the entry points that reach it.
    /// </summary>
    private IEnumerable<ResultDocument> Findings(Manifest manifest, CoverageReport? coverage)
    {
        if (coverage is null) yield break;

        var findings = new Dictionary<(int Rule, SourceLocation Location, string Privilege, RunsAs RunsAs, string Roles, string? Suppressed), (string Message, Properties Properties, List<Section> In)>();
        var sections = manifest.EntryPoints.Select(e => (Section: new Section(e.Id, e.Declaration), e.Requirements, e.GlobalRequirements, Coverage: coverage.For(e)))
            .Concat(manifest.Unreached.Select(m => (Section: new Section(m.Id, m.Declaration), m.Requirements, m.GlobalRequirements, Coverage: coverage.For(m))));
        foreach (var (section, requirements, globalRequirements, sectionCoverage) in sections)
        {
            var needs = requirements.Where(_filter.Matches)
                .Select(r => (Table: (string?)r.Table, Privilege: r.Privilege.ToString(), r.RunsAs, r.Sources, Result: sectionCoverage.For(r)))
                .Concat(globalRequirements.Where(_filter.Matches)
                    .Select(r => (Table: (string?)null, r.Privilege, r.RunsAs, r.Sources, Result: sectionCoverage.For(r))));
            foreach (var (table, privilege, runsAs, sources, result) in needs)
            {
                var identity = runsAs with { Assumed = false };
                var roles = sectionCoverage.Identities.FirstOrDefault(i => i.RunsAs == identity);
                var what = table is null ? privilege : $"{privilege} on {table}";
                var (rule, message) = result.Status switch
                {
                    CoverageStatus.Missing or CoverageStatus.Suppressed => ((int?)MissingRule, $"{what} is missing from {roles?.Roles}, needed as {identity}"),
                    CoverageStatus.NotChecked => (NotCheckedRule, $"{what} as {identity} isn't checked ({roles?.Note ?? "no roles"})"),
                    CoverageStatus.Unverifiable => (UnverifiableRule, $"{what} can't be checked against {roles?.Roles} for {identity} (the role metadata predates privileges not tied to a table)"),
                    _ => (null, ""),
                };
                if (rule is not { } index) continue;

                var suppressed = result.Status == CoverageStatus.Suppressed ? result.Reason ?? "" : null;
                var rolesText = roles?.Roles?.ToString() ?? "";
                foreach (var location in sources.Select(s => s.Location).Distinct())
                {
                    var key = (index, location, $"{table} {privilege}", identity, rolesText, suppressed);
                    if (!findings.TryGetValue(key, out var finding))
                    {
                        findings.Add(key, finding = (message, new Properties(table, privilege, identity.ToString(), roles?.Roles?.Names), []));
                    }
                    finding.In.Add(section);
                }
            }
        }

        foreach (var ((rule, location, _, _, _, suppressed), (message, properties, @in)) in findings)
        {
            yield return Result(rule, $"{message}, in {Sections(@in)}", location, @in, properties,
                suppressed is null ? null : [new SuppressionDocument("external", suppressed.Length > 0 ? suppressed : null)]);
        }
    }

    /// <summary>
    /// A result per unresolved call site that would need a role, so not those running as SYSTEM.
    /// </summary>
    private IEnumerable<ResultDocument> Unresolved(Manifest manifest) =>
        UnresolvedSites.Of(manifest, _filter).Where(s => s.Operation.RunsAs.Kind != RunsAsKind.System).Select(site =>
        {
            var operation = site.Operation;
            return Result(UnresolvedRule, $"{operation.Operation}{UnresolvedSites.PrivilegeSuffix(operation)} as {operation.RunsAs}: {operation.Reason}, in {Sections(site.In)}", operation.Source.Location, site.In,
                new Properties(null, operation.Privileges.Count > 0 ? string.Join(", ", operation.Privileges) : null, operation.RunsAs.ToString(), null), null);
        });

    private ResultDocument Result(int rule, string message, SourceLocation location, IReadOnlyList<Section> sections, Properties properties, IReadOnlyList<SuppressionDocument>? suppressions)
    {
        var related = sections
            .Where(s => s.Declaration is { Line: > 0 })
            .Select((s, i) => new RelatedLocationDocument(i, Physical(s.Declaration!), new MessageDocument(s.Id)))
            .ToList();
        // A location outside the code, such as --operation, isn't a location in a file, so the entry point is used
        var primary = location.Line > 0 ? Physical(location) : related.FirstOrDefault()?.PhysicalLocation;
        return new ResultDocument(
            Rules[rule].Id,
            rule,
            Rules[rule].DefaultConfiguration.Level,
            new MessageDocument(message),
            primary is null ? null : [new LocationDocument(primary)],
            related.Count > 0 ? related : null,
            suppressions,
            properties with { EntryPoints = sections.Select(s => s.Id).ToList() });
    }

    private static string Sections(IReadOnlyList<Section> sections)
    {
        var named = string.Join(", ", sections.Take(NamedSections).Select(s => s.Id));
        return sections.Count > NamedSections ? $"{named} and {sections.Count - NamedSections} more" : named;
    }

    private PhysicalLocationDocument Physical(SourceLocation location) =>
        new(Artifact(location.File), new RegionDocument(location.Line, location.Column));

    /// <summary>
    /// The file relative to <c>%SRCROOT%</c>, with <c>../</c> resolved; a file on another drive keeps its full path,
    /// as a <c>file</c> URI.
    /// </summary>
    private ArtifactLocationDocument Artifact(string file)
    {
        if (file.StartsWith('/') || (file.Length > 1 && file[1] == ':'))
        {
            return new ArtifactLocationDocument("file:///" + Escape(file.TrimStart('/').Split('/')), null);
        }

        var segments = new List<string>();
        foreach (var segment in (sourceRoot + file).Split('/'))
        {
            if (segment is "" or ".") continue;
            if (segment == ".." && segments.Count > 0 && segments[^1] != "..") segments.RemoveAt(segments.Count - 1);
            else segments.Add(segment);
        }
        return new ArtifactLocationDocument(Escape(segments), SourceRootId);
    }

    private static string Escape(IEnumerable<string> segments) =>
        string.Join("/", segments.Select(s => s.EndsWith(':') ? s : Uri.EscapeDataString(s)));

    private sealed record SarifDocument(
        [property: JsonPropertyName("$schema")] string Schema,
        string Version,
        IReadOnlyList<RunDocument> Runs);

    private sealed record RunDocument(ToolDocument Tool, IReadOnlyList<ResultDocument> Results);

    private sealed record ToolDocument(DriverDocument Driver);

    private sealed record DriverDocument(string Name, string? Version, string InformationUri, IReadOnlyList<RuleDocument> Rules);

    private sealed record RuleDocument(string Id, string Name, MessageDocument ShortDescription, MessageDocument FullDescription, ConfigurationDocument DefaultConfiguration);

    private sealed record ConfigurationDocument(string Level);

    private sealed record ResultDocument(
        string RuleId,
        int RuleIndex,
        string Level,
        MessageDocument Message,
        IReadOnlyList<LocationDocument>? Locations,
        IReadOnlyList<RelatedLocationDocument>? RelatedLocations,
        IReadOnlyList<SuppressionDocument>? Suppressions,
        Properties Properties);

    private sealed record MessageDocument(string Text);

    private sealed record LocationDocument(PhysicalLocationDocument PhysicalLocation);

    private sealed record RelatedLocationDocument(int Id, PhysicalLocationDocument PhysicalLocation, MessageDocument Message);

    private sealed record PhysicalLocationDocument(ArtifactLocationDocument ArtifactLocation, RegionDocument? Region);

    private sealed record ArtifactLocationDocument(string Uri, string? UriBaseId);

    private sealed record RegionDocument(int StartLine, int StartColumn);

    private sealed record SuppressionDocument(string Kind, string? Justification);

    /// <param name="Table">The table, or <c>null</c> for a privilege not tied to a table or an unresolved operation.</param>
    /// <param name="Privilege">The privilege, or the privileges of an unresolved operation.</param>
    /// <param name="Roles">The roles checked against.</param>
    /// <param name="EntryPoints">Ids of the entry points and unreached methods that reach the call site.</param>
    private sealed record Properties(string? Table, string? Privilege, string RunsAs, IReadOnlyList<string>? Roles, IReadOnlyList<string>? EntryPoints = null);
}
