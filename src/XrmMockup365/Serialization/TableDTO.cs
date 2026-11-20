using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Serialization
{
    public class TableDTO
    {
        public string Name { get; set; }
        public Dictionary<Guid, TableRowDTO> Rows { get; set; }
    }
}
