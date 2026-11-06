using DG.Tools.XrmMockup;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads the privileges in Dataverse and the execute privileges of custom APIs.
/// </summary>
public interface IPrivilegeReader
{
    /// <summary>
    /// Gets every privilege with the tables it applies to, and every custom API that has an execute privilege.
    /// Both are sorted by name.
    /// </summary>
    Task<PrivilegeCatalog> GetPrivilegeCatalogAsync(CancellationToken ct = default);
}
