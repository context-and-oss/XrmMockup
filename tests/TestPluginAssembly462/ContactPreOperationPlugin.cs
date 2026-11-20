using System;
using Microsoft.Xrm.Sdk;

namespace TestPluginAssembly462
{
    // No dependency on XrmMockup or the current-framework test assembly: this is a
    // standalone Dataverse plugin compiled against the .NET Framework 4.6.2 SDK.
    public class ContactPreOperationPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var target = (Entity)context.InputParameters["Target"];
            target["lastname"] = "ModifiedByNet462Plugin";
        }
    }
}
