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

## Workflow diagnostics

Set `XrmMockupSettings.EnableWorkflowTracing = true` to enable workflow diagnostics.
It defaults to `false`. Diagnostics include workflow name and ID, mode, primary entity,
node execution details, failure, and completion. A paused workflow reports waiting rather
than success; subsequent executions report resume and retain the workflow identity.

```csharp
var settings = new XrmMockupSettings
{
    // Configure metadata, plugins and code activities as usual.
    EnableWorkflowTracing = true
};
```

Messages use the existing execution-scoped `ITracingService` and configured
`TracingServiceFactory`, including its collection/callback behavior. Disabling diagnostics
does not disable tracing from code activities or plugins. Invalid format strings with
arguments still throw `FormatException`; messages without arguments retain literal braces.

## Releasing

Maintainers: see [RELEASE.md](RELEASE.md) for PR labels, repository setup, and independent product releases.
