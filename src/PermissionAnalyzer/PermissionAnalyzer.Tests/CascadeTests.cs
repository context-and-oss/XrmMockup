using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// The plugin steps and custom APIs that an entry point's operations fire, which run in the same request and need
/// their privileges too.
/// </summary>
public class CascadeTests
{
    private const string UserId = "6f1c2a9e-0000-4000-8000-000000000001";

    /// <summary>
    /// A contact Create step that updates an account, and an account Update step that deletes a lead.
    /// </summary>
    private const string Plugins = """
        namespace Contoso
        {
            public class ContactPlugin : Plugin
            {
                public ContactPlugin()
                {
                    RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Update(new Account()));
                    RegisterPluginStep<Contact>(EventOperation.Delete, ExecutionStage.PreOperation, c => c.OrganizationService.Delete("task", Guid.Empty));
                }
            }

            public class AccountPlugin : Plugin
            {
                public AccountPlugin() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("lead", Guid.Empty));
            }
        }
        """;

    [Fact]
    public void FunctionFiresStepsAsItsApplicationUser()
    {
        var manifest = Analyze.Code("""
            public class Functions
            {
                [Microsoft.Azure.Functions.Worker.Function("ProcessOrder")]
                public void Run([Microsoft.Azure.Functions.Worker.HttpTrigger] object request)
                {
                    var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                    client.Create(new Contact());
                }
            }
            """ + Plugins);
        var function = manifest.Entry("function:ProcessOrder");

        Assert.Equal(
            ["account:Write as ApplicationUser", "contact:Create as ApplicationUser", "lead:Delete as ApplicationUser"],
            function.RequirementsWithIdentity());
        Assert.Equal(
            ["plugin:Contoso.AccountPlugin:Update:account:PostOperation", "plugin:Contoso.ContactPlugin:Create:contact:PostOperation"],
            function.Triggers);
        Assert.All(function.Requirements, r => Assert.Equal(RequirementBasis.Direct, r.Basis));

        var lead = Assert.Single(function.Requirements, r => r.Table == "lead");
        Assert.Equal(
            [
                "Functions.Run",
                "Create contact",
                "plugin:Contoso.ContactPlugin:Create:contact:PostOperation",
                "ContactPlugin.ctor (lambda, line 32)",
                "Update account",
                "plugin:Contoso.AccountPlugin:Update:account:PostOperation",
                "AccountPlugin.ctor (lambda, line 39)",
            ],
            Assert.Single(lead.Sources).Path);
    }

    [Fact]
    public void StepsFiredForAnotherUserRunAsThatUser()
    {
        var manifest = Analyze.Code("""
            public class Functions
            {
                [Microsoft.Azure.Functions.Worker.Function("ActForUser")]
                public static void ActForUser([Microsoft.Azure.Functions.Worker.TimerTrigger("0 * * * *")] object timer, Guid userId)
                {
                    var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                    client.CallerId = userId;
                    client.Delete("contact", Guid.Empty);
                }
            }
            """ + Plugins);

        Assert.Equal(
            ["contact:Delete as ImpersonatedCaller", "task:Delete as ImpersonatedCaller"],
            manifest.Entry("function:ActForUser").RequirementsWithIdentity());
    }

    [Fact]
    public void ImpersonatingStepRunsAsItsUser()
    {
        var manifest = Analyze.Code(
            """
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Entity("lead")));
                }

                public class Importer
                {
                    public void Run(IOrganizationService service) => service.Update(new Account());
                }
            }
            """,
            setup: builder =>
            {
                builder.AddPluginSteps([new PluginStepRegistration("Contoso.AccountPlugin", "Test", "Update", "account", 40, UserId)]);
                builder.AddEntryMethods(["Contoso.Importer.Run"]);
            });

        var run = manifest.Entry("method:Contoso.Importer.Run(IOrganizationService)");
        Assert.Equal(["account:Write as ApplicationUser (assumed)", $"lead:Create as User {UserId}"], run.RequirementsWithIdentity());
    }

    [Fact]
    public void RequestFiresTheStepsOfEachOfItsMessages()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin()
                    {
                        RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("lead", Guid.Empty));
                        RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("task", Guid.Empty));
                        RegisterPluginStep<Account>("Upsert", ExecutionStage.PostOperation, c => c.OrganizationService.Delete("email", Guid.Empty));
                        RegisterPluginStep<Account>(EventOperation.Delete, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("fax", Guid.Empty));
                    }
                }

                public class Importer
                {
                    public void Run(IOrganizationService service) => Save(service, new UpsertRequest { Target = new Account() });

                    static void Save(IOrganizationService service, OrganizationRequest request) => service.Execute(request);
                }
            }
            """,
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));

        var run = manifest.Entry("method:Contoso.Importer.Run(IOrganizationService)");
        Assert.Equal(["account:Create", "account:Write", "email:Delete", "lead:Delete", "task:Delete"], run.Requirements());
        Assert.Equal(3, run.Triggers.Count);
    }

    [Fact]
    public void StepsThatFireEachOtherAreExpandedOnce()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c =>
                    {
                        c.OrganizationService.Update(new Account());
                        c.OrganizationService.Update(new Contact());
                    });
                }

                public class ContactPlugin : Plugin
                {
                    public ContactPlugin() => RegisterPluginStep<Contact>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Update(new Account()));
                }
            }
            """);

        var account = manifest.Entry("plugin:Contoso.AccountPlugin:Update:account:PostOperation");
        Assert.Equal(["account:Write", "contact:Write"], account.Requirements());
        Assert.Equal(
            ["plugin:Contoso.AccountPlugin:Update:account:PostOperation", "plugin:Contoso.ContactPlugin:Update:contact:PostOperation"],
            account.Triggers);
    }

    [Fact]
    public void OperationOnAnUnknownTableFiresEveryStepOfItsMessage()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class Importer
                {
                    public void Run(IOrganizationService service, Entity entity) => service.Delete(entity.LogicalName, entity.Id);
                }
            }
            """ + Plugins,
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));

        var run = manifest.Entry("method:Contoso.Importer.Run(IOrganizationService, Entity)");
        var task = Assert.Single(run.Requirements);
        Assert.Equal(("task", Privilege.Delete, RequirementBasis.CascadeUnknown), (task.Table, task.Privilege, task.Basis));
        Assert.Equal(["plugin:Contoso.ContactPlugin:Delete:contact:PreOperation"], run.Triggers);
        Assert.Single(run.Unresolved, u => u.Operation == "Delete");
    }

    [Fact]
    public void MetadataStepWithoutCodeIsAFinding()
    {
        var manifest = Analyze.Code(
            """
            namespace Contoso
            {
                public class Importer
                {
                    public void Run(IOrganizationService service) => service.Create(new Account());
                }
            }
            """,
            setup: builder =>
            {
                builder.AddPluginSteps(
                [
                    new PluginStepRegistration("Other.AccountPlugin", "Other", "Create", "account", 20, null),
                    new PluginStepRegistration("Other.ContactPlugin", "Other", "Create", "contact", 20, null),
                ]);
                builder.AddEntryMethods(["Contoso.Importer.Run"]);
            });

        var run = manifest.Entry("method:Contoso.Importer.Run(IOrganizationService)");
        Assert.Equal(["account:Create"], run.Requirements());
        Assert.Equal(["plugin:Other.AccountPlugin:Create:account:PreOperation"], run.Triggers);
        var unresolved = Assert.Single(run.Unresolved);
        Assert.Equal("fires plugin step plugin:Other.AccountPlugin:Create:account:PreOperation, whose code isn't analyzed", unresolved.Reason);
        Assert.Empty(unresolved.Privileges);
    }

    [Fact]
    public void CustomApiCallRunsItsCode()
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
                    public void Run(IOrganizationService service) => service.Execute(new OrganizationRequest("ctx_Recalculate"));
                }
            }
            """,
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));

        var run = manifest.Entry("method:Contoso.Importer.Run(IOrganizationService)");
        Assert.Equal(["account:Write"], run.Requirements());
        Assert.Single(run.Triggers);
        Assert.StartsWith("custom-api:", run.Triggers[0]);
    }
}
