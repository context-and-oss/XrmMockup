using System;
using System.Collections.Generic;
using XrmPluginCore.Enums;

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// The type of operation that produced a <see cref="PluginTraceLog"/>, mirroring the
    /// OperationType option set on the platform's plugintracelog entity.
    /// </summary>
    public enum PluginTraceOperationType
    {
        Unknown = 0,
        Plugin = 1,
        WorkflowActivity = 2,
    }

    /// <summary>
    /// A single grouped trace-log entry, capturing all trace messages emitted by one
    /// plugin (or workflow activity) execution together with the metadata identifying it,
    /// mirroring the plugintracelog records produced by a real Dynamics 365 environment.
    /// </summary>
    public sealed class PluginTraceLog
    {
        /// <summary>The full name of the invoking type, e.g. the plugin class.</summary>
        public string TypeName { get; internal set; }

        /// <summary>The message being processed, e.g. Create, Update or a custom API name.</summary>
        public string MessageName { get; internal set; }

        /// <summary>The primary entity of the operation, or "none" if there is no bound entity.</summary>
        public string PrimaryEntity { get; internal set; }

        /// <summary>
        /// The unsecure configuration supplied to the plugin step.
        /// <para>Not currently modeled by XrmMockup and therefore always null.</para>
        /// </summary>
        public string Configuration { get; internal set; }

        /// <summary>
        /// The secure configuration supplied to the plugin step.
        /// <para>Not currently modeled by XrmMockup and therefore always null.</para>
        /// </summary>
        public string SecureConfiguration { get; internal set; }

        /// <summary>Whether the entry originated from a plugin or a workflow activity.</summary>
        public PluginTraceOperationType OperationType { get; internal set; }

        /// <summary>
        /// The id of the plugin step (SDK message processing step) that was executed.
        /// <para>Not currently modeled by XrmMockup and therefore always null.</para>
        /// </summary>
        public Guid? PluginStepId { get; internal set; }

        /// <summary>The execution depth: 1 for a directly invoked plugin, 2 for a plugin invoked by a plugin, etc.</summary>
        public int Depth { get; internal set; }

        /// <summary>The correlation id shared across the whole execution chain.</summary>
        public Guid CorrelationId { get; internal set; }

        /// <summary>Whether the execution was synchronous or asynchronous.</summary>
        public ExecutionMode Mode { get; internal set; }

        /// <summary>The id of the request that triggered the execution, if available.</summary>
        public Guid? RequestId { get; internal set; }

        /// <summary>The time at which execution started, according to the mockup's (possibly offset) clock.</summary>
        public DateTime ExecutionStartTime { get; internal set; }

        /// <summary>The wall-clock duration of the execution, in milliseconds.</summary>
        public double ExecutionDurationMs { get; internal set; }

        /// <summary>
        /// The trace messages emitted during the execution, in order. This is the platform's
        /// "Message Block", kept as a list rather than a single concatenated string.
        /// </summary>
        public IReadOnlyList<string> MessageBlock { get; internal set; } = new List<string>();

        /// <summary>The message and stack trace of the exception that ended the execution, or null if it succeeded.</summary>
        public string ExceptionDetails { get; internal set; }

        /// <summary>The <see cref="MessageBlock"/> joined into a single string, matching the platform's concatenated block.</summary>
        public string MessageBlockText => string.Join(Environment.NewLine, MessageBlock ?? new List<string>());
    }
}
