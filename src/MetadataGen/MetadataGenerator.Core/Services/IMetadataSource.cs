using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;

namespace XrmMockup.MetadataGenerator.Core.Services;

/// <summary>
/// Abstraction for metadata sources (online Dataverse, file system, etc.)
/// </summary>
public interface IMetadataSource
{
    /// <summary>
    /// Gets the complete metadata skeleton.
    /// </summary>
    Task<MetadataSkeleton> GetMetadataAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets workflow entities.
    /// </summary>
    Task<IEnumerable<Entity>> GetWorkflowsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets security roles.
    /// </summary>
    Task<Dictionary<Guid, SecurityRole>> GetSecurityRolesAsync(Guid rootBusinessUnitId, CancellationToken ct = default);

    /// <summary>
    /// Gets every privilege and the execute privileges of custom APIs.
    /// </summary>
    Task<PrivilegeCatalog> GetPrivilegeCatalogAsync(CancellationToken ct = default);
}
