### 1.20.2 - UNRELEASED
* Chore: Refresh the internal `workflow` option set enums against current Dataverse, and use them in place of the hardcoded `category`/`statecode` literals in the workflow manager and metadata generator (#352)

### 1.20.1 - 1 September 2026
* Fix: Match alternate keys by value, not reference (#350)

### 1.20.0 - 23 August 2026
* Breaking: XrmMockup365 now targets `net10.0` instead of `net8.0` (the `net462` target is unchanged). Required by DataverseConnection 1.2.x, which is a `net10.0` package
* Change: Updated `DataverseConnection` to 1.2.5. A bare `AddDataverse()` now defaults to `InteractiveBrowserCredential` instead of `DefaultAzureCredential`; set `DataverseCredentialType` to `azcli` or `devicecode` in configuration to change it

### 1.19.1 - 6 August 2026
* Fix: System-managed attributes copied onto Target for post-operation visibility no longer satisfy Update filtering attributes (#346)

### 1.19.0 - 5 August 2026
* Fix: Handle PrivilegeDepth.RecordFilter like PrivilegeDepth.Global instead of throwing exception (#345)

### 1.18.8 - 14 July 2026
* Fix: Align plugin Target with Dataverse for system-managed fields (#344)

### 1.18.7 - 9 July 2026
* Add: `QualifyLead` request support. (Based on code from @ladekarl in #51) (#343)
* Fix: Reject RetrieveMultiple queries filtering on non-existent attributes (#342)

### 1.18.6 - 7 July 2026
* Add: Collect Plugin Trace Logs and handle invalid formatting strings correctly (by throwing formatting exceptions) (#341)

### 1.18.5 - 1 July 2026
* Fix: Evaluating a classic calculated field during Retrieve/RetrieveMultiple no longer issues a spurious `Update` of the record. The calculated value is now computed and projected onto the returned entity without persisting, so reads no longer bump `modifiedon`, fire update plugins, or trip the update-time circular-reference guard (which rejected records carrying a self-referential lookup, e.g. a `systemuser` whose `createdby` is itself). Real workflows and rollup fields still update as before.
* Fix: RetrieveMultiple no longer throws when building the formatted value for a Money attribute that has no associated `transactioncurrencyid` (e.g. a calculated Money column projected onto a record without a currency); the formatted value is simply omitted.

### 1.18.4 - 1 July 2026
* Fix: AppendTo/security checks for business- and organization-owned entities (#339)

### 1.18.3 - 26 June 2026
* Fix: Classic calculated-field Concat threw `InvalidCastException` when a non-string column was concatenated with a string column, blocking Retrieve/RetrieveMultiple even if the calculated column was not requested (#338)
* Improvement: Retrieve and RetrieveMultiple now skip evaluating calculated and formula fields that are not referenced by the ColumnSet (or, for calc fields, by the criteria/orders/links) (#338)
* Fix: When the Workflows folder contained multiple XML files referencing the same workflow id (typically leftover files from older metadata generations), initialization threw a confusing internal "record already exists" FaultException. It now throws a `MockupException` at load time listing the conflicting files and instructing the user to re-generate metadata (#337)

### 1.18.2 - 25 June 2026
* Fix: Top level link criteria were unioned instead of cross joined (#336)

### 1.18.1 - 15 June 2026
* Fix: LinkCriteria and negative conditions did work correctly (#333)

### 1.18.0 - 23 March 2026
* Add: Added ability to log startup sequence and Plugin/API detection to a file (#314)
* Add: Support re-sending emails with Failed status in SendEmailRequestHandler (#318)
* Add: IPluginExecutionContext2-7 support to PluginContext (#323)

* Fix: Throw better exception when security role isn't found on create (#315)
* Fix: Ordinal sorting (byte-code based instead of locale) of the SecurityRoles struct to reduce unnessary changes to the file (#316)
* Fix: SendEmailRequestHandler to support all recipient entity types (#320)
* Fix: NullReferenceException when plugins with post-images fire via Multiple requests (#322)

### 1.17.11 - 02 March 2026
* Fix: Added validation of emailaddress on all to references in SendEmailRequest (@JonasGLund99) #313

### 1.17.10 - 24 February 2026
* Add: NotAny join support (@KristianKlodeD) (#281)

### 1.17.9 - 15 January 2026
* Add: File block upload/download support (@magesoe) (#307)

### 1.17.8 - 12 January 2026
* Add: Support for specifying which security roles are included (@magesoe) (#304)
* Add: Settings option to skip execution of workflows. Defaults to the value of TriggerProcesses but allows specific override (@mkholt) (#305)
* Add: Better error messages when workflow can't find code-activity (@mkholt) (#305)
* Add: Better error message when Core can't find Entity Metadata (@mkholt) (#305)

### 1.17.7 - 08 January 2026
* Fix: Use ConcurrentQueue for plugin execution to avoid issues with parallel tests
* Fix: Use ConcurrentDictionary for database tables to avoid issues with parallel tests

### 1.17.6 - 07 January 2026
* Fix: If metadata didn't contain the organization entity it would fail on initialization

### 1.17.5 - 16 December 2025
* Add: Full filtering support for RetrieveMetadataChangesRequest
* Add: Preliminary support for BooleanManagedProperty
* Add: Deprecation warning to metadata generator - replaced by dotnet tool

### 1.17.4 - 15 December 2025
* Fix: Issues with fullname and name on EntityReference

### 1.17.3 - 14 November 2025
* Add: Added IManagedIdentityService (@magesoe) (#300)

### 1.17.2 - 08 October 2025
* Refactor: Change plugin and workflow handling to use string instead of EventOperation (@mkholt) (#292)
* Add: Support Revise Quote Request (@Lucki2g) (#294)

### 1.17.1 - 03 October 2025
* Update: Move to new version of XrmPluginCore to support combined Plugin / CustomAPI base classes (@mkholt)

### 1.17.0 - 29 September 2025
* Add: Support for DG.XrmPluginCore based plugins and custom apis (@mkholt)
* Support: Remove .NET6 build target, since .NET6 is out of support by Microsoft. (@mkholt)
* Fix: Improve implementation of Win- and CloseQuoteRequestHandlers (@Lucki2g)
* Fix: Type conversion in RetrieveMultiple logic (@mkholt)
* Fix: Order was ignored in FetchXML parsing (@AnyTick)

### 1.16.1 - 23 September 2025
* Add: Support for Win- & CloseQuoteRequestHandlers (@Lucki2g)

### 1.16.0 - 12 September 2025
* Add: Caching of the static parts of XrmMockup, such that each test can get its own instance with their own db. (@magesoe)
* Add: Support for OverrideCreatedOn (@AnyTick)
* Fix: In/NotIn operators in FetchXML parsing (@AnyTick)

### 1.15.3 - 17 June 2025
* Add: Support for OlderThanX... DateTime operators in FetchXML parsing (@AnyTick)
* Fix: Make ToActivityPointer support custom activities by using the metadata provided objecttypecode for activitytypecode (@nedergaardcloud)

### 1.15.2 - 11 June 2025
* Added regardingobjectid to ToActivityPointer (@nedergaardcloud)
* Add: Implementation of "ends-width", "begins-with", and "like" filtering in FetchXML (@AnyTick)
* Fix: Ordering defaults to Ascending (@AnyTick)
* Fix: Filter comparisons for Null, NotNull, and DateTime gt, ge, lt, le (@AnyTick)

### 1.15.1 - 14 May 2025
* Update: Update to production release of Microsoft.PowerFx.Dataverse.Eval dependency (@mkholt)

### 1.15.0 - 13 May 2025
* Add: Support for ExecuteTransactionRequest (@magesoe)
* Add: Support for updating RelatedEntities via Create- and UpdateRequests (@Lucki2g)
* Add: Support for PowerFx based Formula fields in Retrieve and RetrieveMultiple requests (@mkholt)

* Fix: Set system fields (createdon, modifiedon, createdby, modifiedby) in the pre-operation stage of Create and Update requests (@mlRosenquist)
* Fix: Made PowerFx evaluation optional, since not all methods in PowerFx is supported by the Eval engine yet. (@magesoe)
* Fix: Calculations between Money and WholeNumber fields (@mkholt)
* Fix: Waiting workflows weren't cleared on reset (@magesoe)
* Fix: AlternateKeys weren't handled correctly in Create and Update (@magesoe)

### 1.14.5 - 19 March 2025
* Fix: Ordering failing in RetrieveMultipleRequestHandler when not selecting the attribute to order by (@mkholt)

### 1.14.4 - 07 March 2025
* Add ability to specify proxy classes through list of assemblies (@Lucki2g)

### 1.14.3 - 05 March 2025
* Add column comparison in MatchesCriteria (@magesoe)
* Throw better exception when attempting to format an invalid OptionSet value (@mkholt)

### 1.14.2 - 27 February 2025
* Fix assembly lookup to support SDK-Style .csproj files (@Kageschwump)

### 1.14.1 - 14 February 2025
* Add support for UpdateMultipleRequest (@mkholt)
* Add support for UpsertMultipleRequest (@mkholt)

### 1.14.0 - 14 February 2025
* Add support for CustomAPI execution (@magesoe)
* Support for CreateMultipleRequest (@magesoe)

### 1.13.3 - 29 November 2024
* Extend RetrieveMultipleRequestHandler with support for TopCount and Take (@mai98)
* Fix plugin vs workflow execution order (@mai98)

### 1.13.2 - 25 October 2024
* Support for InstantiateTemplateRequest (@nedergaardcloud)

### 1.13.1 - 18 September 2024
* Fix: An update request of Incident without a StateCode would throw NullReferenceException (@mkholt)

### 1.13.0 - 18 September 2024
* Add support for .NET8 (@mkholt)
* Remove support for versions older than 9 (365) (@mkholt)
* Set InitiatingUserId and UserId in workflow context (@mai98)

### 1.12.5 - 10 September 2024
* Fix: CloseIncidentRequestHandler now uses UpdateRequest instead of SetStateRequest (@mai98)
* Fix: Throw FaultException when trying to update a resolved Incident (@mai98)
* Add support for Retrieve plugins (@mai98)
* Support Distinct in RetrieveMultipleRequestHandler when using Query Expressions (@mai98)
* Support for SendEmailRequest and PartyList (@mai98)
* Add file block upload handlers, without file-storage (@magesoe @nedergaardcloud)

### 1.12.4 - 23 August 2024
* Add support for multiple ThenBy's in retrievemultiple (@misoeli)

### 1.12.3 - 22 December 2023
* Add initializer based on an existing instance, letting us copy an already initialized XrmMockup instance (@mkholt)
* Add optional tracing class factory to settings (@mkholt)
* Add timers during initialization (@mkholt)

### 1.12.2 - 24 August 2023
* Add flag to mitigate duplicate security role names (@mkholt)

### 1.12.1 - 12 May 2023
* Use Mockup service instead of XrmExtension (@magesoe)

### 1.12.0 - 16 December 2022
* Add support for RetrieveCurrentOrganization Request (@kato88)

### 1.11.2 - 23 November 2022
* Ensure metadata folder is added in both packages (@mkholt)
* Implement IOrganizationServiceAsync2 (@mkholt)

### 1.11.1 - 21 November 2022
* Ensure dlls are not loaded multiple times (@magesoe)

### 1.11.0 - 10 November 2022
* Add support for .NET6 (@mkholt)

### 1.10.1 - 24 August 2022
* Add TargetType to SetEntityProperty actions in workflow handling to ensure differing types are handled correctly (@mkholt)

### 1.10.0 - 14 March 2022
* Added SDK message processing step to plugin query (@kato88)
* Added snapshot to file feature (@tvbirch)

### 1.9.0 - 13 December 2021
* Added support for virtual datatype (@nillas12)

### 1.8.2 - 27 September 2021
* Log when metadata has been generated

### 1.8.1 - 25 May 2021
* Handle duplicate access right for same security role

### 1.8.0 - 09 April 2021
* Added support for AddUserToRecordTeam and RemoveUserFromRecordTeam requests (@MattTrinder1)

### 1.7.13 - 09 April 2021
* Fixed error with excess white-space in fullnames (@MattTrinder1)

### 1.7.12 - 16 March 2021
* Made plugin finding with DAXIF language agnostic

### 1.7.11 - 12 March 2021
* Added option to reset a single table in the database (@MattTrinder1)

### 1.7.10 - 12 March 2021
* Improved performance of RetrieveMultiple for large queries by up to 30 percent (@MattTrinder1)

### 1.7.9 - 12 March 2021
* Fixed updated to parent records in workflow (@MattTrinder1)
* Improved handling of type casting in workflows (@MattTrinder1)

### 1.7.8 - 12 March 2021
* isDocument on annotation is now automatically populated (@MattTrinder1)

### 1.7.7 - 05 March 2021
* Security checks are now skipped for lookups if the value is unchanged (@MattTrinder1)

### 1.7.6 - 05 March 2021
* Entity attributes are now cloned when the request is received. (@MattTrinder1)
* Team metadata is now generated by default.

### 1.7.5 - 12 February 2021
* Added BeginsWith, EndsWith and not versions (@MattTrinder1)

### 1.7.4 - 12 February 2021
* Entity attributes are now cloned (@MattTrinder1)

### 1.7.3 - 12 February 2021
* Added support for closeincident plugins
* Reverted breaking change to DAXIF registration plugins

### 1.7.2 - 05 February 2021
* String comparison in queries is now case insensitive (@MattTrinder1)

### 1.7.1 - 29 January 2021
* Added support for extensions to XrmMockup that tap into core

### 1.6.5 - 26 January 2021
* Added support for dictionary variables in workflows (@MattTrinder1)

### 1.6.4 - 26 January 2021
* Improved performance of fetching a row in the database (@MattTrinder1)

### 1.6.2 - 26 January 2021
* Security error messages now include the SecLib::AccessCheckEx2 tag (@MattTrinder1)

### 1.6.1 - 26 January 2021
* Plugins registered on AnyEntity are now handled in all cases (@MattTrinder1)

### 1.6.0 - 26 January 2021
* Plugin steps are now able have the same name but different assemblies (@MattTrinder1)

### 1.5.2 - 22 January 2021
* Fixed an error where assign cascaded to organization owned entities (@MattTrinder1)

### 1.5.1 - 22 January 2021
* Added support for RetrieveMetadataChangesRequest (@MattTrinder1)

### 1.5.0 - 22 January 2021
* Moved security checks to validation step in the pipeline (@MattTrinder1)

### 1.4.6 - 22 January 2021
* Added support for impersonating user in plugins (@MattTrinder1)

### 1.4.5 - 14 January 2021
* Fixed error when using entityreferences in linkentities.

### 1.4.4 - 14 January 2021
* Add formatted values for late-bound entities (@MattTrinder1)

### 1.4.3 - 14 January 2021
* Added calculated field calculation to retrieve multiple (@MattTrinder1)

### 1.4.2 - 14 January 2021
* Added roletemplates as actual records (@MattTrinder1)

### 1.4.1 - 14 January 2021
* Fixed error where tracing service throws an exception (@MattTrinder1)

### 1.4.0 - 25 November 2020
* CodeActivities are now allowed to extend abstract classes.
* Plugins without a base class will now be found if any class from the dll is referenced in the basePluginTypes setting.

### 1.3.3 - 25 November 2020
* Fixed issue where empty calculated fields caused retrieves to fail.
* Fixed issue with retrieving plugins registered on AnyEntity, that was not registered with DAXIF.

### 1.3.2 - 12 October 2020
* Fix issue in published nuget package where latest version of metadata generator was not added.

### 1.3.1 - 10 October 2020
* Fixed error with sales solution which had a role without a businessunit

### 1.3.0 - 07 September 2020
* Added support for RetrieveMultiple plugins
* Made some system attributes available in plugins

### 1.2.5 - 17 June 2020
* Added Connection String Authentication method

### 1.2.4 - 11 May 2020
* Fixed an issue in 2011 where default teams would fail to create

### 1.2.3 - 22 April 2020
* Fixed additional errors with orphaned security

### 1.2.2 - 08 April 2020
* Fixed generation error due to orphaned roleprivilege

### 1.2.1 - 24 March 2020
*  Added new method `AddSecurityRolesToPrinciple` for adding  security roles to a user or a team

### 1.2.0 - 21 February 2020
* Default teams are now added and managed in relation to businessunits.
* Custom workflows with optional parameters will now correctly be passed null values form workflows.
* The metadata generator will now fetch plugin images from selected solutions instead of a hard-coded solution.

### 1.1.1 - 18 February 2020
* Fix a bug with team security where members of an owner team had full access to any records the team owned
* Fixed a bug with XrmMockup365 when it was installed on projects without the tooling connector

### 1.1.0 - 11 February 2020
* Added better description when no default status reason exists
* Added support for client secret

### 1.0.0 - 31 January 2020
* It's happening!
* Inactive incidents can no longer be modified
* Fixed error with execution-ordering of sync and async plugins and workflows
* Excluded most standard workflows when generating metadata

### 0.13.3-beta - 12 November 2019
* Added TotalRecordCount to RetrieveMultipleResponse

### 0.13.1-beta - 10 September 2019
* Fixed an issue where CloseIncident request did not trigger plugins listening on update or set state of incident

### 0.13.0-beta - 18 July 2019
* XrmMockup is now back to include the changes introduced after 0.9.10
* Refactoring of the latest pull request to ensure that XrmMockup performance is not affected
* Fix issue in Retrieve Multiple request unable to handle missing entity alias on linked-entity and entity name on linked-entity filter criterias
* Re-added privilege check for Append and Append-To on create and update. This feature can be toggled of if necessary  
* Refactor internal check for user and team privilege
* Added new method `GetPrivilege` for retriving the privilege for a given user or team
* Added new method `HasPermission` that checks if a user or team has a given access to a record

### 0.12.0-beta - 10 April 2019
* Reverted to a stable version based on 0.9.10. Releases after 0.9.10 introduced severe performance degradation with a factor of 3-4 slower tests.
* Releases also introduced bugs and invalid CRM logic. Will work on reimplementing the changes added in later version, but will require time to ensure quality.
* Please use version between 0.9.10 and 0.12.0 if needed.
* added changes from release 0.11.1

### 0.11.1-beta - 01 April 2019
* Fixed a bug in calculated fields which expected caluclated fields based on lookup fields to always have a value.
* Added a new optional flag "BigBang" that simulates the entire universe.

### 0.11.0-beta - 04 March 2019
* Implemented FilterExpressions conditions across multiple LinkEntities (@aronmek)
* Changed the Created/Modified On fields to be Utc based. (@aronmek)
* Fixed error related to shared variables in plugincontext (@aronmek)

### 0.10.2-beta - 04 March 2019
* Fixed error when checking permissions in Deep privileges
* Now, XrmMockup also considers files in execution directory when looking for proxy types.

### 0.10.1-beta - 25 February 2019
* Added ConditionOperator NotLike
* Fixed error with Like, where both sides were wildcard

### 0.10.0-beta - 25 February 2019
* Added a check that ensures selected attributes exist on the entity.
* Fixed errors in MergeRequest.
* Fixed errors related to security checks for Parent Businessunit privilege.

### 0.9.11-beta - 18 February 2019
* Fixed error where default values for boolean and picklists were not set (thanks @majakubowski)
* Allows several plugin steps for the same plugin type (thanks @majakubowski)
* Now supports QueryByAttribute, RetrieveAttribute, RetrievePrincipalAccess & WhoAmI (thanks @majakubowski)
* Fixed error where null values was not handled correctly on create and update (thanks @majakubowski)
* Workflows: Added support for clear (thanks @majakubowski)
* Workflows: Added support for send email (thanks @majakubowski)
* Workflows: Various bug fixes and improvements (thanks @majakubowski)

### 0.9.10-beta - 16 November  2018
* Add support for formatted values in RetrieveMultiple

### 0.9.9-beta - 5 November  2018
* Add support for 'In' and 'NotIn' in conditional expression for query expressions

### 0.9.8-beta - September  2018
* Fixed a bug where members of a team could not read any records owned by a team in the same Business Unit og child Business Unit

### 0.9.7-beta - September 12 2018
* Added target type Guid, fixed bug when reference did not have value.

### 0.9.6-beta - August 16 2018
* Fixed a bug in delete request where a related entity only had N:N relationship.

### 0.9.5-beta - June 29 2018
* Fixed a bug when accessing ParentPluginContext on a PluginContext.

### 0.9.4-beta - June 22 2018
* Fixed a bug where registered plugins are disabled when XrmMockup is initialized first time

### 0.9.3-beta - June 13 2018
* Fixed a bug in ExecuteMultiple where IsFaulted was not set

### 0.9.2-beta - June 07 2018
* Fix bug in Owner Team where members could not access records that the team had access too

### 0.9.1-beta - June 07 2018
* Reverted handling of access teams
* Reverted TeamType for now as access teams are not supported
* CreateTeam now creates an owner team explicitly
* Fix bugs in AddMembersTeamRequest and RemoveMembersTeamRequest

### 0.9.0-beta - June 06 2018
* Add user priviliage check access through owner teams
* Update teams to handle Owner teams
* Updated CreateTeam methods to also excpect a team type
* Add new methods for adding or removing users from a team
* Fix bug when fetching security roles in Metadata Generator

### 0.8.1-beta - May 25 2018
* Added new method for registering additional plugins
* Added new method for disabling registered plugins
* It is now possible to test a selection of plugins
* XML documentation is added to nuget package

### 0.8.0-beta - May 23 2018
* Add new snapshot feature. It is now possible to take a snapshot of the database and swap between different snapshot making for faster initialization setup
* Fixed Assign Request to actually update the owner
* Fixed issue in cascading behaviour due to incorrect fetching of related entities.
* Implemented new requests:
 - IsValidStateTransition
 - CloseIncident
 - RetrieveExchangeRate
* Added filtering of metadata in RetrieveEntityRequest

### 0.7.3-beta - February 23 2018
* Changed dependency description of workflow dll for 365 version.

### 0.7.2-beta - February 21 2018
* Fixed bug where nuget package could be installed in projects running .NET <4.6.2 but did not actually work. Now nuget fails with an error if trying to install in projects running older versions of .NET framework.

### 0.7.1-beta - February 16 2018
* Fixed a bug where the primary entity id was not included beyond the first plugin execution in a chain.
* Fixed a bug where activitypointer was not included as a default entity in the metadata generator.

### 0.7.0-beta - February 12 2018
* Added handling of pre-validation plugins
* Added a new optional parameter in the XrmSettings for specifying the location of the directory containing the metadata files
* Changed the accessibility level for the Plugin class to be internal. The class was only intended for internal system plugins
* Added a new configuration parameter for MetadataGenerator to disable fetching of metadata of entities in local assemblies. This feature is now false as default
* Changed MetadataGenerator to also fetch entities from the list of specified solutions.
* The changes to which metadata generator means that the generation of metadata will take longer time to complete and might not contain the same entities as before. However, it is now more deterministic and consistent.

### 0.6.0-beta - Januar 12 2018
* Fixed an issue in SetEntityProperty where ExchangeRate could be null
* Updated live-debugging feature to work again that was previous lost in refactoring
* Fixed an issue where making two XrmMockup Instance resulted in EnableProxyType not working on second instance
* Updated target framwork to .NETFramework 4.6.2
* Fixed an issue where money field were incorrectly calculated

### 0.5.1-beta - December 21 2017
* Added a public getter for the base currency.
* Fixed error with Name property not being set on entityReferences.
* Fixed https://github.com/delegateas/XrmMockup/issues/26 concerning duplicate detection of N:N relations.

#### 0.5.0-beta - December 21 2017
* Added support for activity pointers
* Fixed bugs related to statecode and statuscode attributes

#### 0.4.4-beta - December 08 2017
* It's now illegal to create an organization service for a non-existing user
* Removed excessive console output

#### 0.4.3-beta - October 10 2017
* Added support for testing with data from CRM

#### 0.4.2-beta - September 21 2017
* Fixed https://github.com/delegateas/XrmMockup/issues/15 concerning implementing RetrieveOptionSetRequest
* Fixed https://github.com/delegateas/XrmMockup/issues/14 concerning adding formatted values to retrieved records

#### 0.4.1-beta - September 15 2017
* Fixed https://github.com/delegateas/XrmMockup/issues/17 with loading assemblies
* Fixed https://github.com/delegateas/XrmMockup/issues/16 with id being invalid in LINQ for context

#### 0.4.0-beta - September 01 2017
* Most classes have been moved to the namespace `DG.Tools.XrmMockup`.
* Changed the internal database implementation to act more like it's real counter-part.
* MetadataGenerator can now also consume command-line arguments, instead of only those in the .config file.
* Fixed handling of generic OrganizationRequests given a RequestName.
* Fixed a problem with `AddHours(...)` on XrmMockupBase.
* Fixed a problem with the stack trace being cut-off when an error occurred inside a plugin.
* Added a setting, which enables developers to choose to ignore requests, in case they aren't implemented, instead of throwing an exception.

#### 0.3.5-beta - July 19 2017
* Fixed bug with status transitions when they were disabled

#### 0.3.4-beta - July 19 2017
* Added support for status transitions
* Fixed invalid cast exception when creating an activity with ActivityParty set

#### 0.3.3-beta - July 11 2017
* Fixed queries ignoring certain conditions like `NotEqual` if attribute was null
* Fixed queries assuming that the linked entity-table already exists

#### 0.3.2-beta - July 7 2017
* Added support for `RetrieveEntityRequest`

#### 0.3.1-beta - July 4 2017
* A lot of minor code refactoring
* Rename of a few files
* Security roles GUIDs are now found at `DG.Tools.XrmMockup.SecurityRoles`
* Added `TypeDeclarations.cs` to NuGet package so it is automatically added to the target project

#### 0.3.0-beta - June 30 2017
* Changed to beta
* XrmMockupSettings now takes enumerables of CodeActivity and Plugin types
* Added `On`, `OnOrAfter`, and `OnOrBefore` as valid operators in workflow execution
* MetadataGenerator uses `appSettings` instead of the Visual Studio Settings designer
* Waiting workflows now properly reset during tests

#### 0.2.28-alpha - June 30 2017
* Bug fixes

#### 0.2.27-alpha - June 29 2017
* Orderby can now contain attributes that aren't selected in the query

#### 0.2.26-alpha - June 23 2017
* Added D365 version

#### 0.2.25-alpha - June 09 2017
* Fixed error with the ids of entities being set to the wrong attribute

#### 0.2.24-alpha - June 09 2017
* Associate and Dissasociate can now trigger plugins
* Intersect entities can now be retrieved if they have metadata

#### 0.2.23-alpha - May 24 2017
* Fixed error with plugins not triggering on win or lose

#### 0.2.22-alpha - May 24 2017
* Fixed error with plugins not triggering on win or lose

#### 0.2.21-alpha - May 24 2017
* Fixed error with ownerid not being set on createRequest
* Added support for Win and Lose Opportunity requests

#### 0.2.20-alpha - May 11 2017
* Fixed error with null values in entitycollection in workflow

#### 0.2.19-alpha - May 11 2017
* Fixed error with otherwise branches in workflows

#### 0.2.18-alpha - May 11 2017
* Fixed error with assembly loading

#### 0.2.17-alpha - May 10 2017
* Fixed errors with workflows

#### 0.2.16-alpha - May 05 2017
* Fixed errors with orderby in retrieveMultiple

#### 0.2.15-alpha - May 05 2017
* Plugins can now be registered from CRM in addition to DAXIF.

#### 0.2.14-alpha - May 04 2017
* Plugins no longer trigger on unsettable attributes
* Plugins listening to base currency attributes now trigger correctly 

#### 0.2.13-alpha - April 28 2017
* Bug fixes

#### 0.2.12-alpha - April 28 2017
* Bug fixes

#### 0.2.11-alpha - April 27 2017
* Added the option to choose whether a service acts as SDK or UI
* Bug fixes


#### 0.2.10-alpha - April 21 2017
* Metadata generation no longer requires the project to be build. Looks for proxy types in build directories.

#### 0.2.9-alpha - April 06 2017
* Bug fix

#### 0.2.8-alpha - March 24 2017
* Bug fix

#### 0.2.7-alpha - March 24 2017
* Made workflows and security roles modular
* Bug fixes

#### 0.2.6-alpha - March 23 2017
* MetadataGen now looks in debug dlls instead of release
* Bug fixes

#### 0.2.5-alpha - March 16 2017
* Bug fixes

#### 0.2.4-alpha - March 10 2017
* Added logic for workflows with related entities
* Changed the way businessunits and security roles declarations works
* Minor bug fixes

#### 0.2.3-alpha - March 2 2017
* Added logic for workflows with timeouts

#### 0.2.2-alpha - March 2 2017
* Now only workflows that needs to be triggered gets parsed

#### 0.2.1-alpha - March 2 2017
* No longer build for you, looks in release dir instead

#### 0.2.0-alpha - February 28 2017
* Support for security roles
* Workflow execution for crm 2016
* Bug fixes

#### 0.1.7-alpha - Januar 20 2017
* Minor bug fixes

#### 0.1.6-alpha - December 09 2016
* Fixed errors with precision in currencies and added support for calculated and rollup fields

#### 0.1.5-alpha - November 09 2016
* Added support for precision in currencies and fixed errors with standard currencies

#### 0.1.4-alpha - November 02 2016
* Fixed errors with preimage

#### 0.1.3-alpha - November 02 2016
* Added additional cases for currency base updates

#### 0.1.2-alpha - November 02 2016
* Fixed errors with metadata.fsx and assembly loading

#### 0.1.1-alpha - October 28 2016
* Added support for currencies and fixed errors with context

#### 0.1.0-alpha - October 10 2016
* Added metadata usage

#### 0.0.6-alpha - October 7 2016
* Fix to delete functionality

#### 0.0.5-alpha - September 22 2016
* More general fixes

#### 0.0.4-alpha - September 21 2016
* Retrieve and upsert with alternate key fix

#### 0.0.3-alpha - September 19 2016
* Retrieve with alternate key fix

#### 0.0.2-alpha - September 2016
* Early alpha build
