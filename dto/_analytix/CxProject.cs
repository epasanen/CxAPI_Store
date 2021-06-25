using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store.dto
{
    public partial class CxProjectJson
    {
        public Dictionary<string, object> CustomFields { get; set; }
        public string Policies { get; set; }
        public string Preset { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime SAST_LastScanDate { get; set; }
        public long SAST_Scans { get; set; }
        public string TeamName { get; set; }

        public CxProject convertObject()
        {
            CxProject cx = new CxProject()
            {
                TeamName = this.TeamName,
                SAST_Scans = this.SAST_Scans,
                SAST_LastScanDate = this.SAST_LastScanDate,
                ProjectName = this.ProjectName,
                ProjectId = this.ProjectId,
                Preset = this.Preset,
                CustomFields = flattenDictionary(this.CustomFields),
                Policies = this.Policies
            };
            return cx;
        }
        public string flattenDictionary(Dictionary<string, object> dict)
        {
            string result = String.Empty;
            if (dict != null)
            {
                result = "{";
                foreach (string key in dict.Keys)
                {
                    Int64 test;
                    if (Int64.TryParse(dict[key].ToString(), out test))
                    {
                        result += String.Format("\"{0}\":{1},", key, dict[key]);
                    }
                    else
                    {
                        result += String.Format("\"{0}\":\"{1}\",", key, dict[key]);
                    }
                }
                result = result.TrimEnd(',');
                result += "}";
            }
            return result;
        }

    }
    public partial class CxProject
    {
        public string CustomFields { get; set; }
        public string Policies { get; set; }
        public string Preset { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime SAST_LastScanDate { get; set; }
        public long SAST_Scans { get; set; }
        public string TeamName { get; set; }

    }

}
