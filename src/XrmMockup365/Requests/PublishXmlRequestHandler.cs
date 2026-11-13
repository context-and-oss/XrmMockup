using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System.Xml.Linq;

namespace DG.Tools.XrmMockup
{
    internal class PublishXmlRequestHandler : RequestHandler
    {
        internal PublishXmlRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "PublishXml") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<PublishXmlRequest>(orgRequest);
            if (string.IsNullOrWhiteSpace(request.ParameterXml)) throw new FaultException("ParameterXml is required.");
            try
            {
                var document = XDocument.Parse(request.ParameterXml);
                if (document.Root?.Name != "importexportxml") throw new FaultException("Expected an importexportxml root element.");
            }
            catch (System.Xml.XmlException ex) { throw new FaultException("Invalid ParameterXml: " + ex.Message); }
            // This simulator has one metadata layer; edits are immediately visible, so publishing is a no-op.
            return new PublishXmlResponse();
        }
    }
}
