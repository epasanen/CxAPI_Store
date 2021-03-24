using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
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
        public Dictionary<string, object> result { get; set; }
        public Dictionary<string, object> keys { get; set; }

        public libraryClass()
        {
            project = new Dictionary<string, object>();
            scan = new Dictionary<string, object>();
            result = new Dictionary<string, object>();
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
      
        public List<Dictionary<string, object>> projectList;
        public List<Dictionary<string, object>> scanList;
        public List<Dictionary<string, object>> resultList;

        public libraryClasses(resultClass token)
        {
            _token = token;
            projectList = new List<Dictionary<string, object>>();
            scanList = new List<Dictionary<string, object>>();
            resultList = new List<Dictionary<string, object>>();
            projectRows = new Dictionary<long, SortedDictionary<long, List<libraryClass>>>();
            // set defaults
            fileName = token.file_name;
            filePath = token.file_path;
            outputType = "CSV";
        }

        public void storeScanResults(libraryClass results, Dictionary<string, object> keys)
        {
            SortedDictionary<long, List<libraryClass>> scanRows = null;
            results.projectId = Convert.ToInt64(!keys.ContainsKey("Key_Project_Id") ? "0" : keys["Key_Project_Id"].ToString());
            results.scanId = Convert.ToInt64(!keys.ContainsKey("Key_Scan_Id") ? "0" : keys["Key_Scan_Id"].ToString());
            results.scanStartDate = DateTime.Parse(!keys.ContainsKey("Key_Start_Date") ? DateTime.MinValue.ToShortDateString() : keys["Key_Start_Date"].ToString());
            results.teamName = !keys.ContainsKey("Key_Project_Team_Name") ? " " : keys["Key_Project_Team_Name"].ToString();
            results.presetName = !keys.ContainsKey("Key_Project_Preset_Name") ? " " : keys["Key_Project_Preset_Name"].ToString();
            results.fileName = !keys.ContainsKey("Key_Result_FileName") ? " " : keys["Key_Result_FileName"].ToString();

            if (!projectRows.ContainsKey(results.projectId)) { projectRows.Add(results.projectId, new SortedDictionary<long, List<libraryClass>>()); }
            if (projectRows.ContainsKey(results.projectId)) { scanRows = projectRows[results.projectId]; }

            if (!scanRows.ContainsKey(results.scanId)) { scanRows.Add(results.scanId, new List<libraryClass>()); }
            if (scanRows.ContainsKey(results.scanId)) { scanRows[results.scanId].Add(results); }

        }

        public void selectOutput(string customFile)
        {
            List<Dictionary<string,object>> objList = generateGenericOutput(customFile);
            switch (this.outputType.ToLower())
            {
                case "csv":
                case "pdf":
                case "xlsx":
                    generateFlatOutput(objList, customFile);
                    break;
                case "datatable":
                    generateDataTable(objList, customFile);
                    break;
            }
    
        }
        public List<Dictionary<string, object>> generateGenericOutput(string customFile)
        {
            List<Dictionary<string, object>> objList = new List<Dictionary<string, object>>();

            foreach (long projectId in projectRows.Keys)
            {
                SortedDictionary<long, List<libraryClass>> scanResults = projectRows[projectId];
                foreach (long scanId in scanResults.Keys)
                {
                    List<libraryClass> libraries = scanResults[scanId];
                    foreach (libraryClass library in libraries)
                    {
                        projectList.Add(library.project);
                        scanList.Add(library.scan);

                        Dictionary<string, object> final = new Dictionary<string, object>();
                        final = final.Concat(library.project.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.scan.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        final = final.Concat(library.result.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        objList.Add(final);
                    }
                
                }
            }
            return objList;
        }
        public void generateFlatOutput(List<Dictionary<string,object>> objList, string customFile)
        {
            List<MasterDTO> masterList = new List<MasterDTO>();
            foreach (var obj in objList)
            {
                masterList.Add(mapToMasterDTO(obj));
            }
            string ext = this.outputType.ToLower();
            string file = String.Format("{0}.{1}",this.fileName.Substring(0, this.fileName.LastIndexOf('.')),ext);

            string fileName = this.filePath + this._token.os_path + file;
            Export.SimpleExport(_token, customFile, masterList, fileName, ext);
        }
        public void generateDataTable(List<Dictionary<string, object>> objList, string customFile)
        {
            buildResults build = new buildResults(_token);
            DataTable projects = build.makeTable(projectList, customFile,"projects");
            DataTable scans = build.makeTable(scanList, customFile, "scans");
            DataTable results = build.makeTable(objList, customFile, "results");
           // Queryable queryable = new Queryable(_token, projects,scans,results);
           // queryable.queryTable("");

        }
        public MasterDTO mapToMasterDTO(Dictionary<string, object> dict)
        {
            MasterDTO master = new MasterDTO();

            string normalizeName(string name) => name.ToLowerInvariant();

            var type = master.GetType();

            var setters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetSetMethod() != null)
                .ToDictionary(p => normalizeName(p.Name));

            foreach (var item in dict)
            {
                if (setters.TryGetValue(normalizeName(item.Key), out var setter))
                {
                    //var value = setter.PropertyType.
                    setter.SetValue(master, Convert.ToString(item.Value));
                }
            }
            return master;
        }


    }
}
