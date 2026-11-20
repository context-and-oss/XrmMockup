using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using DG.Tools.XrmMockup.Online;

namespace DG.Tools.XrmMockup
{
    public class StaticMetadataCache
    {
        public MetadataSkeleton Metadata { get; }
        public List<Entity> Workflows { get; }
        public List<SecurityRole> SecurityRoles { get; }
        public Dictionary<string, Type> EntityTypeMap { get; }
        public EntityReference BaseCurrency { get; }
        public int BaseCurrencyPrecision { get; }
        internal IOnlineDataService OnlineDataService { get; }
        public ILoggerFactory LoggerFactory { get; }

        internal StaticMetadataCache(MetadataSkeleton metadata, List<Entity> workflows, List<SecurityRole> securityRoles,
            Dictionary<string, Type> entityTypeMap, EntityReference baseCurrency, int baseCurrencyPrecision,
            IOnlineDataService onlineDataService, ILoggerFactory loggerFactory = null)
        {
            Metadata = metadata;
            Workflows = workflows;
            SecurityRoles = securityRoles;
            EntityTypeMap = entityTypeMap;
            BaseCurrency = baseCurrency;
            BaseCurrencyPrecision = baseCurrencyPrecision;
            OnlineDataService = onlineDataService;
            LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }
    }
}
