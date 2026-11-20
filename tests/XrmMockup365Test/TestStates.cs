using System;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestStates : UnitTestBase
    {
        public TestStates(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestSetState()
        {
            var acc = new Account()
            {
                StateCode = account_statecode.Active,
                StatusCode = account_statuscode.Active
            };
            acc.Id = orgAdminUIService.Create(acc);
            acc.SetState(orgAdminUIService, account_statecode.Inactive, account_statuscode.Inactive);

            var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
            Assert.Equal(account_statecode.Inactive, retrieved.StateCode);
        }

        [Fact]
        public void TestStatusTransitions()
        {
            var man = new ctx_parent()
            {
                statecode = ctx_parent_statecode.Active,
                statuscode = ctx_parent_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);
            orgAdminUIService.Execute(new SetStateRequest
            {
                EntityMoniker = man.ToEntityReference(),
                State = new Microsoft.Xrm.Sdk.OptionSetValue((int)ctx_parent_statecode.Inactive),
                Status = new Microsoft.Xrm.Sdk.OptionSetValue((int)ctx_parent_statuscode.Inactive)
            });

            var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, man.Id, new ColumnSet(true)) as ctx_parent;
            Assert.Equal(ctx_parent_statecode.Inactive, retrieved.statecode);
            Assert.Equal(ctx_parent_statuscode.Inactive, retrieved.statuscode);

            try
            {
                orgAdminUIService.Execute(new SetStateRequest
                {
                    EntityMoniker = man.ToEntityReference(),
                    State = new Microsoft.Xrm.Sdk.OptionSetValue((int)ctx_parent_statecode.Active),
                    Status = new Microsoft.Xrm.Sdk.OptionSetValue((int)ctx_parent_statuscode.Active)
                });
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition()
        {
            var man = new ctx_parent()
            {
                statecode = ctx_parent_statecode.Active,
                statuscode = ctx_parent_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, man.Id, new ColumnSet(true)) as ctx_parent;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = ctx_parent_statecode.Inactive.ToString(),
                NewStatus = (int)ctx_parent_statuscode.Inactive
            };

            var response = orgAdminUIService.Execute(request) as IsValidStateTransitionResponse;
            Assert.True(response.IsValid);
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenStateCodeInvalid()
        {
            var man = new ctx_parent()
            {
                statecode = ctx_parent_statecode.Active,
                statuscode = ctx_parent_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, man.Id, new ColumnSet(true)) as ctx_parent;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = ctx_parent_statecode.Active.ToString(),
                NewStatus = (int)ctx_parent_statuscode.Inactive
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenStatusCodeInvalid()
        {
            var man = new ctx_parent()
            {
                statecode = ctx_parent_statecode.Active,
                statuscode = ctx_parent_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, man.Id, new ColumnSet(true)) as ctx_parent;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = ctx_parent_statecode.Inactive.ToString(),
                NewStatus = (int)ctx_parent_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenEntityIsMissing()
        {
            var request = new IsValidStateTransitionRequest
            {
                NewState = ctx_parent_statecode.Active.ToString(),
                NewStatus = (int)ctx_parent_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenNewStateIsMissing()
        {
            var man = new ctx_parent()
            {
                statecode = ctx_parent_statecode.Active,
                statuscode = ctx_parent_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, man.Id, new ColumnSet(true)) as ctx_parent;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewStatus = (int)ctx_parent_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenNewStatusIsMissing()
        {
            var man = new ctx_parent()
            {
                statecode = ctx_parent_statecode.Active,
                statuscode = ctx_parent_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, man.Id, new ColumnSet(true)) as ctx_parent;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = ctx_parent_statecode.Active.ToString()
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenRecordDoesNotExist()
        {
            var request = new IsValidStateTransitionRequest
            {
                Entity = new Microsoft.Xrm.Sdk.EntityReference(ctx_parent.EntityLogicalName, Guid.NewGuid()),
                NewState = ctx_parent_statecode.Active.ToString(),
                NewStatus = (int)ctx_parent_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenEnforceStateTransitionsFalse()
        {
            // Originally backed by dg_field (an entity that does NOT enforce state transitions).
            // dg_field was removed; the standard "account" entity also does not enforce transitions,
            // so it stands in here to exercise the "enforce transitions = false" code path.
            var account = new Account()
            {
                StateCode = account_statecode.Active,
                StatusCode = account_statuscode.Active,
            };
            account.Id = orgAdminUIService.Create(account);

            var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = account_statecode.Inactive.ToString(),
                NewStatus = (int)account_statuscode.Inactive
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenSameState()
        {
            var man = new ctx_parent()
            {
                statecode = ctx_parent_statecode.Active,
                statuscode = ctx_parent_statuscode.Active,
            };
            man.Id = orgAdminUIService.Create(man);
            var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, man.Id, new ColumnSet(true)) as ctx_parent;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = ctx_parent_statecode.Active.ToString(),
                NewStatus = (int)ctx_parent_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestStateStatuscodeCreate()
        {
            // Migrated: the original also asserted Incident default state/status (Incident removed); kept the
            // Account portion, which verifies default statecode/statuscode behaviour on create.
            var accId = orgAdminService.Create(new Account { StateCode = account_statecode.Inactive });
            var acc = orgAdminService.Retrieve(Account.EntityLogicalName, accId, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(account_statecode.Active, acc.StateCode);
            Assert.Equal(account_statuscode.Active, acc.StatusCode);

            accId = orgAdminService.Create(new Account { StatusCode = account_statuscode.Active });
            acc = orgAdminService.Retrieve(Account.EntityLogicalName, accId, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(account_statecode.Active, acc.StateCode);
            Assert.Equal(account_statuscode.Active, acc.StatusCode);
        }

        [Fact]
        public void TestStateStatuscodeUpdate()
        {
            var acc = new Account { };
            acc.Id = orgAdminService.Create(acc);
            var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(account_statecode.Active, retrieved.StateCode);
            Assert.Equal(account_statuscode.Active, retrieved.StatusCode);

            acc.StateCode = account_statecode.Inactive;
            orgAdminService.Update(acc);
            retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(account_statecode.Inactive, retrieved.StateCode);
            Assert.Equal(account_statuscode.Inactive, retrieved.StatusCode);

            acc.StateCode = null;
            acc.StatusCode = account_statuscode.Active;
            orgAdminService.Update(acc);

            retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(account_statecode.Active, retrieved.StateCode);
            Assert.Equal(account_statuscode.Active, retrieved.StatusCode);
        }
    }
}
