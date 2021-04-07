using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store.dto
{
    public partial class CxProjectJson
    {
        public Dictionary<string, object> CustomFields { get; set; }
        public Dictionary<string, object> Policies { get; set; }
        public string Preset { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime SastLastScanDate { get; set; }
        public int SastScans { get; set; }
        public string TeamName { get; set; }

        public CxProject convertObject()
        {
            CxProject cx = new CxProject()
            {
                TeamName = this.TeamName,
                SastScans = this.SastScans,
                SastLastScanDate = this.SastLastScanDate,
                ProjectName = this.ProjectName,
                ProjectId = this.ProjectId,
                Preset = this.Preset,
                CustomFields = flattenDictionary(this.CustomFields),
                Policies = flattenDictionary(this.Policies)

            };
            return cx;
        }
        public string flattenDictionary(Dictionary<string, object> dict)
        {
            string result = String.Empty;
            if (dict != null)
            {
                foreach (string key in dict.Keys)
                {
                    result += String.Format("{{{0}:{1}}}", key, dict[key]);
                }
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
        public DateTime SastLastScanDate { get; set; }
        public int SastScans { get; set; }
        public string TeamName { get; set; }

    }

}
