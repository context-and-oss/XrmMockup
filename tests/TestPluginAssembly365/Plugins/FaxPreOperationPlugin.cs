using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DG.Some.Namespace
{
    public class FaxPreOperationPlugin : Plugin
    {
        public FaxPreOperationPlugin()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Fax>(EventOperation.Create, ExecutionStage.PreOperation, OnCreate);
            RegisterPluginStep<Fax>(EventOperation.Update, ExecutionStage.PreOperation, OnUpdate);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private void OnCreate(LocalPluginContext ctx)
        {
            object targetFax;
            ctx.PluginExecutionContext.InputParameters.TryGetValue("Target", out targetFax);
            if (targetFax == null) return;

            (targetFax as Entity)["category"] = "test category";
        }
        private void OnUpdate(LocalPluginContext ctx)
        {
            object targetFax;
            ctx.PluginExecutionContext.InputParameters.TryGetValue("Target", out targetFax);
            if (targetFax == null) return;

            (targetFax as Entity)["isbilled"] = true;
        }
    }
}
