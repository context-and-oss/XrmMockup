namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    public class CreateAccountApi : Plugin
    {
        public CreateAccountApi()
        {
            RegisterCustomAPI("CreateAccount", Execute)
                .AddRequestParameter("Name", CustomApiParameterType.String)
                .AddResponseProperty("Name", CustomApiParameterType.String);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var name = localContext.PluginExecutionContext.InputParameters["Name"] as string;

            service.Create(new Entity("account")
            {
                ["name"] = name
            });

            localContext.PluginExecutionContext.OutputParameters["Name"] = name;
        }
    }
}
