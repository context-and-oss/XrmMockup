using System.CommandLine;
using System.Text.RegularExpressions;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// How much of the report is shown, and filters that narrow it, applied after analysis.
/// </summary>
public class ReportTests
{
    private static readonly RoleDefinition Salesperson = new("Salesperson", new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>>
    {
        ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Read] = PrivilegeDepth.User },
    });

    /// <summary>
    /// An account Update step that reads an account (granted), creates a contact through a store (missing) and
    /// deletes one as SYSTEM, a contact Create step that only reads an account, and a method nothing calls that
    /// deletes an account (missing).
    /// </summary>
    private const string Source = """
        public class P : Plugin
        {
            public P()
            {
                RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, Execute);
                RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Retrieve("account", Guid.Empty, new ColumnSet()));
            }

            private void Execute(LocalPluginContext context)
            {
                context.OrganizationService.Retrieve("account", Guid.Empty, new ColumnSet());
                new Store().Save(context.OrganizationService);
                context.OrganizationAdminService.Delete("contact", Guid.Empty);
            }
        }

        public class Store
        {
            public void Save(IOrganizationService service) => service.Create(new Contact());
        }

        public class Purger
        {
            public void Purge(IOrganizationService service) => service.Delete("account", Guid.Empty);
        }
        """;

    private const string UpdateStep = "plugin:P:Update:account:PostOperation";
    private const string CreateStep = "plugin:P:Create:contact:PostOperation";

    private static string Text(ReportOptions options, bool roles = true)
    {
        var manifest = Analyze.Code(Source, ["account", "contact"]);
        var coverage = roles ? CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") }) : null;
        using var output = new StringWriter();
        new TextManifestWriter(options).Write(manifest, coverage, output);
        return output.ToString();
    }

    private static ReportFilter Filter(string[]? tables = null, string[]? privileges = null, RunsAsKind[]? identities = null, RequirementBasis[]? bases = null) =>
        new(tables ?? [], privileges ?? [], identities ?? [], bases ?? []);

    private static string[] Lines(string text) => text.Split('\n');

    /// <summary>
    /// The line is a requirement row matching <paramref name="pattern"/>, whose columns are padded to the widest value.
    /// </summary>
    private static bool Row(string line, string pattern) => Regex.IsMatch(line, $"^  {pattern}$");

    [Fact]
    public void NormalShowsOnlyWhatNeedsAttention()
    {
        var lines = Lines(Text(new ReportOptions(Detail.Normal)));

        Assert.Contains(lines, l => Row(l, "✘ contact +Create +Caller +missing +Code.cs:36"));
        Assert.Contains(lines, l => Row(l, @"✘ account +Delete +Caller \(assumed\) +missing +Code.cs:41"));
        Assert.DoesNotContain(lines, l => l.StartsWith("  ✔ ", StringComparison.Ordinal) || l.StartsWith("  · ", StringComparison.Ordinal));
        // The Create step needs nothing the roles don't grant
        Assert.DoesNotContain(lines, l => l.StartsWith(CreateStep, StringComparison.Ordinal));
        Assert.DoesNotContain("Privileges per table", lines);
        Assert.DoesNotContain(lines, l => l.Contains(" → ", StringComparison.Ordinal));
        Assert.Contains("Result: 2 granted, 2 missing, 1 as SYSTEM", lines);
        Assert.Contains("Missing privileges:", lines);
        Assert.Equal("Not shown: 3 requirements with nothing to fix, 1 assumption. --detail detailed lists them, full also adds every source and call path", lines[^2]);
    }

    [Fact]
    public void DetailedAddsGrantedRequirementsButNotCallPaths()
    {
        var lines = Lines(Text(new ReportOptions(Detail.Detailed)));

        Assert.Contains(lines, l => Row(l, "✔ account +Read +Caller +User +Code.cs:23, Code.cs:28"));
        Assert.Contains(lines, l => Row(l, "· contact +Delete +System +SYSTEM +Code.cs:30"));
        Assert.Contains("  ~ service comes from parameter 'service'  Code.cs:41", lines);
        Assert.Contains(lines, l => l.StartsWith(CreateStep, StringComparison.Ordinal));
        Assert.Contains("Privileges per table", lines);
        Assert.DoesNotContain(lines, l => l.Contains(" → ", StringComparison.Ordinal));
        Assert.Equal("Not shown: every source and call path. --detail full adds them", lines[^2]);
    }

    [Fact]
    public void MinimalIsTheDefaultAndListsWhatEachRoleSetLacks()
    {
        var lines = Lines(Text(ReportOptions.Default));

        Assert.Equal(Detail.Minimal, ReportOptions.Default.Detail);
        Assert.Equal(
            [
                "Missing from Salesperson (--role), for Caller:",
                "  account  Delete",
                "  contact  Create",
                "",
            ],
            lines.Take(4));
        Assert.DoesNotContain(lines, l => l.Contains(UpdateStep, StringComparison.Ordinal) || l.Contains("Code.cs", StringComparison.Ordinal));
        Assert.DoesNotContain("Missing privileges:", lines);
        Assert.Contains("Result: 2 granted, 2 missing, 1 as SYSTEM", lines);
        Assert.Equal("Not shown: entry points, sources and requirements with nothing to fix. --detail compact adds the entry points, normal the sources, detailed everything", lines[^2]);
    }

    [Fact]
    public void CompactAddsTheEntryPointsThatNeedEachPrivilege()
    {
        var lines = Lines(Text(new ReportOptions(Detail.Compact)));

        Assert.Equal(
            [
                "Missing from Salesperson (--role), for Caller:",
                "  account  Delete",
                "  contact  Create",
                "",
                "  By entry point:",
                "",
                "    " + UpdateStep,
                "      contact  Create",
                "",
                "    Purger.Purge(IOrganizationService)",
                "      account  Delete",
                "",
            ],
            lines.Take(12));
        Assert.Equal("Not shown: sources and requirements with nothing to fix. --detail normal adds the sources, detailed everything", lines[^2]);
    }

    [Fact]
    public void CompactListsEntryPointsThatNeedTheSameTogether()
    {
        const string source = """
            public class P : Plugin
            {
                public P()
                {
                    RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, Execute);
                    RegisterPluginStep<Contact>(EventOperation.Update, ExecutionStage.PostOperation, Execute);
                    RegisterPluginStep<Contact>(EventOperation.Delete, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("lead", Guid.Empty));
                }

                private void Execute(LocalPluginContext context)
                {
                    context.OrganizationService.Delete("account", Guid.Empty);
                    context.OrganizationService.Delete("contact", Guid.Empty);
                    context.OrganizationService.Create(new Entity("lead"));
                }
            }
            """;
        // Deleting a contact fires the contact Delete step, so both Update steps also need Delete on lead
        var manifest = Analyze.Code(source, ["account", "contact", "lead"]);
        using var output = new StringWriter();
        new TextManifestWriter(new ReportOptions(Detail.Compact)).Write(manifest, null, output);

        Assert.Equal(
            [
                "Caller needs:",
                "  account  Delete",
                "  contact  Delete",
                "  lead     Create, Delete",
                "",
                "  By entry point:",
                "",
                "    plugin:P:Delete:contact:PostOperation",
                "      lead  Delete",
                "",
                "    plugin:P:Update:account:PostOperation",
                "    plugin:P:Update:contact:PostOperation",
                "      account  Delete",
                "      contact  Delete",
                "      lead     Create, Delete",
                "",
            ],
            Lines(output.ToString()).Take(16));
    }

    [Fact]
    public void FullShowsCallPathsAndNothingIsLeftOut()
    {
        var text = Text(new ReportOptions(Detail.Full));

        Assert.Contains("P.Execute → Store.Save", text);
        Assert.DoesNotContain("Not shown", text);
    }

    [Fact]
    public void WithoutRolesMinimalOnlyLeavesOutSystem()
    {
        var lines = Lines(Text(ReportOptions.Default, roles: false));

        Assert.Equal("Not shown: entry points, sources and what runs as SYSTEM. --detail compact adds the entry points, normal the sources, detailed everything", lines[^2]);
    }

    [Fact]
    public void WithoutRolesNormalShowsEveryRequirement()
    {
        var lines = Lines(Text(new ReportOptions(Detail.Normal), roles: false));

        Assert.Contains(lines, l => Row(l, "account +Read +Caller +Code.cs:23, Code.cs:28"));
        Assert.Contains(lines, l => Row(l, "contact +Delete +System +Code.cs:30"));
        Assert.Contains(lines, l => l.StartsWith(CreateStep, StringComparison.Ordinal));
        Assert.Contains("Privileges per table", lines);
    }

    [Fact]
    public void TableFilterNarrowsTheSectionsAndMissingPrivilegesButNotTheCounts()
    {
        var lines = Lines(Text(new ReportOptions(Detail.Detailed, Filter(tables: ["CONTACT"]))));

        Assert.DoesNotContain(lines, l => l.StartsWith("  ✔ account", StringComparison.Ordinal) || l.StartsWith("  ✘ account", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.StartsWith(CreateStep, StringComparison.Ordinal));
        Assert.DoesNotContain("Not reached from any entry point", lines);
        Assert.Contains(lines, l => l.StartsWith(UpdateStep, StringComparison.Ordinal));
        Assert.Contains("Filtered by --table CONTACT: 2 of 5 requirements match", lines);
        Assert.Contains("Result: 2 granted, 2 missing, 1 as SYSTEM", lines);
        var missing = lines.SkipWhile(l => l != "Missing privileges:").Skip(1).TakeWhile(l => l.StartsWith("  ✘ ", StringComparison.Ordinal));
        Assert.Equal(["  ✘ contact  Create  Caller  Salesperson (--role)  in " + UpdateStep], missing);
    }

    [Fact]
    public void MinimalIsNarrowedByTheFilter()
    {
        var lines = Lines(Text(new ReportOptions(Detail.Minimal, Filter(tables: ["account"]))));

        Assert.DoesNotContain(lines, l => l.Contains("contact", StringComparison.Ordinal));
        Assert.Contains("  account  Delete", lines);
    }

    [Fact]
    public void FilterThatMatchesNothingLeavesOnlyTheTotals()
    {
        var lines = Lines(Text(ReportOptions.Default with { Filter = Filter(bases: [RequirementBasis.AllImplementations]) }));

        Assert.DoesNotContain(lines, l => l.StartsWith("plugin:", StringComparison.Ordinal));
        Assert.DoesNotContain("Missing privileges:", lines);
        Assert.Contains("Filtered by --basis all-implementations: 0 of 5 requirements match", lines);
        Assert.Contains("Result: 2 granted, 2 missing, 1 as SYSTEM", lines);
    }

    [Fact]
    public void IdentityAndPrivilegeFiltersCombine()
    {
        var lines = Lines(Text(new ReportOptions(Detail.Detailed, Filter(privileges: ["delete"], identities: [RunsAsKind.System]))));

        var rows = lines.Where(l => l.StartsWith("  · ", StringComparison.Ordinal) || l.StartsWith("  ✔ ", StringComparison.Ordinal) || l.StartsWith("  ✘ ", StringComparison.Ordinal));
        Assert.True(Row(Assert.Single(rows), "· contact +Delete +System +SYSTEM +Code.cs:30"));
    }

    [Fact]
    public void JsonIsNarrowedByTheFilterButKeepsTheResult()
    {
        var manifest = Analyze.Code(Source, ["account", "contact"]);
        var coverage = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });
        using var output = new StringWriter();
        new JsonManifestWriter(Filter(tables: ["contact"])).Write(manifest, coverage, output);
        var json = output.ToString();

        // account Read and Delete are left out; the Update step's trigger is still on account
        Assert.DoesNotContain("\"privilege\": \"Read\"", json);
        Assert.Contains("\"privilege\": \"Create\"", json);
        Assert.DoesNotContain($"\"id\": \"{CreateStep}\"", json);
        Assert.Contains("\"unreached\": []", json);
        Assert.Contains("\"missing\": 2", json);
        Assert.Contains("\"missing\": [\n    {\n      \"table\": \"contact\"", json);
    }

    [Fact]
    public void FiltersTakeCommaSeparatedAndRepeatedValues()
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);
        var result = command.Parse(["--table", "account,contact", "--table", "lead", "--runs-as", "system", "--basis", "filter-unknown", "-d", "full"]);

        Assert.Empty(result.Errors);
        var report = options.Read(result).Report!;
        Assert.Equal(Detail.Full, report.Detail);
        Assert.Equal("--table account, contact, lead; --runs-as System; --basis filter-unknown", report.Filter.ToString());
    }

    [Fact]
    public void StrictAlsoFailsOnMissing()
    {
        var command = new RootCommand();
        var options = new AnalysisOptions();
        options.AddTo(command);

        var plain = options.Read(command.Parse([]));
        Assert.False(plain.FailOnMissing);
        var strict = options.Read(command.Parse(["--strict"]));
        Assert.True(strict.FailOnMissing);
        Assert.True(strict.Strict);
    }

    [Fact]
    public void NoRolesCantBeUsedWithRolesOrFailing()
    {
        var command = new RootCommand();
        new AnalysisOptions().AddTo(command);

        Assert.Empty(command.Parse(["--no-roles"]).Errors);
        Assert.Equal(
            "--no-roles can't be used with --fail-on-missing, which needs roles.",
            Assert.Single(command.Parse(["--no-roles", "--fail-on-missing"]).Errors).Message);
        Assert.Equal(
            "--no-roles can't be used with --role, which needs roles.",
            Assert.Single(command.Parse(["--no-roles", "-r", "Salesperson", "--strict"]).Errors).Message);
    }

    [Fact]
    public void UnknownIdentityOrBasisIsAnError()
    {
        var command = new RootCommand();
        new AnalysisOptions().AddTo(command);
        var result = command.Parse(["--runs-as", "Admin", "--basis", "guess"]);

        Assert.Equal(
            [
                "Unknown identity 'Admin' in --runs-as. Use Caller, User, ApplicationUser, ImpersonatedCaller, System.",
                "Unknown basis 'guess' in --basis. Use direct, execute-privilege, all-implementations, all-lookup-targets, filter-unknown.",
            ],
            result.Errors.Select(e => e.Message));
    }
}
