using XrmPluginCore;
using XrmPluginCore.Interfaces.CustomApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy.XrmPluginCore
{
    internal class CustomApiRegistrationStrategy : IRegistrationStrategy<ICustomApiConfig>
    {
        private readonly ILogger _logger;

        public CustomApiRegistrationStrategy(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public IEnumerable<ICustomApiConfig> AnalyzeType(IPlugin plugin)
        {
            if (plugin is ICustomApiDefinition customApiDefinition)
            {
                _logger.LogDebug("[XrmPluginCore] {TypeName}: implements ICustomApiDefinition, extracting registration",
                    plugin.GetType().FullName);
                yield return customApiDefinition.GetRegistration();
            }
            else
            {
                _logger.LogDebug("[XrmPluginCore] {TypeName}: does not implement ICustomApiDefinition, skipping",
                    plugin.GetType().FullName);
            }
        }
    }
}
