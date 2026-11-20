using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy
{
    internal class MetadataRegistrationStrategy
    {
        private readonly ILogger _logger;

        public MetadataRegistrationStrategy(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public IEnumerable<IPluginStepConfig> AnalyzeType(Type pluginType, List<MetaPlugin> metaPlugins)
        {
            // Retrieve registration from CRM metadata
            var metaSteps =
                metaPlugins
                .Where(x =>
                    x.AssemblyName == pluginType.FullName &&
                    x.PluginTypeAssemblyName == pluginType.Assembly.GetName().Name);

            // Fallback for backwards compatability for old Metadata files
            if (!metaSteps.Any())
            {
                _logger.LogDebug("[Metadata] {TypeName}: no match on FullName+AssemblyName, trying FullName-only fallback",
                    pluginType.FullName);

                metaSteps =
                    metaPlugins
                    .Where(x => x.AssemblyName == pluginType.FullName);
            }

            var count = 0;
            foreach (var metaStep in metaSteps)
            {
                count++;
                _logger.LogDebug("[Metadata] {TypeName}: found metadata step '{Message}' on '{Entity}' stage={Stage}",
                    pluginType.FullName, metaStep.MessageName, metaStep.PrimaryEntity, metaStep.Stage);

                yield return new PluginStepConfig
                {
                    ExecutionStage = (ExecutionStage)metaStep.Stage,
                    EventOperation = metaStep.MessageName,
                    EntityLogicalName = metaStep.PrimaryEntity,
                    Deployment = 0,
                    ExecutionMode = (ExecutionMode)metaStep.Mode,
                    Name = metaStep.Name,
                    ExecutionOrder = metaStep.Rank,
                    FilteredAttributes = metaStep.FilteredAttributes,
                    ImpersonatingUserId = metaStep.ImpersonatingUserId ?? (Guid?)null,
                    AsyncAutoDelete = metaStep.AsyncAutoDelete,
                    ImageSpecifications = metaStep.Images?.Select(x => new ImageSpecification
                    {
                        ImageName = x.Name,
                        EntityAlias = x.EntityAlias,
                        ImageType = (ImageType)x.ImageType,
                        Attributes = x.Attributes
                    }) ?? Enumerable.Empty<IImageSpecification>()
                };
            }

            if (count == 0)
            {
                _logger.LogDebug("[Metadata] {TypeName}: no matching metadata entries found", pluginType.FullName);
            }
        }
    }
}
