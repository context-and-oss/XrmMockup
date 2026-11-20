using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class LogicalComparison : IWorkflowNode
    {
        [DataMember]
        public LogicalOperator Operator { get; private set; }
        [DataMember]
        public string LeftOperand { get; private set; }
        [DataMember]
        public string RightOperand { get; private set; }
        [DataMember]
        public string Result { get; private set; }

        public LogicalComparison(string Operator, string LeftOperand, string RightOperand, string Result)
        {
            this.Operator = (LogicalOperator)Enum.Parse(typeof(LogicalOperator), Operator);
            this.LeftOperand = LeftOperand;
            this.RightOperand = RightOperand;
            this.Result = Result;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var left = (bool?)variables[LeftOperand];
            var right = (bool?)variables[RightOperand];

            if (!left.HasValue || !right.HasValue)
            {
                variables[Result] = null;
                return;
            }

            switch (Operator)
            {
                case LogicalOperator.Or:
                    variables[Result] = left.Value || right.Value;
                    break;
                case LogicalOperator.And:
                    variables[Result] = left.Value && right.Value;
                    break;
                default:
                    throw new NotImplementedException($"Unknown operator '{Operator}'");
            }
        }
    }
}
