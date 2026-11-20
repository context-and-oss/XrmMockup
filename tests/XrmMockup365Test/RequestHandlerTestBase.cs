using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public abstract class RequestHandlerTestBase : UnitTestBase
    {
        internal readonly Core engine;
        protected readonly MetadataSkeleton metadata;
        protected readonly XrmMockupSettings settings;

        protected RequestHandlerTestBase(XrmMockupFixture fixture) : base(fixture)
        {
            settings = fixture.Settings;
            engine = (Core)typeof(XrmMockupBase).GetField("Core", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(crm);
            metadata = (MetadataSkeleton)typeof(Core).GetField("metadata", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(engine);
        }

        protected static void Set(object target, string property, object value) => target.GetType().GetProperty(property).SetValue(target, value);

        protected AttributeMetadata Attribute(string source, string name)
        {
            var attribute = CloneMetadata(engine.GetEntityMetadata("account").Attributes.First(a => a.LogicalName == source));
            attribute.LogicalName = name;
            Set(attribute, "IsValidForCreate", true);
            Set(attribute, "IsValidForUpdate", true);
            Set(attribute, "IsValidForRead", true);
            return attribute;
        }

        protected AttributeMetadata Text(string name) => Attribute("name", name);

        protected AttributeMetadata Date(string name) => Attribute("createdon", name);

        protected AttributeMetadata Boolean(string name) => Attribute("donotemail", name);

        protected AttributeMetadata Choice(string name, params int[] values)
        {
            var attribute = (PicklistAttributeMetadata)Attribute("preferredcontactmethodcode", name);
            attribute.OptionSet = new OptionSetMetadata(new OptionMetadataCollection(values.Select(v => new OptionMetadata(TestLabel(new Label(v.ToString(), 1033)), v)).ToList()));
            return attribute;
        }

        protected AttributeMetadata Lookup(string name, params string[] targets)
        {
            var attribute = (LookupAttributeMetadata)Attribute("primarycontactid", name);
            attribute.Targets = targets;
            return attribute;
        }

        protected void AddTable(string name, params AttributeMetadata[] attributes)
        {
            var entity = new EntityMetadata { LogicalName = name, OwnershipType = OwnershipTypes.OrganizationOwned };
            Set(entity, "PrimaryIdAttribute", name + "id");
            Set(entity, "PrimaryNameAttribute", attributes.FirstOrDefault(a => a is StringAttributeMetadata)?.LogicalName);
            Set(entity, "Attributes", new[] { Attribute("accountid", name + "id") }.Concat(attributes).ToArray());
            Set(entity, "ManyToOneRelationships", Array.Empty<OneToManyRelationshipMetadata>());
            Set(entity, "OneToManyRelationships", Array.Empty<OneToManyRelationshipMetadata>());
            Set(entity, "ManyToManyRelationships", Array.Empty<ManyToManyRelationshipMetadata>());
            metadata.EntityMetadata[name] = entity;
        }

        protected Guid Create(string name, params (string Name, object Value)[] attributes)
        {
            var entity = new Entity(name);
            foreach (var attribute in attributes) entity[attribute.Name] = attribute.Value;
            return orgAdminService.Create(entity);
        }

        protected Entity Read(string name, Guid id) => orgAdminService.Retrieve(name, id, new ColumnSet(true));

        protected EntityCollection All(string name) => orgAdminService.RetrieveMultiple(new QueryExpression(name) { ColumnSet = new ColumnSet(true) });

        protected AttributeMetadata AccountAttribute(string name) => ((RetrieveAttributeResponse)orgAdminService.Execute(new RetrieveAttributeRequest { EntityLogicalName = "account", LogicalName = name })).AttributeMetadata;

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

        protected static Label TestLabel(Label label)
        {
            label.UserLocalizedLabel = label.LocalizedLabels[0];
            return label;
        }
    }
}
