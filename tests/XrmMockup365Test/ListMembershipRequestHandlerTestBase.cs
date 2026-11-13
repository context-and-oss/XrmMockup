using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public abstract class ListMembershipRequestHandlerTestBase : RequestHandlerTestBase
    {
        protected ListMembershipRequestHandlerTestBase(XrmMockupFixture fixture) : base(fixture)
        {
            // Marketing-list metadata is local to tests of the two membership requests.
            AddTable("list", Text("listname"), Choice("createdfromcode", 1, 2, 4), Boolean("type"), Boolean("lockstatus"));
            AddTable("listmember", Lookup("listid", "list"), Lookup("entityid", "account", "contact", "lead"));
        }

        protected Guid List(bool dynamic = false) => Create("list", ("createdfromcode", new OptionSetValue(2)), ("type", dynamic), ("listname", "Contacts"));

        protected Guid Contact() => orgAdminService.Create(new Contact { FirstName = "Member" });
    }
}
