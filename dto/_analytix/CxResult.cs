using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store.dto
{
    public partial class CxResult
    {
        public string FalsePositive { get; set; }
        public string NodeCodeSnippet { get; set; }
        public long NodeColumn { get; set; }
        public string NodeFileName { get; set; }
        public long NodeId { get; set; }
        public long NodeLength { get; set; }
        public long NodeLine { get; set; }
        public string NodeName { get; set; }
        public string NodeType { get; set; }
        public long PathId { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public object QueryCategories { get; set; }
        public long QueryCweId { get; set; }
        public string QueryGroup { get; set; }
        public long QueryId { get; set; }
        public string QueryLanguage { get; set; }
        public string QueryName { get; set; }
        public string QuerySeverity { get; set; }
        public string QueryVersionCode { get; set; }
        public string Remark { get; set; }
        public Uri ResultDeepLink { get; set; }
        public long ResultId { get; set; }
        public string ResultSeverity { get; set; }
        public DateTimeOffset ScanFinished { get; set; }
        public long ScanId { get; set; }
        public string ScanProduct { get; set; }
        public string ScanType { get; set; }
        public long SimilarityId { get; set; }
        public long SinkColumn { get; set; }
        public string SinkFileName { get; set; }
        public long SinkLine { get; set; }
        public long State { get; set; }
        public string Status { get; set; }
        public string TeamName { get; set; }
        public string VulnerabilityId { get; set; }
    }

}
