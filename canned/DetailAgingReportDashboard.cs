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
    public partial class AgingOutputDashboard
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string opened { get; set; }
        public string closed { get; set; }
        public string modified { get; set; }
        public string status { get; set; }
        public string workflowStage { get; set; }
        public string Class { get; set; }
        public string assetUrl { get; set; }
        public string rating { get; set; }
        public string description { get; set; }
        public string solution { get; set; }
        public string cvssScore { get; set; }
        public string cve { get; set; }
        public string firstFound { get; set; }
        public string priority{ get; set; }
        public string treatmentAdded { get; set; }
        public string application { get; set; }
        public string technicalOwner { get; set; }
        public string internetFacing { get; set; }
        public string daysToFix { get; set; }

        public AgingOutputDashboard()
        {
            this.treatmentAdded = "Null";
            this.internetFacing = "Null";
            this.daysToFix = "Null";
            this.cvssScore = "Null";
            this.Url = "Null";
        }

    }
    public class DetailAgingReportDashboard
    {

        private MakeReports makeReports;
        private resultClass token;
        private DataSet dataSet;
        private SQLiteMaster sqlite;
        public DetailAgingReportDashboard(resultClass token, MakeReports makeReports)
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
                    AgingOutputDashboard agingOutput = new AgingOutputDashboard();
                    Vulnerability vulnerability = getResult.vulnerability[key];

                    agingOutput.status = vulnerability.VulnerabilityStatus.Contains("Closed") || vulnerability.VulnerabilityStatus.Contains("Fixed") ? "Closed" : "Open";
                    agingOutput.Id = String.Format("{0}_{1}", vulnerability.SimilarityId, vulnerability.FileNameHash);
                    agingOutput.Url = "Null";
                    agingOutput.opened = agingOutput.status.Contains("Open") ? vulnerability.firstScan.ToString("yyyy-MM-dd") : "Null";
                    agingOutput.closed = agingOutput.status.Contains("Closed") ? vulnerability.firstScan.ToString("yyyy-MM-dd") : "Null";
                    agingOutput.modified = vulnerability.ChangeDate.ToString("yyyy-MM-dd").Contains("0001") ? "Null" : vulnerability.ChangeDate.ToString("yyyy-MM-dd");
                    agingOutput.workflowStage = StateDescription((int)vulnerability.State).Contains("Proposed") ? "Proposed" : "None";
                    agingOutput.Class = vulnerability.QueryName;
                    agingOutput.assetUrl = TrimFileName(vulnerability.NodeFileName);
                    agingOutput.rating = StateDescription((int)vulnerability.State);
                    agingOutput.description = vulnerability.DeepLink;
                    agingOutput.solution = vulnerability.ChangeEvent;
                    agingOutput.cve = vulnerability.QueryCweId.ToString();
                    agingOutput.firstFound = vulnerability.firstScan.ToString("yyyy-MM-dd");
                    agingOutput.priority = vulnerability.Severity;
                    agingOutput.application = pdr.ProjectName;
                    agingOutput.technicalOwner = pdr.TeamName;
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
                return "Proposed Not Exploitable";
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

