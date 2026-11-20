using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Workflow;

namespace WorkflowExecuter
{
    [DataContract]
    internal class CreateVariable : IWorkflowNode
    {
        [DataMember]
        public object[][] Parameters { get; private set; }
        [DataMember]
        public string TargetType { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public CreateVariable(object[][] Parameters, string TargetType, string VariableName)
        {
            this.Parameters = Parameters;
            this.TargetType = TargetType;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            switch (TargetType)
            {
                case "String":
                    variables[VariableName] = ((string)Parameters[0][1]).Contains("(Arguments)") ? variables[(string)Parameters[0][1]] : (string)Parameters[0][1];
                    break;
                case "Boolean":
                    {
                        var value = ((string)Parameters[0][1]).ToLower();
                        variables[VariableName] = value == "1" || value == "true" ? true : false;
                    }
                    break;
                case "Int32":
                    variables[VariableName] = int.Parse((string)Parameters[0][1]);
                    break;
                case "Guid":
                    variables[VariableName] = Guid.Parse((string)Parameters[0][1]);
                    break;
                case "Decimal":
                    variables[VariableName] = decimal.Parse((string)Parameters[0][1]);
                    break;
                case "OptionSetValue":
                    variables[VariableName] = new OptionSetValue(int.Parse((string)Parameters[0][1]));
                    break;
                case "Money":
                    variables[VariableName] = new Money(decimal.Parse((string)Parameters[0][1]));
                    break;
                case "EntityReference":
                    if (Parameters[0].Count() == 5 && ((string)Parameters[0][0]).Contains("EntityReference"))
                    {
                        var entRef = new EntityReference((string)Parameters[0][1], (Guid)variables[(string)Parameters[0][3]]);
                        entRef.Name = (string)Parameters[0][2];
                        variables[VariableName] = entRef;
                    }
                    else if (Parameters[0].Count() == 3 && ((string)Parameters[0][0]).Contains("EntityReference"))
                    {
                        if (!variables.ContainsKey((string)Parameters[0][1]))
                        {
                            throw new WorkflowException($"The variable '{(string)Parameters[0][1]}' has not been initialized");
                        }

                        variables[VariableName] = variables[(string)Parameters[0][1]];
                    }
                    else if (Parameters[0].Count() == 3 && ((string)Parameters[0][0]).Contains("Guid"))
                    {
                        variables[VariableName] = new Guid((string)Parameters[0][1]);
                    }
                    break;
                case "DateTime":
                    {
                        var value = (string)Parameters[0][1];
                        if (int.TryParse(value, out int result))
                        {
                            variables[VariableName] = result;
                        }
                        else
                        {
                            variables[VariableName] = DateTime.Parse(value);
                        }
                    }
                    break;
                case "[System.DateTime.MinValue]":
                    {
                        variables[VariableName] = DateTime.MinValue;
                    }
                    break;
                case "Object":
                    {
                        if (Parameters[0][1] as string == "[System.DateTime.MinValue]")
                        {
                            variables[VariableName] = DateTime.MinValue;
                        }
                        else if (Parameters[0][1] as string == "[System.DateTime.MaxValue]")
                        {
                            variables[VariableName] = DateTime.MaxValue;
                        }
                        else
                        {
                            variables[VariableName] = null;
                        }
                    }
                    break;
                case "XrmTimeSpan":
                    {
                        var param = Parameters[0].Select(s => int.Parse(s as string)).ToArray();
                        variables[VariableName] = new XrmTimeSpan(param[4], param[3], param[0], param[1], param[2]);
                    }
                    break;
                case "EntityCollection":
                    {
                        var variablesInstance = variables;
                        var entities = Parameters[0].Skip(1)
                            .Select(v => variablesInstance[(string)v] as EntityReference)
                            .Where(r => r != null)
                            .Select(r => orgService.Retrieve(r.LogicalName, r.Id, new ColumnSet(true)));
                        variables[VariableName] = new EntityCollection(entities.ToList());
                    }
                    break;
                default:
                    throw new WorkflowException($"Unknown target type: {TargetType}.");
            }
        }
    }
}
