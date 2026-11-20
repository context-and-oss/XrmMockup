#if NET10_0_OR_GREATER
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using XrmMockup.PermissionAnalyzer;

namespace DG.Tools.XrmMockup
{
    /// <summary>Loads only the shared SDK library payload, never either product's CLI executable.</summary>
    internal static class PermissionAnalysisBootstrap
    {
        private static readonly Lazy<Type> Loader = new Lazy<Type>(LoadWorkspaceType);

        public static Task<PermissionAnalysis> LoadAsync(string projectOrSolutionPath, CancellationToken cancellationToken)
        {
            try
            {
                return (Task<PermissionAnalysis>)Loader.Value.GetMethod("LoadAsync", new[] { typeof(string), typeof(CancellationToken) })
                    .Invoke(null, new object[] { projectOrSolutionPath, cancellationToken });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static Type LoadWorkspaceType()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "XrmMockup.PermissionAnalysis", "XrmMockup.PermissionAnalyzer.Workspace.dll");
            if (!File.Exists(path))
                throw new InvalidOperationException("Enable permission source loading by setting <XrmMockupEnablePermissionAnalysis>true</XrmMockupEnablePermissionAnalysis> in the .NET 10 test project. No dotnet tool installation is required.");
            var context = new PermissionLibraryContext(path);
            return context.LoadFromAssemblyPath(path).GetType("XrmMockup.PermissionAnalyzer.Workspace.PermissionAnalysisLoader", throwOnError: true);
        }

        // Isolate the SDK's Roslyn/MSBuild dependencies from the host's workflow dependencies.
        // The public contracts alone are shared, so manifests/scopes cross this boundary without
        // casts between two copies of the same assembly's types.
        private sealed class PermissionLibraryContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver resolver;
            private readonly string directory;

            public PermissionLibraryContext(string workspacePath) : base("XrmMockup.PermissionAnalysis", isCollectible: false)
            {
                resolver = new AssemblyDependencyResolver(workspacePath);
                directory = Path.GetDirectoryName(workspacePath);
            }

            protected override Assembly Load(AssemblyName name)
            {
                var contracts = typeof(PermissionAnalysis).Assembly;
                if (name.Name == contracts.GetName().Name) return contracts;
                var path = resolver.ResolveAssemblyToPath(name);
                if (path == null)
                {
                    var candidate = Path.Combine(directory, name.Name + ".dll");
                    if (File.Exists(candidate)) path = candidate;
                }
                return path == null ? null : LoadFromAssemblyPath(path);
            }

            protected override IntPtr LoadUnmanagedDll(string name)
            {
                var path = resolver.ResolveUnmanagedDllToPath(name);
                return path == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
            }
        }
    }
}
#endif
