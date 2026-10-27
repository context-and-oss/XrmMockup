using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class DeterminismTests
{
    private const string Source = """
        namespace Contoso
        {
            class B { void Z(IOrganizationService s) { s.Delete("contact", Guid.Empty); s.Create(new Account()); } }
            class A { void Y(IOrganizationService s, Entity e) { s.Update(e); s.Retrieve("account", Guid.Empty, new ColumnSet()); } }

            public class P : Plugin
            {
                public P()
                {
                    RegisterPluginStep<Contact>(EventOperation.Update, ExecutionStage.PostOperation, c => new B2().Run(c.OrganizationService));
                    RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreOperation, c => new B2().Run(c.OrganizationAdminService));
                }
            }

            public interface IStore { void Save(IOrganizationService s); }
            public class S1 : IStore { public void Save(IOrganizationService s) => s.Create(new Contact()); }
            public class S2 : IStore { public void Save(IOrganizationService s) => s.Create(new Account()); }
            class B2 { public IStore? Store; public void Run(IOrganizationService s) { Store!.Save(s); s.Delete("contact", Guid.Empty); } }
        }
        """;

    [Fact]
    public void SameCompilationAddedTwiceMergesAndOutputIsStable()
    {
        // A multi-targeted project is analyzed once per target framework
        var once = new ManifestBuilder(Analyze.Root);
        once.Add(Analyze.Compile(Source));

        var twice = new ManifestBuilder(Analyze.Root);
        twice.Add(Analyze.Compile(Source));
        twice.Add(Analyze.Compile(Source));

        foreach (var writer in new IManifestWriter[] { new TextManifestWriter(), new TextManifestWriter(new ReportOptions(Detail.Compact)), new TextManifestWriter(new ReportOptions(Detail.Normal)), new TextManifestWriter(new ReportOptions(Detail.Full)), new JsonManifestWriter(), new MarkdownManifestWriter(), new SarifManifestWriter() })
        {
            Assert.Equal(Write(writer, once), Write(writer, twice));
        }
    }

    [Fact]
    public void ACallThatResolvesDifferentlyPerTargetFrameworkKeepsBoth()
    {
        // The call is at the same place in both compilations, but its constant differs, as with #if per framework
        const string Call = """
            namespace Contoso
            {
                class Cleanup { void Run(IOrganizationService s) => s.Delete(Names.Table, Guid.Empty); }
            }
            """;
        var builder = new ManifestBuilder(Analyze.Root);
        builder.Add(Analyze.Compile(Call + """static class Names { public const string Table = "account"; }"""));
        builder.Add(Analyze.Compile(Call + """static class Names { public const string Table = "contact"; }"""));

        Assert.Equal(["account:Delete", "contact:Delete"], Assert.Single(builder.Build().Unreached).Requirements());
    }

    [Fact]
    public void OutputDoesNotDependOnCompilationOrder()
    {
        var forward = new ManifestBuilder(Analyze.Root);
        forward.Add(Analyze.Compile(Source, "Contoso.A"));
        forward.Add(Analyze.Compile(Source, "Contoso.B"));

        var backward = new ManifestBuilder(Analyze.Root);
        backward.Add(Analyze.Compile(Source, "Contoso.B"));
        backward.Add(Analyze.Compile(Source, "Contoso.A"));

        foreach (var writer in new IManifestWriter[] { new TextManifestWriter(), new TextManifestWriter(new ReportOptions(Detail.Compact)), new TextManifestWriter(new ReportOptions(Detail.Normal)), new TextManifestWriter(new ReportOptions(Detail.Full)), new JsonManifestWriter(), new MarkdownManifestWriter(), new SarifManifestWriter() })
        {
            Assert.Equal(Write(writer, forward), Write(writer, backward));
        }
    }

    [Fact]
    public void UnreachedMethodsAreSortedOrdinally()
    {
        var manifest = Analyze.Code(Source);
        Assert.Equal(["Contoso.A.Y(IOrganizationService, Entity)", "Contoso.B.Z(IOrganizationService)"], manifest.Unreached.Select(m => m.Id));
    }

    [Fact]
    public void OutputUsesLineFeeds()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        builder.Add(Analyze.Compile(Source));
        var text = Write(new JsonManifestWriter(), builder) + Write(new TextManifestWriter(), builder) + Write(new MarkdownManifestWriter(), builder) + Write(new SarifManifestWriter(), builder);
        Assert.DoesNotContain('\r', text);
    }

    [Fact]
    public void JsonListsEntryPointsWithIdentities()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        builder.Add(Analyze.Compile(Source));
        var json = Write(new JsonManifestWriter(), builder);

        Assert.Contains("\"id\": \"plugin:Contoso.P:Update:contact:PostOperation\"", json);
        Assert.Contains("\"runsAs\": \"System\"", json);
        Assert.Contains("\"basis\": \"all-implementations\"", json);
        Assert.Contains("\"unreached\": [", json);
    }

    private static string Write(IManifestWriter writer, ManifestBuilder builder)
    {
        using var output = new StringWriter();
        writer.Write(builder.Build(["account"]), null, output);
        return output.ToString();
    }
}
