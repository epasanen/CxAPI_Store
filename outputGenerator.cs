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


namespace CxAPI_Store
{
    public static class outputGenerator
    {
        public static dynamic buildObject(resultClass token, DataSet dataSet)
        {
            string dyno = File.ReadAllText(String.Format("{0}{1}{2}", token.template_path, token.os_path, "query.csx"));
            dynamic script = CSScript.Evaluator.LoadCode(dyno,new object[] { token, dataSet });
            return script;
        }
        public static async void useCsHtmlTemplate(resultClass token, string templateName, dynamic model, bool html = true, bool pdf = true)
        {
            try
            {
                var engine = new RazorLightEngineBuilder().UseFileSystemProject(token.template_path).UseMemoryCachingProvider().Build();
                string trimmed = token.template_file.Substring(0, token.template_file.LastIndexOf('.'));
                string report = token.file_name.Substring(0, token.file_name.LastIndexOf('.'));
                string result = await engine.CompileRenderAsync(token.template_file, model);
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


    }
}
