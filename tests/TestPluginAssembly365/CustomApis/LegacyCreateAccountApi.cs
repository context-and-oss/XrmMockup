namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;

    public class LegacyCreateAccountApi : LegacyCustomApi
    {
        public LegacyCreateAccountApi() : base(typeof(LegacyCreateAccountApi))
        {
            RegisterCustomAPI(nameof(LegacyCreateAccountApi), Execute)
                .AddRequestParameter(new CustomAPIConfig.CustomAPIRequestParameter("Name", RequestParameterType.String))
                .AddResponseProperty(new CustomAPIConfig.CustomAPIResponseProperty("Name", RequestParameterType.String));
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException(nameof(localContext));
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
