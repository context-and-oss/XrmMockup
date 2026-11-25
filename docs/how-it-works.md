# How XrmMockup works

[Documentation](README.md)

XrmMockup simulates a real CRM instance, and therefore it needs data from your CRM instance. This is done by the MetadataGenerator, which queries your CRM instance for:
* Entity metadata
* Transaction currencies
* Data about your organization
* Plugins
* Workflows
* Security roles

This enables XrmMockup to fill in the gaps between your test and the metadata of your CRM instance. XrmMockup provides its own organization services, which you call inside your tests. Through these services you can execute real XRM/CRM SDK requests, which XrmMockup interprets like a real CRM instance. This includes checking for validation, ensuring security access and executing plugins and workflows.

All CRUD operations done by the requests, plugins and workflows are all applied to an internal database inside XrmMockup, which XrmMockup can clear at your command, such that you can run a new test. See the [setup example](setup.md) and the repository's [per-test UnitTestBase](../tests/XrmMockup365Test/UnitTestBase.cs). XrmMockup simulates supported behavior; it is not a complete implementation of every Dataverse request or platform feature.
