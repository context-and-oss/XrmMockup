# Local plugin registration

[Documentation](README.md)

## DAXIF# local plugin registration
By default XrmMockup integrates with our other tools. That means if you use DAXIF#'s local plugin registration then XrmMockup will automatically detect and use that instead of the registration found in your online CRM. This means you can change your plugin registration - and test it - without needing to sync plugins and metadata again.

An example of a DAXIF# plugin registration is shown below.

```csharp
public class AccountNumberPlugin : Plugin {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountNumberPlugin"/> class.
        /// </summary>
        public AccountNumberPlugin() : base(typeof(AccountNumberPlugin)) {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteLogic)
                  .SetExecutionMode(ExecutionMode.Asynchronous)
                  .AddFilteredAttributes(x => x.AccountNumber);
        }

        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="localContext">The <see cref="LocalPluginContext"/> which contains the
        /// <see cref="IPluginExecutionContext"/>,
        /// <see cref="IOrganizationService"/>
        /// and <see cref="ITracingService"/>
        /// </param>
        protected void ExecuteLogic(LocalPluginContext localContext) {
        }
    }
```
