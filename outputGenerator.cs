using CSScriptLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Reflection;
using CxAPI_Store.dto;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using RazorLight;
using CsvHelper;


namespace CxAPI_Store
{
    public static class outputGenerator
    {
        public static dynamic buildObject(resultClass token, MakeReports makeReports)
        {
            string dyno = File.ReadAllText(String.Format("{0}{1}{2}", token.template_path, token.os_path, "query.csx"));
            dynamic script = CSScript.Evaluator.LoadCode(dyno, new object[] { token, makeReports });
            return script;
        }
        public static dynamic cannedObject(resultClass token, MakeReports makeReports)
        {
            Type cannedType = Type.GetType("CxAPI_Store." + token.report_name);
            var cannedObject =  Activator.CreateInstance(cannedType, token, makeReports);
            MethodInfo cannedMethod = cannedType.GetMethod("fetchReport");
            return cannedMethod.Invoke(cannedObject, null);
        } 
        public static async void useCsHtmlTemplate(resultClass token, string path, string templateName, dynamic model, bool html = true, bool pdf = true)
        {
            try
            {
                var engine = new RazorLightEngineBuilder().UseFileSystemProject(path).UseMemoryCachingProvider().Build();
                string report = (token.file_name.LastIndexOf('.')) > 0 ? token.file_name.Substring(0, token.file_name.LastIndexOf('.')) : token.file_name;
                string result = await engine.CompileRenderAsync(String.Format("{0}.cshtml", templateName),model);
                File.WriteAllText(String.Format("{0}{1}{2}.html", token.file_path, token.os_path, report), result);
                if (pdf) { createPdf(result, String.Format("{0}{1}{2}.pdf", token.file_path, token.os_path, report)); }
                if (!html) { File.Delete(String.Format("{0}{1}{2}.html", token.file_path, token.os_path, report)); }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public static void createPdf(string src, string dest)
        {
            ConverterProperties cp = new ConverterProperties();
            var writer = new PdfWriter(new FileInfo(dest));
            HtmlConverter.ConvertToPdf(src, writer);
        }
        public static void simpleCSV(resultClass token, dynamic model)
        {
            csvHelper csvHelper = new csvHelper();
            csvHelper.writeCVSFile(model, token);

        }

    }
}
