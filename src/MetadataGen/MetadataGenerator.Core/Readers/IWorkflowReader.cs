using Microsoft.Xrm.Sdk;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads workflow definitions from Dataverse.
/// </summary>
public interface IWorkflowReader
{
    /// <summary>
    /// Gets active workflow entities from the active solution.
    /// </summary>
    Task<IEnumerable<Entity>> GetWorkflowsAsync(CancellationToken ct = default);
}
