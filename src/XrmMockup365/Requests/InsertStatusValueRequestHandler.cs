using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk.Metadata;

namespace DG.Tools.XrmMockup
{
    internal class InsertStatusValueRequestHandler : OptionValueRequestHandler
    {
        internal InsertStatusValueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "InsertStatusValue") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InsertStatusValueRequest>(orgRequest);
            ValidateLabel(request.Label);
            var entity = GetMetadataEntity(request.EntityLogicalName);
            var status = entity.Attributes.FirstOrDefault(a => a.LogicalName == request.AttributeLogicalName) as StatusAttributeMetadata;
            if (status?.OptionSet == null) throw new FaultException("The attribute must be a status column.");
            var state = entity.Attributes.OfType<StateAttributeMetadata>().FirstOrDefault();
            if (state?.OptionSet == null || !state.OptionSet.Options.Any(o => o.Value == request.StateCode))
                throw new FaultException("StateCode must identify an existing state.");
            var value = NewOptionValue(status.OptionSet, request.Value);
            entity = CloneMetadata(entity);
            status = (StatusAttributeMetadata)entity.Attributes.First(a => a.LogicalName == request.AttributeLogicalName);
            status.OptionSet.Options.Add(new StatusOptionMetadata
            {
                MetadataId = Guid.NewGuid(), Value = value, State = request.StateCode,
                Label = CopyLabel(request.Label),
                Description = request.Description == null ? null : CopyLabel(request.Description)
            });
            ReplaceMetadata(entity);
            var response = new InsertStatusValueResponse();
            response.Results["NewOptionValue"] = value;
            return response;
        }
    }
}
