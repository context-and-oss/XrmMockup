#if DATAVERSE_SERVICE_CLIENT
using System;
using DataverseConnection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools.XrmMockup.Online
{
    /// <summary>
    /// In-process implementation for connecting to a live Dataverse environment.
    /// Used for testing with real data when OnlineEnvironment is configured.
    /// Authentication comes from the DataverseConnection package, which since 1.2.0 defaults to
    /// InteractiveBrowserCredential — a sign-in prompt, not the ambient DefaultAzureCredential
    /// chain. Set DataverseCredentialType in configuration to pick azcli or devicecode instead.
    /// </summary>
    internal class OnlineDataService : IOnlineDataService
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ServiceClient _serviceClient;
        private bool _disposed;

        public OnlineDataService(string environmentUrl)
        {
            if (string.IsNullOrWhiteSpace(environmentUrl))
                throw new ArgumentNullException(nameof(environmentUrl));

            // Use DataverseConnection for authentication
            var services = new ServiceCollection();
            services.AddDataverse(options => options.DataverseUrl = environmentUrl);
            _serviceProvider = services.BuildServiceProvider();
            _serviceClient = _serviceProvider.GetRequiredService<ServiceClient>();
        }

        public bool IsConnected => _serviceClient?.IsReady == true;

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            if (entityName == null) throw new ArgumentNullException(nameof(entityName));
            if (columnSet == null) throw new ArgumentNullException(nameof(columnSet));

            return _serviceClient.Retrieve(entityName, id, columnSet);
        }

        public EntityCollection RetrieveMultiple(QueryExpression query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            return _serviceClient.RetrieveMultiple(query);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _serviceClient?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}
#endif
