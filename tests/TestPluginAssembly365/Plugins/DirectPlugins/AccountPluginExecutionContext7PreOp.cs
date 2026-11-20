using Microsoft.Xrm.Sdk;
using System;

namespace DG.Some.Namespace
{
    public class AccountPluginExecutionContext7PreOp : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Resolve IPluginExecutionContext7 from the service provider
            var context7 = (IPluginExecutionContext7)serviceProvider.GetService(typeof(IPluginExecutionContext7));
            if (context7 == null)
            {
                throw new InvalidPluginExecutionException("IPluginExecutionContext7 resolved to null");
            }

            // Verify default property values
            if (context7.IsPortalsClientCall)
            {
                throw new InvalidPluginExecutionException("IsPortalsClientCall should default to false");
            }

            if (context7.IsApplicationUser)
            {
                throw new InvalidPluginExecutionException("IsApplicationUser should default to false");
            }

            if (context7.AuthenticatedUserId == Guid.Empty)
            {
                throw new InvalidPluginExecutionException("AuthenticatedUserId should not be Guid.Empty");
            }

            // Verify that it still works as IPluginExecutionContext
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context == null)
            {
                throw new InvalidPluginExecutionException("IPluginExecutionContext resolved to null");
            }

            // Stamp the target to prove the plugin fired and resolved IPluginExecutionContext7.
            // Only modify accounts created with a specific name to avoid interfering with other tests.
            var target = context7.InputParameters["Target"] as Entity;
            if (target != null && target.GetAttributeValue<string>("name") == "Context7Test")
            {
                target["description"] = "Context7Resolved";
            }
        }
    }
}
