using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;
using CxAPI_Store.dto;
using System.Xml.Linq;

namespace CxAPI_Store
{
    public class severityCounters
    {

        public Dictionary<string, int> severityCounter;
        public severityCounters()
        {
            severityCounter = new Dictionary<string, int>() { { "High", 0 }, { "Medium", 0 }, { "Low", 0 }, { "Info", 0 } };
        }
        public severityCounters(int High, int Medium, int Low, int Info)
        {
            severityCounter = new Dictionary<string, int>() { { "High", High }, { "Medium", Medium }, { "Low", Low }, { "Info", Info } };
        }

    }
    public class agingPolicy
    {

        public Dictionary<string, int> aging;
        public Dictionary<string, string> description;
        public agingPolicy()
        {
            aging = new Dictionary<string, int>() { { "High", 0 }, { "Medium", 0 }, { "Low", 0 }, { "Info", 0 } };
        }
        public agingPolicy(int High, int Medium, int Low, int Info)
        {
            aging = new Dictionary<string, int>() { { "High", High }, { "Medium", Medium }, { "Low", Low }, { "Info", Info } };
            description = new Dictionary<string, string>() { { "High", String.Format("High > {0}", High) }, { "High", String.Format("Medium > {0}", Medium) }, { "Low", String.Format("Log > {0}", Low) }, { "Info", String.Format("Info > {0}", Info) } };
        }
    }
    public class dataSetbyKey
    {
        public Dictionary<string, DataSet> dataSets;
        public dataSetbyKey()
        {
            dataSets = new Dictionary<string, DataSet>() { { "High", new DataSet() }, { "Medium", new DataSet() }, { "Low", new DataSet() }, { "Info", new DataSet() } };
        }
        public void dataSetAdd(string key, DataTable value)
        {
            dataSets[key].Tables.Add(value);
        }
    }
    public class MonthByNumber
    {
        public List<string> lastmonth { get; set; }
        public List<string> lastquery { get; set; }
        public List<int> toprisk { get; set; }
        public List<int> myrisk { get; set; }
        public MonthByNumber(int months, string field)
        {
            lastmonth = new List<string>();
            lastquery = new List<string>();
            toprisk = new List<int>();
            myrisk = new List<int>();

            int _months = months * -1;
            for (int i = _months; i < 1; i++)
            {
                DateTime back = DateTime.Now.AddMonths(i);
                DateTime next = DateTime.Now.AddMonths(i + 1);
                lastmonth.Add(back.ToString("MMM"));
                string query = String.Format("{0} > '{1}' and {2} < '{3}'", field, back.ToString("yyyy-MM-01 00:00"), field, next.ToString("yyyy-MM-01 00:00"));
                lastquery.Add(query);
            }
            clearTop(months);
            clearRisk(months);
        }
        public List<int> clearTop(int months)
        {
            toprisk = new List<int>();
            for (int i = 0; i < months + 1; i++)
            {
                toprisk.Add(0);
            }
            return toprisk;

        }
        public List<int> clearRisk(int months)
        {
            myrisk = new List<int>();
            for (int i = 0; i < months + 1; i++)
            {
                myrisk.Add(0);
            }
            return myrisk;

        }

    }
    public class GetResultCounts
    {
        private SQLiteMaster sqlite;
        public long ProjectID;
        public long ScanID;
        public DateTime ScanFinished;
        public Dictionary<string, int> State;
        public Dictionary<string, int> Severity;


        public GetResultCounts(SQLiteMaster sqlite)
        {
            this.sqlite = sqlite;
            this.State = new Dictionary<string, int>() { { "To Verify", 0 }, { "Not Exploitable", 0 }, { "Confirmed", 0 }, { "Urgent", 0 }, { "Proposed Not Exploitable", 0 } };
            this.Severity = new Dictionary<string, int>() { { "High", 0 }, { "Medium", 0 }, { "Low", 0 }, { "Info", 0 } };
        }
        public GetResultCounts(SQLiteMaster sqlite, List<string> labels)
        {
            this.State = new Dictionary<string, int>();
            this.sqlite = sqlite;
            this.Severity = new Dictionary<string, int>() { { "High", 0 }, { "Medium", 0 }, { "Low", 0 }, { "Info", 0 } };
            foreach (string s in labels)
            {
                State.Add(s, 0);
            }
        }
        public object GetCorrectCounts(long projectId, long ScanId, string falsePositive = "True")
        {
            string sql = String.Format("Select SimilarityId, ResultId, PathId, State, ResultSeverity from Results where ProjectId = {0} and ScanId = {1} and FalsePositive != '{2}'", projectId, ScanId, falsePositive);
            DataTable table = sqlite.SelectIntoDataTable("CorrectCount", sql);
            Severity["High"] = table.AsEnumerable().Where(w => w.Field<string>("ResultSeverity") == "High").Count();
            Severity["Medium"] = table.AsEnumerable().Where(w => w.Field<string>("ResultSeverity") == "Medium").Count();
            Severity["Low"] = table.AsEnumerable().Where(w => w.Field<string>("ResultSeverity") == "Low").Count();
            Severity["Info"] = table.AsEnumerable().Where(w => w.Field<string>("ResultSeverity") == "Info").Count();
            List<string> Loop = State.Keys.ToList();
            long stateCount = 0;
            foreach (string Key in Loop)
            {
                State[Key] = table.AsEnumerable().Where(w => w.Field<long>("State") == stateCount).Count();
                stateCount++;
            }
            return this;
        }
        public object GetCorrectCounts(long projectId, string query)
        {
            DataTable table = sqlite.SelectIntoDataTable("ScanCount", String.Format("Select ScanId from Scans where ProjectId = {0} and {1} order by ScanId Desc", projectId, query));
            var scan = table.AsEnumerable().Select(row => new { ScanId = row.Field<long>("ScanId") }).FirstOrDefault();
            if (scan != null)
                return GetCorrectCounts(projectId, scan.ScanId);
            return this;
        }
    }
    public class ScanHistory
    {
        public string Reviewer { get; set; }
        public DateTime ChangeDate { get; set; }
        public string Event { get; set; }

        public ScanHistory(string reviewer, string events, string changeDate)
        {
            this.Reviewer = reviewer;
            this.ChangeDate = DateTime.Parse(changeDate);
            this.Event = events;
        }

    }
    public class Vulnerability
    {
        public string VulnerabilityStatus { get; set; }
        public Dictionary<int, ScanHistory> history { get; set; }
        public bool isFalsePositive { get; set; }

        public DateTime agingDate { get; set; }
        public bool Open { get; set; }
        public DateTime StatusChanged { get; set; }
        public string Reviewer { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeEvent { get; set; }
        public long ProjectId { get; set; }
        public long SimilarityId { get; set; }
        public long FileNameHash { get; set; }
        public string ProjectName { get; set; }
        public long ScanId { get; set; }
        public long PathId { get; set; }
        public long VulnerabilityId { get; set; }
        public string Remark { get; set; }
        public string Severity { get; set; }
        public int Age { get; set; }
        public string DeepLink { get; set; }
        public long NodeLine { get; set; }
        public long NodeColumn { get; set; }
        public string NodeFileName { get; set; }
        public int ScanCount { get; set; }
        public string NodeCodeSnippet { get; set; }
        public long State { get; set; }
        public string QueryName { get; set; }
        public string QueryLanguage { get; set; }
        public long QueryCweId { get; set; }
        public DateTime firstScan { get; set; }
        public DateTime lastScan { get; set; }




        public Vulnerability(CxOptions options, string firstScan, string lastScan, int count, string Status, DateTime now, bool fp = false, bool open = true)
        {
            this.VulnerabilityStatus = Status;
            this.StatusChanged = DateTime.Parse(options.ScanFinished);
            this.firstScan = DateTime.Parse(firstScan);
            this.lastScan = DateTime.Parse(lastScan);
            this.ScanCount = count;
            this.ProjectId = options.ProjectId;
            this.ProjectName = options.ProjectName;
            this.PathId = options.PathId;
            this.ScanId = options.ScanId;
            this.VulnerabilityId = options.VulnerabilityId;
            this.NodeCodeSnippet = options.NodeCodeSnippet;
            this.NodeColumn = options.NodeColumn;
            this.NodeLine = options.NodeLine;
            this.State = options.State;
            this.QueryName = options.QueryName;
            this.QueryLanguage = options.QueryLanguage;
            this.SimilarityId = options.SimilarityId;
            this.FileNameHash = options.FileNameHash;
            this.Severity = options.Severity;
            this.NodeFileName = options.NodeFileName;
            this.DeepLink = options.DeepLink;
            this.QueryCweId = options.QueryCweId;

            this.isFalsePositive = fp;
            this.Open = open;
            if (fp)
            {
                this.VulnerabilityStatus = "Closed";
                this.Age = (now.Date - DateTime.Parse(lastScan)).Days;
                this.Open = false;
                this.agingDate = DateTime.Parse(lastScan);
            }
            else if (Status.Contains("Fixed"))
            {
                this.Age = (now.Date - DateTime.Parse(lastScan)).Days;
                this.Open = false;
                this.agingDate = DateTime.Parse(lastScan);
            }
            else if (Status.Contains("Recurring"))
            {
                this.Age = (now.Date - DateTime.Parse(firstScan)).Days;
                this.Open = true;
                this.agingDate = DateTime.Parse(firstScan);
            }
            else if (Status.Contains("New"))
            {
                this.Age = (now.Date - DateTime.Parse(firstScan)).Days;
                this.Open = true;
                this.agingDate = DateTime.Parse(firstScan);
            }

        }

        public void VulnerabilityRemark(string remark, string reviewer, string change, string changeDate)
        {
            this.ChangeDate = DateTime.Parse(changeDate);
            this.Reviewer = reviewer;
            this.ChangeEvent = change;
            this.Remark = remark;
        }

    }
    public class CxOptions
    {
        public string Key { get; set; }
        public long SimilarityId { get; set; }
        public long FileNameHash { get; set; }
        public string ScanFinished { get; set; }
        public string FalsePositive { get; set; }
        public string ProjectName { get; set; }
        public long ProjectId { get; set; }
        public long PathId { get; set; }
        public long ScanId { get; set; }
        public long VulnerabilityId { get; set; }
        public string Severity { get; set; }
        public string Remark { get; set; }
        public string DeepLink { get; set; }
        public long NodeLine { get; set; }
        public long NodeColumn { get; set; }
        public string NodeFileName { get; set; }
        public string NodeCodeSnippet { get; set; }
        public long State { get; set; }
        public string QueryName { get; set; }
        public string QueryLanguage { get; set; }
        public long QueryCweId { get; set; }



        public string setKey(long SimilarityId, long FileNameHash)
        {
            return String.Format("{0:D10}_{1:D10}", SimilarityId, FileNameHash);
        }

    }

    public class GetResultAging
    {
        private SQLiteMaster sqlite;
        private DateTime now;
        private resultClass token;
        public SortedDictionary<string, Vulnerability> vulnerability;

        public GetResultAging(SQLiteMaster sqlite, resultClass token)
        {
            this.sqlite = sqlite;
            this.now = DateTime.UtcNow;
            this.token = token;
            this.vulnerability = new SortedDictionary<string, Vulnerability>();
        }

        public Vulnerability VulnerabilityExists(long SimilarityId, long FileNameHash)
        {
            string Key = String.Format("{0:D10}_{1:D10}", SimilarityId, FileNameHash);
            if (this.vulnerability.ContainsKey(Key))
                return this.vulnerability[Key];

            return null;
        }

        public object GetResultAgingbySimilarityId(long ProjectId)
        {
            StringBuilder sql = new StringBuilder(String.Format("Select ScanId, ScanFinished from Scans where ProjectId = {0} ", ProjectId));
            StringBuilder range = new StringBuilder();
            if (token.start_time != null)
                range.Append(String.Format("and ScanFinished > datetime('{0:yyyy-MM-ddThh:mm:ss}') ", token.start_time));
            if (token.end_time != null)
                range.Append(String.Format("and ScanFinished < datetime('{0:yyyy-MM-ddThh:mm:ss}') ", token.end_time));

            var allscan = sqlite.SelectIntoDataTable("ScanResult", String.Format(" {0} {1} order by ScanFinished asc ", sql.ToString(), range.ToString())).AsEnumerable()

                .Select(row => new
                {
                    ScanId = row.Field<long>("ScanId"),
                    ScanFinished = row.Field<string>("ScanFinished")
                });

            var firstscan = allscan.FirstOrDefault();
            var lastscan = allscan.LastOrDefault();

            /*
                        var lastscan = sqlite.SelectIntoDataTable("ScanResult", String.Format(" {0} {1} order by ScanFinished {2} limit 1", sql.ToString(), range.ToString(), "DESC")).AsEnumerable()
                            .Select(row => new
                            {
                                ScanId = row.Field<long>("ScanId"),
                                ScanFinished = row.Field<string>("ScanFinished")
                            })
                            .FirstOrDefault();
             */
            sql.Clear();

            sql.Append(String.Format("Select * from Results where ProjectId = {0}", ProjectId));
            if (firstscan != null)
            {
                var first = sqlite.SelectIntoDataTable("FirstResult", String.Format("{0} and ScanID = {1} ", sql.ToString(), firstscan.ScanId)).AsEnumerable()
                    .Select(row => new CxOptions
                    {
                        Key = String.Format("{0:D10}_{1:D10}", row.Field<long>("SimilarityId"), row.Field<long>("FileNameHash")),
                        SimilarityId = row.Field<long>("SimilarityId"),
                        FileNameHash = row.Field<long>("FileNameHash"),
                        ScanFinished = row.Field<string>("ScanFinished"),
                        FalsePositive = row.Field<string>("FalsePositive"),
                        Remark = row.Field<string>("Remark"),
                        ProjectId = row.Field<long>("ProjectId"),
                        ProjectName = row.Field<string>("ProjectName"),
                        PathId = row.Field<long>("PathId"),
                        ScanId = row.Field<long>("ScanId"),
                        VulnerabilityId = row.Field<long>("VulnerabilityId"),
                        Severity = row.Field<string>("ResultSeverity"),
                        NodeCodeSnippet = row.Field<string>("NodeCodeSnippet"),
                        NodeColumn = row.Field<long>("NodeColumn"),
                        NodeFileName = row.Field<string>("NodeFileName"),
                        NodeLine = row.Field<long>("NodeLine"),
                        DeepLink = row.Field<string>("ResultDeepLink"),
                        State = row.Field<long>("State"),
                        QueryName = row.Field<string>("QueryName"),
                        QueryLanguage = row.Field<string>("QueryLanguage"),
                        QueryCweId = row.Field<long>("QueryCweId")

                    })
                     .ToHashSet();

                var last = sqlite.SelectIntoDataTable("LastResult", String.Format("{0} and ScanID = {1} ", sql.ToString(), lastscan.ScanId)).AsEnumerable()
                    .Select(row => new CxOptions
                    {
                        Key = String.Format("{0:D10}_{1:D10}", row.Field<long>("SimilarityId"), row.Field<long>("FileNameHash")),
                        SimilarityId = row.Field<long>("SimilarityId"),
                        FileNameHash = row.Field<long>("FileNameHash"),
                        ScanFinished = row.Field<string>("ScanFinished"),
                        FalsePositive = row.Field<string>("FalsePositive"),
                        Remark = row.Field<string>("Remark"),
                        ProjectId = row.Field<long>("ProjectId"),
                        ProjectName = row.Field<string>("ProjectName"),
                        PathId = row.Field<long>("PathId"),
                        ScanId = row.Field<long>("ScanId"),
                        VulnerabilityId = row.Field<long>("VulnerabilityId"),
                        Severity = row.Field<string>("ResultSeverity"),
                        NodeCodeSnippet = row.Field<string>("NodeCodeSnippet"),
                        NodeColumn = row.Field<long>("NodeColumn"),
                        NodeFileName = row.Field<string>("NodeFileName"),
                        NodeLine = row.Field<long>("NodeLine"),
                        DeepLink = row.Field<string>("ResultDeepLink"),
                        State = row.Field<long>("State"),
                        QueryName = row.Field<string>("QueryName"),
                        QueryLanguage = row.Field<string>("QueryLanguage"),
                        QueryCweId = row.Field<long>("QueryCweId")
                    })
                    .ToHashSet();

                //var firstKeys = new HashSet<long>(first.Select(x => x.SimilarityId));
                //string keys = String.Format(" and SimilarityId in ({0})", String.Join(',', firstKeys.ToArray()));

                var matchscan = allscan.Select(r => string.Format("{0}", string.Join(",", r.ScanId)));
                var scans = string.Join(",", matchscan);
                if (!String.IsNullOrEmpty(scans))
                    scans = String.Format(" and ScanId in ({0}) ", scans);
                else
                    scans = "";


                var all = sqlite.SelectIntoDataTable("AllResults", String.Format("{0} {1} {2} order by ScanFinished", sql.ToString(), range.ToString(), scans)).AsEnumerable()
                    .Select(row => new CxOptions
                    {
                        Key = String.Format("{0:D10}_{1:D10}", row.Field<long>("SimilarityId"), row.Field<long>("FileNameHash")),
                        SimilarityId = row.Field<long>("SimilarityId"),
                        FileNameHash = row.Field<long>("FileNameHash"),
                        ScanFinished = row.Field<string>("ScanFinished"),
                        FalsePositive = row.Field<string>("FalsePositive"),
                        Remark = row.Field<string>("Remark"),
                        ProjectId = row.Field<long>("ProjectId"),
                        ProjectName = row.Field<string>("ProjectName"),
                        PathId = row.Field<long>("PathId"),
                        ScanId = row.Field<long>("ScanId"),
                        VulnerabilityId = row.Field<long>("VulnerabilityId"),
                        Severity = row.Field<string>("ResultSeverity"),
                        NodeCodeSnippet = row.Field<string>("NodeCodeSnippet"),
                        NodeColumn = row.Field<long>("NodeColumn"),
                        NodeFileName = row.Field<string>("NodeFileName"),
                        NodeLine = row.Field<long>("NodeLine"),
                        DeepLink = row.Field<string>("ResultDeepLink"),
                        State = row.Field<long>("State"),
                        QueryName = row.Field<string>("QueryName"),
                        QueryLanguage = row.Field<string>("QueryLanguage"),
                        QueryCweId = row.Field<long>("QueryCweId")
                    })
                    .ToHashSet();


                var recurringKey = new HashSet<string>(first.Select(x => x.Key));
                var recurringLast = new HashSet<string>(last.Where(x => recurringKey.Contains(x.Key)).OrderBy(o => o.ScanFinished).Select(x => x.Key).Distinct());
                var recurringResult = (last.Where(x => recurringLast.Contains(x.Key)).Select(s => new { v = SetVulnerability(vulnerability, s, all, "Recurring") })).ToHashSet();
                first.RemoveWhere(x => recurringLast.Contains(x.Key));
                last.RemoveWhere(x => recurringLast.Contains(x.Key));

                var newKey = new HashSet<string>(last.Select(x => x.Key));
                var allNew = new HashSet<string>(all.Where(x => newKey.Contains(x.Key)).OrderBy(o => o.ScanFinished).Select(x => x.Key).Distinct());
                var allNewResult = all.Where(x => allNew.Contains(x.Key)).Select(s => new { v = SetVulnerability(vulnerability, s, all, "New") }).ToHashSet();
                last.RemoveWhere(x => allNew.Contains(x.Key));
                all.RemoveWhere(x => allNew.Contains(x.Key));

                var fixedKey = new HashSet<string>(first.Select(x => x.Key));
                var fixedAll = new HashSet<string>(all.Where(x => fixedKey.Contains(x.Key)).OrderByDescending(o => o.ScanFinished).Select(x => x.Key).Distinct());
                var allFixedResult = all.Where(x => fixedAll.Contains(x.Key)).Select(s => new { v = SetVulnerability(vulnerability, s, all, "Fixed") }).ToHashSet();
                last.RemoveWhere(x => fixedAll.Contains(x.Key));
                all.RemoveWhere(x => fixedAll.Contains(x.Key));


            }
            return this;

        }
        private Vulnerability SetVulnerability(SortedDictionary<string, Vulnerability> dict, CxOptions select, IEnumerable<CxOptions> options, String status)
        {
            Vulnerability v;
            if (select.ProjectId == 201)
                Console.WriteLine("{0} - {1} {2} {3} found", select.SimilarityId, select.FileNameHash, select.ProjectId, select.QueryName);


            var topScan = options.Where(x => x.Key == select.Key).OrderBy(o => o.ScanFinished).Select(x => new { ScanId = x.ScanId, ScanFinished = x.ScanFinished }).FirstOrDefault();
            var lastScan = options.Where(x => x.Key == select.Key).OrderBy(o => o.ScanFinished).Select(x => new { ScanId = x.ScanId, ScanFinished = x.ScanFinished }).LastOrDefault();
            int count = options.Where(x => x.ScanId >= topScan.ScanId && x.ScanId <= lastScan.ScanId).Select(row=> row.ScanId).Distinct().Count();

            if (!dict.ContainsKey(select.Key))
            {
                if (select.FalsePositive != "True")
                {
                    v = new Vulnerability(select, topScan.ScanFinished, lastScan.ScanFinished, count, status, now, false, true);
                    dict.Add(select.Key, v);
                    GetLastChangedRemark(select.Remark, v);
                }
                else
                {
                    v = new Vulnerability(select, topScan.ScanFinished, lastScan.ScanFinished, count, status, now, true, false);
                    dict.Add(select.Key, v);
                    GetLastChangedRemark(select.Remark, v);
                }
            }
            return null;
        }


        private void GetLastChangedRemark(string remark, Vulnerability v)
        {
            var splits = remark.Split("\r\n");
            Dictionary<int, ScanHistory> pairs = new Dictionary<int, ScanHistory>();
            int count = 0;
            foreach (string split in splits)
            {
                string result = GetPattern(split, "Changed status to");
                if (!String.IsNullOrEmpty(result))
                {
                    var dateSplit = result.Split(", [");
                    var dateTrim = dateSplit[1].Substring(0, dateSplit[1].LastIndexOf("]:"));
                    var changeEvent = result.Split("]:")[1];
                    var reviewer = dateSplit[0];
                    reviewer = reviewer.Substring(0, reviewer.LastIndexOf(' '));
                    pairs.Add(count, new ScanHistory(reviewer, changeEvent, dateTrim));
                    count++;
                }
            }
            v.history = pairs;
            if (pairs.ContainsKey(0))
            {
                ScanHistory history = pairs[0];
                v.VulnerabilityRemark(remark, history.Reviewer, history.Event, history.ChangeDate.ToString());
            }
        }

        private string GetPattern(string s, string pattern)
        {
            if (s.Contains(pattern))
                return s;
            return String.Empty;
        }
    }


    public class CWEID
    {
        public Dictionary<int[], string> owasp { get; set; }

        public int[] A1 = new int[] { 20, 23, 36, 73, 74, 77, 89, 90, 94, 98, 99, 113, 114, 117, 120, 121, 134, 135, 170, 193, 200, 400, 416, 425, 434, 470, 472, 476, 494, 501, 502, 552, 562, 624, 643, 652, 730, 776, 787, 789, 829, 915, 917, 10008, 10502, 10548, 10601, 10721 };
        public int[] A2 = new int[] { 15, 20, 201, 259, 269, 285, 293, 300, 303, 326, 362, 384, 472, 488, 520, 521, 522, 539, 547, 566, 603, 613, 732, 784, 798, 10012, 10014, 10024, 10027, 10704, 10710 };
        public int[] A3 = new int[] { 11, 12, 15, 200, 209, 248, 256, 257, 259, 260, 310, 311, 312, 315, 319, 321, 326, 327, 328, 330, 338, 359, 376, 377, 378, 379, 492, 499, 522, 523, 532, 535, 538, 539, 544, 547, 548, 549, 552, 599, 614, 615, 642, 646, 759, 760, 780, 10011, 10602, 10702 };
        public int[] A4 = new int[] { 611, 776 };
        public int[] A5 = new int[] { 15, 20, 22, 23, 36, 73, 77, 79, 98, 284, 285, 293, 378, 379, 472, 493, 501, 565, 566, 602, 603, 606, 610, 646, 668, 829, 915, 918, 10005, 10504, 10505 };
        public int[] A6 = new int[] { 12, 15, 20, 89, 101, 102, 103, 104, 105, 107, 108, 109, 110, 116, 120, 209, 243, 250, 254, 259, 260, 285, 321, 329, 330, 336, 346, 362, 457, 472, 489, 497, 533, 534, 539, 544, 547, 599, 605, 608, 614, 694, 732, 749, 798, 829, 838, 856, 922, 1021, 10520, 10544, 10546, 10549, 10708, 10711 };
        public int[] A7 = new int[] { 79, 83, 113, 352, 1004, 10501, 10706 };
        public int[] A8 = new int[] { 502 };
        public int[] A9 = new int[] { 20, 79, 89, 94, 111, 242, 329, 330, 352, 382, 398, 400, 477, 618, 667, 676, 695, 730, 937, 10703, 11215 };
        public int[] A10 = new int[] { 10000 };
        public CWEID()
        {
            owasp = new Dictionary<int[], string>();
            owasp.Add(A1, "A1-Injection");
            owasp.Add(A2, "A2-Broken Authentication");
            owasp.Add(A3, "A3-Sensitive Data Exposure");
            owasp.Add(A4, "A4-XML External Entities(XXE)");
            owasp.Add(A5, "A5-Broken Access Control");
            owasp.Add(A6, "A6-Security Misconfiguration");
            owasp.Add(A7, "A7-Cross - Site Scripting(XSS)");
            owasp.Add(A8, "A8-Insecure Deserialization");
            owasp.Add(A9, "A9-Using Components with Known Vulnerabilities");
            owasp.Add(A10, "A10-Insufficient Logging & Monitoring");
        }
    }

    public class mitre
    {

        public Dictionary<int[], string> cweId { get; set; }
        private resultClass token;

        public mitre(resultClass token)
        {
            this.token = token;
        }
        public void load_owasp(string topCategory = "1026")
        {
            cweId = new Dictionary<int[], string>();

            string xmlPath = String.Format("{0}{1}mitre{1}{2).xml", token.exe_path, token.os_path, topCategory);
            var xml = XElement.Load(xmlPath);
            var categories = xml.Descendants(xml.Name.Namespace.GetName("Categories")).Descendants(xml.Name.Namespace.GetName("Category"));
    
            foreach (XElement category in categories)
            {
                string name = (string)category.Attribute("Name");
                var allMembers = category.Descendants(xml.Name.Namespace.GetName("Has_Member"));
                List<int> intList = new List<int>();
                foreach (XElement hasMember in allMembers)
                {
                    intList.Add((int)hasMember.Attribute("CWE_ID"));
                }
                if (intList.Count == 0)
                    intList.Add(0);
                cweId.Add(intList.ToArray(), name);
            }

        }
        public void load_mitre(string topCategory = "1350")
        {

            string xmlPath = String.Format("{0}{1}mitre{1}{2).xml", token.exe_path, token.os_path, topCategory);
            var xml = XElement.Load(xmlPath);
            var categories = xml.Descendants(xml.Name.Namespace.GetName("Weaknesses")).Descendants(xml.Name.Namespace.GetName("Weakness"));
            //categories.Dump();
            foreach (XElement category in categories)
            {
                string name = (string)category.Attribute("Name");
                var allMembers = category.Descendants(xml.Name.Namespace.GetName("Related_Weakness"));
                List<int> intList = new List<int>();
                foreach (XElement hasMember in allMembers)
                {
                    intList.Add((int)hasMember.Attribute("CWE_ID"));
                }
                if (intList.Count == 0)
                    intList.Add(0);
                cweId.Add(intList.ToArray(), name);
            }
        }

    }

}

