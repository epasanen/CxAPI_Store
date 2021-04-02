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
    public class fetchAnalytix
    {
        public resultClass _token;
        public Dictionary<long, CxProject> CxProjects;
        public Dictionary<long, SortedDictionary<long, CxScan>> CxScans;
        public Dictionary<long, SortedDictionary<long, List<CxResult>>> CxResults;

        public fetchAnalytix(resultClass token)
        {
            CxProjects = new Dictionary<long, CxProject>();
            CxScans = new Dictionary<long, SortedDictionary<long, CxScan>>();
            CxResults = new Dictionary<long, SortedDictionary<long, List<CxResult>>>();
            _token = token;

        }

        public void loadAnalytix()
        {
            getProjects();
            getScans();
            getResults();
        }
        public void loadRawFiles()
        {
            getRawData();
        }
        private bool getProjects()
        {
            var jsonFiles = Directory.EnumerateFiles(_token.archival_path, "sast_project_info.*.log");
            foreach (string filename in jsonFiles)
            {
                foreach (string content in File.ReadAllLines(filename))
                {
                    CxProject project = JsonConvert.DeserializeObject<CxProject>(content);
                    if (!CxProjects.ContainsKey(project.ProjectId))
                    {
                        CxProjects.Add(project.ProjectId, project);
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
                    CxScan scan = JsonConvert.DeserializeObject<CxScan>(content);
                    if (!CxScans.ContainsKey(scan.ProjectId))
                    {
                        CxScans.Add(scan.ProjectId, new SortedDictionary<long, CxScan>());
                    }
                    if (!CxScans[scan.ProjectId].ContainsKey(scan.ScanId))
                    {
                        CxScans[scan.ProjectId].Add(scan.ScanId, scan);
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
                    CxResult result = JsonConvert.DeserializeObject<CxResult>(content);
                    if (!CxResults.ContainsKey(result.ProjectId))
                    {
                        CxResults.Add(result.ProjectId, new SortedDictionary<long, List<CxResult>>());
                    }
                    if (!CxResults[result.ProjectId].ContainsKey(result.ScanId))
                    {
                        CxResults[result.ProjectId].Add(result.ScanId, new List<CxResult>());
                    }
                    CxResults[result.ProjectId][result.ScanId].Add(result);

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
            var template = readAnalytixTemplate();
            maptoprojects(template, dataSet, projectId);

        }

        private CxProject maptoprojects(Dictionary<string, object> template, DataSet dataSet, long projectId)
        {
            var results = from projects in dataSet.Tables["projects"].AsEnumerable()
                          where projects.Field<long>("Project_Id") == projectId
                          select new
                          {
                              ProjectId = (long)projects["Project_ID"],
                              Policies = "compute{Dictionary(unk, unk)}",
                              TeamName = (string)projects["Project_Team_Name"],
                              Preset = (long)projects["Project_Preset_Name"],
                              CustomFields = "compute{ Dictionary(Project_customFields_~node_name, Project_customFields_~node_value)}",
                              SastLastScanDate = "compute{ Query(summaries, orderby = Summary_DateAndTime_FinishedOn)}",
                              SastScans = "compute{ Query(summaries, count = Summary_DateAndTime_FinishedOn)}"
                          };
            return (CxProject)mapKeystoValues(results, new CxProject());
        }
        private CxScan maptoscans(DataSet dataSet, long projectId)
        {

            IEnumerable results = from scan in dataSet.Tables["scans"].AsEnumerable()
                          join summary in dataSet.Tables["summaries"].AsEnumerable() on (long)scan["Key_Scan_Id"] equals (long)summary["Key_Scan_Id"]
                          where scan.Field<long>("Project_Id") == projectId
                          select new
                          {
                              ScanStarted = (DateTime)summary["Summary_DateAndTime_StartedOn"],
                              ScanFinished = (DateTime)summary["Summary_DateAndTime_FinishedOn"],
                              EngineStart = (DateTime)summary["Summary_DateAndTime_EngineStartedOn"],
                              EngineFinished = (DateTime)summary["Summary_DateAndTime_EngineFinishedOn"],
                              FileCount = (DateTime)summary["Summary_ScanState_LinesOfCode"],
                              LinesOfCode = (int)summary["Summary_ScanState_FilesCount"],
                              FailedLinesOfCode = (int)summary["Summary_ScanState_FailedLinesOfCode"],
                              CxVersion = (string)summary["Summary_ScanState_CxVersion"],
                              Languages = "compute{ List(Summary_ScanState_LanguageStateCollection_~node_LanguageName)}",
                              Initiator = (string)summary["Summary_InitiatorName"],
                              ScanRisk = (DateTime)summary["Summary_ScanRisk"],
                              ScanRiskSeverity = (DateTime)summary["Summary_ScanRiskSeverity"],
                              High = (DateTime)summary["Summary_HighSeverity"],
                              Medium = (DateTime)summary["Summary_MediumSeverity"],
                              Low = (DateTime)summary["Summary_LowSeverity"],
                              Info = (DateTime)summary["Summary_InfoSeverity"],
                              ProjectId = (DateTime)scan["Scan_ProjectId"],
                              ProjectName = (DateTime)scan["Scan_ProjectName"],
                              DeepLink = (DateTime)scan["DeepLink"],
                              ScanStart = (DateTime)scan["Scan_ScanStart"],
                              Preset = (DateTime)scan["Scan_Preset"],
                              ScanTime = (DateTime)scan["Scan_ScanTime"],
                              Product = "compute{System.Format(\"{0}\",\"SAST\")}",
                              ReportCreationTime = (DateTime)scan["Scan_ReportCreationTime"],
                              TeamName = (DateTime)scan["Scan_Team"],
                              ScanComments = (DateTime)scan["Scan_ScanComments"],
                              ScanType = (DateTime)scan["Scan_ScanType"],
                              SourceOrigin = (DateTime)scan["Scan_SourceOrigin"]
                          };
            return (CxProject)mapKeystoValues(results, new CxProject());

        }

        private object mapKeystoValues(IEnumerable dr, object target)
        {
            foreach (PropertyInfo prop in target.GetType().GetProperties())
            {
                var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                string property = prop.Name;
                if (dr.
                {
                    if (key.Contains("compute"))
                    {
                        target = mapComputedValues(target, dr, key, prop);
                    }
                    else
                    {
                        if (dr.Table.Columns.Contains(key))
                        {
                            var result = dr[key];
                            if (_token.debug && _token.verbosity > 0) Console.WriteLine("Target {0} column {1} value {2}", target, key, result);

                            if (type == typeof(DateTimeOffset))
                            {
                                prop.SetValue(target, result is DBNull ? DateTimeOffset.MinValue : (DateTimeOffset)result);
                            }
                            else if (type == typeof(DateTime))
                            {
                                prop.SetValue(target, result is DBNull ? DateTime.MinValue : (DateTime)result);
                            }
                            else if (type == typeof(long))
                            {
                                prop.SetValue(target, result is DBNull ? 0 : Convert.ToInt64(result));
                            }
                            else if (type == typeof(int))
                            {
                                prop.SetValue(target, result is DBNull ? 0 : Convert.ToInt32(result));
                            }
                            else if (type == typeof(string))
                            {
                                prop.SetValue(target, result is DBNull ? String.Empty : result.ToString());
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine(String.Format("Cannot find column {0} in table {1}", key, dr.Table.TableName));
                        }
                    }

                }
                else
                {
                    Console.Error.WriteLine(String.Format("Cannot find {0} in template file"), property);
                    return null;
                }
            }

            return target;
        }

        private object mapComputedValues(object target, DataRow dr, string value, PropertyInfo prop)
        {
            string strip = value.Substring(value.IndexOf('{') + 1, (value.LastIndexOf('}') - (value.IndexOf('{') + 1)));
            string values = strip.Substring(strip.IndexOf('(') + 1, (strip.LastIndexOf(')') - (strip.IndexOf('(') + 1)));
            if (strip.Contains("Dictionary"))
            {
                prop.SetValue(target, mapDictionary(values, dr));
            }
            else if (strip.Contains("List"))
            {
                prop.SetValue(target, mapList(values, dr));
            }
            else if (strip.Contains("String.Format"))
            {
                prop.SetValue(target, mapStringFormat(values, dr));
            }
            else if (strip.Contains("Query"))
            {
                prop.SetValue(target, mapQuery(values, dr));
            }
            return target;

        }
        private Dictionary<string, object> mapDictionary(string values, DataRow dr)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            for (int i = 0; i < 10; i++)
            {
                string repl = values.Replace("~node", i.ToString());
                var keyval = repl.Split(',');
                if (dr.Table.Columns.Contains(keyval[0].Trim()))
                {
                    if (!String.IsNullOrEmpty(dr[keyval[0]].ToString().Trim()))
                        dict.Add(dr[keyval[0].Trim()].ToString(), dr[keyval[1].Trim()]);
                }
                else
                {
                    break;
                }
            }
            return dict;
        }
        private List<object> mapList(string values, DataRow dr)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < 10; i++)
            {
                string repl = values.Replace("~node", i.ToString());
                if (dr.Table.Columns.Contains(repl))
                {
                    list.Add(dr[repl]);
                }
                else
                {
                    break;
                }
            }
            return list;
        }
        private string mapStringFormat(string values, DataRow dr)
        {
            var split = values.Split(',').ToList();
            string format = split[0];
            split.Remove(format);
            return (String.Format(format, split.ToArray()));
        }
        private object mapQuery(string values, DataRow dr)
        {
            var split = values.Split(',').ToList();
            DataTable dt = dr.Table.DataSet.Tables[split[0]];
            var field = split[1].Split('=');
            if (field[0].Contains("orderby"))
            {
                return dt.AsEnumerable().Select(s => s.Field<DateTime>(field[1])).OrderByDescending(f => f).FirstOrDefault();
            }
            if (field[0].Contains("count"))
            {
                return dt.AsEnumerable().Select(s => s.Field<DateTime>(field[1])).Count();
            }
            return null;

        }


        /*      private CxProject maptoscans(Dictionary<string, object> template, DataSet dataSet, long projectId)
              {
                  var project = new CxProject();
                  var dt1 = dataSet.Tables["projects"];
                  var results = from table1 in dt1.AsEnumerable()
                                join table2 in dt2.AsEnumerable() on (int)table1["CustID"] equals (int)table2["CustID"]
                                select new
                                {
                                    CustID = (int)table1["CustID"],
                                    ColX = (int)table1["ColX"],
                                    ColY = (int)table1["ColY"],
                                    ColZ = (int)table2["ColZ"]
                                };

              }

              private CxProject maptoscans(Dictionary<string, object> template, DataSet dataSet, long projectId)
              {
                  var project = new CxProject();
                  var dt1 = dataSet.Tables["projects"];
                  var results = from table1 in dt1.AsEnumerable()
                                select new
                                {
                                    CustID = (int)table1["CustID"],
                                    ColX = (int)table1["ColX"],
                                    ColY = (int)table1["ColY"],
                                    ColZ = (int)table2["ColZ"]
                                };

              }
              /*
          foreach (var item in results)
          {
              Console.WriteLine(String.Format("ID = {0}, ColX = {1}, ColY = {2}, ColZ = {3}", item.CustID, item.ColX, item.ColY, item.ColZ));
          }
          Console.ReadLine();
                        foreach (var car in carList)
                        {
                            foreach (PropertyInfo prop in car.GetType().GetProperties())
                            {
                                var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                if (type == typeof(DateTime))
                                {
                                    Console.WriteLine(prop.GetValue(car, null).ToString());
                                }
                            }
                        }
              */



        private Dictionary<string, object> readAnalytixTemplate()
        {
            Dictionary<string, object> template = new Dictionary<string, object>();
            string masterFile = _token.master_path + _token.os_path + "AnalytixTemplate.yaml";
            foreach (string line in File.ReadLines(masterFile))
            {
                string[] keyval = line.Split(':');
                if (keyval.Length == 2)
                {
                    template.Add(keyval[0].Trim(), keyval[1].Trim());
                }
            }

            return template;
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
    }
}
