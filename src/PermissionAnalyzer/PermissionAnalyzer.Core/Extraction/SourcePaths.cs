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

    private string Relative(string path)
    {
        if (string.IsNullOrEmpty(path)) return "<unknown>";

        var full = Path.GetFullPath(path);
        if (full.StartsWith(_root, PathComparison)) full = full.Substring(_root.Length);
        return full.Replace('\\', '/');
    }
}
