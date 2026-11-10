using System;
using System.Collections.Generic;
using System.IO;
using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Internal;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestWorkflowCacheIsolation
    {
        private static WorkflowManager CreateManager()
        {
            return new WorkflowManager(
                null,
                new[] { typeof(DG.Some.Namespace.AccountWorkflowActivity) },
                false,
                new List<Entity>(),
                new SortedDictionary<string, EntityMetadata>());
        }

        [Fact]
        public void ParsingSameWorkflowInAnotherInstanceDoesNotResetExecutionState()
        {
            var workflow = Utility.GetWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "Activeworkflow.xml"));
            // Keep this regression independent of other tests using the same workflow file.
            workflow.Id = Guid.NewGuid();
            var firstManager = CreateManager();
            var secondManager = CreateManager();
            var firstTree = firstManager.ParseWorkflow(workflow);
            Assert.NotNull(firstTree);

            var account = new Account { Id = Guid.NewGuid(), Name = "First instance" };
            const string primaryEntity = "InputEntities(\"primaryEntity\")";
            firstTree.Variables[primaryEntity] = account;

            // This used to return the first instance's tree and HardReset its variables,
            // even if the first instance was still executing a workflow for its account.
            var secondTree = secondManager.ParseWorkflow(workflow);
            Assert.NotNull(secondTree);
            Assert.Same(account, firstTree.Variables[primaryEntity]);
            Assert.NotSame(firstTree, secondTree);
            Assert.NotSame(firstTree.Variables, secondTree.Variables);

            secondTree.Variables[primaryEntity] = new Account { Id = Guid.NewGuid(), Name = "Second instance" };
            Assert.Same(account, firstTree.Variables[primaryEntity]);

            // Caching within one manager is still allowed, with state reset between runs.
            Assert.Same(firstTree, firstManager.ParseWorkflow(workflow));
            Assert.Null(firstTree.Variables[primaryEntity]);
            Assert.NotNull(secondTree.Variables[primaryEntity]);
        }
    }
}
