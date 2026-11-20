using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore.Enums;

namespace TestPluginAssembly365.Plugins.ServiceBased
{

    /// <summary>
    /// AccountUpdateErpFigures Plugin.
    /// Fires when the following attributes are updated:
    /// All Attributes
    /// </summary>
    public class AccountPreImagePlugin : DIPlugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountPreImagePlugin"/> class.
        /// </summary>
        public AccountPreImagePlugin()
        {
            RegisterStep<Account, IAccountService>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                nameof(IAccountService.HandleUpdate))
                .AddFilteredAttributes(a => a.ParentAccountId)
                .WithPreImage(a => a.ParentAccountId);
        }
    }
}