# XrmMockup

[![Build Status](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml)
![NuGet Version](https://img.shields.io/nuget/v/XrmMockup365?label=nuget%20%7C%20XrmMockup)
![NuGet Version (MetadataGenerator)](https://img.shields.io/nuget/v/XrmMockup.MetadataGenerator?label=nuget%20%7C%20MetadataGenerator)



XrmMockup is a tool which simulates ***your*** exact Dynamics 365/CRM instance locally including all of its logic in the form of workflows, plugins, and the security model.

This means you can write tests towards XrmMockup as though it was the real CRM instance, and check that it behaves as expected with all of your customizations.

You can test (and debug!) the entirety of the functionality you have implemented in your CRM system, with all 
the plugins and workflows automatically being run inside your test cases if necessary -- along with all the standard out-of-the-box CRM logic. 

Find more in-depth information and examples in the [wiki](https://github.com/delegateas/XrmMockup/wiki).

## Supported frameworks

XrmMockup365 targets .NET Framework 4.6.2, .NET Framework 4.8, and .NET 10.
The test suite runs on .NET Framework 4.8 and .NET 10, including regression coverage
for plugins compiled against .NET Framework 4.6.2.

## Features

* Simulates a target Dynamics 365/CRM instance by reading a local copy of its metadata.
* Supports executing most of the standard requests via an `IOrganizationService`.
* Automatically executes plugins and workflows during requests which trigger them.
* Enforces the security model of CRM, thus allowing you to test your security roles and business unit setup.
* Enables changing the time of the internal clock of XrmMockup during tests, which enables testing of time-based events (i.e waiting workflows or other time-sensitive business logic).

## Custom mock services

Register a caller-supplied service for plugins that request services beyond the
built-in execution context, tracing, organization-service factory, and managed
identity services:

```csharp
crm.AddService<IServiceEndpointNotificationService>(mockEndpointService);
// Plugins resolve this same object from their IServiceProvider.
crm.RemoveService<IServiceEndpointNotificationService>();
crm.ResetServices(); // Clear all custom registrations, not built-in services.
```

Registration is keyed by **exactly `T`**. `crm.AddService(new MyEndpointMock())`
registers `MyEndpointMock`, not its implemented interfaces. Specify the service
interface explicitly, as above, when that is what the plugin requests. Unknown,
removed, or cleared services return `null`, so plugins can probe optional services.

Null services throw `ArgumentNullException`; duplicate types and attempts to
register built-in service types throw `ArgumentException` without changing the
existing service. Removing an unregistered type is a no-op. Built-in services
cannot be replaced or removed through these APIs (use `TracingServiceFactory`
in settings to customize tracing).

Each mockup instance owns its registrations, even when instances reuse the same
settings and cached metadata. Every execution provider in that instance, including
nested plugin executions, sees the same registered objects and subsequent
registration changes. `ResetEnvironment()` resets simulation data but retains
custom registrations; use `ResetServices()` to clear them explicitly. XrmMockup
does not dispose registered objects: their lifecycle remains the caller's
responsibility. Configure registrations outside concurrent execution; these APIs
do not promise thread safety for registration changes or custom services.

## Releasing

Maintainers: see [RELEASE.md](RELEASE.md) for PR labels, repository setup, and independent product releases.
