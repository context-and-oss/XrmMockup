using DG.Tools.XrmMockup;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads security role definitions from Dataverse.
/// </summary>
public interface ISecurityRoleReader
{
    /// <summary>
    /// Gets security roles with their privileges.
    /// Duplicate role names are automatically mitigated by appending a counter suffix.
    /// </summary>
    /// <param name="rootBusinessUnitId">The root business unit ID.</param>
    /// <param name="solutions">Solution names to get roles from.</param>
    /// <param name="securityRoles">
    /// Named security roles to include. null = not configured (all roles when no solutions).
    /// Empty array = explicitly no named roles.
    /// </param>
    /// <param name="allSecurityRoles">When true, all roles are returned regardless of other filters.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Dictionary<Guid, SecurityRole>> GetSecurityRolesAsync(
        Guid rootBusinessUnitId,
        string[] solutions,
        string[]? securityRoles,
        bool allSecurityRoles,
        CancellationToken ct = default);
}
