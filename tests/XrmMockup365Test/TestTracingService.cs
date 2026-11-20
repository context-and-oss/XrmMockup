using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using System.Linq;

namespace DG.XrmMockupTest
{
    public class TestTracingService : UnitTestBase
    {
        public TestTracingService(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TracingServiceShouldAcceptXMLFormatStrings()
        {
            ITracingService trace = new TracingService();
            trace.Trace("cams_signatures : <head><style type=text/css>p,li{font-family: Arial,Helvetica,sans-serif;font-size: 11pt;}</style></head><body><p contenteditable=\"false\"><span style=\font - family: Arial; font - size: 11pt; \">By signing the Agreement Plan, the signatories are certifying, subject to the exceptions detailed below, both the completeness of the Agreement Plan and the agreed commitment between [CUSTOMER] and [DELIVERY AGENT].</span></p></body>");
        }

        [Fact]
        public void TracingServiceShouldEvaluateFormatStringWithArgs()
        {
            ITracingService trace = new TracingService();
            trace.Trace("Value is {0} and {1}", 42, "test");
        }

        [Fact]
        public void TracingServiceShouldThrowOnInvalidFormatStringWithArgs()
        {
            ITracingService trace = new TracingService();
            // Referencing an argument index that isn't supplied would throw a FormatException
            // in a real environment; the mockup must surface it rather than swallow it.
            Assert.Throws<System.FormatException>(() => trace.Trace("Value is {1}", 42));
        }

        [Fact]
        public void TraceMessagesAreCollectedAndExposedForAssertions()
        {
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountTracePlugin));
            crm.ClearTraceLog();

            var account = new Account { Name = "Contoso" };
            account.Id = orgAdminService.Create(account);

            Assert.Contains("Creating account", crm.TraceLog);
            Assert.Contains("Account name is Contoso", crm.TraceLog);
        }

        [Fact]
        public void ClearTraceLogEmptiesCollectedMessages()
        {
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountTracePlugin));

            var account = new Account { Name = "Contoso" };
            account.Id = orgAdminService.Create(account);
            Assert.NotEmpty(crm.TraceLog);

            crm.ClearTraceLog();
            Assert.Empty(crm.TraceLog);
        }

        [Fact]
        public void PluginTraceLogGroupsMessagesByExecutionWithMetadata()
        {
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountTracePlugin));
            crm.ClearPluginTraceLog();

            var account = new Account { Name = "Contoso" };
            account.Id = orgAdminService.Create(account);

            var entry = Assert.Single(crm.PluginTraceLog.Where(l => l.TypeName == typeof(DG.Some.Namespace.AccountTracePlugin).FullName));

            Assert.Equal("Create", entry.MessageName);
            Assert.Equal("account", entry.PrimaryEntity);
            Assert.Equal(XrmPluginCore.Enums.ExecutionMode.Synchronous, entry.Mode);
            Assert.Equal(PluginTraceOperationType.Plugin, entry.OperationType);
            Assert.Equal(1, entry.Depth);
            Assert.Null(entry.ExceptionDetails);

            // Both Trace calls from this execution are grouped together, in order. (The plugin
            // base class also emits its own Entered/Exiting trace messages around the handler.)
            Assert.Contains("Creating account", entry.MessageBlock);
            Assert.Contains("Account name is Contoso", entry.MessageBlock);
            Assert.True(
                entry.MessageBlock.ToList().IndexOf("Creating account") < entry.MessageBlock.ToList().IndexOf("Account name is Contoso"),
                "Trace messages should be recorded in the order they were emitted");
            Assert.Contains("Creating account", entry.MessageBlockText);
        }

        [Fact]
        public void PluginTraceLogCapturesExceptionDetails()
        {
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountTraceThrowPlugin));
            crm.ClearPluginTraceLog();

            var account = new Account { Name = "Contoso" };
            Assert.ThrowsAny<System.Exception>(() => orgAdminService.Create(account));

            var entry = Assert.Single(crm.PluginTraceLog.Where(l => l.TypeName == typeof(DG.Some.Namespace.AccountTraceThrowPlugin).FullName));

            Assert.Contains("About to throw", entry.MessageBlock);
            Assert.NotNull(entry.ExceptionDetails);
            Assert.Contains(DG.Some.Namespace.AccountTraceThrowPlugin.ThrowMessage, entry.ExceptionDetails);
        }

        [Fact]
        public void PluginTraceLogCapturesCustomApiExecution()
        {
            crm.ClearPluginTraceLog();

            orgAdminService.Execute(new OrganizationRequest("dg_TraceApi"));

            var entry = Assert.Single(crm.PluginTraceLog.Where(l => l.TypeName == typeof(DG.Some.Namespace.TraceApi).FullName));

            Assert.Equal("dg_TraceApi", entry.MessageName);
            Assert.Equal(PluginTraceOperationType.Plugin, entry.OperationType);
            Assert.Equal(XrmPluginCore.Enums.ExecutionMode.Synchronous, entry.Mode);
            Assert.Contains("TraceApi running", entry.MessageBlock);
            Assert.Contains("TraceApi step 2", entry.MessageBlock);
            Assert.Null(entry.ExceptionDetails);
        }

        [Fact]
        public void PluginTraceLogCapturesWorkflowActivityExecution()
        {
            crm.AddWorkflow(System.IO.Path.Combine("../../..", "Metadata", "OtherWorkflows", "Activeworkflow.xml"));
            crm.ClearPluginTraceLog();

            var account = new Account { Name = "Wap" };
            account.Id = orgAdminUIService.Create(account);

            var entry = Assert.Single(crm.PluginTraceLog.Where(l => l.OperationType == PluginTraceOperationType.WorkflowActivity));

            Assert.Equal("account", entry.PrimaryEntity);
            Assert.Contains(entry.MessageBlock, m => m.StartsWith("AccountWorkflowActivity executing for"));
            Assert.Null(entry.ExceptionDetails);
        }
    }
}
