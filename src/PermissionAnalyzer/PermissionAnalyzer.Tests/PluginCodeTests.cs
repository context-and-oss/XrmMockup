using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Plugin code from another solution, added for cascades only, such as the plugins an Azure Function app fires.
/// </summary>
public class PluginCodeTests
{
    private const string Step = "plugin:Plugins.OrderPlugin:Create:ctx_order:PostOperation";

    private const string Function = """
        public class Functions
        {
            [Microsoft.Azure.Functions.Worker.Function("ProcessOrder")]
            public void Run([Microsoft.Azure.Functions.Worker.HttpTrigger] object request)
            {
                var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                client.Create(new Entity("ctx_order"));
            }
        }
        """;

    /// <summary>
    /// An order Create step, which deletes a lead, and a helper no step calls.
    /// </summary>
    private const string Plugins = """
        namespace Plugins
        {
            public class OrderPlugin : Plugin
            {
                public OrderPlugin() =>
                    RegisterPluginStep<Order>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("lead", Guid.Empty));
            }

            [EntityLogicalName("ctx_order")]
            public class Order : Entity
            {
                public Order() : base("ctx_order") { }
            }

            public class Unused
            {
                public void Run(IOrganizationService service) => service.Delete("task", Guid.Empty);
            }

            public class AccountPlugin : IPlugin
            {
                public void Execute(IServiceProvider serviceProvider) { }
            }
        }
        """;

    [Fact]
    public void StepsInPluginCodeRunWhenFiredButAreNotReported()
    {
        var manifest = Build();

        var function = Assert.Single(manifest.EntryPoints);
        Assert.Equal("function:ProcessOrder", function.Id);
        Assert.Equal([Step], function.Triggers);
        Assert.Equal(["ctx_order:Create", "lead:Delete"], function.Requirements());
        Assert.Empty(manifest.Unreached);
    }

    [Fact]
    public void PluginCodePathsAreRelativeToTheAnalyzedSolution()
    {
        var lead = Assert.Single(Build().Entry("function:ProcessOrder").Requirements, r => r.Table == "lead");

        Assert.Equal("../plugins/Code.cs", Assert.Single(lead.Sources).Location.File);
    }

    [Fact]
    public void ScopeSelectsEntryPointsInPluginCode()
    {
        var manifest = Build(EntryPointSelector.Plugin("OrderPlugin"));

        Assert.Equal(["lead:Delete"], Assert.Single(manifest.EntryPoints).Requirements());
    }

    [Fact]
    public void MetadataStepsOfPluginCodeAreNotReported()
    {
        var manifest = Build(setup: builder => builder.AddPluginSteps([new PluginStepRegistration("Plugins.AccountPlugin", "Plugins", "Update", "account", 40, null)]));

        Assert.Equal(["function:ProcessOrder"], manifest.EntryPoints.Select(e => e.Id));
        Assert.Equal(
            ["plugin:Plugins.AccountPlugin:Update:account:PostOperation"],
            Build(EntryPointSelector.Plugin("AccountPlugin"), builder => builder.AddPluginSteps([new PluginStepRegistration("Plugins.AccountPlugin", "Plugins", "Update", "account", 40, null)]))
                .EntryPoints.Select(e => e.Id));
    }

    [Fact]
    public void CodeInTheAnalyzedSolutionIsReportedEvenWhenAlsoAddedAsPluginCode()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        builder.AddPluginCode(Analyze.Compile(Plugins, "Plugins"));
        builder.Add(Analyze.Compile(Plugins, "Plugins"));

        var manifest = builder.Build();

        Assert.Contains(manifest.EntryPoints, e => e.Id == Step);
        Assert.Equal("Plugins.Unused.Run(IOrganizationService)", Assert.Single(manifest.Unreached).Id);
    }

    private static Manifest Build(EntryPointSelector? scope = null, Action<ManifestBuilder>? setup = null)
    {
        var builder = new ManifestBuilder(Analyze.Root);
        setup?.Invoke(builder);
        Assert.True(builder.Add(Analyze.Compile(Function, "Functions")));
        Assert.True(builder.AddPluginCode(Analyze.Compile(Plugins, "Plugins", "../plugins/Code.cs")));
        return builder.Build(scope: scope);
    }
}
