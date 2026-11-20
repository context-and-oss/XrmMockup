using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;

namespace XrmMockup.MetadataGenerator.Core.Services;

/// <summary>
/// Serializes metadata to output files.
/// </summary>
public interface IMetadataSerializer
{
    /// <summary>
    /// Serializes the metadata skeleton to the configured output directory.
    /// </summary>
    Task SerializeMetadataAsync(MetadataSkeleton skeleton, CancellationToken ct = default);

    /// <summary>
    /// Serializes workflows to the configured output directory.
    /// </summary>
    Task SerializeWorkflowsAsync(IEnumerable<Entity> workflows, CancellationToken ct = default);

    /// <summary>
    /// Serializes security roles to the configured output directory.
    /// </summary>
    Task SerializeSecurityRolesAsync(Dictionary<Guid, SecurityRole> securityRoles, CancellationToken ct = default);

    /// <summary>
    /// Serializes the privilege catalog to the configured output directory.
    /// </summary>
    Task SerializePrivilegeCatalogAsync(PrivilegeCatalog catalog, CancellationToken ct = default);

    /// <summary>
    /// Generates the TypeDeclarations.cs file with security role GUIDs.
    /// </summary>
    Task GenerateTypeDeclarationsAsync(Dictionary<Guid, SecurityRole> securityRoles, CancellationToken ct = default);
}
