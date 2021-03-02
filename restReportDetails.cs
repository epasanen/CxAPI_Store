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
            fetchProjectFiles fetchProject = new fetchProjectFiles(token);
            fetchProject.fetchFilteredScans(token);
            buildResults build = new buildResults(token);
            List<object> objList = new List<object>();


            foreach (ProjectObject project in fetchProject.CxProjects)
            {
                var scanValues = new SortedDictionary<long,ScanObject>(fetchProject.CxIdxScans[Convert.ToInt64(project.id)]);
                var resultValues = fetchProject.CxIdxResults[Convert.ToInt64(project.id)];
                foreach (var item in scanValues.OrderBy(i=>i.Key))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(resultValues[item.Key]);
                    string json = JsonConvert.SerializeXmlNode(doc);
                    Dictionary<string, object> xmlDict = Flatten.DeserializeAndFlatten(json);
                    objList.AddRange(build.fetchYMLDetails(token, xmlDict));
                    //fetchProject.writeDictionary(token, ymlDict);
                 
                }
            }
            csvHelper csv = new csvHelper();
            csv.writeCVSFile(objList, token);

            return true;
        }
        public void Dispose()
        {

        }

    }
}

