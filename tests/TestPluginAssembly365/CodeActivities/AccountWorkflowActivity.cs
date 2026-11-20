using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.Some.Namespace {
    public sealed partial class AccountWorkflowActivity : CodeActivity {
        protected override void Execute(CodeActivityContext executionContext) {
            var traceService = executionContext.GetExtension<ITracingService>()
                as ITracingService;
            var workflowExecutionContext =
                executionContext.GetExtension<IWorkflowContext>()
                as IWorkflowContext;

            // Verify workflow context attributes: Initating User and User
            if (workflowExecutionContext.InitiatingUserId == Guid.Empty) throw new Exception("Initiating user is empty");
            if (workflowExecutionContext.UserId == Guid.Empty) throw new Exception("User is empty");

            var factory =
                executionContext.GetExtension<IOrganizationServiceFactory>()
                as IOrganizationServiceFactory;
            var orgService =
                factory.CreateOrganizationService(workflowExecutionContext.UserId)
                as IOrganizationService;
            var orgAdminService =
                factory.CreateOrganizationService(null)
                as IOrganizationService;

            var accRef = name.Get(executionContext);
            traceService.Trace("AccountWorkflowActivity executing for {0}", accRef.Id);
            var account = orgService.Retrieve(Account.EntityLogicalName, accRef.Id, new ColumnSet("name")) as Account;
            account.Name += "setFromCodeActivity";
            orgService.Update(account);
            this.doubleName.Set(executionContext, account.ToEntityReference());
        }

        // Define Input/Output Arguments
        [RequiredArgument]
        [Input("name")]
        [ReferenceTarget(Account.EntityLogicalName)]
        public InArgument<EntityReference> name { get; set; }

        [Output("doubleName")]
        [ReferenceTarget(Account.EntityLogicalName)]
        public OutArgument<EntityReference> doubleName { get; set; }
    }
}
