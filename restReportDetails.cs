using CxAPI_Store.dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using YamlDotNet.Core;


namespace CxAPI_Store
{
    class restReportDetails : IDisposable
    {
        public resultClass token;
        public string opLog;

        public restReportDetails(resultClass token)
        {
            this.token = token;
            opLog = token.dump_operation;
        }
        public bool opLogger(ProjectObject project, long scan)
        {
            bool a = false;
            bool b = false;
            bool c = false;

            foreach (string s in opLog.Split(':'))
            {
                if (s.Contains("projectid"))
                {
                    a = true;
                    continue;
                }
                if (s.Contains("projectname"))
                {
                    b = true;
                    continue;
                }
                if (s.Contains("scanid"))
                {
                    c = true;
                    continue;
                }
                if (a)
                {
                    if (project.id.Contains(s))
                        return true;
                    a = false;
                }
                if (b)
                {
                    if (project.name.Contains(s))
                        return true;
                    b = false;
                }
                if (c)
                {
                    if (scan == Convert.ToInt64(s))
                        return true;
                    c = false;
                }

            }
            return false;
        }
        public bool opLogger()
        {
            if (opLog.Contains(":break"))
                return true;
            return false;
        }

        public bool fetchReport()
        {
            DataStore store = new DataStore(token);
            string set_path = token.template_path;
            string fileName = token.template_file;
            if (token.debug) Console.WriteLine(@"Using configuration in path " + set_path);
            List<string> customFiles = new List<string>(Directory.GetFiles(set_path, fileName, SearchOption.AllDirectories));
            store.restoreDataSet();
            dynamic obj = outputGenerator.buildObject(token, store.dataSet);
            outputGenerator.useCsHtmlTemplate(token, "test.cshtml", obj, false, true);
            //foreach (string customFile in customFiles)
            //{
            //    fetchReportOptions(customFile, store, token);
            //   store.selectOption(customFile);
            //}
            return true;
        }
        public DataStore buildDataSet(bool noSave = false)
        {
            DataStore store = new DataStore(token);
            buildDataSet buildData = new buildDataSet(token, store);
            string set_path = token.template_path;
            fetchProjectFiles fetchProject = new fetchProjectFiles(token);
            fetchProject.fetchFilteredScans(token);
            string fileName = token.template_file;
            if (token.debug) Console.WriteLine(@"Using configuration in path " + set_path);
            List<string> customFiles = new List<string>(Directory.GetFiles(set_path, fileName, SearchOption.AllDirectories));
            string masterFile = token.master_path + token.os_path + "MasterTemplate.yaml";
            fetchResults(fetchProject, masterFile, buildData);
            if (!noSave) { store.saveDataSet(); }
            return store;
        }


        public void fetchResults(fetchProjectFiles fetchProject, string masterFile, buildDataSet build)
        {

            Dictionary<string, object> header = new Dictionary<string, object>();
            Dictionary<string, object> results = new Dictionary<string, object>();

            foreach (ProjectObject project in fetchProject.CxProjects)
            {

                var resultStatisticsValues = fetchProject.CxIdxResultStatistics[Convert.ToInt64(project.id)];
                var scanValues = new SortedDictionary<long, ScanObject>(fetchProject.CxIdxScans[Convert.ToInt64(project.id)]);
                var resultValues = fetchProject.CxIdxResults[Convert.ToInt64(project.id)];
                header = fetchHeaders(fetchProject, project);
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
                        if (opLogger(project, item.Key))
                        {
                            Console.WriteLine("ProjectName {0} ProjectId: {1} ScanId {2}", project.name, project.id, item.Key);
                            build.writeDictionary(token, results);
                        }
                        if (!opLogger())
                        {
                            build.fetchDetails(token, results, masterFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (token.debug && token.verbosity > 0) Console.WriteLine("Failed to load ProjectName {0} ProjectId: {1} ScanId {2}", project.name, project.id, item.Key);
                        Console.WriteLine("Failure fetching detail xml {0} : {1}", item.Key, ex.Message);
                    }
                }
            }
            return;
        }
        public Dictionary<string, object> fetchHeaders(fetchProjectFiles fetchProject, ProjectObject project)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            var scanValues = fetchProject.CxIdxScans[Convert.ToInt64(project.id)];
            var projectSettings = fetchProject.CxSettings[Convert.ToInt64(project.id)];
            var projectDetails = fetchProject.CxProjectDetail[Convert.ToInt64(project.id)];
            result = Flatten.DeserializeAndFlatten(project);
            var teamAndPreset = fetchProject.getTeamAndPresetNames(project.teamId, projectSettings.preset.id);
            result = Flatten.DeserializeAndFlatten(teamAndPreset, result);
            result = Flatten.DeserializeAndFlatten(projectSettings, result);
            result = Flatten.DeserializeAndFlatten(projectDetails, result);

            return result;
        }

        private bool fetchReportOptions(string customFile, DataStore store, resultClass token)
        {
            bool comment = false;


            foreach (string line in File.ReadLines(customFile))
            {
                if (line.StartsWith("//")) continue;
                if (line.StartsWith("/*"))
                {
                    comment = true;
                    continue;
                }
                if (line.StartsWith("*/"))

                {
                    comment = false;
                    continue;
                }
                if (comment) { continue; }


                string[] split = line.Split(":");
                int splitCount = split.Length;
                if (splitCount == 3)
                {
                    split[1] = String.Format("{0}:{1}", split[1], split[2]);
                    splitCount = 2;
                }
                if (splitCount != 2) { return false; };
                if (String.IsNullOrEmpty(split[1].Trim())) { continue; };

                if (split[0].Contains("CxReport."))
                {
                    switch (split[0])
                    {
                        case "CxReport.File.Name":
                            store.fileName = split[1].Trim();
                            break;
                        case "CxReport.File.Path":
                            store.filePath = split[1].Trim();
                            break;
                        case "CxReport.Output.Type":
                            store.outputType = split[1].Trim();
                            break;

                        default: break;
                    }
                }

            }

            return true;
        }

        public void Dispose()
        {

        }

    }
}

