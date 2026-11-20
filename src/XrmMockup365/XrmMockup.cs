using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("XrmMockup365Test")]
namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class for mocking a crm 365 instance
    /// </summary>
    public class XrmMockup365 : XrmMockupBase {

        private static readonly Dictionary<XrmMockupSettings, StaticMetadataCache> metadataCache = new Dictionary<XrmMockupSettings, StaticMetadataCache>();
        private static readonly object cacheLock = new object();

        private XrmMockup365(XrmMockupSettings Settings, MetadataSkeleton metadata = null, List<Entity> workflows = null, List<SecurityRole> securityRoles = null) :
            base(Settings, metadata, workflows, securityRoles)
        {
        }

        private XrmMockup365(XrmMockupSettings Settings, StaticMetadataCache staticCache) :
            base(Settings, staticCache)
        {
        }
        
        /// <summary>
        /// Gets a new instance of XrmMockup365 with its own database
        /// </summary>
        /// <param name="Settings"></param>
        public static XrmMockup365 GetInstance(XrmMockupSettings Settings) {
            StaticMetadataCache cache;
            
            lock (cacheLock)
            {
                if (!metadataCache.ContainsKey(Settings)) 
                {
                    metadataCache[Settings] = Core.BuildStaticMetadataCache(Settings);
                }
                cache = metadataCache[Settings];
            }

            // Always return a new instance with its own database
            return new XrmMockup365(Settings, cache);
        }

        /// <summary>
        /// Gets a new instance of XrmMockup365 using the same metadata as the provided instance
        /// </summary>
        /// <param name="xrmMockup">The existing instance to copy</param>
        /// <param name="settings">
        ///     If provided, will override the settings from the existing instance.<br/>
        ///     <em>NOTE: Changing <see cref="XrmMockupSettings.MetadataDirectoryPath"/> will not trigger a reload</em>
        /// </param>
        public static XrmMockup365 GetInstance(XrmMockup365 xrmMockup, XrmMockupSettings settings = null)
        {
            var effectiveSettings = settings ?? xrmMockup.Settings;
            
            // Try to use cached metadata if settings match
            StaticMetadataCache cache;
            lock (cacheLock)
            {
                if (metadataCache.ContainsKey(effectiveSettings))
                {
                    cache = metadataCache[effectiveSettings];
                }
                else
                {
                    // Create a new cache entry using the existing instance's data
                    cache = new StaticMetadataCache(
                        xrmMockup.Metadata,
                        xrmMockup.Workflows,
                        xrmMockup.SecurityRoles,
                        new Dictionary<string, Type>(), // Will be rebuilt if needed
                        xrmMockup.BaseCurrency,
                        0, // Will be retrieved from metadata
                        null // Will be rebuilt if needed
                    );
                    metadataCache[effectiveSettings] = cache;
                }
            }
            
            return new XrmMockup365(effectiveSettings, cache);
        }
    }
}
