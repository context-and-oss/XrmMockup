using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Update steps filtered on columns, which an update fires only when it sets one of them.
/// </summary>
public class FilteredAttributeTests
{
    private const string Step = "plugin:Contoso.OrderPlugin:Update:ctx_order:PostOperation";

    /// <summary>
    /// An order Update step filtered on the status, which deletes a lead.
    /// </summary>
    private const string Plugins = """
        namespace Contoso
        {
            public class OrderPlugin : Plugin
            {
                public OrderPlugin()
                {
                    RegisterPluginStep<Order>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("lead", Guid.Empty))
                        .AddFilteredAttributes(o => o.Status);
                }
            }

            [EntityLogicalName("ctx_order")]
            public class Order : Entity
            {
                public Order() : base("ctx_order") { }

                [AttributeLogicalName("ctx_name")]
                public string? Name { get => GetAttributeValue<string>("ctx_name"); set => this["ctx_name"] = value; }

                [AttributeLogicalName("ctx_status")]
                public int? Status { get => GetAttributeValue<int?>("ctx_status"); set => this["ctx_status"] = value; }
            }
        }
        """;

    [Fact]
    public void UpdateOfOtherColumnsDoesNotFireTheStep()
    {
        var run = Run("""
            public void Run(IOrganizationService service)
            {
                var order = new Order { Id = Guid.NewGuid(), Name = "Renamed" };
                order["ctx_description"] = "Changed";
                service.Update(order);
            }
            """);

        Assert.Empty(run.Triggers);
        Assert.Equal(["ctx_order:Write"], run.Requirements());
    }

    [Fact]
    public void UpdateOfAFilteredColumnFiresTheStep()
    {
        var run = Run("""
            public void Run(IOrganizationService service)
            {
                var order = new Entity("ctx_order", Guid.NewGuid());
                order["ctx_status"] = 2;
                service.Update(order);
            }
            """);

        Assert.Equal([Step], run.Triggers);
        Assert.Equal(RequirementBasis.Direct, Assert.Single(run.Requirements, r => r.Table == "lead").Basis);
    }

    [Fact]
    public void UpdateOfUnknownColumnsFiresTheStepAsFilterUnknown()
    {
        var run = Run("public void Run(IOrganizationService service, Order order) => service.Update(order);", "Order");

        Assert.Equal([Step], run.Triggers);
        Assert.Equal(RequirementBasis.FilterUnknown, Assert.Single(run.Requirements, r => r.Table == "lead").Basis);
        Assert.Equal(RequirementBasis.Direct, Assert.Single(run.Requirements, r => r.Table == "ctx_order").Basis);
    }

    [Fact]
    public void EntityPassedToAMethodThatCanSetMoreColumnsIsFilterUnknown()
    {
        var run = Run("""
            public void Run(IOrganizationService service)
            {
                var order = new Order { Name = "Renamed" };
                Fill(order);
                service.Update(order);
            }

            private static void Fill(Order order) => order.Status = 2;
            """);

        Assert.Equal(RequirementBasis.FilterUnknown, Assert.Single(run.Requirements, r => r.Table == "lead").Basis);
    }

    [Fact]
    public void ColumnsAreFollowedIntoTheMethodThatUpdates()
    {
        var run = Run("""
            public void Run(IOrganizationService service)
            {
                Save(service, new Order { Name = "Renamed" });
                var order = new Order();
                order.Status = 2;
                Save(service, order);
            }

            private static void Save(IOrganizationService service, Order order) => service.Update(order);
            """);

        Assert.Equal([Step], run.Triggers);
        var lead = Assert.Single(run.Requirements, r => r.Table == "lead");
        Assert.Equal(RequirementBasis.Direct, lead.Basis);
    }

    [Fact]
    public void RequestTargetColumnsDecideTheStepsItFires()
    {
        var update = Run("public void Run(IOrganizationService service) => service.Execute(new UpdateRequest { Target = new Order { Name = \"Renamed\" } });");
        Assert.Empty(update.Triggers);

        var upsert = Run("public void Run(IOrganizationService service) => service.Execute(new UpsertRequest { Target = new Order { Status = 2 } });");
        Assert.Equal([Step], upsert.Triggers);
    }

    [Fact]
    public void FilterFromTheMetadataApplies()
    {
        var manifest = Analyze.Code(
            """
            namespace Contoso
            {
                public class Importer
                {
                    public void Rename(IOrganizationService service) => service.Update(new Entity("account") { ["name"] = "Renamed" });

                    public void Close(IOrganizationService service) => service.Update(new Entity("account") { ["statecode"] = 1 });
                }
            }
            """,
            setup: builder =>
            {
                builder.AddPluginSteps([new PluginStepRegistration("Other.AccountPlugin", "Other", "Update", "account", 40, null, ["statecode"])]);
                builder.AddEntryMethods(["Contoso.Importer.Rename", "Contoso.Importer.Close"]);
            });

        Assert.Empty(manifest.Entry("method:Contoso.Importer.Rename(IOrganizationService)").Triggers);
        Assert.Equal(
            ["plugin:Other.AccountPlugin:Update:account:PostOperation"],
            manifest.Entry("method:Contoso.Importer.Close(IOrganizationService)").Triggers);
    }

    [Fact]
    public void FilterCanBeGivenByColumnName()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin() =>
                        RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Delete("lead", Guid.Empty))
                            .AddFilteredAttributes("Name");
                }

                public class Importer
                {
                    public void Run(IOrganizationService service) => service.Update(new Entity("account") { ["description"] = "Changed" });
                }
            }
            """,
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));

        var step = manifest.Entry("plugin:Contoso.AccountPlugin:Update:account:PostOperation");
        Assert.Equal(["name"], step.Trigger!.FilteredAttributes);
        Assert.Empty(manifest.Entry("method:Contoso.Importer.Run(IOrganizationService)").Triggers);
    }

    private static EntryPoint Run(string members, string parameter = "")
    {
        var manifest = Analyze.Code(
            "namespace Contoso { public class Importer { " + members + " } }" + Plugins,
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));
        var suffix = parameter.Length > 0 ? ", " + parameter : "";
        return manifest.Entry($"method:Contoso.Importer.Run(IOrganizationService{suffix})");
    }
}
