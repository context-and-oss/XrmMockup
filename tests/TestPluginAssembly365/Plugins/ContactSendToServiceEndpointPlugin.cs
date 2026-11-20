using TestPluginAssembly365.Plugins.LegacyDaxif;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System;

namespace DG.Some.Namespace
{
    /// <summary>
    /// This plugin is used to test custom mock service registrations, the actual endpoint id doesn't matter here
    /// </summary>
    public class SendContactToServiceEndpointPlugin : LegacyPlugin
    {
        public SendContactToServiceEndpointPlugin() : base(typeof(SendContactToServiceEndpointPlugin))
        {
            RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PreOperation, Execute)
                .SetExecutionMode(ExecutionMode.Synchronous);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            var target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            var contact = target.ToEntity<Contact>();

            if (contact.Description == "Test_IServiceEndpointNotificationService"
                || contact.Description == "Test_IServiceEndpointNotificationService_Nested")
            {
                var serviceEndpoint = (IServiceEndpointNotificationService)localContext.ServiceProvider.GetService(typeof(IServiceEndpointNotificationService));
                var endpointReference = new EntityReference("serviceendpoint", Guid.Empty);

                localContext.TracingService.Trace("Custom service plugin depth: {0}", localContext.PluginExecutionContext.Depth);
                serviceEndpoint?.Execute(endpointReference, localContext.PluginExecutionContext);

                if (contact.Description == "Test_IServiceEndpointNotificationService_Nested")
                {
                    localContext.OrganizationService.Create(new Contact
                    {
                        Description = "Test_IServiceEndpointNotificationService"
                    });
                }
            }
        }
    }
}
