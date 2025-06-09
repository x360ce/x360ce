using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
			MinimizeToTray = true;
            MinimizeOnClose = true;
            StartWithWindowsState = FormWindowState.Minimized;
			InternetFeatures = true;
			InternetAutoLoad = true;
			InternetAutoSave = true;
			ShowDevicesTab = true;
			EnableShowFormInfo = false;
		}
		/// <summary>
		/// Avoid de-serialization duplicates by using separate method.
		/// </summary>
		public void InitializeDefaults()
		{
			if (string.IsNullOrEmpty(InternetDatabaseUrl))
				InternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx";
			if (InternetDatabaseUrls == null)
				InternetDatabaseUrls = new BindingList<string>();
			if (InternetDatabaseUrls.Count == 0)
			{
				InternetDatabaseUrls.Add("http://www.x360ce.com/webservices/x360ce.asmx");
				InternetDatabaseUrls.Add("http://localhost:20360/webservices/x360ce.asmx");
			}
			if (GameScanLocations == null)
				GameScanLocations = new BindingList<string>();
			if (string.IsNullOrEmpty(ComputerDisk))
				ComputerDisk = Engine.BoardInfo.GetDiskId();
			if (ComputerId == Guid.Empty)
				ComputerId = Engine.BoardInfo.GetHashedDiskId(ComputerDisk);
			if (string.IsNullOrEmpty(ProfilePath))
				ProfilePath = EngineHelper.AppDataPath;
			if (ProfileId == Guid.Empty)
				ProfileId = Engine.Data.UserProfile.GenerateProfileId(ComputerId, EngineHelper.AppDataPath);
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
		public string AppVersion { get; set; }

		public string SettingsVersion { get; set; } = "4";

		[DefaultValue(false), Description("Throw or suspend errors.")]
		public bool DebugMode { get { return _DebugMode; } set { _DebugMode = value; OnPropertyChanged(); } }
		bool _DebugMode;


		[DefaultValue(false), Description("Allow only one instance of the application to run at a time.")]
		public bool AllowOnlyOneCopy { get { return _AllowOnlyOneCopy; } set { _AllowOnlyOneCopy = value; OnPropertyChanged(); } }
		bool _AllowOnlyOneCopy;



		[DefaultValue(false), Description("Make program Top Window")]
		public bool AlwaysOnTop { get { return _AlwaysOnTop; } set { _AlwaysOnTop = value; OnPropertyChanged(); } }
		bool _AlwaysOnTop;

		[DefaultValue(false), Description("StartDInputService with Windows.")]
		public bool StartWithWindows { get { return _StartWithWindows; } set { _StartWithWindows = value; OnPropertyChanged(); } }
		bool _StartWithWindows;

		[DefaultValue(FormWindowState.Minimized), Description("Windows State when program starts with Windows.")]
		public FormWindowState StartWithWindowsState { get { return _StartWithWindowsState; } set { _StartWithWindowsState = value; OnPropertyChanged(); } }
		FormWindowState _StartWithWindowsState;

		public bool ShowProgramsTab { get; set; }
		public bool ShowSettingsTab { get; set; }
		public bool ShowDevicesTab { get; set; }
		public bool ShowIniTab { get; set; }

		[DefaultValue(true), Description("Enable the use of Internet features like the settings database.")]
		public bool InternetFeatures { get { return _InternetFeatures; } set { _InternetFeatures = value; OnPropertyChanged(); } }
		bool _InternetFeatures;

		[DefaultValue(true), Description("Auto load settings from Internet Database.")]
		public bool InternetAutoLoad { get { return _InternetAutoLoad; } set { _InternetAutoLoad = value; OnPropertyChanged(); } }
		bool _InternetAutoLoad;

		[DefaultValue(true), Description("Auto save settings to Internet Database.")]
		public bool InternetAutoSave { get { return _InternetAutoSave; } set { _InternetAutoSave = value; OnPropertyChanged(); } }
		bool _InternetAutoSave;

		public const string DefaultInternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx";

		[DefaultValue(DefaultInternetDatabaseUrl), Description("Internet settings database URL.")]
		public string InternetDatabaseUrl
		{
			get { return _InternetDatabaseUrl; }
			set
			{
				// If server changed then...
				if (_InternetDatabaseUrl != value)
					// Reset Cloud RSA Public encryption key.
					// Key will be retrieved automatically next time.
					CloudRsaPublicKey = null;
				_InternetDatabaseUrl = value;

				OnPropertyChanged();
			}
		}
		string _InternetDatabaseUrl;

		[DefaultValue(UpdateFrequency.ms1_1000Hz), Description("Virtual Controller update frequency.")]
		public UpdateFrequency PollingRate
		{
			get { return _PollingRate; }
			set { _PollingRate = value; OnPropertyChanged(); }
		}
		UpdateFrequency _PollingRate = UpdateFrequency.ms1_1000Hz;


		[Description("Device Use Buffered Data: false - device.GetCurrentState(), 1 - device.GetBufferedData().")]
		public bool UseDeviceBufferedData
		{
			get { return _UseDeviceBufferedData; }
			set { _UseDeviceBufferedData = value; OnPropertyChanged(); }
		}
		bool _UseDeviceBufferedData;


		[DefaultValue(false), Description("Configure Hid Guardian Automatically.")]
		public bool HidGuardianConfigureAutomatically
		{
			get { return _HidGuardianConfigureAutomatically; }
			set { _HidGuardianConfigureAutomatically = value; OnPropertyChanged(); }
		}
		bool _HidGuardianConfigureAutomatically = false;

		[DefaultValue("{7}")]
		public string GuideButtonAction
		{
			get { return _GuideButtonAction; }
			set { _GuideButtonAction = value; OnPropertyChanged(); }
		}
		string _GuideButtonAction = "{7}";

		public BindingList<string> InternetDatabaseUrls { get; set; }

		[DefaultValue(null), Description("The locations to scan for games.")]
		public BindingList<string> GameScanLocations { get; set; }
		public string ComputerDisk { get; set; }
		public Guid ComputerId { get; set; }

		public string ProfilePath { get; set; }

		public Guid ProfileId { get; set; }

		public string LoginEnabled { get; set; }

		public bool IncludeProductsInsideINI { get; set; }

		public string UserRsaPublicKey { get; set; }
		public string UserRsaPrivateKey { get; set; }

		public string CloudRsaPublicKey { get; set; }

		public string Username { get; set; }
		public string Password { get; set; }

		[DefaultValue(true)]
		public bool MinimizeToTray { get; set; }

        public bool MinimizeOnClose { get; set; }
        public bool ExcludeSupplementalDevices { get; set; }

		#region ■ Direct Input

		public bool ExcludeVirtualDevices { get; set; }

		[DefaultValue(true)]
		public bool AcquireHiddenDevicesInExclusiveMode { get; set; } = true;

		[DefaultValue(true)]
		public bool AcquireMappedDevicesInExclusiveMode { get; set; } = true;

		#endregion

		[DefaultValue(true), Description("Auto-detect currently focused game.")]
		public bool AutoDetectForegroundWindow
		{
			get { return _AutoDetectForegroundWindow; }
			set { _AutoDetectForegroundWindow = value; OnPropertyChanged(); }
		}
		bool _AutoDetectForegroundWindow = true;


		[DefaultValue(true), Description("Make application process DPI aware.")]
		public bool IsProcessDPIAware
		{
			get { return _IsProcessDPIAware; }
			set { _IsProcessDPIAware = value; OnPropertyChanged(); }
		}
		bool _IsProcessDPIAware = true;


		[DefaultValue(false), Description("Check for updates.")]
		public bool CheckForUpdates
		{
			get { return _CheckForUpdates; }
			set { _CheckForUpdates = value; OnPropertyChanged(); }
		}
		bool _CheckForUpdates;

		// Remote Control

		public MapToMask RemoteControllers { get; set; }
		public string RemotePassword { get; set; }
		public int RemotePort { get; set; }
		public bool RemoteEnabled { get { return _RemoteEnabled; } set { _RemoteEnabled = value; OnPropertyChanged(); } }
		bool _RemoteEnabled;

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
			set { _GetXInputStates = value; OnPropertyChanged(); }
		}
		bool _GetXInputStates;

		#region ■ Get Programs 

		[DefaultValue(2)]
		public int GetProgramsMinInstances { get; set; } = 2;

		[DefaultValue(EnabledState.Enabled)]
		public EnabledState GetProgramsIncludeEnabled { get; set; } = EnabledState.Enabled;

		#endregion

		#region ■ Options: Developing

		[DefaultValue(false), Description("Enable Form Info (CTRL+SHIFT+RMB)")]
		public bool EnableShowFormInfo { get { return _EnableShowFormInfo; } set { _EnableShowFormInfo = value; OnPropertyChanged(); } }
		bool _EnableShowFormInfo;


		[DefaultValue(false), Description("Show [Test...] Button.")]
		public bool ShowTestButton { get { return _ShowTestButton; } set { _ShowTestButton = value; OnPropertyChanged(); } }
		bool _ShowTestButton;

		#endregion

		#region ■ INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion


	}
}
