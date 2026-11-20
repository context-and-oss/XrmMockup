using XrmPluginCore;
using Microsoft.Extensions.DependencyInjection;

namespace TestPluginAssembly365.Plugins.ServiceBased
{
    public abstract class DIPlugin : Plugin
    {
        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<IAccountService, AccountService>();
        }
    }
}
