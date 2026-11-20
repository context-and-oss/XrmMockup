using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk.Metadata;

namespace DG.Tools.XrmMockup
{
    internal class InsertOptionValueRequestHandler : OptionValueRequestHandler
    {
        internal InsertOptionValueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "InsertOptionValue") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<InsertOptionValueRequest>(orgRequest);
            ValidateLabel(request.Label);
            OptionSetMetadata options;
            EntityMetadata entity = null;
            if (!string.IsNullOrWhiteSpace(request.OptionSetName))
            {
                if (!string.IsNullOrWhiteSpace(request.EntityLogicalName) || !string.IsNullOrWhiteSpace(request.AttributeLogicalName))
                    throw new FaultException("Specify either a global choice or a local choice, not both.");
                options = metadata.OptionSets.OfType<OptionSetMetadata>().FirstOrDefault(o => o.Name == request.OptionSetName);
                if (options == null) throw new FaultException("The global choice does not exist.");
            }
            else
            {
                entity = GetMetadataEntity(request.EntityLogicalName);
                var attribute = entity.Attributes.FirstOrDefault(a => a.LogicalName == request.AttributeLogicalName) as EnumAttributeMetadata;
                if (!(attribute is PicklistAttributeMetadata || attribute is MultiSelectPicklistAttributeMetadata) || attribute.OptionSet == null || attribute.OptionSet.IsGlobal == true)
                    throw new FaultException("The attribute must be a local choice column.");
                options = attribute.OptionSet;
            }
            var value = NewOptionValue(options, request.Value);
            if (entity != null)
            {
                entity = CloneMetadata(entity);
                options = ((EnumAttributeMetadata)entity.Attributes.First(a => a.LogicalName == request.AttributeLogicalName)).OptionSet;
            }
            else options = CloneMetadata(options);
            options.Options.Add(new OptionMetadata(CopyLabel(request.Label), value)
            {
                MetadataId = Guid.NewGuid(), Description = request.Description == null ? null : CopyLabel(request.Description)
            });
            if (entity != null) ReplaceMetadata(entity);
            else
            {
                metadata.OptionSets = metadata.OptionSets.Select(o => o.Name == request.OptionSetName ? options : o).ToArray();
                // Columns referencing this global choice must observe the same metadata.
                foreach (var current in metadata.EntityMetadata.Values.ToArray())
                    if (current.Attributes.OfType<EnumAttributeMetadata>().Any(a => a.OptionSet?.IsGlobal == true && a.OptionSet.Name == request.OptionSetName))
                    {
                        var clone = CloneMetadata(current);
                        foreach (var attribute in clone.Attributes.OfType<EnumAttributeMetadata>().Where(a => a.OptionSet?.IsGlobal == true && a.OptionSet.Name == request.OptionSetName))
                            attribute.OptionSet = options;
                        ReplaceMetadata(clone);
                    }
            }
            var response = new InsertOptionValueResponse();
            response.Results["NewOptionValue"] = value;
            return response;
        }
    }
}
