# Example: Owner Teams

[Documentation](../README.md)

> This is a fixture excerpt: `businessUnit1` is a `BusinessUnit`; `team1`, `user1`, `parentAccount`, and `account1` are `EntityReference` fields. Generated types, SDK/query namespaces, and services must be supplied by the test project. The role privilege assumptions still need review; see [R5](../documentation-review.md#r5-example-prerequisites-and-test-framework).

## Scenario
A member of a owner team should be able to access any records that the team it self has privilege to.

## Creating The Test
The following test ensures that the specification is met. Note that the TypeDeclarations.cs file should be included in the solution and the namespace `DG.Tools.XrmMockup` should be referenced.
The code shown in this example will use our early-bound context generator, XrmContext. We use this, since it has some great features compared to CrmSvcUtil. For a comparison, look at [https://github.com/delegateas/XrmContext/wiki/functionality](https://github.com/delegateas/XrmContext/wiki/functionality)

```csharp
public void Initialize()
{
    EntityReference businessUnitId = crm.RootBusinessUnit;

    businessUnit1 = new BusinessUnit {
        ParentBusinessUnitId = businessUnitId,
        Name = "Business Unit 1" };
    businessUnit1.Id = orgAdminService.Create(businessUnit1);

    // SystemCustomizer - read account - user level, write account - user level
    var team = new Team {
        Name = "Team",
        BusinessUnitId = businessUnit1.ToEntityReference() };
    // CreateTeam enforces owner-team type.
    team1 = crm.CreateTeam(orgAdminService, team,
        SecurityRoles.SystemCustomizer).ToEntityReference();

    user1 = crm.CreateUser(orgAdminService, businessUnitId,
        SecurityRoles.SystemCustomizer).ToEntityReference();

    parentAccount = new EntityReference(Account.EntityLogicalName,
        orgAdminService.Create(new Account { Name = "Parent 1" }));

    var account = new Account {
        Name = "Child 1",
        ParentAccountId = parentAccount,
        OwnerId = team1 };
    account1 = new EntityReference(Account.EntityLogicalName,
        orgGodService.Create(account));

}

[TestMethod]
public void TeamAccessTest()
{
    Assert.IsFalse(CanRead(user1, account1));

    crm.AddUsersToTeam(team1, user1);
    Assert.IsTrue(CanRead(user1, account1));

    crm.RemoveUsersFromTeam(team1, user1);
    Assert.IsFalse(CanRead(user1, account1));
}

public bool CanRead(EntityReference user, EntityReference account)
{
    try
    {
        var entity = crm.CreateOrganizationService(user.Id).Retrieve(
            Account.EntityLogicalName, account.Id, new ColumnSet(true));
        return entity != null;
    }
    catch (FaultException e)
    {
        return false;
    }
}
```
