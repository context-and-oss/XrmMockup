using System.Text.Json;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class CompletenessTests
{
    private static Manifest Impersonates(string operation) => Analyze.Code($$"""
        public class Functions {
            [Microsoft.Azure.Functions.Worker.Function("Run")]
            public static void Run(Guid userId) {
                var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                client.CallerId = userId;
                {{operation}}
            }
        }
        """, ["account"]);

    private static RoleDefinition Role(string name, bool write = false, bool delegateUser = false, bool publish = false)
    {
        var globals = new Dictionary<string, PrivilegeDepth>(StringComparer.OrdinalIgnoreCase);
        if (delegateUser) globals[GlobalRequirement.ActOnBehalfOfAnotherUser] = PrivilegeDepth.Organization;
        if (publish) globals["prvPublishCustomization"] = PrivilegeDepth.Organization;
        return new RoleDefinition(name, new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>>
        {
            ["account"] = write ? new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Write] = PrivilegeDepth.User } : new Dictionary<Privilege, PrivilegeDepth>()
        }, globals);
    }

    [Fact]
    public void EndUsersWriteCannotCompensateForMissingApplicationUserWrite()
    {
        var manifest = Impersonates("client.Update(new Account());");
        var coverage = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Roles = RoleSet.Combine([Role("App", delegateUser: true)], "--role"),
            CallerRoles = RoleSet.Combine([Role("User", write: true)], "--caller-role")
        });
        var missing = Assert.Single(coverage.Missing);
        Assert.Equal(("account", Privilege.Write, RunsAs.ApplicationUser), (missing.Table, missing.Privilege, missing.RunsAs));
        Assert.False(coverage.Summary.Passed(strict: true));
        var entry = Assert.Single(manifest.EntryPoints);
        Assert.Equal(entry.Requirements[0].Sources, entry.Requirements[1].Sources);
    }

    [Fact]
    public void GlobalOnlyImpersonationRequiresBothPrivilegesAndDelegation()
    {
        var manifest = Impersonates("client.Execute(new PublishAllXmlRequest());");
        var entry = Assert.Single(manifest.EntryPoints);
        Assert.Contains(entry.GlobalRequirements, r => r.Privilege == GlobalRequirement.ActOnBehalfOfAnotherUser && r.RunsAs == RunsAs.ApplicationUser);
        Assert.Equal(2, entry.GlobalRequirements.Count(r => r.Privilege == "prvPublishCustomization"));
        var coverage = CoverageEvaluator.Evaluate(manifest, new CoverageOptions
        {
            Roles = RoleSet.Combine([Role("App", delegateUser: true)], "--role"),
            CallerRoles = RoleSet.Combine([Role("User", publish: true)], "--caller-role")
        });
        Assert.Equal("prvPublishCustomization", Assert.Single(coverage.MissingGlobal).Privilege);
    }

    [Fact]
    public void TeamDelegationDoesNotAuthorizeImpersonationButTeamWriteStillCounts()
    {
        var app = RoleSet.Combine([Role("Direct"), Role("Team", write: true, delegateUser: true)], "assignments", [Role("Direct")]);
        Assert.Equal(PrivilegeDepth.User, app.Depth("account", Privilege.Write));
        Assert.Equal(PrivilegeDepth.Organization, app.GlobalDepth(GlobalRequirement.ActOnBehalfOfAnotherUser));
        Assert.Null(app.DirectGlobalDepth(GlobalRequirement.ActOnBehalfOfAnotherUser));
        var coverage = CoverageEvaluator.Evaluate(Impersonates("client.Update(new Account());"), new CoverageOptions
        {
            Roles = app,
            CallerRoles = RoleSet.Combine([Role("User", write: true)], "--caller-role")
        });
        Assert.Empty(coverage.Missing);
        Assert.Equal(GlobalRequirement.ActOnBehalfOfAnotherUser, Assert.Single(coverage.MissingGlobal).Privilege);
    }

    [Fact]
    public void UnknownLookupContentsRemainIncompleteEvenWithKnownTable()
    {
        var manifest = Analyze.Code("class C { void M(IOrganizationService s) => s.Update(s.Retrieve(\"account\", Guid.Empty, new ColumnSet(true))); }", ["account"]);
        Assert.True(manifest.Incomplete);
        Assert.Contains(Assert.Single(manifest.Unreached).Unresolved, u => u.Privileges.Contains(Privilege.AppendTo));
    }

    [Fact]
    public void RoleFreeJsonIncludesCompletenessTruncationAndReportScope()
    {
        var section = new MethodManifest("C.M", null, [], [], [], truncated: true);
        var manifest = new Manifest([], [section]);
        using var output = new StringWriter();
        new JsonManifestWriter(new ReportFilter(["account"], [], [], [])).Write(manifest, null, output);
        var document = JsonDocument.Parse(output.ToString()).RootElement;
        Assert.Equal(1, document.GetProperty("schemaVersion").GetInt32());
        var analysis = document.GetProperty("analysis");
        Assert.True(analysis.GetProperty("incomplete").GetBoolean());
        Assert.Equal("C.M", analysis.GetProperty("truncatedSections")[0].GetString());
        Assert.Equal("whole-analysis", analysis.GetProperty("countScope").GetString());
        Assert.Contains("account", analysis.GetProperty("reportFilter").GetString());
        Assert.False(document.TryGetProperty("result", out _));
    }

    [Fact]
    public void MissingRoleInventoryIsUnverifiableNotAnExemption()
    {
        var manifest = Analyze.Code("class C { void M(IOrganizationService s) => s.Delete(\"account\", Guid.Empty); }", []);
        var coverage = CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Role("Empty")], "test") });
        Assert.Equal(1, coverage.Summary.Unverifiable);
        Assert.Equal(0, coverage.Summary.NotGoverned);
        Assert.False(coverage.Summary.Passed(strict: true));
        Assert.Contains("exemption", coverage.For(Assert.Single(manifest.Unreached)).For(Assert.Single(manifest.AllRequirements)).Reason);
        using var output = new StringWriter();
        new MarkdownManifestWriter().Write(manifest, coverage, output);
        Assert.Contains("role metadata is incomplete", output.ToString());
        Assert.DoesNotContain("metadata predates privileges not tied to a table", output.ToString());
    }

    [Fact]
    public void PartialMockupEnforcementIsReportedWithoutDroppingRequirements()
    {
        var manifest = Analyze.Code("class C { void M(IOrganizationService s) => s.Execute(new SetStateRequest { EntityMoniker = new EntityReference(\"account\", Guid.Empty) }); }");
        var method = Assert.Single(manifest.Unreached);
        Assert.Contains(method.Requirements, r => r.Table == "account" && r.Privilege == Privilege.Write);
        Assert.Contains(method.Assumptions, a => a.Reason.Contains("XrmMockup does not enforce"));
        Assert.False(manifest.Incomplete);
    }

    [Fact]
    public void RuleEnforcementAnnotationsAndUnresolvedChecksPropagateThroughAsRules()
    {
        var manifest = Analyze.Code("class C { void M(IOrganizationService s) => s.Execute(new OrganizationRequest(\"ctx_Test\")); }", setup: b => b.AddEntryMethods(["C.M"]),
            rules: [new RequestRule("ctx_Test") { As = ["ctx_Inner"] }, new RequestRule("ctx_Inner") { Enforced = false, Unresolved = ["runtime data"] }]);
        var entry = Assert.Single(manifest.EntryPoints);
        Assert.Contains(entry.Assumptions, a => a.Reason.Contains("ctx_Inner"));
        Assert.Contains(entry.Unresolved, u => u.Reason == "runtime data");
        Assert.True(manifest.Incomplete);
    }

    [Fact]
    public void SyntheticWritesDistinguishUnknownFromExplicitlyEmptyAttributes()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        builder.AddOperation("Update", "account");
        var unknown = builder.Build(scope: EntryPointSelector.Operation("Update", "account"));
        Assert.True(unknown.Incomplete);
        Assert.Contains(Assert.Single(unknown.EntryPoints).Unresolved, u => u.Privileges.Contains(Privilege.AppendTo));
        var empty = new ManifestBuilder(Analyze.Root);
        empty.AddOperation("Update", "account", []);
        Assert.False(empty.Build().Incomplete);
    }

    [Fact]
    public void ResolutionDepthLimitCannotSilentlyPass()
    {
        var helpers = string.Join("\n", Enumerable.Range(0, 24).Select(i =>
            $"static IOrganizationService S{i}(IOrganizationService s) => " + (i == 23 ? "s;" : $"S{i + 1}(s);")));
        var manifest = Analyze.Code($$"""
            public class P : Plugin {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation,
                    c => S0(c.OrganizationService).Delete("account", Guid.Empty));
                {{helpers}}
            }
            """, ["account"]);
        Assert.True(manifest.Incomplete);
        Assert.True(Assert.Single(manifest.EntryPoints).Truncated);
    }

    [Fact]
    public void FingerprintsAreStableAndTrackMetadataWithoutRoleData()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(directory, "SecurityRoles"));
            File.WriteAllText(Path.Combine(directory, "SecurityRoles", "role.xml"), "role data");
            var metadata = Path.Combine(directory, "Metadata.xml");
            File.WriteAllText(metadata, "first");
            var settings = new AnalysisSettings(Path.Combine(directory, "Test.csproj"), directory, ["*.Tests"], []);
            var first = AnalysisProvenance.Create(settings, new PermissionsConfiguration(), [], noRoles: true);
            var second = AnalysisProvenance.Create(settings, new PermissionsConfiguration
            {
                Users = new() { ["user"] = new() { Roles = ["Other role"] } }
            }, [], noRoles: true);
            Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
            Assert.Single(first.Metadata);
            File.WriteAllText(metadata, "second");
            Assert.NotEqual(first.Metadata[0].Sha256, AnalysisProvenance.Create(settings, new(), [], true).Metadata[0].Sha256);
            var a = new PermissionsConfiguration { Principals = new() { ["B"] = new(), ["A"] = new() } };
            var b = new PermissionsConfiguration { Principals = new() { ["A"] = new(), ["B"] = new() } };
            Assert.Equal(AnalysisProvenance.Create(settings, a, [], false).ConfigurationHash,
                AnalysisProvenance.Create(settings, b, [], false).ConfigurationHash);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }


    [Fact]
    public void NuGetAuditWarningsAreNotSourceLoadingFailures()
    {
        Assert.False(XrmMockup.PermissionAnalyzer.Tool.Workspace.CodeLoader.IsAnalysisFailure(new Microsoft.CodeAnalysis.WorkspaceDiagnostic(
            Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Failure,
            "Msbuild failed: Package 'Example' 1.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-example")));
        Assert.True(XrmMockup.PermissionAnalyzer.Tool.Workspace.CodeLoader.IsAnalysisFailure(new Microsoft.CodeAnalysis.WorkspaceDiagnostic(
            Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Failure, "Could not load referenced project Example.csproj")));
    }

}
