using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// Writes a manifest in one output format. Output always uses '\n' line endings, so it's the same on every OS.
/// </summary>
internal interface IManifestWriter
{
    void Write(Manifest manifest, TextWriter writer);
}
