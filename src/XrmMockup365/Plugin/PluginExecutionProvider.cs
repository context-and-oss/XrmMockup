using DG.Tools.XrmMockup;
using System;
using System.Collections.Generic;
using System.Text;

namespace XrmMockupShared.Plugin
{
    public class PluginExecutionProvider
    {
        private Action<MockupServiceProviderAndFactory> action;
        private MockupServiceProviderAndFactory provider;

        internal PluginExecutionProvider(Action<MockupServiceProviderAndFactory> action, MockupServiceProviderAndFactory provider)
        {
            this.action = action;
            this.provider = provider;
        }

        public void ExecuteAction()
        {
            action(provider);
        }
    }
}
