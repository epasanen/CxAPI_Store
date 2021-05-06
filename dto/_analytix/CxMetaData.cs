using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store.dto
{
    public partial class CxMetaData
    {
        public string TenantName { get; set; }
        public string ArchivalFilePath { get; set; }
        public string FileName { get; set; }
        public DateTime LastRunDate { get; set; }
  
    }
}
