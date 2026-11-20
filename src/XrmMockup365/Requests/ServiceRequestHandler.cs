using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    // Shared service-backed operations for handlers that execute nested SDK requests.
    internal abstract class ServiceRequestHandler : RequestHandler
    {
        protected ServiceRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security, string requestName)
            : base(core, db, metadata, security, requestName) { }

        protected static void RequireId(Guid id, string name)
        {
            if (id == Guid.Empty) throw new FaultException(name + " must be a non-empty Guid.");
        }

        protected Entity Retrieve(EntityReference target, EntityReference caller)
        {
            if (target == null) throw new FaultException("An entity reference is required.");
            return ((RetrieveResponse)core.Execute(new RetrieveRequest { Target = target, ColumnSet = new ColumnSet(true) }, caller)).Entity;
        }

        protected EntityCollection RetrieveMultiple(QueryBase query, EntityReference caller)
            => ((RetrieveMultipleResponse)core.Execute(new RetrieveMultipleRequest { Query = query }, caller)).EntityCollection;
    }
}
