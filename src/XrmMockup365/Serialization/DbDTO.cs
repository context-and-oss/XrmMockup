using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Serialization
{
    public class DbDTO
    {
        public long RowVersion { get; set; }
        public Dictionary<string, TableDTO> Tables { get; set; }
    }
}
