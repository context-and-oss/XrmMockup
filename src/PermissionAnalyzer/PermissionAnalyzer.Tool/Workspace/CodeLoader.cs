using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using XrmMockup.PermissionAnalyzer.Core.Analysis;

namespace XrmMockup.PermissionAnalyzer.Tool.Workspace;

/// <summary>
/// Loads a solution or project with MSBuild and adds every C# project to a <see cref="ManifestBuilder"/>.
/// The projects must be restored, as for a build.
/// </summary>
internal sealed class CodeLoader(ILogger<CodeLoader> logger)
{
    public async Task AddAsync(string path, ManifestBuilder builder, ProjectFilter filter, CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create();
        // MSBuild reports warnings such as NuGet audit results as failures too, so they're only shown when a
        // project turns out not to load, below
        var failures = new List<string>();
        using var _ = workspace.RegisterWorkspaceFailedHandler(e =>
        {
            logger.LogDebug("{Message}", e.Diagnostic.Message);
            lock (failures) failures.Add(e.Diagnostic.Message);
        });

        logger.LogInformation("Loading {Path}", path);
        IEnumerable<Project> projects;
        if (IsSolution(path))
        {
            projects = (await workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken)).Projects;
        }
        else
        {
            await workspace.OpenProjectAsync(path, cancellationToken: cancellationToken);
            // A project with several target frameworks loads as one project per framework. The projects it
            // references are analyzed too, because entry points in this project call into them, such as
            // function apps whose Dataverse calls are in a shared library
            var fullPath = Path.GetFullPath(path);
            var solution = workspace.CurrentSolution;
            var roots = solution.Projects.Where(p => string.Equals(p.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));
            projects = WithReferences(solution, roots);
        }

        var analyzed = 0;
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        string Problems(Project project)
        {
            lock (failures)
            {
                var messages = failures.Where(f => project.FilePath is { } file && f.Contains(file)).Distinct().Take(3);
                return string.Concat(messages.Select(m => Environment.NewLine + "  " + m));
            }
        }

        // Sorted, so log output and duplicate merging don't depend on load order
        foreach (var project in projects.OrderBy(p => p.FilePath, StringComparer.Ordinal).ThenBy(p => p.Name, StringComparer.Ordinal))
        {
            // Checked before compiling, which is most of the time spent per project
            if (filter.Excludes(project.FilePath) is { } pattern)
            {
                // Once per project file, not per target framework
                if (excluded.Add(project.FilePath!)) logger.LogInformation("Skipping {Project}: excluded by '{Pattern}'", Path.GetFileNameWithoutExtension(project.FilePath), pattern);
                continue;
            }

            if (project.Language != LanguageNames.CSharp)
            {
                logger.LogInformation("Skipping {Project}: only C# is analyzed", project.Name);
                continue;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken)
                ?? throw new ToolException($"Could not compile {project.Name}");

            var problems = Problems(project);
            if (compilation.GetSpecialType(SpecialType.System_Object).TypeKind == TypeKind.Error)
            {
                // Not even the framework resolved, so nothing in the project can be analyzed
                throw new ToolException($"Could not load {project.Name}: its references didn't resolve. Restore or build it on this machine first.{problems}");
            }

            if (!builder.Add(compilation, cancellationToken))
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
            var errors = compilation.GetDiagnostics(cancellationToken).Count(d => d.Severity == DiagnosticSeverity.Error);
            if (errors > 0)
            {
                logger.LogWarning("{Project} has {Count} compile errors; operations in code that doesn't compile may be missed. Restore and build the project first", project.Name, errors);
            }
            else
            {
                logger.LogInformation("Analyzed {Project}", project.Name);
            }
        }

        foreach (var pattern in filter.Unused)
        {
            logger.LogWarning("Exclude pattern '{Pattern}' matches no project", pattern);
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
