using System;
using System.Diagnostics;
using CxAPI_Store;

namespace CxAPI_Store
{
    class dispatcher
    {
        public Stopwatch stopWatch;

        public resultClass dispatch(string[] args)
        {
            resultClass token = Configuration.mono_command_args(args);
            if (token.status != 0) { return token; }
            _options.debug = token.debug;
            _options.level = token.verbosity;
            _options.test = token.test;
            _options.token = token;
            return dispatchTree(token);
        }
        private resultClass dispatchTree(resultClass token)
        {
            secure secure = new secure(token);      
            fetchToken newtoken = new fetchToken();
            switch (token.api_action)
            {
                case api_action.getToken:
                    {
                        newtoken.get_token(secure.decrypt_Credentials());
                        break;
                    }
                case api_action.storeCredentials:
                    {
                        storeCredentials cred = new storeCredentials();
                        token = cred.save_credentials(token);
                        if (token.debug)
                        {
                            newtoken.get_token(secure.decrypt_Credentials());
                        }
                        break;
                    }
                case api_action.scanResults:
                    {
                        token = newtoken.get_token(secure.decrypt_Credentials());
                        if (token.report_name.Contains("REST_REPORT_SUMMARY"))
                        {
                            using (restReportSummary restReportSummary = new restReportSummary(token))
                            {
                                restReportSummary.fetchReport();
                            }
                        }

                        if (token.report_name.Contains("REST_REPORT_DETAILS"))
                        {
                            using (restReportDetails restReportDetail = new restReportDetails(token))
                            {
                                restReportDetail.fetchReport();
                            }
                        }

                        break;
                    }
                case api_action.archivalResults:
                    {
                        token = newtoken.get_token(secure.decrypt_Credentials());
                        using (restStoreResults restStoreResult = new restStoreResults(token))
                        {
                            restStoreResult.fetchResultsAndStore();
                        }

                        break;
                    }
                case api_action.archiveAndScan:
                    {
                        token = newtoken.get_token(secure.decrypt_Credentials());
                        using (restStoreResults restStoreResult = new restStoreResults(token))
                        {
                            restStoreResult.fetchResultsAndStore();
                        }
                        token.api_action = api_action.scanResults;
                        token = dispatchTree(token);

                        break;
                    }

                default:
                    {
                        Console.WriteLine("Cannot find valid report name or operation {0}-{1}", token.api_action, token.report_name);
                        break;
                    }
            }
            return token;
        }
    public dispatcher()
    {
        stopWatch = new Stopwatch();
        stopWatch.Start();
        Console.WriteLine("Start Time: {0}", DateTime.UtcNow.ToString());
    }
    public void Elapsed_Time()
    {
        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
        Console.WriteLine("Stop Time: {0}", DateTime.UtcNow.ToString());
        Console.WriteLine("Total elapsed time: {0}", elapsedTime);
    }
}
}
