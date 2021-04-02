using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store.dto
{
    public partial class CxProject
    {
        public Dictionary<string, object> CustomFields { get; set; }
        public Dictionary<string, object> Policies { get; set; }
        public string Preset { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime SastLastScanDate { get; set; }
        public long SastScans { get; set; }
        public string TeamName { get; set; }

 
    }
}
