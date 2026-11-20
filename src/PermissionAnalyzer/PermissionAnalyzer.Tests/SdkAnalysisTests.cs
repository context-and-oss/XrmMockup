using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class SdkAnalysisTests
{
    private const string Plugins = """
        namespace Contoso {
            public class ContactPlugin : Plugin {
                public ContactPlugin() => RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PostOperation,
                    c => c.OrganizationService.Update(new Account { ["name"] = "changed" }));
            }
            public class AccountPlugin : Plugin {
                public AccountPlugin() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation,
                    c => c.OrganizationService.Delete("lead", Guid.Empty)).SetExecutionMode(ExecutionMode.Asynchronous).AddFilteredAttributes("name");
            }
        }
        """;

    [Fact]
    public void FreshMetadataCopiesReuseSourceAndIncludeRecursiveAsyncCascades()
    {
        var builder = Source(Plugins);
        var metadata = new PermissionMetadata { RoleGovernedTables = ["contact", "account", "lead"] };
        var copy = builder.WithMetadata(metadata);
        copy.AddOperation("Create", "contact");
        var manifest = copy.Build(metadata.RoleGovernedTables, EntryPointSelector.Operation("Create", "contact"));
        var entry = Assert.Single(manifest.EntryPoints);
        Assert.Equal(["account:Write", "contact:Create", "lead:Delete"], entry.Requirements());
        Assert.Equal(2, entry.Triggers.Count);
        Assert.Empty(manifest.Unreached);
        var next = builder.WithMetadata(metadata);
        next.AddOperation("Create", "task");
        Assert.Equal(["task:Create"], Assert.Single(next.Build(scope: EntryPointSelector.Operation("Create", "task")).EntryPoints).Requirements());
    }

    [Theory]
    [InlineData("name", true)]
    [InlineData("description", false)]
    public void OperationFilteringAttributesApply(string attribute, bool fires)
    {
        var builder = Source(Plugins).WithMetadata(new PermissionMetadata());
        builder.AddOperation("Update", "account", [attribute]);
        var entry = Assert.Single(builder.Build(scope: EntryPointSelector.Operation("Update", "account")).EntryPoints);
        Assert.Equal(fires, entry.Triggers.Count != 0);
        Assert.Contains(entry.Requirements, r => r.Table == "account" && r.Privilege == Privilege.Write);
    }

    [Fact]
    public void CurrentLookupMetadataAppliesToSyntheticOperation()
    {
        var builder = Source(Plugins).WithMetadata(new PermissionMetadata { Lookups = [new LookupColumn("task", "regardingobjectid", ["account", "contact"])] });
        builder.AddOperation("Create", "task", ["regardingobjectid"]);
        var entry = Assert.Single(builder.Build(scope: EntryPointSelector.Operation("Create", "task")).EntryPoints);
        Assert.Equal(["account:AppendTo", "contact:AppendTo", "task:Create", "task:Append"], entry.Requirements());
    }

    [Fact]
    public void CheckTimeWorkflowsReplaceLoadedFileRegistrations()
    {
        var source = Source(Plugins);
        source.AddWorkflows([new WorkflowRegistration("File workflow", "task", true, false, null, false, null)]);
        var active = source.WithMetadata(new PermissionMetadata());
        active.AddOperation("Create", "task");
        Assert.Contains(Assert.Single(active.Build(scope: EntryPointSelector.Operation("Create", "task")).EntryPoints).Unresolved, u => u.Reason.Contains("File workflow"));
        var disabled = source.WithMetadata(new PermissionMetadata { Workflows = [] });
        disabled.AddOperation("Create", "task");
        Assert.Empty(Assert.Single(disabled.Build(scope: EntryPointSelector.Operation("Create", "task")).EntryPoints).Unresolved);
    }

    [Fact]
    public void SourceLookupRequirementsUseCheckTimeMetadataAndCanChange()
    {
        var source = Source("""
            [EntityLogicalName("ctx_order")]
            public class Order : Entity {
                [AttributeLogicalName("ctx_customerid")]
                public EntityReference Customer => GetAttributeValue<EntityReference>("ctx_customerid");
            }
            class C {
                public void Run(IOrganizationService service, Order order) => service.Delete(order.Customer.LogicalName, order.Customer.Id);
            }
            """);
        source.AddEntryMethods(["C.Run"]);
        var first = source.WithMetadata(new PermissionMetadata { Lookups = [new LookupColumn("ctx_order", "ctx_customerid", ["account"])] });
        var second = source.WithMetadata(new PermissionMetadata { Lookups = [new LookupColumn("ctx_order", "ctx_customerid", ["contact"])] });
        Assert.Equal(["account:Delete"], Assert.Single(first.Build(scope: EntryPointSelector.Methods(["C.Run"])).EntryPoints).Requirements());
        Assert.Equal(["contact:Delete"], Assert.Single(second.Build(scope: EntryPointSelector.Methods(["C.Run"])).EntryPoints).Requirements());
    }

    [Fact]
    public void TypedPluginRequiresFullIdentityAndMessageStage()
    {
        var builder = Source(Plugins);
        var scope = PermissionScope.ForPlugin("Contoso.ContactPlugin", "Test", "Create", "PostOperation");
        var entry = Assert.Single(builder.Build(scope: EntryPointSelector.TypedPlugin(scope)).EntryPoints);
        Assert.Equal("Contoso.ContactPlugin", entry.Type);
        Assert.Empty(builder.Build(scope: EntryPointSelector.TypedPlugin(PermissionScope.ForPlugin("ContactPlugin", "Test"))).EntryPoints);
        Assert.Empty(builder.Build(scope: EntryPointSelector.TypedPlugin(PermissionScope.ForPlugin("Contoso.ContactPlugin", "Other"))).EntryPoints);
        Assert.Empty(builder.Build(scope: EntryPointSelector.TypedPlugin(PermissionScope.ForPlugin("Contoso.ContactPlugin", "Test", "Delete"))).EntryPoints);
    }

    [Fact]
    public void SharedEngineParity()
    {
        var source = Source(Plugins);
        source.AddOperation("Create", "contact");
        var cli = source.Build(scope: EntryPointSelector.Operation("Create", "contact"));
        var sdk = source.WithMetadata(new PermissionMetadata());
        sdk.AddOperation("Create", "contact");
        var manifest = sdk.Build(scope: EntryPointSelector.Operation("Create", "contact"));
        Assert.Equal(Assert.Single(cli.EntryPoints).RequirementsWithIdentity(), Assert.Single(manifest.EntryPoints).RequirementsWithIdentity());
        Assert.Equal(Assert.Single(cli.EntryPoints).Triggers, Assert.Single(manifest.EntryPoints).Triggers);
    }

    [Fact]
    public void UnknownIdentityCannotPassJustBecauseATableIsNotGoverned()
    {
        var source = Source("""
            public class P : Plugin {
                public P() => RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation,
                    c => c.OrganizationService.Delete("not_governed", Guid.Empty)).SetUserContext(Guid.Parse("6f1c2a9e-0000-4000-8000-000000000001"));
            }
            """);
        var manifest = source.Build([], EntryPointSelector.Plugin("P"));
        var options = new CoverageOptions { Roles = RoleSet.Combine([], "test") };
        Assert.True(CoverageEvaluator.Evaluate(manifest, options).Summary.Passed(strict: true)); // CLI parity
        var result = new PermissionCheckResult(manifest, CoverageEvaluator.EvaluateForPermissionCheck(manifest, options));
        Assert.False(result.Passed);
        Assert.Equal(1, result.Coverage.Summary.NotChecked);
        Assert.Throws<PermissionCheckException>(result.ThrowIfFailed);
    }

    [Fact]
    public void ResultIsAlwaysStrictAndExceptionIncludesDiagnostics()
    {
        var source = Source("class C { public void Run(IOrganizationService s) { s.Create(new Account()); s.Execute(new OrganizationRequest(\"unknown\")); } }");
        source.AddEntryMethods(["C.Run"]);
        var manifest = source.Build(["account"], EntryPointSelector.Methods(["C.Run"]));
        var roles = RoleSet.Combine([new RoleDefinition("Display role name", new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>>())], "test");
        var result = new PermissionCheckResult(manifest, CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = roles }));
        Assert.False(result.Passed);
        var error = Assert.Throws<PermissionCheckException>(result.ThrowIfFailed);
        Assert.Same(result, error.Result);
        Assert.Contains("Display role name", error.Message);
        Assert.Contains("Code.cs", error.Message);
        Assert.Contains("Unresolved", error.Message);
        Assert.Contains("account Create", error.Message);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void AllIncompleteCategoriesFail(bool unresolved, bool uncheckedIdentity, bool unavailable, bool truncated)
    {
        var summary = new CoverageSummary(0, 0, 0, 0, uncheckedIdentity ? 1 : 0, 0, unresolved ? 1 : 0, 0, 0, 0, unavailable ? 1 : 0, truncated ? 1 : 0);
        var result = new PermissionCheckResult(new Manifest([], []), new CoverageReport(new Dictionary<string, SectionCoverage>(), new Dictionary<string, SectionCoverage>(), summary, [], [], false));
        Assert.Equal(!summary.Incomplete, result.Passed);
        if (summary.Incomplete) Assert.Throws<PermissionCheckException>(result.ThrowIfFailed);
        else result.ThrowIfFailed();
    }

    [Fact]
    public async Task WorkspaceLoadDoesNotSilentlyIgnoreCompilationErrors()
    {
        var directory = Path.Combine(Path.GetTempPath(), "permission-sdk-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var project = Path.Combine(directory, "BrokenSource.csproj");
            var sdkAssembly = System.Security.SecurityElement.Escape(typeof(Microsoft.Xrm.Sdk.IPlugin).Assembly.Location);
            await File.WriteAllTextAsync(project, $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <ItemGroup><Reference Include="Microsoft.Xrm.Sdk"><HintPath>{sdkAssembly}</HintPath></Reference></ItemGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, "Broken.cs"), """
                using System;
                using Microsoft.Xrm.Sdk;
                public class P : IPlugin {
                    public void Execute(IServiceProvider provider) { ThisMethodDoesNotExist(); }
                }
                """);
            var start = new System.Diagnostics.ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
            {
                Arguments = $"restore \"{project}\" --ignore-failed-sources",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var restore = System.Diagnostics.Process.Start(start)!;
            var output = await restore.StandardOutput.ReadToEndAsync();
            var errors = await restore.StandardError.ReadToEndAsync();
            await restore.WaitForExitAsync();
            Assert.True(restore.ExitCode == 0, output + errors);
            var analysis = await PermissionAnalysis.LoadAsync(project);
            var manifest = analysis.Analyze(PermissionScope.ForOperation("Create", "task"), new PermissionMetadata());
            Assert.Contains(Assert.Single(manifest.EntryPoints).Unresolved, u => u.Operation == "source compilation" && u.Reason.Contains("ThisMethodDoesNotExist"));
            var result = new PermissionCheckResult(manifest, CoverageEvaluator.Evaluate(manifest, new CoverageOptions
            {
                Roles = RoleSet.Combine([], "test")
            }));
            Assert.False(result.Passed);
            Assert.Throws<PermissionCheckException>(result.ThrowIfFailed);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static ManifestBuilder Source(string code)
    {
        var builder = new ManifestBuilder(Analyze.Root);
        Assert.True(builder.Add(Analyze.Compile(code)));
        return builder;
    }
}
