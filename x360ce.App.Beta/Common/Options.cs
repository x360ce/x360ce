using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App
{
    public class Options
    {
        public Options()
        {
            InternetDatabaseUrls = new List<string>()
            {
                "http://www.x360ce.com/webservices/x360ce.asmx",
                "http://localhost:20360/webservices/x360ce.asmx"
            };
            GameScanLocations = new List<string>()
            {
            };
            InternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx";
        }
        public bool AllowOnlyOneCopy { get; set; }
        public bool InternetFeatures { get; set; }
        public bool InternetAutoLoad { get; set; }
        public bool InternetAutoSave { get; set; }
        public string InternetDatabaseUrl { get; set; }
        public List<string> InternetDatabaseUrls { get; set; }
        public List<string> GameScanLocations { get; set; }

    }
}
