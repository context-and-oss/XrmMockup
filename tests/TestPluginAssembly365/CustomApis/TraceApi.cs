namespace DG.Some.Namespace {
    using System;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Custom API that emits trace messages, used to verify grouped trace-log capture for custom APIs.
    /// Registered as dg_TraceApi.
    /// </summary>
    public class TraceApi : Plugin
    {
        public TraceApi()
        {
            RegisterCustomAPI("TraceApi", Execute)
                .AddResponseProperty("Done", CustomApiParameterType.Boolean);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            localContext.TracingService.Trace("TraceApi running");
            localContext.TracingService.Trace("TraceApi step {0}", 2);
            localContext.PluginExecutionContext.OutputParameters["Done"] = true;
        }
    }
}
