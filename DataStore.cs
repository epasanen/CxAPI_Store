using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
            table.Columns.Add("Key_Result_FileName", typeof(String));
            table.Columns.Add("Key_Result_SimilarityId", typeof(Int64));
            table.Columns.Add("Key_Result_ResultId", typeof(Int64));
            table.Columns.Add("Key_Project_Team_Name", typeof(String));
            table.Columns.Add("Key_Project_Preset_Name", typeof(String));
            table.Columns.Add("Key_Query_Id", typeof(Int64));
            table.Columns.Add("Key_Query_CWE", typeof(Int64));
            table.Columns.Add("Key_Query_Name", typeof(String));
            table.Columns.Add("Key_Query_Language", typeof(String));
            table.Columns.Add("Key_Result_FalsePositive", typeof(String));
            table.Columns.Add("Key_Result_DetectionDate", typeof(String));
  
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
        public Dictionary<string, object> copyAndPurge(string select, Dictionary<string, object> dict)
        {
            DataTable table = dataSet.Tables[select];
            var newrow = table.NewRow();
            foreach (string key in dict.Keys)
            {
                if (table.Columns.Contains(key))
                {
                    newrow[key] = dict[key] ?? DBNull.Value;
                }
            }
            table.Rows.Add(newrow);

            return new Dictionary<string, object>();
        }
        public void selectOption(string customFile)
        {
            Queryable queryable = new Queryable(_token);
            queryable.getByCustomFields(dataSet);
            getTemplate("any", queryable);

        }
        public async void getTemplate(string templateName, object model)
        {
            try
            {
                var engine = new RazorLightEngineBuilder().UseFileSystemProject(_token.template_path).UseMemoryCachingProvider().Build();
             

                string result = await engine.CompileRenderAsync("test.cshtml", model);
                File.WriteAllText(_token.template_path + _token.os_path + "test.html", result);
                createPdf(result, _token.template_path + _token.os_path + "test.pdf");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void createPdf(string src, string dest)
        {
            ConverterProperties cp = new ConverterProperties();
       
            var writer = new PdfWriter(new FileInfo(dest));
            HtmlConverter.ConvertToPdf(src, writer);
        }


    }

}
