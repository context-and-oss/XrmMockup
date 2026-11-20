using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools.XrmMockup.Online
{
    /// <summary>
    /// Interface for fetching data from an online Dataverse environment.
    /// </summary>
    internal interface IOnlineDataService : IDisposable
    {
        /// <summary>
        /// Retrieves a single entity by ID.
        /// </summary>
        Entity Retrieve(string entityName, Guid id, ColumnSet columnSet);

        /// <summary>
        /// Retrieves multiple entities using a QueryExpression.
        /// </summary>
        EntityCollection RetrieveMultiple(QueryExpression query);

        /// <summary>
        /// Gets whether the service is connected.
        /// </summary>
        bool IsConnected { get; }
    }
}
