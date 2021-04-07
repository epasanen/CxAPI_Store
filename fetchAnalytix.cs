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

namespace CxAPI_Store
{
    public class fetchAnalytix : IDisposable
    {
        DataSet dataSet;
        resultClass _token;

        public fetchAnalytix(resultClass token, bool init = true)
        {
            dataSet = new DataSet();
            dataSet.Tables.Add(new DataTable("Projects"));
            dataSet.Tables.Add(new DataTable("Scans"));
            dataSet.Tables.Add(new DataTable("Results"));
            _token = token;
            if (init)
            {
                initAllTables();
            }
        }
        public bool saveDataSet()
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
            return true;
        }
        public bool restoreDataSet()
        {
            if (File.Exists(_token.archival_path + _token.os_path + "CxSchema.xml"))
            {
                dataSet.ReadXmlSchema(_token.archival_path + _token.os_path + "CxSchema.xml");
                dataSet.ReadXml(_token.archival_path + _token.os_path + "Cxdata.xml");
            }

            return true;
        }
        public bool initAllTables()
        {
            // create projects
            initDataTable(new CxProject(), "Projects", new List<string>() { "ProjectId" });
            initDataTable(new CxScan(), "Scans", new List<string>() { "ProjectId", "ScanId" });
            initDataTable(new CxResult(), "Results", new List<string>() { "ProjectId", "ScanId", "VulnerabilityId" });
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
            if (_token.debug && _token.verbosity > 0) Console.WriteLine("Adding to table {0}", select);
            DataTable table = dataSet.Tables[select];
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            DataRow dr = table.NewRow();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                //var prop = property.PropertyType;
                var value = property.GetValue(mapObject, null);
                if (_token.debug && _token.verbosity > 3) Console.WriteLine("Adding '{0}':{1} to table {2}", name, value, select);
                dr[name] = value;
            }
            table.Rows.Add(dr);
            return true;
        }


        public void loadDataSet(bool startNew = true)
        {
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
            saveDataSet();
        }
        private void loadRawFiles()
        {
            getRawData();
            saveDataSet();
        }
        private bool getProjects()
        {
            var jsonFiles = Directory.EnumerateFiles(_token.archival_path, "sast_project_info.*.log");
            foreach (string filename in jsonFiles)
            {
                foreach (string content in File.ReadAllLines(filename))
                {
                    CxProjectJson project = JsonConvert.DeserializeObject<CxProjectJson>(content);
                    if (!dataSet.Tables["Projects"].AsEnumerable().Any(row => project.ProjectId == row.Field<long>("ProjectId")))
                    {
                        addRow(project.convertObject(), "Projects");
                    }
                }
                break;
            }
            return true;
        }
        private bool getScans()
        {
            var jsonFiles = Directory.EnumerateFiles(_token.archival_path, "sast_scan_summary.*.log");
            foreach (string filename in jsonFiles)
            {
                foreach (string content in File.ReadAllLines(filename))
                {
                    CxScan scans = JsonConvert.DeserializeObject<CxScan>(content);
                    if (!dataSet.Tables["Scans"].AsEnumerable().Any(row => scans.ProjectId == row.Field<long>("ProjectId") && scans.ScanId == row.Field<long>("ScanId")))
                    {
                        addRow(scans, "Scans");
                    }
                }

                break;
            }
            return true;
        }
        private bool getResults()
        {
            var jsonFiles = Directory.EnumerateFiles(_token.archival_path, "sast_scan_detail.*.log");
            foreach (string filename in jsonFiles)
            {
                foreach (string content in File.ReadAllLines(filename))
                {
                    CxResult results = JsonConvert.DeserializeObject<CxResult>(content);
                    if (!dataSet.Tables["Results"].AsEnumerable().Any(row => results.ProjectId == row.Field<long>("ProjectId") && results.ScanId == row.Field<long>("ScanId") && results.VulnerabilityId == row.Field<string>("VulnerabilityId")))
                    {
                        addRow(results, "Results");
                    }
                }
                break;
            }
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
            var results = from projects in ds.Tables["projects"].AsEnumerable()
                          where projects.Field<long>("Key_Project_Id") == projectId
                          select new CxProject
                          {
                              ProjectId = (long)projects["Key_Project_Id"],
                              ProjectName = (string)projects["Key_Project_Name"],
                              Policies = "",
                              TeamName = (string)projects["Project_Team_Name"],
                              Preset = (string)projects["Project_Preset_Name"],
                              CustomFields = mapDictionary(ds, "Project_customFields_~node_name", "Project_customFields_~node_value"),
                              SastLastScanDate = (DateTime)mapQueryLast(ds, "Summary_DateAndTime_FinishedOn"),
                              SastScans = (int)mapQueryCount(ds, "Summary_DateAndTime_FinishedOn")
                          };

            foreach (CxProject project in results)
            {
                if (!dataSet.Tables["Projects"].AsEnumerable().Any(row => project.ProjectId == row.Field<long>("ProjectId")))
                {
                    addRow(project, "Projects");
                }
            }
        }
        private void maptoscans(DataSet ds, long projectId)
        {

            var results = from scan in ds.Tables["scans"].AsEnumerable()
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
                if (!dataSet.Tables["Scans"].AsEnumerable().Any(row => scan.ProjectId == row.Field<long>("ProjectId") && scan.ScanId == row.Field<long>("ScanId")))
                {
                    addRow(scan, "Scans");
                }
            }
        }
        private void maptoresults(DataSet ds, long projectId)
        {
            var results = from query in ds.Tables["queries"].AsEnumerable()
                          join project in ds.Tables["projects"].AsEnumerable() on (long)query["Key_Project_Id"] equals (long)project["Key_Project_Id"]
                          join result in ds.Tables["results"].AsEnumerable() on (long)query["Key_Scan_Id"] equals (long)result["Key_Scan_Id"]
                          join node in ds.Tables["nodes"].AsEnumerable() on (long)query["Key_Scan_Id"] equals (long)node["Key_Scan_Id"]
                          where query.Field<long>("Key_Project_Id") == projectId
                          select new CxResult
                          {
                              FalsePositive = (string)result["Result_FalsePositive"],
                              NodeCodeSnippet = (string)node["PathNode_First_Snippet_Line_Code"],
                              NodeColumn = (int)node["PathNode_First_Column"],
                              NodeFileName = (string)node["PathNode_First_FileName"],
                              NodeId = (long)node["PathNode_First_NodeId"],
                              NodeLength = (int)node["PathNode_First_Length"],
                              NodeLine = (int)node["PathNode_First_Line"],
                              NodeName = (string)node["PathNode_First_Name"],
                              NodeType = (string)node["PathNode_First_Type"],
                              PathId = (int)result["Result_PathId"],
                              ProjectId = (long)project["Key_Project_Id"],
                              ProjectName = (string)project["Key_Project_Name"],
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
                              TeamName = (string)project["Key_Project_Team_Name"],
                              VulnerabilityId = String.Format("{0}{1:D4}", (long)result["Result_ResultId"], (int)result["Result_PathId"])
                          };
            foreach (CxResult result in results)
            {
                if (!dataSet.Tables["Results"].AsEnumerable().Any(row => result.ProjectId == row.Field<long>("ProjectId") && result.ScanId == row.Field<long>("ScanId") && result.VulnerabilityId == row.Field<string>("VulnerabilityId")))
                {
                    addRow(result, "Results");
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
