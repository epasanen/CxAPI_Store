using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using CxAPI_Store.dto;
using Newtonsoft.Json;
using DustInTheWind.ConsoleTools.Spinners;

namespace CxAPI_Store
{
    public class fetchAnalytix : IDisposable
    {
        DataSet dataSet;
        resultClass _token;
        SQLiteCreator creator;
        const string ProjectTable = "Projects";
        const string ScanTable = "Scans";
        const string ResultTable = "Results";


        public fetchAnalytix(resultClass token, bool init = true)
        {
            _token = token;
            dataSet = new DataSet();
            dataSet.Tables.Add(new DataTable(ProjectTable));
            dataSet.Tables.Add(new DataTable(ScanTable));
            dataSet.Tables.Add(new DataTable(ResultTable));
            creator = new SQLiteCreator(_token);
          
            if (init)
            {
                initAllTables();
            }
        }
        private bool saveDataSet(bool toSQL = false)
        {
            if (!toSQL)
            {
                if (!dataSet.ExtendedProperties.ContainsKey("LastUpdate"))
                {
                    dataSet.ExtendedProperties.Add("LastUpdate", DateTime.UtcNow.ToString());
                }
                else
                {
                    dataSet.ExtendedProperties["LastUpdate"] = DateTime.UtcNow.ToString();
                }
                dataSet.WriteXmlSchema(_token.archival_path + _token.os_path + "CxSchema.xml");
                dataSet.WriteXml(_token.archival_path + _token.os_path + "Cxdata.xml");
            }
            else
            {
                saveToSQLite();
            }
            return true;

        }
        private bool restoreDataSet()
        {
            if (File.Exists(_token.archival_path + _token.os_path + "CxSchema.xml"))
            {
                dataSet.ReadXmlSchema(_token.archival_path + _token.os_path + "CxSchema.xml");
                dataSet.ReadXml(_token.archival_path + _token.os_path + "Cxdata.xml");
            }

            return true;
        }
        private void saveToSQLite()
        {
            createSQLTable(ProjectTable);
            createSQLTable(ResultTable);
            createSQLTable(ScanTable);
            insertSQLTable(ProjectTable);
            insertSQLTable(ScanTable);
            insertSQLTable(ResultTable);
        }

        private void createSQLTable(string tableName)
        {
            creator.CreateFromDataTable(dataSet.Tables[tableName]);
        }
        private void insertSQLTable(string tableName)
        {
            //creator.InsertDataSetToSQLite(dataSet.Tables[tableName]);
            creator. InsertParametersDataSetToSQLite(dataSet.Tables[tableName]);
        }

        private bool initAllTables()
        {
            // create projects
            initDataTable(new CxProject(), ProjectTable, new List<string>() { "ProjectId" });
            initDataTable(new CxScan(), ScanTable, new List<string>() { "ProjectId", "ScanFinished" });
            initDataTable(new CxResult(), ResultTable, new List<string>() {"ProjectId", "VulnerabilityId","SimilarityId" });
            return true;
        }

        private bool initDataTable(object mapObject, string select, List<string> primaryKeys)
        {
            DataTable table = dataSet.Tables[select];
            List<DataColumn> cols = new List<DataColumn>();
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                var prop = property.PropertyType;
                table.Columns.Add(name, prop);
                if (primaryKeys.Contains(name))
                {
                    cols.Add(table.Columns[name]);
                }
            }
            table.PrimaryKey = cols.ToArray();
            return true;
        }

        private bool addRow(object mapObject, string select)
        {
            if (_token.debug && _token.verbosity > 1) Console.WriteLine("Adding to table {0}", select);
            DataTable table = dataSet.Tables[select];
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            DataRow dr = table.NewRow();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                //var prop = property.PropertyType;
                var value = property.GetValue(mapObject, null);
                if (_token.debug && _token.verbosity > 2) Console.WriteLine("Adding '{0}':{1} to table {2}", name, value, select);
                dr[name] = value;
            }
            table.Rows.Add(dr);
            return true;
        }
        public void loadDataSet(bool startNew = true)
        {
            if (_token.debug && _token.verbosity > 0 && startNew) Console.WriteLine("Initializing dataset");

            if (!startNew)
                restoreDataSet();

            if (Directory.EnumerateFiles(_token.archival_path, "sast_project_info.*.log").Any())
            {
                if (_token.debug && _token.verbosity > 0) Console.WriteLine("Using data from CxAnalytix");
                loadAnalytix();
            }
            else
            {
                if (_token.debug && _token.verbosity > 0) Console.WriteLine("Using data from CxStore");
                loadRawFiles();
            }

        }

        private void loadAnalytix()
        {
            getProjects();
            getScans();
            getResults();
            saveDataSet(true);
        }
        private void loadRawFiles()
        {
            getRawData();
            saveDataSet(true);
        }
        private bool getProjects()
        {
            Console.WriteLine("Loading table {0}", ProjectTable);
            Spinner.Run(() =>
            {
               var jsonFiles = Directory.EnumerateFiles(_token.archival_path, "sast_project_info.*.log");
            foreach (string filename in jsonFiles)
            {
                foreach (string content in File.ReadAllLines(filename))
                {
                    CxProjectJson project = JsonConvert.DeserializeObject<CxProjectJson>(content);
                    if (!dataSet.Tables[ProjectTable].AsEnumerable().Any(row => project.ProjectId == row.Field<long>("ProjectId")))
                    {
                      

                        addRow(project.convertObject(), ProjectTable);
                    }
                }
            }
            });
            return true;
        }
        private bool getScans()
        {
            Console.WriteLine("Loading table {0}", ScanTable);
            Spinner.Run(() =>
            {
                var jsonFiles = Directory.EnumerateFiles(_token.archival_path, "sast_scan_summary.*.log");
                foreach (string filename in jsonFiles)
                {
                    foreach (string content in File.ReadAllLines(filename))
                    {
                        CxScan scans = JsonConvert.DeserializeObject<CxScan>(content);
                        if (!dataSet.Tables[ScanTable].AsEnumerable().Any(row => scans.ProjectId == row.Field<long>("ProjectId") && scans.ScanId == row.Field<long>("ScanId")))
                        {
                            addRow(scans, ScanTable);
                        }
                    }
                }
            });
            return true;
        }
        private bool getResults()
        {
            Console.WriteLine("Loading table {0}", ResultTable);
            Spinner.Run(() =>
            {
                var jsonFiles = Directory.EnumerateFiles(_token.archival_path, "sast_scan_detail.*.log");
            foreach (string filename in jsonFiles)
            {
                foreach (string content in File.ReadAllLines(filename))
                {
                    CxResult results = JsonConvert.DeserializeObject<CxResult>(content);
                    if (!dataSet.Tables[ResultTable].AsEnumerable().Any(row => results.ProjectId == row.Field<long>("ProjectId") && results.VulnerabilityId == row.Field<long>("VulnerabilityId") && results.SimilarityId == row.Field<long>("SimilarityId")))
                    {
                        addRow(results, ResultTable);
                    }
                }
            }
            });

            return true;
        }

        private void getRawData()
        {
            analytixTransform();
        }

        private void mapAnalytixFields(DataSet dataSet, long projectId)
        {
            maptoprojects(dataSet, projectId);
            maptoscans(dataSet, projectId);
            maptoresults(dataSet, projectId);
        }

        private void maptoprojects(DataSet ds, long projectId)
        {
            IEnumerable<CxProject> results = from projects in ds.Tables[ProjectTable].AsEnumerable()
                                             where projects.Field<long>("Key_Project_Id") == projectId
                                             select new CxProject
                                             {
                                                 ProjectId = (long)projects["Key_Project_Id"],
                                                 ProjectName = (string)projects["Key_Project_Name"],
                                                 Policies = "",
                                                 TeamName = (string)projects["Project_Team_Name"],
                                                 Preset = (string)projects["Project_Preset_Name"],
                                                 CustomFields = mapDictionary(ds, "Project_customFields_~node_name", "Project_customFields_~node_value"),
                                                 SAST_LastScanDate = (DateTime)mapQueryLast(ds, "Summary_DateAndTime_FinishedOn"),
                                                 SAST_Scans = (int)mapQueryCount(ds, "Summary_DateAndTime_FinishedOn")
                                             };

            foreach (CxProject project in results)
            {
                if (!dataSet.Tables[ProjectTable].AsEnumerable().Any(row => project.ProjectId == row.Field<long>("ProjectId")))
                {
                    addRow(project, ProjectTable);
                }
            }
        }
        private void maptoscans(DataSet ds, long projectId)
        {

            IEnumerable<CxScan> results = from scan in ds.Tables["scans"].AsEnumerable()
                                          join project in ds.Tables["projects"].AsEnumerable() on (long)scan["Key_Project_Id"] equals (long)project["Key_Project_Id"]
                                          join summary in ds.Tables["summaries"].AsEnumerable() on (long)scan["Key_Scan_Id"] equals (long)summary["Key_Scan_Id"]
                                          where scan.Field<long>("Key_Project_Id") == projectId
                                          select new CxScan
                                          {
                                              ScanStarted = (DateTime)summary["Summary_DateAndTime_StartedOn"],
                                              ScanFinished = (DateTime)summary["Summary_DateAndTime_FinishedOn"],
                                              EngineStart = (DateTime)summary["Summary_DateAndTime_EngineStartedOn"],
                                              EngineFinished = (DateTime)summary["Summary_DateAndTime_EngineFinishedOn"],
                                              FileCount = (int)summary["Summary_ScanState_LinesOfCode"],
                                              LinesOfCode = (int)summary["Summary_ScanState_FilesCount"],
                                              FailedLinesOfCode = (int)summary["Summary_ScanState_FailedLinesOfCode"],
                                              CxVersion = (string)summary["Summary_ScanState_CxVersion"],
                                              Languages = mapList(ds, "Summary_ScanState_LanguageStateCollection_~node_LanguageName"),
                                              Initiator = (string)summary["Summary_InitiatorName"],
                                              ScanRisk = (int)summary["Summary_ScanRisk"],
                                              ScanRiskSeverity = (int)summary["Summary_ScanRiskSeverity"],
                                              High = (int)summary["Summary_HighSeverity"],
                                              Medium = (int)summary["Summary_MediumSeverity"],
                                              Low = (int)summary["Summary_LowSeverity"],
                                              Info = (int)summary["Summary_InfoSeverity"],
                                              ProjectId = (long)scan["Key_Project_Id"],
                                              ProjectName = (string)scan["Key_Project_Name"],
                                              DeepLink = (string)scan["Scan_DeepLink"],
                                              ScanStart = (DateTime)scan["Scan_ScanStart"],
                                              Preset = (string)scan["Scan_Preset"],
                                              ScanTime = (string)scan["Scan_ScanTime"],
                                              ScanProduct = "SAST",
                                              ReportCreationTime = (DateTime)scan["Scan_ReportCreationTime"],
                                              TeamName = (string)scan["Scan_Team"],
                                              ScanComments = (string)scan["Scan_ScanComments"],
                                              ScanType = (string)scan["Scan_ScanType"],
                                              SourceOrigin = (string)scan["Scan_SourceOrigin"],
                                              ScanId = (long)scan["Scan_ScanId"]
                                          };

            foreach (CxScan scan in results)
            {
                if (!dataSet.Tables[ScanTable].AsEnumerable().Any(row => scan.ProjectId == row.Field<long>("ProjectId") && scan.ScanId == row.Field<long>("ScanId")))
                {
                    addRow(scan, ScanTable);
                }
            }
        }
        private void maptoresults(DataSet ds, long projectId)
        {

            IEnumerable<CxScan> scans = from scan in dataSet.Tables[ScanTable].AsEnumerable()
                                        where scan.Field<long>("ProjectId") == projectId
                                        orderby scan.Field<DateTime>("ScanFinished") descending
                                        select new CxScan
                                        {
                                            ScanId = (long)scan["ScanId"],
                                            ProjectId = (long)scan["ProjectID"],
                                            ProjectName = (string)scan["ProjectName"],
                                            ScanFinished = (DateTime)scan["ScanFinished"]
                                        };
            if (_token.debug && _token.verbosity > 0) Console.WriteLine("ProjectId: {0}, Scan Count: {1}", projectId, scans.Count());
            foreach (CxScan scan in scans)
            {
                try
                {
                    IEnumerable<CxResult> results = from query in ds.Tables["queries"].AsEnumerable()
                                                    join result in ds.Tables["results"].AsEnumerable() on ((long)query["Key_Scan_Id"], (long)query["Key_Query_Id"]) equals ((long)result["Key_Scan_Id"], (long)result["Key_Query_Id"])
                                                    join node in ds.Tables["nodes"].AsEnumerable() on ((long)result.Field<long>("Key_Result_NodeId"), (long)result["Key_Result_SimilarityId"]) equals ((long)node.Field<long>("Key_Result_NodeId"), (long)node["Key_Result_SimilarityId"])
                                                    where query.Field<long>("Key_Scan_Id") == scan.ScanId
                                                    select new CxResult
                                                    {
                                                        FalsePositive = (string)result["Result_FalsePositive"],
                                                        NodeCodeSnippet = (string)node["PathNode_First_Snippet_Line_Code"],
                                                        NodeColumn = (int)node["PathNode_First_Column"],
                                                        NodeFileName = (string)node["PathNode_First_FileName"],
                                                        NodeId = (long)node["Key_Result_Count"],
                                                        NodeLength = (int)node["PathNode_First_Length"],
                                                        NodeLine = (int)node["PathNode_First_Line"],
                                                        NodeName = (string)node["PathNode_First_Name"],
                                                        NodeType = (string)node["PathNode_First_Type"],
                                                        PathId = (int)result["Result_PathId"],
                                                        ProjectId = (long)result["Key_Project_Id"],
                                                        ProjectName = (string)result["Key_Project_Name"],
                                                        QueryCategories = (string)query["Query_categories"],
                                                        QueryCweId = (int)query["Query_cweId"],
                                                        QueryGroup = (string)query["Query_group"],
                                                        QueryId = (long)query["Query_id"],
                                                        QueryLanguage = (string)query["Query_Language"],
                                                        QueryName = (string)query["Query_name"],
                                                        QuerySeverity = (string)query["Query_Severity"],
                                                        QueryVersionCode = (string)query["Query_QueryVersionCode"],
                                                        Remark = (string)result["Result_Remark"],
                                                        ResultDeepLink = (string)result["Result_DeepLink"],
                                                        ResultId = (long)result["Result_ResultId"],
                                                        ResultSeverity = (string)result["Result_Severity"],
                                                        ScanFinished = (DateTime)result["Key_Finish_Date"],
                                                        ScanId = (long)result["Key_Scan_Id"],
                                                        ScanProduct = "SAST",
                                                        ScanType = (string)result["Key_Scan_Type"],
                                                        SimilarityId = (long)result["Result_SimilarityId"],
                                                        SinkColumn = (int)node["PathNode_Last_Column"],
                                                        SinkFileName = (string)node["PathNode_Last_FileName"],
                                                        SinkLine = (int)node["PathNode_Last_Line"],
                                                        State = (int)result["Result_state"],
                                                        Status = (string)result["Result_Status"],
                                                        TeamName = (string)result["Key_Project_Team_Name"],
                                                        VulnerabilityId = (long)result["Key_Result_NodeId"]
                                                    };

                    if (_token.debug && _token.verbosity > 0) Console.WriteLine("ProjectName: {0}, ScanId: {1}, Scan Date: {2}, Result Count: {3}", scan.ProjectName, scan.ScanId, scan.ScanFinished, results.Count());
                    foreach (CxResult result in results)
                    {
                        if (_token.debug && _token.verbosity > 2) listObject(result);
                        if (!dataSet.Tables[ResultTable].AsEnumerable().Any(row => result.ProjectId == row.Field<long>("ProjectId") && result.VulnerabilityId == row.Field<long>("VulnerabilityId") && result.SimilarityId == row.Field<long>("SimilarityId")))
                            addRow(result, ResultTable);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed to import scan results, ProjectName: {0}, ScanId: {1}, Scan Date: {2}", scan.ProjectName, scan.ScanId, scan.ScanFinished);
                    Console.Error.WriteLine(ex.Message);

                }
            }
        }

        private string mapDictionary(DataSet ds, string keyField, string valueField)
        {
            string result = String.Empty;
            for (int i = 0; i < 10; i++)
            {
                string key = keyField.Replace("~node", i.ToString());
                string value = valueField.Replace("~node", i.ToString());
                if (ds.Tables["projects"].Columns.Contains(key.Trim()))
                {
                    string okey = ds.Tables["projects"].AsEnumerable().Select(s => s.Field<string>(key)).FirstOrDefault();
                    string oval = ds.Tables["projects"].AsEnumerable().Select(s => s.Field<string>(value)).FirstOrDefault();
                    if (!String.IsNullOrEmpty(okey))
                    {
                        result += String.Format("{{{0}:{1}}}", okey, oval);
                    }
                }
                else
                {
                    break;
                }
            }
            return result;
        }

        private void listObject(object mapObject)
        {
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                var prop = property.PropertyType;
                var value = property.GetValue(mapObject, null);
                if (prop == typeof(String))
                {
                    value = value.ToString().Substring(0, value.ToString().Length > 20 ? 20 : value.ToString().Length);
                }
                Console.WriteLine("{0}:{1}", name, value);
            }
            Console.WriteLine("---------------------------------------");
        }
        private string mapList(DataSet ds, string value)
        {
            string result = String.Empty;
            for (int i = 0; i < 10; i++)
            {
                string repl = value.Replace("~node", i.ToString());
                if (ds.Tables["queries"].Columns.Contains(repl))
                {
                    result += String.Format("{0};", ds.Tables["queries"].AsEnumerable().Select(s => s.Field<string>(repl)).FirstOrDefault());
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private object mapQueryLast(DataSet ds, string field)
        {
            return ds.Tables["summaries"].AsEnumerable().Select(s => s.Field<DateTime>(field)).OrderByDescending(f => f).FirstOrDefault();
        }
        private object mapQueryCount(DataSet ds, string field)
        {
            return ds.Tables["summaries"].AsEnumerable().Select(s => s.Field<DateTime>(field)).Count();
        }


        private void analytixTransform()
        {
            fetchProjectFiles fetchProjects = new fetchProjectFiles(_token);
            fetchProjects.fetchFilteredScans(_token);

            foreach (ProjectObject project in fetchProjects.CxProjects)
            {
                DataStore store = new DataStore(_token);
                buildDataSet buildData = new buildDataSet(_token, store);
                string masterFile = _token.master_path + _token.os_path + "MasterTemplate.yaml";
                fetchResults(project, fetchProjects, masterFile, buildData);
                mapAnalytixFields(store.dataSet, Convert.ToInt64(project.id));
                if (_token.autosave) saveDataSet();
            }
        }

        private void fetchResults(ProjectObject project, fetchProjectFiles fetchProjects, string masterFile, buildDataSet build)
        {
            long projectId = Convert.ToInt64(project.id);
            Dictionary<string, object> header = new Dictionary<string, object>();
            Dictionary<string, object> results = new Dictionary<string, object>();

            var resultStatisticsValues = fetchProjects.CxIdxResultStatistics[projectId];
            var scanValues = new SortedDictionary<long, ScanObject>(fetchProjects.CxIdxScans[projectId]);
            var resultValues = fetchProjects.CxIdxResults[projectId];
            header = fetchHeaders(fetchProjects, project, projectId);
            var projects = build.fetchProject(_token, header, masterFile);
            foreach (var item in scanValues.OrderBy(i => i.Key))
            {
                try
                {
                    if (_token.debug && _token.verbosity > 0) Console.WriteLine("ProjectName {0} ProjectId: {1} ScanId {2}", project.name, project.id, item.Key);
                    results = Flatten.DeserializeAndFlatten(scanValues[item.Key], new Dictionary<string, object>());
                    results = Flatten.DeserializeAndFlatten(resultStatisticsValues[item.Key], results);
                    XmlDocument doc = new XmlDocument();
                    string xml = resultValues[item.Key];
                    if (xml.Contains("~~Error")) { throw new InvalidOperationException("XML file not properly downloaded"); }
                    doc.LoadXml(resultValues[item.Key]);
                    string json = JsonConvert.SerializeXmlNode(doc);
                    Dictionary<string, object> xmlDict = Flatten.DeserializeAndFlatten(json);
                    results = Flatten.DeserializeAndFlatten(xmlDict, results);
                    build.fetchDetails(_token, results, masterFile);
                }
                catch (Exception ex)
                {
                    if (_token.debug && _token.verbosity > 0) Console.WriteLine("Failed to load ProjectName {0} ProjectId: {1} ScanId {2}", project.name, project.id, item.Key);
                    Console.WriteLine("Failure fetching detail xml {0} : {1}", item.Key, ex.Message);
                }
            }

            return;
        }
        private Dictionary<string, object> fetchHeaders(fetchProjectFiles fetchProject, ProjectObject project, long projectId)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            var scanValues = fetchProject.CxIdxScans[projectId];
            var projectSettings = fetchProject.CxSettings[projectId];
            var projectDetails = fetchProject.CxProjectDetail[projectId];
            result = Flatten.DeserializeAndFlatten(project);
            var teamAndPreset = fetchProject.getTeamAndPresetNames(project.teamId, projectSettings.preset.id);
            result = Flatten.DeserializeAndFlatten(teamAndPreset, result);
            result = Flatten.DeserializeAndFlatten(projectSettings, result);
            result = Flatten.DeserializeAndFlatten(projectDetails, result);
            return result;
        }

        public void Dispose()
        {

        }
    }
}
