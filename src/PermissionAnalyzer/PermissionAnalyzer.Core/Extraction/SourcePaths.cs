using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// Turns Roslyn locations into <see cref="SourceLocation"/>s relative to the analysis root.
/// </summary>
internal sealed class SourcePaths
{
    private static readonly StringComparison PathComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _root;
    private readonly ConcurrentDictionary<string, string> _relative = new(StringComparer.Ordinal);

    public SourcePaths(string rootDirectory)
    {
        var root = Path.GetFullPath(rootDirectory);
        _root = root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? root : root + Path.DirectorySeparatorChar;
    }

    public SourceLocation Of(Location location)
    {
        var span = location.GetLineSpan();
        return new SourceLocation(Relative(span.Path), span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1);
    }

    /// <summary>
    /// Where a symbol is declared in source; <c>null</c> for a symbol from a referenced assembly.
    /// </summary>
    public SourceLocation? Declaration(ISymbol symbol) =>
        symbol.Locations.FirstOrDefault(l => l.IsInSource) is { } location ? Of(location) : null;

    /// <summary>
    /// The path relative to the root, with <c>../</c> for a file outside it, such as plugin code in a solution next to
    /// the analyzed one. A file on another drive keeps its full path.
    /// </summary>
    private string Relative(string path) => _relative.GetOrAdd(path ?? "", NewRelative);

    private string NewRelative(string path)
    {
        if (string.IsNullOrEmpty(path)) return "<unknown>";

        var full = Path.GetFullPath(path);
        if (full.StartsWith(_root, PathComparison)) return full.Substring(_root.Length).Replace('\\', '/');

        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var rootParts = _root.TrimEnd(separators).Split(separators);
        var parts = full.Split(separators);
        var common = 0;
        while (common < rootParts.Length && common < parts.Length - 1 && string.Equals(rootParts[common], parts[common], PathComparison)) common++;

        if (common == 0) return full.Replace('\\', '/');
        return string.Concat(Enumerable.Repeat("../", rootParts.Length - common)) + string.Join("/", parts.Skip(common));
    }
}
