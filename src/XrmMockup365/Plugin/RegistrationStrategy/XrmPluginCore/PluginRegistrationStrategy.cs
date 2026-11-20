using XrmPluginCore;
using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy.XrmPluginCore
{
    internal class PluginRegistrationStrategy : IRegistrationStrategy<IPluginStepConfig>
    {
        private readonly ILogger _logger;

        public PluginRegistrationStrategy(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public IEnumerable<IPluginStepConfig> AnalyzeType(IPlugin plugin)
        {
            if (plugin is IPluginDefinition pluginDefinition)
            {
                var registrations = pluginDefinition.GetRegistrations().ToList();
                _logger.LogDebug("[XrmPluginCore] {TypeName}: implements IPluginDefinition, found {Count} registration(s)",
                    plugin.GetType().FullName, registrations.Count);
                return registrations;
            }

            _logger.LogDebug("[XrmPluginCore] {TypeName}: does not implement IPluginDefinition, skipping", plugin.GetType().FullName);
            return Enumerable.Empty<IPluginStepConfig>();
        }
    }
}
