namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A privilege in the privilege catalog of the metadata, for looking up a privilege by name.
/// </summary>
/// <param name="Name">The privilege name, such as <c>prvReadAccount</c>.</param>
/// <param name="Privilege">
/// The table privilege it grants on <paramref name="Tables"/>, or <c>null</c> when its access right isn't one.
/// </param>
/// <param name="Tables">
/// Logical names of the tables it applies to. Empty for a privilege not tied to a table.
/// </param>
public sealed record PrivilegeDefinition(string Name, Privilege? Privilege, IReadOnlyList<string> Tables);

/// <summary>
/// A custom API in the privilege catalog of the metadata.
/// </summary>
/// <param name="UniqueName">The message, such as <c>ctx_Recalculate</c>.</param>
/// <param name="ExecutePrivilegeName">The privilege calling it needs, or <c>null</c> when it needs none.</param>
public sealed record CustomApiDefinition(string UniqueName, string? ExecutePrivilegeName);
