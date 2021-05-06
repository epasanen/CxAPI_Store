using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using CxAPI_Store.dto;
using System.Threading;
using System.Runtime.InteropServices;
using System.Linq;

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
            if (!Directory.EnumerateFiles(token.archival_path,"sast_project_info.*.log").Any())
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
            }
            else
            {
                throw new Exception("This is a CxAnalytix directory. Run CxAnalytix to update files");
            }
            return true;
        }

        public bool waitForResult(resultClass token, List<ReportTrace> trace, getScanResults scanResults)
        {
            int read_timeout = token.result_timeout * token.max_threads;
            int wait_timeout = read_timeout * 2;

            bool waitFlag = false;
            DateTime wait_expired = DateTime.UtcNow;
            while (!waitFlag)
            {
                if (wait_expired.AddSeconds(wait_timeout) < DateTime.UtcNow)
                {
                    Console.Error.WriteLine("waitForResult timeout! {0} seconds", wait_timeout);
                    getTimeOutObjects(trace);
                    break;
                }
                if (token.debug && token.verbosity > 0) { Console.WriteLine("Sleeping 3 second(s)"); }
                Thread.Sleep(3000);
                bool noHold = true;
                foreach (ReportTrace rt in trace)
                {
                    if (!rt.isRead)
                    {
                        waitFlag = false;
                        if (rt.TimeStamp.AddMinutes(read_timeout) < DateTime.UtcNow)
                        {
                            Console.Error.WriteLine("Timeout! {0} seconds. ReportId/ScanId/ProjectID/ProjectName {1}/{2}/{3}/{4}", rt.reportId, rt.scanId, rt.projectId, rt.projectName);
                            rt.isRead = true;
                            continue;
                        }
                        if (scanResults.GetResultStatus(rt.reportId, token) == 2)
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Ready status for reportId {0}/{1}/{2}/{3}", rt.reportId, rt.scanId, rt.projectId, rt.projectName); }
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Wait a max of {0} seconds",token.result_timeout); }
                            var result = scanResults.GetResult(rt.reportId, token, token.result_timeout);
                            if (result != null)
                            {
                                if (token.debug && token.verbosity > 0) { Console.WriteLine("Fetch data successful for reportId {0}/{1}/{2}/{3}", rt.reportId, rt.scanId, rt.projectId, rt.projectName); }
                                string scanName = String.Format("Results_{0:D10}_{1:yyyy-MM-ddTHH-mm-ssZ}.xml", rt.scanId, rt.scanTime);
                                string scanDir = String.Format("{0}{1}{2:D10}{3}{4:D10}", token.archival_path, _osPath, Convert.ToInt64(rt.projectId), _osPath, rt.scanId);
                                string scanPath = String.Format("{0}{1}{2}", scanDir, _osPath, scanName);
                                File.WriteAllText(scanPath, token.op_result, System.Text.Encoding.UTF8);
                                rt.isRead = true;
                            }
                            else
                            {
                                rt.isRead = true;
                                Console.Error.WriteLine("Failed fetch of report {0}/{1}/{2}/{3}", rt.reportId,rt.scanId,rt.projectId,rt.projectName);
                                string scanName = String.Format("Results_{0:D10}_{1:yyyy-MM-ddTHH-mm-ssZ}.hold", rt.scanId, rt.scanTime);
                                string scanDir = String.Format("{0}{1}{2:D10}{3}{4:D10}", token.archival_path, _osPath, Convert.ToInt64(rt.projectId), _osPath, rt.scanId);
                                string scanPath = String.Format("{0}{1}{2}", scanDir, _osPath, scanName);
                                File.WriteAllText(scanPath, String.Format("~~Error fetch of report {0}/{1}/{2}/{3}", rt.reportId,rt.scanId,rt.projectId,rt.projectName, System.Text.Encoding.UTF8));
                                if (token.debug && token.verbosity > 0) { Console.WriteLine("Write placeholder for reportId {0}/{1}/{2}/{3}", rt.reportId, rt.scanId, rt.projectId, rt.projectName); }
                                if (token.debug && token.verbosity > 1)
                                {
                                    Console.Error.WriteLine("Dumping XML:");
                                    Console.Error.Write(result.ToString());
                                }
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("Report status {0}/{1}/{2}/{3}/{4}", rt.reportId, rt.scanId, rt.projectId, rt.projectName, token.op_result);
                            if (token.status == 3) //Failure
                            {
                                rt.isRead = true;
                            }
                            else
                            {
                                noHold = false;
                            }
                        }
                    }
                    else
                    {
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Waiting for reportId {0}", rt.reportId); }
                    }

                }
                waitFlag = noHold;
            }

            return true;
        }
        private void getTimeOutObjects(List<ReportTrace> trace)
        {
            string result = string.Empty;
            foreach (ReportTrace rt in trace)
            {
                Console.Error.WriteLine("ProjectName {0}, ProjectId {1},  ScanId {2}, TimeStamp {3}, isRead {3}", rt.projectName, rt.projectId, rt.scanId, rt.TimeStamp, rt.isRead);
            }

        }
        public void Dispose()
        {

        }

    }

}