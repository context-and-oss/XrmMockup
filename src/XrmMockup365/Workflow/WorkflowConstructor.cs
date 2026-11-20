using DG.Tools.XrmMockup.Internal;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WorkflowParser;
using Argument = WorkflowParser.Argument;
using Variable = WorkflowParser.Variable;


namespace WorkflowExecuter
{
    internal class WorkflowConstructor
    {

        public enum ParseAs
        {
            Workflow, RollUp
        }

        public static WorkflowTree Parse(Entity workflow, Dictionary<string, CodeActivity> codeActivites, ParseAs parseAs)
        {
            if (workflow == null || workflow.LogicalName != LogicalNames.Workflow)
            {
                throw new WorkflowException($"Entity had logicalname '{LogicalNames.Workflow}' instead of workflow");
            }
            var parsed = Parser.Parse(workflow.GetAttributeValue<string>("xaml"));
            var workflowName = workflow.GetAttributeValue<string>("name");
            var triggerFields = new HashSet<string>();
            if (workflow.GetAttributeValue<string>("triggeronupdateattributelist") != null)
            {
                foreach (var field in workflow.GetAttributeValue<string>("triggeronupdateattributelist").Split(','))
                {
                    triggerFields.Add(field);
                }
            }

            var arguments = parsed.Members.Properties.Where(p => p.Attributes != null).Select(p => ConvertToArgument(p));
            var input = arguments.Where(a => a.Direction == WorkflowArgument.DirectionType.Input && !a.IsTarget).ToList();
            var output = arguments.Where(a => a.Direction == WorkflowArgument.DirectionType.Output && !a.IsTarget).ToList();

            return new WorkflowTree(CompressTree(parsed.Workflow, parseAs, workflowName),
                workflow.GetAttributeValue<bool?>("triggeroncreate"), workflow.GetAttributeValue<bool?>("triggerondelete"),
               triggerFields, workflow.GetOptionSetValue<workflow_runas>("runas"), workflow.GetOptionSetValue<Workflow_Scope>("scope"),
               workflow.GetOptionSetValue<workflow_stage>("createstage"), workflow.GetOptionSetValue<workflow_stage>("updatestage"),
               workflow.GetOptionSetValue<workflow_stage>("deletestage"), workflow.GetOptionSetValue<Workflow_Mode>("mode"),
               workflow.GetAttributeValue<EntityReference>("ownerid").Id, workflow.GetAttributeValue<string>("primaryentity"), codeActivites,
               input, output);
        }

        private static WorkflowArgument ConvertToArgument(Property p)
        {
            return new WorkflowArgument(p.Name, bool.Parse(p.Attributes[0].Value), bool.Parse(p.Attributes[1].Value),
                p.Attributes[2].Value, p.Attributes[3].Value == "Input" ? WorkflowArgument.DirectionType.Input : WorkflowArgument.DirectionType.Output,
                p.Attributes[4].Value);
        }

        public static WorkflowTree ParseRollUp(string xaml, string workflowName)
        {
            var parsed = Parser.Parse(xaml);
            return new WorkflowTree(CompressTree(parsed.Workflow, ParseAs.RollUp, workflowName)); ;
        }

        public static WorkflowTree ParseCalculated(string xaml, string workflowName)
        {
            var parsed = Parser.Parse(xaml);
            return new WorkflowTree(CompressTree(parsed.Workflow, ParseAs.Workflow, workflowName))
            {
                IsCalculatedField = true,
            };
        }

        private static IWorkflowNode CompressTree(WorkflowParser.Workflow workflow, ParseAs parseAs, string workflowName)
        {
            if (parseAs == ParseAs.RollUp)
            {
                return CompressRollUp(workflow.Activities[0] as WorkflowParser.ActivitySequence, workflowName);
            }
            if (parseAs == ParseAs.Workflow)
            {
                return new ActivityList(CompressActivityCollection(workflow.Activities, workflowName).ToArray());
            }
            throw new NotImplementedException("No logic for parsing that kind of xaml in workflowconstructor");
        }

        private static IWorkflowNode CompressRollUp(WorkflowParser.ActivitySequence sequence, string workflowName)
        {
            var activites = sequence.Activities;
            var hierarchicalRelationshipName = (activites[0] as WorkflowParser.ActivitySequence).Variables[0].Default.TrimEdge();
            var filterResult = (activites[1] as WorkflowParser.ActivitySequence).Variables != null ?
                (activites[1] as WorkflowParser.ActivitySequence).Variables[0].Name : null;
            var aggregateResult = (activites[2] as WorkflowParser.ActivitySequence).Variables[0].Name;
            var filter = new List<IWorkflowNode>();
            filter.AddRange(CompressVariableCollection((activites[1] as WorkflowParser.ActivitySequence).Variables));
            filter.AddRange(CompressActivityCollection((activites[1] as WorkflowParser.ActivitySequence).Activities, workflowName));
            var aggregation = new List<IWorkflowNode>();
            aggregation.AddRange(CompressVariableCollection((activites[2] as WorkflowParser.ActivitySequence).Variables));
            aggregation.AddRange(CompressActivityCollection((activites[2] as WorkflowParser.ActivitySequence).Activities, workflowName));
            return new RollUp(hierarchicalRelationshipName, filterResult, aggregateResult, filter, aggregation);
        }



        private static IWorkflowNode CompressCollection(Collection collection, string workflowName)
        {
            if (collection.Variables != null)
            {
                return new ActivityList(CompressVariableCollection(collection.Variables).ToArray(), VariableNames(collection.Variables));
            }

            if (collection.Activities != null)
            {
                return new ActivityList(CompressActivityCollection(collection.Activities, workflowName).ToArray());
            }

            return new Skip();
        }

        private static List<string> VariableNames(Variable[] collectionVariables)
        {
            var variableNames = new List<string>();
            foreach (var variable in collectionVariables)
            {
                variableNames.Add(variable.Name);
            }
            return variableNames;
        }

        private static List<IWorkflowNode> CompressVariableCollection(WorkflowParser.Variable[] variables)
        {
            var nodes = new List<IWorkflowNode>();
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    // "True"/"False" are reserved boolean constants seeded by WorkflowTree.Reset().
                    // CRM-generated calculated-field XAML re-declares a local variable named "True"
                    // with Default="False"; materializing it would clobber the constant and make every
                    // ConditionBranch (whose Condition is "[True]") evaluate false, skipping the whole
                    // calculation. Leave the engine constants untouched.
                    if (variable.Name == "True" || variable.Name == "False")
                    {
                        continue;
                    }
                    if (variable.Default != null)
                    {
                        if (variable.Type.StartsWith("scg:Dictionary"))
                        {
                            nodes.Add(new CreateVariable(new object[][] { new object[] { "", variable.Default } }, variable.Type.Replace("scg:",""), variable.Name));
                        }
                        else
                        {
                            nodes.Add(new CreateVariable(new object[][] { new object[] { "", variable.Default } }, variable.Type.Split(':')[1], variable.Name));
                        }
                    }
                    else if (variable.DefaultList != null)
                    {
                        if (variable.Type.Contains("XrmTimeSpan"))
                        {
                            var timespan = variable.DefaultList[0].XrmTimeSpan;
                            nodes.Add(new CreateVariable(new object[][] { new[] { timespan.Days, timespan.Hours, timespan.Minutes, timespan.Months, timespan.Years } }, variable.Type.Split(':')[1], variable.Name));
                        }
                        else
                        {
                            nodes.Add(new CreateVariable(new object[][] { new[] { "" }.Concat(variable.DefaultList.Select(l => l.Value)).ToArray() }, variable.Type.Split(':')[1], variable.Name));
                        }
                    }
                }
            }
            return nodes;
        }
        private static List<IWorkflowNode> CompressActivityCollection(WorkflowParser.Activity[] activities, string workflowName)
        {
            var nodes = new List<IWorkflowNode>();
            if (activities != null)
            {
                for (int i = 0; i < activities.Length; i++)
                {
                    var node = CompressActivity(activities[i], workflowName);
                    if (isCondition(node) && activities.Skip(i + 1).Take(activities.Length).Count() > 0)
                    {
                        ((Condition)node).Otherwise = new ActivityList(CompressActivityCollection(
                                activities.Skip(i + 1).Take(activities.Length).ToArray(), workflowName).ToArray());
                        nodes.Add(node);
                        break;
                    }
                    if (node is ActivityList)
                    {
                        nodes.AddRange((node as ActivityList).Activities);
                    }
                    else
                    {
                        nodes.Add(node);
                    }
                }
            }

            return nodes;
        }


        private static bool isCondition(IWorkflowNode node)
        {
            return node is Condition;
        }

        private static IWorkflowNode CompressActivity(WorkflowParser.Activity activity, string workflowName)
        {
            if (activity is ActivityReference) { return CompressActivityReference(activity as ActivityReference, workflowName); }
            if (activity is WorkflowParser.GetEntityProperty)
            {
                return CompressGetEntityProperty(activity as WorkflowParser.GetEntityProperty);
            }
            if (activity is WorkflowParser.TerminateWorkflow)
            {
                return CompressTerminateWorkflow(activity as WorkflowParser.TerminateWorkflow);
            }

            if (activity is WorkflowParser.SetEntityProperty)
            {
                return CompressSetEntityProperty(activity as WorkflowParser.SetEntityProperty);
            }

            if (activity is WorkflowParser.CreateEntity)
            {
                return CompressCreateEntity(activity as WorkflowParser.CreateEntity);
            }

            if (activity is WorkflowParser.UpdateEntity)
            {
                return CompressUpdateEntity(activity as WorkflowParser.UpdateEntity);
            }

            if (activity is WorkflowParser.AssignEntity)
            {
                return CompressAssignEntity(activity as WorkflowParser.AssignEntity);
            }

            if (activity is WorkflowParser.SetAttributeValue)
            {
                return CompressSetAttributeValue(activity as WorkflowParser.SetAttributeValue);
            }

            if (activity is WorkflowParser.SetState)
            {
                return CompressSetState(activity as WorkflowParser.SetState);
            }

            if (activity is WorkflowParser.Assign)
            {
                return CompressAssign(activity as WorkflowParser.Assign);
            }

            if (activity is WorkflowParser.ActivitySequence)
            {
                return CompressSequence(activity as WorkflowParser.ActivitySequence, workflowName);
            }

            if (activity is WorkflowParser.Collection)
            {
                return CompressCollection(activity as WorkflowParser.Collection, workflowName);
            }

            if (activity is WorkflowParser.Null)
            {
                return new Skip();
            }

            if (activity is WorkflowParser.Postpone)
            {
                return CompressPostpone(activity as WorkflowParser.Postpone);
            }

            if (activity is WorkflowParser.Persist)
            {
                return new Persist();
            }

            if (activity is WorkflowParser.SendEmail)
            {
                return CompressSendEmail(activity as WorkflowParser.SendEmail);
            }

            throw new NotImplementedException("Unknown activity, implement the compressor.");
        }

        private static IWorkflowNode CompressPostpone(WorkflowParser.Postpone postpone)
        {
            return new Postpone(postpone.BlockExecution, postpone.PostponeUntil.TrimEdge());
        }

        private static IWorkflowNode CompressSequence(WorkflowParser.ActivitySequence sequence, string workflowName)
        {
            var nodes = new List<IWorkflowNode>();
            nodes.AddRange(CompressVariableCollection(sequence.Variables));
            nodes.AddRange(CompressActivityCollection(sequence.Activities, workflowName));
            return new ActivityList(nodes.ToArray());
        }

        private static IWorkflowNode CompressAssign(WorkflowParser.Assign assign)
        {
            return new Assign(assign.To.TrimEdge(), assign.Value.TrimEdge());
        }

        private static IWorkflowNode CompressSetState(WorkflowParser.SetState setState)
        {
            return new SetState(setState.Entity.TrimEdge(), setState.EntityId, setState.EntityName,
                new Microsoft.Xrm.Sdk.OptionSetValue(setState.StateArguments.First().ReferenceLiteral.OptionSetValue.Value),
                new Microsoft.Xrm.Sdk.OptionSetValue(setState.StatusArguments.First().ReferenceLiteral.OptionSetValue.Value));
        }

        private static IWorkflowNode CompressSetAttributeValue(WorkflowParser.SetAttributeValue setAttributeValue)
        {
            return new SetAttributeValue(setAttributeValue.Entity.TrimEdge(), setAttributeValue.EntityName);
        }

        private static IWorkflowNode CompressAssignEntity(WorkflowParser.AssignEntity assignEntity)
        {
            Regex reg = new Regex(@"\(.+\,");
            var ownerId = reg.Match(assignEntity.Owner);
            return new AssignEntity(assignEntity.Entity.TrimEdge(), ownerId.Value.TrimEdge());
        }

        private static IWorkflowNode CompressUpdateEntity(WorkflowParser.UpdateEntity updateEntity)
        {
            return new UpdateEntity(updateEntity.Entity.TrimEdge(), updateEntity.EntityName);
        }

        private static IWorkflowNode CompressCreateEntity(WorkflowParser.CreateEntity createEntity)
        {
            if (createEntity.EntityId.Contains("Null"))
            {
                return new CreateEntity(null, createEntity.Entity.TrimEdge(), createEntity.EntityName);
            }

            // check entity id is correct format
            return new CreateEntity(createEntity.EntityId, createEntity.Entity.TrimEdge(), createEntity.EntityName);
        }

        private static IWorkflowNode CompressSetEntityProperty(WorkflowParser.SetEntityProperty setEntityProperty)
{
            var targetTypeArg = setEntityProperty.InArguments?.First(a => a.ReferenceLiteral != null);
            var targetType = targetTypeArg?.ReferenceLiteral?.Value?.Split(':')[1];

            return new SetEntityProperty(setEntityProperty.Attribute, setEntityProperty.Entity.TrimEdge(), setEntityProperty.EntityName,
                setEntityProperty.Value.TrimEdge(), targetType);
        }

        private static IWorkflowNode CompressGetEntityProperty(WorkflowParser.GetEntityProperty getEntityProperty)
        {
            var targetTypeArg = getEntityProperty.InArguments?.First(a => a.ReferenceLiteral != null);
            var targetType = targetTypeArg?.ReferenceLiteral?.Value?.Split(':')[1];

            return new GetEntityProperty(getEntityProperty.Attribute, getEntityProperty.Entity.TrimEdge(), getEntityProperty.EntityName,
                getEntityProperty.Value.TrimEdge(), targetType);
        }

        private static IWorkflowNode CompressTerminateWorkflow(WorkflowParser.TerminateWorkflow terminateWorkflow)
        {
            OperationStatus status;
            if (terminateWorkflow.Exception.Contains("Microsoft.Xrm.Sdk.OperationStatus.Succeeded"))
            {
                status = OperationStatus.Succeeded;
            }
            else if (terminateWorkflow.Exception.Contains("Microsoft.Xrm.Sdk.OperationStatus.Canceled"))
            {
                status = OperationStatus.Canceled;
            }
            else
            {
                throw new WorkflowException($"Unknown exception in terminateWorkflow '{terminateWorkflow.Exception}'");
            }

            Regex reg = new Regex(@"\(.+\,");
            var messageId = reg.Match(terminateWorkflow.Reason);
            return new TerminateWorkflow(status, messageId.Value.TrimEdge());
        }


        private static IWorkflowNode CompressActivityReference(ActivityReference activityReference, string workflowName)
        {
            return CompressActivityReference(activityReference, new List<IWorkflowNode>(), workflowName);
        }

        private static IWorkflowNode CompressActivityReference(ActivityReference activityReference, List<IWorkflowNode> toPrepend, string workflowName)
        {
            var args = activityReference.Arguments;
            if (args != null && args.Count() > 0)
            {
                var outArg = args.FirstOrDefault(a => a is WorkflowParser.OutArgument);

                if (!activityReference.AssemblyQualifiedName.Contains("Microsoft.Crm.Workflow.Activities."))
                {
                    var regex = new Regex(@"[^\.][\w]+(?=,)");
                    var codeActivityName = regex.Match(activityReference.AssemblyQualifiedName).Value;
                    var inArguments = args.Where(a => a is WorkflowParser.InArgument).ToDictionary(arg => arg.Key, arg => ParseCodeActivityArgument(arg.Value));
                    var outArguments = args.Where(a => a is WorkflowParser.OutArgument).ToDictionary(a => a.Value.TrimEdge(), a => a.Key);
                    return new CallCodeActivity(codeActivityName, workflowName, inArguments, outArguments);
                }

                switch (args[0].Key)
                {
                    case "ConditionOperator":
                        if (outArg == null)
                        {
                            throw new WorkflowException("Found no out argument, please check your workflow has the correct format");
                        }
                        if (args.Length == 4)
                        {
                            return new ConditionExp(args[0].Value,
                                CompressParameters(args[1].Value).ToArray(),
                                args[2].Value.TrimEdge(),
                                outArg.Value.TrimEdge());
                        }
                        else
                        {
                            return new ConditionExp(args[0].Value,
                                null,
                                args[1].Value.TrimEdge(),
                                outArg.Value.TrimEdge());
                        }
                    case "ExpressionOperator":
                        if (outArg == null)
                        {
                            throw new WorkflowException("Found no out argument, please check your workflow has the correct format");
                        }
                        return CompressExpressionOperator(args, outArg);
                    case "Condition":
                        if (args[0].Value == "True" || args[0].Value == "False")
                        {
                            return new Condition(args[0].Value,
                            CompressActivity(activityReference.Properties[0], workflowName), CompressActivity(activityReference.Properties[1], workflowName));
                        }
                        return new Condition(args[0].Value.TrimEdge(),
                            CompressActivity(activityReference.Properties[0], workflowName), CompressActivity(activityReference.Properties[1], workflowName));
                    case "Wait":
                        if (args[0].Value == "True")
                        {
                            var list = new List<IWorkflowNode>();
                            list.Add(new WaitStart());
                            list.AddRange(CompressActivityCollection(activityReference.Properties, workflowName));
                            return new ActivityList(list.ToArray());
                        }
                        return new ActivityList(CompressActivityCollection(activityReference.Properties, workflowName).ToArray());

                    case "LogicalOperator":
                        return new LogicalComparison(args[0].Value, args[1].Value.TrimEdge(), args[2].Value.TrimEdge(), outArg.Value.TrimEdge());

                    case "Value":
                        return new ConvertType(args[0].Value.TrimEdge(), args[1].ReferenceLiteral.Value.Split(':')[1], outArg.Value.TrimEdge());

                    default:
                        throw new NotImplementedException($"No implementation for activityreference with key '{args[0].Key}'");
                }
            }

            return new ActivityList(CompressActivityCollection(activityReference.Properties, workflowName).ToArray());

        }

        private static string ParseCodeActivityArgument(string argument)
        {
            var directCastRegex = new Regex(@"[^(]+(?=,)");
            if (argument.Contains("DirectCast("))
            {
                return directCastRegex.Match(argument).Value;
            }
            return argument.TrimEdge();
        }

        private static IWorkflowNode CompressExpressionOperator(WorkflowParser.Argument[] args, WorkflowParser.Argument outArg)
        {
            switch (args[0].Value)
            {
                case "CreateCrmType":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Trim().Replace("\"", "")).ToArray());
                        if (args[2].ReferenceLiteral != null && args[2].ReferenceLiteral.Value != null)
                        {
                            return new CreateVariable(parameters.ToArray(), args[2].ReferenceLiteral.Value.Split(':')[1],
                                outArg.Value.TrimEdge());
                        }
                        else if (args[1].Value.Contains("EntityReference"))
                        {
                            return new CreateVariable(parameters.ToArray(), "EntityReference", outArg.Value.TrimEdge());
                        }
                        else
                        {
                            throw new NotImplementedException("Unknown amount of arguments in CreateCrmType, implement the required functionality.");
                        }
                    }

                case "SelectFirstNonNull":
                    {
                        var parameters = CompressParameters(args[1].Value).FirstOrDefault();
                        if (parameters == null)
                        {
                            throw new WorkflowException($"Incorrect format for parameters '{args[1].Value}'");
                        }
                        return new SelectFirstNonNull(
                            parameters.Split(',').Select(p => p.Trim()).ToArray(), outArg.Value.TrimEdge());
                    }

                case "Multiply":
                case "Divide":
                case "Subtract":
                case "Add":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Arithmetic(parameters.ToArray(), args[2].ReferenceLiteral.Value?.Split(':')[1],
                                outArg.Value.TrimEdge(), args[0].Value);

                    }
                case "Sum":
                case "Maximum":
                case "Minimum":
                case "Average":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Aggregate(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);

                    }
                case "Concat":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Concat(parameters.ToArray(), outArg.Value.TrimEdge());

                    }
                case "AddDays":
                case "AddHours":
                case "AddMonths":
                case "AddWeeks":
                case "AddYears":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new AddTime(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }
                case "SubtractDays":
                case "SubtractHours":
                case "SubtractMonths":
                case "SubtractWeeks":
                case "SubtractYears":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new SubtractTime(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }
                case "DiffInDays":
                case "DiffInHours":
                case "DiffInMinutes":
                case "DiffInMonths":
                case "DiffInWeeks":
                case "DiffInYears":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new DiffInTime(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }
                case "TrimLeft":
                case "TrimRight":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Trim(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }

                case "RetrieveLastExecutionTime":
                    {
                        return new RetrieveLastExecutionTime(outArg.Value.TrimEdge());
                    }
                case "Now":
                case "RetrieveCurrentTime":
                    {
                        return new RetrieveCurrentTime(outArg.Value.TrimEdge());
                    }
                case "CustomOperationArguments":
                    {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new CustomOperationArguments(parameters.ToArray(), outArg.Value.TrimEdge());
                    }
                default:
                    throw new NotImplementedException($"No implementation for expression operator '{args[0].Value}'");
            }
        }

        private static IEnumerable<string> CompressParameters(string value)
        {
            Regex reg = new Regex(@"\{(.+)\}|(,)");
            return reg.Matches(value).Cast<Match>().Select(m => m.Value.Trim('{', '}', ' '));
        }

        private static IWorkflowNode CompressSendEmail(WorkflowParser.SendEmail sendEmail)
            => new SendEmail(sendEmail.EntityId, sendEmail.DisplayName, sendEmail.Entity);

    }
}
