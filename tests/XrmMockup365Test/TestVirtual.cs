using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace DG.XrmMockupTest
{
    public class TestVirtual : UnitTestBase
    {
        public TestVirtual(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void VirtualTest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var bus = new ctx_parent
                {
                    ctx_Name = "HelloBus"
                };
                var busId = orgAdminService.Create(bus);

                orgAdminService.Update(new ctx_parent
                {
                    ctx_parentId = busId,
                    ctx_Documenttypes = new List<ctx_parent_ctx_documenttypes>() { ctx_parent_ctx_documenttypes.Doc, ctx_parent_ctx_documenttypes.PDF }
                });

                var retrieved = orgAdminService.Retrieve(ctx_parent.EntityLogicalName, busId, new ColumnSet(true)) as ctx_parent;
                var dsdsds = context.ctx_parentSet.ToList();
                Assert.True(retrieved.ctx_Documenttypes.Any());
                Assert.Equal(new List<ctx_parent_ctx_documenttypes>() { ctx_parent_ctx_documenttypes.Doc, ctx_parent_ctx_documenttypes.PDF }, retrieved.ctx_Documenttypes);
            }
        }
    }
}
