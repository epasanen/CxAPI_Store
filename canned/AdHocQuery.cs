using CxAPI_Store.dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static CxAPI_Store.CxConstant;
using System.Linq;
using System.Dynamic;

namespace CxAPI_Store
{

    public class AdHocQuery
    {

        private MakeReports makeReports;
        private resultClass token;
//        private DataSet dataSet;
        private SQLiteMaster sqlite;
        public AdHocQuery(resultClass token, MakeReports makeReports)
        {
            this.token = token;
            this.makeReports = makeReports;
            this.sqlite = makeReports.sqllite();
        }
        public dynamic fetchReport()
        {

            // Use the  command line filters
            string sql = token.query_filter;
            DataTable dt = sqlite.SelectIntoDataTable("AdHoc", sql);
            List<dynamic> results = dt.AsDynamicEnumerable().ToList();
            return results;
        }
        public void Dispose()
        {

        }

    }

}

