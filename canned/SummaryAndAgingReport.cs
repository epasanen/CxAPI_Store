using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using static CxAPI_Store.CxConstant;

namespace CxAPI_Store
{
    public class SummaryAndAgingReport
    {
        public DataSet dataSet;
        public string businessName;
        public Dictionary<string, DataSet> messy = new Dictionary<string, DataSet>();
        public MonthByNumber mbn;
        public int maxMonths = 7;
        public string runDate;
        public string overAllSeverity = String.Empty;
        public Dictionary<string, int> allCounts;
        public Dictionary<string, int> nCounts;
        public Dictionary<string, int> policy;
        public resultClass token;
        public MakeReports report;
        public SQLiteMaster sqlite;

        public SummaryAndAgingReport(resultClass token, MakeReports report)
        {
            this.token = token;
            this.report = report;
            this.sqlite = report.sqllite();
            dataSet = new DataSet();
            messy = new Dictionary<string, DataSet>() { { "High", new DataSet() }, { "Medium", new DataSet() }, { "Low", new DataSet() }, { "Info", new DataSet() } };
            allCounts = new Dictionary<string, int>() { { "High", 0 }, { "Medium", 0 }, { "Low", 0 }, { "Info", 0 } };
            nCounts = new Dictionary<string, int>() { { "High", 0 }, { "Medium", 0 }, { "Low", 0 }, { "Info", 0 } };
            policy = new Dictionary<string, int>() { { "High", 30 }, { "Medium", 60 }, { "Low", 90 }, { "Info", 180 } };
        }

        public object fetchReport()
        {
            businessName = token.tenant;
            runDate = DateTime.UtcNow.ToLongDateString();

            dataSet = report.filterFromCommandLine();
            riskScoreByBusinessApp();
            previousVsCurrentScans();
            openFindingsViolations();
            byOWASPCategory();
            scopeOfScan();
            openDetails();
            return this;
        }
        public void riskScoreByBusinessApp()
        {
            mbn = new MonthByNumber(maxMonths, "ScanFinished");

            DataTable table = new DataTable("months");

            table.Columns.Add("Project_Id", typeof(Int64));
            table.Columns.Add("ComponentName", typeof(String));
            for (int i = 0; i < mbn.toprisk.Count; i++)
            {
                table.Columns.Add(String.Format("MonthRisk_{0}", i), typeof(Int32));
            }

            foreach (DataRow dr in dataSet.Tables[ProjectTable].Rows)
            {
                var myrisk = mbn.clearRisk(maxMonths);
                string project_name = (string)dr["ProjectName"];
                long project_Id = (long)dr["ProjectId"];

                for (int i = 0; i < myrisk.Count; i++)
                {
                    var query = mbn.lastquery[i];
                    DataView view = new DataView(dataSet.Tables[ScanTable]);
                    view.RowFilter = String.Format("ProjectId = {0} and {1}", dr["ProjectId"], query);
                    view.Sort = "ScanFinished DESC";
                    var result = view.ToTable(true, "ScanRiskSeverity");
                    if (result.Rows.Count > 0)
                    {
                        myrisk[i] = Convert.ToInt32(result.Rows[0].ItemArray[0]);
                        mbn.toprisk[i] = (myrisk[i] > mbn.toprisk[i]) ? myrisk[i] : mbn.toprisk[i];
                    }
                }
                DataRow newrow = table.NewRow();
                newrow["ComponentName"] = project_name;
                newrow["Project_Id"] = project_Id;
                for (int i = 0; i < myrisk.Count; i++)
                {
                    newrow[String.Format("MonthRisk_{0}", i)] = myrisk[i];
                }
                table.Rows.Add(newrow);
            }

            DataRow toprow = table.NewRow();
            toprow["ComponentName"] = "Total Risk Score";
            toprow["Project_Id"] = 0;
            for (int i = 0; i < mbn.toprisk.Count; i++)
            {
                toprow[String.Format("MonthRisk_{0}", i)] = mbn.toprisk[i];
            }
            table.Rows.Add(toprow);
            DataView newview = new DataView(table);
            newview.Sort = "Project_Id ASC";
            dataSet.Tables.Add(newview.ToTable());

        }
        public void previousVsCurrentScans()
        {
            MonthByNumber mbn = new MonthByNumber(1, "ScanFinished");
            List<string> severities = new List<string>() { "High", "Medium", "Low", "Info" };
            DataTable table = new DataTable("severities");
            table.Columns.Add("Severity", typeof(String));
            table.Columns.Add("PreviousMonth", typeof(Int32));
            table.Columns.Add("CurrentMonth", typeof(Int32));
            table.Columns.Add("Difference", typeof(Int32));
            Dictionary<long, List<GetResultCounts>> byMonth = new Dictionary<long, List<GetResultCounts>>();
            foreach (string severity in severities)
            {
                int lastcount = 0;
                int thiscount = 0;
                foreach (DataRow dr in dataSet.Tables[ProjectTable].Rows)
                {
                    long projectId = (long)dr["ProjectID"];
                    if (!byMonth.ContainsKey(projectId))
                    {
                        GetResultCounts lastCounts = new GetResultCounts(sqlite);
                        GetResultCounts thisCounts = new GetResultCounts(sqlite);
                        lastCounts.GetCorrectCounts(projectId, mbn.lastquery[0]);
                        thisCounts.GetCorrectCounts(projectId, mbn.lastquery[1]);
                        byMonth.Add(projectId, new List<GetResultCounts>() { lastCounts, thisCounts });
                    }
                    lastcount = lastcount + byMonth[projectId][0].Severity[severity];
                    thiscount = thiscount + byMonth[projectId][1].Severity[severity];
                }

                DataRow newrow = table.NewRow();
                newrow["Severity"] = severity;
                newrow["PreviousMonth"] = lastcount;
                newrow["CurrentMonth"] = thiscount;
                newrow["Difference"] = lastcount - thiscount;
                table.Rows.Add(newrow);
                if (thiscount > 0 && String.IsNullOrEmpty(overAllSeverity))
                {
                    overAllSeverity = severity;
                }

            }
            dataSet.Tables.Add(table);
        }
        public void openFindingsViolations(bool all = false)
        {
            MonthByNumber mbn = new MonthByNumber(1, "ScanFinished");
            DataTable table = makeaTable("openFindingsViolations");
            DataTable cloneTable = makeaTable("cloneTable");

            DateTime today = DateTime.Now;
            Dictionary<long, GetResultAging> doOnce = new Dictionary<long, GetResultAging>();
            foreach (string Key in policy.Keys)
            {
                foreach (DataRow dr in dataSet.Tables[ProjectTable].Rows)
                {

                    int policyLength = policy[Key];
                    long projectId = (long)dr["ProjectId"];
                    long lastScanId;
                    DateTime lastScanDate;
                    string project_name = (string)dr["ProjectName"];
                    string isIncremental = String.Empty;

                    if (token.debug && token.verbosity > 0) Console.WriteLine("Process {0} severity {1}", project_name, Key);
                    DataView scanview = new DataView(dataSet.Tables[ScanTable]);

                    scanview.RowFilter = String.Format("ProjectId = {0}", dr["ProjectId"]);
                    scanview.Sort = "ScanFinished DESC";
                    var endscan = scanview.ToTable();
                    if (endscan.Rows.Count > 0)
                    {
                        lastScanId = (long)endscan.Rows[0]["ScanId"];
                        lastScanDate = (DateTime)endscan.Rows[0]["ScanFinished"];
                        isIncremental = (string)endscan.Rows[0]["ScanType"];
                    }
                    else
                    {
                        continue;
                    }
                    if (!doOnce.ContainsKey(projectId))
                    {
                        GetResultAging temp = new GetResultAging(sqlite, token);
                        temp.GetResultAgingbySimilarityId(projectId);
                        doOnce.Add(projectId, temp);
                    }
                    GetResultAging aging = doOnce[projectId];

                    //string sql = String.Format("select ProjectId, SimilarityId, FileNameHash, ScanFinished, ResultSeverity from Results where ProjectId = {0} and ResultSeverity = '{1}'", projectId, Key);
                    //DataTable first = sqlite.SelectIntoDataTable("FirstResult", sql);

                    DataTable last = sqlite.SelectIntoDataTable(dataSet.Tables[ResultTable], "LastResult", String.Format("and ProjectId = {0} and ScanId = {1} and ResultSeverity = '{2}' and FalsePositive != 'True'", projectId, lastScanId, Key));

                    var startList = last.AsEnumerable()
                         .Select(row => new
                         {
                             ProjectId = row.Field<long>("ProjectId"),
                             SimilarityId = row.Field<long>("SimilarityId"),
                             FileNameHash = row.Field<long>("FileNameHash"),
                             ResultId = row.Field<long>("ResultId"),
                             PathId = row.Field<long>("PathId")
                         })
                    .Distinct()
                    .ToList();

                    if (token.debug && token.verbosity > 0) Console.WriteLine("Results to be processed: {0}", startList.Count);

                    foreach (var result in startList)
                    {
                        string infile = String.Empty;
                        string outfile = String.Empty;
                        string inline = String.Empty;
                        string outline = String.Empty;
                        string name = String.Empty;
                        string qname = String.Empty;
                        string deepLink = String.Empty;

                        long similarity_id = result.SimilarityId;
                        long fileNameHash = result.FileNameHash;
                        if (similarity_id != 0)
                        {

                            var endList = last.AsEnumerable()
                                .Where(row => row.Field<long>("ProjectId") == result.ProjectId && row.Field<string>("ResultSeverity") == Key && row.Field<string>("FalsePositive") != "True")
                                .Where(row => row.Field<long>("SimilarityId") == result.SimilarityId && row.Field<long>("ResultId") == result.ResultId && row.Field<long>("PathId") == result.PathId)
                                .Where(row=> row.Field<long>("FileNameHash") == result.FileNameHash)
                                .OrderBy(row => row.Field<long>("ScanId"))
                                .Select(row => new
                                {
                                    SimilarityId = row.Field<long>("SimilarityId"),
                                    VulnerabilityId = row.Field<long>("VulnerabilityId"),
                                    QueryName = row.Field<string>("QueryName"),
                                    ResultDeepLink = row.Field<string>("ResultDeepLink"),
                                    NodeFileName = row.Field<string>("NodeFileName"),
                                    SinkFileName = row.Field<string>("SinkFileName"),
                                    NodeLine = row.Field<long>("NodeLine"),
                                    SinkLine = row.Field<long>("SinkLine"),
                                    //ScanFinished = row.Field<string>("ScanFinished"),
                                    //DetectionDate = row.Field<DateTime>("DetectionDate")

                                })
                                .ToList();

                            var final = endList.LastOrDefault();


                            /*                           var simobj = first.AsEnumerable()
                                                           .Where(w => w.Field<long>("SimilarityId") == similarity_id && w.Field<long>("FileNameHash") == final.FileNameHash && w.Field<string>("ResultSeverity") == Key)
                                                           .Select(row => new
                                                           {
                                                               ScanFinished = row.Field<string>("ScanFinished")
                                                           })
                                                       .FirstOrDefault();
                            */
                            var simobj = aging.VulnerabilityExists(similarity_id, fileNameHash);
                            if (simobj is null) 
                                Console.WriteLine("{0} - {1} {2} not found", similarity_id, fileNameHash, projectId);
                            DateTime exists = (simobj != null) ? simobj.agingDate : DateTime.UtcNow;
                            
                            nCounts[Key]++;

                            deepLink = final.ResultDeepLink;
                            infile = final.NodeFileName;
                            outfile = final.SinkFileName;
                            inline = final.NodeLine.ToString();
                            outline = final.SinkLine.ToString();
                            qname = final.QueryName.ToString();
                            infile = String.IsNullOrEmpty(inline) ? infile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", infile.Substring(infile.LastIndexOf('/') + 1), inline);
                            outfile = String.IsNullOrEmpty(outline) ? outfile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", outfile.Substring(outfile.LastIndexOf('/') + 1), outline);

                            DataRow Row = cloneTable.NewRow();
                            Row["ProjectId"] = projectId;
                            Row["ComponentName"] = project_name;
                            Row["Severity"] = Key;
                            Row["SourceFile"] = infile;
                            Row["DestinationFile"] = outfile;
                            Row["Query"] = qname;
                            Row["FirstFound"] = exists.ToString("yyyy-MMM-dd");
                            Row["DeepLink"] = deepLink;
                            Row["PolicyViolation"] = policyLength;
                            cloneTable.Rows.Add(Row);

                            if (exists.AddDays(policy[Key]) < today || all)
                            {

                                DataRow dataRow = table.NewRow();
                                dataRow["ProjectId"] = projectId;
                                dataRow["ComponentName"] = project_name;
                                dataRow["Severity"] = Key;
                                dataRow["SourceFile"] = infile;
                                dataRow["DestinationFile"] = outfile;
                                dataRow["Query"] = qname;
                                dataRow["FirstFound"] = exists.ToString("yyyy-MMM-dd");
                                dataRow["DeepLink"] = deepLink;
                                dataRow["PolicyViolation"] = policyLength;
                                table.Rows.Add(dataRow);
                            }
                        }
                    }
                }
            }

            dataSet.Tables.Add(table);
            dataSet.Tables.Add(cloneTable);
        }
        public void byOWASPCategory()
        {
            MonthByNumber mbn = new MonthByNumber(1, "ScanFinished");
            CWEID top10 = new CWEID();

            DataTable table = new DataTable("OWASPCategory");
            table.Columns.Add("OWASPCategory", typeof(String));
            table.Columns.Add("PreviousMonth", typeof(Int32));
            table.Columns.Add("CurrentMonth", typeof(Int32));
            table.Columns.Add("Difference", typeof(Int32));

            foreach (int[] cweid in top10.owasp.Keys)
            {
                int lastcount = 0;
                int thiscount = 0;

                foreach (DataRow dr in dataSet.Tables[ProjectTable].Rows)
                {

                    DataView lastview = new DataView(dataSet.Tables[ResultTable]);
                    lastview.RowFilter = String.Format("ProjectId = {0} and {1} and QueryCweId in ({2}) and FalsePositive <> 'True'", dr["ProjectId"], mbn.lastquery[0], String.Join(',', cweid)); //previous month

                    DataView thisview = new DataView(dataSet.Tables[ResultTable]);
                    thisview.RowFilter = String.Format("ProjectId = {0} and {1} and QueryCweId in ({2}) and FalsePositive <> 'True'", dr["ProjectId"], mbn.lastquery[1], String.Join(',', cweid)); //current month

                    lastcount = lastcount + lastview.Count;
                    thiscount = thiscount + thisview.Count;

                }
                DataRow newrow = table.NewRow();
                newrow["OWASPCategory"] = top10.owasp[cweid];
                newrow["PreviousMonth"] = lastcount;
                newrow["CurrentMonth"] = thiscount;
                newrow["Difference"] = lastcount - thiscount;
                table.Rows.Add(newrow);

            }

            dataSet.Tables.Add(table);


        }
        public void scopeOfScan()
        {
            DataTable table = new DataTable("scopeOfScan");
            table.Columns.Add("ComponentName", typeof(String));
            table.Columns.Add("Languages", typeof(String));
            table.Columns.Add("LOC", typeof(String));

            foreach (DataRow dr in dataSet.Tables[ProjectTable].Rows)
            {
                long lastScanId;
                DateTime lastScanDate;
                string projectName;
                string languages;
                long LOC;

                DataView scanview = new DataView(dataSet.Tables[ScanTable]);
                scanview.RowFilter = String.Format("ProjectId = {0}", dr["ProjectId"]);
                scanview.Sort = "ScanFinished DESC";
                var endscan = scanview.ToTable(true, "ScanId", "ScanFinished", "ProjectName", "Languages", "LinesOfCode");
                if (endscan.Rows.Count > 0)
                {
                    lastScanId = (long)endscan.Rows[0]["ScanId"];
                    lastScanDate = (DateTime)endscan.Rows[0]["ScanFinished"];
                    projectName = (string)endscan.Rows[0]["ProjectName"];
                    languages = (string)endscan.Rows[0]["Languages"];
                    LOC = (long)endscan.Rows[0]["LinesOfCode"];
                    DataView summaryview = new DataView(dataSet.Tables[ScanTable]);
                    summaryview.RowFilter = String.Format("ProjectId = {0} and ScanId = {1}", dr["ProjectId"], lastScanId); //current month
                    if (summaryview.Count > 0)
                    {
                        DataRow dataRow = table.NewRow();
                        dataRow["ComponentName"] = projectName;
                        dataRow["Languages"] = languages.Replace("Common;", "");
                        dataRow["LOC"] = LOC;
                        table.Rows.Add(dataRow);
                    }
                }

            }
            dataSet.Tables.Add(table);
        }

        public void openDetails()
        {
            DateTime today = DateTime.Now;
            Dictionary<string, int> policy = new Dictionary<string, int>() { { "High", 30 }, { "Medium", 60 }, { "Low", 90 }, { "Info", 180 } };
            DataTable openTable = dataSet.Tables["cloneTable"];

            foreach (string Key in policy.Keys)
            {
                DataTable table = new DataTable();
                foreach (DataRow dr in dataSet.Tables[ProjectTable].Rows)
                {
                    long projectId = (long)dr["ProjectId"];
                    string projectName = (string)dr["ProjectName"];

                    table = makeaTable(projectName);
                    var openList = openTable.AsEnumerable()
                         .Where(row => row.Field<string>("Severity") == Key && row.Field<long>("ProjectID") == projectId)
                         .OrderBy(o => o.Field<string>("ComponentName"))
                         .Select(row => new
                         {
                             ProjectName = row.Field<string>("ComponentName"),
                             SourceFile = row.Field<string>("SourceFile"),
                             DestinationFile = row.Field<string>("DestinationFile"),
                             Query = row.Field<string>("Query"),
                             DeepLink = row.Field<string>("DeepLink"),
                             FoundFirst = row.Field<string>("FirstFound"),
                             PolicyViolation = row.Field<string>("PolicyViolation"),
                         })
                         .ToList();

                    foreach (var open in openList)
                    {
                        DataRow dataRow = table.NewRow();
                        dataRow["ComponentName"] = open.ProjectName;
                        dataRow["Severity"] = Key;
                        dataRow["SourceFile"] = open.SourceFile;
                        dataRow["DestinationFile"] = open.DestinationFile;
                        dataRow["Query"] = open.Query;
                        dataRow["DeepLink"] = open.DeepLink;
                        dataRow["FirstFound"] = open.FoundFirst;
                        dataRow["PolicyViolation"] = open.PolicyViolation;
                        table.Rows.Add(dataRow);
                    }
                    if (table.Rows.Count > 0)
                    {
                        messy[Key].Tables.Add(table);
                    }
                }
            }
        }
/*
        public void openDetails(bool all = true)
        {
            DateTime today = DateTime.Now;

            Dictionary<string, int> policy = new Dictionary<string, int>() { { "High", 30 }, { "Medium", 60 }, { "Low", 90 }, { "Info", 180 } };

            foreach (string Key in policy.Keys)
            {
                DataTable table = new DataTable();

                foreach (DataRow dr in dataSet.Tables[ProjectTable].Rows)
                {
                    long lastScanId;
                    long projectId = (long)dr["ProjectId"];
                    DateTime lastScanDate;
                    string project_name = (string)dr["ProjectName"];

                    DataView lastview = new DataView(dataSet.Tables[ScanTable]);
                    lastview.RowFilter = String.Format("ProjectId = {0}", projectId); //previous month
                    lastview.Sort = "ScanFinished DESC";
                    DataTable lastmonth = lastview.ToTable(false, "ScanId", "ScanFinished", String.Format("{0}", Key));

                    int scount = lastmonth.Rows.Count > 0 ? (int)lastmonth.Rows[0][String.Format("{0}", Key)] : 0;

                    if (scount > 0)
                    {
                        lastScanId = (long)lastmonth.Rows[0]["ScanId"];
                        lastScanDate = (DateTime)lastmonth.Rows[0]["ScanFinished"];
                    }
                    else
                    {
                        continue;
                    }

                    table = makeaTable(project_name);

                    string sql = String.Format("select ProjectId, SimilarityId, FileNameHash, ScanFinished from Results where ProjectId = {0}", projectId);
                    DataTable first = sqlite.SelectIntoDataTable("FirstResult", sql);


                    DataTable last = sqlite.SelectIntoDataTable(dataSet.Tables[ResultTable], "LastResult", String.Format("and ProjectId = {0} and ScanId = {1}", projectId, lastScanId));
                    var startList = last.AsEnumerable()
                         .Where(row => row.Field<string>("ResultSeverity") == Key && row.Field<string>("FalsePositive") != "True")
                         .Select(row => new
                         {
                             ProjectId = row.Field<long>("ProjectId"),
                             SimilarityId = row.Field<long>("SimilarityId"),
                             ResultId = row.Field<long>("ResultId"),
                             PathId = row.Field<long>("PathId")
                         })
                    .Distinct()
                    .ToList();


                    foreach (var result in startList)
                    {
                        string infile = String.Empty;
                        string outfile = String.Empty;
                        string inline = String.Empty;
                        string outline = String.Empty;
                        string name = String.Empty;
                        string qname = String.Empty;
                        string deepLink = String.Empty;

                        long similarity_id = result.SimilarityId;
                        if (similarity_id != 0)
                        {

                            var endList = last.AsEnumerable()
                                .Where(row => row.Field<long>("ProjectId") == result.ProjectId && row.Field<string>("ResultSeverity") == Key && row.Field<string>("FalsePositive") != "True")
                                .Where(row => row.Field<long>("SimilarityId") == result.SimilarityId && row.Field<long>("ResultId") == result.ResultId && row.Field<long>("PathId") == result.PathId)
                                .OrderBy(row => row.Field<long>("ScanId"))
                                  .Select(row => new
                                  {
                                      SimilarityId = row.Field<long>("SimilarityId"),
                                      VulnerabilityId = row.Field<long>("VulnerabilityId"),
                                      FileNameHash = row.Field<long>("FileNameHash"),
                                      QueryName = row.Field<string>("QueryName"),
                                      ResultDeepLink = row.Field<string>("ResultDeepLink"),
                                      NodeFileName = row.Field<string>("NodeFileName"),
                                      SinkFileName = row.Field<string>("SinkFileName"),
                                      NodeLine = row.Field<long>("NodeLine"),
                                      SinkLine = row.Field<long>("SinkLine"),
                                      //ScanFinished = row.Field<string>("ScanFinished"),
                                      //DetectionDate = row.Field<DateTime>("DetectionDate")

                                  })
                                 .ToList();

                            var final = endList.LastOrDefault();

                            var simobj = first.AsEnumerable()
                                .Where(w => w.Field<long>("SimilarityId") == similarity_id && w.Field<long>("FileNameHash") == final.FileNameHash)
                                .Select(row => new
                                {
                                    ScanFinished = row.Field<string>("ScanFinished")
                                })
                            .FirstOrDefault();

                            DateTime exists = DateTime.Parse(simobj.ScanFinished);
                            nCounts[Key]++;
                            if (exists.AddDays(policy[Key]) < today || all)
                            {

                                deepLink = final.ResultDeepLink;
                                infile = final.NodeFileName;
                                outfile = final.SinkFileName;
                                inline = final.NodeLine.ToString();
                                outline = final.SinkLine.ToString();
                                qname = final.QueryName.ToString();
                                infile = String.IsNullOrEmpty(inline) ? infile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", infile.Substring(infile.LastIndexOf('/') + 1), inline);
                                outfile = String.IsNullOrEmpty(outline) ? outfile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", outfile.Substring(outfile.LastIndexOf('/') + 1), outline);

                                // outfile = Convert.ToString(similarityid);
                                DataRow dataRow = table.NewRow();
                                dataRow["ComponentName"] = project_name;
                                dataRow["Severity"] = Key;
                                dataRow["SourceFile"] = infile;
                                dataRow["DestinationFile"] = outfile;
                                dataRow["Query"] = qname;
                                dataRow["DeepLink"] = deepLink;
                                dataRow["FirstFound"] = exists.ToString("MMM-dd-yyyy");
                                dataRow["PolicyViolation"] = policy[Key];
                                table.Rows.Add(dataRow);
                            }
                        }

                    }
                    if (table.Rows.Count > 0)
                    {
                        messy[Key].Tables.Add(table);
                    }
                }

            }
        }
*/
        private DataTable makeaTable(string name)
        {
            DataTable table = new DataTable(name);
            table.Columns.Add("Severity", typeof(String));
            table.Columns.Add("ProjectId", typeof(long));
            table.Columns.Add("ComponentName", typeof(String));
            table.Columns.Add("SourceFile", typeof(String));
            table.Columns.Add("DestinationFile", typeof(String));
            table.Columns.Add("Query", typeof(String));
            table.Columns.Add("DeepLink", typeof(String));
            table.Columns.Add("FirstFound", typeof(String));
            table.Columns.Add("PolicyViolation", typeof(String));
            return table;
        }
    }
}
