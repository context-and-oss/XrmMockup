using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Facts at the same source location can resolve differently in each target framework.
/// </summary>
public class MultiTargetTests
{
    [Theory]
    [InlineData("Remove(s, Names.Table)", "static void Remove(IOrganizationService s, string table) => s.Delete(table, Guid.Empty);")]
    [InlineData("s.Delete(Table(), Guid.Empty)", "static string Table() => Names.Table;")]
    [InlineData("s.Delete(Table(), Guid.Empty)", "static string Table() => Echo(Names.Table); static string Echo(string table) => table;")]
    [InlineData("s.Delete(Echo(Names.Table), Guid.Empty)", "static string Echo(string table) => table;")]
    public void TablesPassedThroughHelpersAreKept(string call, string helper)
    {
        var source = """
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => Cleanup.Run(c.OrganizationService));
            }
            static class Cleanup
            {
                public static void Run(IOrganizationService s) { CALL; }
                HELPERS
            }
            """.Replace("CALL", call).Replace("HELPERS", helper);
        var first = source + """static class Names { public const string Table = "account"; }""";
        var second = source + """static class Names { public const string Table = "contact"; }""";

        var entry = Build(first, second).Build().Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["account:Delete as Caller", "contact:Delete as Caller"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
        AssertStable(first, second);
    }

    [Theory]
    [InlineData("Repository.Remove<Record>(c.OrganizationService)", "static class Repository { public static void Remove<T>(IOrganizationService service) where T : Entity, new() => service.Delete(new T().LogicalName, Guid.Empty); }")]
    [InlineData("new Repository<Record>(c.OrganizationService).Remove()", "class Repository<T>(IOrganizationService service) where T : Entity, new() { public void Remove() => service.Delete(new T().LogicalName, Guid.Empty); }")]
    public void GenericMethodAndReceiverTypeArgumentsAreKept(string call, string helper)
    {
        var source = """
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => CALL);
            }
            [EntityLogicalName(Names.Table)]
            public class Record : Entity { public Record() : base(Names.Table) { } }
            HELPERS
            """.Replace("CALL", call).Replace("HELPERS", helper);
        var first = source + """static class Names { public const string Table = "account"; }""";
        var second = source + """static class Names { public const string Table = "contact"; }""";

        var entry = Build(first, second).Build().Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["account:Delete as Caller", "contact:Delete as Caller"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
        AssertStable(first, second);
    }

    [Theory]
    [InlineData("return Origins.Service(context);", "Helpers.Service(c).Delete(\"lead\", Guid.Empty)")]
    [InlineData("return Echo(Origins.Service(context));", "Helpers.Service(c).Delete(\"lead\", Guid.Empty)")]
    public void ServiceIdentitiesPassedThroughHelpersAreKept(string body, string call)
    {
        var source = """
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => CALL);
            }
            static class Helpers
            {
                public static IOrganizationService Service(LocalPluginContext context) { BODY }
                static IOrganizationService Echo(IOrganizationService service) => service;
            }
            """.Replace("CALL", call).Replace("BODY", body);
        // The helper's body changes behind a shared call site, as under #if for each target framework.
        var first = source + """static class Origins { public static IOrganizationService Service(LocalPluginContext c) => c.OrganizationService; }""";
        var second = source + """static class Origins { public static IOrganizationService Service(LocalPluginContext c) => c.OrganizationAdminService; }""";

        var entry = Build(first, second).Build().Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["lead:Delete as Caller", "lead:Delete as System"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
        AssertStable(first, second);
    }

    [Theory]
    [InlineData("Helpers.Remove(c.VALUE)", "static class Helpers { public static void Remove(IOrganizationService s) => s.Delete(\"lead\", Guid.Empty); }")]
    [InlineData("new Cleanup(c.VALUE).Run()", "class Cleanup(IOrganizationService service) { public void Run() => service.Delete(\"lead\", Guid.Empty); }")]
    public void ServiceArgumentsAndReceiverCreationAreKept(string call, string helper)
    {
        var source = """
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => CALL);
            }
            HELPERS
            """.Replace("CALL", call).Replace("HELPERS", helper);
        var first = source.Replace("VALUE", "OrganizationService");
        var second = source.Replace("VALUE", "OrganizationAdminService");

        var entry = Build(first, second).Build().Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["lead:Delete as Caller", "lead:Delete as System"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
        AssertStable(first, second);
    }

    [Fact]
    public void ConstructorFieldWritesFromBothFrameworksAreKept()
    {
        var source = """
            public class P : Plugin
            {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => new Cleanup(c).Run());
            }
            class Cleanup
            {
                private readonly IOrganizationService service;
                public Cleanup(LocalPluginContext context) { service = VALUE; }
                public void Run() => service.Delete("lead", Guid.Empty);
            }
            """;
        var first = source.Replace("VALUE", "context.OrganizationService");
        var second = source.Replace("VALUE", "context.OrganizationAdminService");

        var entry = Build(first, second).Build().Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["lead:Delete as Caller", "lead:Delete as System"], entry.RequirementsWithIdentity());
        Assert.Empty(entry.Unresolved);
        AssertStable(first, second);
    }

    [Fact]
    public void HelperArgumentsRetainThePluginCascadesOfBothTables()
    {
        var source = """
            public class P : Plugin
            {
                public P()
                {
                    RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, c => Cleanup.Run(c.OrganizationService, Names.Table));
                    RegisterPluginStep<Account>(EventOperation.Delete, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Entity("task")));
                    RegisterPluginStep<Contact>(EventOperation.Delete, ExecutionStage.PostOperation, c => c.OrganizationService.Create(new Entity("lead")));
                }
            }
            static class Cleanup
            {
                public static void Run(IOrganizationService service, string table) => service.Delete(table, Guid.Empty);
            }
            """;
        var first = source + """static class Names { public const string Table = "account"; }""";
        var second = source + """static class Names { public const string Table = "contact"; }""";

        var entry = Build(first, second).Build().Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["account:Delete", "contact:Delete", "lead:Create", "task:Create"], entry.Requirements());
        Assert.Equal(["plugin:P:Delete:account:PostOperation", "plugin:P:Delete:contact:PostOperation"], entry.Triggers);
        AssertStable(first, second);
    }

    private static ManifestBuilder Build(params string[] sources)
    {
        var builder = new ManifestBuilder(Analyze.Root);
        foreach (var source in sources) Assert.True(builder.Add(Analyze.Compile(source)));
        return builder;
    }

    private static void AssertStable(string first, string second)
    {
        var forward = Build(first, second).Build();
        var backward = Build(second, first).Build();
        var repeated = Build(first, second, first, second).Build();
        foreach (var writer in new IManifestWriter[] { new TextManifestWriter(), new JsonManifestWriter(), new MarkdownManifestWriter(), new SarifManifestWriter() })
        {
            Assert.Equal(Write(writer, forward), Write(writer, backward));
            Assert.Equal(Write(writer, forward), Write(writer, repeated));
        }
    }

    private static string Write(IManifestWriter writer, Manifest manifest)
    {
        using var output = new StringWriter();
        writer.Write(manifest, null, output);
        return output.ToString();
    }
}
