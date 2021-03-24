using System;
using CxAPI_Store.dto;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace CxAPI_Store
{
    class getScanResults
    {
        public XElement GetResult(long report_id, resultClass token, int timeout = 30)
        {
            string path = String.Empty;
            try
            {
                get httpGet = new get();

                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxReportFetch, report_id));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path, timeout);
                if (token.status == 0)
                {
                    string result = token.op_result;
                    XElement xl = XElement.Parse(result);
                    return xl;
                }
            }
            catch (Exception ex)
            {
                token.status = -1;
                token.statusMessage = ex.Message;
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("GetResult: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return null;
        }
        public byte[] GetGenaricResult(long report_id, resultClass token)
        {
            string path = String.Empty;
            try
            {
                get httpGet = new get();

                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxReportFetch, report_id));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path);
                if (token.status == 0)
                {
                    return token.byte_result;
                }
            }
            catch (Exception ex)
            {
                token.status = -1;
                token.statusMessage = ex.Message;
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("GetGenaricResult: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }

            }
            return null;
        }

        public long GetResultStatus(long report_id, resultClass token)
        {
            int failure = 3;
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxReportStatus, report_id));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path);
                if (token.status == 0)
                {
                    ReportReady ready = JsonConvert.DeserializeObject<ReportReady>(token.op_result);
                    if (token.debug && token.verbosity > 0)
                    {
                        Console.WriteLine("GetResultStatus: Ready: {0}/{1}", ready.Status.Id, ready.Status.Value);
                    }
                    token.status = (int)ready.Status.Id;
                    token.op_result = ready.Status.Value;
                    return ready.Status.Id;

                }
                else
                {
                    if (token.debug && token.verbosity > 0)
                    {
                        Console.Error.WriteLine("GetResultStatus: bad status returned (0)", token.op_result);
                    }
                }
            }
            catch (Exception ex)
            {
                token.status = -1;
                token.statusMessage = ex.Message;
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("GetResultStatus: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            token.status = failure;
            token.op_result = "Failed";
            return failure;
        }

        public ReportResult SetResultRequest(long scan_id, string report_type, resultClass token)
        {
            string path = String.Empty;
            try
            {
                ReportRequest request = new ReportRequest()
                {
                    reportType = report_type,
                    scanId = scan_id
                };

                post Post = new post();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.post_rest_Uri(CxConstant.CxReportRegister);
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                Post.post_Http(token, path, request);
                if (token.status == 0)
                {
                    ReportResult report = JsonConvert.DeserializeObject<ReportResult>(token.op_result);
                    return report;
                }
            }
            catch (Exception ex)
            {
                token.status = -1;
                token.statusMessage = ex.Message;
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("SetResultRequest: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }

            }
            return null;
        }
    }
}
