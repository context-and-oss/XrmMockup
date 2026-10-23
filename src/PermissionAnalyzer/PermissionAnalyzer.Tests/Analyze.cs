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
        using Microsoft.Xrm.Sdk.Messages; using Microsoft.Crm.Sdk.Messages;
        using Microsoft.Extensions.DependencyInjection;
        using XrmPluginCore;
        using XrmPluginCore.Enums;

        [EntityLogicalName("account")]
        public class Account : Entity { public const string EntityLogicalName = "account"; public Account() : base("account") { } }

        [EntityLogicalName("contact")]
        public class Contact : Entity { public const string EntityLogicalName = "contact"; public Contact() : base("contact") { } }

        """;

    /// <summary>
    /// Look-alikes of the plugin, workflow and Azure Functions APIs, compiled into their own assembly so they are
    /// external to the analyzed code, as the real packages are.
    /// </summary>
    private const string Stubs = """
        using System;
        using Microsoft.Xrm.Sdk;
        using Microsoft.Extensions.DependencyInjection;

        namespace XrmPluginCore.Enums
        {
            public enum EventOperation { Create, Update, Delete, Retrieve }
            public enum ExecutionStage { PreValidation = 10, PreOperation = 20, PostOperation = 40 }
            public enum Privilege { Create, Read, Write, Delete, Append, AppendTo, Assign, Share }
            public enum ExecutionMode { Synchronous, Asynchronous }
        }

        namespace XrmPluginCore.Plugins
        {
            public class PluginStepConfigBuilder<T>
            {
                public PluginStepConfigBuilder<T> SetUserContext(Guid userId) => this;
                public PluginStepConfigBuilder<T> SetName(string name) => this;
                public PluginStepConfigBuilder<T> SetExecutionMode(XrmPluginCore.Enums.ExecutionMode mode) => this;
                public PluginStepConfigBuilder<T> AddFilteredAttributes(params System.Linq.Expressions.Expression<Func<T, object>>[] lambdas) => this;
                public PluginStepConfigBuilder<T> AddFilteredAttributes(params string[] attributes) => this;
            }

            public class CustomApiConfigBuilder
            {
                public CustomApiConfigBuilder WithExecutePrivilegeName(string name) => this;
                public CustomApiConfigBuilder WithExecutePrivilege<T>(XrmPluginCore.Enums.Privilege privilege) where T : Entity => this;
                public CustomApiConfigBuilder WithExecutePrivilege(string schemaName, XrmPluginCore.Enums.Privilege privilege) => this;
            }
        }

        namespace XrmPluginCore
        {
            using XrmPluginCore.Enums;
            using XrmPluginCore.Plugins;

            public interface IExtendedServiceProvider : IServiceProvider { }

            public class LocalPluginContext
            {
                public LocalPluginContext(IExtendedServiceProvider serviceProvider) { }
                public IOrganizationService OrganizationService => null!;
                public IOrganizationService OrganizationAdminService => null!;
                public IPluginExecutionContext PluginExecutionContext => null!;
            }

            public abstract class Plugin : IPlugin
            {
                public void Execute(IServiceProvider serviceProvider) { }
                protected virtual IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services) => services;
                protected PluginStepConfigBuilder<T> RegisterPluginStep<T>(EventOperation operation, ExecutionStage stage, Action<LocalPluginContext> action) where T : Entity => new();
                protected PluginStepConfigBuilder<T> RegisterPluginStep<T>(string operation, ExecutionStage stage, Action<LocalPluginContext> action) where T : Entity => new();
                protected PluginStepConfigBuilder<TEntity> RegisterStep<TEntity, TService>(EventOperation operation, ExecutionStage stage, Action<TService> action) where TEntity : Entity => new();
                protected PluginStepConfigBuilder<TEntity> RegisterStep<TEntity, TService>(EventOperation operation, ExecutionStage stage, string handlerMethod) where TEntity : Entity => new();
                protected PluginStepConfigBuilder<T> RegisterStep<T>(EventOperation operation, ExecutionStage stage, Action<IExtendedServiceProvider> action) where T : Entity => new();
                protected CustomApiConfigBuilder RegisterCustomAPI(string name, Action<LocalPluginContext> action) => new();
                protected CustomApiConfigBuilder RegisterAPI<TService>(string name, Action<TService> action) => new();
            }
        }

        namespace System.Activities
        {
            public class CodeActivityContext { public T GetExtension<T>() where T : class => null!; }
            public abstract class CodeActivity { protected abstract void Execute(CodeActivityContext context); }
        }

        namespace Microsoft.Azure.Functions.Worker
        {
            [AttributeUsage(AttributeTargets.Method)]
            public sealed class FunctionAttribute(string name) : Attribute { public string Name { get; } = name; }

            [AttributeUsage(AttributeTargets.Parameter)]
            public sealed class HttpTriggerAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Parameter)]
            public sealed class TimerTriggerAttribute(string schedule) : Attribute { }
        }
        """;

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(() =>
    {
        var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var references = platform
            .Append(typeof(Entity).Assembly.Location)
            .Append(typeof(IOrganizationServiceAsync2).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();

        var stubs = CSharpCompilation.Create(
            "Stubs",
            [CSharpSyntaxTree.ParseText(Stubs)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var image = new MemoryStream();
        var emitted = stubs.Emit(image);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        references.Add(MetadataReference.CreateFromImage(image.ToArray()));
        return references;
    });

    /// <summary>
    /// Compiles <paramref name="source"/> as <paramref name="file"/>, relative to <see cref="Root"/>.
    /// </summary>
    public static CSharpCompilation Compile(string source, string assemblyName = "Test", string file = "Code.cs", params CSharpCompilation[] references)
    {
        var tree = CSharpSyntaxTree.ParseText(Prelude + source, path: Path.Combine(Root, file));
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            References.Value.Concat(references.Select(r => r.ToMetadataReference())),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        return compilation;
    }

    public static Manifest Code(
        string source,
        IEnumerable<string>? roleGovernedTables = null,
        EntryPointSelector? scope = null,
        Action<ManifestBuilder>? setup = null,
        IEnumerable<RequestRule>? rules = null,
        IEnumerable<LookupColumn>? lookups = null)
    {
        var builder = new ManifestBuilder(Root, rules, lookups);
        setup?.Invoke(builder);
        Assert.True(builder.Add(Compile(source)));
        return builder.Build(roleGovernedTables, scope);
    }

    /// <summary>
    /// The single method that performs data operations, which no entry point reaches.
    /// </summary>
    public static MethodManifest Method(string source)
    {
        var manifest = Code(source);
        Assert.Empty(manifest.EntryPoints);
        return Assert.Single(manifest.Unreached);
    }

    /// <summary>
    /// The entry point with this id.
    /// </summary>
    public static EntryPoint Entry(this Manifest manifest, string id) =>
        manifest.EntryPoints.SingleOrDefault(e => e.Id == id)
        ?? throw new Xunit.Sdk.XunitException($"No entry point {id}; found: {string.Join(", ", manifest.EntryPoints.Select(e => e.Id))}");

    /// <summary>
    /// Requirements as <c>table:Privilege</c>, in manifest order.
    /// </summary>
    public static IReadOnlyList<string> Requirements(this MethodManifest method) =>
        method.Requirements.Select(r => $"{r.Table}:{r.Privilege}").ToList();

    /// <summary>
    /// Requirements as <c>table:Privilege</c>, in manifest order.
    /// </summary>
    public static IReadOnlyList<string> Requirements(this EntryPoint entryPoint) =>
        entryPoint.Requirements.Select(r => $"{r.Table}:{r.Privilege}").Distinct().ToList();

    /// <summary>
    /// Requirements as <c>table:Privilege as RunsAs</c>, in manifest order.
    /// </summary>
    public static IReadOnlyList<string> RequirementsWithIdentity(this EntryPoint entryPoint) =>
        entryPoint.Requirements.Select(r => $"{r.Table}:{r.Privilege} as {r.RunsAs}").ToList();
}
