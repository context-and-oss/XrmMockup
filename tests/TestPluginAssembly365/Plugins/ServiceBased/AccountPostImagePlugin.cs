using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore.Enums;

namespace TestPluginAssembly365.Plugins.ServiceBased
{
    /// <summary>
    /// AccountUpdateErpFigures Plugin.
    /// Fires when the following attributes are updated:
    /// All Attributes
    /// </summary>
    public class AccountPostImagePlugin : DIPlugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountPostImagePlugin"/> class.
        /// </summary>
        public AccountPostImagePlugin()
        {
            RegisterStep<Account, IAccountService>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                nameof(IAccountService.HandleUpdate))
                .AddFilteredAttributes(a => a.ParentAccountId)
                .WithPostImage(a => a.ParentAccountId);
        }
    }
}