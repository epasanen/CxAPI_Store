﻿
using System;
using System.Collections.Generic;
using CxAPI_Store.dto;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;

namespace CxAPI_Store
{
    class getProjectFiles : IDisposable
    {
        public List<ScanObject> CxScans;
        public List<ProjectObject> CxProjects;
        public Dictionary<string, Teams> CxTeams;
        public Dictionary<long, Presets> CxPresets;
        public Dictionary<long, ScanSettings> CxSettings;
        public Dictionary<long, ScanStatistics> CxResultStatistics;
        public List<ReportTrace> trace;
        public string _osPath;

        public getProjectFiles(resultClass token)
        {
            try
            {
                string _os = RuntimeInformation.OSDescription;
                _osPath = _os.Contains("Windows") ? "\\" : "/";
                CxResultStatistics = new Dictionary<long, ScanStatistics>();
                CxSettings = new Dictionary<long, ScanSettings>();
                trace = new List<ReportTrace>();

                // See if the archival directory is available
                String archive = token.archival_path;
                String backup = token.backup_path;
                String cxStoreBackup = String.Format("{0}{1}CxStore_{2:yyyy-MM-ddTHH-mm-ssZ}.zip", backup, _osPath, DateTime.Now);

                if (!Directory.Exists(archive))
                {
                    Directory.CreateDirectory(archive);
                }
                if (!Directory.Exists(backup))
                {
                    Directory.CreateDirectory(backup);
                }
                ZipFile.CreateFromDirectory(archive, cxStoreBackup);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool saveFilteredScans(resultClass token)
        {
            CxProjects = createProjectFolderifNotExists(token);
            CxTeams = get_team_list(token);
            CxPresets = get_preset_list(token);
            CxScans = filter_scans(token);
            saveTeamsinFile(token);
            savePresetsinFile(token);
            if (CxScans.Count == 0)
            {
                Console.Error.WriteLine("No scans were found, please check arguments and retry.");
                return false;
            }
            return true;
        }
        public bool saveTeamsinFile(resultClass token)
        {
            if (!File.Exists(String.Format("{0}{1}000000000_Teams.json", token.archival_path, _osPath)) || (token.purge_projects))
            {
                List<Teams> teams = new List<Teams>();
                foreach (string key in CxTeams.Keys)
                {
                    teams.Add(CxTeams[key]);
                }
                File.WriteAllText(String.Format("{0}{1}000000000_Teams.json", token.archival_path, _osPath), JsonConvert.SerializeObject(teams));
            }
            return true;
        }
        public bool savePresetsinFile(resultClass token)
        {
            if (!File.Exists(String.Format("{0}{1}000000000_Presets.json", token.archival_path, _osPath)) || (token.purge_projects))
            {
                List<Presets> presets = new List<Presets>();
                foreach (long key in CxPresets.Keys)
                {
                    presets.Add(CxPresets[key]);
                }
                File.WriteAllText(String.Format("{0}{1}000000000_Presets.json", token.archival_path, _osPath), JsonConvert.SerializeObject(presets));
            }
            return true;
        }

        public List<ProjectObject> get_projects(resultClass token)
        {
            get httpGet = new get();
            List<ProjectObject> pclass = new List<ProjectObject>();
            secure token_secure = new secure(token);
            token_secure.findToken(token);
            string path = token_secure.get_rest_Uri(CxConstant.CxAllProjects);
            if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
            httpGet.get_Http(token, path);
            if (token.status == 0)
            {
                pclass = JsonConvert.DeserializeObject<List<ProjectObject>>(token.op_result);
            }

            return pclass;
        }

        public List<Presets> get_presets(resultClass token)
        {
            get httpGet = new get();
            List<Presets> pclass = new List<Presets>();

            secure token_secure = new secure(token);
            token_secure.findToken(token);
            string path = token_secure.get_rest_Uri(CxConstant.CxPresets);
            if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
            httpGet.get_Http(token, path);
            if (token.status == 0)
            {
                pclass = JsonConvert.DeserializeObject<List<Presets>>(token.op_result);
            }
            return pclass;
        }
        public Dictionary<string, Teams> get_team_list(resultClass token)
        {
            getScans scans = new getScans();
            Dictionary<string, Teams> result = new Dictionary<string, Teams>();
            List<Teams> tclass = scans.getTeams(token);
            foreach (Teams t in tclass)
            {
                if (!result.ContainsKey(t.id))
                {
                    result.Add(t.id, t);
                }
            }
            return result;
        }

        public Dictionary<long, Presets> get_preset_list(resultClass token)
        {
            getScans scans = new getScans();
            Dictionary<long, Presets> result = new Dictionary<long, Presets>();
            List<Presets> presets = get_presets(token);
            foreach (Presets p in presets)
            {
                if (!result.ContainsKey(Convert.ToInt64(p.id)))
                {
                    result.Add(Convert.ToInt64(p.id), p);
                }
            }
            return result;
        }

        public List<ScanObject> filter_scans(resultClass token)
        {
            getScans scans = new getScans();
            List<ScanObject> outScans = new List<ScanObject>();
            List<ScanObject> projectScans = new List<ScanObject>();
            Dictionary<long, ScanObject> lastOne = new Dictionary<long, ScanObject>();

            foreach (ProjectObject project in CxProjects)
            {
                if (testScans(token, project))
                {
                    List<ScanObject> temp = (token.max_scans > 0) ? scans.getLastScanbyId(token, project.id, token.max_scans) : scans.getScanbyId(token, project.id);
                    projectScans.AddRange(temp);
                }
            }
            foreach (ScanObject scanObject in projectScans)
            {
                if ((scanObject.DateAndTime != null) && (scanObject.Status.Id == 7) && (scanObject.DateAndTime.StartedOn > token.start_time) && (scanObject.DateAndTime.StartedOn < token.end_time))
                {
                    string scanName = String.Format("Scan_{0:D10}_{1:yyyy-MM-ddTHH-mm-ssZ}.json", scanObject.Id, scanObject.DateAndTime.StartedOn);
                    string scanDir = String.Format("{0}{1}{2:D10}{3}{4:D10}", token.archival_path, _osPath, Convert.ToInt64(scanObject.Project.Id), _osPath, scanObject.Id);
                    string scanPath = String.Format("{0}{1}{2}", scanDir, _osPath, scanName);
                    if (!File.Exists(scanPath))
                    {
                        Directory.CreateDirectory(scanDir);
                        File.WriteAllText(scanPath, JsonConvert.SerializeObject(scanObject));
                        string scanStats = String.Format("ScanStatistics_{0:D10}_{1:yyyy-MM-ddTHH-mm-ssZ}.json", scanObject.Id, scanObject.DateAndTime.StartedOn);
                        string scanStatPath = String.Format("{0}{1}{2}", scanDir, _osPath, scanStats);
                        ScanStatistics scanStatistics = getscanStatistics(token, scanObject.Id);
                        File.WriteAllText(scanStatPath, JsonConvert.SerializeObject(scanStatistics));
                    }
                    outScans.Add(scanObject);
                }
            }
            return outScans;
        }

        public bool testScans(resultClass token, ProjectObject project)
        {
            return testProject(token, project) && testTeam(token, project) && testPreset(token, project);
        }
        public bool testProject(resultClass token, ProjectObject project)
        {
            return String.IsNullOrEmpty(token.project_name) || project.name.Contains(token.project_name);
        }
        public bool testTeam(resultClass token, ProjectObject project)
        {
            bool teamFlag = false;
            List<string> teams = new List<string>();
            if (String.IsNullOrEmpty(token.team_name))
            {
                teamFlag = true;
            }
            else
            {
                foreach (string key in CxTeams.Keys)
                {
                    Teams team = CxTeams[key];
                    if (team.name.Contains(token.team_name))
                    {
                        if (project.teamId == team.id)
                        {
                            teamFlag = true;
                            break;
                        }
                    }
                }
            }
            return teamFlag;
        }
        public bool testPreset(resultClass token, ProjectObject project)
        {
            bool presetFlag = false;
            if (String.IsNullOrEmpty(token.preset))
            {
                presetFlag = true;
            }
            else
            {
                foreach (long key in CxPresets.Keys)
                {
                    Presets preset = CxPresets[key];
                    if (preset.name.Contains(token.preset))
                    {
                        ScanSettings scanSettings = CxSettings[Convert.ToInt64(project.id)];
                        {
                            if (scanSettings.preset.id == preset.id)
                                presetFlag = true;
                            break;
                        }
                    }
                }
            }
            return presetFlag;
        }

        public ScanStatistics getscanStatistics(resultClass token, long scanId)
        {
            getScans scans = new getScans();
            ScanStatistics scanStatistics = scans.getScansStatistics(scanId, token);
            return scanStatistics;
        }
        public List<ProjectObject> createProjectFolderifNotExists(resultClass token)
        {
            getScans scans = new getScans();
            List<ProjectObject> projectObjects = get_projects(token);
            List<string> projectName = new List<string>();
            foreach (ProjectObject project in projectObjects)
            {
                if (String.IsNullOrEmpty(token.project_name) || (project.name.Contains(token.project_name)))
                {
                    string projectPath = String.Format("{0}{1}{2:D10}", token.archival_path, _osPath, Convert.ToInt64(project.id));
                    if (!Directory.Exists(projectPath))
                    {
                        Directory.CreateDirectory(projectPath);
                        File.WriteAllText(String.Format("{0}{1}{2:D10}_Project.json", projectPath, _osPath, Convert.ToInt64(project.id)), JsonConvert.SerializeObject(project));
                        if (!token.scan_settings)
                        {
                            ScanSettings scanSettings = scans.getScanSettings(token, project.id);
                            CxSettings.Add(Convert.ToInt64(project.id), scanSettings);
                            File.WriteAllText(String.Format("{0}{1}{2:D10}_ScanSettings.json", projectPath, _osPath, Convert.ToInt64(project.id)), JsonConvert.SerializeObject(scanSettings));
                        }
                    }
                    if (token.scan_settings)
                    {
                        ScanSettings refreshSettings = scans.getScanSettings(token, project.id);
                        CxSettings.Add(Convert.ToInt64(project.id), refreshSettings);
                        File.WriteAllText(String.Format("{0}{1}{2:D10}_ScanSettings.json", projectPath, _osPath, Convert.ToInt64(project.id)), JsonConvert.SerializeObject(refreshSettings));
                    }
                }
            }
            if (token.purge_projects)
            {
                deleteProjectFolderifExists(projectName, token);
            }
            return projectObjects;
        }
        public bool deleteProjectFolderifExists(List<string> projectName, resultClass token)
        {
            List<string> directories = new List<string>(Directory.EnumerateDirectories(token.archival_path));
            foreach (string directory in directories)
            {
                string projectPath = String.Format("{0}{1}{2}", token.archival_path, _osPath, directory);
                if (!projectName.Contains(directory))
                {
                    Directory.Delete(projectPath, true);
                }
            }
            return true;
        }
        public string getTeamName(string key)
        {
            return CxTeams[key].fullName;
        }
        public string getPresetName(long key)
        {
            return CxPresets[key].name;
        }

        public void Dispose()
        {

        }
    }
}
