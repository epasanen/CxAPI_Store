using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace CxAPI_Store
{
    public class Queryable
    {
        public DataSet tempSet;
        public string businessName;

        public DataTable projects;
        public DataTable scans;
        public DataTable summaries;
        public DataTable queries;
        public DataTable results;
        public DataTable nodes;
        public Dictionary<string, DataSet> messy = new Dictionary<string, DataSet>();
        public MonthByNumber mbn;
        public int maxMonths = 7;
        public string runDate;
        public string overAllSeverity = String.Empty;
        public Dictionary<string, int> allCounts;

        public resultClass _token;

        public Queryable(resultClass token)
        {
            _token = token;
            tempSet = new DataSet();
            messy = new Dictionary<string, DataSet>() { { "High", new DataSet() }, { "Medium", new DataSet() }, { "Low", new DataSet() }, { "Info", new DataSet() } };
            allCounts = new Dictionary<string, int>() { { "High", 0 }, { "Medium", 0 }, { "Low", 0 }, { "Info", 0 } };
        }

        public void dumpTable(DataTable table)
        {

            foreach (DataRow dr in table.Rows)
            {
                foreach (DataColumn dc in table.Columns)
                {
                    File.AppendAllText(@"C:\scans\hold\dump.txt", String.Format("{0}:{1}\n", dc.ColumnName, dr.ItemArray[dc.Ordinal]));
                }
            }
            //            File.AppendAllText(@"C:\scans\hold\dump.txt", capture);
        }
        public void dumpTables()
        {
            File.Delete(@"C:\scans\hold\dump.txt");
            dumpTable(projects);
            dumpTable(scans);
            dumpTable(summaries);
            dumpTable(queries);
            dumpTable(results);
            dumpTable(nodes);
        }


        public void getByCustomFields(DataSet dataSet)
        {
            projectsByCustomField(dataSet);
            dumpTables();
            riskScoreByBusinessApp();
            previousVsCurrentScans();
            openFindingsViolations();
            byOWASPCategory();
            scopeOfScan();
            openDetails();
        }


        public void projectsByCustomField(DataSet dataSet)
        {
            runDate = DateTime.Now.ToString("MMMM dd, yyyy");
            DataView projectView = new DataView(dataSet.Tables["projects"]);
            // projectView.RowFilter = _token.query_filter;
            projectView.RowFilter = "Project_customFields_0_value = '" + _token.query_filter + "' or Project_customFields_1_value = '" + _token.query_filter + "' or Project_customFields_2_value = '" + _token.query_filter + "'";
            projectView.Sort = "Project_Id ASC";
            projects = projectView.ToTable();
            tempSet.Tables.Add(projects);
            businessName = _token.query_filter;
            var rows = projects.AsEnumerable()
             .Select(r => string.Format("{0}", string.Join(",", r.ItemArray[0])));
            var output = string.Join(",", rows.ToArray());
            scans = new DataView(dataSet.Tables["scans"], String.Format("Key_Project_Id IN ({0})", output), "Key_Start_Date DESC", DataViewRowState.CurrentRows).ToTable();
            summaries = new DataView(dataSet.Tables["summaries"], String.Format("Key_Project_Id IN ({0})", output), "Key_Start_Date DESC", DataViewRowState.CurrentRows).ToTable();
            queries = new DataView(dataSet.Tables["queries"], String.Format("Key_Project_Id IN ({0})", output), "Key_Start_Date DESC", DataViewRowState.CurrentRows).ToTable();
            results = new DataView(dataSet.Tables["results"], String.Format("Key_Project_Id IN ({0})", output), "Key_Start_Date DESC", DataViewRowState.CurrentRows).ToTable();
            nodes = new DataView(dataSet.Tables["nodes"], String.Format("Key_Project_Id IN ({0})", output), "Key_Start_Date DESC", DataViewRowState.CurrentRows).ToTable();

        }
        public void riskScoreByBusinessApp()
        {
            mbn = new MonthByNumber(maxMonths, "Key_Start_Date");

            DataTable table = new DataTable("months");

            table.Columns.Add("Project_Id", typeof(Int64));
            table.Columns.Add("ComponentName", typeof(String));
            for (int i = 0; i < mbn.toprisk.Count; i++)
            {
                table.Columns.Add(String.Format("MonthRisk_{0}", i), typeof(Int32));
            }

            foreach (DataRow dr in tempSet.Tables["projects"].Rows)
            {
                var myrisk = mbn.clearRisk(maxMonths);
                string project_name = (string)dr["Project_Name"];
                int project_Id = (int)dr["Project_Id"];

                for (int i = 0; i < myrisk.Count; i++)
                {
                    var query = mbn.lastquery[i];
                    DataView view = new DataView(summaries);
                    view.RowFilter = String.Format("Key_Project_Id = {0} and {1}", dr["Project_Id"], query);
                    view.Sort = "Key_Start_Date DESC";
                    var result = view.ToTable(true, "Summary_ScanRiskSeverity");
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
            tempSet.Tables.Add(newview.ToTable());

        }
        public void previousVsCurrentScans()
        {
            MonthByNumber mbn = new MonthByNumber(1, "Key_Start_Date");
            List<string> severities = new List<string>() { "High", "Medium", "Low", "Info" };
            DataTable table = new DataTable("severities");
            table.Columns.Add("Severity", typeof(String));
            table.Columns.Add("PreviousMonth", typeof(Int32));
            table.Columns.Add("CurrentMonth", typeof(Int32));
            table.Columns.Add("Difference", typeof(Int32));
            foreach (string severity in severities)
            {
                int lastcount = 0;
                int thiscount = 0;
                foreach (DataRow dr in tempSet.Tables["projects"].Rows)
                {

                    DataView lastview = new DataView(summaries);
                    lastview.RowFilter = String.Format("Key_Project_Id = {0} and {1}", dr["Key_Project_Id"], mbn.lastquery[0]); //previous month
                    lastview.Sort = "Key_Start_Date DESC";
                    DataTable lastmonth = lastview.ToTable(false, String.Format("Summary_{0}Severity", severity));

                    DataView thisview = new DataView(summaries);
                    thisview.RowFilter = String.Format("Key_Project_Id = {0} and {1}", dr["Key_Project_Id"], mbn.lastquery[1]); //current month
                    thisview.Sort = "Key_Start_Date DESC";
                    DataTable thismonth = thisview.ToTable(false, String.Format("Summary_{0}Severity", severity));


                    object lastsum = lastmonth.Rows.Count > 0 ? lastmonth.Rows[0][0] : 0;
                    object thissum = thismonth.Rows.Count > 0 ? thismonth.Rows[0][0] : 0;

                    lastcount = lastcount + ((lastsum is DBNull) ? 0 : (int)lastsum);
                    thiscount = thiscount + ((thissum is DBNull) ? 0 : (int)thissum);
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
            tempSet.Tables.Add(table);
        }
        public void openFindingsViolations(bool all = false)
        {
            Dictionary<string, int> policy = new Dictionary<string, int>() { { "High", 30 }, { "Medium", 60 }, { "Low", 90 }, { "Info", 180 } };
            MonthByNumber mbn = new MonthByNumber(1, "Key_Start_Date");
            DataTable table = new DataTable("openFindingsViolations");
            table.Columns.Add("ComponentName", typeof(String));
            table.Columns.Add("Severity", typeof(String));
            table.Columns.Add("SourceFile", typeof(String));
            table.Columns.Add("DestinationFile", typeof(String));
            table.Columns.Add("Object", typeof(String));
            table.Columns.Add("Description", typeof(String));
            table.Columns.Add("FirstFound", typeof(String));
            table.Columns.Add("PolicyViolation", typeof(String));
            DateTime today = DateTime.Now;

            foreach (string Key in policy.Keys)
            {
                foreach (DataRow dr in tempSet.Tables["projects"].Rows)
                {

                    int policyLength = policy[Key];
                    long lastScanId;
                    DateTime lastScanDate;
                    long project_id = Convert.ToInt64(dr["Key_Project_Id"]);
                    string project_name = (string)dr["Key_Project_Name"];
                    string isIncremental = String.Empty;


                    DataView scanview = new DataView(summaries);

                    scanview.RowFilter = String.Format("Key_Project_Id = {0}", dr["Project_Id"]);
                    scanview.Sort = "Key_Start_Date DESC";
                    var endscan = scanview.ToTable();
                    if (endscan.Rows.Count > 0)
                    {
                        lastScanId = (long)endscan.Rows[0]["Key_Scan_Id"];
                        lastScanDate = (DateTime)endscan.Rows[0]["Key_Start_Date"];
                        isIncremental = (string)endscan.Rows[0]["Summary_IsIncremental"];
                    }
                    else
                    {
                        continue;
                    }

                    DataView startview = new DataView(results);
                    startview.RowFilter = String.Format("Key_Project_Id = {0} and Result_Severity = '{1}' and Key_Result_FalsePositive <> 'True'", project_id, Key);
                    var start = startview.ToTable(); // first instance

                    DataView endview = new DataView(start);
                    endview.RowFilter = String.Format("Key_Scan_Id = {0}", lastScanId);
                    var end = endview.ToTable();

                    foreach (DataRow endrow in end.Rows)
                    {
                        long similarity_id = endrow["Key_Result_SimilarityId"] is DBNull ? 0 : (long)endrow["Key_Result_SimilarityId"];
                        if (similarity_id != 0)
                        {
                            DataView dv = new DataView(start);
                            string filter = String.Format("Key_Result_SimilarityId = {0} and Result_Severity = '{1}'", similarity_id, Key);
                            dv.RowFilter = String.Format("Key_Result_SimilarityId = {0} and Result_Severity = '{1}'", similarity_id, Key);
                            startview.Sort = "Key_Start_Date ASC";
                            if (dv.Count > 0)
                            {
                                //this persists
                                var res = dv.ToTable();
                                DateTime exists = (DateTime)res.Rows[0]["Key_Start_Date"];
                                long similarityid = (long)res.Rows[0]["Key_Result_SimilarityId"];
                                allCounts[Key]++;
                                if (exists.AddDays(policyLength) < today || all)
                                {
                                    DataView node = new DataView(nodes);
                                    node.RowFilter = String.Format("Key_Result_SimilarityId = {0}", similarityid);
                                    if (node.Count > 0)
                                    {
                                        var resultnode = node.ToTable();
                                        string orgfile = resultnode.Rows[0]["Key_Result_FileName"] is DBNull ? "" : (string)resultnode.Rows[0]["Key_Result_FileName"];
                                        string infile = resultnode.Rows[0]["Key_Result_FileName"] is DBNull ? "" : (string)resultnode.Rows[0]["Key_Result_FileName"];
                                        string outfile = resultnode.Rows[0]["PathNode_Last_FileName"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_Last_FileName"];
                                        string inline = resultnode.Rows[0]["PathNode_First_Line"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_First_Line"];
                                        string outline = resultnode.Rows[0]["PathNode_Last_Line"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_Last_Line"];
                                        string name = resultnode.Rows[0]["PathNode_First_Name"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_First_Name"];
                                        string qname = resultnode.Rows[0]["Key_Query_Name"] is DBNull ? "" : (string)resultnode.Rows[0]["Key_Query_Name"];


                                        infile = String.IsNullOrEmpty(inline) ? infile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", infile.Substring(infile.LastIndexOf('/') + 1), inline);
                                        outfile = String.IsNullOrEmpty(outline) ? outfile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", outfile.Substring(outfile.LastIndexOf('/') + 1), outline);
                                        //outfile = Convert.ToString(similarityid) + ' ' +  orgfile;

                                        DataRow dataRow = table.NewRow();
                                        dataRow["ComponentName"] = project_name;
                                        dataRow["Severity"] = Key;
                                        dataRow["SourceFile"] = infile;
                                        dataRow["DestinationFile"] = outfile;
                                        dataRow["Object"] = name;
                                        dataRow["Description"] = qname;
                                        dataRow["FirstFound"] = exists.ToString("yyyy-MMM-dd");
                                        dataRow["PolicyViolation"] = policyLength;
                                        table.Rows.Add(dataRow);
                                    }
                                }
                            }

                        }

                    }

                }
            }
            tempSet.Tables.Add(table);
        }
        public void byOWASPCategory()
        {
            MonthByNumber mbn = new MonthByNumber(1, "Key_Start_Date");
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

                foreach (DataRow dr in tempSet.Tables["projects"].Rows)
                {

                    DataView lastview = new DataView(queries);
                    lastview.RowFilter = String.Format("Key_Project_Id = {0} and {1} and Key_Query_CWE in ({2}) and Key_Result_FalsePositive <> 'True'", dr["Project_Id"], mbn.lastquery[0], String.Join(',', cweid)); //previous month

                    DataView thisview = new DataView(summaries);
                    thisview.RowFilter = String.Format("Key_Project_Id = {0} and {1} and Key_Query_CWE in ({2}) and Key_Result_FalsePositive <> 'True'", dr["Project_Id"], mbn.lastquery[1], String.Join(',', cweid)); //current month

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

            tempSet.Tables.Add(table);


        }
        public void scopeOfScan()
        {
            DataTable table = new DataTable("scopeOfScan");
            table.Columns.Add("ComponentName", typeof(String));
            table.Columns.Add("Languages", typeof(String));
            table.Columns.Add("LOC", typeof(String));

            foreach (DataRow dr in tempSet.Tables["projects"].Rows)
            {
                long lastScanId;
                DateTime lastScanDate;
                string projectName;

                DataView scanview = new DataView(scans);
                scanview.RowFilter = String.Format("Key_Project_Id = {0}", dr["Project_Id"]);
                scanview.Sort = "Key_Start_Date DESC";
                var endscan = scanview.ToTable(true, "Key_Scan_Id", "Key_Start_Date", "Key_Project_Name");
                if (endscan.Rows.Count > 0)
                {
                    lastScanId = (long)endscan.Rows[0]["Key_Scan_Id"];
                    lastScanDate = (DateTime)endscan.Rows[0]["Key_Start_Date"];
                    projectName = (string)endscan.Rows[0]["Key_Project_Name"];
                    DataView summaryview = new DataView(summaries);
                    summaryview.RowFilter = String.Format("Key_Project_Id = {0} and Key_Scan_ID = {1}", dr["Project_Id"], lastScanId); //current month
                    if (summaryview.Count > 0)
                    {
                        var summary = summaryview.ToTable();
                        List<string> lang = new List<string>();
                        for (int i = 0; i < 5; i++)
                        {
                            if (!summary.Columns.Contains(String.Format("Summary_ScanState_LanguageStateCollection_{0}_LanguageName", i))) break;
                            lang.Add((string)summary.Rows[0][String.Format("Summary_ScanState_LanguageStateCollection_{0}_LanguageName", i)]);
                        }
                        string joined = string.Join(",", lang);
                        DataRow dataRow = table.NewRow();
                        dataRow["ComponentName"] = projectName;
                        dataRow["Languages"] = joined;
                        dataRow["LOC"] = summary.Rows[0]["Summary_ScanState_LinesOfCode"];
                        table.Rows.Add(dataRow);

                    }

                }

            }
            tempSet.Tables.Add(table);

        }
        public void openDetails(bool all = true)
        {
            DateTime today = DateTime.Now;

            Dictionary<string, int> policy = new Dictionary<string, int>() { { "High", 30 }, { "Medium", 60 }, { "Low", 90 }, { "Info", 180 } };

            foreach (string Key in policy.Keys)
            {
                DataTable table = new DataTable();

                foreach (DataRow dr in tempSet.Tables["projects"].Rows)
                {
                    long lastScanId;
                    DateTime lastScanDate;
                    string project_name = (string)dr["Project_Name"];

                    DataView lastview = new DataView(summaries);
                    lastview.RowFilter = String.Format("Key_Project_Id = {0}", dr["Key_Project_Id"]); //previous month
                    lastview.Sort = "Key_Start_Date DESC";
                    DataTable lastmonth = lastview.ToTable(false, "Key_Scan_Id", "Key_Start_Date", String.Format("Summary_{0}Severity", Key));

                    int scount = lastmonth.Rows.Count > 0 ? (int)lastmonth.Rows[0][String.Format("Summary_{0}Severity", Key)] : 0;

                    if (scount > 0)
                    {
                        lastScanId = (long)lastmonth.Rows[0]["Key_Scan_ID"];
                        lastScanDate = (DateTime)lastmonth.Rows[0]["Key_Start_Date"];
                    }
                    else
                    {
                        continue;
                    }

                    table = makeaTable(project_name);

                    DataView startview = new DataView(results);
                    startview.RowFilter = String.Format("Key_Project_Id = {0} and Key_Scan_Id = {1} and Result_Severity = '{2}' and Key_Result_FalsePositive <> 'True'", dr["Project_Id"], lastScanId, Key);
                    var start = startview.ToTable(true, "Key_Result_SimilarityId");
                    foreach (DataRow resultRow in start.Rows)
                    {
                        string infile = String.Empty;
                        string outfile = String.Empty;
                        string inline = String.Empty;
                        string outline = String.Empty;
                        string name = String.Empty;
                        string qname = String.Empty;

                        long similarity_id = resultRow["Key_Result_SimilarityId"] is DBNull ? 0 : (long)resultRow["Key_Result_SimilarityId"];
                        if (similarity_id != 0)
                        {

                            DataView dv = new DataView(nodes);
                            dv.RowFilter = String.Format("Key_Result_SimilarityId = {0}", similarity_id);

                            DataView topview = new DataView(results);
                            topview.RowFilter = String.Format("Key_Project_Id = {0} and Key_Result_SimilarityId= {1}", dr["Project_Id"], similarity_id);
                            topview.Sort = "Key_Start_Date ASC";
                            var top = topview.ToTable();
                            DateTime exists = top.Rows.Count > 0 ? (DateTime)top.Rows[0]["Key_Start_Date"] : lastScanDate;

                            if (exists.AddDays(policy[Key]) < today || all)
                            {

                                var resultnode = dv.ToTable();
                                if (resultnode.Rows.Count > 0)
                                {
                                    infile = resultnode.Rows[0]["Key_Result_FileName"] is DBNull ? "" : (string)resultnode.Rows[0]["Key_Result_FileName"];
                                    outfile = resultnode.Rows[0]["PathNode_Last_FileName"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_Last_FileName"];
                                    inline = resultnode.Rows[0]["PathNode_First_Line"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_First_Line"];
                                    outline = resultnode.Rows[0]["PathNode_Last_Line"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_Last_Line"];
                                    name = resultnode.Rows[0]["PathNode_First_Name"] is DBNull ? "" : (string)resultnode.Rows[0]["PathNode_First_Name"];
                                    qname = resultnode.Rows[0]["Key_Query_Name"] is DBNull ? "" : (string)resultnode.Rows[0]["Key_Query_Name"];
                                    infile = String.IsNullOrEmpty(inline) ? infile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", infile.Substring(infile.LastIndexOf('/') + 1), inline);
                                    outfile = String.IsNullOrEmpty(outline) ? outfile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", outfile.Substring(outfile.LastIndexOf('/') + 1), outline);
                                }
                                else
                                {
                                    infile = resultRow["Key_Result_FileName"] is DBNull ? "" : (string)resultRow["Key_Result_FileName"];
                                    outfile = "";
                                    inline = "";
                                    outline = "";
                                    name = "";
                                    qname = (string)resultRow["Key_Query_Name"];
                                    infile = String.IsNullOrEmpty(inline) ? infile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", infile.Substring(infile.LastIndexOf('/') + 1), inline);
                                    outfile = String.IsNullOrEmpty(outline) ? outfile.Substring(infile.LastIndexOf('/') + 1) : String.Format("{0}:{1}", outfile.Substring(outfile.LastIndexOf('/') + 1), outline);
                                }
                                // outfile = Convert.ToString(similarityid);
                                DataRow dataRow = table.NewRow();
                                dataRow["ComponentName"] = project_name;
                                dataRow["Severity"] = Key;
                                dataRow["SourceFile"] = infile;
                                dataRow["DestinationFile"] = outfile;
                                dataRow["Object"] = name;
                                dataRow["Description"] = qname;
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

        private DataTable makeaTable(string name)
        {
            DataTable table = new DataTable(name);
            table.Columns.Add("ComponentName", typeof(String));
            table.Columns.Add("Severity", typeof(String));
            table.Columns.Add("SourceFile", typeof(String));
            table.Columns.Add("DestinationFile", typeof(String));
            table.Columns.Add("Object", typeof(String));
            table.Columns.Add("Description", typeof(String));
            table.Columns.Add("FirstFound", typeof(String));
            table.Columns.Add("PolicyViolation", typeof(int));
            return table;

        }
    }
}
