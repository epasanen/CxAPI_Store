using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using RazorLight;
using RazorLight.Razor;
using System.Threading.Tasks;

namespace CxAPI_Store
{
    public class Severity
    {
        public string Level { get; set; }
        public int LastMonth { get; set; }
        public int CurrentMonth { get; set; }
        public int Difference { get; set; }

        public Severity(string level, int lastMonth, int currentMonth)
        {
            this.Level = level;
            this.LastMonth = lastMonth;
            this.CurrentMonth = currentMonth;
            Difference = CurrentMonth - LastMonth;
        }

    }
    public class OWASP
    {
        public string Level { get; set; }
        public int LastMonth { get; set; }
        public int CurrentMonth { get; set; }
        public int Difference { get; set; }

        public OWASP(string level, int lastMonth, int currentMonth)
        {
            this.Level = level;
            this.LastMonth = lastMonth;
            this.CurrentMonth = currentMonth;
            Difference = CurrentMonth - LastMonth;
        }

    }

    public class OWASPSeverity
    {
        public Dictionary<string, List<OpenFindings>> byseverity { get; set; }
        public OWASPSeverity()
        {
            byseverity = new Dictionary<string, List<OpenFindings>>();
        }
        public void addRow(string severity, string owasp, string name, string sourcefile, string destinationfile, string obj, string desc, string firstfound, string policyviolation)
        {
            var newobj = new OpenFindings(name, severity, sourcefile, destinationfile, obj, desc, firstfound, policyviolation, owasp);
            if (!byseverity.ContainsKey(severity + "-" + owasp))
            {
                byseverity.Add(severity + "-" + owasp, new List<OpenFindings>());
            }
            else
            {
                byseverity[severity + "-" + owasp].Add(newobj);
                byseverity.PartialMatch<List<OpenFindings>>("High");
            }
        }
        public IEnumerable<List<OpenFindings>> getList(string severity)
        {
            var list =  byseverity.PartialMatch<List<OpenFindings>>(severity);
            return list;
        }
    }
    public class ScopeOfScan
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public int LOC { get; set; }

        public ScopeOfScan(string name, string lang, int LOC)
        {
            this.Name = name;
            this.Language = lang;
            this.LOC = LOC;
        }

    }
    public class OpenFindings
    {
        public string ComponentName { get; set; }
        public string Severity { get; set; }
        public string SourceFile { get; set; }
        public string DestinationFile { get; set; }
        public string Object { get; set; }
        public string Description { get; set; }
        public string FirstFound { get; set; }
        public string PolicyViolation { get; set; }
        public string OWASP { get; set; }
        public OpenFindings(string name, string severity, string sourcefile, string destinationfile, string obj, string desc, string firstfound, string policyviolation, string owasp = "")
        {
            ComponentName = name;
            Severity = severity;
            SourceFile = sourcefile;
            DestinationFile = destinationfile;
            Object = obj;
            Description = desc;
            FirstFound = firstfound;
            PolicyViolation = policyviolation;
            OWASP = owasp;
        }

    }
    public class BusinessCategory
    {
        public List<string> Components { get; set; }
        public Dictionary<string, List<string>> risk { get; set; }
        public BusinessCategory()
        {
            Components = new List<string>() { "Total", "Project1", "Project2", "Project3" };
            risk = new Dictionary<string, List<string>>();
            risk.Add("Total", new List<string>() { "2", "3", "20", "30", "5", "9" });
            risk.Add("Project1", new List<string>() { "2", "3", "20", "30", "5", "9" });
            risk.Add("Project2", new List<string>() { "2", "3", "20", "30", "5", "9" });
            risk.Add("Project3", new List<string>() { "2", "3", "20", "30", "5", "9" });
        }

    }
    public class Model
    {
        public MonthByNumber months { get; set; }
        public BusinessCategory business { get; set; }

        public List<Severity> severity { get; set; }
        public List<OWASP> owasp { get; set; }
        public List<OpenFindings> openfindings { get; set; }
        public List<ScopeOfScan> scopeofscan { get; set; }
        public OWASPSeverity owaspseverity { get; set; }


        public Model()
        {
            months = new MonthByNumber(7,"test");
            business = new BusinessCategory();
            severity = new List<Severity>();
            severity.Add(new Severity("High", 12, 10));
            severity.Add(new Severity("Medium", 12, 10));
            severity.Add(new Severity("Low", 12, 10));
            severity.Add(new Severity("Info", 12, 10));
            owasp = new List<OWASP>();
            owasp.Add(new OWASP("SQL Injection", 12, 10));
            owasp.Add(new OWASP("XSS", 12, 10));
            owasp.Add(new OWASP("Second Order Injection", 12, 10));
            owasp.Add(new OWASP("SQL Injection", 12, 10));
            owasp.Add(new OWASP("SQL Injection", 12, 10));
            owasp.Add(new OWASP("SQL Injection", 12, 10));
            owasp.Add(new OWASP("SQL Injection", 12, 10));
            owasp.Add(new OWASP("SQL Injection", 12, 10));
            openfindings = new List<OpenFindings>();
            openfindings.Add(new OpenFindings("Project1", "High", "input.cs", "output.cs", "Node", "SQL Injection", "Jan 1, 2021,", "30"));
            openfindings.Add(new OpenFindings("Project1", "High", "input.cs", "output.cs", "Node", "SQL Injection", "Jan 1, 2021,", "30"));
            openfindings.Add(new OpenFindings("Project1", "High", "input.cs", "output.cs", "Node", "SQL Injection", "Jan 1, 2021,", "30"));
            openfindings.Add(new OpenFindings("Project1", "High", "input.cs", "output.cs", "Node", "SQL Injection", "Jan 1, 2021,", "30"));
            openfindings.Add(new OpenFindings("Project1", "High", "input.cs", "output.cs", "Node", "SQL Injection", "Jan 1, 2021,", "30"));
            scopeofscan = new List<ScopeOfScan>();
            scopeofscan.Add(new ScopeOfScan("Project1", "Java", 3200));
            scopeofscan.Add(new ScopeOfScan("Project2", "Csharp", 13200));
            scopeofscan.Add(new ScopeOfScan("Project3", "JavaScript", 10000));
            scopeofscan.Add(new ScopeOfScan("Project4", "Java", 64000));
            owaspseverity = new OWASPSeverity();
            owaspseverity.addRow("High", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("High", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Medium", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Medium", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Low", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Low", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Low", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Info", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Info", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Info", "54321", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");
            owaspseverity.addRow("Info", "12345", "Project1", "input.cs", "output.cs", "node", "SQL Injection", "Jan 1,2021", "30");

        }
    }
    public class testPdfReport
    {
        public resultClass _token;

        public testPdfReport(resultClass token)
        {
            this._token = token;
        }

        public void testPDF()
        {
            Model model = new Model();
            getTemplate("test.cshtml", model);

        }

        public async void getTemplate(string templateName, Model model)
        {
            try
            {
                var engine = new RazorLightEngineBuilder().UseFileSystemProject(_token.template_path).UseMemoryCachingProvider().Build();

                string result = await engine.CompileRenderAsync("test.cshtml", model);
                File.WriteAllText(_token.template_path + _token.os_path + "test.html", result);
                createPdf(result, _token.template_path + _token.os_path + "test.pdf");
            }
            catch
            {
                throw;
            }
        }

        public void createPdf(string src, string dest)
        {
            var writer = new PdfWriter(new FileInfo(dest));
            HtmlConverter.ConvertToPdf(src, writer);
        }

    }
}
