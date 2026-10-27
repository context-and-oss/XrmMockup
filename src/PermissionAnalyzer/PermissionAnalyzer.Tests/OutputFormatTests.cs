using System.CommandLine;
using System.Text.Json;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// The Markdown report for pull requests and the SARIF findings for code scanning.
/// </summary>
public class OutputFormatTests
{
    private static readonly RoleDefinition Salesperson = new("Salesperson", new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>>
    {
        ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Read] = PrivilegeDepth.User },
    });

    /// <summary>
    /// An account Update step that reads an account (granted), creates a contact (missing) and deletes one as SYSTEM,
    /// and methods nothing calls that delete an account (missing) and a record of an unknown table.
    /// </summary>
    private const string Source = """
        public class P : Plugin
        {
            public P()
            {
                RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, Execute);
            }

            private void Execute(LocalPluginContext context)
            {
                context.OrganizationService.Retrieve("account", Guid.Empty, new ColumnSet());
                context.OrganizationService.Create(new Contact());
                context.OrganizationAdminService.Delete("contact", Guid.Empty);
            }
        }

        public class Purger
        {
            public void Purge(IOrganizationService service) => service.Delete("account", Guid.Empty);

            public void Remove(IOrganizationService service, string table) => service.Delete(table, Guid.Empty);
        }
        """;

    private const string UpdateStep = "plugin:P:Update:account:PostOperation";

    private static string Write(IManifestWriter writer, RoleDefinition? role = null, IReadOnlyList<Suppression>? suppress = null, string file = "Code.cs", string source = Source)
    {
        var builder = new ManifestBuilder(Analyze.Root);
        Assert.True(builder.Add(Analyze.Compile(source, file: file)));
        var manifest = builder.Build(["account", "contact"]);
        var coverage = role is null ? null : CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([role], "--role"), Suppress = suppress ?? [] });
        using var output = new StringWriter();
        writer.Write(manifest, coverage, output);
        return output.ToString();
    }

    private static JsonElement Results(string sarif) => JsonDocument.Parse(sarif).RootElement.GetProperty("runs")[0].GetProperty("results");

    private static string Uri(JsonElement location) => location.GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString()!;

    [Fact]
    public void MarkdownHasATablePerRoleSetWithEntryPointsAndUnresolvedFolded()
    {
        var markdown = Write(new MarkdownManifestWriter(), Salesperson);

        Assert.StartsWith("### ✘ 2 missing privileges\n\nResult: 1 granted, 2 missing, 1 as SYSTEM\n\n> [!WARNING]\n> Not checked: 1 unresolved operation", markdown);
        Assert.Contains("#### Missing from Salesperson (--role), for Caller\n\n| Table | Privileges |\n| --- | --- |\n| `account` | Delete |\n| `contact` | Create |\n\n", markdown);
        Assert.Contains($"<details><summary>By entry point</summary>\n\n- `{UpdateStep}`\n\n| Table | Privileges |\n| --- | --- |\n| `contact` | Create |\n\n- `Purger.Purge(IOrganizationService)`\n", markdown);
        Assert.Contains("<details><summary>1 unresolved operation</summary>\n\n| Operation | Runs as | Reason | Source |\n| --- | --- | --- | --- |\n| `Delete` [Delete] | Caller", markdown);
        Assert.EndsWith("1 unresolved\n", markdown);
    }

    [Fact]
    public void MarkdownWithoutRolesListsWhatEachIdentityNeeds()
    {
        var markdown = Write(new MarkdownManifestWriter());

        Assert.StartsWith("### Required privileges\n\n#### Caller needs\n\n", markdown);
        Assert.DoesNotContain("Result:", markdown);
        Assert.DoesNotContain("System needs", markdown);
    }

    [Fact]
    public void MarkdownEscapesNamesAndIsNarrowedByTheFilter()
    {
        var role = new RoleDefinition("Sales | *Ops*", Salesperson.Privileges);
        var markdown = Write(new MarkdownManifestWriter(new ReportFilter(["contact"], [], [], [])), role);

        Assert.Contains(@"#### Missing from Sales \| \*Ops\* (--role), for Caller", markdown);
        Assert.DoesNotContain("| `account` |", markdown);
        // The counts are of the whole analysis
        Assert.StartsWith("### ✘ 2 missing privileges", markdown);
        Assert.EndsWith("Filtered by --table contact: 2 of 4 requirements match\n", markdown);
    }

    [Fact]
    public void MarkdownHeadingCountsEachMissingPrivilegeOnce()
    {
        var markdown = Write(new MarkdownManifestWriter(), Salesperson, source: """
            public class Importer
            {
                public void First(IOrganizationService service) => service.Create(new Contact());

                public void Second(IOrganizationService service) => service.Create(new Contact());
            }
            """);

        Assert.StartsWith("### ✘ 1 missing privilege\n\nResult: 0 granted, 2 missing", markdown);
    }

    [Theory]
    [InlineData("a|b", @"`a\|b`")]
    [InlineData("a`b", "`` a`b ``")]
    [InlineData("``a`", "``` ``a` ```")]
    [InlineData("a\n| x |\r\nb", @"`a \| x \|  b`")]
    public void MarkdownCodeSpansStayOnOneLineAndInTheirCell(string text, string expected)
    {
        Assert.Equal(expected, MarkdownManifestWriter.Code(text));
    }

    [Fact]
    public void MarkdownTextHasNoLineBreaks()
    {
        Assert.Equal(@"Sales \# Ops", MarkdownManifestWriter.Text("Sales\n# Ops"));
    }

    [Fact]
    public void SarifHasAResultPerCallSiteWithItsEntryPoints()
    {
        var results = Results(Write(new SarifManifestWriter(), Salesperson));

        Assert.Equal(
            ["missing-privilege Create on contact", "missing-privilege Delete on account", "unresolved-operation Delete [Delete]"],
            results.EnumerateArray().Select(r => $"{r.GetProperty("ruleId").GetString()} {r.GetProperty("message").GetProperty("text").GetString()!.Split(" is ")[0].Split(" as ")[0]}"));
        // Sorted by location
        var create = results[0];
        Assert.Equal("error", create.GetProperty("level").GetString());
        Assert.Equal($"Create on contact is missing from Salesperson (--role), needed as Caller, in {UpdateStep}", create.GetProperty("message").GetProperty("text").GetString());
        var location = create.GetProperty("locations")[0];
        Assert.Equal("Code.cs", Uri(location));
        Assert.Equal("%SRCROOT%", location.GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uriBaseId").GetString());
        var related = Assert.Single(create.GetProperty("relatedLocations").EnumerateArray());
        Assert.Equal(UpdateStep, related.GetProperty("message").GetProperty("text").GetString());
        Assert.Equal("contact", create.GetProperty("properties").GetProperty("table").GetString());
        Assert.Equal("warning", results[2].GetProperty("level").GetString());
    }

    [Fact]
    public void SarifSuppressesSuppressedPrivilegesWithTheirReason()
    {
        var results = Results(Write(new SarifManifestWriter(), Salesperson, [new Suppression("contact", Privilege.Create, "Created by a flow")]));

        var suppression = Assert.Single(results.EnumerateArray(), r => r.TryGetProperty("suppressions", out _)).GetProperty("suppressions")[0];
        Assert.Equal("external", suppression.GetProperty("kind").GetString());
        Assert.Equal("Created by a flow", suppression.GetProperty("justification").GetString());
    }

    [Fact]
    public void SarifWithoutRolesHasOnlyUnresolvedOperations()
    {
        var result = Assert.Single(Results(Write(new SarifManifestWriter())).EnumerateArray());
        Assert.Equal("unresolved-operation", result.GetProperty("ruleId").GetString());
    }

    [Fact]
    public void SarifLocationsAreRelativeToTheRepository()
    {
        // The code is next to the analysis root, which is src/Functions in the repository
        var results = Results(Write(new SarifManifestWriter(sourceRoot: "src/Functions/"), Salesperson, file: "../Plugins/Code.cs"));

        Assert.Equal("src/Plugins/Code.cs", Uri(results[0].GetProperty("locations")[0]));
        Assert.DoesNotContain(results.EnumerateArray(), r => Uri(r.GetProperty("locations")[0]).Contains(".."));
    }

    [Fact]
    public void RepositoryPathIsTheRootBelowTheNearestGitFolder()
    {
        var repository = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(repository.FullName, ".git"));
            var root = Directory.CreateDirectory(Path.Combine(repository.FullName, "src", "Functions"));

            Assert.Equal("src/Functions/", SarifManifestWriter.RepositoryPath(root.FullName));
            Assert.Equal("", SarifManifestWriter.RepositoryPath(repository.FullName));
        }
        finally
        {
            repository.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("markdown")]
    [InlineData("sarif")]
    public void FormatAcceptsMarkdownAndSarif(string format)
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);
        var result = command.Parse(["--format", format]);

        Assert.Empty(result.Errors);
        Assert.Equal(format, options.Read(result).Format.ToString().ToLowerInvariant());
    }
}
