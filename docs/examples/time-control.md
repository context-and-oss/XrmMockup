# Example: Time Control

[Documentation](../README.md)

> This migrated example assumes the generated context/types, services, and registered plugin or workflow described below. It is not a standalone test project. See [R5](../documentation-review.md#r5-example-prerequisites-and-test-framework) for incomplete setup; workflow screenshot issues are tracked in [R6](../documentation-review.md#r6-workflow-screenshots-and-waiting-workflow-setup).

## Scenario
When an account is created, wait until a name is set on it and the currency is danish. Then append the name with itself and wait 2 days. After that, set the description to "qwerty".

## Creating the test
The following test ensures the specification is met. The code shown in this example will use our early-bound context generator, XrmContext. We use this, since it has some great features compared to CrmSvcUtil. For a comparison, look at [https://github.com/delegateas/XrmContext/wiki/functionality](https://github.com/delegateas/XrmContext/wiki/functionality)
```csharp
[TestMethod]
public void TestTimeWorkflow() {
    using (var context = new Xrm(orgAdminUIService)) {
        var acc = new Account();
        acc.Id = orgAdminUIService.Create(acc);

        var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
        Assert.IsNull(retrieved.Name);

        acc.Name = "Some name";
        orgAdminUIService.Update(acc);

        retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
        Assert.AreEqual(acc.Name + acc.Name, retrieved.Name);

        crm.AddDays(1);
        retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
        Assert.AreNotEqual("qwerty", retrieved.Description);

        crm.AddDays(2);
        retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
        Assert.AreEqual("qwerty", retrieved.Description);
    }
}
```

## The workflow
A workflow is created, which triggers according to the specification, this workflow is shown below.
![Workflow for testing time in XrmMockup](http://i.imgur.com/gcJUSPW.png)

Then the MetadataGenerator is run to include the new workflow in the metadata, after which the test is green. Notice that XrmMockup automatically runs your waiting workflows and you have the ability to control time by adding time to XrmMockup.
