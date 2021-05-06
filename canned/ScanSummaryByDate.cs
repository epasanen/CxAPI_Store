using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static CxAPI_Store.CxConstant;
using System.Linq;
using System.Dynamic;

namespace CxAPI_Store
{
    public class ScanSummaryObject
    {
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Team { get; set; }
        public int LastHigh { get; set; }
        public int LastMedium { get; set; }
        public int LastLow { get; set; }
        public int LastInfo { get; set; }
        public int NewHigh { get; set; }
        public int NewMedium { get; set; }
        public int NewLow { get; set; }
        public int NewInfo { get; set; }
        public int DiffHigh { get; set; }
        public int DiffMedium { get; set; }
        public int DiffLow { get; set; }
        public int DiffInfo { get; set; }
        public int NotExploitable { get; set; }
        public int Confirmed { get; set; }
        public int ToVerify { get; set; }
        public int Urgent { get; set; }
        public int ProposedNotExploitable { get; set; }
        public DateTime FirstScan { get; set; }
        public DateTime LastScan { get; set; }
        public int ScanCount { get; set; }
    }
    public class ScanSummaryByDate
    {

        private MakeReports makeReports;
        private resultClass token;
        private DataSet dataSet;
        private SQLiteMaster sqlite;
        public ScanSummaryByDate(resultClass token, MakeReports makeReports)
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
            })
            .OrderBy(p => p.ProjectId);

            foreach (CxProject pdr in projectList)
            {

                ScanSummaryObject scanSummary = new ScanSummaryObject();

                var scanList = dataSet.Tables[ScanTable].AsEnumerable().OrderBy(o => o.Field<DateTime>("ScanFinished")).Where(p => p.Field<long>("ProjectId") == pdr.ProjectId).Select(n => new 
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

                scanSummary.ProjectName = pdr.ProjectName;
                scanSummary.Team = pdr.TeamName;
                scanSummary.LastHigh = firstScan.High;
                scanSummary.LastMedium = firstScan.Medium;
                scanSummary.LastLow = firstScan.Low;
                scanSummary.LastInfo = firstScan.Info;
                scanSummary.NewHigh = lastScan.High;
                scanSummary.NewMedium = lastScan.Medium;
                scanSummary.NewLow = lastScan.Low;
                scanSummary.NewInfo = lastScan.Info;
                scanSummary.DiffHigh = firstScan.High - lastScan.High;
                scanSummary.DiffMedium = firstScan.Medium - lastScan.Medium;
                scanSummary.DiffLow = firstScan.Low - lastScan.Low;
                scanSummary.DiffInfo = firstScan.Info - lastScan.Info;
                scanSummary.NotExploitable = 0;
                scanSummary.Confirmed = 0;
                scanSummary.ToVerify = 0;
                scanSummary.Urgent = 0;
                scanSummary.ProposedNotExploitable = 0;
                scanSummary.FirstScan = firstScan.ScanFinished;
                scanSummary.LastScan = lastScan.ScanFinished;
                scanSummary.ScanCount = scanCount;


                DataTable last = sqlite.SelectIntoDataTable(dataSet.Tables[ResultTable], "LastResult", String.Format("and ProjectId = {0} and ScanId = {1}", projectId, lastScan.ScanId));
 
                var resultList = last.AsEnumerable().Select(n => new
                {
                    State = n.Field<long>("State")
                });
                foreach (var rdr in resultList)
                {

                    if (rdr.State == 0)
                        scanSummary.ToVerify++;
                    else if (rdr.State == 1)
                        scanSummary.NotExploitable++;
                    else if (rdr.State == 2)
                        scanSummary.Confirmed++;
                    else if (rdr.State == 3)
                        scanSummary.Urgent++;
                    else if (rdr.State == 4)
                        scanSummary.ProposedNotExploitable++;
                }

                dynoList.Add(scanSummary);
            }

            if (token.debug && token.verbosity > 1)
            {
                foreach (dynamic csv in dynoList)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}", csv.ProjectName, csv.Team, csv.LastHigh, csv.LastMedium, csv.LastLow, csv.NewHigh, csv.NewMedium, csv.NewLow, csv.DiffHigh, csv.DiffMedium, csv.DiffLow, csv.NotExploitable, csv.Confirmed, csv.ToVerify, csv.Urgent, csv.FirstScan, csv.LastScan, csv.ScanCount);
                }
            }

            return dynoList;
        }
        public void Dispose()
        {

        }

    }

}




