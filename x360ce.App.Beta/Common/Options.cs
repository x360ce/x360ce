using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
    public class Options : INotifyPropertyChanged
    {
        public Options()
        {
			// Set default values.
            AllowOnlyOneCopy = true;
            InternetFeatures = true;
            InternetAutoLoad = true;
            InternetAutoSave = true;
			ShowDevicesTab = true;
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
            if (string.IsNullOrEmpty(DiskId))
            {
                DiskId = Engine.BoardInfo.GetDiskId();
            }
            if (ComputerId == Guid.Empty)
            {
                ComputerId = Engine.BoardInfo.GetHashedDiskId();
            }
        }

        public bool CheckAndFixUserRsaKeys()
        {
            // If user RSA keys are missing then...
            if (string.IsNullOrEmpty(UserRsaPublicKey))
            {
                // Create new RSA keys which will be used to send encrypted credentials.
                var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.User);
                var keys = rsa.RsaNewKeys(2048);
                UserRsaPublicKey = keys.Public;
                UserRsaPrivateKey = keys.Private;
                return true;
            }
            return false;
        }

		[DefaultValue(false), Description("Allow only one instance of the application to run at a time.")]
		public bool AllowOnlyOneCopy { get { return _AllowOnlyOneCopy; } set { _AllowOnlyOneCopy = value; ReportPropertyChanged(x => x.GetXInputStates); } }
		bool _AllowOnlyOneCopy;

		[DefaultValue(false), Description("Make program Top Window")]
		public bool AlwaysOnTop { get { return _AlwaysOnTop; } set { _AlwaysOnTop = value; ReportPropertyChanged(x => x.AlwaysOnTop); } }
		bool _AlwaysOnTop;


		[DefaultValue(false), Description("Start with Windows.")]
		public bool StartWithWindows { get { return _StartWithWindows; } set { _StartWithWindows = value; ReportPropertyChanged(x => x.StartWithWindows); } }
		bool _StartWithWindows;

		[DefaultValue(FormWindowState.Normal), Description("Windows State when program starts with Windows.")]
		public FormWindowState StartWithWindowsState { get { return _StartWithWindowsState; } set { _StartWithWindowsState = value; ReportPropertyChanged(x => x.StartWithWindowsState); } }
		FormWindowState _StartWithWindowsState;

		public bool ShowProgramsTab { get; set; }
        public bool ShowSettingsTab { get; set; }
        public bool ShowDevicesTab { get; set; }
        public bool ShowIniTab { get; set; }

        public bool InternetFeatures { get; set; }
        public bool InternetAutoLoad { get; set; }
        public bool InternetAutoSave { get; set; }
        public string InternetDatabaseUrl { get; set; }
        public List<string> InternetDatabaseUrls { get; set; }
        public List<string> GameScanLocations { get; set; }
        public string DiskId { get; set; }
        public Guid ComputerId { get; set; }

        public string LoginEnabled { get; set; }

        public bool IncludeProductsInsideINI { get; set; }

        public string UserRsaPublicKey { get; set; }
        public string UserRsaPrivateKey { get; set; }

        public string CloudRsaPublicKey { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool ExcludeSupplementalDevices { get; set; }
        public bool ExcludeVirtualDevices { get; set; }
        public bool CheckForUpdates { get; set; }

        // Performance Test

        public bool TestEnabled { get; set; }
        public bool TestGetDInputStates { get; set; }
        public bool TestSetXInputStates { get; set; }
        public bool TestUpdateInterface { get; set; }

		public bool TestCheckIssues { get; set; }

		public bool ShowDebugPanel { get; set; }

        public bool GetXInputStates
        {
            get { return _GetXInputStates; }
            set { _GetXInputStates = value; ReportPropertyChanged(x => x.GetXInputStates); }
        }
        bool _GetXInputStates;

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public static string GetName(Expression<Func<Options, object>> selector)
        {
            var body = selector.Body as MemberExpression;
            if (body == null)
            {
                var ubody = (UnaryExpression)selector.Body;
                body = ubody.Operand as MemberExpression;
            }
            return body.Member.Name;
        }

        public void ReportPropertyChanged(Expression<Func<Options, object>> selector)
        {
            var ev = PropertyChanged;
            if (ev == null) return;
            var name = GetName(selector);
            ev(this, new PropertyChangedEventArgs(name));
        }

        #endregion


    }
}
