using System;
using System.Collections.Generic;
using System.Text;

#if DATAVERSE_SERVICE_CLIENT
namespace Microsoft.Xrm.Sdk.Workflow
{
    //
    // Summary:
    //     Provides access to the data associated with the process instance.
    public interface IWorkflowContext : IExecutionContext
    {
        //
        // Summary:
        //     Gets the stage information of the process instance.
        //
        // Returns:
        //     Type: String The stage information of the process instance.
        string StageName { get; }

        //
        // Summary:
        //     Gets the process category information of the process instance: workflow or dialog.
        //
        // Returns:
        //     Type: Int32 The process category information of the process instance: workflow
        //     or dialog.
        int WorkflowCategory { get; }

        //
        // Summary:
        //     Indicates how the workflow is to be executed.
        //
        // Returns:
        //     Type: Int32 Value = 0 (background/asynchronous), 1 (real-time).
        int WorkflowMode { get; }

        //
        // Summary:
        //     Gets the parent context.
        //
        // Returns:
        //     Type: Microsoft.Xrm.Sdk.Workflow.IWorkflowContext The parent context.
        IWorkflowContext ParentContext { get; }
    }
}
#endif