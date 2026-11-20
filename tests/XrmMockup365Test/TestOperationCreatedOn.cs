using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Xunit;
using OperationProbe = System.Action<Microsoft.Xrm.Sdk.IPluginExecutionContext, System.DateTime, Microsoft.Xrm.Sdk.IOrganizationService>;

namespace DG.XrmMockupTest
{
    public class TestOperationCreatedOn : UnitTestBase
    {
        private const string ProbeParameter = "OperationCreatedOnProbe";

        public TestOperationCreatedOn(XrmMockupFixture fixture) : base(fixture)
        {
            crm.DisableRegisteredPlugins(true);
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(OperationCreatedOnPlugin));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void AllPluginStagesReadNonDefaultUtcTimeUsingTheSimulatorClock(int offsetDays)
        {
            crm.AddTime(TimeSpan.FromDays(offsetDays));
            var observations = new List<(int Stage, int Mode, DateTime CreatedOn)>();
            var before = DateTime.UtcNow.AddDays(offsetDays);

            Create(orgAdminService, (context, createdOn, service) =>
                observations.Add((context.Stage, context.Mode, createdOn)));

            var after = DateTime.UtcNow.AddDays(offsetDays);
            Assert.Equal(new[] { (10, 0), (20, 0), (40, 0), (40, 1) },
                observations.Select(o => (o.Stage, o.Mode)));
            Assert.All(observations, observation =>
            {
                Assert.NotEqual(default(DateTime), observation.CreatedOn);
                Assert.Equal(DateTimeKind.Utc, observation.CreatedOn.Kind);
                Assert.InRange(observation.CreatedOn, before, after);
            });
        }

        [Fact]
        public void SynchronousStagesAndParentSnapshotsKeepTheRequestCreationTime()
        {
            var timestamps = new List<DateTime>();
            var before = DateTime.UtcNow;

            Create(orgAdminService, (context, createdOn, service) =>
            {
                if (context.Mode != 0) return;
                timestamps.Add(createdOn);
                if (context.Stage == 10)
                    crm.AddTime(TimeSpan.FromDays(1));
                else
                    Assert.Equal(timestamps[0], context.ParentContext.OperationCreatedOn);
            });

            Assert.Equal(3, timestamps.Count);
            Assert.InRange(timestamps[0], before, DateTime.UtcNow);
            Assert.All(timestamps, timestamp => Assert.Equal(timestamps[0], timestamp));
        }

        [Fact]
        public void NestedRequestHasItsOwnTimestampAndPreservesTheParentTimestamp()
        {
            var parentTimestamps = new List<DateTime>();
            var childTimestamps = new List<DateTime>();
            DateTime childBefore = default;
            DateTime childAfter = default;

            Create(orgAdminService, (context, createdOn, service) =>
            {
                if (context.Mode != 0) return;
                parentTimestamps.Add(createdOn);
                if (context.Stage != 20) return;

                crm.AddTime(TimeSpan.FromDays(1));
                childBefore = DateTime.UtcNow.AddDays(1);
                Create(service, (childContext, childCreatedOn, childService) =>
                {
                    if (childContext.Mode != 0) return;
                    childTimestamps.Add(childCreatedOn);
                    Assert.Equal(2, childContext.Depth);
                    var parent = childContext.ParentContext;
                    while (parent.Depth > 1) parent = parent.ParentContext;
                    Assert.Equal(createdOn, parent.OperationCreatedOn);
                });
                childAfter = DateTime.UtcNow.AddDays(1);
            });

            Assert.Equal(3, parentTimestamps.Count);
            Assert.All(parentTimestamps, timestamp => Assert.Equal(parentTimestamps[0], timestamp));
            Assert.Equal(3, childTimestamps.Count);
            Assert.All(childTimestamps, timestamp =>
            {
                Assert.InRange(timestamp, childBefore, childAfter);
                Assert.Equal(childTimestamps[0], timestamp);
            });
            Assert.True(childTimestamps[0] > parentTimestamps[0]);
        }

        [Fact]
        public void AsyncTimestampIsCapturedAtStagingAndSurvivesDeferredExecution()
        {
            DateTime stagedBefore = default;
            DateTime stagedAfter = default;
            DateTime asyncCreatedOn = default;
            var asyncExecuted = false;

            Create(orgAdminService, (context, createdOn, service) =>
            {
                if (context.Mode != 0 || context.Stage != 20) return;

                Create(service, (childContext, childCreatedOn, childService) =>
                {
                    if (childContext.Mode == 0 && childContext.Stage == 40)
                    {
                        // Async work is staged after synchronous PostOperation finishes.
                        crm.AddTime(TimeSpan.FromDays(1));
                        stagedBefore = DateTime.UtcNow.AddDays(1);
                    }
                    else if (childContext.Mode == 1)
                    {
                        asyncExecuted = true;
                        asyncCreatedOn = childCreatedOn;
                        Assert.Equal(DateTimeKind.Utc, childCreatedOn.Kind);
                    }
                });

                stagedAfter = DateTime.UtcNow.AddDays(1);
                Assert.False(asyncExecuted);
                // Child async work waits until the outermost request finishes.
                crm.AddTime(TimeSpan.FromDays(1));
            });

            Assert.True(asyncExecuted);
            Assert.InRange(asyncCreatedOn, stagedBefore, stagedAfter);
        }

        [Fact]
        public void IndependentRequestsCaptureNewCreationTimes()
        {
            var timestamps = new List<DateTime>();
            OperationProbe probe = (context, createdOn, service) =>
            {
                if (context.Stage == 10) timestamps.Add(createdOn);
            };

            var firstBefore = DateTime.UtcNow;
            Create(orgAdminService, probe);
            var firstAfter = DateTime.UtcNow;
            crm.AddTime(TimeSpan.FromDays(1));
            var secondBefore = DateTime.UtcNow.AddDays(1);
            Create(orgAdminService, probe);

            Assert.Equal(2, timestamps.Count);
            Assert.InRange(timestamps[0], firstBefore, firstAfter);
            Assert.InRange(timestamps[1], secondBefore, DateTime.UtcNow.AddDays(1));
        }

        private static void Create(IOrganizationService service, OperationProbe probe)
        {
            var request = new CreateRequest { Target = new Account { Name = "OperationCreatedOn" } };
            request.Parameters[ProbeParameter] = probe;
            service.Execute(request);
        }

        // Nested so fixture assembly scanning does not register this probe for other tests.
        public class OperationCreatedOnPlugin : XrmPluginCore.Plugin
        {
            public OperationCreatedOnPlugin()
            {
#pragma warning disable CS0618 // Match the existing test plugin registration convention.
                RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreValidation, Execute);
                RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreOperation, Execute);
                RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, Execute);
                RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, Execute)
                    .SetExecutionMode(ExecutionMode.Asynchronous);
#pragma warning restore CS0618
            }

            protected void Execute(LocalPluginContext localContext)
            {
                var context = localContext.PluginExecutionContext;
                var probe = (OperationProbe)context.InputParameters[ProbeParameter];
                probe(context, context.OperationCreatedOn, localContext.OrganizationService);
            }
        }
    }
}
