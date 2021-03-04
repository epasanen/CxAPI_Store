
using System.Collections.Generic;

namespace CxAPI_Store.dto
{



    public class Presets
    {
        public long id { get; set; }
        public object link { get; set; }
        public string name { get; set; }
        public string ownerName { get; set; }
        public string queryIds { get; set; }

    }

    public class SourceSettingsLink
    {
        public string type { get; set; }
        public string rel { get; set; }
        public object uri { get; set; }
    }

    public class ProjectLink
    {
        public string rel { get; set; }
        public string uri { get; set; }
    }

    public class ProjectObject
    {
        public class SourceSettingsLink
        {
            public string type { get; set; }
            public string rel { get; set; }
            public string uri { get; set; }
        }
        public class ProjectLink
        {
            public string rel { get; set; }
            public string uri { get; set; }
        }
        public string id { get; set; }
        public string teamId { get; set; }
        public string name { get; set; }
        public string isPublic { get; set; }
        public SourceSettingsLink sourceSettingsLink { get; set; }
        public Link link { get; set; }
    }


    public class ProjectDetail
    {
        public class Link
        {
            public string rel { get; set; }
            public string uri { get; set; }
            public string type { get; set; }
        }
        public class customField
        {
            public string id { get; set; }
            public string value { get; set; }
            public string name { get; set; }

        }
        public int id { get; set; }
        public int teamId { get; set; }
        public string name { get; set; }
        public bool isPublic { get; set; }
        public customField[]  customFields { get; set; }
        public Link[] links { get; set; }
    }

}

