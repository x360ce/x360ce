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
            AllowOnlyOneCopy = true;
            InternetFeatures = true;
            InternetAutoLoad = true;
            InternetAutoSave = true;
        }
        /// <summary>
        /// Avoid deserialization duplicates by using separate method.
        /// </summary>
        public void InitDefaults()
        {
            if (string.IsNullOrEmpty(InternetDatabaseUrl))
                InternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx";
            if (InternetDatabaseUrls == null)
                InternetDatabaseUrls = new List<string>();
            if (InternetDatabaseUrls.Count == 0)
            {
                InternetDatabaseUrls.Add("http://www.x360ce.com/webservices/x360ce.asmx");
                InternetDatabaseUrls.Add("http://localhost:20360/webservices/x360ce.asmx");
            }
            if (GameScanLocations == null)
                GameScanLocations = new List<string>() { };
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
