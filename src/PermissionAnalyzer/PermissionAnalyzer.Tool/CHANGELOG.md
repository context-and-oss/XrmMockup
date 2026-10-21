# Changelog

All notable changes to XrmMockup.PermissionAnalyzer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### Unreleased
* Add: `required` command, which lists the table privileges each method needs for its direct `IOrganizationService` calls (`Create`, `Retrieve`, `RetrieveMultiple`, `Update`, `Delete`, `Associate`, `Disassociate` and their async forms)
* Add: Table resolution from early-bound types, constants, `new Entity("…")`, query expressions, FetchXML, assignments to locals in the same method and loops over `RetrieveMultiple` results. Anything else is reported as unresolved, with the reason
* Add: Text and JSON output (`--format`), and tables that no downloaded security role mentions
* Add: Configuration under `XrmMockup:Permissions` in `appsettings.json` (`Solution`, `MetadataDirectory`, `ExcludeProjects`)
* Add: `--exclude` to leave out projects, such as test projects, by name or path pattern
