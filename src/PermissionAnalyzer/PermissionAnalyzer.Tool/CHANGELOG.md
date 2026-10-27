# Changelog

All notable changes to XrmMockup.PermissionAnalyzer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### v1.0.0-rc.1 - 23 October 2026
* Add: `xrmmockup-permissions`, which lists the Dataverse table privileges, and privileges not tied to a table, that a solution or project needs, found by static analysis of its `IOrganizationService`, request and `OrganizationServiceContext` calls
* Add: Checking them against security roles from the XrmMockup metadata, with `--fail-on-missing` to exit with `1` when a privilege is missing (`--strict` also fails on what couldn't be checked)
* Add: Entry points (XrmPluginCore plugin steps and custom APIs, plugin steps from the metadata, code activities and Azure Functions), followed through the call graph and dependency injection, with the identity each operation runs as
* Add: Plugin cascades, including filtered attributes, bypass parameters and plugin code from other solutions (`--plugin-solution`)
* Add: Workflows from the metadata that an operation fires are reported as unresolved, since their steps aren't analyzed
* Add: Scopes to analyze one entry point or user operation (`--plugin`, `--custom-api`, `--function`, `--entry`, `--operation`)
* Add: Roles per identity from `--role`, `--caller-role` and the `Principals` and `Users` settings, with `Suppress` and `Rules` for custom messages, under `XrmMockup:Permissions` in `appsettings.json`
* Add: Text, JSON, Markdown and SARIF output (`--format`), with `--detail` levels and `--table`, `--privilege`, `--runs-as` and `--basis` filters. The default shows only the privileges to change per role
* Add: Operations whose table can't be determined are reported as unresolved, with the reason
