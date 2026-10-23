using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class ExecutePrivilegeTests
{
    private const string Importer = "method:Contoso.Importer.Run(IOrganizationService)";

    private static readonly PrivilegeDefinition ReadOrder = new("prvReadctx_Order", Privilege.Read, ["ctx_order"]);
    private static readonly PrivilegeDefinition ActOnBehalf = new("prvActOnBehalfOfAnotherUser", null, []);
    private static readonly PrivilegeDefinition PublishCustomization = new("prvPublishCustomization", null, ["solution"]);

    /// <summary>
    /// A custom API registered with <paramref name="configuration"/> and a method that calls it.
    /// </summary>
    private static Manifest Run(string configuration, Action<ManifestBuilder>? setup = null) =>
        Analyze.Code($$"""
            namespace Contoso
            {
                public class Api : Plugin
                {
                    public Api() => RegisterCustomAPI("ctx_Recalculate", c => c.OrganizationService.Update(new Account())){{configuration}};
                }

                public class Importer
                {
                    public void Run(IOrganizationService service) => service.Execute(new OrganizationRequest("ctx_Recalculate"));
                }
            }
            """,
            setup: builder =>
            {
                setup?.Invoke(builder);
                builder.AddEntryMethods(["Contoso.Importer.Run"]);
            });

    [Fact]
    public void ExecutePrivilegeOnAnEarlyBoundTableIsRequired()
    {
        var manifest = Run(".WithExecutePrivilege<Account>(XrmPluginCore.Enums.Privilege.Read)");
        var run = manifest.Entry(Importer);

        Assert.Equal(["account:Read", "account:Write"], run.Requirements());
        Assert.Equal(RequirementBasis.ExecutePrivilege, run.Requirements.Single(r => r.Privilege == Privilege.Read).Basis);
        Assert.Empty(run.Unresolved);
    }

    [Fact]
    public void ExecutePrivilegeOnASchemaNameIsRequiredOnTheLogicalName()
    {
        var run = Run(""".WithExecutePrivilege("ctx_Order", XrmPluginCore.Enums.Privilege.Write)""").Entry(Importer);

        Assert.Equal(["account:Write", "ctx_order:Write"], run.Requirements());
    }

    [Fact]
    public void CustomApiEntryPointNeedsItsExecutePrivilegeForTheCaller()
    {
        var api = Assert.Single(Run(".WithExecutePrivilege<Account>(XrmPluginCore.Enums.Privilege.Read)").EntryPoints, e => e.Kind == EntryPointKind.CustomApi);

        var requirement = Assert.Single(api.Requirements, r => r.Privilege == Privilege.Read);
        Assert.Equal(RunsAs.Caller, requirement.RunsAs);
        Assert.Empty(Assert.Single(requirement.Sources).Path);
    }

    [Fact]
    public void ExecutePrivilegeNameNeedsThePrivilegeCatalog()
    {
        var run = Run(""".WithExecutePrivilegeName("prvReadctx_Order")""").Entry(Importer);

        var unresolved = Assert.Single(run.Unresolved);
        Assert.Equal("ctx_Recalculate", unresolved.Operation);
        Assert.Equal(
            "custom API 'ctx_Recalculate' needs execute privilege 'prvReadctx_Order', which needs a privilege catalog (PrivilegeCatalog.xml) to look up",
            unresolved.Reason);
        Assert.Equal(["account:Write"], run.Requirements());
    }

    [Fact]
    public void ExecutePrivilegeNameIsLookedUpInThePrivilegeCatalog()
    {
        var run = Run(""".WithExecutePrivilegeName("PRVREADCTX_ORDER")""", b => b.AddPrivilegeCatalog([ReadOrder], [])).Entry(Importer);

        Assert.Equal(["account:Write", "ctx_order:Read"], run.Requirements());
        Assert.Empty(run.Unresolved);
    }

    [Fact]
    public void ExecutePrivilegeNotTiedToATableIsAGlobalRequirement()
    {
        var run = Run(""".WithExecutePrivilegeName("prvActOnBehalfOfAnotherUser")""", b => b.AddPrivilegeCatalog([ActOnBehalf], [])).Entry(Importer);

        var global = Assert.Single(run.GlobalRequirements);
        Assert.Equal(("prvActOnBehalfOfAnotherUser", RequirementBasis.ExecutePrivilege), (global.Privilege, global.Basis));
        Assert.Equal(["account:Write"], run.Requirements());
    }

    [Theory]
    [InlineData("""
        .WithExecutePrivilegeName("prvUnknown")
        """, "custom API 'ctx_Recalculate' needs execute privilege 'prvUnknown', which isn't in the privilege catalog")]
    [InlineData("""
        .WithExecutePrivilegeName("prvPublishCustomization")
        """, "custom API 'ctx_Recalculate' needs execute privilege 'prvPublishCustomization', whose access right isn't a table privilege")]
    [InlineData("""
        .WithExecutePrivilegeName(System.Environment.GetEnvironmentVariable("PRIVILEGE"))
        """, "custom API 'ctx_Recalculate' needs an execute privilege set at runtime")]
    public void ExecutePrivilegeThatCantBeLookedUpIsUnresolved(string configuration, string reason)
    {
        var run = Run(configuration, b => b.AddPrivilegeCatalog([ActOnBehalf, PublishCustomization], [])).Entry(Importer);

        Assert.Equal(reason, Assert.Single(run.Unresolved).Reason);
    }

    [Fact]
    public void CustomApiOnlyInTheCatalogNeedsItsExecutePrivilege()
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service)
                {
                    service.Execute(new OrganizationRequest("ctx_Export"));
                    service.Execute(new OrganizationRequest("ctx_Ping"));
                }
            }
            """,
            setup: b => b.AddPrivilegeCatalog([ReadOrder], [new CustomApiDefinition("ctx_Export", "prvReadctx_Order"), new CustomApiDefinition("ctx_Ping", null)]));
        var method = Assert.Single(manifest.Unreached);

        Assert.Equal(["ctx_order:Read"], method.Requirements());
        Assert.Equal(
            ["custom API 'ctx_Export' is called; its code isn't analyzed", "custom API 'ctx_Ping' is called; its code isn't analyzed"],
            method.Unresolved.Select(u => u.Reason));
    }

    [Fact]
    public void ImpersonationNeedsActOnBehalfOfAnotherUserOnTheImpersonator()
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
                    client.Create(new Account());
                }
            }
            """);
        var actForUser = manifest.Entry("function:ActForUser");

        var global = Assert.Single(actForUser.GlobalRequirements);
        Assert.Equal(
            (GlobalRequirement.ActOnBehalfOfAnotherUser, RunsAs.ApplicationUser, RequirementBasis.Direct),
            (global.Privilege, global.RunsAs, global.Basis));
        Assert.Equal(2, global.Sources.Count);
    }
}
