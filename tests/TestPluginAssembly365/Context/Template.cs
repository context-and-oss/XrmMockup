using DG.XrmContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Runtime.Serialization;

// NOTE: This early-bound type is hand-written rather than emitted by XrmContext. The generated
// XrmContext.cs is produced from a fixed reference org (Env.lab4) that does not include the
// 'template' entity, so this minimal proxy type was added to support SendEmailFromTemplate tests.
// It follows the same generated style (ExtendedEntity, AttributeLogicalName) so it behaves like a
// generated proxy type under XrmMockup's EnableProxyTypes. Extend with more attributes as needed.

namespace DG.XrmFramework.BusinessDomain.ServiceContext
{
    [EntityLogicalName("template")]
    [DataContract()]
    public partial class Template : ExtendedEntity<EmptyEnum, EmptyEnum>
    {
        public const string EntityLogicalName = "template";

        public const int EntityTypeCode = 2010;

        public Template() : base(EntityLogicalName) { }

        public Template(Guid Id) : base(EntityLogicalName, Id) { }

        [AttributeLogicalName("templateid")]
        public override Guid Id
        {
            get { return base.Id; }
            set { SetId("templateid", value); }
        }

        [AttributeLogicalName("templateid")]
        public Guid? TemplateId
        {
            get { return GetAttributeValue<Guid?>("templateid"); }
            set { SetId("templateid", value); }
        }

        /// <summary>
        /// <para>Title of the e-mail template. (Primary name attribute.)</para>
        /// </summary>
        [AttributeLogicalName("title")]
        public string Title
        {
            get { return GetAttributeValue<string>("title"); }
            set { SetAttributeValue("title", value); }
        }

        /// <summary>
        /// <para>Subject line of the template. Stored as an XSLT stylesheet in Dataverse.</para>
        /// </summary>
        [AttributeLogicalName("subject")]
        public string Subject
        {
            get { return GetAttributeValue<string>("subject"); }
            set { SetAttributeValue("subject", value); }
        }

        /// <summary>
        /// <para>Body of the template. Stored as an XSLT stylesheet in Dataverse.</para>
        /// </summary>
        [AttributeLogicalName("body")]
        public string Body
        {
            get { return GetAttributeValue<string>("body"); }
            set { SetAttributeValue("body", value); }
        }

        /// <summary>
        /// <para>Description of the template.</para>
        /// </summary>
        [AttributeLogicalName("description")]
        public string Description
        {
            get { return GetAttributeValue<string>("description"); }
            set { SetAttributeValue("description", value); }
        }

        /// <summary>
        /// <para>Logical name of the entity the template applies to (EntityName attribute).</para>
        /// </summary>
        [AttributeLogicalName("templatetypecode")]
        public string TemplateTypeCode
        {
            get { return GetAttributeValue<string>("templatetypecode"); }
            set { SetAttributeValue("templatetypecode", value); }
        }

        /// <summary>
        /// <para>Whether the template is a personal template (true) or an organization template (false).</para>
        /// </summary>
        [AttributeLogicalName("ispersonal")]
        public bool? IsPersonal
        {
            get { return GetAttributeValue<bool?>("ispersonal"); }
            set { SetAttributeValue("ispersonal", value); }
        }

        /// <summary>
        /// <para>Language of the template (LCID).</para>
        /// </summary>
        [AttributeLogicalName("languagecode")]
        public int? LanguageCode
        {
            get { return GetAttributeValue<int?>("languagecode"); }
            set { SetAttributeValue("languagecode", value); }
        }
    }
}
