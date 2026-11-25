# Example: Debug using live data

[Documentation](../README.md)

> **Historical authentication example — do not use as current setup.** The `Env` fields below no longer exist in the current API, and online support is available only on .NET 10. The legacy instructions are retained for review rather than silently modernized. See [R3](../documentation-review.md#r3-live-data-authentication-and-platform-boundary) for the source evidence and proposed replacement.

## Setup
First you need to add information about your CRM instance, such that XrmMockup can fetch data during testing. This is done by setting the `OnlineEnvironment` setting in `XrmMockupSettings`.

```csharp
OnlineEnvironment = new Env {
    providerType = AuthenticationProviderType.OnlineFederation,
    uri = "https://yourinstance/XRMServices/2011/Organization.svc",
    username = "yourusername",
    password = "yourpassword"
}
```
Domain can also be added if needed. By setting this - XrmMockup now searches for any missing data inside your CRM system. Therefore this type of instance of XrmMockup is not suited for tests, but is useful when debugging. Therefore we recommend having two instance of XrmMockup, one for testing and one for debugging with live data.

## Using the debugging instance
In order to use the debugging instance you simply create an `IOrganizationService` for that instance.

```csharp
var crmRealData = XrmMockup365.GetInstance(realDataSettings);
var orgRealDataService = crmRealData.GetAdminService();
```

And then use that service inside a test - where you point to actual data from your CRM instance.
The code shown in this example will use our early-bound context generator, XrmContext. We use this, since it has some great features compared to CrmSvcUtil. For a comparison, look at [https://github.com/delegateas/XrmContext/wiki/functionality](https://github.com/delegateas/XrmContext/wiki/functionality)
```csharp
[TestMethod]
public void TestRealDataRetrieve() {
    var acc = new Account(new Guid("9155CF31-BA6A-E611-80E0-C4346BAC0E68")) { // actual account
        Name = "babuasd"
    };
    orgRealDataService.Update(acc); // update using correct service
    var retrieved = orgRealDataService.Retrieve(Account.EntityLogicalName, acc.Id, new
        ColumnSet(true)).ToEntity<Account>();
    Assert.AreEqual(acc.Name, retrieved.Name); // value we set inside the test
    Assert.AreEqual("12321123312", retrieved.AccountNumber); // value from your CRM instance
}
```
