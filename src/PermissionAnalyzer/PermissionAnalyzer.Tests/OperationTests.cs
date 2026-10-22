using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// User operations given with <c>--operation</c>: the privileges the request needs as the caller, and the plugin
/// steps it fires.
/// </summary>
public class OperationTests
{
    private const string UpdateStep = "plugin:Contoso.OrderPlugin:Update:ctx_order:PostOperation";
    private const string CreateStep = "plugin:Contoso.OrderPlugin:Create:ctx_order:PostOperation";

    /// <summary>
    /// An order Update step filtered on the status, which deletes a lead, and a Create step, which creates a task.
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
                    RegisterPluginStep<Order>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Entity("task")));
                }
            }

            [EntityLogicalName("ctx_order")]
            public class Order : Entity
            {
                public Order() : base("ctx_order") { }

                [AttributeLogicalName("ctx_status")]
                public int? Status { get => GetAttributeValue<int?>("ctx_status"); set => this["ctx_status"] = value; }
            }
        }
        """;

    [Fact]
    public void UpdateOfAFilteredColumnFiresTheStep()
    {
        var operation = Run("Update", "ctx_order", ["CTX_Status"]);

        Assert.Equal("operation:Update ctx_order(ctx_status)", operation.Id);
        Assert.Equal(EntryPointKind.Operation, operation.Kind);
        Assert.Equal([UpdateStep], operation.Triggers);
        Assert.Equal(["ctx_order:Write", "lead:Delete"], operation.Requirements());
        Assert.All(operation.Requirements, r => Assert.Equal(RunsAs.Caller, r.RunsAs));
        Assert.All(operation.Requirements, r => Assert.Equal(RequirementBasis.Direct, r.Basis));
    }

    [Fact]
    public void UpdateOfOtherColumnsDoesNotFireTheStep()
    {
        var operation = Run("Update", "ctx_order", ["ctx_name"]);

        Assert.Empty(operation.Triggers!);
        Assert.Equal(["ctx_order:Write"], operation.Requirements());
    }

    [Fact]
    public void UpdateWithoutAttributesFiresFilteredStepsAsFilterUnknown()
    {
        var operation = Run("Update", "ctx_order");

        Assert.Equal("operation:Update ctx_order", operation.Id);
        Assert.Equal([UpdateStep], operation.Triggers);
        Assert.Equal(RequirementBasis.FilterUnknown, Assert.Single(operation.Requirements, r => r.Table == "lead").Basis);
    }

    [Fact]
    public void UpsertFiresCreateAndUpdateSteps()
    {
        var operation = Run("UpsertRequest", "ctx_order", ["ctx_status"]);

        Assert.Equal([CreateStep, UpdateStep], operation.Triggers);
        Assert.Equal(["ctx_order:Create", "ctx_order:Write", "lead:Delete", "task:Create"], operation.Requirements());
    }

    [Fact]
    public void LookupColumnsNeedAppendAndAppendTo()
    {
        var operation = Run(
            "Create",
            "ctx_order",
            ["ctx_customerid", "ctx_ownerteamid"],
            [new LookupColumn("ctx_order", "ctx_customerid", ["account", "contact"]), new LookupColumn("ctx_order", "ctx_ownerteamid", ["team"])]);

        Assert.Equal(
            ["account:AppendTo", "contact:AppendTo", "ctx_order:Create", "ctx_order:Append", "task:Create", "team:AppendTo"],
            operation.Requirements());
        Assert.Equal(RequirementBasis.AllLookupTargets, Assert.Single(operation.Requirements, r => r.Table == "account").Basis);
        Assert.Equal(RequirementBasis.Direct, Assert.Single(operation.Requirements, r => r.Table == "team").Basis);
    }

    [Fact]
    public void RequestMembersOtherThanTheTableAreUnresolved()
    {
        var operation = Run("Associate", "ctx_order");

        Assert.Equal(["ctx_order:Read", "ctx_order:AppendTo"], operation.Requirements());
        var unresolved = Assert.Single(operation.Unresolved);
        Assert.Equal("RelatedEntities of the Associate request isn't known", unresolved.Reason);
        Assert.Equal([Privilege.Read, Privilege.Append], unresolved.Privileges);
        Assert.Equal("--operation", unresolved.Source.Location.ToString());
    }

    [Fact]
    public void MessageWithoutARuleIsUnresolved()
    {
        var operation = Run("ctx_Recalculate", "ctx_order");

        Assert.Empty(operation.Requirements);
        Assert.Equal("custom API 'ctx_Recalculate' is called; its privileges are not analyzed yet", Assert.Single(operation.Unresolved).Reason);
    }

    [Fact]
    public void StepsFiredByTheOperationHaveItsPath()
    {
        var operation = Run("Update", "ctx_order", ["ctx_status"]);

        var lead = Assert.Single(Assert.Single(operation.Requirements, r => r.Table == "lead").Sources);
        Assert.Equal(["Update ctx_order", UpdateStep], lead.Path.Take(2));
    }

    private static EntryPoint Run(string message, string table, IReadOnlyList<string>? columns = null, IEnumerable<LookupColumn>? lookups = null)
    {
        var manifest = Analyze.Code(
            Plugins,
            scope: EntryPointSelector.Operation($"{message} {table}"),
            setup: builder => builder.AddOperation(message, table, columns),
            lookups: lookups);
        return Assert.Single(manifest.EntryPoints);
    }
}
