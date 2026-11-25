# Example: Workflow

[Documentation](../README.md)

> This migrated example assumes the generated context/types, services, and registered plugin or workflow described below. It is not a standalone test project. See [R5](../documentation-review.md#r5-example-prerequisites-and-test-framework) for incomplete setup; workflow screenshot issues are tracked in [R6](../documentation-review.md#r6-workflow-screenshots-and-waiting-workflow-setup).

## Scenario
When an account is created, the name should be changed to the following format "name - name " (including the trailing space asserted below).

## Creating the test
The following test ensures that the specification is met.
```csharp
[TestMethod]
public void TestStringAdd() {
    using (var context = new Xrm(orgAdminUIService)) {
        var acc = new Account();
        acc.Name = "WapAdd";
        acc.Id = orgAdminUIService.Create(acc);

        var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
        Assert.AreEqual(acc.Name + " - " + acc.Name + " ", retrieved.Name);
    }
}
```

## Creating the workflow
The logic is to be implemented by a workflow. The following workflow implements that logic.

![Workflow used in test for XrmMockup](http://i.imgur.com/vKmRQGD.png)

Then the MetadataGenerator is run, such that the new workflow is included in the metadata. Notice that the workflow is automatically run by XrmMockup when the account is created.
