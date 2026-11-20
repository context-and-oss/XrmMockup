using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestWorkflowDiagnosticsIntegration : IClassFixture<XrmMockupFixture>
    {
        private readonly XrmMockupSettings baseline;
        public TestWorkflowDiagnosticsIntegration(XrmMockupFixture fixture) { baseline = fixture.Settings; }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConfiguredFactoryReceivesDiagnosticsAndCodeActivityTraces(bool enabled)
        {
            var factory = new CapturingFactory();
            // Do not mutate the shared fixture settings: opt-in is instance-specific.
            var settings = new XrmMockupSettings
            {
                BasePluginTypes = baseline.BasePluginTypes,
                CodeActivityInstanceTypes = baseline.CodeActivityInstanceTypes,
                EnableProxyTypes = baseline.EnableProxyTypes,
                IncludeAllWorkflows = false,
                MetadataDirectoryPath = baseline.MetadataDirectoryPath,
                EnableWorkflowTracing = enabled,
                TracingServiceFactory = factory
            };
            var crm = XrmMockup365.GetInstance(settings);
            var workflowPath = Path.Combine(settings.MetadataDirectoryPath, "OtherWorkflows", "Activeworkflow.xml");
            var workflow = DG.Tools.XrmMockup.Internal.Utility.GetWorkflow(workflowPath);
            crm.AddWorkflow(workflowPath);
            crm.ClearPluginTraceLog();
            var service = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            service.Create(new Account { Name = "Wap" });

            Assert.Contains(factory.Messages, m => m.StartsWith("AccountWorkflowActivity executing for"));
            Assert.Equal(enabled, factory.Messages.Any(m => m.StartsWith("Starting ")));
            Assert.Equal(enabled, factory.Messages.Any(m => m.Contains("completed successfully")));
            if (enabled)
            {
                var start = Assert.Single(factory.Messages.Where(m => m.StartsWith("Starting ")));
                Assert.Contains(workflow.GetAttributeValue<string>("name"), start);
                Assert.Contains(workflow.Id.ToString(), start);
                Assert.Contains(factory.Messages, m => m.StartsWith("Entering CallCodeActivity."));
            }

            var entry = Assert.Single(crm.PluginTraceLog.Where(e => e.OperationType == PluginTraceOperationType.WorkflowActivity));
            Assert.Contains(entry.MessageBlock, m => m.StartsWith("AccountWorkflowActivity executing for"));
            Assert.Equal(enabled, entry.MessageBlock.Any(m => m.StartsWith("Starting ")));
            Assert.Equal(enabled, entry.MessageBlock.Any(m => m.Contains("completed successfully")));
            Assert.Null(entry.ExceptionDetails);
            Assert.Contains(factory.Services, s => s.Messages.SequenceEqual(entry.MessageBlock));
        }

        private sealed class CapturingFactory : ITracingServiceFactory
        {
            internal List<string> Messages { get; } = new List<string>();
            internal List<TracingService> Services { get; } = new List<TracingService>();
            public ITracingService GetService()
            {
                var service = new TracingService(Messages.Add);
                Services.Add(service);
                return service;
            }
        }
    }
}
