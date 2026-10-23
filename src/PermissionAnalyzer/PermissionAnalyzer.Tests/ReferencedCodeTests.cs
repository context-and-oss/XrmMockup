using XrmMockup.PermissionAnalyzer.Core.Analysis;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Code of a library the analyzed project references, such as a data access library that several apps share.
/// </summary>
public class ReferencedCodeTests
{
    /// <summary>
    /// A repository with a method the function calls and one only other apps call.
    /// </summary>
    private const string Library = """
        namespace Shared
        {
            public class Repository
            {
                public void DeleteLead(IOrganizationService service) => service.Delete("lead", Guid.Empty);

                public void DeleteTask(IOrganizationService service) => service.Delete("task", Guid.Empty);
            }
        }
        """;

    private const string Function = """
        public class Functions
        {
            [Microsoft.Azure.Functions.Worker.Function("PurgeLead")]
            public void Run([Microsoft.Azure.Functions.Worker.HttpTrigger] object request)
            {
                var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                new Shared.Repository().DeleteLead(client);
            }
        }
        """;

    [Fact]
    public void CodeTheProjectCallsIsAnalyzedButTheRestIsNotUnreached()
    {
        var library = Analyze.Compile(Library, "Shared", "shared/Code.cs");
        var builder = new ManifestBuilder(Analyze.Root);
        Assert.True(builder.AddReferencedCode(library));
        Assert.True(builder.Add(Analyze.Compile(Function, "Functions", references: library)));

        var manifest = builder.Build();

        Assert.Equal(["lead:Delete"], manifest.Entry("function:PurgeLead").Requirements());
        Assert.Empty(manifest.Unreached);
    }

    [Fact]
    public void EntryPointsInReferencedCodeAreReported()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        builder.AddReferencedCode(Analyze.Compile(Function, "Functions", references: Analyze.Compile(Library, "Shared")));

        Assert.Equal(["function:PurgeLead"], builder.Build().EntryPoints.Select(e => e.Id));
    }

    [Fact]
    public void CodeInTheAnalyzedProjectIsUnreachedEvenWhenAlsoAddedAsReferencedCode()
    {
        var library = Analyze.Compile(Library, "Shared");
        var builder = new ManifestBuilder(Analyze.Root);
        builder.AddReferencedCode(library);
        builder.Add(library);

        Assert.Equal(
            ["Shared.Repository.DeleteLead(IOrganizationService)", "Shared.Repository.DeleteTask(IOrganizationService)"],
            builder.Build().Unreached.Select(m => m.Id));
    }
}
