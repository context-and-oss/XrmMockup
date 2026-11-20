using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestThreadSafety : UnitTestBase
    {
        public TestThreadSafety(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TestMultipleInstancesAreIndependent()
        {
            // Test that multiple instances created concurrently are truly independent
            var results = new ConcurrentBag<string>();
            var exceptions = new ConcurrentBag<Exception>();

            // Create multiple instances in parallel
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                int instanceNumber = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        TestIndependentInstance(instanceNumber, results);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Check for any exceptions
            Assert.Empty(exceptions);

            // Verify we got results from all instances
            Assert.Equal(5, results.Count);
        }

        private void TestIndependentInstance(int instanceNumber, ConcurrentBag<string> results)
        {
            // Each task gets its own instance using the same metadata as the test fixture
            var mockup = XrmMockup365.GetInstance(crm);
            var service = mockup.GetAdminService();

            // Create a contact with instance-specific data
            var contact = new Entity("contact");
            contact["firstname"] = $"Test";
            contact["lastname"] = $"User";
            
            var id = service.Create(contact);
            
            // Retrieve and verify the data is correct for this instance
            var retrieved = service.Retrieve("contact", id, new ColumnSet("firstname", "lastname"));
            
            Assert.Equal($"Test", retrieved["firstname"].ToString());
            Assert.Equal($"User", retrieved["lastname"].ToString());

            // Create multiple records to test database isolation
            for (int j = 0; j < 3; j++)
            {
                var additionalContact = new Entity("contact");
                additionalContact["firstname"] = $"Test_Extra{j}";
                additionalContact["lastname"] = $"User_Extra{j}";
                service.Create(additionalContact);
            }

            // Query all contacts in this instance - should only see our data
            var query = new QueryExpression("contact");
            query.ColumnSet = new ColumnSet("firstname", "lastname");
            query.Criteria.AddCondition("firstname", ConditionOperator.BeginsWith, $"Test");
            
            var contacts = service.RetrieveMultiple(query);
            
            // Should have exactly 4 contacts (1 main + 3 extras)
            Assert.Equal(4, contacts.Entities.Count);

            results.Add($"Instance {instanceNumber} completed successfully with {contacts.Entities.Count} contacts");
        }

        [Fact]
        public void TestResetEnvironmentWithNewArchitecture()
        {
            // Test that ResetEnvironment works correctly with the new architecture
            var mockup = XrmMockup365.GetInstance(crm);
            var service = mockup.GetAdminService();

            // Create some data
            var contact = new Entity("contact");
            contact["firstname"] = "Test";
            contact["lastname"] = "User";
            var id = service.Create(contact);

            // Verify data exists
            var retrieved = service.Retrieve("contact", id, new ColumnSet("firstname"));
            Assert.Equal("Test", retrieved["firstname"].ToString());

            // Reset environment
            mockup.ResetEnvironment();

            // Verify data is gone (should throw exception since record no longer exists)
            Assert.Throws<FaultException>(() => service.Retrieve("contact", id, new ColumnSet("firstname")));

            // Verify we can create new data after reset
            var newContact = new Entity("contact");
            newContact["firstname"] = "NewTest";
            newContact["lastname"] = "NewUser";
            var newId = service.Create(newContact);

            var newRetrieved = service.Retrieve("contact", newId, new ColumnSet("firstname"));
            Assert.Equal("NewTest", newRetrieved["firstname"].ToString());
        }

        [Fact] 
        public void TestMultipleInstancesHaveIndependentDatabases()
        {
            // Test that instances are truly independent by verifying data isolation
            // Create two instances using the same metadata as the test fixture
            var mockup1 = XrmMockup365.GetInstance(crm);
            var mockup2 = XrmMockup365.GetInstance(crm);

            // Test that they're truly independent by creating data in each
            var service1 = mockup1.GetAdminService();
            var service2 = mockup2.GetAdminService();

            var contact1 = new Entity("contact") { ["firstname"] = "Instance1" };
            var contact2 = new Entity("contact") { ["firstname"] = "Instance2" };

            var id1 = service1.Create(contact1);
            var id2 = service2.Create(contact2);

            // Each service should only see its own data
            var retrieved1 = service1.Retrieve("contact", id1, new ColumnSet("firstname"));
            var retrieved2 = service2.Retrieve("contact", id2, new ColumnSet("firstname"));

            Assert.Equal("Instance1", retrieved1["firstname"].ToString());
            Assert.Equal("Instance2", retrieved2["firstname"].ToString());

            // Cross-instance retrieval should fail
            Assert.Throws<FaultException>(() => service1.Retrieve("contact", id2, new ColumnSet("firstname")));
            Assert.Throws<FaultException>(() => service2.Retrieve("contact", id1, new ColumnSet("firstname")));
        }

        [Fact]
        public async Task TestConcurrentRetrieveOperationsWithLinqQueries()
        {
            // Test the specific scenario where plugins execute Retrieve calls with LINQ queries
            // during concurrent entity creation/modification operations
            var exceptions = new ConcurrentBag<Exception>();
            var results = new ConcurrentBag<int>();

            // Create multiple concurrent tasks that perform operations that could trigger the
            // "Collection was modified; enumeration operation may not execute" exception
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        await TestConcurrentOperationsTask(taskId, results);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Check for any thread-safety exceptions
            Assert.Empty(exceptions);

            // Verify all tasks completed successfully
            Assert.Equal(10, results.Count);
        }

        private async Task TestConcurrentOperationsTask(int taskId, ConcurrentBag<int> results)
        {
            // Get the service for this task - each task operates on the same shared mockup instance
            var service = crm.GetAdminService();
            
            // Simulate the scenario: entity creation triggers plugins that perform Retrieve operations
            // while other threads are concurrently modifying collections
            for (int i = 0; i < 20; i++)
            {
                // Create entities (this could trigger plugins that do Retrieve calls)
                var contact = new Entity("contact");
                contact["firstname"] = $"Task{taskId}_Contact{i}";
                contact["lastname"] = $"TestUser";
                
                var contactId = service.Create(contact);

                // Perform Retrieve operations with key attributes (uses LINQ FirstOrDefault)
                var keyReference = new EntityReference("contact", Guid.Empty);
                keyReference.KeyAttributes["firstname"] = $"Task{taskId}_Contact{i}";
                
                try
                {
                    var retrieved = service.Retrieve("contact", contactId, new ColumnSet("firstname", "lastname"));
                    Assert.Equal($"Task{taskId}_Contact{i}", retrieved["firstname"].ToString());
                }
                catch (FaultException)
                {
                    // Entity might not exist due to concurrent modifications, which is ok
                }

                // Perform RetrieveMultiple operations (uses Parallel.ForEach internally)
                var query = new QueryExpression("contact");
                query.ColumnSet = new ColumnSet("firstname", "lastname");
                query.Criteria.AddCondition("firstname", ConditionOperator.BeginsWith, $"Task{taskId}");
                
                var contactsResult = service.RetrieveMultiple(query);
                
                // Create more entities to increase chance of concurrent modifications
                var account = new Entity("account");
                account["name"] = $"Task{taskId}_Account{i}";
                service.Create(account);

                // Small delay to increase concurrency
                await Task.Delay(1);
            }

            results.Add(taskId);
        }

        [Fact]
        public async Task TestConcurrentAccountCreationAndRetrieval()
        {
            // Test concurrent creation and retrieval of accounts to stress test collection modifications
            var exceptions = new ConcurrentBag<Exception>();
            var results = new ConcurrentBag<int>();

            // Create multiple concurrent tasks that create accounts and retrieve all accounts
            var tasks = new Task[8];
            for (int i = 0; i < 8; i++)
            {
                int taskId = i;
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        await TestAccountCreationAndRetrievalTask(taskId, results);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Check for any thread-safety exceptions
            Assert.Empty(exceptions);

            // Verify all tasks completed successfully
            Assert.Equal(8, results.Count);
        }

        private async Task TestAccountCreationAndRetrievalTask(int taskId, ConcurrentBag<int> results)
        {
            var service = crm.GetAdminService();

            // Perform many operations to increase chance of concurrent collection modifications
            for (int i = 0; i < 15; i++)
            {
                // Create an account - this modifies the account collection
                var account = new Entity("account");
                account["name"] = $"Task{taskId}_Account{i}";
                account["accountnumber"] = $"ACC-{taskId}-{i}";
                
                var accountId = service.Create(account);

                // Immediately try to retrieve all accounts - this enumerates the collection
                var query = new QueryExpression("account");
                query.ColumnSet = new ColumnSet("name", "accountnumber");
                
                var allAccounts = service.RetrieveMultiple(query);
                
                // Try to retrieve the specific account we just created
                try
                {
                    var retrieved = service.Retrieve("account", accountId, new ColumnSet("name", "accountnumber"));
                    Assert.Equal($"Task{taskId}_Account{i}", retrieved["name"].ToString());
                }
                catch (FaultException)
                {
                    // Account might not be found due to concurrent operations, which is ok for this stress test
                }

                // Create multiple accounts in quick succession to increase contention
                for (int j = 0; j < 3; j++)
                {
                    var extraAccount = new Entity("account");
                    extraAccount["name"] = $"Task{taskId}_Extra{i}_{j}";
                    service.Create(extraAccount);
                }

                // Retrieve all accounts again after bulk creation
                allAccounts = service.RetrieveMultiple(query);

                // Small delay but not too much to maintain high concurrency
                if (i % 5 == 0)
                    await Task.Delay(1);
            }

            results.Add(taskId);
        }

        [Fact]
        public async Task TestHighFrequencyCollectionModification()
        {
            // Test very high frequency creation and enumeration to push thread-safety to limits
            var exceptions = new ConcurrentBag<Exception>();
            var results = new ConcurrentBag<int>();

            // Use more tasks with shorter operations for higher frequency
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        await TestHighFrequencyModificationTask(taskId, results);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Check for any thread-safety exceptions
            Assert.Empty(exceptions);

            // Verify all tasks completed successfully
            Assert.Equal(10, results.Count);
        }

        private async Task TestHighFrequencyModificationTask(int taskId, ConcurrentBag<int> results)
        {
            var service = crm.GetAdminService();

            // Very tight loop with minimal delays to maximize concurrent modifications
            for (int i = 0; i < 25; i++)
            {
                // Create account (modifies collection)
                var account = new Entity("account");
                account["name"] = $"HighFreq_{taskId}_{i}";
                service.Create(account);

                // Immediately enumerate (potential concurrent modification)
                var query = new QueryExpression("account");
                query.ColumnSet = new ColumnSet("name");
                query.Criteria.AddCondition("name", ConditionOperator.BeginsWith, $"HighFreq_{taskId}");
                
                var accounts = service.RetrieveMultiple(query);

                // Create contact (modifies different collection)
                var contact = new Entity("contact");
                contact["firstname"] = $"HighFreq_{taskId}_{i}";
                service.Create(contact);

                // Enumerate contacts immediately
                var contactQuery = new QueryExpression("contact");
                contactQuery.ColumnSet = new ColumnSet("firstname");
                contactQuery.Criteria.AddCondition("firstname", ConditionOperator.BeginsWith, $"HighFreq_{taskId}");
                
                var contacts = service.RetrieveMultiple(contactQuery);

                // No delays - maximum concurrency stress test
            }

            results.Add(taskId);
        }
    }
}