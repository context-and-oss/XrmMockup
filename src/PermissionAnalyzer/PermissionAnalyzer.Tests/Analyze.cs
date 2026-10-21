using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tests;

/// <summary>
/// Compiles source in memory against the real Dataverse SDK and analyzes it.
/// </summary>
internal static class Analyze
{
    public const string Root = "/src";

    /// <summary>
    /// Usings and an early-bound <c>Account</c> and <c>Contact</c> every snippet can use.
    /// </summary>
    private const string Prelude = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;
        using Microsoft.Xrm.Sdk;
        using Microsoft.Xrm.Sdk.Client;
        using Microsoft.Xrm.Sdk.Query;
        using Microsoft.Xrm.Sdk.Messages;

        [EntityLogicalName("account")]
        public class Account : Entity { public const string EntityLogicalName = "account"; public Account() : base("account") { } }

        [EntityLogicalName("contact")]
        public class Contact : Entity { public const string EntityLogicalName = "contact"; public Contact() : base("contact") { } }

        """;

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(() =>
    {
        var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return platform
            .Append(typeof(Entity).Assembly.Location)
            .Append(typeof(IOrganizationServiceAsync2).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    });

    public static CSharpCompilation Compile(string source, string assemblyName = "Test")
    {
        var tree = CSharpSyntaxTree.ParseText(Prelude + source, path: Path.Combine(Root, "Code.cs"));
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        return compilation;
    }

    public static Manifest Code(string source, IEnumerable<string>? roleGovernedTables = null)
    {
        var builder = new ManifestBuilder(Root);
        Assert.True(builder.Add(Compile(source)));
        return builder.Build(roleGovernedTables);
    }

    /// <summary>
    /// The single method that performs data operations.
    /// </summary>
    public static MethodManifest Method(string source)
    {
        var manifest = Code(source);
        return Assert.Single(manifest.Methods);
    }

    /// <summary>
    /// Requirements as <c>table:Privilege</c>, in manifest order.
    /// </summary>
    public static IReadOnlyList<string> Requirements(this MethodManifest method) =>
        method.Requirements.Select(r => $"{r.Table}:{r.Privilege}").ToList();
}
