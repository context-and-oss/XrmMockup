using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// Writes a manifest in one output format. Output always uses '\n' line endings, so it's the same on every OS.
/// </summary>
internal interface IManifestWriter
{
    /// <param name="coverage">Role coverage to show with each requirement, or <c>null</c> for the plain manifest.</param>
    void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer);
}
