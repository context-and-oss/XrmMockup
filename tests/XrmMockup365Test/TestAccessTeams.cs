using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Crm.Sdk.Messages;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestAccessTeams : UnitTestBase
    {
    public TestAccessTeams(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void SimpleReadTest()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            //check that user 1 cannot see contact 2
            try
            {
                var checkContact = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have permission"))
                {
                    throw;
                }
            }

            var req = new AddUserToRecordTeamRequest();
            req.Record = contact2.ToEntityReference();
            req.SystemUserId = testUser1.Id;

            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, "TestReadContact");
            var tt = orgAdminService.RetrieveMultiple(q);

            req.TeamTemplateId = tt.Entities.First().Id;

            orgAdminService.Execute(req);

            //that user 1 can now see contact 2
            var checkContact2 = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));

            var removereq = new RemoveUserFromRecordTeamRequest();
            removereq.Record = contact2.ToEntityReference();
            removereq.SystemUserId = testUser1.Id;
            removereq.TeamTemplateId = tt.Entities.First().Id;

            orgAdminService.Execute(removereq);

            //check that user 1 cannot see contact 2
            try
            {
                var checkContact = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have permission"))
                {
                    throw;
                }
            }
        }

        [Fact]
        public void SimpleUpdateTest()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            //check that user 1 cannot update contact 2
            var updateContact = new Entity("contact");
            updateContact.Id = contact2.Id;
            updateContact["firstname"] = "fred";
            try
            {
                testUser1Service.Update(updateContact);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have write access"))
                {
                    throw;
                }
            }

            var req = new AddUserToRecordTeamRequest();
            req.Record = contact2.ToEntityReference();
            req.SystemUserId = testUser1.Id;

            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, "TestWriteContact");
            var tt = orgAdminService.RetrieveMultiple(q);

            req.TeamTemplateId = tt.Entities.First().Id;

            orgAdminService.Execute(req);

            //that user 1 can now see contact 2
            updateContact = new Entity("contact");
            updateContact.Id = contact2.Id;
            updateContact["firstname"] = "fred";
            testUser1Service.Update(updateContact);

            var removereq = new RemoveUserFromRecordTeamRequest();
            removereq.Record = contact2.ToEntityReference();
            removereq.SystemUserId = testUser1.Id;
            removereq.TeamTemplateId = tt.Entities.First().Id;

            orgAdminService.Execute(removereq);

            //check that user 1 cannot see contact 2
            updateContact = new Entity("contact");
            updateContact.Id = contact2.Id;
            updateContact["firstname"] = "bob";
            try
            {
                testUser1Service.Update(updateContact);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have write access"))
                {
                    throw;
                }
            }
        }

        [Fact]
        public void SimpleShareTest()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            //check that user 1 cannot share contact 2 with user 3
            var grant = new GrantAccessRequest();
            grant.PrincipalAccess = new PrincipalAccess() { Principal = testUser3.ToEntityReference(), AccessMask = AccessRights.ReadAccess };
            grant.Target = contact2.ToEntityReference();
            try
            {
                testUser1Service.Execute(grant);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have share access"))
                {
                    throw;
                }
            }

            //user 1 needs read access to be able to share contact 2 with read access to add him to the read access team first
            AddUserToAccessTeam("TestReadContact", contact2.ToEntityReference(), testUser1.Id);

            AddUserToAccessTeam("TestShareContact", contact2.ToEntityReference(), testUser1.Id);
            testUser1Service.Execute(grant);

            testUser3Service.Retrieve("contact", contact2.Id, new ColumnSet(true));
        }
        [Fact]
        public void TestUserAddedToMultipleSimpleAccessTeams()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            AddUserToAccessTeam("TestReadContact", contact2.ToEntityReference(), testUser1.Id);
            AddUserToAccessTeam("TestWriteContact", contact2.ToEntityReference(), testUser1.Id);
            AddUserToAccessTeam("TestDeleteContact", contact2.ToEntityReference(), testUser1.Id);

            var c2 = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));

            var updateContact = new Entity("contact");
            updateContact.Id = contact2.Id;
            updateContact["firstname"] = "fred";
            testUser1Service.Update(updateContact);

            testUser1Service.Delete("contact", contact2.Id);
        }
        [Fact]
        public void TestUserAddedToSingleComplexAccessTeam()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            AddUserToAccessTeam("TestMultipleContact", contact2.ToEntityReference(), testUser1.Id);
            
            var c2 = testUser1Service.Retrieve("contact", contact2.Id, new ColumnSet(true));

            var updateContact = new Entity("contact");
            updateContact.Id = contact2.Id;
            updateContact["firstname"] = "fred";
            testUser1Service.Update(updateContact);

            testUser1Service.Delete("contact", contact2.Id);
        }

        [Fact]
        public void SimpleAssignTest()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            AddUserToAccessTeam("TestWriteContact", contact2.ToEntityReference(), testUser1.Id);

            //check that user 1 cannot assign contact 2 to himself
            var updateContact = new Entity("contact");
            updateContact.Id = contact2.Id;
            updateContact["ownerid"] = testUser1.ToEntityReference();
            try
            {
                testUser1Service.Update(updateContact);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have assign access"))
                {
                    throw;
                }
            }

            AddUserToAccessTeam("TestAssignContact", contact2.ToEntityReference(), testUser1.Id);
            updateContact = new Entity("contact");
            updateContact.Id = contact2.Id;
            updateContact["ownerid"] = testUser1.ToEntityReference();
            testUser1Service.Update(updateContact);
        }

        [Fact]
        public void SimpleDeleteTest()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            try
            {
                testUser1Service.Delete("contact", contact2.Id);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("permission to access entity 'contact' for delete"))
                {
                    throw;
                }
            }

            var req = new AddUserToRecordTeamRequest();
            req.Record = contact2.ToEntityReference();
            req.SystemUserId = testUser1.Id;

            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, "TestDeleteContact");
            var tt = orgAdminService.RetrieveMultiple(q);

            req.TeamTemplateId = tt.Entities.First().Id;

            orgAdminService.Execute(req);

            //that user 1 can now delete contact 2
            testUser1Service.Delete("contact", contact2.Id);
        }

        [Fact]
        public void SimpleAppendTest()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var account = new Account()
            {
                Name = "test 1 account"
            };
            account.Id = testUser1Service.Create(account);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            var account2 = new Account()
            {
                Name = "test 2 account"
            };
            account2.Id = testUser2Service.Create(account2);

            AddUserToAccessTeam("TestWriteContact", contact2.ToEntityReference(), testUser1.Id);

            //check that user 1 does not have permission to append account 1 to contact 2
            var updateContact = new Contact();
            updateContact.Id = contact2.Id;
            updateContact.ParentCustomerId = new EntityReference("account", account.Id);

            try
            {
                testUser1Service.Update(updateContact);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have Append access"))
                {
                    throw;
                }
            }

            AddUserToAccessTeam("TestAppendContact", contact2.ToEntityReference(), testUser1.Id);
            updateContact = new Contact();
            updateContact.Id = contact2.Id;
            updateContact.ParentCustomerId = new EntityReference("account", account.Id);
            testUser1Service.Update(updateContact);
        }

        [Fact]
        public void SimpleAppendToTest()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var account = new Account()
            {
                Name = "test 1 account"
            };
            account.Id = testUser1Service.Create(account);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            var account2 = new Account()
            {
                Name = "test 2 account"
            };
            account2.Id = testUser2Service.Create(account2);

            AddUserToAccessTeam("TestWriteContact", contact2.ToEntityReference(), testUser1.Id);

            //check that user 1 does not have permission to append account 2 to contact 1
            var updateContact = new Contact();
            updateContact.Id = contact.Id;
            updateContact.ParentCustomerId = new EntityReference("account", account2.Id);

            try
            {
                testUser1Service.Update(updateContact);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have AppendTo access"))
                {
                    throw;
                }
            }

            AddUserToAccessTeam("TestAppendToAccount", account2.ToEntityReference(), testUser1.Id);
            updateContact = new Contact();
            updateContact.Id = contact.Id;
            updateContact.ParentCustomerId = new EntityReference("account", account2.Id);
            testUser1Service.Update(updateContact);
        }

        [Fact]
        public void RemovingAUserFromAnAccessTeamShouldLeaveHimInRemainingTeams()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);

            var account = new Account()
            {
                Name = "test 1 account"
            };
            account.Id = testUser1Service.Create(account);

            var contact2 = new Contact()
            {
                FirstName = "test 2"
            };
            contact2.Id = testUser2Service.Create(contact2);

            var account2 = new Account()
            {
                Name = "test 2 account"
            };
            account2.Id = testUser2Service.Create(account2);

            AddUserToAccessTeam("TestWriteContact", contact2.ToEntityReference(), testUser1.Id);

            //check that user 1 does not have permission to append account 1 to contact 2
            var updateContact = new Contact();
            updateContact.Id = contact2.Id;
            updateContact.ParentCustomerId = new EntityReference("account", account.Id);

            try
            {
                testUser1Service.Update(updateContact);
                throw new XunitException();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("does not have Append access"))
                {
                    throw;
                }
            }

            AddUserToAccessTeam("TestAppendContact", contact2.ToEntityReference(), testUser1.Id);
            updateContact = new Contact();
            updateContact.Id = contact2.Id;
            updateContact.ParentCustomerId = new EntityReference("account", account.Id);
            testUser1Service.Update(updateContact);

            RemoveUserFromAccessTeam("TestAppendContact", contact2.ToEntityReference(), testUser1.Id);

            //remove the account so it's not inheriting the permisson from there
            var updateContactE = new Entity("contact");
            updateContactE.Id = contact2.Id;
            updateContactE["parentcustomerid"] = null;
            testUser2Service.Update(updateContactE);

            //check they can still edit
            updateContact = new Contact();
            updateContact.Id = contact2.Id;
            updateContact["firstname"] = "bob";
            testUser1Service.Update(updateContact);
        }

        private void AddUserToAccessTeam(string accessTeamName, EntityReference record, Guid userId)
        {
            var req = new AddUserToRecordTeamRequest();
            req.Record = record;
            req.SystemUserId = userId;
            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, accessTeamName);
            var tt = orgAdminService.RetrieveMultiple(q);
            req.TeamTemplateId = tt.Entities.First().Id;
            orgAdminService.Execute(req);
        }

        private void AddUserToAccessTeam(string accessTeamName, EntityReference record, Guid userId,IOrganizationService service)
        {
            var req = new AddUserToRecordTeamRequest();
            req.Record = record;
            req.SystemUserId = userId;
            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, accessTeamName);
            var tt = orgAdminService.RetrieveMultiple(q);
            req.TeamTemplateId = tt.Entities.First().Id;
            service.Execute(req);
        }
        private void RemoveUserFromAccessTeam(string accessTeamName, EntityReference record, Guid userId)
        {
            var req = new RemoveUserFromRecordTeamRequest();
            req.Record = record;
            req.SystemUserId = userId;
            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, accessTeamName);
            var tt = orgAdminService.RetrieveMultiple(q);
            req.TeamTemplateId = tt.Entities.First().Id;
            orgAdminService.Execute(req);
        }
        private void RemoveUserFromAccessTeam(string accessTeamName, EntityReference record, Guid userId,IOrganizationService service)
        {
            var req = new RemoveUserFromRecordTeamRequest();
            req.Record = record;
            req.SystemUserId = userId;
            var q = new QueryExpression("teamtemplate");
            q.Criteria.AddCondition("teamtemplatename", ConditionOperator.Equal, accessTeamName);
            var tt = orgAdminService.RetrieveMultiple(q);
            req.TeamTemplateId = tt.Entities.First().Id;
            service.Execute(req);
        }

        [Fact]
        public void ShouldNotBeAbleToAddUserToAccessTeamWithoutSharePriveligeOnEntity()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);
            try
            {
                AddUserToAccessTeam("TestWriteContact", contact.ToEntityReference(), testUser2.Id, testUser4Service);
            }
            catch (Exception ex)
            {
                Assert.Contains("does not have share permission", ex.Message);
            }
        }
        
        [Fact]
        public void ShouldNotBeAbleToRemoveUserFromAccessTeamWithoutSharePriveligeOnEntity()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);
            AddUserToAccessTeam("TestWriteContact", contact.ToEntityReference(), testUser2.Id);
            try
            {
                RemoveUserFromAccessTeam("TestWriteContact", contact.ToEntityReference(), testUser2.Id, testUser4Service);
            }
            catch (Exception ex)
            {
                Assert.Contains("does not have share permission", ex.Message);
            }
        }

        [Fact]
        public void ShouldNotBeAbleToAddUserToAccessTeamWithoutRightsOnTheRecordWhichMatchTheAccessTeam()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);
            try
            {
                AddUserToAccessTeam("TestWriteContact", contact.ToEntityReference(), testUser2.Id, testUser3Service);
            }
            catch (Exception ex)
            {
                Assert.Contains("permission on the contact entity", ex.Message);
            }
        }

        [Fact]
        public void ShouldNotBeAbleToRemoveUserFromAccessTeamWithoutRightsOnTheRecordWhichMatchTheAccessTeam()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);
            AddUserToAccessTeam("TestWriteContact", contact.ToEntityReference(), testUser2.Id);
            try
            {
                RemoveUserFromAccessTeam("TestWriteContact", contact.ToEntityReference(), testUser2.Id,testUser3Service);
            }
            catch (Exception ex)
            {
                Assert.Contains("permission on the contact entity", ex.Message);
            }
        }

        [Fact]
        public void AddedUserMustHavePrivelegesOnTheEntityWhichMatchTheAccessTeam()
        {
            var contact = new Contact()
            {
                FirstName = "test 1"
            };
            contact.Id = testUser1Service.Create(contact);
            try
            {
                AddUserToAccessTeam("TestWriteContact", contact.ToEntityReference(), testUser5.Id, testUser1Service);
            }
            catch (Exception ex)
            {
                Assert.Contains("cannot join team", ex.Message);
            }
        }
    }
}
