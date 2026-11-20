//using System.IO;
//using DG.XrmFramework.BusinessDomain.ServiceContext;
//using Xunit;

//namespace DG.XrmMockupTest
//{
//    public class TestCWAAccountOptional : UnitTestBase
//    {
//        public TestCWAAccountOptional(XrmMockupFixture fixture) : base(fixture) { }

//        [Fact]
//        public void TestWorkFlowWithEmptyOptionalParameter()
//        {
//            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "CWAAccountoptional.xml"));

//            var account = new Account();

//            account.Id = orgAdminService.Create(account);
//        }

//        [Fact]
//        public void TestWorkFlowWithFilledOptionalParameter()
//        {
//            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "CWAAccountoptionalWithDateValue.xml"));

//            var account = new Account();

//            account.Id = orgAdminService.Create(account);
//        }
//    }
//}
