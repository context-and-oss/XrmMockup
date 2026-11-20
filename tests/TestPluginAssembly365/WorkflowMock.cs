#if DATAVERSE_SERVICE_CLIENT

using System;

/// <summary>
/// Implementations of the different Workflow related attributes
/// These are part of the SDK package, which we do not have access to in .NET builds
/// </summary>
namespace Microsoft.Xrm.Sdk.Workflow
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ParameterAttribute : Attribute
    {
        public string Name { get; private set ;}

        protected ParameterAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class InputAttribute : ParameterAttribute
    {
        public InputAttribute(string name)
            : base(name)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OutputAttribute : ParameterAttribute
    {
        public OutputAttribute(string name)
            : base(name)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ReferenceTargetAttribute : Attribute
    {
        public string EntityName { get; set; }

        public ReferenceTargetAttribute(string entityName)
        {
            EntityName = entityName;
        }
    }
}
#endif