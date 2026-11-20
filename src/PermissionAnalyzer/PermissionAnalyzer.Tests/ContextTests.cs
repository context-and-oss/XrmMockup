using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// <c>OrganizationServiceContext</c>: LINQ queries, query properties of generated contexts, and changes saved
/// through the context.
/// </summary>
public class ContextTests
{
    /// <summary>
    /// A context like a generated one, with a query property per table.
    /// </summary>
    private const string Context = """
        public class Xrm : OrganizationServiceContext
        {
            public Xrm(IOrganizationService service) : base(service) { }

            public IQueryable<Account> AccountSet => CreateQuery<Account>();

            public IQueryable<Contact> ContactSet { get { return CreateQuery<Contact>(); } }
        }
        """;

    [Fact]
    public void QueryPropertyNeedsReadWhereItIsUsed()
    {
        var method = Analyze.Method(Context + """
            class C
            {
                List<Account> M(IOrganizationService service, Guid id) =>
                    new Xrm(service).AccountSet.Where(a => a.Id == id).ToList();
            }
            """);

        Assert.Equal("C.M(IOrganizationService, Guid)", method.Id);
        Assert.Equal(["account:Read"], method.Requirements());
        var location = Assert.Single(method.Requirements[0].Sources).Location;
        Assert.Equal((28, 26), (location.Line, location.Column));
    }

    [Fact]
    public void CreateQueryNeedsRead()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    var context = new OrganizationServiceContext(service);
                    var contacts = context.CreateQuery<Contact>().ToList();
                    var leads = (from l in context.CreateQuery("lead") select l).ToList();
                }
            }
            """);

        Assert.Equal(["contact:Read", "lead:Read"], method.Requirements());
    }

    [Fact]
    public void JoinedQueriesNeedReadAndOnlyTheOuterOneFiresRetrieveMultiple()
    {
        var manifest = Analyze.Code(Context + """
            public class Functions
            {
                [Microsoft.Azure.Functions.Worker.Function("Contacts")]
                public void Run([Microsoft.Azure.Functions.Worker.HttpTrigger] object request)
                {
                    var xrm = new Xrm(new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret"));
                    var names = (from a in xrm.AccountSet
                                 join c in xrm.ContactSet on a.Id equals c.Id
                                 select c.Id).ToList();
                }
            }
            """,
            setup: builder => builder.AddPluginSteps(
            [
                new PluginStepRegistration("Contoso.AccountQuery", "Contoso", "RetrieveMultiple", "account", 20, null),
                new PluginStepRegistration("Contoso.ContactQuery", "Contoso", "RetrieveMultiple", "contact", 20, null),
            ]));

        var function = manifest.Entry("function:Test/Contacts");
        Assert.Equal(["account:Read as ApplicationUser", "contact:Read as ApplicationUser"], function.RequirementsWithIdentity());
        Assert.Equal(["plugin:Contoso.AccountQuery:RetrieveMultiple:account:PreOperation"], function.Triggers);
    }

    [Fact]
    public void ChangesNeedThePrivilegesOfTheirMessage()
    {
        var method = Analyze.Method(Context + """
            class C
            {
                void M(Xrm xrm)
                {
                    xrm.AddObject(new Account());
                    var contact = xrm.ContactSet.First();
                    xrm.UpdateObject(contact);
                    xrm.DeleteObject(new Entity("lead"));
                    xrm.SaveChanges();
                }
            }
            """);

        Assert.Equal(["account:Create", "contact:Read", "contact:Write", "lead:Delete"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void LinksNeedAppendAndAppendTo()
    {
        var method = Analyze.Method(Context + """
            class C
            {
                void M(Xrm xrm, Account account)
                {
                    xrm.AddLink(account, new Relationship("account_contacts"), new Contact());
                    xrm.AddRelatedObject(account, new Relationship("account_tasks"), new Entity("task"));
                }
            }
            """);

        Assert.Equal(
            ["account:Read", "account:AppendTo", "contact:Read", "contact:Append", "task:Create", "task:Read", "task:Append"],
            method.Requirements());
    }

    [Fact]
    public void LoadPropertyIsUnresolved()
    {
        var method = Analyze.Method(Context + """
            class C
            {
                void M(Xrm xrm, Account account) => xrm.LoadProperty(account, new Relationship("account_contacts"));
            }
            """);

        Assert.Equal("related records are read through a relationship, whose table isn't known", Assert.Single(method.Unresolved).Reason);
    }

    [Fact]
    public void ContextRunsAsTheServiceItIsCreatedWith()
    {
        var manifest = Analyze.Code(Context + """
            namespace Contoso
            {
                public class ContactPlugin : Plugin
                {
                    public ContactPlugin() =>
                        RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, c => new Archiver(new ArchiveContext(c.OrganizationService)).Run());
                }

                public class ArchiveContext : Xrm
                {
                    public ArchiveContext(IOrganizationService service) : base(service) { }

                    public void Archive(Contact contact) => DeleteObject(contact);
                }

                public class Archiver
                {
                    private readonly ArchiveContext _xrm;

                    public Archiver(ArchiveContext xrm) { _xrm = xrm; }

                    public void Run() => _xrm.Archive(_xrm.ContactSet.First());
                }
            }
            """);

        var step = Assert.Single(manifest.EntryPoints);
        Assert.Equal(["contact:Read as Caller", "contact:Delete as Caller"], step.RequirementsWithIdentity());
        Assert.Empty(step.Assumptions);
        Assert.Empty(manifest.Unreached);
    }
}
