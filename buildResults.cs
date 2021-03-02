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
        private int maxPathNodes = 1; // just the top most path node for now
        private Dictionary<long, SortedDictionary<string, object>> sorted;

        public buildResults(resultClass token)
        {
            this.token = token;
            sorted = new Dictionary<long, SortedDictionary<string, object>>();
        }


        public List<object> fetchYMLDetails(resultClass token, Dictionary<string, object> dict)
        {
            List<object> objList = new List<object>();
            string fileName = String.IsNullOrEmpty(token.template_file) ? "DetailedReport.yaml" : token.template_file;

            int queryCount = 0, resultCount = 0, pathNodeCount = 0;
            int queryScore = 0, resultScore = 0, pathNodeScore = 0, headerScore = 0;
            string _os = RuntimeInformation.OSDescription;
            string _ospath = _os.Contains("Windows") ? "\\" : "/";
            string set_path = token.template_path;
            string yaml = String.Format("{0}{1}{2}", set_path, _ospath, fileName);
            if (token.debug) Console.WriteLine(@"Using configuration in path " + yaml);
            if (File.Exists(yaml))
            {

                Dictionary<string, object> headers = new Dictionary<string, object>();
                headerScore = getNodes(yaml, "CxScan", dict, headers, 0, 0, 0);
                if (headerScore > 0)
                {
                    for (queryCount = 0; queryCount < maxQueries; queryCount++)
                    {
                        Dictionary<string, object> queries = new Dictionary<string, object>();
                        queryScore = getNodes(yaml, "CxQuery", dict, queries, queryCount, 0, 0);
                        if (queryScore == 0) break;

                        for (resultCount = 0; resultCount < maxResults; resultCount++)
                        {
                            Dictionary<string, object> results = new Dictionary<string, object>();
                            resultScore = getNodes(yaml, "CxResult", dict, results, queryCount, resultCount, 0);
                            if (resultScore == 0) break;

                            for (pathNodeCount = 0; pathNodeCount < maxPathNodes; pathNodeCount++)
                            {
                                Dictionary<string, object> pathNode = new Dictionary<string, object>();
                                pathNodeScore = getNodes(yaml, "CxPathNode", dict, pathNode, queryCount, resultCount, pathNodeCount);
                                if (pathNodeScore == 0) break;

                                Dictionary<string, object> keys = new Dictionary<string, object>();
                                getNodes(yaml, "CxKeys", dict, keys, queryCount, resultCount, pathNodeCount);

                                Dictionary<string, object> final = new Dictionary<string, object>();

                                final = final.Concat(headers.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                                final = final.Concat(queries.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                                final = final.Concat(results.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                                final = final.Concat(pathNode.Where(kvp => !final.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                                var scanValues = new SortedDictionary<string,object>(final);
                                sorted.Add((long)scanValues["_KeyProjectId"], scanValues);

                                objList.Add(Flatten.CreateFlattenObject(final));

                            }
                        }
                    }
                }
            }
            else
            {
                throw new FileNotFoundException("File or path missing or incorrect: " + yaml);
            }

            return objList;
        }


        public List<object> fetchYMLSummary(resultClass token, Dictionary<string, object> dict)
        {
            List<object> objList = new List<object>();
            string fileName = String.IsNullOrEmpty(token.template_file) ? "SummaryReport.yaml" : token.template_file;
            int headerScore = 0;
            string _os = RuntimeInformation.OSDescription;
            string _ospath = _os.Contains("Windows") ? "\\" : "/";
            string set_path = String.IsNullOrEmpty(token.template_path) ? String.Format("{0}{1}templates", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), _ospath) : token.template_path;
            string yaml = String.Format("{0}{1}{2}", set_path, _ospath, fileName);
            if (token.debug) Console.WriteLine(@"Using configuration in path " + yaml);
            if (File.Exists(yaml))
            {
                Dictionary<string, object> headers = new Dictionary<string, object>();
                headerScore = getNodes(yaml, "CxHeader", dict, headers, 0, 0, 0);
                if (headerScore > 0)
                {
                    Dictionary<string, object> keys = new Dictionary<string, object>();
                    getNodes(yaml, "CxKeys", dict, keys, 0, 0, 0);
                    objList.Add(Flatten.CreateFlattenObject(headers));
                }
            }
            else
            {
                throw new FileNotFoundException("File or path missing or incorrect: " + yaml);
            }

            return objList;
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
        private bool findPattern(KeyValuePair<string, string> raw, Dictionary<string, object> dict, Dictionary<string, object> results, int queryCount, int resultCount, int pathNodeCount)
        {
            string key = String.Empty;

            key = raw.Key.Replace("~nn", queryCount.ToString());
            key = key.Replace("~rr", resultCount.ToString());
            key = key.Replace("~pp", pathNodeCount.ToString());
            if (dict.ContainsKey(key.Trim()))
            {
                results.Add(raw.Value, dict[key]);
                return true;
            }
            results.Add(raw.Value, "");
            return false;
        }
        private List<KeyValuePair<string, string>> findFilterTag(string yaml, string desired)
        {
            List<string> tags = new List<string>() { "CxScan", "CxQuery", "CxResult", "CxPathNode", "CxKeys" };
            List<KeyValuePair<string, string>> lkv = new List<KeyValuePair<string, string>>();
            string key = String.Empty;
            string value = String.Empty;
            bool mark = false;
            bool comment = false;
            foreach (string line in File.ReadLines(yaml))
            {
                if (line.StartsWith("//")) continue;
                if (line.StartsWith("/*"))
                {
                    comment = true;
                }
                if (line.StartsWith("*/"))
                {
                    comment = false;
                    continue;
                }
                if (comment) { continue; }

                string[] split = line.Split(":");
                if (split.Length != 2) { continue; }

                key = split[0].Trim();
                value = split[1].Trim();
                if (tags.Contains(key) && key.Contains(desired))
                {
                    mark = true;
                    continue;
                }
                if (mark)
                {
                    if (tags.Contains(key) && !key.Contains(desired))
                    {
                        break;
                    }
                    lkv.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            return lkv;
        }

        public void Dispose()
        {

        }

    }

}
