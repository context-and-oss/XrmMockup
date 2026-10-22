using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class EntryPointTests
{
    private const string UserId = "6a1e5c4d-2b3f-4a5e-8c7d-9e0f1a2b3c4d";

    [Fact]
    public void PluginCoreStepRunsAsCaller()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin()
                    {
                        RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, context =>
                            context.OrganizationService.Create(new Contact()));
                    }
                }
            }
            """);

        var entry = manifest.Entry("plugin:Contoso.AccountPlugin:Update:account:PostOperation");
        Assert.Equal(EntryPointKind.PluginStep, entry.Kind);
        Assert.Equal("Contoso.AccountPlugin", entry.Type);
        Assert.Equal(new Trigger("Update", "account", "PostOperation"), entry.Trigger);
        Assert.Equal(["contact:Create as Caller"], entry.RequirementsWithIdentity());
        Assert.Empty(manifest.Unreached);
    }

    [Fact]
    public void AdminServiceRunsAsSystem()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>("Create", ExecutionStage.PreOperation, Execute);

                private void Execute(LocalPluginContext context)
                {
                    context.OrganizationAdminService.Delete("contact", Guid.Empty);
                    context.OrganizationService.Retrieve("account", Guid.Empty, new ColumnSet());
                }
            }
            """);

        var entry = manifest.Entry("plugin:P:Create:account:PreOperation");
        Assert.Equal(["account:Read as Caller", "contact:Delete as System"], entry.RequirementsWithIdentity());
    }

    [Fact]
    public void SetUserContextRunsAsThatUser()
    {
        var manifest = Analyze.Code($$"""
            public class P : Plugin
            {
                public P()
                {
                    RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Contact()))
                        .SetUserContext(new Guid("{{UserId}}"));
                }
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(UserId, entry.ImpersonatingUserId);
        Assert.Equal([$"contact:Create as User {UserId}"], entry.RequirementsWithIdentity());
    }

    [Fact]
    public void CallsAreFollowedWithServiceAndTableArguments()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, Execute);

                private void Execute(LocalPluginContext context) => new Helper().Touch(context.OrganizationService, "contact");
            }

            public class Helper
            {
                public void Touch(IOrganizationService service, string table) => service.Delete(table, Guid.Empty);
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        var requirement = Assert.Single(entry.Requirements);
        Assert.Equal(("contact", Privilege.Delete, RunsAs.Caller), (requirement.Table, requirement.Privilege, requirement.RunsAs));
        Assert.Equal(2, Assert.Single(requirement.Sources).Path.Count);
        Assert.Empty(entry.Unresolved);
        Assert.Empty(manifest.Unreached);
    }

    [Fact]
    public void InjectedServiceUsesRegisteredImplementation()
    {
        var manifest = Analyze.Code("""
            public interface IAccountService { void HandleCreate(); void HandleUpdate(); }

            public class AccountService(IOrganizationServiceFactory factory, IPluginExecutionContext context) : IAccountService
            {
                private readonly IOrganizationService service = factory.CreateOrganizationService(context.UserId);
                public void HandleCreate() => service.Create(new Contact());
                public void HandleUpdate() => service.Update(new Account());
            }

            public class OtherAccountService : IAccountService
            {
                public void HandleCreate() { }
                public void HandleUpdate() { }
            }

            public class P : Plugin
            {
                public P()
                {
                    RegisterStep<Account, IAccountService>(EventOperation.Create, ExecutionStage.PostOperation, s => s.HandleCreate());
                    RegisterStep<Account, IAccountService>(EventOperation.Update, ExecutionStage.PostOperation, nameof(IAccountService.HandleUpdate));
                }

                protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services) =>
                    services.AddScoped<IAccountService, AccountService>();
            }
            """);

        var create = manifest.Entry("plugin:P:Create:account:PostOperation");
        Assert.Equal(["contact:Create as Caller"], create.RequirementsWithIdentity());
        Assert.Equal(RequirementBasis.Direct, create.Requirements[0].Basis);

        var update = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["account:Write as Caller"], update.RequirementsWithIdentity());
    }

    [Fact]
    public void UnregisteredInterfaceCallReachesAllImplementations()
    {
        var manifest = Analyze.Code("""
            public interface IStore { void Save(IOrganizationService service); }
            public class AccountStore : IStore { public void Save(IOrganizationService service) => service.Create(new Account()); }
            public class ContactStore : IStore { public void Save(IOrganizationService service) => service.Create(new Contact()); }

            public class P : Plugin
            {
                private readonly IStore store;
                public P(IStore store)
                {
                    this.store = store;
                    RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, c => this.store.Save(c.OrganizationService));
                }
            }
            """);

        var entry = manifest.Entry("plugin:P:Create:account:PostOperation");
        Assert.Equal(["account:Create as Caller", "contact:Create as Caller"], entry.RequirementsWithIdentity());
        Assert.All(entry.Requirements, r => Assert.Equal(RequirementBasis.AllImplementations, r.Basis));
    }

    [Fact]
    public void CustomApiIsAnEntryPoint()
    {
        var manifest = Analyze.Code("""
            public class DoThing : Plugin
            {
                public DoThing() => RegisterCustomAPI("ctx_DoThing", c => c.OrganizationService.Create(new Account()));
            }
            """);

        var entry = manifest.Entry("custom-api:ctx_DoThing");
        Assert.Equal(EntryPointKind.CustomApi, entry.Kind);
        Assert.Equal(["account:Create as Caller"], entry.RequirementsWithIdentity());
    }

    [Fact]
    public void CodeActivityRunsAsCaller()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class Activity : System.Activities.CodeActivity
                {
                    protected override void Execute(System.Activities.CodeActivityContext context)
                    {
                        var execution = context.GetExtension<IPluginExecutionContext>();
                        var factory = context.GetExtension<IOrganizationServiceFactory>();
                        var service = factory.CreateOrganizationService(execution.UserId) as IOrganizationService;
                        service.Create(new Contact());
                    }
                }
            }
            """);

        var entry = manifest.Entry("activity:Contoso.Activity");
        Assert.Equal(EntryPointKind.CodeActivity, entry.Kind);
        Assert.Equal(["contact:Create as Caller"], entry.RequirementsWithIdentity());
    }

    [Fact]
    public void AzureFunctionRunsAsApplicationUser()
    {
        var manifest = Analyze.Code("""
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
            """);

        var process = manifest.Entry("function:ProcessOrder");
        Assert.Equal(EntryPointKind.Function, process.Kind);
        Assert.Equal("HttpTrigger", process.Trigger?.Binding);
        Assert.Equal(["account:Create as ApplicationUser"], process.RequirementsWithIdentity());

        var actForUser = manifest.Entry("function:ActForUser");
        Assert.Equal("TimerTrigger", actForUser.Trigger?.Binding);
        Assert.Equal(["contact:Delete as ImpersonatedCaller"], actForUser.RequirementsWithIdentity());
    }

    [Fact]
    public void FunctionReachesServicesInReferencedProject()
    {
        // A function app whose Dataverse calls are all in a shared library, registered with DI
        var library = Analyze.Compile("""
            namespace Contoso.Logic
            {
                public interface ITemplateService { void Delete(Guid id); }
                public class TemplateService(IOrganizationService service) : ITemplateService
                {
                    public void Delete(Guid id) => service.Update(new Account { Id = id });
                }
                public static class Handler
                {
                    public static async Task<object> HandleAsync(Func<Task<object>> action) => await action();
                }
                public static class Registration
                {
                    public static IServiceCollection AddLogic(this IServiceCollection services) => services.AddScoped<ITemplateService, TemplateService>();
                }
            }
            """, "Contoso.Logic");
        var functions = Analyze.Compile("""
            namespace Contoso.Functions
            {
                public class DeleteTemplate(Contoso.Logic.ITemplateService templates)
                {
                    [Microsoft.Azure.Functions.Worker.Function("DeleteTemplate")]
                    public Task<object> Run([Microsoft.Azure.Functions.Worker.HttpTrigger] object request, Guid id) =>
                        Contoso.Logic.Handler.HandleAsync(async () => { templates.Delete(id); return id; });
                }
            }
            """, "Contoso.Functions", library);

        var builder = new ManifestBuilder(Analyze.Root);
        Assert.True(builder.Add(library));
        Assert.True(builder.Add(functions));
        var manifest = builder.Build(["account"]);

        Assert.Equal(["account:Write as ApplicationUser (assumed)"], manifest.Entry("function:DeleteTemplate").RequirementsWithIdentity());
        Assert.Empty(manifest.Unreached);
    }

    [Fact]
    public void MethodsNoEntryPointReachesAreUnreached()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, c => Used(c.OrganizationService));
                private static void Used(IOrganizationService s) => s.Create(new Contact());
            }

            public class Unused
            {
                public void M(IOrganizationService s) => s.Delete("account", Guid.Empty);
            }
            """);

        Assert.Single(manifest.EntryPoints);
        var unreached = Assert.Single(manifest.Unreached);
        Assert.Equal("Unused.M(IOrganizationService)", unreached.Id);
        Assert.Equal(["account:Delete"], unreached.Requirements());
    }

    [Fact]
    public void PlainPluginGetsOneEntryPointPerMetadataStep()
    {
        const string source = """
            namespace Contoso
            {
                public class LegacyPlugin : IPlugin
                {
                    public void Execute(IServiceProvider serviceProvider)
                    {
                        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext))!;
                        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))!;
                        var service = factory.CreateOrganizationService(context.UserId);
                        service.Create(new Contact());
                    }
                }
            }
            """;

        var withoutSteps = Analyze.Code(source);
        var plugin = withoutSteps.Entry("plugin:Contoso.LegacyPlugin");
        Assert.Equal(EntryPointKind.Plugin, plugin.Kind);
        Assert.Equal(["contact:Create as Caller"], plugin.RequirementsWithIdentity());

        var withSteps = Analyze.Code(source, setup: builder => builder.AddPluginSteps(
        [
            new PluginStepRegistration("Contoso.LegacyPlugin", "Test", "Update", "account", 40, UserId.ToUpperInvariant()),
            new PluginStepRegistration("Contoso.LegacyPlugin", "Other", "Create", "contact", 20, null),
        ]));

        Assert.Equal(
            ["plugin:Contoso.LegacyPlugin:Create:contact:PreOperation", "plugin:Contoso.LegacyPlugin:Update:account:PostOperation"],
            withSteps.EntryPoints.Select(e => e.Id));
        var impersonated = withSteps.Entry("plugin:Contoso.LegacyPlugin:Update:account:PostOperation");
        Assert.Equal(UserId, impersonated.ImpersonatingUserId);
        Assert.Equal([$"contact:Create as User {UserId}"], impersonated.RequirementsWithIdentity());
    }

    [Fact]
    public void MetadataStepAddsImpersonationToCodeStep()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Contact()));
            }
            """,
            setup: builder => builder.AddPluginSteps([new PluginStepRegistration("P", "Test", "update", "account", 40, UserId)]));

        var entry = Assert.Single(manifest.EntryPoints);
        Assert.Equal("plugin:P:Update:account:PostOperation", entry.Id);
        Assert.Equal([$"contact:Create as User {UserId}"], entry.RequirementsWithIdentity());
    }

    [Fact]
    public void ScopeSelectsEntryPoints()
    {
        const string source = """
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin()
                    {
                        RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Contact()));
                        RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreOperation, c => c.OrganizationService.Delete("contact", Guid.Empty));
                    }
                }

                public class Importer
                {
                    public void Run(IOrganizationService service) => service.Update(new Account());
                    public void Run(IOrganizationService service, string table) => service.Delete(table, Guid.Empty);
                }
            }
            """;

        var plugin = Analyze.Code(source, scope: EntryPointSelector.Plugin("accountplugin:Update"));
        Assert.Equal(["plugin:Contoso.AccountPlugin:Update:account:PostOperation"], plugin.EntryPoints.Select(e => e.Id));
        Assert.Empty(plugin.Unreached);

        var stage = Analyze.Code(source, scope: EntryPointSelector.Plugin("Contoso.AccountPlugin:Create:PreOperation"));
        Assert.Equal(["plugin:Contoso.AccountPlugin:Create:account:PreOperation"], stage.EntryPoints.Select(e => e.Id));

        var methods = Analyze.Code(
            source,
            scope: EntryPointSelector.Methods(["Contoso.Importer.Run"]),
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));
        Assert.Equal(
            ["method:Contoso.Importer.Run(IOrganizationService)", "method:Contoso.Importer.Run(IOrganizationService, string)"],
            methods.EntryPoints.Select(e => e.Id));
        // The update fires the account plugin's Update step, which runs as the same identity
        Assert.Equal(["account:Write as ApplicationUser (assumed)", "contact:Create as ApplicationUser (assumed)"], methods.EntryPoints[0].RequirementsWithIdentity());
        Assert.Equal(["plugin:Contoso.AccountPlugin:Update:account:PostOperation"], methods.EntryPoints[0].Triggers);

        var none = Analyze.Code(source, scope: EntryPointSelector.Function("Missing"));
        Assert.Empty(none.EntryPoints);
    }

    [Fact]
    public void PluginOverridingBaseExecuteRunsTheOverride()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public abstract class PluginBase : IPlugin
                {
                    public virtual void Execute(IServiceProvider serviceProvider) { }
                }

                public class DaxifBase : IPlugin
                {
                    public void Execute(IServiceProvider serviceProvider) { }
                }

                public class Derived : DaxifBase { }

                public class ContactPlugin : PluginBase
                {
                    public override void Execute(IServiceProvider serviceProvider)
                    {
                        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))!;
                        factory.CreateOrganizationService(null).Create(new Contact());
                    }
                }
            }
            """);

        // DaxifBase is only a base class, so it gets no entry point of its own
        Assert.Equal(["plugin:Contoso.ContactPlugin", "plugin:Contoso.Derived"], manifest.EntryPoints.Select(e => e.Id));
        Assert.Equal(["contact:Create as System"], manifest.Entry("plugin:Contoso.ContactPlugin").RequirementsWithIdentity());
        Assert.Empty(manifest.Unreached);
    }
}
