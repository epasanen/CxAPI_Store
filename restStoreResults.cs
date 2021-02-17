using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using CxAPI_Store.dto;
using System.Threading;
using System.Runtime.InteropServices;

namespace CxAPI_Store
{
    class restStoreResults : IDisposable
    {
        public resultClass token;
        public List<ReportTrace> trace;
        public string _osPath;

        public restStoreResults(resultClass token)
        {
            this.token = token;
            this.trace = new List<ReportTrace>();
            string _os = RuntimeInformation.OSDescription;
            _osPath = _os.Contains("Windows") ? "\\" : "/";
        }

        public bool fetchResultsAndStore()
        {
            getProjectFiles getProjectFiles = new getProjectFiles(token);
            getScanResults scanResults = new getScanResults();

            if (getProjectFiles.saveFilteredScans(token))
            {

                foreach (ScanObject scan in getProjectFiles.CxScans)
                {
                    string scanName = String.Format("Results_{0:D10}_{1:yyyy-MM-ddTHH-mm-ssZ}.xml", scan.Id, scan.DateAndTime.StartedOn);
                    string scanDir = String.Format("{0}{1}{2:D10}{3}{4:D10}", token.archival_path, _osPath, Convert.ToInt64(scan.Project.Id), _osPath, scan.Id);
                    string scanPath = String.Format("{0}{1}{2}", scanDir, _osPath, scanName);

                    if (!File.Exists(scanPath))
                    {
                        ReportResult result = scanResults.SetResultRequest(scan.Id, "XML", token);
                        if (result != null)
                        {
                            trace.Add(new ReportTrace(scan.Project.Id, scan.Project.Name, getProjectFiles.CxTeams[scan.OwningTeamId].fullName, scan.DateAndTime.StartedOn, scan.Id, result.ReportId, "XML"));
                        }
                        if (trace.Count % token.max_threads == 0)
                        {
                            waitForResult(token, trace, scanResults);
                            trace.Clear();
                        }
                    }
                }
                waitForResult(token, trace, scanResults);
                trace.Clear();
            }
            return true;
        }

        public bool waitForResult(resultClass token, List<ReportTrace> trace, getScanResults scanResults)
        {
            bool waitFlag = false;
            DateTime wait_expired = DateTime.UtcNow;
            while (!waitFlag)
            {
                if (wait_expired.AddMinutes(2) < DateTime.UtcNow)
                {
                    Console.Error.WriteLine("waitForResult timeout! {0}", getTimeOutObjects(trace));
                    break;
                }
                if (token.debug && token.verbosity > 0) { Console.WriteLine("Sleeping 3 second(s)"); }
                Thread.Sleep(3000);

                foreach (ReportTrace rt in trace)
                {
                    if (!rt.isRead)
                    {
                        waitFlag = false;
                        if (rt.TimeStamp.AddMinutes(2) < DateTime.UtcNow)
                        {
                            Console.Error.WriteLine("ReportId/ScanId {0}/{1} timeout!", rt.reportId, rt.scanId);
                            rt.isRead = true;
                            continue;
                        }
                        if (scanResults.GetResultStatus(rt.reportId, token))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Reaady status for reportId {0}", rt.reportId); }
                            Thread.Sleep(2000);
                            var result = scanResults.GetResult(rt.reportId, token);
                            if (result != null)
                            {
                                if (token.debug && token.verbosity > 0) { Console.WriteLine("Fetch data for reportId {0}", rt.reportId); }
                                string scanName = String.Format("Results_{0:D10}_{1:yyyy-MM-ddTHH-mm-ssZ}.xml", rt.scanId, rt.scanTime);
                                string scanDir = String.Format("{0}{1}{2:D10}{3}{4:D10}", token.archival_path, _osPath, Convert.ToInt64(rt.projectId), _osPath, rt.scanId);
                                string scanPath = String.Format("{0}{1}{2}", scanDir, _osPath, scanName);
                                File.WriteAllText(scanPath, token.op_result, System.Text.Encoding.UTF8);
                                rt.isRead = true;
                            }
                            else
                            {
                                rt.isRead = true;
                                Console.Error.WriteLine("Failed processing reportId {0}", rt.reportId);
                                if (token.debug && token.verbosity > 1)
                                {
                                    Console.Error.WriteLine("Dumping XML:");
                                    Console.Error.Write(result.ToString());
                                }
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("Failed retrieving reportId {0}", rt.reportId);
                            rt.isRead = true;
                        }
                    }
                    else
                    {
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Waiting for reportId {0}", rt.reportId); }
                    }

                }
                waitFlag = true;
            }

            return true;
        }
        private string getTimeOutObjects(List<ReportTrace> trace)
        {
            string result = string.Empty;
            foreach (ReportTrace rt in trace)
            {
                result += string.Format("ProjectName {0}, ScanId {1}, TimeStamp {2}, isRead {3}", rt.projectName, rt.scanId, rt.TimeStamp, rt.isRead) + Environment.NewLine;
            }
            return result;
        }
        public void Dispose()
        {

        }

    }

}