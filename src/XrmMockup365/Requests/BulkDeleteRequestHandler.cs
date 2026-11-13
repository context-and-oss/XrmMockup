using System;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class BulkDeleteRequestHandler : ServiceRequestHandler
    {
        internal BulkDeleteRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "BulkDelete") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<BulkDeleteRequest>(orgRequest);
            if (string.IsNullOrWhiteSpace(request.JobName) || request.QuerySet == null || request.QuerySet.Length == 0 || request.QuerySet.Any(q => q == null))
                throw new FaultException("JobName and a non-empty QuerySet are required.");
            if (request.ToRecipients == null || request.CCRecipients == null) throw new FaultException("ToRecipients and CCRecipients are required.");
            // Like workflows, one-off jobs run synchronously in the local simulator. Never silently run a scheduled job early.
            if (!string.IsNullOrWhiteSpace(request.RecurrencePattern) || request.StartDateTime.ToUniversalTime() > DateTime.UtcNow.Add(core.TimeOffset))
                throw new NotSupportedException("Scheduled or recurring bulk deletion is not supported; use an immediate one-off job.");
            if (request.SendEmailNotification) throw new NotSupportedException("Bulk deletion email notifications are not supported.");
            var records = new System.Collections.Generic.Dictionary<EntityReference, Entity>();
            foreach (var original in request.QuerySet)
            {
                var query = CloneQuery(original);
                query.PageInfo = new PagingInfo { Count = query.TopCount.HasValue ? 0 : 5000, PageNumber = 1 };
                EntityCollection page;
                do
                {
                    page = RetrieveMultiple(query, userRef);
                    foreach (var record in page.Entities) records[record.ToEntityReference()] = record;
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = page.PagingCookie;
                } while (page.MoreRecords);
            }
            // Check all delete rights before making changes, including when queries overlap.
            foreach (var record in records.Values)
                if (!security.HasPermission(record, AccessRights.DeleteAccess, userRef))
                    throw new FaultException("The caller does not have delete access to all matching records.");
            var snapshot = core.TakeJsonSnapshot();
            try
            {
                var now = DateTime.UtcNow.Add(core.TimeOffset);
                var job = new Entity("asyncoperation")
                {
                    ["name"] = request.JobName, ["operationtype"] = new OptionSetValue(13),
                    ["statecode"] = new OptionSetValue(0), ["statuscode"] = new OptionSetValue(0)
                };
                var id = ((CreateResponse)core.Execute(new CreateRequest { Target = job }, userRef)).id;
                foreach (var record in records.Keys)
                    if (db.GetEntityOrNull(record) != null)
                    {
                        core.Execute(new DeleteRequest { Target = record }, userRef);
                        if (db.GetEntityOrNull(record) != null)
                            throw new FaultException("A matching record could not be deleted (for example, due to a restricted relationship).");
                    }
                // Job bookkeeping is system-managed and is not subject to caller field writability.
                var storedJob = db.GetEntity("asyncoperation", id);
                storedJob["operationtype"] = new OptionSetValue(13);
                storedJob["statecode"] = new OptionSetValue(3);
                storedJob["statuscode"] = new OptionSetValue(30);
                if (storedJob.Attributes.Contains("startedon") || metadata.EntityMetadata["asyncoperation"].Attributes.Any(a => a.LogicalName == "startedon")) storedJob["startedon"] = now;
                if (metadata.EntityMetadata["asyncoperation"].Attributes.Any(a => a.LogicalName == "completedon")) storedJob["completedon"] = DateTime.UtcNow.Add(core.TimeOffset);
                db.Update(storedJob);
                var response = new BulkDeleteResponse();
                response.Results["JobId"] = id;
                return response;
            }
            catch
            {
                core.RestoreJsonSnapshot(snapshot);
                throw;
            }
        }

        private static QueryExpression CloneQuery(QueryExpression query)
        {
            var serializer = new DataContractSerializer(typeof(QueryExpression));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, query);
                stream.Position = 0;
                return (QueryExpression)serializer.ReadObject(stream);
            }
        }
    }
}
