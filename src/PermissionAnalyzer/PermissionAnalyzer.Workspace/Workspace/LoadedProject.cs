using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace XrmMockup.PermissionAnalyzer.Tool.Workspace;

/// <summary>Loaded framework variants and source contents, including generated trees.</summary>
internal sealed record LoadedProject(string Path, string Name, IReadOnlyList<string> Symbols, string SourceHash, bool PluginCode)
{
    public static LoadedProject Of(Project project, Compilation compilation, string root, bool pluginCode, CancellationToken cancellationToken)
    {
        string Relative(string path) => string.IsNullOrEmpty(path) ? "<generated>" : System.IO.Path.GetRelativePath(root, path).Replace('\\', '/');
        var sources = compilation.SyntaxTrees.OrderBy(t => Relative(t.FilePath), StringComparer.Ordinal)
            .Select(t => { var path = Relative(t.FilePath); var text = t.GetText(cancellationToken).ToString(); return $"{path.Length}:{path}{text.Length}:{text}"; });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(sources)))).ToLowerInvariant();
        return new LoadedProject(Relative(project.FilePath!), project.Name,
            project.ParseOptions?.PreprocessorSymbolNames.OrderBy(s => s, StringComparer.Ordinal).ToList() ?? [], hash, pluginCode);
    }
}
