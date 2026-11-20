using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore.Enums;

namespace TestPluginAssembly365.Plugins.ServiceBased
{

    /// <summary>
    /// AccountUpdateErpFigures Plugin.
    /// Fires when the following attributes are updated:
    /// All Attributes
    /// </summary>
    public class AccountPrePostImagePlugin : DIPlugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountPrePostImagePlugin"/> class.
        /// </summary>
        public AccountPrePostImagePlugin()
        {
            RegisterStep<Account, IAccountService>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                nameof(IAccountService.HandleUpdate))
                .AddFilteredAttributes(a => a.ParentAccountId)
                .WithPreImage(a => a.ParentAccountId)
                .WithPostImage(a => a.ParentAccountId);
        }
    }
}