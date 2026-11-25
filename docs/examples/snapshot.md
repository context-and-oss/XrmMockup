# Example: Snapshot

[Documentation](../README.md)

> The shared-instance pattern below is historical. The repository now uses fresh simulations per test. Missing lifecycle and ZIP snapshot guidance is recorded in [R7](../documentation-review.md#r7-snapshot-scope-and-test-isolation); it has not been added to this example.

## Scenario
As a project gets more and more test, the data setup tends to grow as well and will end up taking more and more time of the test execution. This is especially true for XrmMockup, as all data is created through the service where multiple logical checks and actions are applied and executed.
* Validation of requests
* Privilege checks
* Auto filling of system fields
* Execution of system plugins

## Solution
In order to reduce the computation time for setting up data, we have added a snapshot feature where the current XrmMockup database can be stored, retrieved, and deleted during run-time. This means that it is only necessary to build the database once and then retrieve the snapshot during initialization of the test. It is even possible to swap between different snapshots during the test.

The example below uses MSTest's `AssemblyInitialize` to create data once for a shared simulation. This is not an API requirement: snapshots can be created whenever an instance is available, including in a test fixture. Snapshot names belong to that instance, so a fresh instance cannot restore another instance's named snapshot. Do not share a mutable instance between parallel tests without isolation.

## How
The named snapshot API has the following three methods:
* `TakeSnapshot`: Takes a snapshot of the XrmMockup database as it currently is at the time of the call
* `RestoreToSnapshot`: Overwrite the current XrmMockup database with the given snapshot
* `DeleteSnapshot`: Deletes the given snapshot. (This method is most likely unnecessary but were added for any possible use cases)
All three methods takes a string defining the name of the snapshot.

Datasetup and snapshot are created as followed:
```csharp
[AssemblyInitialize]
public static void InitializeMockup(TestContext context)
{
    crm = XrmMockup365.GetInstance(new XrmMockupSettings
    {
        ...
    });

    DataSetupBase();
    crm.TakeSnapshot("Base");

    DataSetupScenario1();
    crm.TakeSnapshot("Scenario1");

    crm.RestoreToSnapshot("Base");
    DataSetupScenario2();
    crm.TakeSnapshot("Scenario2");
}
```
It is possible to create different data setups for different scenarios that might build of the a base case. This does require the database to be restored to a snapshot of the base case.

Initialization and test execution is then done as followed:
```csharp
[TestClass]
public class Scenario1 : BaseTestClass
{
    [TestInitialize]
    public void Initialize()
    {
        crm.RestoreToSnapshot("Scenario1");
    }

    [TestMethod]
    public void UnitTest1()
    {
        ...
    }
}
```
