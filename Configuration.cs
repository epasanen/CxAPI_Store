using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using CxAPI_Store;
using Microsoft.Extensions.Configuration;
using Mono.Options;


namespace CxAPI_Store
{
    class Configuration
    {
        public static IConfigurationRoot _configuration;
        public static string[] _keys;
        public static resultClass _token;

        public static string ospath()
        {
            string _os = RuntimeInformation.OSDescription;
            return  _os.Contains("Windows") ? "\\" : "/";
        }
        public static IConfigurationRoot configuration(string[] args, string path)
        {
            string set_path = String.IsNullOrEmpty(path) ? System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) : System.IO.Path.GetDirectoryName(path);
            string set_file = String.IsNullOrEmpty(path) ? "appsettings.json" : System.IO.Path.GetFileName(path);
            Console.WriteLine(@"Using configuration in path {0}{1}{2}", set_path, ospath(), set_file);
            IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(set_path)
            .AddJsonFile(set_file, optional: true, reloadOnChange: true)
            .AddCommandLine(args);
            _keys = args;

            _configuration = builder.Build();


            return _configuration;
        }
        public static IConfigurationRoot configuration()
        {
            if (_configuration != null) return _configuration;

            Console.WriteLine(@"Using executable path configuration {0}", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
            return _configuration;
        }

        public static string getVersion()
        {
            Assembly assembly = Assembly.Load("CxAPI_Store");
            Version ver = assembly.GetName().Version;
            return "Version: " + ver.Major.ToString() + "." + ver.Minor.ToString() + "." + ver.Revision.ToString() + " build (" + ver.Build.ToString() + ")";
        }
        public static string getdotNet()
        {
            return "dotnet Version: " + Environment.Version.ToString();
        }

        public static settingClass get_settings()
        {
            settingClass _settings = new settingClass();
            _configuration.GetSection("CxRest").Bind(_settings);
            return _settings;
        }

        public static HttpClient _HttpClient(resultClass token)
        {
            if (token.use_proxy)
            {
                Uri uri = new Uri(token.proxy_url);
                Console.WriteLine(@"Proxy found {0}://{1}:{2}", uri.Scheme, uri.Host, uri.Port);
                HttpClientHandler handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(uri),
                    UseProxy = true
                };
                if (!String.IsNullOrEmpty(token.proxy_username))
                {
                    handler.Credentials = new NetworkCredential(token.proxy_username, token.proxy_password, token.proxy_domain);
                    if (token.debug)
                    {
                        Console.WriteLine(@"Using proxy {0} with credentials {1}\\{2}", token.proxy_url, token.proxy_domain, token.proxy_username);
                    }
                }
                else if (token.proxy_use_default)
                {
                    string[] domain = token.user_name.Replace(@"\\", "").Split('\\');
                    if (domain.Length == 1)
                    {
                        handler.Credentials = new NetworkCredential(domain[0], token.credential);
                        if (token.debug)
                        {
                            Console.WriteLine(@"Using proxy {0} with credentials {1}", token.proxy_url, domain[0]);
                        }
                    }
                    else
                    {
                        handler.Credentials = new NetworkCredential(domain[1], token.credential, domain[0]);
                        if (token.debug)
                        {
                            Console.WriteLine(@"Using proxy {0} with credentials {1}\\{2}", token.proxy_url, domain[0], domain[1]);
                        }
                    }
                }
                HttpClient httpclient = new HttpClient(handler, true);
                Console.WriteLine(@"Proxy configuration {0}", handler.Proxy.GetProxy(new Uri(token.CxUrl)));
                return httpclient;
            }
            else
            {
                return new HttpClient();
            }
        }

        public static void debug_configuration(settingClass _settings, resultClass _token)
        {
            string _os = RuntimeInformation.OSDescription;
            string folder = _os.Contains("Windows") ? "\\" : "/";
            Console.WriteLine("-----------------------------------------------------------------------");
            Console.WriteLine("Setting file: {0}", _settings.CxDataFilePath + folder + _settings.CxDataFileName);
            Console.WriteLine("Executable Path: {0}", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
          
            Console.WriteLine("Configuration Settings:");
            foreach (PropertyInfo propertyInfo in _settings.GetType().GetProperties())
            {
                Console.WriteLine("{0}:{1}", propertyInfo.Name, propertyInfo.GetValue(_settings, null));
            }
            Console.WriteLine("-----------------------------------------------------------------------");
            Console.WriteLine("Command Line Settings:");
            foreach (PropertyInfo propertyInfo in _token.GetType().GetProperties())
            {
                Console.WriteLine("{0}:{1}", propertyInfo.Name, propertyInfo.GetValue(_token, null));
            }
            Console.WriteLine("-----------------------------------------------------------------------");
            Console.WriteLine("");

        }
        public static resultClass mono_command_args(string[] args)
        {
            resultClass token = new resultClass();
            List<string> extra;

            var p = new OptionSet() {
                { "a|appsettings=", "Optional, point to alternate appsettings directory",
                  v => token.appsettings = v },
                { "t|get_token", "Fetch the bearer token from the CxSAST service",
                  v => token.api_action = api_action.getToken },
                { "c|store_credentials", "Store username and credential in an encrypted file",
                  v => token.api_action = api_action.storeCredentials },
                { "tt|tenant=", "Tag DB with tenant name",
                  v => token.tenant = v },
                { "r|reports", "Create reports, filtered by project, time and other properties",
                  v => token.api_action = api_action.generateReports },
                { "af|API_Files", "Fetch data using API and store in files (when not using CxAnalytix)",
                  v => token.api_action = api_action.archivetoFiles },
                { "fdb|Files_to_DB", "Store into DataSet, filtered by time and project",
                  v => token.api_action = api_action.buildDataSet },
                { "ai|add_indexes", "Add default indexes",
                  v => token.api_action = api_action.add_indexes },
                { "tool|tools", "Launch a development tool",
                  v => token.api_action = api_action.tools },
                { "init|initialize", "Initialize DataSet before storing data",
                  v => token.initialize = true  },
                { "auto|autosave", "Save DataSet after each project loaded.",
                  v => token.autosave = true  },
                { "pn|project_name=", "Filter with project name, Will return project if any portion of the project name is a match",
                  v => token.project_name = v },
                { "tn|team_name=", "Filter with team name, Will return a team if any portion of the team name is a match",
                  v => token.team_name = v },
                { "psn|preset_name=", "Filter with preset name, Will return a preset if any portion of the team name is a match",
                  v => token.preset = v },
                { "path|file_path=", "Override file path in configuration",
                  v => token.file_path = v },
                { "file|file_name=", "Override file name in configuration",
                  v => token.file_name = v },
                { "sf|severity_filter=", "Filter results by Severity",
                  v => token.severity_filter = v },
                { "ap|archival_path=", "Override archival path in configuration",
                  v => token.archival_path = v },
                { "ab|backup_path=", "Where to save archival path zip backup files",
                  v => token.backup_path = v },
                { "u|user_name=", "The username to use to retreive the token (REST) or session (SOAP)",
                  v => token.user_name = v },
                { "p|password=", "The password needed to retreive the token (REST) or session (SOAP)",
                  v => token.credential = v },
                { "st|start_time=", "Last scan start time",
                  v => token.start_time = DateTime.Parse(v)},
                { "et|end_time=", "Last scan end time",
                  v => token.end_time = DateTime.Parse(v)},
                { "tp|template_path=", "Provide path to templates",
                  v => token.template_path = v },
                { "tf|template_file=", "Provide name of template",
                  v => token.template_file = v },
                { "can|canned", "Use one of the canned reports",
                  v => token.canned = true },
                { "rn|report_name=", "Name of canned report object",
                  v => token.report_name = v },
                { "of|output_type=", "Output report types",
                  v => token.output_type = v },
                //add proxy stuff
                { "up|use_proxy", "Use web proxy",
                  v => token.use_proxy = true },
                { "ud|proxy_use_default", "Use default credentials",
                  v => token.proxy_use_default = true },
                { "pu|proxy_user_name=", "Proxy User Name",
                  v => token.proxy_username = v },
                { "pp|proxy_password=", "Proxy Password",
                  v => token.proxy_password = v },
                { "pd|proxy_domain=", "Proxy Domain",
                  v => token.proxy_domain = v},
                //proxy
                { "v|verbose=", "Change degrees of debugging info",
                  v => token.verbosity = Convert.ToInt32(v) },
                { "mt|max_threads=", "Change the max number of report requests to CxManager",
                  v => token.max_threads = Convert.ToInt32(v) },
                { "ml|max_length=", "Change the max size of data extracted from XML",
                  v => token.max_length = Convert.ToInt32(v) },
                { "ms|max_scans=", "Change the max number of report requests to CxManager",
                  v => token.max_scans = Convert.ToInt32(v) },
                { "dbw|max_write=", "Change the bulk load to DB",
                  v => token.max_write = Convert.ToInt32(v) },
                { "qu|query_filter=", "Set to add additional filters to project extraction",
                  v => token.query_filter = v},
                { "to|result_timeout=", "Set the maximum time to wait for a result to upload.",
                  v => token.result_timeout = Convert.ToInt32(v)},
                { "d|debug", "Output debugging info ",
                  v => token.debug = true },
                { "H|halt", "Exit by entering any key at end of process. ",
                  v => token.stop = true },
                { "T|test", "Set up test DB, folders",
                  v => token.test = true },
                { "?|h|help",  "show your options",
                  v => token.api_action = api_action.help},
            };
            try
            {
                extra = p.Parse(args);
                Configuration.configuration(args, token.appsettings);
                token._setresultClass();
                settingClass _settings = get_settings();
                misc_setup(token,_settings);
                if (_settings.use_proxy || token.use_proxy)
                {
                    token.use_proxy = true;
                    token.proxy_use_default = token.proxy_use_default ? true : _settings.proxy_use_default;
                    token.proxy_url = String.IsNullOrEmpty(token.proxy_url) ? _settings.proxy_url : token.proxy_url;
                    Console.WriteLine("Using proxy {0}", token.proxy_url);
                }

                if (token.debug && token.verbosity > 0)
                {
                    debug_configuration(_settings,token);
                }
            }

            // if (String.IsNullOrEmpty(token.file_name) ? _configuration
            catch (OptionException e)
            {
                Console.Write("CxAPI_Store: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try CxApi --help' for more information.");
                token.status = -1;
            }

            if (token.api_action == api_action.help)
            {
                ShowHelp(p);
            }
            _token = token;
            return _token;
       }

        private static void misc_setup(resultClass token, settingClass _settings)
        {

            token.exe_path = String.Format("{0}", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            token.master_path = String.IsNullOrEmpty(token.template_path) ? String.Format("{0}{1}templates", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), ospath()) : token.template_path;
            token.file_name = String.IsNullOrEmpty(token.file_name) ? _settings.CxDefaultFileName : token.file_name;
            token.file_path = String.IsNullOrEmpty(token.file_path) ? _settings.CxDefaultFilePath : token.file_path;
            token.end_time = (token.end_time == null) ? DateTime.Today : token.end_time;
            token.start_time = (token.start_time == null) ? DateTime.Today.AddYears(-1) : token.start_time;
            token.archival_path = String.IsNullOrEmpty(token.archival_path) ? _settings.CxArchivalFilePath : token.archival_path;
            token.sqlite_connection = String.IsNullOrEmpty(token.sqlite_connection) ? _settings.CxSQLite : token.sqlite_connection;
            token.backup_path = String.IsNullOrEmpty(token.backup_path) ? _settings.CxBackupFilePath : token.backup_path;
            token.tenant = String.IsNullOrEmpty(token.tenant) ? _settings.CxTenant : token.tenant;
            token.tenant = String.IsNullOrEmpty(token.tenant) ? "Default" : token.tenant;
            token.template_path = String.IsNullOrEmpty(token.template_path) ? _settings.CxTemplatesPath : token.template_path;
            token.template_file = String.IsNullOrEmpty(token.template_file) ? _settings.CxTemplateFile : token.template_file;
            token.dump_path = String.IsNullOrEmpty(token.dump_path) ? token.file_path : token.dump_path;
            token.dump_file = String.IsNullOrEmpty(token.dump_file) ? token.file_name : token.dump_file;
            token.exe_directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            token.db_allow_write = _settings.CxSQLiteAllowWrite;
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: CxApi action arguments");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }


}


