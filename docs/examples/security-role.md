# Example: Security Role

[Documentation](../README.md)

> This example assumes downloaded metadata containing a `Scheduler` role that denies Lead Create. It is not a universal role definition. Its broader setup and assertion style are pending review under [R5](../documentation-review.md#r5-example-prerequisites-and-test-framework).

## Scenario
A user with the security role Scheduler should not be able to create a lead.

## Creating the test
The following test ensures that the specification is met. Note that the TypeDeclarations.cs file should be included in the solution and the namespace `DG.Tools.XrmMockup` should be referenced.
The code shown in this example will use our early-bound context generator, XrmContext. We use this, since it has some great features compared to CrmSvcUtil. For a comparison, look at [https://github.com/delegateas/XrmContext/wiki/functionality](https://github.com/delegateas/XrmContext/wiki/functionality)
```csharp
[TestMethod]
public void TestCreateSecurity() {
    using (var context = new Xrm(orgAdminUIService)) {
        var orgUser = new SystemUser();
        orgUser.Id = orgAdminUIService.Create(orgUser);

        var businessunit = new BusinessUnit();
        businessunit.Id = orgAdminUIService.Create(businessunit);

        var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.Scheduler);
        var service = crm.CreateOrganizationService(user.Id);
        try {
            service.Create(new Lead());
            Assert.Fail();
        } catch (Exception e) {
            Assert.IsInstanceOfType(e, typeof(FaultException));
        }
    }
}
```
When a new businessunit is created, it automatically gets security roles attached to it. Since security roles should only be added to the root businessunit, the roles are simply copied from there. All security roles from your CRM instance are available as generated GUID members of the `SecurityRoles` struct. These roles can be passed to the CreateUser or CreateTeam function, which returns a new record, where the security roles are set. An organization service can be created by providing the id of a systemuser. This service allows the developer to test whether the user can do certain actions within the context of the application, where all of the actions are automatically checked by XrmMockup for validity.
