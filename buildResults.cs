using CxAPI_Store.dto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;


namespace CxAPI_Store
{
    class buildResults : IDisposable
    {
        public resultClass token;
        private int maxQueries = 1000;
        private int maxResults = 1000;
        private int maxPathNodes = 1; // just get first and last nodes for now
        private int maxNodes = 1000;


        public buildResults(resultClass token)
        {
            this.token = token;

        }

        public Dictionary<string, object> fetchProject(resultClass token, Dictionary<string, object> dict, string customFile)
        {
            Dictionary<string, object> header = new Dictionary<string, object>();
            getNodes(customFile, "CxProject", dict, header, 0, 0, 0);
            getNodes(customFile, "CxSettings", dict, header, 0, 0, 0);
            return header;
        }
        public Dictionary<string, object> fetchScan(resultClass token, Dictionary<string, object> dict, string customFile)
        {
            Dictionary<string, object> header = new Dictionary<string, object>();
            getNodes(customFile, "CxScan", dict, header, 0, 0, 0);
            getNodes(customFile, "CxSummary", dict, header, 0, 0, 0);
            return header;
        }

        public libraryClasses fetchDetails(resultClass token, Dictionary<string, object> dict, string customFile, Dictionary<string, object> projects, Dictionary<string, object> scans, libraryClasses store)
        {
            List<object> objList = new List<object>();


            int queryCount = 0, resultCount = 0, pathNodeCount = 0;
            int queryScore = 0, resultScore = 0, pathNodeScore = 0;

            Dictionary<string, object> queries = new Dictionary<string, object>();
            for (queryCount = 0; queryCount < maxQueries; queryCount++)
            {
                queryScore = getNodes(customFile, "CxQuery", dict, queries, queryCount, 0, 0);
                if (queryScore == 0) break;

                for (resultCount = 0; resultCount < maxResults; resultCount++)
                {
                    Dictionary<string, object> results = new Dictionary<string, object>();
                    resultScore = getNodes(customFile, "CxResult", dict, results, queryCount, resultCount, 0);
                    if (resultScore == 0) break;

                    for (pathNodeCount = 0; pathNodeCount < maxPathNodes; pathNodeCount++)
                    {
                        Dictionary<string, object> pathNode = new Dictionary<string, object>();
                        pathNodeScore = getNodes(customFile, "CxPathNode.0", dict, pathNode, queryCount, resultCount, pathNodeCount);
                        //                        if (pathNodeScore == 0) break;

                        pathNodeScore = getLastNode(customFile, "CxPathNode.n", dict, pathNode, queryCount, resultCount);

                        Dictionary<string, object> keys = new Dictionary<string, object>();
                        getKeys("CxKeys", dict, keys);

                        libraryClass storeClass = new libraryClass();

                        storeClass.project = projects;
                        storeClass.scan = scans;
                        storeClass.queries = queries;
                        storeClass.results = results;
                        storeClass.pathNodes = pathNode;
                        storeClass.keys = keys;

                        store.storeScanResults(storeClass, keys);

                    }
                }
            }

            return store;
        }


        /*        public List<object> fetchSummary(resultClass token, Dictionary<string, object> dict)
                {
                    List<object> objList = new List<object>();
                    int headerScore = 0;
                    string set_path = token.template_path;
                    string fileName = token.template_file;
                    if (token.debug) Console.WriteLine(@"Using configuration in path " + set_path);
                    List<string> customFiles = new List<string>(Directory.GetFiles(set_path, fileName, SearchOption.AllDirectories));
                    foreach (string customFile in customFiles)
                    {
                        Dictionary<string, object> keys = new Dictionary<string, object>();
                        headerScore = getNodes(customFile, "CxProject", dict, keys, 0, 0, 0);
                        if (headerScore > 0)
                        {
                            getNodes(customFile, "CxSettings", dict, keys, 0, 0, 0);
                            getNodes(customFile, "CxSummary", dict, keys, 0, 0, 0);
                            objList.Add(Flatten.CreateFlattenObject(keys));
                        }
                    }

                    return objList;
                }
        */

        public bool buildTemplate(resultClass token)
        {
            List<object> objList = new List<object>();
            string set_path = token.template_path;
            string fileName = token.template_file;
            if (token.debug) Console.WriteLine(@"Using configuration in path " + set_path);

            List<string> results = new List<string>();
            results.Add("// Keys used for summary reports.");
            results.AddRange(getTags("CxProject"));
            results.AddRange(getTags("CxSettings"));
            results.AddRange(getTags("CxSummary"));
            results.Add("// Keys used for detailed reports.");
            results.AddRange(getTags("CxScan"));
            results.AddRange(getTags("CxQuery"));
            results.AddRange(getTags("CxResult"));
            results.AddRange(getTags("CxPathNode.0"));
            results.AddRange(getTags("CxPathNode.n"));
            File.WriteAllLines(String.Format("{0}{1}{2}", set_path, Configuration.ospath(), fileName), results.ToArray());

            return true;
        }

        private List<string> getTags(string reference)
        {
            Dictionary<string, string> referenceKeys = loadDictionary(String.Format("{0}{1}{2}.yaml", token.master_path, Configuration.ospath(), reference));
            List<string> result = new List<string>();
            foreach (string Key in referenceKeys.Keys)
            {
                result.Add(String.Format("{0}:{1}", referenceKeys[Key].Trim(), referenceKeys[Key].Trim()));
            }
            return result;
        }
        private int getLastNode(string yaml, string desired, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount)
        {
            int count = 0;
            List<KeyValuePair<string, string>> kvp = findFilterTag(yaml, desired);
            foreach (KeyValuePair<string, string> kv in kvp)
            {
                if (findLastNode(kv, dict, results, queryCount, resultCount))
                {
                    count++;
                    continue;
                }

            }
            return count;
        }
        private int getNodes(string yaml, string desired, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount, int pathNodeCount)
        {
            int count = 0;
            List<KeyValuePair<string, string>> kvp = findFilterTag(yaml, desired);
            foreach (KeyValuePair<string, string> kv in kvp)
            {
                if (findPattern(kv, dict, results, queryCount, resultCount, pathNodeCount))
                {
                    count++;
                    continue;
                }

            }
            return count;
        }
        private int getKeys(string desired, Dictionary<string, object> dict, Dictionary<string, object> results)
        {
            int count = 0;
            List<KeyValuePair<string, string>> kvp = findFilterTag(desired);
            foreach (KeyValuePair<string, string> kv in kvp)
            {
                string key = kv.Value.Replace("~nn", "0");
                key = key.Replace("~rr", "0");
                key = key.Replace("~pp", "0");
                if (!results.ContainsKey(kv.Key))
                {
                    if (dict.ContainsKey(key))
                    {
                        results.Add(kv.Key, dict[key]);
                        count++;
                    }
                }
            }
            return count;
        }
        private bool findPattern(KeyValuePair<string, string> raw, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount, int pathNodeCount)
        {
            string key = String.Empty;
            key = raw.Value.Replace("~nn", queryCount.ToString());
            key = key.Replace("~rr", resultCount.ToString());
            if (dict.ContainsKey(key.Trim()))
            {
                if (!results.ContainsKey(raw.Key))
                {
                    results.Add(raw.Key, dict[key]);
                    return true;
                }
            }
            else
            {
                if (!results.ContainsKey(raw.Key))
                {
                    results.Add(raw.Key, " ");
                    return false;
                }
            }
            return false;
        }
        private bool findLastNode(KeyValuePair<string, string> raw, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount)
        {

            string key = String.Empty;
            key = raw.Value.Replace("~nn", queryCount.ToString());
            key = key.Replace("~rr", resultCount.ToString());
            key = key.Replace("~pp", "0");
            if (dict.ContainsKey(key.Trim())) // node zero must exist
            {
                for (int pathNodeCount = 1; pathNodeCount < maxNodes; pathNodeCount++)
                {
                    key = raw.Value.Replace("~nn", queryCount.ToString());
                    key = key.Replace("~rr", resultCount.ToString());
                    key = key.Replace("~pp", pathNodeCount.ToString());
                    if (!dict.ContainsKey(key.Trim()))
                    {
                        key = raw.Value.Replace("~nn", queryCount.ToString());
                        key = key.Replace("~rr", resultCount.ToString());
                        key = key.Replace("~pp", (pathNodeCount - 1).ToString());
                        results.Add(raw.Key, dict[key]);
                        return true;
                    }
                }
            }
            return false;
        }
        private List<KeyValuePair<string, string>> findFilterTag(string reference)
        {
            Dictionary<string, string> referenceKeys = loadDictionary(String.Format("{0}{1}{2}.yaml", token.master_path, Configuration.ospath(), reference), true);
            List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();
            foreach (string key in referenceKeys.Keys)
            {
                results.Add(new KeyValuePair<string, string>(key, referenceKeys[key]));
            }
            return results;
        }
        private List<KeyValuePair<string, string>> findFilterTag(string desired, string reference)
        {
            Dictionary<string, string> customKeys = loadDictionary(desired);
            Dictionary<string, string> referenceKeys = loadDictionary(String.Format("{0}{1}{2}.yaml", token.master_path, Configuration.ospath(), reference), true);
            List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();
            foreach (string key in referenceKeys.Keys)
            {
                if (customKeys.ContainsKey(key))
                {
                    results.Add(new KeyValuePair<string, string>(customKeys[key], referenceKeys[key]));
                }
            }
            return results;
        }
        public Dictionary<string, object> sortTemplate(Dictionary<string,object> rawIn, string desired)
        {
            Dictionary<string, string> customKeys = loadDictionary(desired);
            Dictionary<string, object> sorted = new Dictionary<string, object>();
 
            foreach (string value in customKeys.Values)
            {
                if (rawIn.ContainsKey(value))
                {
                    sorted.Add(value, rawIn[value]);
                }
            }
            return sorted;
        }

        private Dictionary<string, string> loadDictionary(string file, bool flip = false)
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
