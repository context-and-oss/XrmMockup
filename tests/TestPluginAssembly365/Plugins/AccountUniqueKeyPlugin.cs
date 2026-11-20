using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using TestPluginAssembly365.Plugins.SyncAsyncTest;
using XrmPluginCore;
using XrmPluginCore.Enums;

namespace DG.Some.Namespace
{
    public class AccountUniqueKeyPlugin : TestPlugin
    {
        public AccountUniqueKeyPlugin()
        {
#pragma warning disable CS0618
            RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreOperation, Execute);
            RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PreOperation, Execute);
#pragma warning restore CS0618
        }

        private void Execute(LocalPluginContext context)
        {
            if (context.PluginExecutionContext.SharedVariables.TryGetValue("tag", out var value))
                ((Entity)context.PluginExecutionContext.InputParameters["Target"])["name"] = value;
        }
    }
}
