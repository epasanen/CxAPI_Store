using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static CxAPI_Store.CxConstant;
using System.Linq;
using System.Dynamic;
using Newtonsoft.Json;

namespace CxAPI_Store
{
    public class ScanSummary
    {
        public string ProjectName { get; set; }
        public string Team { get; set; }
        public string CustomFields { get; set; }
        public int StartHigh { get; set; }
        public int StartMedium { get; set; }
        public int StartLow { get; set; }
        public int StartTotal { get; set; }
        public int OpenHigh { get; set; }
        public int OpenMedium { get; set; }
        public int OpenLow { get; set; }
        public int OpenTotal { get; set; }
        public int ClosedHigh { get; set; }
        public int ClosedMedium { get; set; }
        public int ClosedLow { get; set; }
        public int ClosedTotal { get; set; }
//        public int ToVerify { get; set; }
//        public int NotExploitable { get; set; }
//       public int ProposedNotExploitable { get; set; }
//       public int Confirmed { get; set; }
//       public int Urgent { get; set; }
        public DateTime FirstScan { get; set; }
        public DateTime LastScan { get; set; }
        //public int ScanCount { get; set; }
    }
    public class ScanSummaryReport
    {

        private MakeReports makeReports;
        private resultClass token;
        private DataSet dataSet;
        private SQLiteMaster sqlite;
        public ScanSummaryReport(resultClass token, MakeReports makeReports)
        {
            this.token = token;
            this.makeReports = makeReports;
            this.sqlite = makeReports.sqllite();
        }
        public List<object> fetchReport()
        {

            // Use the  command line filters
            dataSet = makeReports.filterFromCommandLine();
            List<object> dynoList = new List<object>();

            //loop through projects, so
            var projectList = dataSet.Tables[ProjectTable].AsEnumerable().Select(p => new CxProject
            {
                ProjectId = p.Field<long>("ProjectId"),
                ProjectName = p.Field<string>("ProjectName"),
                TeamName = p.Field<string>("TeamName"),
                CustomFields = p.Field<string>("CustomFields")
            })
            .OrderBy(p => p.ProjectId);
            //strip off front of teams string

            foreach (CxProject pdr in projectList)
            {
                string teamName = pdr.TeamName.Replace('\\', '/'); // for < 9.0
                string[] split = teamName.Split('/');
                string customString = String.Empty;
                if (split.Length > 1)
                {
                    string group = split[split.Length -1];
                    string team = split[split.Length - 2];
                    teamName = String.Format("{0}/{1}", group, team);
                }
                if (!String.IsNullOrEmpty(pdr.CustomFields))
                {
                    string custom = pdr.CustomFields.Replace("}{", ",");
                    custom = custom.Replace("{[{", "");
                    custom = custom.Replace("}]}", "");

                    string[] customfields = custom.Split(',');
 /*                   foreach(var customfield in customfields)
                    {
                        Console.WriteLine(customfields);
                    }
 */
                    customString = custom;

                }
                ScanSummary scanSummary = new ScanSummary(); 
                
                var scanList = dataSet.Tables[ScanTable].AsEnumerable().OrderBy(o => o.Field<DateTime>("ScanFinished"))
                    .Where(p => p.Field<long>("ProjectId") == pdr.ProjectId)
                    .Select(n => new 
                {
                    ScanId = n.Field<long>("ScanId"),
                    High = n.Field<int>("High"),
                    Medium = n.Field<int>("Medium"),
                    Low = n.Field<int>("Low"),
                    Info = n.Field<int>("Info"),
                    ScanFinished = n.Field<DateTime>("ScanFinished")

                });
                var firstScan = scanList.FirstOrDefault();
                var lastScan = scanList.LastOrDefault();
                var scanCount = scanList.Count();
                long projectId = pdr.ProjectId;
                if (scanCount == 0)
                    continue;
                GetResultCounts firstCounts = new GetResultCounts(sqlite);
                var correctedFirst = (GetResultCounts)firstCounts.GetCorrectCounts(projectId, firstScan.ScanId);
                GetResultCounts lastCounts = new GetResultCounts(sqlite);
                var correctedLast = (GetResultCounts)lastCounts.GetCorrectCounts(projectId, lastScan.ScanId);
                //GetResultCounts adjCounts = new GetResultCounts(sqlite);
                //var adjustedLast = (GetResultCounts)adjCounts.GetCorrectCounts(projectId, lastScan.ScanId, "null");
                scanSummary.CustomFields = customString;
                scanSummary.ProjectName = pdr.ProjectName;
                scanSummary.Team = teamName;
                scanSummary.StartHigh = correctedFirst.Severity["High"];
                scanSummary.StartMedium = correctedFirst.Severity["Medium"];
                scanSummary.StartLow = correctedFirst.Severity["Medium"];
                scanSummary.StartTotal = scanSummary.StartHigh + scanSummary.StartMedium + scanSummary.StartLow;
                scanSummary.OpenHigh = correctedLast.Severity["High"];
                scanSummary.OpenMedium = correctedLast.Severity["Medium"];
                scanSummary.OpenLow = correctedLast.Severity["Low"];
                scanSummary.OpenTotal = scanSummary.OpenHigh + scanSummary.OpenMedium + scanSummary.OpenLow;
                //                scanSummary.NotExploitable = 0;
                //                scanSummary.Confirmed = 0;
                //                scanSummary.ToVerify = 0;
                //                scanSummary.Urgent = 0;
                //                scanSummary.ProposedNotExploitable = 0;
                int diffhigh = scanSummary.StartHigh - scanSummary.OpenHigh;
                int diffmed = scanSummary.StartMedium - scanSummary.OpenMedium;
                int difflow = scanSummary.StartLow - scanSummary.OpenLow;
                scanSummary.ClosedHigh = diffhigh;
                scanSummary.ClosedMedium = diffmed;
                scanSummary.ClosedLow = difflow;
                scanSummary.ClosedTotal = diffhigh + diffmed + difflow;
                scanSummary.FirstScan = firstScan.ScanFinished;
                scanSummary.LastScan = lastScan.ScanFinished;
               // scanSummary.ScanCount = scanCount;
//                scanSummary.ProposedNotExploitable = adjustedLast.State["Proposed Not Exploitable"];
//                scanSummary.NotExploitable = adjustedLast.State["Not Exploitable"];
//                scanSummary.ToVerify = adjustedLast.State["To Verify"];
//                scanSummary.Confirmed = adjustedLast.State["Confirmed"];
//                scanSummary.Urgent = adjustedLast.State["Urgent"];
              

                dynoList.Add(scanSummary);
            }

            if (token.debug && token.verbosity > 1)
            {
                foreach (dynamic csv in dynoList)
                {
                   // Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}", csv.ProjectName, csv.Team, csv.LastHigh, csv.LastMedium, csv.LastLow, csv.NewHigh, csv.NewMedium, csv.NewLow, csv.DiffHigh, csv.DiffMedium, csv.DiffLow, csv.NotExploitable, csv.Confirmed, csv.ToVerify, csv.Urgent, csv.correctedFirst, csv.LastScan, csv.ScanCount);
                }
            }

            return dynoList;
        }
        public void Dispose()
        {

        }

    }

}




