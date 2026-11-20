using XrmPluginCore;
using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal abstract class AbstractSystemPlugin
        : IPluginDefinition, IPlugin
    {
        private readonly List<PluginRegistration> PluginRegistrations = new List<PluginRegistration>();
        private string ChildClassName { get; }

        protected AbstractSystemPlugin()
        {
            ChildClassName = GetType().ToString();
        }

        protected void RegisterPluginStep(string entityName, EventOperation eventOperation, ExecutionStage executionStage, IEnumerable<IImageSpecification> images, Action<LocalPluginContext> action)
        {
            var stepConfig = new PluginStepConfig
            {
                ExecutionStage = executionStage,
                EventOperation = eventOperation.ToString(),
                EntityLogicalName = entityName,
                Deployment = Deployment.ServerOnly,
                ExecutionMode = ExecutionMode.Synchronous,
                ExecutionOrder = 1,
                ImageSpecifications = images
            };

            PluginRegistrations.Add(new PluginRegistration
            {
                PluginConfig = stepConfig,
                Action = action
            });
        }

        protected void RegisterPluginStep(string entityName, EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
        {
            RegisterPluginStep(entityName, eventOperation, executionStage, Enumerable.Empty<IImageSpecification>(), action);
        }

        public IEnumerable<IPluginStepConfig> GetRegistrations()
        {
            return PluginRegistrations.ConvertAll(p => p.PluginConfig);
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Construct the Local plug-in context.
            var localcontext = new LocalPluginContext(serviceProvider);

            localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", ChildClassName));

            try
            {
                // Iterate over all of the expected registered events to ensure that the plugin
                // has been invoked by an expected event
                // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
                Action<LocalPluginContext> entityAction =
                    (from a in PluginRegistrations
                    where
                        (int)a.PluginConfig.ExecutionStage == localcontext.PluginExecutionContext.Stage &&
                        a.PluginConfig.EventOperation == localcontext.PluginExecutionContext.MessageName &&
                        (string.IsNullOrWhiteSpace(a.PluginConfig.EntityLogicalName) || a.PluginConfig.EntityLogicalName == localcontext.PluginExecutionContext.PrimaryEntityName)
                    select a.Action).FirstOrDefault();

                if (entityAction != null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}",
                        ChildClassName,
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName));

                    entityAction.Invoke(localcontext);
                }
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));

                // Handle the exception.
                throw;
            }
            finally
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", ChildClassName));
            }
        }
    }

    internal class PluginRegistration
    {
        public IPluginStepConfig PluginConfig { get; set; }
        public Action<LocalPluginContext> Action { get; set; }
    }

    internal class LocalPluginContext
    {
        internal IServiceProvider ServiceProvider { get; }

        internal IOrganizationService OrganizationService { get; }

        internal IOrganizationService OrganizationAdminService { get; }

        internal IPluginExecutionContext PluginExecutionContext { get; }

        internal ITracingService TracingService { get; }

        internal LocalPluginContext(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Obtain the execution context service from the service provider.
            PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the tracing service from the service provider.
            TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the Organization Service factory service from the service provider
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            // Use the factory to generate the Organization Service.
            OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);

            // Use the factory to generate the Organization Admin Service.
            OrganizationAdminService = factory.CreateOrganizationService(null);
        }

        internal void Trace(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || TracingService == null)
            {
                return;
            }

            if (PluginExecutionContext == null)
            {
                TracingService.Trace(message);
            }
            else
            {
                TracingService.Trace(
                    "{0}, Correlation Id: {1}, Initiating User: {2}",
                    message,
                    PluginExecutionContext.CorrelationId,
                    PluginExecutionContext.InitiatingUserId);
            }
        }
    }
}