# Example: CodeActivity

[Documentation](../README.md)

> This migrated example assumes the generated context/types, services, and registered plugin or workflow described below. It is not a standalone test project. See [R5](../documentation-review.md#r5-example-prerequisites-and-test-framework) for incomplete setup; workflow screenshot issues are tracked in [R6](../documentation-review.md#r6-workflow-screenshots-and-waiting-workflow-setup).

## Scenario
Whenever an account is created with the name "Wap", the name should be appended with "setFromCodeActivity".

## Creating the test
The following test ensures that the specification is met. The code shown in this example will use our early-bound context generator, XrmContext. We use this, since it has some great features compared to CrmSvcUtil. For a comparison, look at [https://github.com/delegateas/XrmContext/wiki/functionality](https://github.com/delegateas/XrmContext/wiki/functionality)

```csharp
[TestMethod]
public void TestCreateWorkflow() {
    using (var context = new Xrm(orgAdminUIService)) {
        var acc = new Account();
        acc.Name = "Wap";
        acc.Id = orgAdminUIService.Create(acc);

        var retrieved = context.AccountSet.Where(x => x.AccountId == acc.Id).FirstOrDefault();
        Assert.AreEqual(acc.Name + "setFromCodeActivity", retrieved.Name);
    }
}
```
In order for this test to work a codeactivity and a workflow is created.

## Creating the codeActivity
A codeactivity is created, which implements the logic of appending the name. This codeactivity can then be used inside a workflow on your CRM instance, to ensure that it's run only when a new account is created with the name "Wap".
```csharp
public sealed partial class AccountWorkflowActivity : CodeActivity {
    protected override void Execute(CodeActivityContext executionContext) {
        var traceService = executionContext.GetExtension<ITracingService>()
            as ITracingService;
        var workflowExecutionContext =
            executionContext.GetExtension<IWorkflowContext>()
            as IWorkflowContext;
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
        var account = orgService.Retrieve(Account.EntityLogicalName, accRef.Id, new ColumnSet("name")) as Account;
        account.Name = account.Name + "setFromCodeActivity";
        orgService.Update(account);
        this.appendedName.Set(executionContext, account.ToEntityReference());
    }

    // Define Input/Output Arguments
    [RequiredArgument]
    [Input("name")]
    [ReferenceTarget(Account.EntityLogicalName)]
    public InArgument<EntityReference> name { get; set; }

    [Output("appendedName")]
    [ReferenceTarget(Account.EntityLogicalName)]
    public OutArgument<EntityReference> appendedName { get; set; }
}
```

Now the MetadataGenerator executable is run in order to get the new workflow from your CRM instance. After that the test is green, since the workflow is automatically executed by XrmMockup, which in turn executes the codeactivity.
