namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Tables that reach a service call through a method's return value or a generic method's type argument.
/// </summary>
public class ReturnedTableTests
{
    [Fact]
    public void EntityReturnedByHelperResolvesToEveryReturnedTable()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service, bool flag) => service.Create(Build(flag));

                Entity Build(bool flag)
                {
                    if (flag) return new Account();
                    return new Contact();
                }
            }
            """);

        Assert.Equal(["account:Create", "contact:Create"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void ReturnedValueUsesTheArgumentsOfTheCall()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) => service.Delete(Table("contact"), Guid.Empty);

                static string Table(string name) => name;
            }
            """);

        Assert.Equal(["contact:Delete"], method.Requirements());
        Assert.Empty(method.Unresolved);
    }

    [Fact]
    public void QueryFromGenericHelperResolvesThroughTypeArguments()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => new Repository(c.OrganizationService).ByIds<Contact>());
            }

            public class Repository(IOrganizationService service)
            {
                public List<E> ByIds<E>() where E : Entity => Retrieve<E>(Query<E>());

                private List<E> Retrieve<E>(QueryExpression query) where E : Entity =>
                    service.RetrieveMultiple(query).Entities.Cast<E>().ToList();

                private static QueryExpression Query<E>() where E : Entity
                {
                    var instance = Activator.CreateInstance<E>();
                    var query = new QueryExpression(instance.LogicalName);
                    query.ColumnSet.AddColumn("name");
                    return query;
                }
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["contact:Read as Caller"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void GenericExtensionMethodResolvesTypeArgument()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => c.OrganizationService.Get<Contact>(Guid.Empty));
            }

            public static class Extensions
            {
                public static T Get<T>(this IOrganizationService service, Guid id) where T : Entity, new() =>
                    service.Retrieve(new T().LogicalName, id, new ColumnSet(true)).ToEntity<T>();
            }
            """);

        Assert.Equal(["contact:Read as Caller"], manifest.Entry("plugin:P:Update:account:PostOperation").RequirementsWithIdentity());
    }

    [Fact]
    public void UnboundTypeParameterIsStillReported()
    {
        var manifest = Analyze.Code("""
            public static class Extensions
            {
                public static void Remove<T>(this IOrganizationService service, Guid id) where T : Entity, new() =>
                    service.Delete(new T().LogicalName, id);
            }
            """);

        Assert.Equal("table depends on type parameter 'T'", Assert.Single(Assert.Single(manifest.Unreached).Unresolved).Reason);
    }

    [Fact]
    public void TypeParameterOfAGenericTypeIsTheReceiversTypeArgument()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c =>
                {
                    new Repository<Contact>(c.OrganizationService).Remove(Guid.Empty);
                    new TaskRepository(c.OrganizationService).Archive(Guid.Empty);
                });
            }

            public class Repository<T>(IOrganizationService service) where T : Entity, new()
            {
                public virtual void Remove(Guid id) => service.Delete(new T().LogicalName, id);

                public void Archive(Guid id) => Remove(id);
            }

            public class TaskRepository(IOrganizationService service) : Repository<Task>(service);

            [EntityLogicalName("task")]
            public class Task : Entity
            {
                public Task() : base("task") { }
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["contact:Delete as Caller", "task:Delete as Caller"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void TypeParameterOfAGenericTypeIsPassedOnByAnOverride()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c =>
                {
                    Repository<Contact> repository = new LoggedRepository<Contact>(c.OrganizationService);
                    repository.Remove(Guid.Empty);
                });
            }

            public class Repository<T>(IOrganizationService service) where T : Entity, new()
            {
                protected IOrganizationService Service { get; } = service;

                public virtual void Remove(Guid id) => Service.Delete(new T().LogicalName, id);
            }

            public class LoggedRepository<E>(IOrganizationService service) : Repository<E>(service) where E : Entity, new()
            {
                public override void Remove(Guid id)
                {
                    Service.Create(new E());
                    base.Remove(id);
                }
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["contact:Create as Caller", "contact:Delete as Caller"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void TypeParameterOfAGenericTypeIsTheTypeArgumentOfTheInterfaceItIsCalledOn()
    {
        var manifest = Analyze.Code("""
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => Remove(Repositories.Contacts(c.OrganizationService)));

                private static void Remove(IRepository<Contact> contacts) => contacts.Remove(Guid.Empty);
            }

            public interface IRepository<T> where T : Entity
            {
                void Remove(Guid id);
            }

            public class Repository<E>(IOrganizationService service) : IRepository<E> where E : Entity, new()
            {
                public void Remove(Guid id) => service.Delete(new E().LogicalName, id);
            }

            public static class Repositories
            {
                public static IRepository<Contact> Contacts(IOrganizationService service) => new Repository<Contact>(service);
            }
            """);

        var entry = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["contact:Delete"], entry.Requirements());
        Assert.Empty(entry.Unresolved);
    }

    [Fact]
    public void ReasonInsideHelperIsReported()
    {
        var method = Analyze.Method("""
            class C
            {
                private Entity? _pending;

                void M(IOrganizationService service) => service.Create(Pending());

                Entity Pending() => _pending!;
            }
            """);

        Assert.Equal("entity comes from field '_pending'", Assert.Single(method.Unresolved).Reason);
    }

    [Fact]
    public void RecursiveHelperTerminates()
    {
        var method = Analyze.Method("""
            class C
            {
                void M(IOrganizationService service) => service.RetrieveMultiple(Query(3));

                QueryExpression Query(int depth) => depth == 0 ? new QueryExpression("account") : Query(depth - 1);
            }
            """);

        Assert.Equal(["account:Read"], method.Requirements());
    }
}
