using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;


namespace CxAPI_Store
{
    class restReportSummary : IDisposable
    {
        public resultClass token;

        public restReportSummary(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReport()
        {
            fetchProjectFiles fetchProject = new fetchProjectFiles(token);
            fetchProject.fetchFilteredScans(token);
            buildResults build = new buildResults(token);

            List<object> objList = new List<object>();
            foreach(ProjectObject project in fetchProject.CxProjects)
            {
                var scanValues = fetchProject.CxIdxScans[Convert.ToInt64(project.id)];
                var resultStatisticsValues = fetchProject.CxIdxResultStatistics[Convert.ToInt64(project.id)];
                var projectSettings = fetchProject.CxSettings[Convert.ToInt64(project.id)];
                foreach(long key in scanValues.Keys)
                {
                    Dictionary<string, object> result = Flatten.DeserializeAndFlatten(project);
                    var teamAndPreset = fetchProject.getTeamAndPresetNames(project.teamId, projectSettings.preset.id);
                    result = Flatten.DeserializeAndFlatten(teamAndPreset, result);
                    result = Flatten.DeserializeAndFlatten(projectSettings, result);
                    result = Flatten.DeserializeAndFlatten(scanValues[key], result);
                    result = Flatten.DeserializeAndFlatten(resultStatisticsValues[key],result);
                    //fetchProject.writeDictionary(token, result,"dump_summary.txt");
                    objList.AddRange(build.fetchYMLSummary(token, result));
                }
            }
            csvHelper csv = new csvHelper();
            csv.writeCVSFile(objList,token);

            return true;
        }
        public void Dispose()
        {

        }

    }

}
