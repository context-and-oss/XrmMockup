using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class SetAttributeValue : IWorkflowNode
    {
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }

        public SetAttributeValue(string VariableId, string EntityName)
        {
            this.VariableId = VariableId;
            this.EntityName = EntityName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var relatedlinked = "relatedlinked";
            if (VariableId.Contains(relatedlinked))
            {
                var reg = new Regex(@"\([^#]*\#");
                variables[relatedlinked + "_" + EntityName] = reg.Match(VariableId).Value.Replace("\"", "").Replace(relatedlinked + "_", "").TrimEdge();
                return;
            }

            if (!variables.ContainsKey(VariableId))
            {
                throw new WorkflowException($"The variable with id '{VariableId}' before being set, check the workflow has the correct format.");
            }

            // When evaluating a calculated/formula field during a read, persistence is suppressed: the
            // computed value already lives in the "primaryEntity" variable for the caller to project onto
            // the returned entity, so re-Updating the whole record would be a spurious write (firing the
            // update pipeline, mutating data, and tripping the update-time circular-reference guard).
            // Real workflows leave this flag unset and still persist.
            if (variables.TryGetValue(WorkflowTree.SuppressWritesKey, out var suppress) && suppress is bool b && b)
            {
                return;
            }

            var entity = variables[VariableId] as Entity;
            orgService.Update(entity);

        }
    }
}
