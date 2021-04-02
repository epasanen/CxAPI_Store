using CxAPI_Store.dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CxAPI_Store
{
    class Flatten
    { 
        public static Dictionary<string, object> DeserializeAndFlatten(object obj, Dictionary<string, object> dict = null)
        {
            var json = JsonConvert.SerializeObject(obj);
            return DeserializeAndFlatten(json, dict);
        }

        public static Dictionary<string, object> DeserializeAndFlatten(string json, Dictionary<string, object> dict = null)
        {
            if (dict == null)
            {
                dict = new Dictionary<string, object>();
            }
            JToken token = JToken.Parse(json);
            FillDictionaryFromJToken(dict, token, "");
            return dict;
        }
        public static Dictionary<string, object> FlattenAndConvert(object obj, Dictionary<string, object> dict = null)
        {
            var json = JsonConvert.SerializeObject(obj);
            return FlattenAndConvert(json, dict);
        }

        public static Dictionary<string, object> FlattenAndConvert(string json, Dictionary<string, object> dictionary = null)
        {
            var dict = DeserializeAndFlatten(json,dictionary);
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                int i = kvp.Key.LastIndexOf(".");
                string key = (i > -1 ? kvp.Key.Substring(i + 1) : kvp.Key);
                Match m = Regex.Match(kvp.Key, @"\.([0-9]+)\.");
                if (m.Success) key += m.Groups[1].Value;
                result.Add(key, kvp.Value);
            }
            return result;
        }

        public static object CreateFlattenObject(Dictionary<string, object> dict)
        {
            dynamic dynObject = new ExpandoObject();
            foreach (string key in dict.Keys)
            {
                ((IDictionary<string, object>)dynObject).Add(key, dict[key]);
            }
            return dynObject;
        }
        public static MasterDTO CreateFlattenObject(Dictionary<string, object> dict, Dictionary<string,string> masterKey)
        {
            MasterDTO master = new MasterDTO();
            string normalizeName(string name) => name.ToLowerInvariant();
            var type = master.GetType();

            var setters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetSetMethod() != null)
                .ToDictionary(p => normalizeName(p.Name));

            foreach (var item in dict)
            {
                if (masterKey.ContainsValue(item.Key))
                if (setters.TryGetValue(normalizeName(item.Key), out var setter))
                {
                    //var value = setter.PropertyType.ChangeType(item.Value);
                    setter.SetValue(master, item.Value);
                }
            }

            return master;
        }

        private static void FillDictionaryFromJToken(Dictionary<string, object> dict, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        prefix = prefix.EndsWith(".") ? prefix += "0" : prefix;
                        FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    if (!dict.ContainsKey(prefix))
                    {
                        dict.Add(quickFix(prefix), trimAndEncode(((JValue)token).Value));
                    }
                    break;
            }
        }

        private static string quickFix(string fix)
        {
            var match = Regex.Match(fix, @"Query\.[a-zA-Z]");
            if (match.Success)
            {
                fix = fix.Replace("Query.", "Query.0.");
            }
            match = Regex.Match(fix, @"Result\.[a-zA-Z+@]");
            if (match.Success)
            {
                fix = fix.Replace("Result.", "Result.0.");
            }
            match = Regex.Match(fix, @"PathNode\.[a-zA-Z]");
            if (match.Success)
            {
                fix = fix.Replace("PathNode.", "PathNode.0.");
            }

            return fix;
        }
        private static object trimAndEncode(object block)
        {
            resultClass _token = Configuration._token;
            if (block is string)
            {
                string change = (string)block;
                if (change.Length > _token.max_length)
                {
                    change = change.Substring(0, _token.max_length);
                    return (object)change;
                }
            }
            return block;
        }

        private static string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : prefix + "." + name);
        }

    }
}
