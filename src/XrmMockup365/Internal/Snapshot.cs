using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup.Internal
{
    internal class Snapshot
    {
        public XrmDb db;
        public Security security;
        public EntityReference AdminUserRef;
        public EntityReference RootBusinessUnitRef;
        public Guid OrganizationId;
        public TimeSpan TimeOffset;
    }
}
