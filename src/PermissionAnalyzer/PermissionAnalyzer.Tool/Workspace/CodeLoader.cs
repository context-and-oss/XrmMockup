using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using XrmMockup.PermissionAnalyzer.Core.Analysis;

namespace XrmMockup.PermissionAnalyzer.Tool.Workspace;

/// <summary>
/// Loads a solution or project with MSBuild and adds every C# project to a <see cref="ManifestBuilder"/>, or with
/// <c>pluginCode</c>, as plugin code from another solution (<see cref="ManifestBuilder.AddPluginCode"/>). The projects
/// that a single project references are added as referenced code (<see cref="ManifestBuilder.AddReferencedCode"/>).
/// The projects must be restored, as for a build. Strong-name signing is turned off, because it only affects the
/// output and its key file is often not on the machine.
/// <para>
/// Every path is loaded into one workspace, so a project that several of them reference, such as a shared library of
/// the plugin solutions, is loaded once and added once: by the first path that has it, whose kind of code is never
/// weaker than a later one's.
/// </para>
/// </summary>
internal sealed class CodeLoader(ILogger<CodeLoader> logger) : IDisposable
{
    // MSBuild reports warnings such as NuGet audit results as failures too, so they're only shown when a project
    // turns out not to load, below
    private readonly List<string> _failures = [];

    // What became of each project, by file and name (which has the target framework)
    private readonly Dictionary<(string File, string Name), Outcome> _seen = [];

    private enum Outcome { Added, Skipped }

    private MSBuildWorkspace? _workspace;
    private IDisposable? _failureHandler;

    public void Dispose()
    {
        _failureHandler?.Dispose();
        _workspace?.Dispose();
    }

    private MSBuildWorkspace Workspace()
    {
        if (_workspace is not null) return _workspace;

        var properties = FrameworkReferences.Properties();
        if (properties.TryGetValue(FrameworkReferences.Property, out var frameworkRoot))
        {
            logger.LogDebug(".NET Framework reference assemblies from {Root}", frameworkRoot);
        }
        _workspace = MSBuildWorkspace.Create(properties);
        _failureHandler = _workspace.RegisterWorkspaceFailedHandler(e =>
        {
            logger.LogDebug("{Message}", e.Diagnostic.Message);
            lock (_failures) _failures.Add(e.Diagnostic.Message);
        });
        return _workspace;
    }

    public async Task AddAsync(string path, ManifestBuilder builder, ProjectFilter filter, CancellationToken cancellationToken, bool pluginCode = false)
    {
        var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var workspace = Workspace();

        logger.LogInformation("Loading {Path}", path);
        IEnumerable<Project> projects;
        var referenced = new HashSet<ProjectId>();
        if (IsSolution(path))
        {
            projects = (await workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken)).Projects;
        }
        else
        {
            // Opening a project reuses the projects it references that the workspace already has
            var fullPath = Path.GetFullPath(path);
            if (!workspace.CurrentSolution.Projects.Any(p => string.Equals(p.FilePath, fullPath, StringComparison.OrdinalIgnoreCase)))
            {
                await workspace.OpenProjectAsync(path, cancellationToken: cancellationToken);
            }
            // A project with several target frameworks loads as one project per framework. The projects it
            // references are analyzed too, because entry points in this project call into them, such as
            // function apps whose Dataverse calls are in a shared library. Code in them that the project doesn't
            // reach isn't reported as unreached, since other apps that share the library may call it
            var solution = workspace.CurrentSolution;
            var roots = solution.Projects.Where(p => string.Equals(p.FilePath, fullPath, StringComparison.OrdinalIgnoreCase)).ToList();
            projects = WithReferences(solution, roots).ToList();
            referenced.UnionWith(projects.Select(p => p.Id).Except(roots.Select(p => p.Id)));
        }

        var analyzed = 0;
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        string Problems(Project project)
        {
            lock (_failures)
            {
                var messages = _failures.Where(f => project.FilePath is { } file && f.Contains(file)).Distinct().Take(3);
                return string.Concat(messages.Select(m => Environment.NewLine + "  " + m));
            }
        }

        // Sorted, so log output and duplicate merging don't depend on load order
        foreach (var project in projects.OrderBy(p => p.FilePath, StringComparer.Ordinal).ThenBy(p => p.Name, StringComparer.Ordinal))
        {
            // Checked before compiling, which is most of the time spent per project
            if (filter.Excludes(project.FilePath, solutionDirectory) is { } pattern)
            {
                // Once per project file, not per target framework
                if (excluded.Add(project.FilePath!)) logger.LogInformation("Skipping {Project}: excluded by '{Pattern}'", Path.GetFileNameWithoutExtension(project.FilePath), pattern);
                continue;
            }

            var key = (project.FilePath ?? "", project.Name);
            if (_seen.TryGetValue(key, out var outcome))
            {
                logger.LogDebug("{Project} was already loaded", project.Name);
                if (outcome == Outcome.Added) analyzed++;
                continue;
            }

            if (project.Language != LanguageNames.CSharp)
            {
                logger.LogInformation("Skipping {Project}: only C# is analyzed", project.Name);
                _seen[key] = Outcome.Skipped;
                continue;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken)
                ?? throw new ToolException($"Could not compile {project.Name}");
            compilation = compilation.WithOptions(compilation.Options
                .WithCryptoKeyFile(null)
                .WithCryptoKeyContainer(null)
                .WithDelaySign(null)
                .WithPublicSign(false));

            var problems = Problems(project);
            if (compilation.GetSpecialType(SpecialType.System_Object).TypeKind == TypeKind.Error)
            {
                // Not even the framework resolved, so nothing in the project can be analyzed
                var hint = problems.Contains("reference assemblies for .NETFramework", StringComparison.Ordinal)
                    ? " Its .NET Framework reference assemblies weren't found: restore a project that references the matching " +
                      "Microsoft.NETFramework.ReferenceAssemblies.net* package (such as net462), which puts them in the NuGet cache, " +
                      $"or set {FrameworkReferences.Property} to a folder with them."
                    : " Restore or build it on this machine first.";
                throw new ToolException($"Could not load {project.Name}: its references didn't resolve.{hint}{problems}");
            }

            var added = pluginCode ? builder.AddPluginCode(compilation, cancellationToken)
                : referenced.Contains(project.Id) ? builder.AddReferencedCode(compilation, cancellationToken)
                : builder.Add(compilation, cancellationToken);
            _seen[key] = added ? Outcome.Added : Outcome.Skipped;
            if (!added)
            {
                if (problems.Length == 0)
                {
                    logger.LogInformation("Skipping {Project}: it doesn't reference the Dataverse SDK", project.Name);
                }
                else
                {
                    // An unrestored project loses its package references, the Dataverse SDK among them
                    logger.LogWarning("Skipping {Project}: the Dataverse SDK isn't among its resolved references. Restore or build it on this machine if it should be. MSBuild reported:{Problems}", project.Name, problems);
                }
                continue;
            }
            analyzed++;

            // Calls the compiler can't bind are invisible to the analysis, so say how much is affected
            var errors = compilation.GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Count > 0)
            {
                var first = string.Concat(errors.Take(3).Select(e => Environment.NewLine + "  " + e));
                logger.LogWarning(
                    "{Project} has {Count} compile errors; operations in code that doesn't compile may be missed. Restore and build the project first. The first:{Errors}",
                    project.Name,
                    errors.Count,
                    first);
            }
            else
            {
                logger.LogInformation(pluginCode ? "Analyzed {Project} for plugin cascades" : "Analyzed {Project}", project.Name);
            }
        }

        if (analyzed == 0 && excluded.Count > 0)
        {
            throw new ToolException($"No project in {path} is left to analyze: {excluded.Count} excluded, the rest don't reference the Dataverse SDK");
        }

        if (analyzed == 0)
        {
            throw new ToolException($"No project in {path} references the Dataverse SDK, so there is nothing to analyze. If one should, restore or build it on this machine first: until then its package references don't resolve.");
        }
    }

    /// <summary>
    /// The given projects and every project they reference, directly or not.
    /// </summary>
    private static IEnumerable<Project> WithReferences(Solution solution, IEnumerable<Project> roots)
    {
        var found = new Dictionary<ProjectId, Project>();
        var pending = new Stack<Project>(roots);
        while (pending.Count > 0)
        {
            var project = pending.Pop();
            if (found.ContainsKey(project.Id)) continue;
            found.Add(project.Id, project);
            foreach (var reference in project.ProjectReferences)
            {
                if (solution.GetProject(reference.ProjectId) is { } referenced) pending.Push(referenced);
            }
        }
        return found.Values;
    }

    private static bool IsSolution(string path) =>
        path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
}
