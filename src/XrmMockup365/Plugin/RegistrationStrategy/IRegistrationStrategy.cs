using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Plugin.RegistrationStrategy
{
    internal interface IRegistrationStrategy<T>
    {
        IEnumerable<T> AnalyzeType(IPlugin plugin);
    }
}
