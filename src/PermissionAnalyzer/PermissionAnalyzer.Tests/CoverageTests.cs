using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class CoverageTests
{
    private const string UserId = "6a1e5c4d-2b3f-4a5e-8c7d-9e0f1a2b3c4d";

    private static readonly RoleDefinition Salesperson = Role("Salesperson",
        ("account", Privilege.Read, PrivilegeDepth.User),
        ("account", Privilege.Write, PrivilegeDepth.User),
        ("contact", Privilege.Read, PrivilegeDepth.BusinessUnit));

    private static readonly RoleDefinition Manager = Role("Sales Manager",
        ("account", Privilege.Read, PrivilegeDepth.Organization),
        ("contact", Privilege.Create, PrivilegeDepth.User));

    private static readonly RoleDefinition Integration = Role("Integration App",
        ("account", Privilege.Create, PrivilegeDepth.Organization),
        ("contact", Privilege.Delete, PrivilegeDepth.Organization));

    private static readonly string[] Governed = ["account", "contact"];

    private const string PluginSource = """
        public class P : Plugin
        {
            public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, Execute);

            private void Execute(LocalPluginContext context)
            {
                context.OrganizationService.Retrieve("account", Guid.Empty, new ColumnSet());
                context.OrganizationService.Create(new Contact());
                context.OrganizationAdminService.Delete("contact", Guid.Empty);
            }
        }
        """;

    private const string FunctionSource = """
        public class Functions
        {
            [Microsoft.Azure.Functions.Worker.Function("ProcessOrder")]
            public void Run([Microsoft.Azure.Functions.Worker.HttpTrigger] object request)
            {
                var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                client.Create(new Account());
            }

            [Microsoft.Azure.Functions.Worker.Function("ActForUser")]
            public static void ActForUser([Microsoft.Azure.Functions.Worker.TimerTrigger("0 * * * *")] object timer, Guid userId)
            {
                var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                client.CallerId = userId;
                client.Delete("contact", Guid.Empty);
            }
        }
        """;

    [Fact]
    public void CombinedRolesGrantTheUnionWithTheHighestDepth()
    {
        var roles = RoleSet.Combine([Salesperson, Manager], "--role");

        Assert.Equal(["Sales Manager", "Salesperson"], roles.Names);
        Assert.Equal(PrivilegeDepth.Organization, roles.Depth("account", Privilege.Read));
        Assert.Equal(PrivilegeDepth.User, roles.Depth("account", Privilege.Write));
        Assert.Equal(PrivilegeDepth.User, roles.Depth("contact", Privilege.Create));
        Assert.Null(roles.Depth("contact", Privilege.Delete));
        Assert.Null(roles.Depth("lead", Privilege.Read));
    }

    [Fact]
    public void CallerRequirementsAreCheckedAgainstTheRoles()
    {
        var manifest = Analyze.Code(PluginSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(
            ["account:Read as Caller: Granted User", "contact:Create as Caller: Missing", "contact:Delete as System: System"],
            Results(report, entry));
        Assert.Equal(new CoverageSummary(1, 0, 1, 0, 0, 1, 0, 0, 0, 0), report.Summary);
        Assert.False(report.Summary.Passed(strict: false));

        var identity = Assert.Single(report.For(entry).Identities);
        Assert.Equal(RunsAs.Caller, identity.RunsAs);
        Assert.Equal("Salesperson (--role)", identity.Roles?.ToString());
    }

    [Fact]
    public void AnyDepthCountsAndCombinedRolesCover()
    {
        var manifest = Analyze.Code(PluginSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson, Manager], "--role") });

        Assert.Equal(
            ["account:Read as Caller: Granted Organization", "contact:Create as Caller: Granted User", "contact:Delete as System: System"],
            Results(report, manifest.Entry("plugin:P:Update:account:PostOperation")));
        Assert.True(report.Summary.Passed(strict: true));
    }

    [Fact]
    public void ApplicationUserRolesComeFromPrincipalsWithTheFunctionWinning()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var options = new CoverageOptions
        {
            Principals = new Dictionary<string, RoleSet>
            {
                ["test"] = RoleSet.Combine([Salesperson], "Principals: Test"),
                ["Test/ProcessOrder"] = RoleSet.Combine([Integration], "Principals: Test/ProcessOrder"),
            },
        };
        var report = CoverageEvaluator.Evaluate(manifest, options);

        var process = manifest.Entry("function:ProcessOrder");
        Assert.Equal(["account:Create as ApplicationUser: Granted Organization"], Results(report, process));
        Assert.Equal("Integration App (Principals: Test/ProcessOrder)", Assert.Single(report.For(process).Identities).Roles?.ToString());
        Assert.True(report.UsesConfiguredRoles);
    }

    [Fact]
    public void RoleOptionReplacesPrincipals()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var options = new CoverageOptions
        {
            Roles = RoleSet.Combine([Salesperson], "--role"),
            Principals = new Dictionary<string, RoleSet> { ["Test"] = RoleSet.Combine([Integration], "Principals: Test") },
        };
        var report = CoverageEvaluator.Evaluate(manifest, options);

        Assert.Equal(["account:Create as ApplicationUser: Missing"], Results(report, manifest.Entry("function:ProcessOrder")));
        Assert.False(report.UsesConfiguredRoles);
    }

    [Fact]
    public void ApplicationUserWithoutRolesIsNotChecked()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { CallerRoles = RoleSet.Combine([Integration], "--caller-role") });

        var process = manifest.Entry("function:ProcessOrder");
        Assert.Equal(["account:Create as ApplicationUser: NotChecked"], Results(report, process));
        Assert.Equal("not checked: pass --role, or map Test in XrmMockup:Permissions:Principals", Assert.Single(report.For(process).Identities).Note);
        Assert.True(report.Summary.Passed(strict: false));
        Assert.False(report.Summary.Passed(strict: true));
    }

    [Fact]
    public void ImpersonatedCallsUseCallerRolesAndNeedActOnBehalfOfAnotherUser()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var withoutCallerRoles = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Integration], "--role") });
        var actForUser = manifest.Entry("function:ActForUser");

        Assert.Equal(["contact:Delete as ImpersonatedCaller: NotChecked"], Results(withoutCallerRoles, actForUser));
        Assert.StartsWith("not checked: acts for the user in CallerId", withoutCallerRoles.For(actForUser).Identities.Single(i => i.RunsAs.Kind == RunsAsKind.ImpersonatedCaller).Note);

        // The application user's roles don't count: the call runs with the impersonated user's privileges
        var withCallerRoles = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Roles = RoleSet.Combine([Integration], "--role"),
            CallerRoles = RoleSet.Combine([Salesperson], "--caller-role"),
        });
        Assert.Equal(["contact:Delete as ImpersonatedCaller: Missing"], Results(withCallerRoles, actForUser));

        // The roles come from metadata without global privileges, so the privilege to impersonate can't be checked
        Assert.Equal(["prvActOnBehalfOfAnotherUser as ApplicationUser: Unverifiable"], GlobalResults(withCallerRoles, actForUser));
        Assert.Empty(manifest.Entry("function:ProcessOrder").GlobalRequirements);
        Assert.Equal(1, withCallerRoles.Summary.Unverifiable);
    }

    [Fact]
    public void ImpersonationIsCheckedAgainstGlobalPrivileges()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var actForUser = manifest.Entry("function:ActForUser");

        var missing = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([WithGlobal(Integration)], "--role") });
        Assert.Equal(["prvActOnBehalfOfAnotherUser as ApplicationUser: Missing"], GlobalResults(missing, actForUser));
        var global = Assert.Single(missing.MissingGlobal);
        Assert.Equal((GlobalRequirement.ActOnBehalfOfAnotherUser, RunsAs.ApplicationUser), (global.Privilege, global.RunsAs));
        Assert.Equal(["function:ActForUser"], global.In);
        Assert.Equal(0, missing.Summary.Unverifiable);
        Assert.False(missing.Summary.Passed(strict: false));

        var granted = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Roles = RoleSet.Combine([WithGlobal(Integration, ("prvactonbehalfofanotheruser", PrivilegeDepth.Organization))], "--role"),
        });
        Assert.Equal(["prvActOnBehalfOfAnotherUser as ApplicationUser: Granted Organization"], GlobalResults(granted, actForUser));
    }

    [Fact]
    public void GlobalPrivilegeCanBeSuppressedByName()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Roles = RoleSet.Combine([WithGlobal(Integration)], "--role"),
            Suppress = [CoverageSetup.Suppression(new SuppressionConfiguration { Privilege = "prvActOnBehalfOfAnotherUser", Reason = "set up by hand" }, 0)],
        });

        Assert.Equal(["prvActOnBehalfOfAnotherUser as ApplicationUser: Suppressed"], GlobalResults(report, manifest.Entry("function:ActForUser")));
        Assert.Empty(report.MissingGlobal);
    }

    [Fact]
    public void SuppressionWithoutTableNeedsAGlobalPrivilegeName()
    {
        var exception = Assert.Throws<ToolException>(() => CoverageSetup.Suppression(new SuppressionConfiguration { Privilege = "Create" }, 2));

        Assert.StartsWith("XrmMockup:Permissions:Suppress:2 has no Table", exception.Message);
    }

    [Fact]
    public void RolesFromOlderMetadataDontKnowGlobalPrivileges()
    {
        Assert.False(RoleSet.Combine([Integration, WithGlobal(Salesperson)], "--role").KnowsGlobalPrivileges);
        Assert.True(RoleSet.Combine([WithGlobal(Integration), WithGlobal(Salesperson)], "--role").KnowsGlobalPrivileges);
    }

    [Fact]
    public void UnverifiableImpersonationOnlyFailsWhenStrict()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Roles = RoleSet.Combine([Integration], "--role"),
            CallerRoles = RoleSet.Combine([Integration], "--caller-role"),
        });

        Assert.True(report.Summary.Passed(strict: false));
        Assert.False(report.Summary.Passed(strict: true));
    }

    [Fact]
    public void ImpersonatingUserOfAStepUsesTheUsersMapping()
    {
        var source = $$"""
            public class P : Plugin
            {
                public P()
                {
                    RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Contact()))
                        .SetUserContext(new Guid("{{UserId}}"));
                }
            }
            """;
        var manifest = Analyze.Code(source, Governed);
        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");

        // --role is for the caller, not for a specific user
        var unmapped = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Manager], "--role") });
        Assert.Equal([$"contact:Create as User {UserId}: NotChecked"], Results(unmapped, entry));
        Assert.Equal($"not checked: runs as user {UserId}; map it in XrmMockup:Permissions:Users", Assert.Single(unmapped.For(entry).Identities).Note);

        var mapped = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Users = new Dictionary<string, RoleSet> { [UserId.ToUpperInvariant()] = RoleSet.Combine([Manager], "Users: svc-plugins") },
        });
        Assert.Equal([$"contact:Create as User {UserId}: Granted User"], Results(mapped, entry));
        Assert.True(mapped.UsesConfiguredRoles);
    }

    [Fact]
    public void AssumedIdentitiesAreChecked()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class Importer
                {
                    public void Run(IOrganizationService service) => service.Delete("contact", Guid.Empty);
                }
            }
            """, Governed, setup: b => b.AddEntryMethods(["Contoso.Importer.Run"]));
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });

        Assert.Equal(["contact:Delete as ApplicationUser (assumed): Missing"], Results(report, Assert.Single(manifest.EntryPoints)));
        Assert.Equal(1, report.Summary.Missing);
    }

    [Fact]
    public void TablesNoRoleGovernsAreCovered()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Entity("ctx_log")));
            }
            """, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });

        Assert.Equal(["ctx_log:Create as Caller: NotGoverned"], Results(report, Assert.Single(manifest.EntryPoints)));
        Assert.True(report.Summary.Passed(strict: true));
    }

    [Fact]
    public void TablesNoRoleGovernsAreCoveredForIdentitiesWithoutRoles()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Entity("ctx_log")));
            }
            """, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions());

        Assert.Equal(["ctx_log:Create as Caller: NotGoverned"], Results(report, Assert.Single(manifest.EntryPoints)));
        Assert.Equal(0, report.Summary.NotChecked);
    }

    [Fact]
    public void SuppressedPrivilegesDontFail()
    {
        var manifest = Analyze.Code(PluginSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Roles = RoleSet.Combine([Salesperson], "--role"),
            Suppress = [new Suppression("contact", Privilege.Create, "Created by a flow"), new Suppression("account", null, "Not used")],
        });

        var entry = Assert.Single(manifest.EntryPoints);
        Assert.Equal(
            ["account:Read as Caller: Granted User", "contact:Create as Caller: Suppressed", "contact:Delete as System: System"],
            Results(report, entry));
        Assert.Equal("Created by a flow", report.For(entry).For(entry.Requirements[1]).Reason);
        Assert.True(report.Summary.Passed(strict: true));
    }

    [Fact]
    public void RoleCatalogFindsNamesIgnoringCaseAndSuggestsClosest()
    {
        var catalog = new RoleCatalog([Integration, Manager, Salesperson]);

        Assert.Same(Salesperson, Assert.Single(catalog.Find("salesperson")));
        Assert.Empty(catalog.Find("Sales"));
        Assert.Equal("Salesperson", catalog.Suggest("Salesperon")[0]);
        // Both contain the name, so the closer one comes first
        Assert.Equal(["Salesperson", "Sales Manager"], catalog.Suggest("sales").Take(2));
        Assert.Empty(catalog.Suggest("Marketing"));
    }

    [Fact]
    public void UnknownRoleIsAnErrorWithSuggestions()
    {
        var error = Assert.Throws<ToolException>(() =>
            CoverageSetup.Create([Integration, Manager, Salesperson], ["Salesperon"], [], new PermissionsConfiguration(), requireRoles: true));

        Assert.Contains("Unknown security role 'Salesperon' in --role. Did you mean 'Salesperson'", error.Message);
    }

    [Fact]
    public void UnknownRoleInConfigurationNamesTheSetting()
    {
        var configuration = new PermissionsConfiguration
        {
            Principals = new() { ["Contoso.Functions"] = new PrincipalConfiguration { Roles = ["Integration"] } },
        };
        var error = Assert.Throws<ToolException>(() => CoverageSetup.Create([Integration], [], [], configuration, requireRoles: true));

        Assert.Contains("in XrmMockup:Permissions:Principals:Contoso.Functions. Did you mean 'Integration App'?", error.Message);
    }

    [Fact]
    public void FailingNeedsRolesAndAPlainRunDoesnt()
    {
        Assert.Null(CoverageSetup.Create([Salesperson], [], [], new PermissionsConfiguration(), requireRoles: false));
        var error = Assert.Throws<ToolException>(() => CoverageSetup.Create([Salesperson], [], [], new PermissionsConfiguration(), requireRoles: true));
        Assert.StartsWith("No roles to check against", error.Message);
    }

    [Fact]
    public void ConfiguredRolesApplyWithoutFailing()
    {
        var configuration = new PermissionsConfiguration
        {
            Principals = new() { ["Contoso.Functions"] = new PrincipalConfiguration { Roles = ["Integration App"] } },
        };

        Assert.NotNull(CoverageSetup.Create([Integration], [], [], configuration, requireRoles: false));
        // Without roles to look them up in, a plain run only lists what the code needs
        Assert.Null(CoverageSetup.Create(null, [], [], configuration, requireRoles: false));
        Assert.Throws<ToolException>(() => CoverageSetup.Create(null, [], [], configuration, requireRoles: true));
    }

    [Fact]
    public void SuppressionWithUnknownPrivilegeIsAnError()
    {
        var configuration = new PermissionsConfiguration { Suppress = [new SuppressionConfiguration { Table = "account", Privilege = "Update" }] };
        var error = Assert.Throws<ToolException>(() => CoverageSetup.Create([Salesperson], ["Salesperson"], [], configuration, requireRoles: true));

        Assert.StartsWith("XrmMockup:Permissions:Suppress:0 has an unknown Privilege 'Update'", error.Message);
    }

    [Fact]
    public void MissingPrivilegesAreMergedAcrossEntryPoints()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P()
                {
                    RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Contact()));
                    RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Contact()));
                    RegisterPluginStep<Account>(EventOperation.Delete, ExecutionStage.PreOperation, c => c.OrganizationService.Delete("account", Guid.Empty));
                }
            }
            """, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });

        Assert.Equal(
            [
                "account:Delete as Caller (Salesperson (--role)) in plugin:P:Delete:account:PreOperation",
                "contact:Create as Caller (Salesperson (--role)) in plugin:P:Create:account:PostOperation, plugin:P:Update:account:PostOperation",
            ],
            report.Missing.Select(m => $"{m.Table}:{m.Privilege} as {m.RunsAs} ({m.Roles}) in {string.Join(", ", m.In)}"));
        Assert.Equal(3, report.Summary.Missing);
    }

    [Fact]
    public void TextOutputMarksEachRequirement()
    {
        var manifest = Analyze.Code(PluginSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });
        using var output = new StringWriter();
        new TextManifestWriter(new ReportOptions(Verbosity.Detailed)).Write(manifest, report, output);
        var lines = output.ToString().Split('\n');

        Assert.Contains("  roles for Caller: Salesperson (--role)", lines);
        Assert.Contains(lines, l => l.StartsWith("  ✔ account  Read      Caller  User     ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("  ✘ contact  Create    Caller  missing  ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("  · contact  Delete    System  SYSTEM   ", StringComparison.Ordinal));
        Assert.Contains("Result: 1 granted, 1 missing, 1 as SYSTEM", lines);
        Assert.Contains("Missing privileges:", lines);
        Assert.Contains("  ✘ contact  Create  Caller  Salesperson (--role)  in plugin:P:Update:account:PostOperation", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("Not checked: ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("1 entry point, 0 unreached methods, 2 tables, 3 table privileges (3 requirements counted per entry point and method)", StringComparison.Ordinal));
    }

    [Fact]
    public void TextOutputListsWhatWasNotChecked()
    {
        var summary = new CoverageSummary(Granted: 2, NotGoverned: 0, Missing: 0, Suppressed: 0, NotChecked: 1, System: 0, Unresolved: 3, UnresolvedNoRule: 1, UnresolvedCustomApi: 1, Unverifiable: 1);
        Assert.True(summary.Incomplete);
        Assert.True(summary.Passed(strict: false));
        Assert.False(summary.Passed(strict: true));

        var manifest = Analyze.Code(FunctionSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Integration], "--role") });
        using var output = new StringWriter();
        new TextManifestWriter().Write(manifest, report, output);
        var line = Assert.Single(output.ToString().Split('\n'), l => l.StartsWith("Not checked: ", StringComparison.Ordinal));

        Assert.EndsWith("1 privilege not tied to a table (the role metadata predates them; regenerate it)", line);
    }

    [Fact]
    public void TextOutputCountsRequestsWithoutRuleAndCustomApiCalls()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c =>
                {
                    c.OrganizationService.Execute(new AddMembersTeamRequest());
                    c.OrganizationService.Execute(new OrganizationRequest("ctx_Recalculate"));
                    c.OrganizationService.Execute(new OrganizationRequest(c.PluginExecutionContext.MessageName));
                });
            }
            """, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });
        using var output = new StringWriter();
        new TextManifestWriter().Write(manifest, report, output);
        var line = Assert.Single(output.ToString().Split('\n'), l => l.StartsWith("Not checked: ", StringComparison.Ordinal));

        Assert.StartsWith("Not checked: 3 unresolved operations (1 request without a rule, 1 custom API call, 1 with an unknown table or request)", line);
    }

    [Fact]
    public void TextOutputShowsTheImpersonationPrivilege()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Integration], "--role") });
        using var output = new StringWriter();
        new TextManifestWriter(new ReportOptions(Verbosity.Detailed)).Write(manifest, report, output);
        var text = output.ToString();

        Assert.Matches(@"\n  · prvActOnBehalfOfAnotherUser +global +ApplicationUser +unverifiable +", text);
        Assert.Contains("  roles for ImpersonatedCaller: not checked: acts for the user in CallerId", text);
        Assert.Contains("Privileges not tied to a table: prvActOnBehalfOfAnotherUser\n", text);
        Assert.Contains("1 privilege not tied to a table (the role metadata predates them; regenerate it)", text);
    }

    [Fact]
    public void JsonOutputHasCoverageOnlyWithRoles()
    {
        var manifest = Analyze.Code(PluginSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Salesperson], "--role") });

        using var plain = new StringWriter();
        new JsonManifestWriter().Write(manifest, null, plain);
        Assert.DoesNotContain("coverage", plain.ToString());
        Assert.DoesNotContain("\"result\"", plain.ToString());
        Assert.DoesNotContain("\"missing\"", plain.ToString());

        using var covered = new StringWriter();
        new JsonManifestWriter().Write(manifest, report, covered);
        var json = covered.ToString();
        Assert.Contains("\"status\": \"granted\"", json);
        Assert.Contains("\"depth\": \"User\"", json);
        Assert.Contains("\"status\": \"missing\"", json);
        Assert.Contains("\"origin\": \"--role\"", json);
        Assert.Contains("\"missing\": 1", json);
        Assert.Contains("\"unresolvedNoRule\": 0", json);
        Assert.Contains("\"unresolvedCustomApi\": 0", json);
        Assert.Contains("\"in\": [\n        \"plugin:P:Update:account:PostOperation\"\n      ]", json);
        Assert.Contains("\"incomplete\": false", json);
    }

    [Fact]
    public void JsonOutputWritesGlobalRequirementsWithoutATable()
    {
        var manifest = Analyze.Code(FunctionSource, Governed);
        var report = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([WithGlobal(Integration)], "--role") });
        using var output = new StringWriter();
        new JsonManifestWriter().Write(manifest, report, output);
        var json = output.ToString();

        Assert.Contains("\"privilege\": \"prvActOnBehalfOfAnotherUser\",\n          \"runsAs\": \"ApplicationUser\"", json);
        Assert.Contains("\"privilege\": \"Delete\"", json);
        Assert.DoesNotContain("impersonation", json);
        Assert.Contains("\"missing\": [\n    {\n      \"privilege\": \"prvActOnBehalfOfAnotherUser\"", json);
    }

    private static IReadOnlyList<string> Results(CoverageReport report, EntryPoint entryPoint) =>
        entryPoint.Requirements.Select(r =>
        {
            var coverage = report.For(entryPoint).For(r);
            var depth = coverage.Depth is { } d ? $" {d}" : "";
            return $"{r.Table}:{r.Privilege} as {r.RunsAs}: {coverage.Status}{depth}";
        }).ToList();

    private static IReadOnlyList<string> GlobalResults(CoverageReport report, EntryPoint entryPoint) =>
        entryPoint.GlobalRequirements.Select(r =>
        {
            var coverage = report.For(entryPoint).For(r);
            var depth = coverage.Depth is { } d ? $" {d}" : "";
            return $"{r.Privilege} as {r.RunsAs}: {coverage.Status}{depth}";
        }).ToList();

    /// <summary>
    /// The role as downloaded by a metadata generator that has global privileges.
    /// </summary>
    private static RoleDefinition WithGlobal(RoleDefinition role, params (string Name, PrivilegeDepth Depth)[] globalPrivileges) =>
        new(role.Name, role.Privileges, globalPrivileges.ToDictionary(p => p.Name, p => p.Depth, StringComparer.OrdinalIgnoreCase));

    private static RoleDefinition Role(string name, params (string Table, Privilege Privilege, PrivilegeDepth Depth)[] privileges) =>
        new(name, privileges
            .GroupBy(p => p.Table)
            .ToDictionary(g => g.Key, g => (IReadOnlyDictionary<Privilege, PrivilegeDepth>)g.ToDictionary(p => p.Privilege, p => p.Depth)));
}
