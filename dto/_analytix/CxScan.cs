using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store.dto
{
    public partial class CxScan
    {
        public string CxVersion { get; set; }
        public string DeepLink { get; set; }
        public DateTimeOffset EngineFinished { get; set; }
        public DateTimeOffset EngineStart { get; set; }
        public int FailedLinesOfCode { get; set; }
        public int FileCount { get; set; }
        public long Information { get; set; }
        public string Initiator { get; set; }
        public string Languages { get; set; }
        public long LinesOfCode { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Info { get; set; }
        public string Preset { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTimeOffset ReportCreationTime { get; set; }
        public string ScanComments { get; set; }
        public DateTimeOffset ScanStarted{ get; set; }
        public DateTimeOffset ScanFinished { get; set; }
        public long ScanId { get; set; }
        public string ScanProduct { get; set; }
        public long ScanRisk { get; set; }
        public long ScanRiskSeverity { get; set; }
        public DateTime ScanStart { get; set; }
        public string ScanTime { get; set; }
        public string ScanType { get; set; }
        public string SourceOrigin { get; set; }
        public string TeamName { get; set; }
    }
}
