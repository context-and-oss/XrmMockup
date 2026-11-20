using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore.Enums;

namespace TestPluginAssembly365.Plugins.ServiceBased
{

    public class AccountCallServicePlugin : DIPlugin
    {
        public AccountCallServicePlugin()
        {
            // Register using the Action overload
            RegisterStep<Account, IAccountService>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                service => service.HandleCreate());

            // Register using the method name overload
            RegisterStep<Account, IAccountService>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                nameof(IAccountService.HandleUpdate))
                .AddFilteredAttributes(x => x.AccountNumber);
        }
    }
}
