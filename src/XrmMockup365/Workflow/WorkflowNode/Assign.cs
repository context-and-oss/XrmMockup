using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Assign : IWorkflowNode
    {
        [DataMember]
        public string To { get; private set; }
        [DataMember]
        public string Value { get; private set; }

        public Assign(string To, string Value)
        {
            this.To = To;
            this.Value = Value;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var comparaters = new string[] { "<", "<=", "==", ">=", ">" };
            if (comparaters.Any(c => Value.Contains(c)))
            {
                var comparater = comparaters.First(c => Value.Contains(c));
                var sides = Value.Split(new[] { comparater }, StringSplitOptions.None).Select(s => s.Trim()).ToArray();
                var left = sides[0].ToCorrectType(variables, timeOffset);
                var right = sides[1].ToCorrectType(variables, timeOffset);
                if (left is DateTime && right is DateTime)
                {
                    var comparison = ((DateTime)left).CompareTo((DateTime)right);
                    switch (comparater)
                    {
                        case "<":
                            variables[To] = comparison < 0;
                            break;
                        case "<=":
                            variables[To] = comparison <= 0;
                            break;
                        case "==":
                            variables[To] = comparison == 0;
                            break;
                        case ">=":
                            variables[To] = comparison >= 0;
                            break;
                        case ">":
                            variables[To] = comparison > 0;
                            break;
                    }
                    return;
                }

                throw new NotImplementedException("Unknown type when assigning with a value containing comparaters");
            }

            if (Value.Contains(".Id"))
            {
                var toEntity = variables[To.Replace(".Id", "")] as Entity;

                if (Value.Contains("related_"))
                {
                    var regex = new Regex(@"_.+#");
                    var relatedAttr = regex.Match(Value).Value.TrimEdge();
                    var primaryEntity = variables["InputEntities(\"primaryEntity\")"] as Entity;
                    if (!primaryEntity.Attributes.ContainsKey(relatedAttr))
                    {
                        // variables[VariableName] = null;
                        return;
                    }
                    var entRef = primaryEntity.Attributes[relatedAttr] as EntityReference;
                    if (entRef == null)
                    {
                        //  variables[VariableName] = null;
                        return;
                    }
                    var entity = orgService.Retrieve(entRef.LogicalName, entRef.Id, new ColumnSet(true));
                    toEntity.Id = entity.Id;
                }
                else
                {
                    var valueEntity = variables[Value.Replace(".Id", "")] as Entity;
                    toEntity.Id = valueEntity.Id;
                }

                return;
            }

            if (Value.Contains("CreatedEntities(") && Value.Contains("#Temp") && variables.ContainsKey(To))
            {
                var tmp = variables[Value] as Entity;
                var to = variables[To] as Entity;
                foreach (var attr in tmp.Attributes)
                {
                    to.Attributes[attr.Key] = attr.Value;
                }
                return;
            }

            if (Value.Contains(".VisualBasic.IIf("))
            {
                var regex = new Regex(@"\(.+\)");
                var parameters = regex.Match(Value).Value.TrimEdge().Split(',').Select(s => s.Trim()).ToArray();
                if (parameters[0].Contains(".VisualBasic.IsNothing("))
                {
                    var variable = regex.Match(parameters[0]).Value.TrimEdge();
                    variables[To] = !variables.ContainsKey(variable) || variables[variable] == null ?
                        parameters[1].ToCorrectType(variables, timeOffset) : parameters[2].ToCorrectType(variables, timeOffset);
                    return;
                }
                throw new NotImplementedException("Unknown condition in IIf in workflow");
            }

            variables[To] = Value.ToCorrectType(variables, timeOffset);
        }
    }
}
