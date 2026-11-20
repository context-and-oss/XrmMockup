using System;

namespace DG.Tools.XrmMockup.Serialization
{
    public class SnapshotDTO
    {
        public DbDTO Db { get; set; }
        public SecurityModelDTO Security { get; set; }
        public Guid AdminUserId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid RootBusinessUnitId { get; set; }
        public long TimeOffset { get; set; }
    }
}
