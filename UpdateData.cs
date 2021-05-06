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
using static CxAPI_Store.CxConstant;

namespace CxAPI_Store
{
    public class UpdateData : IDisposable
    {
        //        DataSet dataSet;
        resultClass token;
        SQLiteMaster lite;
        SortedDictionary<string, bool> allKeys;
 
        DateTime runDate;

        public UpdateData(resultClass token)
        {
            this.token = token;
            //            dataSet = new DataSet();
            //            dataSet.Tables.Add(new DataTable(ProjectTable));
            //            dataSet.Tables.Add(new DataTable(ScanTable));
            //            dataSet.Tables.Add(new DataTable(ResultTable));

            allKeys = new SortedDictionary<string, bool>();
 
            lite = new SQLiteMaster(token);
            runDate = DateTime.UtcNow;

            if (token.initialize)
            {
                initAllTables();
            }
            getCopyofKeys();

        }


        private void getCopyofKeys()
        {
            //move keys into dictionary object
            DataSet ds = lite.InitJustPrimaryKeys();
            if (token.debug && token.verbosity > 0)
            {
                Console.WriteLine("{0} rows in table {1}", lite.GetRowCount(ProjectTable), ProjectTable);
                Console.WriteLine("{0} rows in table {1}", lite.GetRowCount(ScanTable), ScanTable);
                Console.WriteLine("{0} rows in table {1}", lite.GetRowCount(ResultTable), ResultTable);
            }
            lite.SelectIntoDataTable(ds.Tables[ProjectTable]);
            lite.SelectIntoDataTable(ds.Tables[ScanTable], "order by ScanId desc limit 100000");
            lite.SelectIntoDataTable(ds.Tables[ResultTable], "order by ScanId desc limit 100000");

            PrimaryKeystoDictionary(ds.Tables[ProjectTable]);
            PrimaryKeystoDictionary(ds.Tables[ScanTable]);
            PrimaryKeystoDictionary(ds.Tables[ResultTable]);
            ds.Clear();
            ds.Dispose();

        }
        private void PrimaryKeystoDictionary(DataTable table)
        {
            int rowcount = 0;
            foreach(DataRow dr in table.Rows)
            {
                string keyval = String.Join('-', dr.ItemArray);            
                allKeys.Add(keyval, true);
                if (rowcount++ > 10000)
                    break;
            }

        }
        private void AddPrimaryKeytoDictionary(Dictionary<string, object> primaryKeys)
        {
            List<string> Keys = primaryKeys.Select(x => x.Value.ToString()).ToList();
            string cat = String.Join('-', Keys.ToArray());
            if (!allKeys.ContainsKey(cat))
            {
                allKeys.Add(cat, true);
                if (token.debug && token.verbosity > 1)
                {
                    string keys = String.Join(',', primaryKeys.Select(x => x.Key).ToArray());
                    string values = String.Join(',', primaryKeys.Select(x => x.Value.ToString()).ToArray());
                    Console.WriteLine("Adding '{0}': {1} to unique keys ", keys, values);
                }
            }
        }
        private bool TestPrimaryKeyinDictionary(Dictionary<string,object> primaryKeys)
        {
            List<string> Keys = primaryKeys.Select(x => x.Value.ToString()).ToList();
            string cat = String.Join('-', Keys.ToArray());
            return allKeys.ContainsKey(cat);

        }

        //private bool restoreFullDataSet()
        //{
        //    initAllTables();
        //    Console.WriteLine("Table {0} contains {1} rows", MetaTable, selectFillSQLTable(MetaTable, ""));
        //    Console.WriteLine("Table {0} contains {1} rows", ProjectTable, selectFillSQLTable(ProjectTable, ""));
        //    Console.WriteLine("Table {0} contains {1} rows", ScanTable, selectFillSQLTable(ScanTable, ""));
        //    Console.WriteLine("Table {0} contains {1} rows", ResultTable, selectFillSQLTable(ResultTable, ""));
        //    return true;
        //}
        //private void saveToSQLite()
        //{
        //    if (token.initialize)
        //    {
        //        createSQLTable(ProjectTable);
        //        createSQLTable(ResultTable);
        //        createSQLTable(ScanTable);
        //        createSQLTable(MetaTable);

        //    }
        //    insertUpdateSQLTable(MetaTable);
        //    insertUpdateSQLTable(ProjectTable);
        //    insertUpdateSQLTable(ScanTable);
        //    insertUpdateSQLTable(ResultTable);
        //}

        //        private void createSQLTable(string tableName)
        //        {
        //            lite.CreateFromDataTable(dataSet.Tables[tableName]);
        //        }
        //        private void insertUpdateSQLTable(string tableName)
        //        {
        //            lite.InsertUpdateFromDataTable(dataSet.Tables[tableName]);
        //        }
        //        private void insertSQLTable(string tableName)
        //        {
        //            lite.InsertParametersDataSetToSQLite(dataSet.Tables[tableName]);
        //        }
        //        private int selectFillSQLTable(string tableName, string where = null)
        //        {
        //            DataTable temp = dataSet.Tables[tableName].Copy();
        //            dataSet.Tables.Remove(tableName);
        //            dataSet.Tables.Add(lite.SelectIntoDataTable(temp, where));
        //            return dataSet.Tables[tableName].Rows.Count;
        //        }
        private void initAllTables()
        {
            lite.InitAllSQLTables();
        }

        //private void initDataTable(object mapObject, string tableName, List<string> primaryKeys)
        //{
        //    if (dataSet.Tables.Contains(tableName)) dataSet.Tables.Remove(tableName);
        //    dataSet.Tables.Add(lite.InitializeDataSet(mapObject, tableName, primaryKeys));
        //}

        /*
        private bool addRow(object mapObject, string select)
        {
            if (token.debug && token.verbosity > 1) Console.WriteLine("Adding to table {0}", select);
            DataTable table = dataSet.Tables[select];
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            DataRow dr = table.NewRow();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                //var prop = property.PropertyType;
                var value = property.GetValue(mapObject, null);
                if (token.debug && token.verbosity > 2) Console.WriteLine("Adding '{0}':{1} to table {2}", name, value, select);
                dr[name] = value;
            }
            table.Rows.Add(dr);
            return true;
        }
        private bool addMetaData(string fileName)
        {
            if (token.debug && token.verbosity > 0) Console.WriteLine("Complete reading file {0}", fileName);
            DataTable table = dataSet.Tables[MetaTable];
            DataRow dr = table.NewRow();
            dr["TenantName"] = token.tenant;
            dr["ArchivalFilePath"] = token.archival_path;
            dr["FileName"] = fileName;
            dr["LastRunDate"] = runDate;
            table.Rows.Add(dr);
            return true;
        }
        */

        private bool testMetaData(string fileName)
        {
            List<KeyValuePair<string, object>> kvp = new List<KeyValuePair<string, object>>();
            kvp.Add(new KeyValuePair<string, object>("FileName", fileName));
            return lite.TestforPrimaryKeys(MetaTable, kvp);
        }
        private bool TestAndSetPrimaryKeys(string tableName, Dictionary<string,object> primaryKeys, bool delete = false)
        {
            long deleteCount = 0;
            if (TestPrimaryKeyinDictionary(primaryKeys))
            {
                if (!delete) return true;
                else
                {
                    deleteCount = lite.DeleteUsingPrimaryKeys(tableName, primaryKeys);
                }
            }
            else
            {
                AddPrimaryKeytoDictionary(primaryKeys);
            }
            return false;
        }
        private Dictionary<string,object> AddToDict(Dictionary<string,object> mapDictionary, object mapObject, List<object> primaryKeys)
        {
            string cat = String.Join('-', primaryKeys.ToArray());
            if (!mapDictionary.ContainsKey(cat))
            {
                mapDictionary.Add(cat, mapObject);
                if (token.debug && token.verbosity > 1)
                {
                    Console.WriteLine("Adding '{0}' to local ", cat);
                }
            }
            else
            {
                mapDictionary.Remove(cat);
                mapDictionary.Add(cat, mapObject);
                if (token.debug && token.verbosity > 1)
                {
                    Console.WriteLine("Replacing '{0}' in local ", cat);
                }
            }
 
            return mapDictionary;
        }

        private bool addObject(string tableName, List<object> mapObjects, string fileName)
        {
            lite.InsertObjectListToSQLite(tableName, mapObjects, token.max_write == 0 ? mapObjects.Count : token.max_write) ;
            if (!String.IsNullOrEmpty(fileName)) addObjectMetaData(fileName);
            return true;
        }

        private bool addObjectMetaData(string fileName)
        {
            if (token.debug && token.verbosity > 0) Console.WriteLine("Processed file {0}", fileName);
            var meta = new CxMetaData()
            {
                ArchivalFilePath = token.archival_path,
                FileName = fileName,
                LastRunDate = runDate,
                TenantName = token.tenant
            };
            lite.InsertObjectListToSQLite(MetaTable, new List<object> { meta });
            return true;
        }
        public void loadDataSet(bool startNew = true)
        {
            if (token.debug && token.verbosity > 0 && startNew) Console.WriteLine("Initializing database");

            //           if (!startNew)
            //               restoreFullDataSet();

            if (Directory.EnumerateFiles(token.archival_path, "sast_project_info.*.log").Any())
            {
                if (token.debug && token.verbosity > 0) Console.WriteLine("Using data from CxAnalytix");
                loadAnalytix();
            }
            else
            {
                if (token.debug && token.verbosity > 0) Console.WriteLine("Using data from CxStore");
                loadRawFiles();
            }

        }
        private void loadAnalytix()
        {
            getProjects();
            getScans();
            getResults();
            //saveToSQLite();
        }
        private void loadRawFiles()
        {
            getRawData();
            //saveToSQLite();
        }

        private bool getProjects()
        {
            Dictionary<string, object> projects = new Dictionary<string, object>();
            Console.WriteLine("Loading table {0}", ProjectTable);
            Spinner.Run(() =>
            {
                var jsonFiles = GetOrderedFileList(token.archival_path, "sast_project_info.*.log");
                //var jsonFiles = Directory.EnumerateFiles(token.archival_path, "sast_project_info.*.log");
                foreach (string filename in jsonFiles)
                {
                    if (testMetaData(filename))
                    {
                        foreach (string content in File.ReadAllLines(filename))
                        {
                            CxProjectJson project = JsonConvert.DeserializeObject<CxProjectJson>(content);
                            if (!TestAndSetPrimaryKeys(ProjectTable, new Dictionary<string, object>() {{ "ProjectId", project.ProjectId }}, true))
                            {
                                AddToDict(projects, project.convertObject(), new List<object>() { project.ProjectId });
                            }
                        }
                        addObject(ProjectTable, new List<object>(projects.Values), filename);
                        projects.Clear();
                    }
                }
            });
            return true;
        }
        private bool getScans()
        {
            Dictionary<string, object> scans = new Dictionary<string, object>();
            Console.WriteLine("Loading table {0}", ScanTable);
            Spinner.Run(() =>
            {
                var jsonFiles = GetOrderedFileList(token.archival_path, "sast_scan_summary.*.log");
                //var jsonFiles = Directory.EnumerateFiles(token.archival_path, "sast_scan_summary.*.log");
                foreach (string filename in jsonFiles)
                {
                    if (testMetaData(filename))
                    {
                        foreach (string content in File.ReadAllLines(filename))
                        {
                            CxScan scan = JsonConvert.DeserializeObject<CxScan>(content);
                            if (!TestAndSetPrimaryKeys(ScanTable, new Dictionary<string, object>() { { "ProjectId", scan.ProjectId }, { "ScanId", scan.ScanId } }, true))
                            {
                                AddToDict(scans, scan, new List<object>() { scan.ProjectId, scan.ScanId });
                            }
                        }
                        addObject(ScanTable, new List<object>(scans.Values), filename);
                        scans.Clear();
                    }
                }
            });
            return true;
        }
        private bool getResults()
        {
            Dictionary<string, object> results = new Dictionary<string, object>();
            Console.WriteLine("Loading table {0}", ResultTable);
            Spinner.Run(() =>
            {
                var jsonFiles = GetOrderedFileList(token.archival_path, "sast_scan_detail.*.log");
                //var jsonFiles = Directory.EnumerateFiles(token.archival_path, "sast_scan_detail.*.log");
                foreach (string filename in jsonFiles)
                {
                    if (testMetaData(filename))
                    {
                        foreach (string content in File.ReadAllLines(filename))
                        {
                            CxResult result = JsonConvert.DeserializeObject<CxResult>(content);
                            result.GenerateFileHash();
                            if (!TestAndSetPrimaryKeys(ResultTable, new Dictionary<string, object>() { { "ProjectId", result.ProjectId }, { "VulnerabilityId", result.VulnerabilityId }, { "SimilarityId", result.SimilarityId },{ "FileNameHash", result.FileNameHash }}, true))
                                AddToDict(results, result, new List<object>() { result.ProjectId, result.VulnerabilityId, result.SimilarityId, result.FileNameHash });
                        }
                        addObject(ResultTable, new List<object>(results.Values), filename);
                        results.Clear();
                    }
                }
            });

            return true;
        }
        public IEnumerable<string> GetOrderedFileList(string path, string filePattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            var dirs = (from file in dir.EnumerateFiles(filePattern)
                        orderby file.CreationTime ascending
                        select path + token.os_path + file.Name).Distinct();
            return dirs;
        }

        //private bool getProjects()
        //{
        //    Console.WriteLine("Loading table {0}", ProjectTable);
        //    Spinner.Run(() =>
        //    {
        //        var jsonFiles = Directory.EnumerateFiles(token.archival_path, "sast_project_info.*.log");
        //        foreach (string filename in jsonFiles)
        //        {
        //            if (testMetaData(filename))
        //            {
        //                foreach (string content in File.ReadAllLines(filename))
        //                {
        //                    CxProjectJson project = JsonConvert.DeserializeObject<CxProjectJson>(content);
        //                    if (!dataSet.Tables[ProjectTable].AsEnumerable().Any(row => project.ProjectId == row.Field<long>("ProjectId")))
        //                    {
        //                        addRow(project.convertObject(), ProjectTable);
        //                    }
        //                }
        //                addMetaData(filename);
        //            }
        //        }
        //    });
        //    return true;
        //}
        //private bool getScans()
        //{
        //    Console.WriteLine("Loading table {0}", ScanTable);
        //    Spinner.Run(() =>
        //    {
        //        var jsonFiles = Directory.EnumerateFiles(token.archival_path, "sast_scan_summary.*.log");
        //        foreach (string filename in jsonFiles)
        //        {
        //            if (testMetaData(filename))
        //            {
        //                foreach (string content in File.ReadAllLines(filename))
        //                {
        //                    CxScan scans = JsonConvert.DeserializeObject<CxScan>(content);
        //                    if (!dataSet.Tables[ScanTable].AsEnumerable().Any(row => scans.ProjectId == row.Field<long>("ProjectId") && scans.ScanFinished == row.Field<DateTime>("ScanFinished")))
        //                    {
        //                        addRow(scans, ScanTable);
        //                    }
        //                }
        //                addMetaData(filename);
        //            }
        //        }
        //    });
        //    return true;
        //}
        //private bool getResults()
        //{
        //    Console.WriteLine("Loading table {0}", ResultTable);
        //    Spinner.Run(() =>
        //    {
        //        var jsonFiles = Directory.EnumerateFiles(token.archival_path, "sast_scan_detail.*.log");
        //        foreach (string filename in jsonFiles)
        //        {
        //            if (testMetaData(filename))
        //            {
        //                foreach (string content in File.ReadAllLines(filename))
        //                {
        //                    CxResult results = JsonConvert.DeserializeObject<CxResult>(content);
        //                    if (!dataSet.Tables[ResultTable].AsEnumerable().Any(row => results.ProjectId == row.Field<long>("ProjectId") && results.VulnerabilityId == row.Field<long>("VulnerabilityId") && results.SimilarityId == row.Field<long>("SimilarityId")))
        //                    {
        //                        addRow(results, ResultTable);
        //                    }
        //                }
        //                addMetaData(filename);
        //            }
        //        }
        //    });

        //    return true;
        //}

        //private bool testMetaData(string fileName)
        //{
        //    if (testForFileName(fileName)) return false;
        //    // addMetaData(fileName);
        //    return true;
        //}

        //private bool testForFileName(string fileName)
        //{
        //    DataView lastfile = new DataView(dataSet.Tables[MetaTable]);
        //    lastfile.RowFilter = String.Format("FileName = '{0}'", fileName);
        //    return (lastfile.Count > 0);
        //}

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
            List<object> objList = new List<object>();
            IEnumerable<CxProject> results = from projects in ds.Tables["projects"].AsEnumerable()
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
                if (!TestAndSetPrimaryKeys(ProjectTable, new Dictionary<string, object>() { { "ProjectId", project.ProjectId } }, true))
                {
                    objList.Add(project);
                }
            }
            addObject(ProjectTable, objList, null);
            objList.Clear();
        }
        private void maptoscans(DataSet ds, long projectId)
        {
            List<object> objList = new List<object>();
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
                if (!TestAndSetPrimaryKeys(ProjectTable, new Dictionary<string, object>() { { "ProjectId", scan.ProjectId }, { "ScanId", scan.ScanId } }, true))
                {
                    objList.Add(scan);
                }
            }
            addObject(ScanTable, objList, null);
            objList.Clear();
        }
        private void maptoresults(DataSet ds, long projectId)
        {
            List<object> objList = new List<object>();
            try
            {
                IEnumerable<CxResult> results = from query in ds.Tables["queries"].AsEnumerable()
                                                join scan in ds.Tables["scans"].AsEnumerable() on (long)query["Key_Scan_Id"] equals (long)scan["Key_Scan_Id"]
                                                join result in ds.Tables["results"].AsEnumerable() on ((long)query["Key_Scan_Id"], (long)query["Key_Query_Id"]) equals ((long)result["Key_Scan_Id"], (long)result["Key_Query_Id"])
                                                join node in ds.Tables["nodes"].AsEnumerable() on ((long)result.Field<long>("Key_Result_NodeId"), (long)result["Key_Result_SimilarityId"]) equals ((long)node.Field<long>("Key_Result_NodeId"), (long)node["Key_Result_SimilarityId"])
                                                where scan.Field<long>("Key_Project_Id") == projectId
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

                foreach (CxResult result in results)
                {
                    result.GenerateFileHash();
                    if (token.debug && token.verbosity > 2) listObject(result);
                    if (!TestAndSetPrimaryKeys(ResultTable, new Dictionary<string, object>() { { "ProjectId", result.ProjectId }, { "VulnerabilityId", result.VulnerabilityId }, { "SimilarityId", result.SimilarityId }, { "FileNameHash", result.FileNameHash } }, true))
                        objList.Add(result);
                }
                addObject(ResultTable, objList, null);
                objList.Clear();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

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
            fetchProjectFiles fetchProjects = new fetchProjectFiles(token);
            fetchProjects.fetchFilteredScans(token);

            foreach (ProjectObject project in fetchProjects.CxProjects)
            {
                DataStore store = new DataStore(token);
                buildDataSet buildData = new buildDataSet(token, store);
                string masterFile = token.master_path + token.os_path + "MasterTemplate.yaml";
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
            var projects = build.fetchProject(token, header, masterFile);
            foreach (var item in scanValues.OrderBy(i => i.Key))
            {
                try
                {
                    if (token.debug && token.verbosity > 0) Console.WriteLine("ProjectName {0} ProjectId: {1} ScanId {2}", project.name, project.id, item.Key);
                    results = Flatten.DeserializeAndFlatten(scanValues[item.Key], new Dictionary<string, object>());
                    results = Flatten.DeserializeAndFlatten(resultStatisticsValues[item.Key], results);
                    XmlDocument doc = new XmlDocument();
                    string xml = resultValues[item.Key];
                    if (xml.Contains("~~Error")) { throw new InvalidOperationException("XML file not properly downloaded"); }
                    doc.LoadXml(resultValues[item.Key]);
                    string json = JsonConvert.SerializeXmlNode(doc);
                    Dictionary<string, object> xmlDict = Flatten.DeserializeAndFlatten(json);
                    results = Flatten.DeserializeAndFlatten(xmlDict, results);
                    build.fetchDetails(token, results, masterFile);
                }
                catch (Exception ex)
                {
                    if (token.debug && token.verbosity > 0) Console.WriteLine("Failed to load ProjectName {0} ProjectId: {1} ScanId {2}", project.name, project.id, item.Key);
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
