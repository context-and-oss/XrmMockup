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
        }
        """;

    [Fact]
    public void SameCompilationAddedTwiceMergesAndOutputIsStable()
    {
        // A multi-targeted project is analyzed once per target framework
        var once = new ManifestBuilder(Analyze.Root);
        once.Add(Analyze.Compile(Source, "net462"));

        var twice = new ManifestBuilder(Analyze.Root);
        twice.Add(Analyze.Compile(Source, "net10"));
        twice.Add(Analyze.Compile(Source, "net462"));

        foreach (var writer in new IManifestWriter[] { new TextManifestWriter(), new JsonManifestWriter() })
        {
            Assert.Equal(Write(writer, once), Write(writer, twice));
        }
    }

    [Fact]
    public void MethodsAreSortedOrdinally()
    {
        var manifest = Analyze.Code(Source);
        Assert.Equal(["Contoso.A.Y(IOrganizationService, Entity)", "Contoso.B.Z(IOrganizationService)"], manifest.Methods.Select(m => m.Id));
    }

    [Fact]
    public void OutputUsesLineFeeds()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        builder.Add(Analyze.Compile(Source));
        var text = Write(new JsonManifestWriter(), builder) + Write(new TextManifestWriter(), builder);
        Assert.DoesNotContain('\r', text);
    }

    private static string Write(IManifestWriter writer, ManifestBuilder builder)
    {
        using var output = new StringWriter();
        writer.Write(builder.Build(["account"]), output);
        return output.ToString();
    }
}
