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
* Add: Entry point discovery: XrmPluginCore plugin steps and custom APIs, plugin steps from `Metadata.xml`, code activities and Azure Functions (isolated and in-process). Requirements are now listed per entry point
* Add: Call graph across the solution, following services and tables passed as arguments, fields and constructor parameters, with the call path shown for each source
* Add: Dependency injection resolution of interface calls, falling back to every implementation (basis `all-implementations`)
* Add: The identity each requirement runs as (`Caller`, `User`, `ApplicationUser`, `ImpersonatedCaller`, `System`), including impersonation from `SetUserContext` and plugin step metadata, with assumptions listed when it can't be determined
* Add: `--plugin`, `--custom-api`, `--function` and `--entry` to analyze one entry point, and the `EntryPoints` setting for extra entry methods
* Add: Methods with service calls that no entry point reaches are listed separately
* Change: `--project` also analyzes the projects it references, so entry points reach code in shared libraries
* Change: A missing metadata folder is a warning, because security roles and plugin steps are then not used
* Add: `check` command, which checks the requirements against security roles from the metadata, per identity: `--role` for the caller and the application user, `--caller-role` for users acted for through `CallerId`, and the `Principals` and `Users` settings. Exits with `1` when a privilege is missing; `--strict` also fails on unresolved operations, unchecked identities and unverifiable privileges
* Add: `--role` and `--caller-role` on `required`, which mark each requirement as granted or missing without changing the exit code
* Add: `Principals`, `Users` and `Suppress` settings under `XrmMockup:Permissions`
* Add: Unknown role names are an error that suggests the closest names
* Add: Table resolution through the values helper methods return, with the arguments of each call, and through type arguments of generic methods
* Add: A `Not checked` line after the result, listing unresolved operations, identities without roles, the impersonation privilege and Append/AppendTo, and `unresolvedExecute` and `incomplete` in the JSON result. `check` warns when it passes with something not checked
* Add: A `Missing privileges` list after the result, with each missing privilege once, the roles that lack it and the entry points that need it, also as `missing` in JSON. The `check` error names the missing privileges
* Change: The counts line names unreached methods and counts distinct table privileges next to the requirements per entry point and method
