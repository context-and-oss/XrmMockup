using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Serialization
{
    public class TableRowDTO
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public long RowVersion { get; set; }
        public Dictionary<string, TableColumnDTO> Columns { get; set; }
    }
}
