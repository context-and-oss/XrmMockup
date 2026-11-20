using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class that encapsulates settings for MockupService
    /// </summary>
    public class MockupServiceSettings {
        public enum Role { SDK, UI }

        /// <summary>
        /// Indicates whether plugins and workflows should be triggered
        /// </summary>
        public bool TriggerProcesses { get; }

        /// <summary>
        /// <para>
        /// Indicates whether workflows should be triggered.
        /// </para>
        /// <para>
        /// If <see cref="TriggerProcesses"/> is false, this is implicitly false.
        /// </para>
        /// </summary>
        public bool TriggerWorkflows { get; }

        /// <summary>
        /// Indicates whether read only attributes should be settable
        /// </summary>
        public bool SetUnsettableFields { get; }

        /// <summary>
        /// Indicates whether the service should be a SDK or UI service
        /// </summary>
        public Role ServiceRole { get; }
        /// <summary>
        /// Creates a default service setting
        /// </summary>

        public MockupServiceSettings() : this(true, true, false, Role.SDK) { }

        /// <summary>
        /// Creates a custom service setting
        /// </summary>
        public MockupServiceSettings(bool triggerProcesses, bool setUnsettableFields, Role serviceRole) : this(triggerProcesses, triggerProcesses, setUnsettableFields, serviceRole) { }

        public MockupServiceSettings(bool triggerProcesses, bool triggerWorkflows, bool setUnsettableFields, Role serviceRole)
        {
            this.TriggerProcesses = triggerProcesses;
            this.TriggerWorkflows = triggerWorkflows;
            this.SetUnsettableFields = setUnsettableFields;
            this.ServiceRole = serviceRole;
        }
    }
}
