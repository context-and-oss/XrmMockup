using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Options;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Requests passed to <c>Execute</c>, read through the rule for their message.
/// </summary>
public class ExecuteRuleTests
{
    [Fact]
    public void TypedRequestUsesItsRule()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id)
                {
                    service.Execute(new CreateRequest { Target = new Account() });
                    service.Execute(new AssignRequest { Target = new EntityReference("contact", id) });
                }
            }
            """);

        Assert.Equal(["account:Create", "contact:Write", "contact:Assign"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void UpsertNeedsCreateAndWrite()
    {
        var method = Analyze.Method("""
            class C { void M(IOrganizationService service) => service.Execute(new UpsertRequest { Target = new Contact() }); }
            """);

        Assert.Equal(["contact:Create", "contact:Write"], method.Requirements());
    }

    [Fact]
    public void RequestNeedingNoPrivilegeIsNotReported()
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service)
                {
                    service.Execute(new WhoAmIRequest());
                    service.Execute(new OrganizationRequest("RetrieveVersion"));
                }
            }
            """);

        Assert.Empty(manifest.AllRequirements);
        Assert.Empty(manifest.Unreached.SelectMany(m => m.Unresolved));
    }

    [Fact]
    public void MembersSetThroughLocalAreRead()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id)
                {
                    var request = new OrganizationRequest("Delete");
                    request["Target"] = new EntityReference("contact", id);
                    service.Execute(request);

                    var update = new UpdateRequest();
                    update.Target = new Account();
                    service.Execute(update);

                    var retrieve = new OrganizationRequest { RequestName = "Retrieve" };
                    retrieve.Parameters.Add("Target", new EntityReference("account", id));
                    service.Execute(retrieve);
                }
            }
            """);

        Assert.Equal(["account:Read", "account:Write", "contact:Delete"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void ExecuteMultipleReadsEveryRequest()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, List<Account> accounts, Guid id)
                {
                    var batch = new ExecuteMultipleRequest { Requests = new OrganizationRequestCollection() };
                    foreach (var account in accounts) batch.Requests.Add(new UpdateRequest { Target = account });
                    batch.Requests.Add(new DeleteRequest { Target = new EntityReference("contact", id) });
                    service.Execute(batch);
                }
            }
            """);

        Assert.Equal(["account:Write", "contact:Delete"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void RequestsPassThroughBatchingAndHelpers()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => Run(c.OrganizationService, Guid.Empty));

                static void Run(IOrganizationService service, Guid id)
                {
                    var requests = new List<OrganizationRequest>();
                    requests.Add(new CreateRequest { Target = new Account() });
                    requests.AddRange(Deletes(id));
                    foreach (var chunk in requests.Chunk(100)) Send(service, chunk);
                }

                static IEnumerable<OrganizationRequest> Deletes(Guid id) => [new DeleteRequest { Target = new EntityReference("contact", id) }];

                static void Send(IOrganizationService service, IEnumerable<OrganizationRequest> requests)
                {
                    var transaction = new ExecuteTransactionRequest { Requests = new OrganizationRequestCollection() };
                    transaction.Requests.AddRange(requests);
                    service.Execute(transaction);
                }
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["account:Create", "contact:Delete"], entry.Requirements());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void EntitiesPassThroughListsAndLoops()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    var records = new List<Entity> { new Account() };
                    records.Add(new Entity("lead"));
                    foreach (var record in records) service.Update(record);
                    records.ForEach(r => service.Delete(r.LogicalName, r.Id));
                }
            }
            """);

        Assert.Equal(["account:Write", "account:Delete", "lead:Write", "lead:Delete"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void RequestsBuiltBySelectAreRead()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid[] ids)
                {
                    var requests = new OrganizationRequestCollection();
                    requests.AddRange(ids.Select(id => new DeleteRequest { Target = new EntityReference("contact", id) }));
                    service.Execute(new ExecuteMultipleRequest { Requests = requests });
                }
            }
            """);

        Assert.Equal(["contact:Delete"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void MultipleRequestsReadTheirTargets()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, List<Contact> contacts)
                {
                    service.Execute(new CreateMultipleRequest { Targets = new EntityCollection { EntityName = "account" } });
                    service.Execute(new UpsertMultipleRequest { Targets = new EntityCollection(contacts.Cast<Entity>().ToList()) });
                }
            }
            """);

        Assert.Equal(["account:Create", "contact:Create", "contact:Write"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void RequestParameterResolvesPerCall()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation,
                    c => Send(c.OrganizationService, new UpdateRequest { Target = new Contact() }));

                static void Send(IOrganizationService service, OrganizationRequest request) => service.Execute(request);
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["contact:Write"], entry.Requirements());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void UnboundRequestParameterIsReportedOnce()
    {
        var manifest = Analyze.Code("""
            public static class Extensions
            {
                public static void Send(this IOrganizationService service, OrganizationRequest request) => service.Execute(request);
            }
            """);

        var unresolved = Assert.Single(Assert.Single(manifest.Unreached).Unresolved);
        Assert.Equal("request comes from parameter 'request'", unresolved.Reason);
        Assert.Empty(unresolved.Privileges);
    }

    [Fact]
    public void RequestWithoutRuleIsReported()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, string name)
                {
                    service.Execute(new AddMembersTeamRequest());
                    service.Execute(new OrganizationRequest(name));
                }
            }
            """);

        Assert.Equal(["no rule for message 'AddMembersTeam'", "request name is computed at runtime"], method.Unresolved.Select(u => u.Reason).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void UnknownTargetIsReportedWithItsPrivileges()
    {
        var method = Analyze.Method("""
            class C
            {
                private Entity _target = null!;

                void M(IOrganizationService service) => service.Execute(new AssignRequest { Target = _target.ToEntityReference() });
            }
            """);

        var unresolved = Assert.Single(method.Unresolved);
        Assert.Equal("entity comes from field '_target'", unresolved.Reason);
        Assert.Equal([Privilege.Write, Privilege.Assign], unresolved.Privileges);
    }

    [Fact]
    public void GrantAccessNeedsTheGrantedRights()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, EntityReference user, Guid id) => service.Execute(new GrantAccessRequest
                {
                    Target = new EntityReference("account", id),
                    PrincipalAccess = new PrincipalAccess { Principal = user, AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess },
                });
            }
            """);

        Assert.Equal(["account:Read", "account:Write", "account:Share"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void ConditionalCheckAppliesWhenItsFlagIsSet()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id) => service.Execute(new QualifyLeadRequest
                {
                    LeadId = new EntityReference("lead", id),
                    CreateContact = true,
                    CreateAccount = false,
                    Status = new OptionSetValue(3),
                });
            }
            """);

        Assert.Equal(["contact:Create", "lead:Write"], method.Requirements());
    }

    [Fact]
    public void ConfiguredRuleReplacesAndAddsRules()
    {
        var manifest = Analyze.Code(
            """
            class C
            {
                void M(IOrganizationService service, Guid id)
                {
                    service.Execute(new OrganizationRequest("ctx_Approve") { ["Target"] = new EntityReference("account", id) });
                    service.Execute(new WhoAmIRequest());
                }
            }
            """,
            rules:
            [
                new RequestRule("ctx_Approve") { Checks = [new RuleCheck([Privilege.Write]) { Member = "Target" }] },
                new RequestRule("WhoAmIRequest") { Checks = [new RuleCheck([Privilege.Read]) { Table = "systemuser" }] },
            ]);

        Assert.Equal(["account:Write", "systemuser:Read"], Assert.Single(manifest.Unreached).Requirements());
    }

    [Fact]
    public void NullRequestNeedsNothing()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id, bool flag)
                {
                    var request = Build(id, flag);
                    if (request is null) return;
                    service.Execute(request);
                }

                static OrganizationRequest? Build(Guid id, bool flag) => flag ? new DeleteRequest { Target = new EntityReference("contact", id) } : null;
            }
            """);

        Assert.Equal(["contact:Delete"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void TypeThatIsACollectionOfItselfIsNotARequest()
    {
        var method = Analyze.Method("""
            class Node : IEnumerable<Node>
            {
                public IEnumerator<Node> GetEnumerator() => throw new NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class C
            {
                void M(IOrganizationService service) => Save(service, new Node());
                void Save(IOrganizationService service, Node node) => service.Create(new Account());
            }
            """);

        Assert.Equal(["account:Create"], method.Requirements());
    }

    [Fact]
    public void RulesAreReadFromConfiguration()
    {
        var rules = RuleSetup.Create(Configuration("""
            [
              { "Message": "ctx_Approve", "Target": "Target", "Privileges": ["Write", "$Access"] },
              { "Message": "ctx_ApproveMultiple", "Targets": "Targets", "As": ["ctx_Approve"] },
              { "Message": "ctx_Close", "Checks": [{ "Table": "Task", "Privileges": ["Create"], "When": "CreateTask" }], "Enforced": false },
              { "Message": "WhoAmIRequest" },
              { "Message": "ctx_Purge", "GlobalPrivileges": [" prvBulkDelete ", "PRVBULKDELETE"], "Assumption": "purges one table" }
            ]
            """));

        Assert.Equal(
            [
                new RequestRule("ctx_Approve") { Checks = [new RuleCheck([Privilege.Write]) { Member = "Target", AccessMask = "Access" }] },
                new RequestRule("ctx_ApproveMultiple") { As = ["ctx_Approve"], Targets = "Targets" },
                new RequestRule("ctx_Close") { Checks = [new RuleCheck([Privilege.Create]) { Table = "task", When = "CreateTask" }], Enforced = false },
                new RequestRule("WhoAmIRequest"),
                new RequestRule("ctx_Purge") { GlobalPrivileges = ["prvBulkDelete"], Assumption = "purges one table" },
            ],
            rules,
            RuleComparer.Instance);
    }

    [Theory]
    [InlineData("""[{ "Target": "Target" }]""", "XrmMockup:Permissions:Rules:0 has no Message")]
    [InlineData("""[{ "Message": "ctx_A", "Privileges": ["Write"] }]""", "XrmMockup:Permissions:Rules:0 has Privileges but no Target")]
    [InlineData("""[{ "Message": "ctx_A", "Target": "Target", "Privileges": ["Update"] }]""", "XrmMockup:Permissions:Rules:0 has an unknown privilege 'Update'")]
    [InlineData("""[{ "Message": "ctx_A", "Checks": [{ "Privileges": ["Read"] }] }]""", "XrmMockup:Permissions:Rules:0:Checks:0 needs either a Member or a Table")]
    [InlineData("""[{ "Message": "ctx_A", "GlobalPrivileges": [" "] }]""", "XrmMockup:Permissions:Rules:0 has an empty GlobalPrivileges entry")]
    public void ConfiguredRuleMistakesNameTheSetting(string rules, string error)
    {
        var exception = Assert.Throws<ToolException>(() => RuleSetup.Create(Configuration(rules)));

        Assert.StartsWith(error, exception.Message);
    }

    private static PermissionsConfiguration Configuration(string rules)
    {
        var directory = Directory.CreateTempSubdirectory("permissions-rules").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), $$"""{ "XrmMockup": { "Permissions": { "Rules": {{rules}} } } }""");
            return ConfigReader.Read(Path.Combine(directory, "appsettings.json")).Permissions;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Records compare their lists by reference, so rules are compared by content.
    /// </summary>
    private sealed class RuleComparer : IEqualityComparer<RequestRule>
    {
        public static RuleComparer Instance { get; } = new();

        public bool Equals(RequestRule? x, RequestRule? y) => Describe(x!) == Describe(y!);

        public int GetHashCode(RequestRule obj) => Describe(obj).GetHashCode();

        private static string Describe(RequestRule rule) =>
            $"{rule.Message} as [{string.Join(",", rule.As)}] targets {rule.Targets} requests {rule.Requests} enforced {rule.Enforced} " +
            $"globals [{string.Join(",", rule.GlobalPrivileges)}] assumption {rule.Assumption} checks " +
            string.Join(";", rule.Checks.Select(c => $"{c.Member}/{c.Table}/{c.AccessMask}/{c.When}:{string.Join(",", c.Privileges)}"));
    }

    [Fact]
    public void CustomApiCallIsReportedApart()
    {
        var manifest = Analyze.Code("""
            class C
            {
                void M(IOrganizationService service)
                {
                    service.Execute(new OrganizationRequest("ctx_Recalculate"));
                    service.Execute(new OrganizationRequest("Recalculate"));
                }
            }
            """,
            setup: builder => builder.AddPluginSteps([new PluginStepRegistration("Contoso.Plugins.Recalculate", "Contoso.Plugins", "Recalculate", null, 40, null)]));

        Assert.Equal(
            ["custom API 'Recalculate' is called; its code and execute privilege are unknown", "custom API 'ctx_Recalculate' is called; its code and execute privilege are unknown"],
            Assert.Single(manifest.Unreached).Unresolved.Select(u => u.Reason).Order(StringComparer.Ordinal));
    }
}
