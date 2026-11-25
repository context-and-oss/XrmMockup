# Frequently asked questions

[Documentation](README.md)

> Generator options in this migrated FAQ refer to the legacy generator. The modern CLI has its own [configuration reference](../src/MetadataGen/MetadataGenerator.Tool/README.md); do not apply legacy keys to it. See [R1](documentation-review.md#r1-legacy-setup-and-generator-configuration).

### Historical comparison with FakeXrmEasy (review pending)

> The following comparison is preserved from the 2023 wiki. Its claims about another project have not been verified and must not be treated as a current feature comparison. See [R4](documentation-review.md#r4-third-party-comparison).
FakeXrmEasy approaches testing by forcing you to Mock everything yourself. XrmMockup has already mocked everything, which allows you to just write tests. This means all your processes can call each other - just like your real CRM system. The reasoning is that the real complexity of a CRM system comes from the chaining of processes, therefore XrmMockup tests the actual effects of your code in your system, which FakeXrmEasy does not completely do.

In addition, XrmMockup supports testing with security roles and testing Workflows, which is unsupported by FakeXrmEasy.

### Where are my traces?
The default tracing service writes `ITracingService` messages to the console, where your test runner can capture them. A custom `TracingServiceFactory` controls its own output, so console output is not guaranteed with a custom factory. See [TracingService.cs](../src/XrmMockup365/TracingService.cs). Additional trace and logging APIs are listed for review in [R9](documentation-review.md#r9-current-engine-features-without-user-guides).

### I got "Unable to cast object of type 'Microsoft.Xrm.Sdk.Entity' to type 'Db.Crm.Context.Account"
This error is the result of the test using a context that XrmMockup is not aware of. This can either be because `EnableProxyTypes` is not set to true or it can be that XrmMockup can't find your context through the plugin assemblies you have provided through the settings.

Set `EnableProxyTypes = true`. For explicit proxy-type selection, set `XrmMockupSettings.Assemblies` to the assembly containing the generated entity types (for example, `new[] { typeof(Account).Assembly }`). When no assemblies are specified, XrmMockup discovers suitable loaded/output assemblies. `BasePluginTypes` identifies plugin assemblies; it is not the explicit proxy-assembly setting. See [XrmMockupBase.cs](../src/XrmMockup365/XrmMockupBase.cs).

### My user / team have no security roles - how do I add them?
XrmMockup has helper functions called `CreateUser` and `CreateTeam` that create users or owner teams with a business unit and security roles. They require an `IOrganizationService`, a reference to a business unit (such as `crm.RootBusinessUnit`), and role GUIDs from the generated `Metadata/TypeDeclarations.cs`, exposed as `DG.Tools.XrmMockup.SecurityRoles.XXXX`.

The functions return a retrieve result of the created user / team.

`CreateTeam` creates owner teams. Access-team requests are also supported; see [TestAccessTeams.cs](../tests/XrmMockup365Test/TestAccessTeams.cs) for tested scenarios. This does not imply support for every Dataverse access-team behavior.

### The metadata generator seems to include to many entities, why?
The MetadataGenerator fetches as many entities as possible. Thus the entities given by the `entities` and `solutions` parameters are combined, as well as any entities given by any context that is referenced - unless the `fetchFromAssemblies` setting is set to false.

### Does XrmMockup support actions?
Yes, see [TestAction.cs](../tests/XrmMockup365Test/TestAction.cs) to see examples of tests using actions.
