using System.Linq;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestPublishXmlRequestHandler : RequestHandlerTestBase
    {
        public TestPublishXmlRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("PublishXml")));
        }

        [Theory]
        [InlineData("<importexportxml><entities /></importexportxml>", true)]
        [InlineData("<invalid />", false)]
        [InlineData("<importexportxml>", false)]
        [InlineData("", false)]
        public void PublishValidatesXml(string xml, bool valid)
        {
            var request = new PublishXmlRequest { ParameterXml = xml };
            if (valid) Assert.IsType<PublishXmlResponse>(orgAdminService.Execute(request));
            else Assert.Throws<FaultException>(() => orgAdminService.Execute(request));
        }
    }
}
