using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    // Choice and status insertion share label handling, value allocation and copy-on-write metadata.
    internal abstract class OptionValueRequestHandler : RequestHandler
    {
        protected OptionValueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security, string requestName)
            : base(core, db, metadata, security, requestName) { }

        protected EntityMetadata GetMetadataEntity(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !metadata.EntityMetadata.TryGetValue(name, out var entity))
                throw new FaultException("A valid EntityLogicalName is required.");
            return entity;
        }

        protected static void ValidateLabel(Label label)
        {
            if (label == null || label.LocalizedLabels.Count == 0 || label.LocalizedLabels.Any(l => string.IsNullOrWhiteSpace(l.Label)))
                throw new FaultException("Label must contain non-empty localized labels.");
        }

        protected static Label CopyLabel(Label label)
        {
            if (label == null) return null;
            var copy = CloneMetadata(label);
            // The SDK request label commonly has only LocalizedLabels. Retrieved metadata also
            // carries the caller-language label used by the simulator's formatted-value logic.
            if (copy.UserLocalizedLabel == null && copy.LocalizedLabels.Count > 0)
                copy.UserLocalizedLabel = copy.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == 1033) ?? copy.LocalizedLabels[0];
            return copy;
        }

        protected static int NewOptionValue(OptionSetMetadata options, int? requested)
        {
            var value = requested ?? checked(Math.Max(100000000, options.Options.Where(o => o.Value.HasValue).Select(o => o.Value.Value).DefaultIfEmpty(0).Max() + 1));
            if (value < 0 || options.Options.Any(o => o.Value == value))
                throw new FaultException("The option value must be non-negative and unique within the choice.");
            return value;
        }

        protected static T CloneMetadata<T>(T value)
        {
            var serializer = new DataContractSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                stream.Position = 0;
                return (T)serializer.ReadObject(stream);
            }
        }

        protected void ReplaceMetadata(EntityMetadata entity)
        {
            metadata.EntityMetadata[entity.LogicalName] = entity;
            if (db.HasTable(entity.LogicalName)) db[entity.LogicalName].ReplaceMetadata(entity);
        }
    }
}
