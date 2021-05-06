using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store.dto
{
    public partial class CxResult
    {
        public string FalsePositive { get; set; }
        public string NodeCodeSnippet { get; set; }
        public int NodeColumn { get; set; }
        public string NodeFileName { get; set; }
        public long NodeId { get; set; }
        public int NodeLength { get; set; }
        public int NodeLine { get; set; }
        public string NodeName { get; set; }
        public string NodeType { get; set; }
        public long PathId { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string QueryCategories { get; set; }
        public long QueryCweId { get; set; }
        public string QueryGroup { get; set; }
        public long QueryId { get; set; }
        public string QueryLanguage { get; set; }
        public string QueryName { get; set; }
        public string QuerySeverity { get; set; }
        public string QueryVersionCode { get; set; }
        public string Remark { get; set; }
        public string ResultDeepLink { get; set; }
        public long ResultId { get; set; }
        public string ResultSeverity { get; set; }
        public DateTime ScanFinished { get; set; }
        public long ScanId { get; set; }
        public string ScanProduct { get; set; }
        public string ScanType { get; set; }
        public long SimilarityId { get; set; }
        public int SinkColumn { get; set; }
        public string SinkFileName { get; set; }
        public int SinkLine { get; set; }
        public int State { get; set; }
        public string Status { get; set; }
        public string TeamName { get; set; }
        public long VulnerabilityId { get; set; }
        //public DateTime DetectionDate { get; set; }
        public long FileNameHash { get; set; }

        public void GenerateFileHash()
        {
            //Int64 hashCode = 0;
            if (!string.IsNullOrEmpty(NodeFileName))
            {
                //Unicode Encode Covering all characterset
                byte[] byteContents = Encoding.Unicode.GetBytes(NodeFileName);
                System.Security.Cryptography.SHA256 hash =
                new System.Security.Cryptography.SHA256CryptoServiceProvider();
                byte[] hashText = hash.ComputeHash(byteContents);
                Int64 hashCodeStart = BitConverter.ToInt64(hashText, 0);
                Int64 hashCodeMedium = BitConverter.ToInt64(hashText, 8);
                Int64 hashCodeEnd = BitConverter.ToInt64(hashText, 24);
                FileNameHash = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
            }
        }
    }

}
