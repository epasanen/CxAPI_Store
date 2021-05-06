using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using static CxAPI_Store.CxConstant;

namespace CxAPI_Store
{
    public class MakeReports : IDisposable
    {
        private resultClass token;
        private SQLiteMaster lite;
        private DataSet dataSet;

        public MakeReports(resultClass token)
        {
            this.token = token;
            lite = new SQLiteMaster(token);
            dataSet = lite.InitAllDataTables();
        }
        public SQLiteMaster sqllite()
        {
            return lite;
        }
        public void runReports()
        {
           
            if (token.canned)
                runCannedReport();
            else
                runSpecializedReport();

        }
        public void dataTableReplace(DataTable table)
        {
            DataTable temp = table.Copy();
            if (dataSet.Tables.Contains(table.TableName))
               dataSet.Tables.Remove(table.TableName);
            dataSet.Tables.Add(temp);
        }
        private void runSpecializedReport()
        {


        }
        private void runCannedReport()
        {
            dynamic obj = outputGenerator.cannedObject(token, this);
            if (token.output_type.Contains("csv"))
            {
                outputGenerator.simpleCSV(token, obj);
            }
            else if (token.output_type.Contains("pdf") || (token.output_type.Contains("html")))
            {
                outputGenerator.useCsHtmlTemplate(token, String.Format("{0}{1}canned",token.exe_path,token.os_path), token.report_name, obj, token.output_type.Contains("pdf"), token.output_type.Contains("html"));
            }
        }
        public DataSet filterFromCommandLine()
        {
            StringBuilder sql = new StringBuilder();

            if (!String.IsNullOrEmpty(token.project_name))
                sql.Append(String.Format("and ProjectName like '{0}' ", token.project_name));
            if (!String.IsNullOrEmpty(token.team_name))
                sql.Append(String.Format("and TeamName like '{0}' ", token.team_name));
            if (!String.IsNullOrEmpty(token.preset))
                sql.Append(String.Format("and Preset like '{0}' ", token.preset));
            if (!String.IsNullOrEmpty(token.query_filter))
                sql.Append(String.Format("and ({0}) ", token.query_filter));

            dataTableReplace(lite.SelectIntoDataTable(dataSet.Tables[ProjectTable], sql.ToString()));
            var matchproject = dataSet.Tables[ProjectTable].AsEnumerable().Select(r => string.Format("{0}", string.Join(",", r["ProjectId"])));
            var projects = string.Join(",", matchproject);

            sql.Clear();

            if (token.start_time != null)
                sql.Append(String.Format("and ScanFinished > datetime('{0:yyyy-MM-ddThh:mm:ss}') ", token.start_time));
            if (token.end_time != null)
                sql.Append(String.Format("and ScanFinished < datetime('{0:yyyy-MM-ddThh:mm:ss}') ", token.end_time));

            sql.Append(String.Format("and ProjectId in ({0}) order by ScanId", projects));
            dataTableReplace(lite.SelectIntoDataTable(dataSet.Tables[ScanTable], sql.ToString()));
            var matchscan = dataSet.Tables[ScanTable].AsEnumerable().Select(r => string.Format("{0}", string.Join(",", r["ScanId"])));
            var scans = string.Join(",", matchscan);
            sql.Clear();

/*            var listScans = dataSet.Tables[ScanTable].AsEnumerable()
                .OrderBy(s => s.Field<long>("ScanId"))
                .Select(row => new { ScanId = row.Field<long>("ScanId") })
                .ToList();

            dataTableReplace(lite.SelectIntoDataTable(dataSet.Tables[ResultTable], FirstResult, String.Format("and ScanId = {0}", listScans.FirstOrDefault().ScanId)));
            dataTableReplace(lite.SelectIntoDataTable(dataSet.Tables[ResultTable], LastResult, String.Format("and ScanId = {0}", listScans.LastOrDefault().ScanId)));
*/

            return dataSet;
        }
        public void Dispose()
        {
        }
    }
}
