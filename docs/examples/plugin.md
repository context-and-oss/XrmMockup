# Example: Plugin

[Documentation](../README.md)

> This migrated example assumes the generated context/types, services, and registered plugin or workflow described below. It is not a standalone test project. See [R5](../documentation-review.md#r5-example-prerequisites-and-test-framework) for incomplete setup; workflow screenshot issues are tracked in [R6](../documentation-review.md#r6-workflow-screenshots-and-waiting-workflow-setup).

## Scenario
Each time an account is created, a new contact must be created, which is then set as the primary contact for the account.

## Creating the test
The following test ensures that the specification is met. The code shown in this example will use our early-bound context generator, XrmContext. We use this, since it has some great features compared to CrmSvcUtil. For a comparison, look at [https://github.com/delegateas/XrmContext/wiki/functionality](https://github.com/delegateas/XrmContext/wiki/functionality)
```csharp
[TestMethod]
public void TestPrimaryContactIsCreated()
{
    using (var context = new Xrm(orgAdminUIService))
    {
        var acc = new Account();
        acc.Id = orgAdminUIService.Create(acc);

        var retrieved = context.AccountSet.Where(x => x.AccountId == acc.Id).FirstOrDefault();
        Assert.IsNotNull(retrieved.PrimaryContactId);
        var primaryContact = context.ContactSet.Where(x => x.ContactId == retrieved.PrimaryContactId.Id).FirstOrDefault();
        Assert.IsNotNull(primaryContact);
    }
}
```

This test will fail until the plugin is created, which implements the logic.

## Creating the plugin
The following plugin creates a new contact whenever a new account is created, and sets the new contact as primaryContact on the account.

```csharp
public class AccountPrimaryContactPlugin : Plugin {

    public AccountPrimaryContactPlugin()
        : base(typeof(AccountPrimaryContactPlugin)) {
        RegisterPluginStep<Account>(
            EventOperation.Create,
            ExecutionStage.PostOperation,
            Execute);

    }

    protected void Execute(LocalPluginContext localContext) {
        if (localContext == null) {
            throw new ArgumentNullException("localContext");
        }

        // get service from context
        var service = localContext.OrganizationService;

        // create new contact
        var contact = new Contact();
        contact.Id = service.Create(contact);

        // set as primaryContact
        var acc = new Account(localContext.PluginExecutionContext.PrimaryEntityId);
        acc.PrimaryContactId = contact.ToEntityReference();
        service.Update(acc);
    }
}
```
The test is now green, since the plugin is automatically triggered by XrmMockup whenever an account is created.
