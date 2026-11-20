using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup.Internal;
using System;

namespace WorkflowExecuter
{
    /// <summary>
    /// Opt-in engine diagnostics. The execution's original tracing service remains the sink:
    /// code activities always retain its formatting, collection and callback behavior.
    /// </summary>
    internal static class WorkflowDiagnostics
    {
        private sealed class DiagnosticTracingService : ITracingService
        {
            private readonly ITracingService inner;
            internal DiagnosticTracingService(ITracingService inner) { this.inner = inner; }
            public void Trace(string format, params object[] args) => inner.Trace(format, args);
        }

        internal static void Trace(ITracingService trace, string format, params object[] args)
        {
            if (trace is DiagnosticTracingService)
                trace.Trace(format, args);
        }

        internal static void Run(string name, Guid id, Workflow_Mode? mode, EntityReference primaryEntity,
            TimeSpan timeOffset, ITracingService trace, bool enabled, bool resuming,
            Action<ITracingService> execute, Func<WaitInfo> getWait)
        {
            var executionTrace = enabled ? new DiagnosticTracingService(trace) : trace;
            Trace(executionTrace, "{0} {1} workflow '{2}' ({3}) for entity '{4}' ({5}). timeOffset: {6}",
                resuming ? "Resuming" : "Starting", mode, name, id, primaryEntity?.LogicalName, primaryEntity?.Id, timeOffset);
            try
            {
                execute(executionTrace);
            }
            catch (Exception ex)
            {
                Trace(executionTrace, "{0} workflow '{1}' ({2}) failed: {3}: {4}",
                    mode, name, id, ex.GetType().Name, ex.Message);
                throw;
            }

            var wait = getWait();
            if (wait != null)
            {
                // Store identity, not the tracing service, so a resumed execution uses its own sink.
                wait.WorkflowName = name;
                wait.WorkflowId = id;
                wait.WorkflowMode = mode;
                Trace(executionTrace, "{0} workflow '{1}' ({2}) waiting at activity {3}", mode, name, id, wait.ElementIndex);
            }
            else
            {
                Trace(executionTrace, "{0} workflow '{1}' ({2}) completed successfully", mode, name, id);
            }
        }
    }
}
