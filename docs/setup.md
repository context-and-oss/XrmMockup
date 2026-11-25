# Setup

[Documentation](README.md)

> **Legacy setup — review pending.** The generator configuration and MSTest base class below are preserved from the wiki, not a verified modern quickstart. For the .NET 10 metadata CLI, use the [Metadata Generator guide](../src/MetadataGen/MetadataGenerator.Tool/README.md). See [R1 and R2 in the review report](documentation-review.md#r1-legacy-setup-and-generator-configuration) before adopting the legacy instructions. Do not commit credentials or regenerate the repository's test metadata as a routine test step.

## 1 Install from nuget
Install the `XrmMockup365` NuGet package. The current engine targets .NET Framework 4.6.2, .NET Framework 4.8, and .NET 10; the repository no longer contains a separate engine project for each historical CRM version.

## 2 Configuring the MetadataGenerator
The configuration file is located at `your-test-project-folder/Metadata/MetadataGenerator/MetadataGenerator**.exe.config`, where ** is your version of XrmMockup.

The configuration file looks like the following. Replace the settings with your information.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="url" value="https://INSTANCE.crm4.dynamics.com/XRMServices/2011/Organization.svc" />
    <add key="username" value="admin@INSTANCE.onmicrosoft.com" />
    <add key="password" value="pass@word1" />
    <add key="domain" value="" />
    <add key="ap" value="OnlineFederation" />
    <add key="solutions" value="Contoso" />
    <add key="entities" value="" />
  </appSettings>
</configuration>
```

In order to use an entity inside a test, XrmMockup needs to know it exists. Add all entities that XrmMockup needs to handle in the `entities` argument. If you have a solution, then add it in the solutions argument. All entities inside those solutions will also be included.

### Supported Arguments
| Argument                       | Alias        | Description
| --                             | --           | --
| url                            |              | Environment.
| username                       | usr, u       | Authentication parameter.
| password                       | pwd, p       | Authentication parameter.
| domain                         | dmn, d       | Authentication parameter.
| method                         |              | Authentication parameter.
| authenticationprovider         | provider, ap | Authentication parameter.
| mfaAppId                       |              | Authentication parameter.
| mfaReturnUrl                   |              | Authentication parameter.
| mfaClientSecret                |              | Authentication parameter.
| connectionString               |              | Authentication parameter.
| outDir                         | out, o       | Output folder for generated files.
| entities                       | es           | Comma-separated list of tables. Context is generated for listed tables.
| solutions                      | ss           | Comma-separated list of solutions. Context is generated for all tables in listed solutions.
| projectPath                    | pp           | Path to the testing project.
| fetchFromAssemblies            | fa           | Context is generated for tables in assembly.
| mitigateDuplicateSecurityRoles |              | Handles multiple security roles having the same name.

Authentication parameters are used to connect to Dynamics. For documentation on how to connect we refer to [Microsoft's documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect).

## 3 Build your project
XrmMockup loads proxy types from your project's build paths, therefore be sure that the project has been build at least once, before generating the metadata.

## 4 Generating metadata
Run the generator executable located at `your-test-project-folder/Metadata/MetadataGenerator/MetadataGenerator**.exe`.

You should now notice that the metadata folder contains two new files, `Metadata.xml` and `TypeDeclarations.cs`, as well as two new folders, SecurityRoles and Workflows.

The metadata only resembles the crm instance from when it was fetched, therefore the MetadataGenerator should be executed after each change in your crm instance, in order to test those changes as well.


## 5 Defining a base test class
Now the metadata is setup and you're ready to test, but to make your life easier we present an example of a base test class, which all your test classes should extend, such that it becomes easier to write tests. After creating the base test class you're ready to write test, by simply extending it.

The base test class below uses MSTest. For a faster test setup consider using XUnit. For a base test class that works with XUnit see [XrmMockupFixture](../tests/XrmMockup365Test/XrmmockupFixture.cs) and [UnitTestBase](../tests/XrmMockup365Test/UnitTestBase.cs).

```csharp
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.Tools.XrmMockup;

namespace DG.XrmMockupTest {

	[TestClass]
	public class UnitTestBase {
		protected IOrganizationService orgAdminUIService;
		protected IOrganizationService orgAdminService;
		protected static XrmMockup365 crm;

		public UnitTestBase() {
			this.orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
			this.orgAdminService = crm.GetAdminService();
		}

		[TestCleanup]
		public void TestCleanup() {
			crm.ResetEnvironment();
		}


		[AssemblyInitialize]
		public static void InitializeServices(TestContext context) {
			InitializeMockup(context);
		}

		public static void InitializeMockup(TestContext context) {
			crm = XrmMockup365.GetInstance(new XrmMockupSettings {
				BasePluginTypes = new Type[] { typeof(Plugin) },
				CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
				EnableProxyTypes = true,
				IncludeAllWorkflows = true
			});
		}
	}
}
```
