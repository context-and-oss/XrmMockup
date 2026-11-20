using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Xrm.Sdk;
using WorkflowExecuter;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestWorkflowDiagnostics
    {
        private static WorkflowTree Tree(params IWorkflowNode[] nodes)
        {
            return new WorkflowTree(new ActivityList(nodes))
            {
                Name = "Diagnostic {workflow}",
                Id = Guid.NewGuid(),
                Mode = Workflow_Mode.Realtime,
                PrimaryEntityLogicalname = "account"
            };
        }

        private static Entity Account() => new Entity("account", Guid.NewGuid()) { ["name"] = "Not the workflow name" };

        private static void Execute(WorkflowTree tree, ITracingService trace, bool enabled = true)
        {
            tree.Execute(Account(), TimeSpan.FromHours(2), null, null, trace, enableWorkflowTracing: enabled);
        }

        [Fact]
        public void ParseNullAndWrongEntityReportWorkflowErrors()
        {
            var activities = new Dictionary<string, CodeActivity>();
            var nullError = Assert.Throws<WorkflowException>(() => WorkflowConstructor.Parse(null, activities, WorkflowConstructor.ParseAs.Workflow));
            Assert.Contains("<null>", nullError.Message);
            var wrongError = Assert.Throws<WorkflowException>(() => WorkflowConstructor.Parse(new Entity("account"), activities, WorkflowConstructor.ParseAs.Workflow));
            Assert.Contains("'account'", wrongError.Message);
            Assert.Contains("instead of workflow", wrongError.Message);
        }

        [Fact]
        public void ParseRetainsWorkflowIdentity()
        {
            var workflow = DG.Tools.XrmMockup.Internal.Utility.GetWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "Activeworkflow.xml"));
            var tree = WorkflowConstructor.Parse(workflow, new Dictionary<string, CodeActivity>(), WorkflowConstructor.ParseAs.Workflow);
            Assert.Equal(workflow.Id, tree.Id);
            Assert.Equal(workflow.GetAttributeValue<string>("name"), tree.Name);
        }

        [Fact]
        public void TracesWorkflowIdentityNodesAndCompletionInOrder()
        {
            var trace = new TracingService();
            var tree = Tree(new CreateVariable(new[] { new object[] { null, "reason {0}" } }, "String", "reason"),
                new TerminateWorkflow(OperationStatus.Succeeded, "reason"));
            var entity = Account();
            tree.Execute(entity, TimeSpan.FromHours(2), null, null, trace, enableWorkflowTracing: true);
            Assert.Contains("Starting Realtime workflow 'Diagnostic {workflow}'", trace.Messages[0]);
            Assert.Contains(tree.Id.ToString(), trace.Messages[0]);
            Assert.Contains("entity 'account'", trace.Messages[0]);
            Assert.Contains(entity.Id.ToString(), trace.Messages[0]);
            Assert.Contains("02:00:00", trace.Messages[0]);
            Assert.DoesNotContain("Not the workflow name", trace.Messages[0]);
            Assert.Equal("Entering CreateVariable. VariableName: reason", trace.Messages[2]);
            Assert.Equal("Entering TerminateWorkflow. Status: Succeeded MessageId: reason", trace.Messages[3]);
            Assert.Contains("completed successfully", trace.Messages.Last());
        }

        [Fact]
        public void FailureIsDiagnosedWithoutSuccessAndOriginalExceptionIsPreserved()
        {
            var trace = new TracingService();
            var error = new InvalidOperationException("Failure {details}");
            var tree = Tree(new TraceNode("before failure", error));
            Assert.Same(error, Assert.Throws<InvalidOperationException>(() => Execute(tree, trace)));
            Assert.Contains("before failure", trace.Messages);
            Assert.Contains(tree.Id.ToString(), trace.Messages.Last());
            Assert.Contains("failed: InvalidOperationException: Failure {details}", trace.Messages.Last());
            Assert.DoesNotContain(trace.Messages, m => m.Contains("completed successfully"));
        }

        [Fact]
        public void CanceledTerminationReportsStatusAndFailure()
        {
            var tree = Tree(new CreateVariable(new[] { new object[] { null, "Stop now" } }, "String", "reason"),
                new TerminateWorkflow(OperationStatus.Canceled, "reason"));
            var trace = new TracingService();
            var error = Assert.Throws<InvalidPluginExecutionException>(() => Execute(tree, trace));
            Assert.Equal("Stop now", error.Message);
            Assert.Contains("Entering TerminateWorkflow. Status: Canceled MessageId: reason", trace.Messages);
            Assert.Contains("failed:", trace.Messages.Last());
            Assert.DoesNotContain(trace.Messages, m => m.Contains("completed successfully"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void VerbosityDoesNotSuppressCodeActivityTraces(bool enabled)
        {
            var trace = new TracingService();
            Execute(Tree(new TraceNode("Activity {literal braces}")), trace, enabled);
            Assert.Contains("Activity {literal braces}", trace.Messages);
            Assert.Equal(enabled, trace.Messages.Any(m => m.StartsWith("Starting ")));
            Assert.Equal(enabled, trace.Messages.Any(m => m.Contains("completed successfully")));
            if (!enabled) Assert.Single(trace.Messages);
        }

        [Fact]
        public void DiagnosticsAreDisabledByDefaultAndCanBeToggledOnReusedTree()
        {
            Assert.False(new XrmMockupSettings().EnableWorkflowTracing);
            var tree = Tree(new Persist());
            var trace = new TracingService();
            tree.Execute(Account(), TimeSpan.Zero, null, null, trace);
            Assert.Empty(trace.Messages);
            Execute(tree, trace);
            Assert.Contains("Entering Persist", trace.Messages);
            var disabledTrace = new TracingService();
            Execute(tree, disabledTrace, false);
            Assert.Empty(disabledTrace.Messages);
        }

        [Fact]
        public void WaitAndRepeatedResumeUseWorkflowIdentityAndFreshExecutionSinks()
        {
            var tree = Tree(new WaitStart());
            var initialTrace = new TracingService();
            Execute(tree, initialTrace);
            var wait = Assert.IsType<WaitInfo>(tree.Variables["Wait"]);
            Assert.Contains("waiting at activity 0", initialTrace.Messages.Last());
            Assert.DoesNotContain(initialTrace.Messages, m => m.Contains("completed successfully"));

            var resumeTrace = new TracingService();
            wait.Resume(TimeSpan.FromDays(1), null, null, resumeTrace, true);
            Assert.Contains("Resuming Realtime workflow 'Diagnostic {workflow}'", resumeTrace.Messages[0]);
            Assert.Contains(tree.Id.ToString(), resumeTrace.Messages[0]);
            Assert.Contains("waiting at activity 0", resumeTrace.Messages.Last());
            Assert.DoesNotContain(initialTrace.Messages, m => m.StartsWith("Resuming"));
            var secondWait = Assert.IsType<WaitInfo>(wait.VariablesInstance["Wait"]);
            Assert.Equal(tree.Id, secondWait.WorkflowId);

            secondWait.Element.Activities[0] = new Persist();
            var finishTrace = new TracingService();
            secondWait.Resume(TimeSpan.FromDays(2), null, null, finishTrace, true);
            Assert.Contains("Resuming", finishTrace.Messages[0]);
            Assert.Contains(tree.Id.ToString(), finishTrace.Messages.Last());
            Assert.Contains("completed successfully", finishTrace.Messages.Last());
            Assert.Null(secondWait.VariablesInstance["Wait"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResumeFailureRetainsErrorAndRespectsVerbosity(bool enabled)
        {
            var tree = Tree(new WaitStart());
            Execute(tree, new TracingService(), false);
            var wait = Assert.IsType<WaitInfo>(tree.Variables["Wait"]);
            var error = new InvalidOperationException("resume failure");
            wait.Element.Activities[0] = new TraceNode("resumed activity", error);
            var trace = new TracingService();
            Assert.Same(error, Assert.Throws<InvalidOperationException>(() => wait.Resume(TimeSpan.Zero, null, null, trace, enabled)));
            Assert.Contains("resumed activity", trace.Messages);
            Assert.Equal(enabled, trace.Messages.Any(m => m.Contains("failed:")));
            Assert.DoesNotContain(trace.Messages, m => m.Contains("completed successfully"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FormattingErrorsStillSurfaceAndValidMessagesReachCollectionAndCallback(bool enabled)
        {
            var sink = new List<string>();
            var trace = new TracingService(sink.Add);
            Execute(Tree(new TraceNode("literal {braces}")), trace, enabled);
            trace.Trace("Value: {0}", 42);
            trace.Trace("null args {braces}", (object[])null);
            Assert.Equal(trace.Messages, sink);
            Assert.Contains("Value: 42", sink);
            Assert.Contains("null args {braces}", sink);
            var count = trace.Messages.Count;
            Assert.Throws<FormatException>(() => trace.Trace("bad {1}", 42));
            Assert.Equal(count, trace.Messages.Count);
            var tree = Tree(new TraceNode("bad {1}", formatArguments: new object[] { 42 }));
            Assert.Throws<FormatException>(() => Execute(tree, trace, enabled));
            Assert.DoesNotContain(sink, m => m == "bad {1}");
        }

        [Fact]
        public void FactoryKeepsExecutionScopedMessagesAndAddsNoGlobalListeners()
        {
            var listeners = Trace.Listeners.Cast<TraceListener>().ToArray();
            var factory = new TracingServiceFactory();
            var first = Assert.IsType<TracingService>(factory.GetService());
            var second = Assert.IsType<TracingService>(factory.GetService());
            Execute(Tree(new TraceNode("first execution")), first);
            Execute(Tree(new TraceNode("second execution")), second, false);
            Assert.DoesNotContain("second execution", first.Messages);
            Assert.Equal(new[] { "second execution" }, second.Messages);
            Assert.Equal(first.Messages.Concat(second.Messages), factory.TraceLog);
            Assert.Equal(listeners, Trace.Listeners.Cast<TraceListener>().ToArray());
            factory.Clear();
            Assert.Empty(factory.TraceLog);
            Assert.NotEmpty(first.Messages);
        }

        private sealed class TraceNode : IWorkflowNode
        {
            private readonly string message;
            private readonly Exception error;
            private readonly object[] formatArguments;
            internal TraceNode(string message, Exception error = null, object[] formatArguments = null)
            {
                this.message = message;
                this.error = error;
                this.formatArguments = formatArguments;
            }
            public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
                IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
            {
                trace.Trace(message, formatArguments);
                if (error != null) throw error;
            }
        }
    }
}
