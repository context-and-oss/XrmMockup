using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using DG.Tools.XrmMockup.Online;

namespace DG.Tools.XrmMockup.Internal
{
    /// <summary>
    /// Data transfer object for core initialization
    /// </summary>
    internal class CoreInitializationData
    {
        public XrmMockupSettings Settings { get; set; }
        public MetadataSkeleton Metadata { get; set; }
        public List<Entity> Workflows { get; set; }
        public List<SecurityRole> SecurityRoles { get; set; }
        public EntityReference BaseCurrency { get; set; }
        public int BaseCurrencyPrecision { get; set; }
        public IOnlineDataService OnlineDataService { get; set; }
        public Dictionary<string, Type> EntityTypeMap { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
    }
}
