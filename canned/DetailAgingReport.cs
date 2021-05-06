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
    public partial class AgingOutput
    {
        public string ProjectName { get; set; }
        public string Team { get; set; }
        public string PresetName { get; set; }
        public string Query { get; set; }
        public string QueryLanguage { get; set; }
        public long similarityId { get; set; }
        public string isFalsePositive { get; set; }
        //public int startState { get; set; }
        public string StateDesc { get; set; }
        public string Status { get; set; }
        public string Severity { get; set; }
        //public int endState { get; set; }
        //public string endStateDesc { get; set; }
        //public string endStatus { get; set; }
        //public string endSeverity { get; set; }
        public int lineNo { get; set; }
        public int column { get; set; }
        //public string firstLine { get; set; }
        public string fileName { get; set; }
        public string deepLink { get; set; }
        public string remark { get; set; }
        //public DateTime scanDate { get; set; }
        public DateTimeOffset firstScan { get; set; }
        public DateTimeOffset lastScan { get; set; }
        public int age { get; set; }
        public int scanCount { get; set; }
    }
    public class DetailAgingReport
    {

        private MakeReports makeReports;
        private resultClass token;
        private DataSet dataSet;
        private SQLiteMaster sqlite;
        public DetailAgingReport(resultClass token, MakeReports makeReports)
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
                Preset = p.Field<string>("Preset")
            });

            foreach (CxProject pdr in projectList)
            {

                GetResultAging getResult = new GetResultAging(sqlite, token);
                getResult.GetResultAgingbySimilarityId(pdr.ProjectId);

                long projectId = pdr.ProjectId;

                foreach (string key in getResult.vulnerability.Keys)
                {
                    AgingOutput agingOutput = new AgingOutput();
                    Vulnerability vulnerability = getResult.vulnerability[key];

                    agingOutput.ProjectName = pdr.ProjectName;
                    agingOutput.Team = pdr.TeamName;
                    agingOutput.PresetName = pdr.Preset;
                    agingOutput.remark = vulnerability.Remark;
                    agingOutput.age = vulnerability.Age;
                    agingOutput.Severity = vulnerability.Severity;
                    agingOutput.lineNo = (int)vulnerability.NodeLine;
                    agingOutput.column = (int)vulnerability.NodeColumn;
                    agingOutput.deepLink = vulnerability.DeepLink;
                    agingOutput.Status = vulnerability.VulnerabilityStatus;
                    agingOutput.isFalsePositive = vulnerability.isFalsePositive ? "True" : "False";
                    agingOutput.fileName = TrimFileName(vulnerability.NodeFileName);
                    agingOutput.StateDesc = StateDescription((int)vulnerability.State);
                    agingOutput.Query = vulnerability.QueryName;
                    agingOutput.QueryLanguage = vulnerability.QueryLanguage;
                    agingOutput.similarityId = vulnerability.SimilarityId;
                    agingOutput.firstScan = vulnerability.firstScan;
                    agingOutput.lastScan = vulnerability.lastScan;
                    agingOutput.scanCount = vulnerability.ScanCount;
                    dynoList.Add(agingOutput);

                }
            }


            if (token.debug && token.verbosity > 1)
            {
                foreach (AgingOutput csv in dynoList)
                {
                    //Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}", csv.ProjectName, csv.Team, csv.LastHigh, csv.LastMedium, csv.LastLow, csv.NewHigh, csv.NewMedium, csv.NewLow, csv.DiffHigh, csv.DiffMedium, csv.DiffLow, csv.NotExploitable, csv.Confirmed, csv.ToVerify, csv.Urgent, csv.FirstScan, csv.LastScan, csv.ScanCount);
                }
            }

            return dynoList;
        }

        private string StateDescription(int state)
        {
            if (state == 0)
                return "To Verify";
            else if (state == 1)
                return "Not Exploitable";
            else if (state == 2)
                return "Confirmed";
            else if (state == 3)
                return "Urgent";
            else if (state == 4)
                return "ProposedNotExploitable";
            return "";
        }
        private string TrimFileName(string fileName)
        {
            try
            {
                int found = fileName.LastIndexOf(@"\");
                if (found > 0)
                {
                    return fileName.Substring(found + 1);
                }
                found = fileName.LastIndexOf(@"/");
                if (found > 0)
                {
                    return fileName.Substring(found + 1);
                }
                return String.Empty;
            }
            catch
            {
                return String.Empty;
            }
        }
    
        public void Dispose()
        {

        }

    }

}

