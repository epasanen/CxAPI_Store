using CxAPI_Store.dto;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;


namespace CxAPI_Store
{
    class buildDataSet : IDisposable
    {
        public resultClass token;
        private int maxQueries = 1000;
        private int maxResults = 1000;
        private int maxPathNodes = 1; // just get first and last nodes for now
        private int maxNodes = 1000;
        private string lastCustomFile = String.Empty;

        public DataStore dataStore;
        public Dictionary<string, string> masterTemplate;
        public Dictionary<string, string> customKeys;



        public buildDataSet(resultClass token, DataStore dataStore)
        {
            this.token = token;
            string FileName = String.Format("{0}{1}{2}.yaml", token.master_path, token.os_path, "MasterMap");
            masterTemplate = loadDictionary(FileName);
            this.dataStore = dataStore;
        }

        public Dictionary<string, object> fetchProject(resultClass token, Dictionary<string, object> dict, string customFile)
        {
            int projectScore = 0;
            Dictionary<string, object> projects = new Dictionary<string, object>();
            if (!lastCustomFile.Contains(customFile))
            {
                customKeys = loadDictionary(customFile, false);
            }
            projectScore = getNodes("Project_", dict, projects, 0, 0, 0);
            dataStore.copyAndPurge("projects",projects);
         
            return projects;
        }
        public void fetchDetails(resultClass token, Dictionary<string, object> dict, string customFile)
        {
            List<object> objList = new List<object>();


            int queryCount = 0, resultCount = 0, pathNodeCount = 0;
            int scanScore = 0, summaryScore = 0, queryScore = 0, resultScore = 0, pathNodeFirstScore = 0, pathNodeLastScore = 0;

            Dictionary<string, object> scan = new Dictionary<string, object>();
            Dictionary<string, object> summary = new Dictionary<string, object>();

            scanScore = getNodes("Scan_", dict, scan, queryCount, 0, 0);
            summaryScore = getNodes("Summary_", dict, summary, queryCount, 0, 0);

            dataStore.copyAndPurge("scans", scan);
            dataStore.copyAndPurge("summaries", summary);


            for (queryCount = 0; queryCount < maxQueries; queryCount++)
            {
                Dictionary<string, object> query = new Dictionary<string, object>();
                queryScore = getNodes("Query_", dict, query, queryCount, 0, 0);
                dataStore.copyAndPurge("queries", query);
                if (queryScore == 0) break;

                for (resultCount = 0; resultCount < maxResults; resultCount++)
                {
                    Dictionary<string, object> results = new Dictionary<string, object>();
                    resultScore = getNodes("Result_", dict, results, queryCount, resultCount, 0);
                    dataStore.copyAndPurge("results", results);

                    if (resultScore == 0) break;

                    for (pathNodeCount = 0; pathNodeCount < maxPathNodes; pathNodeCount++)
                    {
                        Dictionary<string, object> nodes = new Dictionary<string, object>();

                        pathNodeFirstScore = getNodes("PathNode_First_", dict, nodes, queryCount, resultCount, pathNodeCount);
                        pathNodeLastScore = getandMapLastNode("PathNode_Last_", dict, nodes, queryCount, resultCount);
                        dataStore.copyAndPurge("nodes", nodes);
                    }
                }
            }

            return;
        }
        public void writeDictionary(resultClass token, Dictionary<string, object> dict, string fileName = "MasterMap.yaml")
        {
            string dictText = String.Empty;

            foreach (string key in dict.Keys)
            {
                string keys = key.Replace('.', '_');
                if (!masterTemplate.ContainsKey(keys))
                {
                    masterTemplate.Add(keys, key);
                }
            }
            foreach (string key in masterTemplate.Keys)
            {
                dictText += String.Format("{0}:{1}{2}", key, masterTemplate[key], Environment.NewLine);
            }
            File.WriteAllText(token.template_path + "\\" + fileName, dictText);
        }


        public bool buildTemplate(resultClass token)
        {
            List<object> objList = new List<object>();
            string set_path = token.template_path;
            string fileName = token.template_file;
            if (token.debug) Console.WriteLine(@"Using configuration in path " + set_path);

            string template = String.Format("{0}{1}{2}", set_path, Configuration.ospath(), fileName);
            fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
            string aClass = String.Format("{0}{1}{2}.txt", set_path, Configuration.ospath(), fileName);

            List<string> results = new List<string>();
            results.Add("// Keys used for all reports.");
            results.AddRange(getTags("MasterMap"));
            File.WriteAllLines(template, results.ToArray());

            Dictionary<string, string> keypair = loadDictionary(template, false);

            string newClass = "public class MasterDTO" + Environment.NewLine;
            newClass += "{" + Environment.NewLine;
            foreach (KeyValuePair<string, string> keyvalue in keypair)
            {
                newClass += String.Format("public string {0} {{get; set;}}{1}", keyvalue.Value, Environment.NewLine);
            }
            newClass += "}" + Environment.NewLine;
            File.WriteAllText(aClass, newClass);

            return true;
        }

        private List<string> getTags(string reference, bool rewrite = false)
        {
            Dictionary<string, string> referenceKeys = loadDictionary(String.Format("{0}{1}{2}.yaml", token.master_path, Configuration.ospath(), reference));
            List<string> result = new List<string>();
            foreach (string Key in referenceKeys.Keys)
            {
                result.Add(String.Format("{0}:{1}", Key.Trim(), Key.Trim()));
            }
            if (rewrite)
            {
                File.WriteAllLines(String.Format("{0}{1}{2}.yaml", token.template_path, Configuration.ospath(), reference), result.ToArray());
            }
            return result;
        }
        private int getandMapLastNode(string yaml, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount)
        {
            int count = 0;
            List<KeyValuePair<string, string>> kvp = findFilterTag(yaml);
            foreach (KeyValuePair<string, string> kv in kvp)
            {
                if (findandMapLastNode(kv, dict, results, queryCount, resultCount))
                {
                    count++;
                    continue;
                }

            }
            return count;
        }
        private int getNodes(string partial, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount, int pathNodeCount)
        {

            int count = 0;

            List<KeyValuePair<string, string>> kvp = findFilterTag(partial);
            foreach (KeyValuePair<string, string> kv in kvp)
            {
                if (findAndMapPattern(kv, dict, results, queryCount, resultCount, pathNodeCount))
                {
                    count++;
                    continue;
                }

            }
            getKeys("CxKeys", dict, results, queryCount, resultCount);
            return count;
        }
        private int getKeys(string partial, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount)
        {
            int count = 0;
            List<KeyValuePair<string,string>> keyFiles = loadKeyValues(String.Format("{0}{1}{2}.yaml", token.master_path, token.os_path, partial));
            // get the real key from the mastermap file
            foreach (KeyValuePair<string,string> kvp in keyFiles)
            {
                if (masterTemplate.ContainsKey(kvp.Value))
                {
                    string realKey = masterTemplate[kvp.Value];
                    realKey = realKey.Replace("~qq", queryCount.ToString());
                    realKey = realKey.Replace("~rr", resultCount.ToString());
                    if (dict.ContainsKey(realKey.Trim()))
                    {
                        if (!results.ContainsKey(kvp.Key))
                        {
                            results.Add(kvp.Key, dict[realKey.Trim()]);
                            count++;
                        }
                        else
                        {
                            var val = dict[realKey.Trim()].ToString();
                            if (!String.IsNullOrEmpty(val))             
                            {
                                results[kvp.Key] =  dict[realKey.Trim()];
                            }
                        }
                    }
                }
            }
            return count;
        }
   
        private bool findAndMapPattern(KeyValuePair<string, string> customFile, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount, int pathNodeCount)
        {
            // get the real key from the mastermap file
            if (masterTemplate.ContainsKey(customFile.Key))
            {
                string realKey = masterTemplate[customFile.Key];
                realKey = realKey.Replace("~qq", queryCount.ToString());
                realKey = realKey.Replace("~rr", resultCount.ToString());
                if (dict.ContainsKey(realKey.Trim()))
                {
                    if (!results.ContainsKey(customFile.Key))
                    {
                        results.Add(customFile.Key, dict[realKey.Trim()]);
                        return true;
                    }
                }
                else
                {
                    if (!results.ContainsKey(customFile.Key))
                    {
                        results.Add(customFile.Key, String.Empty);
                        return false;
                    }
                }
            }
            return false;
        }

        private bool findandMapLastNode(KeyValuePair<string, string> customFile, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount)
        {
            // get the real key from the mastermap file
            if (masterTemplate.ContainsKey(customFile.Key))
            {
                string realKey = masterTemplate[customFile.Key];
                realKey = realKey.Replace("~qq", queryCount.ToString());
                realKey = realKey.Replace("~rr", resultCount.ToString());
                realKey = realKey.Replace("~pp", "0");
                if (dict.ContainsKey(realKey.Trim())) // node zero must exist
                {
                    for (int pathNodeCount = 1; pathNodeCount < maxNodes; pathNodeCount++)
                    {
                        realKey = masterTemplate[customFile.Key];
                        realKey = realKey.Replace("~qq", queryCount.ToString());
                        realKey = realKey.Replace("~rr", resultCount.ToString());
                        realKey = realKey.Replace("~pp", pathNodeCount.ToString());
                        if (!dict.ContainsKey(realKey.Trim()))
                        {
                            realKey = masterTemplate[customFile.Key];
                            realKey = realKey.Replace("~qq", queryCount.ToString());
                            realKey = realKey.Replace("~rr", resultCount.ToString());
                            realKey = realKey.Replace("~pp", (pathNodeCount - 1).ToString());
                            results.Add(customFile.Key, dict[realKey.Trim()]);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private List<KeyValuePair<string, string>> findFilterTag(string partial)
        {

            IEnumerable<string> partialKeys = customKeys.PartialMatch<string>(partial);
            List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();
            foreach (string key in partialKeys)
            {
                results.Add(new KeyValuePair<string, string>(key, customKeys[key]));
            }
            return results;
        }
    

        public string sortTemplate(string desired, string jsonTemplate)
        {
            string fileName = token.master_path + token.os_path + jsonTemplate;
            var jObj = JObject.Parse(File.ReadAllText(fileName));
            var jArray = new JArray();

            Dictionary<string, string> customKeys = loadDictionary(desired);
            foreach (string key in customKeys.Keys)
            {
                string sObject = String.Format("{{field:\"{0}\",title: \"{1}\"}}", key, customKeys[key]);
                var column = JObject.Parse(sObject);
                jArray.Add(column);
            }
            jObj["body"][1]["columns"] = jArray;
            return jObj.ToString(Newtonsoft.Json.Formatting.Indented);
        }
        public DataTable makeTable(List<Dictionary<string,object>> convert, string desired, string tableName)
        {
            DataTable table = new DataTable(tableName);

            Dictionary<string, string> customKeys = loadDictionary(desired);
            foreach (string key in customKeys.Keys)
            {
                table.Columns.Add(key, Type.GetType("System.Object"));
            }
            foreach (var obj in convert)
            {
                DataRow dr = table.NewRow();
                foreach (KeyValuePair<string, object> keyValue in obj)
                {
                    dr[keyValue.Key] = keyValue.Value;
                }
                table.Rows.Add(dr);
            }
            return table;
        }
        public List<KeyValuePair<string, object>> makeKeyValuePair(List<object> convert)
        {
            var jArray = new JArray();
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();
            foreach (ExpandoObject obj in convert)
            {
                foreach (KeyValuePair<string, object> keyValue in obj)
                {
                    result.Add(keyValue);
                }

            }
            return result;
        }
        public List<MasterDTO> makeTypedObject(List<object> convert)
        {
            var jArray = new JArray();
            List<MasterDTO> result = new List<MasterDTO>();
            foreach (ExpandoObject obj in convert)
            {
                MasterDTO masterDTO = new MasterDTO();
                foreach (KeyValuePair<string, object> keyValue in obj)
                {
                    PropertyInfo propertyInfo = masterDTO.GetType().GetProperty(keyValue.Key);
                    propertyInfo.SetValue(null, keyValue.Value);
                }
                result.Add(masterDTO);
            }
            return result;
        }

        public Dictionary<string, string> loadDictionary(string file, bool flip = false)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            bool comment = false;
            foreach (string line in File.ReadLines(file))
            {
                if (testForComments(line, comment))
                {
                    string[] keyValue = line.Split(":");
                    if (flip)
                    {
                        if (!dict.ContainsKey(keyValue[1].Trim()) && (!String.IsNullOrEmpty(keyValue[0].Trim())))
                        {
                            dict.Add(keyValue[1].Trim(), keyValue[0].Trim());
                        }
                    }
                    else
                    {
                        if (!dict.ContainsKey(keyValue[0].Trim()) && (!String.IsNullOrEmpty(keyValue[1].Trim())))
                        {
                            dict.Add(keyValue[0].Trim(), keyValue[1].Trim());
                        }
                    }
                }
            }
            return dict;
        }
        public List<KeyValuePair<string,string>> loadKeyValues(string file)
        {
            List<KeyValuePair<string, string>> kv = new List<KeyValuePair<string, string>>();
            bool comment = false;
            foreach (string line in File.ReadLines(file))
            {
                if (testForComments(line, comment))
                {
                    string[] keyValue = line.Split(":");
                    kv.Add(new KeyValuePair<string,string>(keyValue[0].Trim(), keyValue[1].Trim()));  
                }
            }
            return kv;
        }
        private bool testForComments(string inString, bool comment)
        {

            if (inString.StartsWith("//")) return false;
            if (inString.StartsWith("/*"))
            {
                comment = true;
                return false;
            }
            if (inString.StartsWith("*/"))
            {
                comment = false;
                return false;
            }
            if (comment) { return false; }

            string[] split = inString.Split(":");
            if (split.Length != 2) { return false; };
            if (String.IsNullOrEmpty(split[1].Trim())) { return false; };
            if (split[0].Contains("CxReport.")) { return false; }

            return true;
        }
        public void Dispose()
        {

        }

    }

}
