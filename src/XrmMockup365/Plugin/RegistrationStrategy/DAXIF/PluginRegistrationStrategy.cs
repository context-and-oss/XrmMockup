using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy.DAXIF
{
    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes,impersonating user id
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = Tuple<string, int, string, string>;
    using ExtendedStepConfig = Tuple<int, int, string, int, string, string>;
    using ImageTuple = Tuple<string, string, int, string>;

    internal class PluginRegistrationStrategy : IRegistrationStrategy<IPluginStepConfig>
    {
        private readonly ILogger _logger;

        public PluginRegistrationStrategy(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public IEnumerable<IPluginStepConfig> AnalyzeType(IPlugin plugin)
        {
            var pluginType = plugin.GetType();
            if (pluginType.GetMethod("PluginProcessingStepConfigs") == null)
            {
                _logger.LogDebug("[DAXIF] {TypeName}: no PluginProcessingStepConfigs method found, skipping", pluginType.FullName);
                return Enumerable.Empty<IPluginStepConfig>();
            }

            _logger.LogDebug("[DAXIF] {TypeName}: found PluginProcessingStepConfigs method, extracting steps", pluginType.FullName);

            var configs = pluginType
                .GetMethod("PluginProcessingStepConfigs")
                .Invoke(plugin, new object[] { })
                as IEnumerable<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>>;

            var results = configs.Select(c =>
            {
                var hasImpersonatingUser = Guid.TryParse(c.Item2.Item6, out var impersonatingUserId);

                return new PluginStepConfig
                {
                    ExecutionStage = (ExecutionStage)c.Item1.Item2,
                    EventOperation = c.Item1.Item3,
                    EntityLogicalName = c.Item1.Item4,
                    Deployment = (Deployment)c.Item2.Item1,
                    ExecutionMode = (ExecutionMode)c.Item2.Item2,
                    Name = c.Item2.Item3,
                    ExecutionOrder = c.Item2.Item4,
                    FilteredAttributes = c.Item2.Item5,
                    ImpersonatingUserId = hasImpersonatingUser ? impersonatingUserId : null as Guid?,
                    AsyncAutoDelete = false,
                    ImageSpecifications = c.Item3.Select(i => new ImageSpecification
                    {
                        ImageName = i.Item1,
                        EntityAlias = i.Item2,
                        ImageType = (ImageType)i.Item3,
                        Attributes = i.Item4
                    })
                };
            }).ToList();

            _logger.LogDebug("[DAXIF] {TypeName}: extracted {Count} plugin step(s)", pluginType.FullName, results.Count);
            return results;
        }
    }
}
