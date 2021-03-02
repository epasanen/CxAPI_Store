﻿using System;
using System.Collections.Generic;
using CxAPI_Store.dto;
using CxAPI_Store;

namespace CxAPI_Store
{
    public class tokenClass
    {
        public string user_name { get; set; }
        public string credential { get; set; }
        public string grant_type { get; set; }
        public string scope { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string CxUrl { get; set; }
        public string CxAPIResolver { get; set; }
        public string CxSDKWebService { get; set; }
        public string get_token { get; set; }
        public string bearer_token { get; set; }
        public double expiration { get; set; }
        public DateTime timestamp { get; set; }

    }
 
    public class resultClass : tokenClass
    {
        public int status { get; set; }
        public string statusMessage { get; set; }
        public string request { get; set; }
        public api_action api_action { get; set; }
        public string file_path { get; set; }
        public string file_name { get; set; }
        public string save_result_path { get; set; }
        public string save_result_filename { get; set; }
        public string archival_path { get; set; }
        public string save_result { get; set; }
        public string op_result { get; set; }
        public byte[] byte_result { get; set; }
        public string appsettings { get; set; }
        public DateTime? start_time { get; set; }
        public DateTime? end_time { get; set; }
        public string session_id { get; set; }
        public string project_name { get; set; }
        public string team_name { get; set; }
        public string preset { get; set; }
        public bool pipe { get; set; }
        public string os_path { get; set; }
        public bool debug { get; set; }
        public bool test { get; set; }
        public int verbosity { get; set; }
        public int max_threads { get; set; }
        public int max_scans { get; set; }
        public bool use_proxy { get; set; }
        public bool proxy_use_default { get; set; }
        public bool scan_settings { get; set; }
        public string proxy_url { get; set; }
        public string proxy_username { get; set; }
        public string proxy_password { get; set; }
        public string proxy_domain { get; set; }
        public string report_name { get; set; }
        public string severity_filter { get; set; }
        public string filename_filter { get; set; }
        public bool purge_projects { get; set; }
        public string backup_path { get; set; }
        public string template_path { get; set; }
        public string template_file { get; set; }
        public string master_path { get; set; }

        List<ProjectObject> projectClass { get; set; }

        public resultClass()
        {
            debug = false;
            api_action = api_action.help;
            max_threads = 5;
            max_scans = 0;
            test = false;
            scan_settings = false;
            purge_projects = false;
            severity_filter = "High,Medium,Low,Info";
        }

        public void _setresultClass()
        {
            secure secure = new secure();
            settingClass settings = secure.get_settings();

            grant_type = settings.grant_type;
            scope = settings.scope;
            client_id = settings.client_id;
            client_secret = settings.client_secret;
            CxUrl = settings.CxUrl;
            timestamp = DateTime.UtcNow;
            os_path = secure._os.Contains("Windows") ? "\\" : "/";
            use_proxy = settings.use_proxy;
            proxy_use_default = settings.proxy_use_default;
            proxy_url = settings.proxy_url;

        }

    }
 

public class resultToken
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string token_type { get; set; }
    }

    public class encryptClass
    {
        public string user_name { get; set; }
        public string credential { get; set; }
        public string token { get; set; }
        public string token_creation { get; set; }
        public string token_expires { get; set; }
    }
    public class settingClass
    {
        public string CxUrl { get; set; }
        public string CxDefaultFilePath { get; set; }
        public string CxDefaultFileName { get; set; }
        public string CxDataFilePath { get; set; }
        public string CxDataFileName { get; set; }
        public string CxArchivalFilePath { get; set; }
        public string CxTemplatesPath { get; set; }
        public string CxTemplateFile { get; set; }
        public string CxBackupFilePath { get; set; }
        public string grant_type { get; set; }
        public string scope { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string token { get; set; }
        public string project { get; set; }
        public string scans { get; set; }
        public bool use_proxy { get; set; }
        public bool proxy_use_default { get; set; }
        public string proxy_url { get; set; }
        public string debug { get; set; }

    }
    public class settingToken
    {
        public string CxUrl { get; set; }
        public string action { get; set; }
    }
    public class csvScanOutput_1
    {
        public string Project_Name { get; set; }
        public string Owner { get; set; }
        public string Team { get; set; }
        public string Preset { get; set; }
        public int Total_Vulerabilities { get; set; }
        public DateTime Last_Scan { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Info { get; set; }

    }
    public class csvScanOutput_2
    {
        public string Team { get; set; }
        public string Project_Name { get; set; }
        public string Owner { get; set; }
        public string Onboarded { get; set; }
        public DateTime Onboarding_Date { get; set; }
        public int Total_Vulerabilities { get; set; }
        public DateTime Last_Scan { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }

    }

    public class CxUsers
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string email { get; set; }
        public bool isAudit { get; set; }
        public DateTime createDate { get; set; }
        public DateTime lastLogin { get; set; }
        public string exparation { get; set; }
        public string company { get; set; }
        public bool isActive { get; set; }
        public string jobTitle { get; set; }
        public string teams { get; set; }
    }
}


