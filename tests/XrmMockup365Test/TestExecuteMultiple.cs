using System;
using System.Linq;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestExecuteMultiple : UnitTestBase
    {
        private Account account1;
        private Account account2;
        private Account account3;
        private Account account4;
        private Account account5;

        public TestExecuteMultiple(XrmMockupFixture fixture) : base(fixture)
        {
            account1 = new Account { Name = "Account 1" };
            account2 = new Account { Name = "Account 2" };
            account3 = new Account { Name = "Account 3" };
            account4 = new Account { Name = "Account 4" };
            account5 = new Account { Name = "Account 5" };
            account1.Id = orgAdminUIService.Create(account1);
            account2.Id = orgAdminUIService.Create(account2);
            account3.Id = orgAdminUIService.Create(account3);
            account4.Id = orgAdminUIService.Create(account4);
            account5.Id = orgAdminUIService.Create(account5);
        }

        [Fact]
        public void TestExecuteMultipleWithResults()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // Create an ExecuteMultipleRequest object.
                ExecuteMultipleRequest requestWithResults = new ExecuteMultipleRequest()
                {
                    // Assign settings that define execution behavior: continue on error, return responses. 
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    // Create an empty organization request collection.
                    Requests = new OrganizationRequestCollection()
                };

                // Create several (local, in memory) entities in a collection. 
                EntityCollection create = new EntityCollection()
                {
                    EntityName = Account.EntityLogicalName,
                    Entities = {
                        new Account { Name = "Account 1" },
                        new Account { Name = "Account 2" },
                        new Account { Name = "Account 3" },
                        new Account { Name = "Account 4" },
                        new Account { Name = "Account 5" }
                    }
                };

                // Add a CreateRequest for each entity to the request collection.
                foreach (var entity in create.Entities)
                {
                    CreateRequest createRequest = new CreateRequest { Target = entity };
                    requestWithResults.Requests.Add(createRequest);
                }

                // Execute all the requests in the request collection using a single web method call.
                ExecuteMultipleResponse responseWithResults =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithResults);

                Assert.Equal(5, responseWithResults.Responses.Count);
                Assert.False(responseWithResults.IsFaulted);
                // Display the results returned in the responses.
                foreach (var responseItem in responseWithResults.Responses)
                {
                    var response = responseItem.Response;
                    Assert.NotNull(response);
                    Assert.Null(responseItem.Fault);
                    Assert.IsType<CreateResponse>(response);

                    // make this Test work, values are the same, but any is not allowed for some reason
                    //Assert.True(context.AccountSet.Any(x => x.Id.Equals(response.Results["id"])));
                }
            }
        }

        [Fact]
        public void TestExecuteMultipleWithNoResults()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                ExecuteMultipleRequest requestWithNoResults = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to not continue after the first error is received
                    // and to not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };

                // Update the entities that were previously created.
                EntityCollection update = new EntityCollection()
                {
                    EntityName = Account.EntityLogicalName,
                    Entities =
                        {
                            new Account { Name = "Updated Account 1", Id = account1.Id },
                            new Account { Name = "Updated Account 2", Id = account2.Id },
                            new Account { Name = "Updated Account 3", Id = account3.Id },
                            new Account { Name = "Updated Account 4", Id = account4.Id },
                            new Account { Name = "Updated Account 5", Id = account5.Id },
                        }
                };

                foreach (var entity in update.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithNoResults.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithNoResults =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithNoResults);

                Assert.Empty(responseWithNoResults.Responses);
                Assert.False(responseWithNoResults.IsFaulted);
            }
        }

        [Fact]
        public void TestExecuteMultipleWithContinueOnError()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                ExecuteMultipleRequest requestWithContinueOnError = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };

                // Update the entities but introduce some bad attribute values so we get errors.
                EntityCollection updateWithErrors = new EntityCollection()
                {
                    EntityName = Account.EntityLogicalName,
                    Entities =
                        {
                            new Account { Name = "Updated Account 1", Id = account1.Id },
                            new Account { Name = "Updated Account 2", Id = new Guid() },
                            new Account { Name = "Updated Account 3", Id = account3.Id },
                            new Account { Name = "Updated Account 4", Id = new Guid() },
                            new Account { Name = "Updated Account 5", Id = account5.Id },
                        }
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithContinueOnError.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithContinueOnError =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithContinueOnError);

                Assert.Equal(2, responseWithContinueOnError.Responses.Count);
                Assert.True(responseWithContinueOnError.IsFaulted);
                Assert.True(responseWithContinueOnError.Responses.All(
                    x => x.Response == null && x.Fault != null && x.Fault.Message != null));
            }
        }

        [Fact]
        public void TestExecuteMultipleWithStopOnError()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                ExecuteMultipleRequest requestWithStopOnError = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };

                EntityCollection updateWithErrors = new EntityCollection()
                {
                    EntityName = Account.EntityLogicalName,
                    Entities =
                        {
                            new Account { Name = "Updated Account 1", Id = account1.Id },
                            new Account { Name = "Updated Account 2", Id = new Guid() },
                            new Account { Name = "Updated Account 3", Id = account3.Id },
                            new Account { Name = "Updated Account 4", Id = new Guid() },
                            new Account { Name = "Updated Account 5", Id = account5.Id },
                        }
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithStopOnError.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithStopOnError =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithStopOnError);

                Assert.Single(responseWithStopOnError.Responses);
                Assert.True(responseWithStopOnError.IsFaulted);
                Assert.True(responseWithStopOnError.Responses.All(
                    x => x.Response == null && x.Fault != null && x.Fault.Message != null));
            }
        }

        [Fact]
        public void TestExecuteMultipleWithContinueOnErrorAndReturn()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                ExecuteMultipleRequest requestWithContinueOnErrorAndReturns = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };

                EntityCollection updateWithErrors = new EntityCollection()
                {
                    EntityName = Account.EntityLogicalName,
                    Entities =
                        {
                            new Account { Name = "Updated Account 1", Id = account1.Id },
                            new Account { Name = "Updated Account 2", Id = new Guid() },
                            new Account { Name = "Updated Account 3", Id = account3.Id },
                            new Account { Name = "Updated Account 4", Id = new Guid() },
                            new Account { Name = "Updated Account 5", Id = account5.Id },
                        }
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithContinueOnErrorAndReturns.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithContinueOnErrorAndReturns =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithContinueOnErrorAndReturns);

                Assert.Equal(5, responseWithContinueOnErrorAndReturns.Responses.Count);
                Assert.Equal(2, responseWithContinueOnErrorAndReturns.Responses.Count(
                     x => x.Response == null && x.Fault != null && x.Fault.Message != null));
                Assert.Equal(3, responseWithContinueOnErrorAndReturns.Responses.Count(
                     x => x.Response != null && x.Fault == null));
                Assert.True(responseWithContinueOnErrorAndReturns.IsFaulted);
            }
        }

        [Fact]
        public void TestExecuteMultipleWithStopOnErrorAndReturn()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                ExecuteMultipleRequest requestWithStopOnErrorAndReturns = new ExecuteMultipleRequest()
                {
                    // Set the execution behavior to continue on an error and not return responses.
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };

                EntityCollection updateWithErrors = new EntityCollection()
                {
                    EntityName = Account.EntityLogicalName,
                    Entities =
                        {
                            new Account { Name = "Updated Account 1", Id = account1.Id },
                            new Account { Name = "Updated Account 2", Id = new Guid() },
                            new Account { Name = "Updated Account 3", Id = account3.Id },
                            new Account { Name = "Updated Account 4", Id = new Guid() },
                            new Account { Name = "Updated Account 5", Id = account5.Id },
                        }
                };

                foreach (var entity in updateWithErrors.Entities)
                {
                    UpdateRequest updateRequest = new UpdateRequest { Target = entity };
                    requestWithStopOnErrorAndReturns.Requests.Add(updateRequest);
                }

                ExecuteMultipleResponse responseWithStopOnErrorAndReturns =
                    (ExecuteMultipleResponse)orgAdminUIService.Execute(requestWithStopOnErrorAndReturns);

                Assert.Equal(2, responseWithStopOnErrorAndReturns.Responses.Count);
                Assert.Equal(1, responseWithStopOnErrorAndReturns.Responses.Count(
                     x => x.Response == null && x.Fault != null && x.Fault.Message != null));
                Assert.Equal(1, responseWithStopOnErrorAndReturns.Responses.Count(
                     x => x.Response != null && x.Fault == null));
                Assert.True(responseWithStopOnErrorAndReturns.IsFaulted);
            }
        }
    }
}