using DG.Tools.XrmMockup.Internal;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using XrmPluginCore.Enums;

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// Shared helper that runs a plugin, custom API or workflow execution while timing it and
    /// capturing any thrown exception, then records a grouped <see cref="PluginTraceLog"/> entry
    /// built from that execution's own trace messages together with the supplied metadata.
    /// </summary>
    internal static class PluginTraceRecorder
    {
        /// <param name="unwrapTargetInvocation">
        /// When true, a <see cref="TargetInvocationException"/> is unwrapped and its inner exception
        /// rethrown, preserving the plugin pipeline's historical behavior.
        /// </param>
        /// <param name="record">When false, the execution still runs but no trace entry is recorded (e.g. internal system plugins).</param>
        public static void Run(
            ICoreOperations core,
            MockupServiceProviderAndFactory provider,
            PluginContext ctx,
            string typeName,
            string messageName,
            string primaryEntity,
            PluginTraceOperationType operationType,
            ExecutionMode mode,
            Action execute,
            bool unwrapTargetInvocation,
            bool record)
        {
            var startTime = DateTime.UtcNow.Add(core.TimeOffset);
            var stopwatch = Stopwatch.StartNew();
            string exceptionDetails = null;
            try
            {
                execute();
            }
            catch (TargetInvocationException e) when (unwrapTargetInvocation)
            {
                // Fall back to the TargetInvocationException itself when there is no inner
                // exception, so Capture is never handed null (which would mask the failure).
                var toThrow = e.InnerException ?? (Exception)e;
                exceptionDetails = toThrow.ToString();
                ExceptionDispatchInfo.Capture(toThrow).Throw();
            }
            catch (Exception e)
            {
                exceptionDetails = e.ToString();
                throw;
            }
            finally
            {
                stopwatch.Stop();
                if (record)
                {
                    var messages = (provider.GetService<ITracingService>() as TracingService)?.Messages.ToList()
                        ?? new List<string>();

                    core.RecordPluginTrace(new PluginTraceLog
                    {
                        TypeName = typeName,
                        MessageName = messageName,
                        PrimaryEntity = string.IsNullOrEmpty(primaryEntity) ? "none" : primaryEntity,
                        OperationType = operationType,
                        Depth = ctx.Depth,
                        CorrelationId = ctx.CorrelationId,
                        Mode = mode,
                        RequestId = ctx.RequestId,
                        ExecutionStartTime = startTime,
                        ExecutionDurationMs = stopwatch.Elapsed.TotalMilliseconds,
                        MessageBlock = messages,
                        ExceptionDetails = exceptionDetails,
                        // Configuration, SecureConfiguration and PluginStepId are not modeled by XrmMockup.
                    });
                }
            }
        }
    }
}
