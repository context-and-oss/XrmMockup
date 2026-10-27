using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Workflows from the metadata, which operations fire like plugin steps but whose steps aren't analyzed.
/// </summary>
public class WorkflowTests
{
    private const string UserId = "6f1c2a9e-0000-4000-8000-000000000001";

    private const string Importer = """
        namespace Contoso
        {
            public class Importer
            {
                public void Create(IOrganizationService service) => service.Create(new Entity("account"));

                public void Rename(IOrganizationService service) => service.Update(new Entity("account") { ["name"] = "Renamed" });

                public void Close(IOrganizationService service) => service.Update(new Entity("account") { ["statecode"] = 1 });
            }
        }
        """;

    [Fact]
    public void FiredRealTimeWorkflowIsUnresolved()
    {
        var manifest = Run(new WorkflowRegistration("Set credit limit", "account", OnCreate: true, OnDelete: false, null, RealTime: true, null));

        var run = manifest.Entry("method:Contoso.Importer.Create(IOrganizationService)");
        Assert.Equal(["account:Create"], run.Requirements());
        Assert.Equal(["workflow:Set credit limit:Create:account"], run.Triggers);
        var unresolved = Assert.Single(run.Unresolved);
        Assert.Equal("fires real-time workflow 'Set credit limit', whose steps aren't analyzed", unresolved.Reason);
        Assert.Equal(RunsAsKind.ApplicationUser, unresolved.RunsAs.Kind);
        Assert.Empty(unresolved.Privileges);
    }

    [Fact]
    public void UpdateFiresTheWorkflowOnlyForItsColumns()
    {
        var manifest = Run(new WorkflowRegistration("Notify owner", "account", OnCreate: false, OnDelete: false, ["statecode"], RealTime: true, null));

        Assert.Empty(manifest.Entry("method:Contoso.Importer.Create(IOrganizationService)").Unresolved);
        Assert.Empty(manifest.Entry("method:Contoso.Importer.Rename(IOrganizationService)").Unresolved);
        Assert.Equal(["workflow:Notify owner:Update:account"], manifest.Entry("method:Contoso.Importer.Close(IOrganizationService)").Triggers);
    }

    [Fact]
    public void BackgroundWorkflowRunsAsItsOwner()
    {
        var manifest = Run(new WorkflowRegistration("Send welcome", "account", OnCreate: true, OnDelete: false, null, RealTime: false, UserId));

        var unresolved = Assert.Single(manifest.Entry("method:Contoso.Importer.Create(IOrganizationService)").Unresolved);
        Assert.Equal("fires background workflow 'Send welcome', whose steps aren't analyzed", unresolved.Reason);
        Assert.Equal(RunsAs.User(UserId), unresolved.RunsAs);
    }

    [Fact]
    public void ResultCountsFiredWorkflows()
    {
        var manifest = Run(new WorkflowRegistration("Set credit limit", "account", OnCreate: true, OnDelete: false, null, RealTime: true, null));

        var summary = CoverageEvaluator.Evaluate(manifest, new CoverageOptions()).Summary;
        Assert.Equal(1, summary.UnresolvedWorkflow);
        Assert.False(summary.Passed(strict: true));
        Assert.Equal("Not checked: 1 unresolved operation (1 workflow whose steps aren't analyzed)", ReportText.NotChecked(summary with { NotChecked = 0 }));
    }

    private static Manifest Run(WorkflowRegistration workflow) =>
        Analyze.Code(Importer, setup: builder =>
        {
            builder.AddWorkflows([workflow]);
            builder.AddEntryMethods(["Contoso.Importer.Create", "Contoso.Importer.Rename", "Contoso.Importer.Close"]);
        });
}
