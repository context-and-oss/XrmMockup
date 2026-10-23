using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Requests that need a privilege by name, such as prvBulkDelete, rather than a privilege on the tables they act on.
/// </summary>
public class GlobalPrivilegeTests
{
    private static IReadOnlyList<string> Globals(MethodManifest method) =>
        method.GlobalRequirements.Select(r => $"{r.Privilege} as {r.RunsAs.Kind}").ToList();

    [Fact]
    public void RequestNeedsThePrivilegeItsRuleNames()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) =>
                    service.Execute(new BulkDeleteRequest { JobName = "Purge", QuerySet = [new QueryExpression("account")] });
            }
            """);

        var global = Assert.Single(method.GlobalRequirements);
        Assert.Equal(("prvBulkDelete", RunsAsKind.Caller, RequirementBasis.Direct), (global.Privilege, global.RunsAs.Kind, global.Basis));
        Assert.Empty(method.Requirements);
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void RequestCanNeedSeveralPrivileges()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) => service.Execute(new RetrieveAuditDetailsRequest { AuditId = Guid.Empty });
            }
            """);

        Assert.Equal(["prvReadAuditSummary as Caller", "prvReadRecordAuditHistory as Caller"], Globals(method));
    }

    [Fact]
    public void RequestByNameNeedsThePrivilegeToo()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) => service.Execute(new OrganizationRequest("PublishAllXml"));
            }
            """);

        Assert.Equal(["prvPublishCustomization as Caller"], Globals(method));
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void MergeNeedsPrvMergeAsWellAsTablePrivileges()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) =>
                    service.Execute(new MergeRequest { Target = new EntityReference("account", Guid.Empty), SubordinateId = Guid.Empty });
            }
            """);

        Assert.Equal(["prvMerge as Caller"], Globals(method));
        Assert.Equal(["account:Read", "account:Write"], method.Requirements());
    }

    [Fact]
    public void PrivilegeIsLookedUpInThePrivilegeCatalog()
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service)
                {
                    service.Execute(new BulkDeleteRequest());
                    service.Execute(new ExecuteWorkflowRequest { WorkflowId = Guid.Empty, EntityId = Guid.Empty });
                    service.Execute(new PublishAllXmlRequest());
                }
            }
            """,
            setup: builder => builder.AddPrivilegeCatalog(
            [
                new PrivilegeDefinition("PRVBULKDELETE", null, []),
                new PrivilegeDefinition("prvWorkflowExecution", Privilege.Read, ["workflow"]),
                new PrivilegeDefinition("prvPublishCustomization", null, ["solution"]),
            ], []));
        var method = Assert.Single(manifest.Unreached);

        // The catalog's spelling, a table privilege for one tied to a table, and unresolved where a role can't grant it
        Assert.Equal(["PRVBULKDELETE as Caller"], Globals(method));
        Assert.Equal(["workflow:Read"], method.Requirements());
        var unresolved = Assert.Single(method.Unresolved);
        Assert.Equal(("PublishAllXml", "the PublishAllXml request needs privilege 'prvPublishCustomization', whose access right isn't a table privilege"), (unresolved.Operation, unresolved.Reason));
    }

    [Fact]
    public void PrivilegeMissingFromThePrivilegeCatalogIsUnresolved()
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service) => service.Execute(new OrganizationRequest("ctx_Purge"));
            }
            """,
            setup: builder => builder.AddPrivilegeCatalog([new PrivilegeDefinition("prvBulkDelete", null, [])], []),
            rules: [new RequestRule("ctx_Purge") { GlobalPrivileges = ["prvBulkDelte"] }]);
        var method = Assert.Single(manifest.Unreached);

        Assert.Empty(method.GlobalRequirements);
        Assert.Equal("the ctx_Purge request needs privilege 'prvBulkDelte', which isn't in the privilege catalog", Assert.Single(method.Unresolved).Reason);
    }

    [Fact]
    public void AsRuleAddsItsPrivilegesAndAssumption()
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service) => service.Execute(new OrganizationRequest("ctx_PurgeAll"));
            }
            """,
            rules:
            [
                new RequestRule("ctx_Purge") { GlobalPrivileges = ["prvBulkDelete"], Assumption = "only closed records are purged" },
                new RequestRule("ctx_PurgeAll") { As = ["ctx_Purge"], GlobalPrivileges = ["prvDeleteRecordChangeHistory"] },
            ]);
        var method = Assert.Single(manifest.Unreached);

        Assert.Equal(["prvBulkDelete as Caller", "prvDeleteRecordChangeHistory as Caller"], Globals(method));
        Assert.Contains("only closed records are purged", method.Assumptions.Select(a => a.Reason));
    }

    [Fact]
    public void SendEmailAssumesItIsSentAsTheCaller()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) => service.Execute(new SendEmailRequest { EmailId = Guid.Empty, IssueSend = true });
            }
            """);

        Assert.Contains("SendEmail sends the email as the calling user; sending it as another user needs prvSendAsUser", method.Assumptions.Select(a => a.Reason));
        Assert.Empty(method.GlobalRequirements);
    }

    [Fact]
    public void SettingOverriddenCreatedOnNeedsPrvOverrideCreatedOnCreatedBy()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    service.Create(new Account { ["overriddencreatedon"] = DateTime.UtcNow.AddYears(-1) });
                    service.Execute(new CreateRequest { Target = new Contact { ["overriddencreatedon"] = DateTime.UtcNow } });
                    service.Create(new Account { ["name"] = "Contoso" });
                }
            }
            """);

        var global = Assert.Single(method.GlobalRequirements);
        Assert.Equal("prvOverrideCreatedOnCreatedBy", global.Privilege);
        // The two that set it, not the third Create
        Assert.Equal(2, global.Sources.Count);
    }

    [Fact]
    public void PrivilegeAStepNeedsIsNeededByTheEntryPointThatFiresIt()
    {
        var manifest = Analyze.Code("""
            namespace Contoso
            {
                public class AccountPlugin : Plugin
                {
                    public AccountPlugin() =>
                        RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, c => c.OrganizationService.Execute(new BulkDeleteRequest()));
                }

                public class Importer
                {
                    public void Run(IOrganizationService service) => service.Create(new Account());
                }
            }
            """,
            setup: builder => builder.AddEntryMethods(["Contoso.Importer.Run"]));
        var run = manifest.Entry("method:Contoso.Importer.Run(IOrganizationService)");

        var global = Assert.Single(run.GlobalRequirements);
        Assert.Equal("prvBulkDelete", global.Privilege);
        Assert.Contains("plugin:Contoso.AccountPlugin:Create:account:PostOperation", Assert.Single(global.Sources).Path);
    }
}
