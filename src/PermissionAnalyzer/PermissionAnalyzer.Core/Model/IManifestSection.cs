namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A part of the manifest with its own requirements: an <see cref="EntryPoint"/> or an unreached
/// <see cref="MethodManifest"/>.
/// </summary>
public interface IManifestSection
{
    string Id { get; }

    SourceLocation? Declaration { get; }

    /// <summary>
    /// Requirements sorted by table, privilege, identity and basis.
    /// </summary>
    IReadOnlyList<Requirement> Requirements { get; }

    /// <summary>
    /// Privileges not tied to a table, sorted by name, identity and basis.
    /// </summary>
    IReadOnlyList<GlobalRequirement> GlobalRequirements { get; }

    IReadOnlyList<UnresolvedOperation> Unresolved { get; }

    IReadOnlyList<Assumption> Assumptions { get; }
}
