using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class ExtractionTests
{
    [Fact]
    public void EarlyBoundCreateNeedsCreate()
    {
        var method = Analyze.Method("""
            class C { void M(IOrganizationService service) => service.Create(new Account()); }
            """);

        Assert.Equal("C.M(IOrganizationService)", method.Id);
        Assert.Equal(["account:Create"], method.Requirements());
        Assert.Equal(RequirementBasis.Direct, method.Requirements[0].Basis);
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void LateBoundEntityWithConstantNameIsResolved()
    {
        var method = Analyze.Method("""
            class C
            {
                const string Table = "Contact";
                void M(IOrganizationService service)
                {
                    service.Update(new Entity(Table));
                    service.Delete("account", Guid.Empty);
                }
            }
            """);

        Assert.Equal(["account:Delete", "contact:Write"], method.Requirements());
    }

    [Fact]
    public void LocalsAndConditionalsResolveToEveryPossibleTable()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, bool flag)
                {
                    var name = flag ? "account" : "contact";
                    Entity entity = new Entity(name);
                    service.Create(entity);
                }
            }
            """);

        Assert.Equal(["account:Create", "contact:Create"], method.Requirements());
    }

    [Fact]
    public void RetrieveNeedsRead()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) =>
                    service.Retrieve(Contact.EntityLogicalName, Guid.Empty, new ColumnSet(true));
            }
            """);

        Assert.Equal(["contact:Read"], method.Requirements());
    }

    [Fact]
    public void QueryExpressionIncludesLinkedTables()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    var query = new QueryExpression("account");
                    query.AddLink("contact", "primarycontactid", "contactid");
                    service.RetrieveMultiple(query);
                }
            }
            """);

        Assert.Equal(["account:Read", "contact:Read"], method.Requirements());
    }

    [Fact]
    public void NestedLinksAreReportedAsUnresolved()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    var query = new QueryExpression("account");
                    query.LinkEntities.Add(new LinkEntity("account", "contact", "primarycontactid", "contactid", JoinOperator.Inner));
                    service.RetrieveMultiple(query);
                }
            }
            """);

        Assert.Equal(["account:Read"], method.Requirements());
        Assert.Single(method.Unresolved);
    }

    [Fact]
    public void FetchXmlIncludesLinkEntities()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, string name)
                {
                    var fetch = $"<fetch><entity name='account'><link-entity name='contact' from='contactid' to='primarycontactid'><filter><condition attribute='fullname' operator='eq' value='{name}' /></filter></link-entity></entity></fetch>";
                    service.RetrieveMultiple(new FetchExpression(fetch));
                }
            }
            """);

        Assert.Equal(["account:Read", "contact:Read"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void AssociateNeedsAppendToOnTargetAndAppendOnRelated()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id) =>
                    service.Associate("account", id, new Relationship("contact_customer_accounts"),
                        new EntityReferenceCollection { new EntityReference("contact", id) });
            }
            """);

        Assert.Equal(["account:Read", "account:AppendTo", "contact:Read", "contact:Append"], method.Requirements());
    }

    [Fact]
    public void ExecuteIsReportedAsUnresolved()
    {
        var method = Analyze.Method("""
            class C { void M(IOrganizationService service) => service.Execute(new OrganizationRequest("WhoAmI")); }
            """);

        Assert.Empty(method.Requirements);
        var unresolved = Assert.Single(method.Unresolved);
        Assert.Equal("Execute", unresolved.Operation);
        Assert.Equal("Execute requests are not analyzed yet", unresolved.Reason);
    }

    [Fact]
    public void ParameterEntityIsReportedWithReason()
    {
        var method = Analyze.Method("""
            class C { void M(IOrganizationService service, Entity target) => service.Update(target); }
            """);

        var unresolved = Assert.Single(method.Unresolved);
        Assert.Equal("Update", unresolved.Operation);
        Assert.Equal([Privilege.Write], unresolved.Privileges);
        Assert.Equal("entity comes from parameter 'target'", unresolved.Reason);
        Assert.Equal(("Code.cs", 18, 74), (unresolved.Source.Location.File, unresolved.Source.Location.Line, unresolved.Source.Location.Column));
    }

    [Fact]
    public void IndexerIsDescribedWithItsKey()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, ParameterCollection inputs) => service.Update((Entity)inputs["Target"]);
            }
            """);

        Assert.Equal("entity comes from inputs[\"Target\"]", Assert.Single(method.Unresolved).Reason);
    }

    [Fact]
    public void TypeParameterIsDescribed()
    {
        var method = Analyze.Method("""
            class C
            {
                void M<T>(IOrganizationService service) where T : Entity, new() => service.Create(new T());
            }
            """);

        Assert.Equal("table depends on type parameter 'T'", Assert.Single(method.Unresolved).Reason);
    }

    [Fact]
    public void LoopOverRetrieveMultipleResolvesToQueryTable()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    var query = new QueryExpression("contact");
                    query.AddLink("account", "parentcustomerid", "accountid");
                    foreach (var record in service.RetrieveMultiple(query).Entities)
                    {
                        service.Delete(record.LogicalName, record.Id);
                    }
                }
            }
            """);

        Assert.Equal(["account:Read", "contact:Read", "contact:Delete"], method.Requirements());
    }

    [Fact]
    public void LambdasAndLocalFunctionsCountTowardsTheirMember()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    Action create = () => service.Create(new Account());
                    void Update() => service.Update(new Contact());
                    create();
                    Update();
                }
            }
            """);

        Assert.Equal(["account:Create", "contact:Write"], method.Requirements());
    }

    [Fact]
    public void ServiceImplementationsAndAsyncMethodsAreRecognized()
    {
        var method = Analyze.Method("""
            class C
            {
                async Task M(Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 service) =>
                    await service.CreateAsync(new Account());
            }
            """);

        Assert.Equal(["account:Create"], method.Requirements());
    }

    [Fact]
    public void CompilationWithoutSdkIsSkipped()
    {
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Empty");
        Assert.False(new Core.Analysis.ManifestBuilder(Analyze.Root).Add(compilation));
    }

    [Fact]
    public void TablesWithoutRoleAreListed()
    {
        var manifest = Analyze.Code("""
            class C { void M(IOrganizationService service) { service.Create(new Account()); service.Create(new Contact()); } }
            """, roleGovernedTables: ["Account"]);

        Assert.Equal(["contact"], manifest.NotRoleGoverned);
    }
}
