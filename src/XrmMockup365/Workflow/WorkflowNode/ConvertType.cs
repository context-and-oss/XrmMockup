using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class ConvertType : IWorkflowNode
    {
        [DataMember]
        public string Input { get; private set; }
        [DataMember]
        public string Type { get; private set; }
        [DataMember]
        public string Result { get; private set; }

        public ConvertType(string Input, string Type, string Result)
        {
            this.Input = Input;
            this.Type = Type;
            this.Result = Result;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {

            if (!variables.ContainsKey(Input))
            {
                variables.Add(Input, null);
                variables.Add(Result, null);
                return;
            }

            switch (Type)
            {
                case "EntityReference":
                    if (variables[Input] is EntityReference)
                    {
                        variables[Result] = variables[Input];
                        break;
                    }
                    throw new NotImplementedException($"Unknown input when trying to convert type {variables[Input].GetType().Name} to entityreference");
                default:
                    if (variables[Input] == null)
                    {
                        variables[Result] = null;
                        break;
                    }
                    else if (Type.ToLower() == variables[Input].GetType().Name.ToLower())
                    {
                        variables[Result] = variables[Input];
                        break;
                    }
                    throw new NotImplementedException($"Unknown input when trying to convert type {variables[Input].GetType().Name} to {Type}");
            }
        }
    }
}
