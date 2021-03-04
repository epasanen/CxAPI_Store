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


namespace CxAPI_Store
{
    class restReportDetails : IDisposable
    {
        public resultClass token;

        public restReportDetails(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReport()
        {

            string set_path = token.template_path; 
            fetchProjectFiles fetchProject = new fetchProjectFiles(token);
            fetchProject.fetchFilteredScans(token);

            string fileName = token.template_file;
            if (token.debug) Console.WriteLine(@"Using configuration in path " + set_path);
            List<string> customFiles = new List<string>(Directory.GetFiles(set_path, fileName, SearchOption.AllDirectories));
            foreach (string customFile in customFiles)
            {
                libraryClasses results = fetchResults(fetchProject, customFile);
                results.generateCsv();

            }
            return true;
        }


        public libraryClasses fetchResults(fetchProjectFiles fetchProject, string customFile)
        {
            libraryClasses store = new libraryClasses(token);
            buildResults build = new buildResults(token);
            Dictionary<string, object> result = new Dictionary<string, object>();

            foreach (ProjectObject project in fetchProject.CxProjects)
            {
                var resultStatisticsValues = fetchProject.CxIdxResultStatistics[Convert.ToInt64(project.id)];
                var scanValues = new SortedDictionary<long, ScanObject>(fetchProject.CxIdxScans[Convert.ToInt64(project.id)]);
                var resultValues = fetchProject.CxIdxResults[Convert.ToInt64(project.id)];
                result = fetchHeaders(fetchProject, project);
                foreach (var item in scanValues.OrderBy(i => i.Key))
                {
                    result = Flatten.DeserializeAndFlatten(scanValues[item.Key], result);
                    result = Flatten.DeserializeAndFlatten(resultStatisticsValues[item.Key], result);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(resultValues[item.Key]);
                    string json = JsonConvert.SerializeXmlNode(doc);
                    Dictionary<string, object> xmlDict = Flatten.DeserializeAndFlatten(json);
                    result = Flatten.DeserializeAndFlatten(xmlDict, result);
                    //fetchProject.writeDictionary(token, ymlDict);
                    store = build.fetchDetails(token, result, customFile, store);
                }
            }
            return store;
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
        public void Dispose()
        {

        }

    }
}

