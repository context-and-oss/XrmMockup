using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Xml.Linq;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestExecuteFetchRequestHandler : RequestHandlerTestBase
    {
        public TestExecuteFetchRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("ExecuteFetch")));
        }

        [Fact]
        public void FetchResultsEscapeXmlAndUseInvariantNumbersWithoutProxyRequirements()
        {
            var account = new Account { Name = "A & B <test>", Revenue = 1234.56789m };
            account.Id = orgAdminService.Create(account);
            var oldCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");
                var response = (ExecuteFetchResponse)orgAdminService.Execute(new ExecuteFetchRequest { FetchXml = "<fetch><entity name='account'><attribute name='name'/><attribute name='revenue'/></entity></fetch>" });
                var row = XDocument.Parse(response.FetchXmlResult).Root.Element("result");
                Assert.Equal("A & B <test>", row.Element("name").Value);
                Assert.Equal(((Money)Read("account", account.Id)["revenue"]).Value.ToString(CultureInfo.InvariantCulture), row.Element("revenue").Value);
            }
            finally { CultureInfo.CurrentCulture = oldCulture; }
        }

        [Fact]
        public void FetchHandlesAliasesLookupsAndPaging()
        {
            var contact = new Contact { FirstName = "A & <B>" }; contact.Id = orgAdminService.Create(contact);
            orgAdminService.Create(new Account { Name = "First", PrimaryContactId = contact.ToEntityReference() });
            orgAdminService.Create(new Account { Name = "Second", PrimaryContactId = contact.ToEntityReference() });
            var response = (ExecuteFetchResponse)orgAdminService.Execute(new ExecuteFetchRequest { FetchXml = "<fetch count='1' page='1'><entity name='account'><attribute name='primarycontactid'/><link-entity name='contact' from='contactid' to='primarycontactid' alias='c'><attribute name='firstname'/></link-entity></entity></fetch>" });
            var xml = XDocument.Parse(response.FetchXmlResult);
            Assert.Equal("1", (string)xml.Root.Attribute("morerecords"));
            Assert.Equal("A & <B>", xml.Root.Element("result").Element("c.firstname").Value);
            Assert.Equal(contact.Id.ToString("D").ToUpperInvariant(), xml.Root.Element("result").Element("primarycontactid").Value);
        }

        [Fact]
        public void FetchRejectsMissingXml()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new ExecuteFetchRequest()));
        }

        [Fact]
        public void FetchUsesLegacyImplicitEntityAlias()
        {
            var contact = new Contact { FirstName = "Legacy alias" }; contact.Id = orgAdminService.Create(contact);
            orgAdminService.Create(new Account { PrimaryContactId = contact.ToEntityReference() });
            var response = (ExecuteFetchResponse)orgAdminService.Execute(new ExecuteFetchRequest
            {
                FetchXml = "<fetch><entity name='account'><link-entity name='contact' from='contactid' to='primarycontactid'><attribute name='firstname'/></link-entity></entity></fetch>"
            });
            Assert.Equal("Legacy alias", XDocument.Parse(response.FetchXmlResult).Root.Element("result").Element("contact.firstname").Value);
        }

        [Fact]
        public void FetchCountWithoutPageMeansFirstPageNotTopLimit()
        {
            orgAdminService.Create(new Account { Name = "First" });
            orgAdminService.Create(new Account { Name = "Second" });
            var response = (ExecuteFetchResponse)orgAdminService.Execute(new ExecuteFetchRequest
            {
                FetchXml = "<fetch count='1'><entity name='account'><attribute name='name'/></entity></fetch>"
            });
            var root = XDocument.Parse(response.FetchXmlResult).Root;
            Assert.Single(root.Elements("result"));
            Assert.Equal("1", (string)root.Attribute("morerecords"));
        }
    }
}
