# XrmMockup

[![Build Status](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml)
![NuGet Version](https://img.shields.io/nuget/v/XrmMockup365?label=nuget%20%7C%20XrmMockup)
![NuGet Version (MetadataGenerator)](https://img.shields.io/nuget/v/XrmMockup.MetadataGenerator?label=nuget%20%7C%20MetadataGenerator)



XrmMockup simulates Dynamics 365/Dataverse locally using your downloaded metadata,
plugin assemblies, workflows, and security roles.

Write tests against its organization services to exercise supported platform behavior
and the interactions between your customizations without a live environment.
Plugins and workflows run automatically when supported requests trigger them.
XrmMockup is a simulator, not a complete clone of every Dataverse feature; unsupported
requests and differences from the live platform still require consideration.

Find more in-depth information and examples in the [documentation](docs/README.md).
The [migration review report](docs/documentation-review.md) records legacy guidance and documentation gaps that need human review.

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

## Releasing

Maintainers: see [RELEASE.md](RELEASE.md) for PR labels, repository setup, and independent product releases.
