# Organization services: UI vs SDK

[Documentation](README.md)

## The issue
XrmMockup recognizes that there is a difference between working with CRM though the UI and through the SDK. The service role selects UI-specific or SDK-specific behavior. In the current implementation, omitted Boolean defaults and picklist default values are populated during Create in **both** roles; they are not a UI-only distinction. UI-specific checks include read access to lookup targets during Create when Append/AppendTo checking is enabled. See [CreateRequestHandler.cs](../src/XrmMockup365/Requests/CreateRequestHandler.cs). These are simulator behaviors, not a claim that an organization service reproduces the complete Dataverse UI.

## The solution
XrmMockup's organization service factory methods accept `MockupServiceSettings`. Its `ServiceRole` property uses `MockupServiceSettings.Role.UI` or `Role.SDK`. The parameterless settings constructor selects SDK behavior and enables processes and workflows, without enabling writes to normally unsettable fields.

```csharp
var sdkService = crm.GetAdminService();
var uiService = crm.GetAdminService(new MockupServiceSettings(
    true, false, MockupServiceSettings.Role.UI));
```

See [MockupServiceSettings.cs](../src/XrmMockup365/MockupServiceSettings.cs) for constructor definitions.
