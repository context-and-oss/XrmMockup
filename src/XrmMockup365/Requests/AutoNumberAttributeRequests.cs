using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal abstract class AutoNumberRequestHandler : RequestHandler
    {
        protected AutoNumberRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security, string name)
            : base(core, db, metadata, security, name) { }

        protected void ValidateAttribute(string entity, string attribute)
        {
            if (string.IsNullOrEmpty(entity) || string.IsNullOrEmpty(attribute) ||
                !metadata.EntityMetadata.TryGetValue(entity, out var table) ||
                !(table.Attributes.SingleOrDefault(a => a.LogicalName == attribute) is StringAttributeMetadata column) ||
                string.IsNullOrEmpty(column.AutoNumberFormat))
                throw new FaultException($"Attribute '{attribute}' of entity '{entity}' is not an auto-number attribute.");
        }
    }

    internal sealed class SetAutoNumberSeedRequestHandler : AutoNumberRequestHandler
    {
        internal SetAutoNumberSeedRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "SetAutoNumberSeed") { }

        internal override OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef)
        {
            var typed = MakeRequest<SetAutoNumberSeedRequest>(request);
            ValidateAttribute(typed.EntityName, typed.AttributeName);
            if (!typed.Parameters.TryGetValue("Value", out var value) || !(value is long))
                throw new FaultException("The auto-number seed Value must be an Int64.");
            core.AutoNumbers.SetSeed(typed.EntityName, typed.AttributeName, (long)value);
            return new SetAutoNumberSeedResponse();
        }
    }

    internal sealed class GetAutoNumberSeedRequestHandler : AutoNumberRequestHandler
    {
        internal GetAutoNumberSeedRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "GetAutoNumberSeed") { }

        internal override OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef)
        {
            var typed = MakeRequest<GetAutoNumberSeedRequest>(request);
            ValidateAttribute(typed.EntityName, typed.AttributeName);
            var response = new GetAutoNumberSeedResponse();
            response.Results["AutoNumberSeedValue"] = core.AutoNumbers.GetSeed(typed.EntityName, typed.AttributeName);
            return response;
        }
    }

    internal sealed class GetNextAutoNumberValueRequestHandler : AutoNumberRequestHandler
    {
        internal GetNextAutoNumberValueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "GetNextAutoNumberValue") { }

        internal override OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef)
        {
            var typed = MakeRequest<GetNextAutoNumberValueRequest>(request);
            ValidateAttribute(typed.EntityName, typed.AttributeName);
            var response = new GetNextAutoNumberValueResponse();
            response.Results["NextAutoNumberValue"] = core.AutoNumbers.GetNext(typed.EntityName, typed.AttributeName);
            return response;
        }
    }
}
