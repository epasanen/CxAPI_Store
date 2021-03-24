using System;
using System.Collections.Generic;
using SimpleExporter;
using System.Text;
using SimpleExporter.Writer;
using SimpleExporter.Writer.PdfReportWriter;
using SimpleExporter.Writer.XlsxReportWriter;
using SimpleExporter.Definition;
using SimpleExporter.Source;
using SimpleExporter.Helpers;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Data;
using CxAPI_Store.dto;

namespace CxAPI_Store
{
    public static class Export
    {
        public static void SimpleExport(resultClass token, string customFile, List<MasterDTO> convert, string fileName, string ext)
        {
            buildResults build = new buildResults(token);
            string single = build.sortTemplate(customFile, "basicTemplate.json");
            var reportDefinition = ReportDefinition.FromJson(single);
            var query = convert;
            var report = SimpleExporter.SimpleExporter.CreateReport(reportDefinition, query.ToReportDataSource("MasterDTO"));

            //CSV
            switch (ext)
            {
                case "csv":

                    using (var fs = File.Create(fileName))
                    {
                        var writer = new DelimitedTextReportWriter();
                        report.WriteReport(fs, writer);
                        Console.WriteLine("(CSV) created: {0}", fs.Name);

                    }
                    break;

                //Xlsx
                case "xlsx":
                    using (var fs = File.Create(fileName))
                    {
                        var writer = new XlsxReportWriter();
                        report.WriteReport(fs, writer);
                        Console.WriteLine("(Xlsx) created: {0}", fs.Name);
                    }
                    break;

                //PDF
                case "pdf":
                    using (var fs = File.Create(fileName))
                    {
                        var writer = new PdfReportWriter();
                        report.WriteReport(fs, writer);
                        Console.WriteLine("(PDF) created: {0}", fs.Name);
                    }
                    break;
            }


        }
 
 
    }
}
