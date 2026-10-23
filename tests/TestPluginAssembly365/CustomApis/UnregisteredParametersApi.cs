namespace DG.Some.Namespace {
    using System;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Custom API used to verify handling of parameters that are not part of the registration.
    /// Returns the received input parameter names and 'tag' shared variable, and sets an unregistered output parameter when requested.
    /// Registered as dg_UnregisteredParametersApi.
    /// </summary>
    public class UnregisteredParametersApi : Plugin
    {
        public UnregisteredParametersApi()
        {
            RegisterCustomAPI("UnregisteredParametersApi", Execute)
                .AddRequestParameter("Input", CustomApiParameterType.String)
                .AddRequestParameter("SetUnregisteredOutput", CustomApiParameterType.Boolean)
                .AddResponseProperty("InputKeys", CustomApiParameterType.String)
                .AddResponseProperty("Tag", CustomApiParameterType.String);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var context = localContext.PluginExecutionContext;
            context.OutputParameters["InputKeys"] = string.Join(",", context.InputParameters.Keys);
            if (context.SharedVariables.TryGetValue("tag", out string tag))
            {
                context.OutputParameters["Tag"] = tag;
            }

            if (context.InputParameters.TryGetValue("SetUnregisteredOutput", out bool setUnregistered) && setUnregistered)
            {
                context.OutputParameters["Unregistered"] = "unregistered";
            }
        }
    }
}
