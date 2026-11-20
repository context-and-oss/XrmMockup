using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Trim : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Method { get; private set; }

        public Trim(string[][] Parameters, string VariableName, string Method)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Method = Method;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var word = (string)variables[Parameters[0][0]];
            var trimLength = int.Parse((string)variables[Parameters[0][1]]);
            switch (Method)
            {
                case "TrimLeft":
                    variables[VariableName] = word.Substring(trimLength);
                    break;
                case "TrimRight":
                    variables[VariableName] = word.Substring(0, word.Length - trimLength);
                    break;
                default:
                    throw new NotImplementedException($"Unknown trim method '{Method}'");
            }
        }
    }
}
