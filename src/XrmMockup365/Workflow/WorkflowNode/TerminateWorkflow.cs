using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WorkflowExecuter
{
    [DataContract]
    internal class TerminateWorkflow : IWorkflowNode
    {
        [DataMember]
        public OperationStatus status { get; private set; }
        [DataMember]
        public string messageId { get; private set; }

        public TerminateWorkflow(OperationStatus status, string messageId)
        {
            this.status = status;
            this.messageId = messageId;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var sb = new StringBuilder($"Workflow exited with status '{status}'");
            if (variables[messageId] != null && (variables[messageId] as string != ""))
            {
                sb.Append($", the reason was '{variables[messageId]}'");
            }
            if (status == OperationStatus.Canceled)
            {
                throw new InvalidPluginExecutionException(OperationStatus.Canceled, variables[messageId].ToString());
            }
            else
            {
                Console.WriteLine(sb.ToString());
            }
        }
    }
}
