
using System;
using System.Collections.Generic;
using CxAPI_Store.dto;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;

namespace CxAPI_Store
{
    class fetchProjectFiles : IDisposable
    {
        public List<ProjectObject> CxProjects;
        public Dictionary<string, Teams> CxTeams;
        public Dictionary<long, Presets> CxPresets;
        public Dictionary<long, ScanSettings> CxSettings;
        public Dictionary<long, ProjectDetail> CxProjectDetail;
        public Dictionary<long, Dictionary<long, ScanObject>> CxIdxScans;
        public Dictionary<long,Dictionary<long, ScanStatistics>> CxIdxResultStatistics;
        public Dictionary<long,Dictionary<long, string>> CxIdxResults;
        public string _osPath;

        public fetchProjectFiles(resultClass token)
        {
            try
            {
                string _os = RuntimeInformation.OSDescription;
                _osPath = _os.Contains("Windows") ? "\\" : "/";
                CxProjects = new List<ProjectObject>();
                CxSettings = new Dictionary<long, ScanSettings>();
                CxProjectDetail = new Dictionary<long, ProjectDetail>();
                CxTeams = new Dictionary<string, Teams>();
                CxPresets = new Dictionary<long, Presets>();
                CxIdxScans = new Dictionary<long, Dictionary<long, ScanObject>>();
                CxIdxResultStatistics = new Dictionary<long, Dictionary<long, ScanStatistics>>();
                CxIdxResults = new Dictionary<long, Dictionary<long, string>>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool fetchFilteredScans(resultClass token)
        {
            fetchTeamsFromFile(token);
            fetchPresetsFromFile(token);
            fetchFromScanFiles(token);
            if (CxIdxScans.Count == 0)
            {
                Console.Error.WriteLine("No scans were found, please check arguments and retry.");
                return false;
            }
            return true;
        }

        public bool fetchTeamsFromFile(resultClass token)
        {
            string teamPath = String.Format("{0}{1}000000000_Teams.json", token.archival_path, _osPath);
            List<Teams> teams = JsonConvert.DeserializeObject<List<Teams>>(File.ReadAllText(teamPath));
            foreach (Teams team in teams)
            {
                CxTeams.Add(team.id, team);
            }
            return true;
        }
        public bool fetchPresetsFromFile(resultClass token)
        {
            string presetPath = String.Format("{0}{1}000000000_Presets.json", token.archival_path, _osPath);
            List<Presets> presets = JsonConvert.DeserializeObject<List<Presets>>(File.ReadAllText(presetPath));
            foreach (Presets preset in presets)
            {
                CxPresets.Add(preset.id, preset);
            }
            return true;
        }
        public bool fetch_projects(resultClass token)
        {
            List<string> directories = new List<string>(Directory.EnumerateDirectories(token.archival_path));
            foreach (string directory in directories)
            {
                string[] fsplits = directory.Split(_osPath);
                string fileName = fsplits[fsplits.Length - 1];
                string projectJson = String.Format("{0}{1}{2}_Project.json", directory, _osPath, fileName);
                string settingsJson = String.Format("{0}{1}{2}_ScanSettings.json", directory, _osPath, fileName);
                string projectDetailJson = String.Format("{0}{1}{2}_ProjectDetail.json", directory, _osPath, fileName);
                ProjectObject projectObject = JsonConvert.DeserializeObject<ProjectObject>(File.ReadAllText(projectJson));
                ScanSettings scanSettings = JsonConvert.DeserializeObject<ScanSettings>(File.ReadAllText(settingsJson));
                ProjectDetail projectDetail = JsonConvert.DeserializeObject<ProjectDetail>(File.ReadAllText(settingsJson));
                if (filterProjectSettings(token, projectObject, scanSettings))
                {
                    CxProjects.Add(projectObject);
                    CxSettings.Add(Convert.ToInt64(projectObject.id), scanSettings);
                }
            }
            return true;
        }

        public bool fetchScanFiles(resultClass token, ProjectObject project)
        {
            CxIdxScans.Add(Convert.ToInt64(project.id), new Dictionary<long, ScanObject>());
            CxIdxResultStatistics.Add(Convert.ToInt64(project.id), new Dictionary<long, ScanStatistics>());
            CxIdxResults.Add(Convert.ToInt64(project.id), new Dictionary<long, string>());

            string scanDir = String.Format("{0}{1}{2:D10}", token.archival_path, _osPath, Convert.ToInt64(project.id));
            List<string> fileList = new List<string>();
            List<string> directories = new List<string>(Directory.EnumerateDirectories(scanDir));
            foreach (string directory in directories)
            {
                List<string> files = new List<string>(Directory.EnumerateFiles(directory));
                foreach (string file in files)
                {
                    string[] fsplits = file.Split(_osPath);
                    string fileName = fsplits[fsplits.Length - 1];
                    if (file.Contains("Scan_"))
                    {
                        fileList.Add(fileName.Replace("Scan_",""));
                    }
                }
            }
            List<string> sorted = fileList.OrderByDescending(i => i).ToList();
            int fileCount = token.max_scans == 0 ? sorted.Count : token.max_scans;
            fileCount = fileCount > sorted.Count ? sorted.Count : fileCount;
            for (int count = 0; count < fileCount; count++)
            {
                string fileName = sorted[count];
                string[] fsplits = fileName.Split("_");
                string scanId = fsplits[0];

                string scanPath = String.Format("{0}{1}{2}", scanDir, _osPath, scanId);
                List<string> scanFiles = new List<string>(Directory.EnumerateFiles(scanPath));
                string uniqueKey = String.Format("{0:D10}{1:D10}", Convert.ToInt64(project.id), Convert.ToInt64(scanId));
                
                foreach (string scanFile in scanFiles)
                {
                    if (scanFile.Contains("Scan_"))
                    {
                        CxIdxScans[Convert.ToInt64(project.id)].Add(Convert.ToInt64(scanId), JsonConvert.DeserializeObject<ScanObject>(File.ReadAllText(scanFile)));
                    }
                    if (scanFile.Contains("ScanStatistics_"))
                    {
                        CxIdxResultStatistics[Convert.ToInt64(project.id)].Add(Convert.ToInt64(scanId), JsonConvert.DeserializeObject<ScanStatistics>(File.ReadAllText(scanFile)));
                    }
                    if (scanFile.Contains("Results_"))
                    {
                        CxIdxResults[Convert.ToInt64(project.id)].Add(Convert.ToInt64(scanId), File.ReadAllText(scanFile));
                    }
                }
            }

            return true;
        }

        public bool filterProjectSettings(resultClass token, ProjectObject project, ScanSettings settings)
        {
            return testProject(token, project) && testTeam(token, project) && testPreset(token, project, settings);
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
        public bool testPreset(resultClass token, ProjectObject project, ScanSettings scanSettings)
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
                        if (scanSettings.preset.id == preset.id)
                            presetFlag = true;
                        break;
                    }
                }
            }
            return presetFlag;
        }

        public object getTeamAndPresetNames(string teamKey, long presetKey)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result.Add("TeamName", getTeamName(teamKey));
            result.Add("PresetName", getPresetName(presetKey));
            return Flatten.CreateFlattenObject(result);
        }
        private object getTeamName(string key)
        {
            return CxTeams[key].fullName;
        }
        private object getPresetName(long key)
        {
            return CxPresets[key].name;
        }
        public void writeDictionary(resultClass token, Dictionary<string,object> dict, string fileName="dump.txt")
        {
            string dictText = String.Empty;
            foreach(string key in dict.Keys)
            {
                dictText += String.Format("{0} > {1}\n", key, dict[key] != null ? dict[key].ToString() : String.Empty);
            }
            File.WriteAllText(token.file_path + "\\" + fileName, dictText);
        }

        private bool fetchFromScanFiles(resultClass token)
        {
            fetch_projects(token);
            foreach (ProjectObject project in CxProjects)
            {
                fetchScanFiles(token, project);
            }

            return true;
        }
 
        public void Dispose()
        {

        }
    }
}
