using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

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
}
