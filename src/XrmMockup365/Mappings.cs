using DG.Tools.XrmMockup.Internal;
using XrmPluginCore.Enums;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG.Tools.XrmMockup {

    internal static class Mappings
    {
        public static Dictionary<Type, string> EntityImageProperty = new Dictionary<Type, string>()
        {
            { typeof(AssignRequest), nameof(AssignRequest.Target) },
            { typeof(AssociateRequest), nameof(AssociateRequest.Target) },
            { typeof(CreateMultipleRequest), nameof(CreateMultipleRequest.Targets) },
            { typeof(DeleteMultipleRequest), nameof(DeleteMultipleRequest.Targets) },
            { typeof(CreateRequest), nameof(CreateRequest.Target) },
            { typeof(DeleteRequest), nameof(DeleteRequest.Target) },
            { typeof(DeliverIncomingEmailRequest), nameof(DeliverIncomingEmailRequest.MessageId) },
            { typeof(DeliverPromoteEmailRequest), nameof(DeliverPromoteEmailRequest.EmailId) },
            { typeof(DisassociateRequest), nameof(DisassociateRequest.Target) },
            { typeof(ExecuteWorkflowRequest), nameof(ExecuteWorkflowRequest.EntityId) },
            { typeof(MergeRequest), nameof(MergeRequest.Target) },
            { typeof(RetrieveRequest), nameof(RetrieveRequest.Target) },
            { typeof(SendEmailRequest), nameof(SendEmailRequest.EmailId) },
            { typeof(SetStateRequest), nameof(SetStateRequest.EntityMoniker) },
            { typeof(UpdateMultipleRequest), nameof(UpdateMultipleRequest.Targets) },
            { typeof(UpdateRequest), nameof(UpdateRequest.Target) },
            { typeof(UpsertMultipleRequest), nameof(UpsertMultipleRequest.Targets) },
            { typeof(UpsertRequest), nameof(UpsertRequest.Target) },
        };

        public static Dictionary<Type, EventOperation?> RequestToEventOperation = new Dictionary<Type, EventOperation?>()
        {
            { typeof(AssignRequest), EventOperation.Assign },
            { typeof(AssociateRequest), EventOperation.Associate },
            { typeof(CreateRequest), EventOperation.Create },
            { typeof(DeleteRequest), EventOperation.Delete },
            { typeof(DisassociateRequest), EventOperation.Disassociate },
            { typeof(GrantAccessRequest), EventOperation.GrantAccess },
            { typeof(MergeRequest), EventOperation.Merge },
            { typeof(ModifyAccessRequest), EventOperation.ModifyAccess },
            { typeof(RetrieveRequest), EventOperation.Retrieve },
            { typeof(RetrieveMultipleRequest), EventOperation.RetrieveMultiple },
            { typeof(RetrievePrincipalAccessRequest), EventOperation.RetrievePrincipalAccess },
            //{ typeof(RetrieveSharedPrincipalAccessRequest), EventOperation.RetrieveSharedPrincipalAccess }, // No such request
            { typeof(RevokeAccessRequest), EventOperation.RevokeAccess },
            { typeof(SetStateRequest), EventOperation.SetState },
            //{ typeof(SetStateDynamicEntityRequest), EventOperation.SetStateDynamicEntity }, // No such request
            { typeof(UpdateRequest), EventOperation.Update },
            { typeof(UpsertRequest), EventOperation.Upsert },
            { typeof(WinOpportunityRequest), EventOperation.Win },
            { typeof(LoseOpportunityRequest), EventOperation.Lose },
            { typeof(WinQuoteRequest), EventOperation.Win },
            { typeof(CloseQuoteRequest), EventOperation.Lose },
            { typeof(CloseIncidentRequest), EventOperation.Close },
            { typeof(CreateMultipleRequest), EventOperation.CreateMultiple },
            { typeof(UpdateMultipleRequest), EventOperation.UpdateMultiple },
            { typeof(UpsertMultipleRequest), EventOperation.UpsertMultiple }
        };

        public static Dictionary<EventOperation, EventOperation> RequestToMultipleRequest = new Dictionary<EventOperation, EventOperation>()
        {
            { EventOperation.Create, EventOperation.CreateMultiple },
            { EventOperation.Update, EventOperation.UpdateMultiple },
            { EventOperation.Upsert, EventOperation.UpsertMultiple }
        };

        public static EventOperation? SingleOperationFromMultiple(EventOperation operation)
        {
            if (!RequestToMultipleRequest.ContainsValue(operation)) return null;
            return RequestToMultipleRequest.First(x => x.Value == operation).Key;
        }

        public static Type EventOperationToRequest(EventOperation operation)
        {
            if (!RequestToEventOperation.ContainsValue(operation)) return null;
            return RequestToEventOperation.First(x => x.Value == operation).Key;
        }

        public static readonly Dictionary<string, ConditionOperator> ConditionalOperator = new Dictionary<string, ConditionOperator>
        {
            { "begins-with", ConditionOperator.BeginsWith },
            { "between", ConditionOperator.Between },
            { "ends-with", ConditionOperator.EndsWith },
            { "eq", ConditionOperator.Equal },
            { "eq-businessid", ConditionOperator.EqualBusinessId },
            { "eq-userid", ConditionOperator.EqualUserId },
            { "eq-userteams", ConditionOperator.EqualUserTeams },
            { "ge", ConditionOperator.GreaterEqual },
            { "gt", ConditionOperator.GreaterThan },
            { "in", ConditionOperator.In },
            { "in-fiscal-period", ConditionOperator.InFiscalPeriod },
            { "in-fiscal-period-and-year", ConditionOperator.InFiscalPeriodAndYear },
            { "in-fiscal-year", ConditionOperator.InFiscalYear },
            { "in-or-after-fiscal-period-and-year", ConditionOperator.InOrAfterFiscalPeriodAndYear },
            { "in-or-before-fiscal-period-and-year", ConditionOperator.InOrBeforeFiscalPeriodAndYear },
            { "last-seven-days", ConditionOperator.Last7Days },
            { "last-fiscal-period", ConditionOperator.LastFiscalPeriod },
            { "last-fiscal-year", ConditionOperator.LastFiscalYear },
            { "last-month", ConditionOperator.LastMonth },
            { "last-week", ConditionOperator.LastWeek },
            { "last-x-days", ConditionOperator.LastXDays },
            { "last-x-fiscal-periods", ConditionOperator.LastXFiscalPeriods },
            { "last-x-fiscal-years", ConditionOperator.LastXFiscalYears },
            { "last-x-hours", ConditionOperator.LastXHours },
            { "last-x-months", ConditionOperator.LastXMonths },
            { "last-x-weeks", ConditionOperator.LastXWeeks },
            { "last-x-years", ConditionOperator.LastXYears },
            { "last-year", ConditionOperator.LastYear },
            { "le", ConditionOperator.LessEqual },
            { "like", ConditionOperator.Like },
            { "lt", ConditionOperator.LessThan },
            { "ne", ConditionOperator.NotEqual },
            { "ne-businessid", ConditionOperator.NotEqualBusinessId },
            { "ne-userid", ConditionOperator.NotEqualUserId },
            { "next-fiscal-period", ConditionOperator.NextFiscalPeriod },
            { "next-fiscal-year", ConditionOperator.NextFiscalYear },
            { "next-month", ConditionOperator.NextMonth },
            { "next-seven-days", ConditionOperator.Next7Days },
            { "next-week", ConditionOperator.NextWeek },
            { "next-x-days", ConditionOperator.NextXDays },
            { "next-x-fiscal-periods", ConditionOperator.NextXFiscalPeriods },
            { "next-x-fiscal-years", ConditionOperator.NextXFiscalYears },
            { "next-x-hours", ConditionOperator.NextXHours },
            { "next-x-months", ConditionOperator.NextXMonths },
            { "next-x-weeks", ConditionOperator.NextXWeeks },
            { "next-x-years", ConditionOperator.NextXYears },
            { "next-year", ConditionOperator.NextYear },
            { "not-between", ConditionOperator.NotBetween },
            { "not-in", ConditionOperator.NotIn },
            { "not-null", ConditionOperator.NotNull },
            { "null", ConditionOperator.Null },
            { "olderthan-x-days", ConditionOperator.OlderThanXDays },
            { "olderthan-x-hours", ConditionOperator.OlderThanXHours },
            { "olderthan-x-minutes", ConditionOperator.OlderThanXMinutes },
            { "olderthan-x-months", ConditionOperator.OlderThanXMonths },
            { "olderthan-x-weeks", ConditionOperator.OlderThanXWeeks },
            { "olderthan-x-years", ConditionOperator.OlderThanXYears },
            { "on", ConditionOperator.On },
            { "on-or-after", ConditionOperator.OnOrAfter },
            { "on-or-before", ConditionOperator.OnOrBefore },
            { "this-fiscal-period", ConditionOperator.ThisFiscalPeriod },
            { "this-fiscal-year", ConditionOperator.ThisFiscalYear },
            { "this-month", ConditionOperator.ThisMonth },
            { "this-week", ConditionOperator.ThisWeek },
            { "this-year", ConditionOperator.ThisYear },
            { "today", ConditionOperator.Today },
            { "tomorrow", ConditionOperator.Tomorrow },
            { "yesterday", ConditionOperator.Yesterday }
        };

        public static EntityReference GetPrimaryEntityReferenceFromRequest(OrganizationRequest request)
        {
            switch (request)
            {
                case RetrieveRequest retrieveRequest:
                    return retrieveRequest.Target;
                case CreateRequest createRequest:
                    return createRequest.Target.ToEntityReferenceWithKeyAttributes();
                case UpdateRequest updateRequest:
                    return updateRequest.Target.ToEntityReferenceWithKeyAttributes();
                case UpsertRequest upsertRequest:
                    return upsertRequest.Target.ToEntityReferenceWithKeyAttributes();
                case DeleteRequest deleteRequest:
                    return deleteRequest.Target;
                case SetStateRequest setStateRequest:
                    return setStateRequest.EntityMoniker;
                case AssignRequest assignRequest:
                    return assignRequest.Target;
                case AssociateRequest associateRequest:
                    return associateRequest.Target;
                case DisassociateRequest disassociateRequest:
                    return disassociateRequest.Target;
                case MergeRequest mergeRequest:
                    return mergeRequest.Target;
                case WinOpportunityRequest winOpportunityRequest:
                    return winOpportunityRequest.OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");
                case LoseOpportunityRequest loseOpportunityRequest:
                    return loseOpportunityRequest.OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");
                case WinQuoteRequest winQuoteRequest:
                    return winQuoteRequest.QuoteClose.GetAttributeValue<EntityReference>("quoteid");
                case CloseQuoteRequest closeQuoteRequest:
                    return closeQuoteRequest.QuoteClose.GetAttributeValue<EntityReference>("quoteid");
                case RetrieveMultipleRequest retrieveMultipleRequest:
                    return GetPrimaryEntityReferenceFromQuery(retrieveMultipleRequest.Query);
                case CloseIncidentRequest closeIncidentRequest:
                    return closeIncidentRequest.IncidentResolution?.GetAttributeValue<EntityReference>("incidentid");
            }

            return null;
        }

        public static EntityReference GetPrimaryEntityReferenceFromQuery(QueryBase query)
        {
            switch (query)
            {
                case FetchExpression fe: return new EntityReference(XmlHandling.FetchXmlToQueryExpression(fe.Query).EntityName, Guid.Empty);
                case QueryExpression qe: return new EntityReference(qe.EntityName, Guid.Empty);
                case QueryByAttribute qba: return new EntityReference(qba.EntityName, Guid.Empty);
                default: return null;
            }
        }
    }
}
