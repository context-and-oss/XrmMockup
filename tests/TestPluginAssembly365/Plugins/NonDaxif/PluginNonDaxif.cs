namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    

    /// <summary>
    /// Base class for all Plugins.
    /// </summary>    
    public abstract class PluginNonDaxif : IPlugin {
        protected class LocalPluginContext {
            internal IServiceProvider ServiceProvider {
                get;

                private set;
            }

            internal IOrganizationService OrganizationService {
                get;

                private set;
            }

            internal IPluginExecutionContext PluginExecutionContext {
                get;

                private set;
            }

            internal ITracingService TracingService {
                get;

                private set;
            }

            private LocalPluginContext() {
            }

            internal LocalPluginContext(IServiceProvider serviceProvider) {
                if (serviceProvider == null) {
                    throw new ArgumentNullException("serviceProvider");
                }

                // Obtain the execution context service from the service provider.
                this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Obtain the tracing service from the service provider.
                this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Obtain the Organization Service factory service from the service provider
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Use the factory to generate the Organization Service.
                this.OrganizationService = factory.CreateOrganizationService(this.PluginExecutionContext.UserId);
            }

            internal void Trace(string message) {
                if (string.IsNullOrWhiteSpace(message) || this.TracingService == null) {
                    return;
                }

                if (this.PluginExecutionContext == null) {
                    this.TracingService.Trace(message);
                } else {
                    this.TracingService.Trace(
                        "{0}, Correlation Id: {1}, Initiating User: {2}",
                        message,
                        this.PluginExecutionContext.CorrelationId,
                        this.PluginExecutionContext.InitiatingUserId);
                }
            }
        }

        public abstract void Execute(IServiceProvider serviceProvider);
    }
}