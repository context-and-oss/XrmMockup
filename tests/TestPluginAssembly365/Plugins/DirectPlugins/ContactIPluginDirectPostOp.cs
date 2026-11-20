using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Some.Namespace
{
    public class ContactIPluginDirectPostOp : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            var contact = context.InputParameters["Target"] as Entity;
            if (contact.GetAttributeValue<string>("firstname") == "ChangeMePleasePostOp")
            {
                contact["firstname"] = "NameIsModifiedPostOp";
                service.Update(contact);
            }
        }
    }
}
