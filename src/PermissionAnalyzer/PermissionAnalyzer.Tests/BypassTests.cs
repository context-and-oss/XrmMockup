using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Requests that bypass custom plugin steps: they need a privilege to, and don't run the steps they bypass.
/// </summary>
public class BypassTests
{
    private const string Importer = "method:Contoso.Importer.Run(IOrganizationService)";

    /// <summary>
    /// A synchronous account Create step that deletes a lead, an asynchronous one that deletes a task, and an importer
    /// that creates an account with a request that <paramref name="parameters"/> sets up.
    /// </summary>
    private static EntryPoint Run(string parameters, Action<Core.Analysis.ManifestBuilder>? setup = null) =>
        Analyze.Code($$"""
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin()
                    {
                        RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("lead", Guid.Empty));
                        RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreOperation, c => c.OrganizationService.Delete("task", Guid.Empty))
                            .SetExecutionMode(ExecutionMode.Asynchronous);
                    }
                }

                public class Importer
                {
                    public void Run(IOrganizationService service)
                    {
                        var request = new CreateRequest { Target = new Account() };
                        {{parameters}}
                        service.Execute(request);
                    }
                }
            }
            """,
            setup: builder =>
            {
                setup?.Invoke(builder);
                builder.AddEntryMethods(["Contoso.Importer.Run"]);
            }).Entry(Importer);

    private static IReadOnlyList<string> Globals(EntryPoint entryPoint) => entryPoint.GlobalRequirements.Select(r => r.Privilege).ToList();

    [Fact]
    public void RequestWithoutBypassRunsEveryStep()
    {
        var run = Run("");

        Assert.Equal(["account:Create", "lead:Delete", "task:Delete"], run.Requirements());
        Assert.Empty(run.GlobalRequirements);
    }

    [Fact]
    public void BypassCustomPluginExecutionSkipsSynchronousSteps()
    {
        var run = Run("""request.Parameters["BypassCustomPluginExecution"] = true;""");

        Assert.Equal(["account:Create", "task:Delete"], run.Requirements());
        Assert.Equal(["prvBypassCustomPlugins"], Globals(run));
    }

    [Theory]
    [InlineData("CustomSync", new[] { "account:Create", "task:Delete" })]
    [InlineData("CustomAsync", new[] { "account:Create", "lead:Delete" })]
    [InlineData("CustomSync,CustomAsync", new[] { "account:Create" })]
    public void BypassBusinessLogicExecutionSkipsTheStepsItNames(string value, string[] requirements)
    {
        var run = Run($$"""request.Parameters.Add("BypassBusinessLogicExecution", "{{value}}");""");

        Assert.Equal(requirements, run.Requirements());
        Assert.Equal(["prvBypassCustomBusinessLogic"], Globals(run));
    }

    [Theory]
    [InlineData("""request.Parameters["BypassCustomPluginExecution"] = Environment.UserName == "admin";""", "prvBypassCustomPlugins")]
    [InlineData("""request.Parameters["BypassBusinessLogicExecution"] = Environment.GetEnvironmentVariable("BYPASS");""", "prvBypassCustomBusinessLogic")]
    [InlineData("""request.Parameters["BypassBusinessLogicExecutionStepIds"] = "7d1c4f3a-0000-4000-8000-000000000001";""", "prvBypassCustomBusinessLogic")]
    public void BypassThatIsntKnownNeedsThePrivilegeButSkipsNoStep(string parameters, string privilege)
    {
        var run = Run(parameters);

        Assert.Equal(["account:Create", "lead:Delete", "task:Delete"], run.Requirements());
        Assert.Equal([privilege], Globals(run));
    }

    [Fact]
    public void BypassSetToFalseNeedsNothing()
    {
        var run = Run("""request.Parameters["BypassCustomPluginExecution"] = false;""");

        Assert.Equal(["account:Create", "lead:Delete", "task:Delete"], run.Requirements());
        Assert.Empty(run.GlobalRequirements);
    }

    [Fact]
    public void ModeOfAStepFromTheMetadataIsUsed()
    {
        var run = Run(
            """request.Parameters["BypassBusinessLogicExecution"] = "CustomAsync";""",
            builder => builder.AddPluginSteps(
            [
                new PluginStepRegistration("Contoso.AccountPlugin", "Test", "Create", "account", 40, null, Asynchronous: true),
                new PluginStepRegistration("Other.ContactPlugin", "Other", "Create", "account", 40, null, Asynchronous: true),
            ]));

        Assert.Equal(["account:Create"], run.Requirements());
        Assert.Empty(run.Unresolved);
    }

    [Fact]
    public void CustomApiCodeRunsWhateverIsBypassed()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class Api : Plugin
                {
                    public Api() => RegisterCustomAPI("ctx_Recalculate", c => c.OrganizationService.Update(new Account()));
                }

                public class Importer
                {
                    public void Run(IOrganizationService service)
                    {
                        var request = new OrganizationRequest("ctx_Recalculate");
                        request.Parameters["BypassBusinessLogicExecution"] = "CustomSync,CustomAsync";
                        service.Execute(request);
                    }
                }
            }
            """,
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));
        var run = manifest.Entry(Importer);

        Assert.Equal(["account:Write"], run.Requirements());
        Assert.Equal(["prvBypassCustomBusinessLogic"], Globals(run));
    }
}
