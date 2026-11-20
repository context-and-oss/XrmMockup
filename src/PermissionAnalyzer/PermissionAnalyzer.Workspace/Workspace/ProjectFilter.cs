using System.Text.RegularExpressions;

namespace XrmMockup.PermissionAnalyzer.Tool.Workspace;

/// <summary>
/// Decides which projects to leave out of the analysis. A pattern uses <c>*</c> and <c>?</c> wildcards and is
/// case-insensitive. Without a slash it matches the project file name without extension, e.g. <c>*.Tests</c>;
/// with one it matches the project path relative to the analyzed solution, e.g. <c>tests/*</c>.
/// </summary>
internal sealed class ProjectFilter
{
    private readonly string _rootDirectory;
    private readonly IReadOnlyList<(string Pattern, Regex Regex, bool IsPath)> _patterns;
    private readonly HashSet<string> _used = new(StringComparer.Ordinal);

    public ProjectFilter(string rootDirectory, IEnumerable<string> patterns)
    {
        _rootDirectory = rootDirectory;
        _patterns = patterns
            .Select(p => p.Trim().Replace('\\', '/'))
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(p => (p, ToRegex(p), p.Contains('/')))
            .ToList();
    }

    public IReadOnlyList<string> Patterns => _patterns.Select(p => p.Pattern).ToList();

    /// <summary>
    /// The first pattern that excludes the project at <paramref name="projectPath"/>, or <c>null</c> to analyze it.
    /// Path patterns match relative to <paramref name="solutionDirectory"/>, the directory of the solution the project
    /// is loaded from, which is the analyzed one by default.
    /// </summary>
    public string? Excludes(string? projectPath, string? solutionDirectory = null)
    {
        if (projectPath is null) return null;

        var name = Path.GetFileNameWithoutExtension(projectPath);
        var relative = Path.GetRelativePath(solutionDirectory ?? _rootDirectory, projectPath).Replace('\\', '/');
        foreach (var (pattern, regex, isPath) in _patterns)
        {
            if (regex.IsMatch(isPath ? relative : name))
            {
                _used.Add(pattern);
                return pattern;
            }
        }
        return null;
    }

    /// <summary>
    /// Patterns that haven't excluded any project so far, which are likely typos.
    /// </summary>
    public IEnumerable<string> Unused => _patterns.Select(p => p.Pattern).Where(p => !_used.Contains(p));

    private static Regex ToRegex(string pattern) =>
        new("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
