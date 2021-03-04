using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CxAPI_Store
{

    public class libraryClass
    {
        public Dictionary<string, object> header { get; set; }
        public Dictionary<string, object> scan { get; set; }
        public Dictionary<string, object> queries { get; set; }
        public Dictionary<string, object> results { get; set; }
        public Dictionary<string, object> pathNodes { get; set; }

        public libraryClass()
        {
            header = new Dictionary<string, object>();
            scan = new Dictionary<string, object>();
            queries = new Dictionary<string, object>();
            results = new Dictionary<string, object>();
            pathNodes = new Dictionary<string, object>();
        }
    }
    public class libraryClasses
    {
        public resultClass _token { get; set; }
        public string outputType { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }
        Dictionary<long, SortedDictionary<long, List<libraryClass>>> projectRows { get; set; }
        SortedDictionary<long, List<libraryClass>> scanRows { get; set; }
        public List<libraryClass> resultRows { get; set; }
        public libraryClasses(resultClass token)
        {
            _token = token;
            resultRows = new List<libraryClass>();
            projectRows = new Dictionary<long, SortedDictionary<long, List<libraryClass>>>();
            scanRows = new SortedDictionary<long, List<libraryClass>>();
        }

        public void storeScanResults(libraryClass results, Dictionary<string,object> keys)
        {
            long projectId = Convert.ToInt64(keys["_KeyProjectId"]);
            long scanId = Convert.ToInt64(keys["_KeyScanId"]);
            resultRows.Add(results);
            if (!scanRows.ContainsKey(scanId))
            {
                scanRows.Add(scanId,resultRows);
            }
            if (!projectRows.ContainsKey(projectId))
            {
                projectRows.Add(projectId, scanRows);
            }

        }

        public void  generateCsv()
        {
            List<object> objList = new List<object>();
            foreach(long projectId in projectRows.Keys)
            {
                SortedDictionary<long, List<libraryClass>> scanResults = projectRows[projectId];
                foreach(long scanId in scanResults.Keys)
                {
                    List<libraryClass> libraries = scanResults[scanId];
                    foreach (libraryClass library in libraries)
                    {
                        Dictionary<string,object> final = new Dictionary<string, object>();
                        final = final.Concat(library.header.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.scan.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.queries.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.results.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.pathNodes.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        objList.Add(Flatten.CreateFlattenObject(final));
                    }

                }
            }
            csvHelper csv = new csvHelper();
            csv.writeCVSFile(objList,_token);

        }
    }
}
