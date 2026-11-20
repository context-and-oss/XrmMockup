using Microsoft.Xrm.Sdk;

namespace XrmMockup.MetadataGenerator.Core.Connection;

/// <summary>
/// Provides access to an IOrganizationService instance.
/// </summary>
public interface IOrganizationServiceProvider
{
    /// <summary>
    /// Gets the organization service instance.
    /// </summary>
    IOrganizationService Service { get; }

    /// <summary>
    /// Gets the connected host URL.
    /// </summary>
    string ConnectedHost { get; }

    Guid ConnectedOrgId { get; }
}
