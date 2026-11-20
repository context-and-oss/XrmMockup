using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class ConditionExp : IWorkflowNode
    {
        [DataMember]
        public ConditionOperator Operator { get; private set; }
        [DataMember]
        public string[] Parameters { get; private set; }
        [DataMember]
        public string Operand { get; private set; }
        [DataMember]
        public string ReturnName { get; private set; }



        public ConditionExp(string Operator, string[] Parameters, string Operand, string ReturnName)
        {
            this.Operator = (ConditionOperator)Enum.Parse(typeof(ConditionOperator), Operator);
            this.Parameters = Parameters;
            this.Operand = Operand;
            this.ReturnName = ReturnName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var operand = variables[Operand];
            var variablesInstance = variables;
            var parameters = Parameters != null && Parameters.Count() == 1 ? Parameters[0].Split(',').Select(p => variablesInstance[p.Trim()]).ToArray() : null;

            if (Operator != ConditionOperator.NotNull && Operator != ConditionOperator.Null && (operand == null || parameters == null))
            {
                variables[ReturnName] = null;
                return;
            }

            switch (Operator)
            {
                case ConditionOperator.Equal:
                    variables[ReturnName] = operand.Equals(parameters[0]);
                    break;

                case ConditionOperator.NotEqual:
                    variables[ReturnName] = !operand.Equals(parameters[0]);
                    break;

                case ConditionOperator.BeginsWith:
                    variables[ReturnName] = ((string)operand).StartsWith((string)parameters[0]);
                    break;

                case ConditionOperator.DoesNotBeginWith:
                    variables[ReturnName] = !((string)operand).StartsWith((string)parameters[0]);
                    break;

                case ConditionOperator.EndsWith:
                    variables[ReturnName] = ((string)operand).EndsWith((string)parameters[0]);
                    break;

                case ConditionOperator.DoesNotEndWith:
                    variables[ReturnName] = !((string)operand).EndsWith((string)parameters[0]);
                    break;

                case ConditionOperator.Contains:
                    variables[ReturnName] = ((string)operand).Contains((string)parameters[0]);
                    break;

                case ConditionOperator.DoesNotContain:
                    variables[ReturnName] = !((string)operand).Contains((string)parameters[0]);
                    break;

                case ConditionOperator.NotNull:
                    variables[ReturnName] = operand != null;
                    break;

                case ConditionOperator.Null:
                    variables[ReturnName] = operand == null;
                    break;

                case ConditionOperator.In:
                    variables[ReturnName] = parameters.Any(x => x.Equals(operand));
                    break;

                case ConditionOperator.NotIn:
                    variables[ReturnName] = !parameters.Any(x => x.Equals(operand));
                    break;

                case ConditionOperator.On:
                    variables[ReturnName] = ((DateTime)operand).CompareTo((DateTime)parameters[0]) == 0;
                    break;

                case ConditionOperator.OnOrAfter:
                    variables[ReturnName] = ((DateTime)operand).CompareTo((DateTime)parameters[0]) >= 0;
                    break;

                case ConditionOperator.OnOrBefore:
                    variables[ReturnName] = ((DateTime)operand).CompareTo((DateTime)parameters[0]) <= 0;
                    break;

                case ConditionOperator.GreaterThan:
                case ConditionOperator.GreaterEqual:
                case ConditionOperator.LessThan:
                case ConditionOperator.LessEqual:
                    decimal? comparableOperand = null;
                    decimal? comparableParameter = null;
                    if (parameters[0] == null)
                    {
                        variables[ReturnName] = false;
                        break;
                    }
                    if (parameters[0] is int)
                    {
                        comparableOperand = (int)operand;
                        comparableParameter = (int)parameters[0];
                    }
                    else if (parameters[0] is Money)
                    {
                        comparableOperand = (operand as Money).Value;
                        comparableParameter = (parameters[0] as Money).Value;
                    }
                    else if (parameters[0] is decimal)
                    {
                        comparableOperand = (decimal)operand;
                        comparableParameter = (decimal)parameters[0];
                    }
                    else if (parameters[0] is DateTime)
                    {
                        comparableOperand = ((DateTime)operand).Ticks;
                        comparableParameter = ((DateTime)parameters[0]).Ticks;
                    }

                    if (comparableParameter == null)
                    {
                        throw new NotImplementedException($"Unknown type when trying to compare");
                    }

                    switch (Operator)
                    {
                        case ConditionOperator.GreaterThan:
                            variables[ReturnName] = comparableOperand > comparableParameter;
                            break;
                        case ConditionOperator.GreaterEqual:
                            variables[ReturnName] = comparableOperand >= comparableParameter;
                            break;
                        case ConditionOperator.LessThan:
                            variables[ReturnName] = comparableOperand < comparableParameter;
                            break;
                        case ConditionOperator.LessEqual:
                            variables[ReturnName] = comparableOperand <= comparableParameter;
                            break;
                    }
                    break;

                default:
                    throw new NotImplementedException($"Unknown operator '{Operator}'");
            }
        }
    }
}
