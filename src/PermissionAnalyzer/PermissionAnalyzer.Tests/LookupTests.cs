using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Append on a record that gets a lookup set, and AppendTo on the record the lookup refers to, as XrmMockup checks
/// them on Create and Update.
/// </summary>
public class LookupTests
{
    /// <summary>
    /// An early-bound table with a lookup property, as the context generator writes it.
    /// </summary>
    private const string Order = """

        [EntityLogicalName("ctx_order")]
        public class Order : Entity
        {
            public Order() : base("ctx_order") { }

            [AttributeLogicalName("ctx_customerid")]
            public EntityReference? CustomerId { get => GetAttributeValue<EntityReference>("ctx_customerid"); set => this["ctx_customerid"] = value; }

            [AttributeLogicalName("ctx_name")]
            public string? Name { get => GetAttributeValue<string>("ctx_name"); set => this["ctx_name"] = value; }
        }
        """;

    private static readonly LookupColumn[] Metadata =
    [
        new("ctx_order", "ctx_customerid", ["account", "contact"]),
        new("contact", "parentcustomerid", ["account", "contact"]),
        new("account", "parentaccountid", ["account"]),
    ];

    [Fact]
    public void CreateWithEarlyBoundLookupNeedsAppendAndAppendTo()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id) =>
                    service.Create(new Order { Name = "x", CustomerId = new EntityReference(Account.EntityLogicalName, id) });
            }
            """ + Order);

        Assert.Equal(["account:AppendTo", "ctx_order:Create", "ctx_order:Append"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void LookupsSetThroughALocalAreFound()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id)
                {
                    var account = new Account { Id = id };
                    var contact = new Entity("contact", id);
                    contact["parentcustomerid"] = account.ToEntityReference();
                    contact.Attributes.Add("ctx_managerid", new EntityReference(Contact.EntityLogicalName, id));
                    service.Update(contact);
                }
            }
            """);

        Assert.Equal(["account:AppendTo", "contact:Write", "contact:Append", "contact:AppendTo"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void OwnerAndOtherColumnsNeedNoAppend()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id) =>
                    service.Create(new Entity("account") { ["name"] = "x", ["ownerid"] = new EntityReference("systemuser", id) });
            }
            """);

        Assert.Equal(["account:Create"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void RetrievedEntityAddsNoLookups()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id)
                {
                    var account = service.Retrieve("account", id, new ColumnSet(true)).ToEntity<Account>();
                    account["name"] = "x";
                    service.Update(account);
                }
            }
            """);

        Assert.Equal(["account:Read", "account:Write"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void DeconstructedEntityAddsOnlyTheLookupsSetOnIt()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id)
                {
                    var (account, _) = Pair();
                    service.Update(account);

                    var (contact, _) = (new Contact(), 1);
                    contact["parentcustomerid"] = new EntityReference("account", id);
                    service.Update(contact);
                }

                static (Account, int) Pair() => (new Account(), 1);
            }
            """);

        Assert.Equal(["account:Write", "account:AppendTo", "contact:Write", "contact:Append"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void UnknownLookupValueIsUnresolvedWithoutMetadata()
    {
        var method = Analyze.Method("""
            class C
            {
                EntityReference parent = null!;
                void M(IOrganizationService service) => service.Create(new Entity("account") { ["parentaccountid"] = parent });
            }
            """);

        Assert.Equal(["account:Create", "account:Append"], method.Requirements());
        var unresolved = Assert.Single(method.Unresolved);
        Assert.Equal("lookup value: entity comes from field 'parent'", unresolved.Reason);
        Assert.Equal([Privilege.AppendTo], unresolved.Privileges);
    }

    [Fact]
    public void MetadataGivesTheTablesOfUnknownLookupValues()
    {
        var manifest = Analyze.Code(
            """
            class C
            {
                EntityReference reference = null!;
                void M(IOrganizationService service)
                {
                    service.Create(new Entity("account") { ["parentaccountid"] = reference });
                    service.Create(new Order { CustomerId = reference });
                }
            }
            """ + Order,
            lookups: Metadata);
        var method = Assert.Single(manifest.Unreached);

        Assert.Empty(method.Unresolved);
        Assert.Equal(
            [
                "account:Create Direct",
                "account:Append Direct",
                "account:AppendTo Direct",
                "contact:AppendTo AllLookupTargets",
                "ctx_order:Create Direct",
                "ctx_order:Append Direct",
            ],
            method.Requirements.Select(r => $"{r.Table}:{r.Privilege} {r.Basis}"));
    }

    [Fact]
    public void KnownTargetOfPolymorphicLookupIsKept()
    {
        var manifest = Analyze.Code(
            """
            class C
            {
                void M(IOrganizationService service, Guid id) =>
                    service.Create(new Order { CustomerId = new EntityReference("contact", id) });
            }
            """ + Order,
            lookups: Metadata);
        var method = Assert.Single(manifest.Unreached);

        Assert.Equal(["contact:AppendTo", "ctx_order:Create", "ctx_order:Append"], method.Requirements());
        Assert.All(method.Requirements, r => Assert.Equal(RequirementBasis.Direct, r.Basis));
    }

    [Fact]
    public void LookupsSetInAHelperAreBoundAtTheCallSite()
    {
        var entry = Single("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => M(c.OrganizationService, Guid.Empty));

                void M(IOrganizationService service, Guid id)
                {
                    Save(service, new Contact(), new EntityReference("account", id));
                    Save(service, new Order(), null);
                }

                void Save(IOrganizationService service, Entity entity, EntityReference? parent)
                {
                    if (parent != null) entity["parentcustomerid"] = parent;
                    service.Create(entity);
                }
            }
            """ + Order);

        Assert.Equal(["account:AppendTo", "contact:Create", "contact:Append", "ctx_order:Create", "ctx_order:Append"], entry.Requirements());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void EntityWithLookupsPassedToHelperNeedsAppend()
    {
        var entry = Single("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c =>
                {
                    Save(c.OrganizationService, new Order { CustomerId = new EntityReference("contact", Guid.Empty) });
                    Save(c.OrganizationService, new Account { ["name"] = "x" });
                });

                static void Save(IOrganizationService service, Entity entity) => service.Create(entity);
            }
            """ + Order);

        Assert.Equal(["account:Create", "contact:AppendTo", "ctx_order:Create", "ctx_order:Append"], entry.Requirements());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void RequestsThatWriteEntitiesNeedAppendForTheirLookups()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid[] ids)
                {
                    service.Execute(new UpsertRequest { Target = new Order { CustomerId = new EntityReference("account", ids[0]) } });

                    var contacts = new List<Entity>();
                    foreach (var id in ids)
                    {
                        var contact = new Entity("contact");
                        contact["parentcustomerid"] = new EntityReference("account", id);
                        contacts.Add(contact);
                    }
                    service.Execute(new CreateMultipleRequest { Targets = new EntityCollection(contacts) { EntityName = "contact" } });
                }
            }
            """ + Order);

        Assert.Equal(
            ["account:AppendTo", "contact:Create", "contact:Append", "ctx_order:Create", "ctx_order:Write", "ctx_order:Append"],
            method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void LocalPassedToTheCallItIsAssignedFromIsResolvedOnce()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service)
                {
                    var entity = new Entity("account");
                    entity = Prepare(entity);
                    service.Create(entity);
                }

                static Entity Prepare(Entity entity) => entity;
            }
            """);

        Assert.Equal(["account:Create"], method.Requirements());
    }

    [Fact]
    public void AssignNeedsNoAppend()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, Guid id) =>
                    service.Execute(new AssignRequest { Target = new EntityReference("account", id), Assignee = new EntityReference("systemuser", id) });
            }
            """);

        Assert.Equal(["account:Write", "account:Assign"], method.Requirements());
    }

    [Fact]
    public void LoaderReadsLookupTargetsFromEntityMetadata()
    {
        var contact = new EntityMetadata { LogicalName = "contact" };
        var lookup = new LookupAttributeMetadata { LogicalName = "parentcustomerid", Targets = ["contact", "account"] };
        var name = new StringAttributeMetadata { LogicalName = "fullname" };
        typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.Attributes))!.SetValue(contact, new AttributeMetadata[] { name, lookup });

        Assert.Equal(
            [new LookupColumn("contact", "parentcustomerid", ["account", "contact"])],
            LookupLoader.Convert([contact]),
            new LookupColumnComparer());
    }

    [Fact]
    public void LoaderReadsXrmMockupMetadataFiles()
    {
        var columns = LookupLoader.Load(XrmMockupMetadataDirectory());

        var parent = Assert.Single(columns, c => c.Table == "contact" && c.Column == "parentcustomerid");
        Assert.Equal(["account", "contact"], parent.Targets);
        Assert.Empty(LookupLoader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }

    private static EntryPoint Single(string source)
    {
        var manifest = Analyze.Code(source);
        Assert.Empty(manifest.Unreached);
        return Assert.Single(manifest.EntryPoints);
    }

    private static string XrmMockupMetadataDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "tests", "XrmMockup365Test", "Metadata"));

    private sealed class LookupColumnComparer : IEqualityComparer<LookupColumn>
    {
        public bool Equals(LookupColumn? x, LookupColumn? y) =>
            x is not null && y is not null && x.Table == y.Table && x.Column == y.Column && x.Targets.SequenceEqual(y.Targets);

        public int GetHashCode(LookupColumn obj) => HashCode.Combine(obj.Table, obj.Column);
    }
}
