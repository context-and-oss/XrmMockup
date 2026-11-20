using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.CustomApi;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup
{
    internal class CustomApiConfig : ICustomApiConfig
    {
        public AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; set; }

        public BindingType BindingType { get; set; }

        public string BoundEntityLogicalName { get; set; }

        public string Description { get; set; }

        public string DisplayName { get; set; }

        public bool EnabledForWorkflow { get; set; }

        public string ExecutePrivilegeName { get; set; }

        public bool IsCustomizable { get; set; }

        public bool IsFunction { get; set; }

        public bool IsPrivate { get; set; }

        public string Name { get; set; }

        public Guid? OwnerId { get; set; }

        public OwnerType? OwnerType { get; set; }

        public string PluginType { get; set; }

        public string UniqueName { get; set; }

        public IEnumerable<IRequestParameter> RequestParameters { get; set; }

        public IEnumerable<IResponseProperty> ResponseProperties { get; set; }
    }

    internal class RequestParameter : IRequestParameter
    {
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public bool IsCustomizable { get; set; }
        public bool IsOptional { get; set; }
        public string LogicalEntityName { get; set; }
        public AttributeTypeCode TypeCode { get; set; }
        public string Description { get; set; }
        public CustomApiParameterType Type { get; set; }
    }

    internal class ResponseProperty : IResponseProperty
    {
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public bool IsCustomizable { get; set; }
        public string LogicalEntityName { get; set; }
        public AttributeTypeCode TypeCode { get; set; }
        public string Description { get; set; }
        public CustomApiParameterType Type { get; set; }
    }
}
