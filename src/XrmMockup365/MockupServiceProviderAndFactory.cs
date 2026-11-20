#if DATAVERSE_SERVICE_CLIENT
using Microsoft.PowerPlatform.Dataverse.Client;
#endif
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {

    /// <summary>
    /// A factory used to generate MockupServices based on a given Mockup instance and pluginContext
    /// </summary>
    internal class MockupServiceProviderAndFactory : IServiceProvider, IOrganizationServiceFactory {

        private Core core;
        private ITracingService tracingService;
        private PluginContext pluginContext;

        /// <summary>
        /// Creates new MockupServiceProviderAndFactory object
        /// </summary>
        /// <param name="core"></param>
        public MockupServiceProviderAndFactory(Core core) : this(core, null, core.TracingServiceFactory) { }

        internal MockupServiceProviderAndFactory(Core core, PluginContext pluginContext, ITracingServiceFactory tracingServiceFactory) {
            this.core = core;
            this.pluginContext = pluginContext;
            this.tracingService = tracingServiceFactory.GetService();
        }

        /// <summary>
        /// Get service from servicetype. Returns null if unknown type
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType) {
            if (serviceType == typeof(IPluginExecutionContext)
                || serviceType == typeof(IPluginExecutionContext2)
                || serviceType == typeof(IPluginExecutionContext3)
                || serviceType == typeof(IPluginExecutionContext4)
                || serviceType == typeof(IPluginExecutionContext5)
                || serviceType == typeof(IPluginExecutionContext6)
                || serviceType == typeof(IPluginExecutionContext7)) return this.pluginContext;
            if (serviceType == typeof(ITracingService)) return this.tracingService;
            if (serviceType == typeof(IOrganizationServiceFactory)) return this;
            if (serviceType == typeof(IManagedIdentityService)) return new DummyManagedIdentityService();
            return null;
        }

        public TService GetService<TService>() where TService: class
        {
            return GetService(typeof(TService)) as TService;
        }

        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            return new MockupService(core, userId, this.pluginContext);
        }

#if DATAVERSE_SERVICE_CLIENT
        /// <summary>
        /// Returns a new MockupService with the given userId, or standard user if null. 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IOrganizationServiceAsync2 CreateOrganizationServiceAsync(Guid? userId)
        {
            return new MockupService(core, userId, this.pluginContext);
        }

        public IOrganizationServiceAsync2 CreateOrganizationService(Guid? userId, MockupServiceSettings settings)
        {
            return new MockupService(core, userId, this.pluginContext, settings);
        }

        public IOrganizationServiceAsync2 CreateAdminOrganizationService()
        {
            return new MockupService(core, null, this.pluginContext);
        }

        public IOrganizationServiceAsync2 CreateAdminOrganizationService(MockupServiceSettings settings)
        {
            return new MockupService(core, null, this.pluginContext, settings);
        }
#else
        public IOrganizationService CreateOrganizationService(Guid? userId, MockupServiceSettings settings) {
            return new MockupService(core, userId, this.pluginContext, settings);
        }

        public IOrganizationService CreateAdminOrganizationService() {
            return new MockupService(core, null, this.pluginContext);
        }

        public IOrganizationService CreateAdminOrganizationService(MockupServiceSettings settings) {
            return new MockupService(core, null, this.pluginContext, settings);
        }
#endif
    }
}
