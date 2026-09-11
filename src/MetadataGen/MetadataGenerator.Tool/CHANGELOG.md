# Changelog

All notable changes to XrmMockup.MetadataGenerator will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### v1.2.0 - 11 September 2026
* Add: `--credential-type` (`-t`) to override DataverseConnection's `DataverseCredentialType` setting — `browser` (the default), `devicecode` or `azcli`
* Add: `--dataverse-url` (`-u`) to override the `DataverseUrl` setting
* Change: Documentation aligned with DataverseConnection's current configuration keys (`DataverseUrl`/`DataverseCredentialType`, legacy `DATAVERSE_URL`/`DATAVERSE_CREDENTIAL_TYPE` still honoured) and its browser-auth default. CI/CD examples now select a non-interactive credential
* Change: Bumped the `xrmcontext` tool used by the regeneration scripts to 4.0.0-beta.26, which bundles the current DataverseConnection; the repo's own `appsettings.json` files now use the `DataverseUrl` key
* Fix: The regeneration script now installs the pinned `xrmcontext` version on every run, and locates the tool manifest wherever the SDK writes it, so a version bump actually takes effect

### v1.1.0 - 1 September 2026
* Fix: Updated to DataverseConnection v1.2.5 (#347)

### v1.0.5 - 5 August 2026
* Fix: Handle PrivilegeDepth.RecordFilter like PrivilegeDepth.Global instead of throwing exception

### v1.0.4 - 23 March 2026
* Add: Show version header on start (#321)

### v1.0.3 - 6 March 2026
* Fix: Metadata for the package didn't get picked up after Directory.Build.props was created

### v1.0.2 - 6 March 2026
* Fix: More explicit handling around when all security roles are downloaded

### v1.0.1 - 16 January 2026
* Add: Only download security roles that are relevant for the solution, with ability to add security roles (#304)

### v1.0.0 - 16 December 2025

* Initial release
* Add: CLI tool for generating XrmMockup metadata from Dataverse
* Add: Support for solution-based entity filtering
* Add: Support for explicit entity list filtering
* Add: Pretty-print option for XML output formatting
* Add: Configuration via appsettings.json or CLI arguments
* Add: DataContract serialization compatible with existing XrmMockup metadata format
