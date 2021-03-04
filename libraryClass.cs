using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CxAPI_Store
{

    public class libraryClass
    {
        public long projectId { get; set; }
        public long scanId { get; set; }
        public string teamName { get; set; }
        public string presetName { get; set; }
        public string fileName { get; set; }
        public DateTime? scanStartDate { get; set; }
        public long singularityId { get; set; }
        public string state { get; set; }  //not exploitable, etc.
        public string severity { get; set; } // high, medium, etc
        public DateTime? firstFoundDate { get; set; }
        public DateTime? lastFoundDate { get; set; }


        public Dictionary<string, object> project { get; set; }
        public Dictionary<string, object> scan { get; set; }
        public Dictionary<string, object> queries { get; set; }
        public Dictionary<string, object> results { get; set; }
        public Dictionary<string, object> pathNodes { get; set; }
        public Dictionary<string, object> keys { get; set; }

        public libraryClass()
        {
            project = new Dictionary<string, object>();
            scan = new Dictionary<string, object>();
            queries = new Dictionary<string, object>();
            results = new Dictionary<string, object>();
            pathNodes = new Dictionary<string, object>();
            keys = new Dictionary<string, object>();
        }
    }
    public class libraryClasses
    {
        public resultClass _token { get; set; }
        public string outputType { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }
        Dictionary<long, SortedDictionary<long, List<libraryClass>>> projectRows { get; set; }
        //SortedDictionary<long, List<libraryClass>> scanRows { get; set; }
        //public List<libraryClass> resultRows { get; set; }
        public libraryClasses(resultClass token)
        {
            _token = token;
            projectRows = new Dictionary<long, SortedDictionary<long, List<libraryClass>>>();
            // set defaults
            fileName = token.file_name;
            filePath = token.file_path;
            outputType = "CSV";
        }

        public void storeScanResults(libraryClass results, Dictionary<string,object> keys)
        {
            SortedDictionary<long, List<libraryClass>> scanRows = null;
            results.projectId = Convert.ToInt64(String.IsNullOrEmpty(keys["_KeyProjectId"].ToString()) ? "0" : keys["_KeyProjectId"].ToString());
            results.scanId = Convert.ToInt64(String.IsNullOrEmpty(keys["_KeyScanId"].ToString()) ? "0" : keys["_KeyScanId"].ToString());
            results.scanStartDate = DateTime.Parse(String.IsNullOrEmpty(keys["_KeyStartDate"].ToString()) ? "0" : keys["_KeyStartDate"].ToString());
            results.teamName = String.IsNullOrEmpty(keys["_KeyTeamName"].ToString()) ? " " : keys["_KeyTeamName"].ToString();
            results.presetName = String.IsNullOrEmpty(keys["_KeyPresetName"].ToString()) ? " " : keys["_KeyPresetName"].ToString();
            results.fileName = String.IsNullOrEmpty(keys["_KeyFileName"].ToString()) ? " " : keys["_KeyFileName"].ToString();

            if (!projectRows.ContainsKey(results.projectId)) { projectRows.Add(results.projectId, new SortedDictionary<long, List<libraryClass>>());}
            if (projectRows.ContainsKey(results.projectId)) { scanRows = projectRows[results.projectId]; }

            if (!scanRows.ContainsKey(results.scanId)) { scanRows.Add(results.scanId, new List<libraryClass>()); }
            if (scanRows.ContainsKey(results.scanId)) { scanRows[results.scanId].Add(results); }

        }

        public void generateCsv(string customFile)
        {
            List<object> objList = new List<object>();
            buildResults build = new buildResults(_token);

            foreach(long projectId in projectRows.Keys)
            {
                SortedDictionary<long, List<libraryClass>> scanResults = projectRows[projectId];
                foreach(long scanId in scanResults.Keys)
                {
                    List<libraryClass> libraries = scanResults[scanId];
                    foreach (libraryClass library in libraries)
                    {
                        Dictionary<string,object> final = new Dictionary<string, object>();
                        final = final.Concat(library.project.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.scan.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.queries.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.results.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.pathNodes.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = build.sortTemplate(final, customFile);
                        objList.Add(Flatten.CreateFlattenObject(final));
                    }

                }
            }
           

            csvHelper csv = new csvHelper();
            string fileName = this.filePath + this._token.os_path + this.fileName;
            csv.writeCVSFile(objList, fileName);

        }
 
    }
}
