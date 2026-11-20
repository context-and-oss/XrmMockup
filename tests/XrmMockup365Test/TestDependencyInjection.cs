using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System.Linq;
using TestPluginAssembly365.Plugins.ServiceBased;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestDependencyInjection : UnitTestBase
    {
        public TestDependencyInjection(XrmMockupFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void CallsDependencyInjectedServiceInPlugin()
        {
            var accountId = orgAdminService.Create(new Account
            {
                Name = "Test Account for DI",
                AccountNumber = "DI-123"
            });

            using (var xrm = new Xrm(orgAdminService))
            {
                var contact = xrm.ContactSet
                    .Where(c => c.ParentCustomerId.Id == accountId)
                    .Select(c => c.ContactId)
                    .ToList();

                Assert.Single(contact);
            }
        }

        [Fact]
        public void CallsDependencyInjectedServiceInPluginWithPreAndPostImages()
        {
            var parentAccountId = orgAdminService.Create(new Account
            {
                Name = "Parent Account for DI"
            });
            var accountId = orgAdminService.Create(new Account
            {
                Name = "Test Account for DI Pre/Post Image",
                ParentAccountId = new EntityReference(Account.EntityLogicalName, parentAccountId)
            });

            // Update the ParentAccountId to trigger the plugin
            orgAdminService.Update(new Account
            {
                Id = accountId,
                AccountNumber = "DI-456",
                ParentAccountId = null
            });

            using (var xrm = new Xrm(orgAdminService))
            {
                var tasks = xrm.TaskSet
                    .Where(t => t.RegardingObjectId.Id == accountId)
                    .OrderBy(t => t.Subject)
                    .ToList();

                Assert.Equal(4, tasks.Count);

                Assert.Collection(tasks,
                    t => Assert.Equal($"10 {nameof(IAccountService)}.{nameof(IAccountService.HandleUpdate)}(PostImage postImage) - PostImage ParentAccount: null", t.Subject),
                    t => Assert.Equal($"20 {nameof(IAccountService)}.{nameof(IAccountService.HandleUpdate)}(PreImage preImage) - PreImage ParentAccount: {parentAccountId}", t.Subject),
                    t => Assert.Equal($"30 {nameof(IAccountService)}.{nameof(IAccountService.HandleUpdate)}(PreImage preImage, PostImage postImage) - PreImage ParentAccountId: {parentAccountId}, PostImage ParentAccount: null", t.Subject),
                    t => Assert.Equal($"40 {nameof(IAccountService)}.{nameof(IAccountService.HandleUpdate)}() - AccountNumber: DI-456", t.Subject)
                );
            }
        }
    }
}
