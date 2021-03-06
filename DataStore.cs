using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CxAPI_Store.dto;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using RazorLight;

namespace CxAPI_Store
{

    public class DataStore
    {
        public resultClass _token { get; set; }

        public DataSet dataSet;

        public string fileName;
        public string filePath;
        public string outputType;


        public DataStore(resultClass token)
        {

            _token = token;
            dataSet = new DataSet();
            dataSet.Tables.Add(new DataTable("projects"));
            dataSet.Tables.Add(new DataTable("scans"));
            dataSet.Tables.Add(new DataTable("summaries"));
            dataSet.Tables.Add(new DataTable("queries"));
            dataSet.Tables.Add(new DataTable("results"));
            dataSet.Tables.Add(new DataTable("nodes"));

            // set defaults
            fileName = token.file_name;
            filePath = token.file_path;
            outputType = "CSV";

            maptoTable(dataSet.Tables["projects"], new MasterDTO(), "Project_");
            maptoTable(dataSet.Tables["scans"], new MasterDTO(), "Scan_");
            maptoTable(dataSet.Tables["summaries"], new MasterDTO(), "Summary_");
            maptoTable(dataSet.Tables["queries"], new MasterDTO(), "Query_");
            maptoTable(dataSet.Tables["results"], new MasterDTO(), "Result_");
            maptoTable(dataSet.Tables["nodes"], new MasterDTO(), "PathNode_");
        }


        public bool saveDataSet()
        {
            dataSet.WriteXmlSchema(_token.archival_path + _token.os_path + "CxSchema.xml");
            dataSet.WriteXml(_token.archival_path + _token.os_path + "Cxdata.xml");
            return true;
        }
        public bool restoreDataSet()
        {
            dataSet.ReadXmlSchema(_token.archival_path + _token.os_path + "CxSchema.xml");
            dataSet.ReadXml(_token.archival_path + _token.os_path + "Cxdata.xml");
            return true;
        }
        public bool maptoTable(DataTable table, MasterDTO mapObject, string pattern)
        {

            table.Columns.Add("Key_Project_Id", typeof(Int64));
            table.Columns.Add("Key_Scan_Id", typeof(Int64));
            table.Columns.Add("Key_Project_Name", typeof(String));
            table.Columns.Add("Key_Start_Date", typeof(DateTime));
            table.Columns.Add("Key_Finish_Date", typeof(DateTime));
            table.Columns.Add("Key_Scan_Type", typeof(string));
            table.Columns.Add("Key_Result_FileName", typeof(String));
            table.Columns.Add("Key_Result_SimilarityId", typeof(Int64));
            table.Columns.Add("Key_Result_ResultId", typeof(Int64));
            table.Columns.Add("Key_PathNode_NodeId", typeof(Int32));
            table.Columns.Add("Key_Result_PathId", typeof(Int64));
            table.Columns.Add("Key_Result_NodeId", typeof(Int64));
            table.Columns.Add("Key_Project_Team_Name", typeof(String));
            table.Columns.Add("Key_Project_Preset_Name", typeof(String));
            table.Columns.Add("Key_Query_Id", typeof(Int64));
            table.Columns.Add("Key_Query_CWE", typeof(Int64));
            table.Columns.Add("Key_Query_Name", typeof(String));
            table.Columns.Add("Key_Query_Language", typeof(String));
            table.Columns.Add("Key_Result_FalsePositive", typeof(String));
            table.Columns.Add("Key_Result_DetectionDate", typeof(String));
            table.Columns.Add("Key_Query_Count", typeof(Int64));
            table.Columns.Add("Key_Result_Count", typeof(Int64));
     
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.Name.StartsWith(pattern))
                {
                    var name = property.Name;
                    var prop = property.PropertyType;
                    table.Columns.Add(name, prop);
                }
                // Add index key

            }
            return true;
        }
        public Dictionary<string, object> copyAndPurge(string select, Dictionary<string,object> projectCommon, Dictionary<string, object> keyValues)
        {
            var dict = keyValues.Concat(projectCommon.Where(kvp => !keyValues.ContainsKey(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            DataTable table = dataSet.Tables[select];
            var newrow = table.NewRow();
            foreach (string key in dict.Keys)
            {
                if (table.Columns.Contains(key))
                {
                    if (table.Columns[key].DataType == typeof(DateTime))
                    {
                        newrow[key] = dict[key] ?? DateTime.MinValue;
                    }
                    else if (table.Columns[key].DataType == typeof(String))
                    {
                        newrow[key] = dict[key] ?? String.Empty;
                    }
                    else if (table.Columns[key].DataType == typeof(Boolean))
                    {
                        newrow[key] = dict[key] ?? false;
                    }
                    else
                    {
                        newrow[key] = dict[key] ?? 0;
                    }
                }
            }
            table.Rows.Add(testfornulls(newrow));

            return new Dictionary<string, object>();
        }
        private DataRow testfornulls(DataRow dr)
        {
            DataTable dt = dr.Table;
            for (int i=0; i < dr.ItemArray.Length;i++)
            {
                if (_token.debug && _token.verbosity > 2) Console.WriteLine("Table {0}, Column {1}, Type {2}, Data {3}, DataType {4}", dt.TableName, dt.Columns[i].ColumnName, dt.Columns[i].DataType, dr.ItemArray[i], dr.ItemArray[i].GetType());
                if (dr.ItemArray[i].GetType() == typeof(DBNull))
                {
                    if (_token.debug && _token.verbosity > 2) Console.WriteLine("DBNull -> Table {0}, Column {1}, Type {2}, Data {3}", dt.TableName, dt.Columns[i].ColumnName, dt.Columns[i].DataType, dr.ItemArray[i]);
                    if (dt.TableName.Contains("results") || dt.TableName.Contains("queries") || dt.TableName.Contains("nodes"))
                        Console.WriteLine("DBNull -> Table {0}, Column {1}, Type {2}, Data {3}", dt.TableName, dt.Columns[i].ColumnName, dt.Columns[i].DataType, dr.ItemArray[i]);
                    if (dt.Columns[i].DataType == typeof(String)) dr.ItemArray[i] = String.Empty;
                    else if (dt.Columns[i].DataType == typeof(DateTime)) dr.ItemArray[i] = DateTime.MinValue;
                    else if (dt.Columns[i].DataType == typeof(Int32)) dr.ItemArray[i] = 0;
                    else dr.ItemArray[i] = 0;
                }
            }

            return dr;
        }

 
    }

}
