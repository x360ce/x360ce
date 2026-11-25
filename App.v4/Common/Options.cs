using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
			InternetFeatures = true;
			InternetAutoLoad = true;
			InternetAutoSave = true;
			ShowDevicesTab = true;
			EnableShowFormInfo = false;
		}
		/// <summary>
		/// Avoid deserialization duplicates by using separate method.
		/// </summary>
		public void InitDefaults()
		{
			InternetDatabaseUrl = string.IsNullOrEmpty(InternetDatabaseUrl)
				? DefaultInternetDatabaseUrl
				: SettingName.CurrentAddress(InternetDatabaseUrl);
			if (InternetDatabaseUrls == null)
				InternetDatabaseUrls = new BindingList<string>();
			if (InternetDatabaseUrls.Count == 0)
			{
				InternetDatabaseUrls.Add(DefaultInternetDatabaseUrl);
				InternetDatabaseUrls.Add(SettingName.LocalInternetDatabaseUrl);
			}
			var current = InternetDatabaseUrls.Select(SettingName.CurrentAddress).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (!current.SequenceEqual(InternetDatabaseUrls))
			{
				InternetDatabaseUrls.Clear();
				foreach (var url in current)
					InternetDatabaseUrls.Add(url);
			}
			// The address box shows only what is in its list. A saved address missing from the list
			// left the box showing the first entry, and the box's first change wrote that entry back
			// over the saved one.
			if (!InternetDatabaseUrls.Contains(InternetDatabaseUrl))
				InternetDatabaseUrls.Add(InternetDatabaseUrl);
			if (string.IsNullOrEmpty(SdlDatabaseUrl))
				SdlDatabaseUrl = DefaultSdlDatabaseUrl;
			if (SdlDatabaseUrls == null)
				SdlDatabaseUrls = new BindingList<string>();
			if (SdlDatabaseUrls.Count == 0)
				SdlDatabaseUrls.Add(DefaultSdlDatabaseUrl);
			// As for the web service address: the box shows only what is in its list.
			if (!SdlDatabaseUrls.Contains(SdlDatabaseUrl))
				SdlDatabaseUrls.Add(SdlDatabaseUrl);
			if (GameScanLocations == null)
				GameScanLocations = new BindingList<string>() { };
			if (DisabledPlugins == null)
				DisabledPlugins = new BindingList<string>();
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

		[DefaultValue(false), Description("Allow only one instance of the application to run at a time.")]
		public bool AllowOnlyOneCopy { get { return _AllowOnlyOneCopy; } set { _AllowOnlyOneCopy = value; OnPropertyChanged(); } }
		bool _AllowOnlyOneCopy;

		[DefaultValue(false), Description("Make program Top Window")]
		public bool AlwaysOnTop { get { return _AlwaysOnTop; } set { _AlwaysOnTop = value; OnPropertyChanged(); } }
		bool _AlwaysOnTop;

		[DefaultValue(ThemeType.Auto), Description("Light or dark colours. System follows the Windows setting for apps.")]
		public ThemeType Theme { get { return _Theme; } set { _Theme = value; OnPropertyChanged(); } }
		ThemeType _Theme;

		/// <summary>The font the windows are drawn in, as <see cref="AppFonts.InterfaceFamilyFor"/> reads it.</summary>
		[DefaultValue(""), Description("The font the windows are drawn in: \"" + AppFonts.DefaultName + "\", Windows’ own, when empty; Selawik, built in, for \"" + AppFonts.BuiltInName + "\"; otherwise an installed font’s name. Takes effect when x360ce starts again.")]
		public string InterfaceFont { get { return _InterfaceFont; } set { _InterfaceFont = value; OnPropertyChanged(); } }
		string _InterfaceFont;

		/// <summary>The size the windows' text is drawn at, in points, as <see cref="AppFonts.SizeFor"/> reads it.</summary>
		[DefaultValue(0f), Description("The size the windows’ text is drawn at, in points, whatever the font. When 0: Windows’ text size. Takes effect when x360ce starts again.")]
		public float InterfaceFontSize { get { return _InterfaceFontSize; } set { _InterfaceFontSize = value; OnPropertyChanged(); } }
		float _InterfaceFontSize;

		public bool ShowProgramsTab { get; set; }
		public bool ShowSettingsTab { get; set; }
		public bool ShowDevicesTab { get; set; }
		public bool ShowIniTab { get; set; }

		[DefaultValue(true), Description("Enable the use of Internet features like the settings database.")]
		public bool InternetFeatures { get { return _InternetFeatures; } set { _InternetFeatures = value; OnPropertyChanged(); } }
		bool _InternetFeatures;

		[DefaultValue(true), Description("Meant to load settings for a device from the Internet Database as soon as it is added. Nothing does that yet: use Download From Cloud on the Cloud page.")]
		public bool InternetAutoLoad { get { return _InternetAutoLoad; } set { _InternetAutoLoad = value; OnPropertyChanged(); } }
		bool _InternetAutoLoad;

		[DefaultValue(true), Description("Auto save settings to Internet Database.")]
		public bool InternetAutoSave { get { return _InternetAutoSave; } set { _InternetAutoSave = value; OnPropertyChanged(); } }
		bool _InternetAutoSave;

		public const string DefaultInternetDatabaseUrl = SettingName.DefaultInternetDatabaseUrl;

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

		[DefaultValue(true), Description("Update the interface. Turn off to see what the engine reaches with nothing drawing.")]
		/// <summary>Whether the window redraws itself.</summary>
		/// <remarks>
		/// Deliberately not stored. It exists to be turned off for a moment, to see what the
		/// engine reaches with nothing drawing. Kept across restarts, somebody who turned it
		/// off and forgot would meet a window that never updates again and no longer
		/// remembers why - so it comes back on every time the program starts.
		/// </remarks>
		[System.Xml.Serialization.XmlIgnore]
		public bool UpdateInterface
		{
			get { return _UpdateInterface; }
			set { _UpdateInterface = value; OnPropertyChanged(); }
		}
		bool _UpdateInterface = true;

		/// <summary>Whether the Issues page runs its checks. Off, no check runs and the page says so.</summary>
		[DefaultValue(true), Description("Run the checks of the Issues page.")]
		public bool IssueChecksEnabled
		{
			get { return _IssueChecksEnabled; }
			set { _IssueChecksEnabled = value; OnPropertyChanged(); }
		}
		bool _IssueChecksEnabled = true;


		/// <summary>True: a press or release that comes and goes between two engine passes reaches the controller, for one pass, on every device. False: each pass reads only where each control is then.</summary>
		[DefaultValue(true), Description("Read every change a device reports, not only its state at each pass.")]
		public bool ReadEveryChange
		{
			get { return _ReadEveryChange; }
			set { _ReadEveryChange = value; OnPropertyChanged(); }
		}
		bool _ReadEveryChange = true;

		/// <summary>True: plugins from anyone load. False: only the plugins embedded in this program and those signed by its publisher.</summary>
		/// <remarks>A plugin runs inside this program with all its rights, so loading one from anyone else is the person's choice.</remarks>
		[DefaultValue(false), Description("Load plugins that are not signed by the publisher of x360ce.")]
		public bool AllowThirdPartyPlugins
		{
			get { return _AllowThirdPartyPlugins; }
			set { _AllowThirdPartyPlugins = value; OnPropertyChanged(); }
		}
		bool _AllowThirdPartyPlugins;

		/// <summary>Names of plugins switched off on the Options tab's Plugins page. Empty by default, so built-in plugins load automatically.</summary>
		[Description("Names of plugins switched off on the Options tab’s Plugins page.")]
		public BindingList<string> DisabledPlugins { get; set; }

		/// <summary>Names of plugins off by default that were switched on on the Options tab's Plugins page.</summary>
		[Description("Names of plugins off by default that were switched on on the Options tab’s Plugins page.")]
		public BindingList<string> EnabledPlugins { get; set; }

		/// <summary>True unless the plugin was switched off on the Options tab's Plugins page. Built-in plugins are enabled by default, except those off by default until switched on.</summary>
		public bool IsPluginEnabled(string name)
		{
			if (string.IsNullOrEmpty(name))
				return true;
			if (DisabledPlugins != null && DisabledPlugins.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
				return false;
			return !Plugins.PluginManager.IsOffByDefault(name)
				|| (EnabledPlugins != null && EnabledPlugins.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)));
		}

		/// <summary>Records whether a plugin is enabled or disabled on the Options tab's Plugins page.</summary>
		public void SetPluginEnabled(string name, bool enabled)
		{
			if (string.IsNullOrEmpty(name))
				return;
			// A plugin off by default is kept as switched on, so it loads on the next start too.
			if (EnabledPlugins == null)
				EnabledPlugins = new BindingList<string>();
			var switchedOn = EnabledPlugins.Where(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)).ToArray();
			if (enabled && switchedOn.Length == 0 && Plugins.PluginManager.IsOffByDefault(name))
			{
				EnabledPlugins.Add(name);
				OnPropertyChanged(nameof(EnabledPlugins));
			}
			else if (!enabled && switchedOn.Length > 0)
			{
				foreach (var item in switchedOn)
					EnabledPlugins.Remove(item);
				OnPropertyChanged(nameof(EnabledPlugins));
			}
			if (DisabledPlugins == null)
				DisabledPlugins = new BindingList<string>();
			var existing = DisabledPlugins.Where(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)).ToArray();
			if (enabled)
			{
				if (existing.Length == 0)
					return;
				foreach (var item in existing)
					DisabledPlugins.Remove(item);
				OnPropertyChanged(nameof(DisabledPlugins));
			}
			else
			{
				if (existing.Length > 0)
					return;
				DisabledPlugins.Add(name);
				OnPropertyChanged(nameof(DisabledPlugins));
			}
		}

		/// <summary>True: an AutoHotkey script that includes x360ce.ahk reads the devices and changes what their controllers read. False: no script can reach the engine.</summary>
		/// <remarks>Off by default: a script changes what games receive, so letting one do that is the person's choice.</remarks>
		[DefaultValue(false), Description("Let AutoHotkey scripts read devices and change what controllers read.")]
		public bool AllowAutoHotkeyScripts
		{
			get { return _AllowAutoHotkeyScripts; }
			set { _AllowAutoHotkeyScripts = value; OnPropertyChanged(); }
		}
		bool _AllowAutoHotkeyScripts;

		/// <summary>How often an AutoHotkey script tells x360ce it still runs, in milliseconds.</summary>
		/// <remarks>AutoHotkey's timers step every 15.6 ms, so a shorter time beats no faster.</remarks>
		[DefaultValue(ScriptChannel.DefaultHeartbeatMilliseconds), Description("How often an AutoHotkey script tells x360ce it still runs, in milliseconds.")]
		public int AutoHotkeyHeartbeat
		{
			get { return _AutoHotkeyHeartbeat; }
			set { _AutoHotkeyHeartbeat = value; OnPropertyChanged(); }
		}
		int _AutoHotkeyHeartbeat = ScriptChannel.DefaultHeartbeatMilliseconds;

		/// <summary>How long x360ce waits for a heartbeat before it lets go of everything a hung AutoHotkey script held, in milliseconds.</summary>
		/// <remarks>A script that closes or crashes is let go of at once; this is for one that stops responding.</remarks>
		[DefaultValue(ScriptChannel.DefaultLeaseMilliseconds), Description("How long x360ce waits before it lets go of what a hung AutoHotkey script held, in milliseconds.")]
		public int AutoHotkeyHangTimeout
		{
			get { return _AutoHotkeyHangTimeout; }
			set { _AutoHotkeyHangTimeout = value; OnPropertyChanged(); }
		}
		int _AutoHotkeyHangTimeout = ScriptChannel.DefaultLeaseMilliseconds;

		#region Wheel telemetry and steering feel

		/// <summary>True: read live vehicle telemetry over Shared Memory and x360ce's own port (37150) while a wheel centering spring is active.</summary>
		[DefaultValue(true), Description("Automatically read live game telemetry over Shared Memory and x360ce’s port (37150) for the wheel centering spring.")]
		public bool WheelTelemetryEnabled
		{
			get { return _WheelTelemetryEnabled; }
			set { _WheelTelemetryEnabled = value; OnPropertyChanged(); }
		}
		bool _WheelTelemetryEnabled = true;

		/// <summary>x360ce's dedicated conflict-free loopback UDP port for game telemetry (37ACD plugin range).</summary>
		[DefaultValue(37150), Description("Loopback UDP port x360ce listens on for standard game telemetry packets (default 37150).")]
		public int WheelTelemetryPort
		{
			get { return _WheelTelemetryPort; }
			set { _WheelTelemetryPort = value; OnPropertyChanged(); }
		}
		int _WheelTelemetryPort = 37150;

		/// <summary>True: also bind standard game UDP ports (5300, 20777, 4123, 5606). Off by default so SimHub, CrewChief, Pit House and other telemetry apps keep their ports.</summary>
		[DefaultValue(false), Description("Also listen on standard game UDP ports (5300, 20777, 4123, 5606). Off by default so other telemetry apps are never blocked.")]
		public bool WheelTelemetryStandardPorts
		{
			get { return _WheelTelemetryStandardPorts; }
			set { _WheelTelemetryStandardPorts = value; OnPropertyChanged(); }
		}
		bool _WheelTelemetryStandardPorts;

		/// <summary>True: relay received UDP telemetry datagrams to <see cref="WheelTelemetryForwardIp"/>:<see cref="WheelTelemetryForwardPort"/> (or <see cref="WheelTelemetryForwardTargets"/>).</summary>
		[DefaultValue(false), Description("Forward received UDP telemetry packets to another local app (such as SimHub) or another PC/tablet on the LAN.")]
		public bool WheelTelemetryForwardEnabled
		{
			get { return _WheelTelemetryForwardEnabled; }
			set { _WheelTelemetryForwardEnabled = value; OnPropertyChanged(); }
		}
		bool _WheelTelemetryForwardEnabled;

		/// <summary>Destination IP address to relay received UDP telemetry datagrams to (default 127.0.0.1).</summary>
		[DefaultValue("127.0.0.1"), Description("Destination IP address to forward received UDP telemetry packets to (default 127.0.0.1).")]
		public string WheelTelemetryForwardIp
		{
			get { return _WheelTelemetryForwardIp; }
			set { _WheelTelemetryForwardIp = string.IsNullOrWhiteSpace(value) ? "127.0.0.1" : value.Trim(); OnPropertyChanged(); }
		}
		string _WheelTelemetryForwardIp = "127.0.0.1";

		/// <summary>Optional UDP port to relay received telemetry datagrams to (for example, SimHub), or 0 for none.</summary>
		[DefaultValue(0), Description("Optional UDP port to forward received telemetry packets to (0 = off).")]
		public int WheelTelemetryForwardPort
		{
			get { return _WheelTelemetryForwardPort; }
			set { _WheelTelemetryForwardPort = value; OnPropertyChanged(); }
		}
		int _WheelTelemetryForwardPort;

		/// <summary>Optional comma-separated UDP forwarding targets (port or IP:port, e.g. "127.0.0.1:20778, 192.168.1.50:20777") to relay telemetry to multiple apps or LAN dashboards.</summary>
		[DefaultValue(""), Description("Optional comma-separated ports or IP:port targets to forward received telemetry packets to (for example, 127.0.0.1:20778, 192.168.1.50:20777).")]
		public string WheelTelemetryForwardTargets
		{
			get { return _WheelTelemetryForwardTargets; }
			set { _WheelTelemetryForwardTargets = value ?? ""; OnPropertyChanged(); }
		}
		string _WheelTelemetryForwardTargets = "";

		/// <summary>True: enforce the selected Wheel range with a software bump stop and auto-scale steering on wheels without hardware range control.</summary>
		[DefaultValue(false), Description("Enforce the selected Wheel range with a software bump stop and scale the steering axis on wheels x360ce cannot set the hardware range of.")]
		public bool WheelSoftLockEnabled
		{
			get { return _WheelSoftLockEnabled; }
			set { _WheelSoftLockEnabled = value; OnPropertyChanged(); }
		}
		bool _WheelSoftLockEnabled;

		/// <summary>True: drive supported steering wheels' RGB/RPM shift LED strip (Logitech G27, G29, G923) from live game telemetry RPM.</summary>
		[DefaultValue(true), Description("Drive supported steering wheels’ RGB/RPM shift LED strip from live game telemetry RPM.")]
		public bool WheelRgbStripEnabled
		{
			get { return _WheelRgbStripEnabled; }
			set { _WheelRgbStripEnabled = value; OnPropertyChanged(); }
		}
		bool _WheelRgbStripEnabled = true;

		/// <summary>How much sustained game rumble lightens the centering spring while turning when no live telemetry is active (0 to 60 %).</summary>
		[DefaultValue(0), Description("How much sustained game rumble lightens the centering spring while turning when no live telemetry is active (0 to 60 %).")]
		public int WheelRumbleUndersteer
		{
			get { return _WheelRumbleUndersteer; }
			set { _WheelRumbleUndersteer = value; OnPropertyChanged(); }
		}
		int _WheelRumbleUndersteer;

		/// <summary>Optional steering velocity feed-forward in milliseconds (0 to 80 ms) to counteract gamepad steering filter lag.</summary>
		[DefaultValue(0), Description("Optional steering velocity lead compensation in milliseconds (0 to 80) to counteract gamepad steering smoothing lag.")]
		public int WheelSteeringLeadMs
		{
			get { return _WheelSteeringLeadMs; }
			set { _WheelSteeringLeadMs = value; OnPropertyChanged(); }
		}
		int _WheelSteeringLeadMs;

		/// <summary>Aligning torque / counter-steer scale in percent (0 to 200, default 100).</summary>
		[DefaultValue(100), Description("Aligning torque and counter-steer pull strength from live game telemetry (0 to 200 %).")]
		public int WheelTelemetryAlignStrength
		{
			get { return _WheelTelemetryAlignStrength; }
			set { _WheelTelemetryAlignStrength = value; OnPropertyChanged(); }
		}
		int _WheelTelemetryAlignStrength = 100;

		/// <summary>Front-tyre understeer lightening scale in percent (0 to 100, default 100).</summary>
		[DefaultValue(100), Description("How strongly front-tyre grip loss lightens the wheel when understeering (0 to 100 %).")]
		public int WheelTelemetryUndersteerStrength
		{
			get { return _WheelTelemetryUndersteerStrength; }
			set { _WheelTelemetryUndersteerStrength = value; OnPropertyChanged(); }
		}
		int _WheelTelemetryUndersteerStrength = 100;

		/// <summary>Curb and road bump impulse scale in percent (0 to 100, default 100).</summary>
		[DefaultValue(100), Description("Strength of curb strikes and suspension bump impulses from live telemetry (0 to 100 %).")]
		public int WheelTelemetryCurbStrength
		{
			get { return _WheelTelemetryCurbStrength; }
			set { _WheelTelemetryCurbStrength = value; OnPropertyChanged(); }
		}
		int _WheelTelemetryCurbStrength = 100;

		/// <summary>Telemetry surface damping scale in percent (0 to 200, default 100).</summary>
		[DefaultValue(100), Description("How strongly live surface and speed telemetry scales wheel damping (0 to 200 %).")]
		public int WheelTelemetryDampingStrength
		{
			get { return _WheelTelemetryDampingStrength; }
			set { _WheelTelemetryDampingStrength = value; OnPropertyChanged(); }
		}
		int _WheelTelemetryDampingStrength = 100;

		/// <summary>Where a wheel's road and impact feel comes from while live telemetry drives its centering spring.</summary>
		[DefaultValue(x360ce.Plugins.WheelRumbleSource.GameWithoutGearChanges), Description("Where the wheel's road and impact feel comes from while live telemetry drives the centering spring: the game's rumble, the game's rumble without its gear change kicks, telemetry hits, or both.")]
		public x360ce.Plugins.WheelRumbleSource WheelRumbleSource
		{
			get { return _WheelRumbleSource; }
			set { _WheelRumbleSource = value; OnPropertyChanged(); }
		}
		x360ce.Plugins.WheelRumbleSource _WheelRumbleSource = x360ce.Plugins.WheelRumbleSource.GameWithoutGearChanges;

		/// <summary>How much of the game's rumble plays while the gear changes, in percent (0 to 100, default 0).</summary>
		[DefaultValue(0), Description("How much of the game's rumble plays for a moment while the gear changes (0 to 100 %).")]
		public int WheelGearChangeRumble
		{
			get { return _WheelGearChangeRumble; }
			set { _WheelGearChangeRumble = value; OnPropertyChanged(); }
		}
		int _WheelGearChangeRumble;

		#endregion


		[DefaultValue(false), Description("Hide a device through HID Hide while it is mapped to a controller.")]
		public bool HidHideConfigureAutomatically
		{
			get { return _HidHideConfigureAutomatically; }
			set { _HidHideConfigureAutomatically = value; OnPropertyChanged(); }
		}
		bool _HidHideConfigureAutomatically = false;

		[DefaultValue("{7}")]
		public string GuideButtonAction
		{
			get { return _GuideButtonAction; }
			set { _GuideButtonAction = value; OnPropertyChanged(); }
		}
		string _GuideButtonAction = "{7}";

		/// <summary>The master switch for the emulated controllers.</summary>
		/// <remarks>
		/// Off means no virtual controller is made whatever the game asks for, so a wheel can be swapped
		/// for a real pad without closing the program. Read on the engine thread once per pass.
		/// </remarks>
		[DefaultValue(true), Description("Turns the emulated controllers on. Off leaves games with the real ones.")]
		public bool XInputEnabled
		{
			get { return _XInputEnabled; }
			set { _XInputEnabled = value; OnPropertyChanged(); }
		}
		bool _XInputEnabled = true;

		/// <summary>Whether the emulation hotkey is registered at all. Off by default, so no keys are taken from other programs unasked.</summary>
		[DefaultValue(false), Description("Turns the emulation hotkey on. Off by default, so the keys stay with other programs until you choose.")]
		public bool EmulationHotkeyEnabled
		{
			get { return _EmulationHotkeyEnabled; }
			set { _EmulationHotkeyEnabled = value; OnPropertyChanged(); }
		}
		bool _EmulationHotkeyEnabled;

		/// <summary>The keys the field starts with, so there is nothing to work out before ticking the box.</summary>
		public const string DefaultEmulationHotkey = "Ctrl + Alt + X";

		/// <summary>Keys that flip <see cref="XInputEnabled"/> from anywhere, written as Windows shows a shortcut.</summary>
		[DefaultValue(DefaultEmulationHotkey), Description("Keys that turn the emulated controllers on and off from inside a game, once the hotkey is on. Click the field and press other keys to change them.")]
		public string EmulationHotkey
		{
			get { return _EmulationHotkey; }
			set { _EmulationHotkey = value; OnPropertyChanged(); }
		}
		string _EmulationHotkey = DefaultEmulationHotkey;

		/// <summary>Whether the emulation buttons are looked for at all. Off by default, so a game gets every press unasked.</summary>
		[DefaultValue(false), Description("Turns the emulation buttons on. Off by default, so the buttons do only what the game makes of them until you choose.")]
		public bool EmulationButtonsEnabled
		{
			get { return _EmulationButtonsEnabled; }
			set { _EmulationButtonsEnabled = value; OnPropertyChanged(); }
		}
		bool _EmulationButtonsEnabled;

		/// <summary>The buttons the field starts with, which few games ask to be held together.</summary>
		public const string DefaultEmulationButtons = "Back + Start";

		/// <summary>Buttons of an emulated controller that, held together, flip <see cref="XInputEnabled"/>, written as <see cref="HotkeyHelper.FormatButtons"/> writes them.</summary>
		[DefaultValue(DefaultEmulationButtons), Description("Controller buttons that, held together, turn the emulated controllers on and off from inside a game, once they are on. Click the field and hold other buttons to change them.")]
		public string EmulationButtons
		{
			get { return _EmulationButtons; }
			set { _EmulationButtons = value; OnPropertyChanged(); }
		}
		string _EmulationButtons = DefaultEmulationButtons;

		/// <summary>Whether a hotkey press answers with a note on the screen, where a game would hide the tray balloon.</summary>
		[DefaultValue(true), Description("Shows a short note on the screen when the hotkey is pressed, where a game would hide the usual notification.")]
		public bool EmulationHotkeyOverlay
		{
			get { return _EmulationHotkeyOverlay; }
			set { _EmulationHotkeyOverlay = value; OnPropertyChanged(); }
		}
		bool _EmulationHotkeyOverlay = true;

		/// <summary>Whether a device the current game uses disconnecting or connecting again is said with a note on the screen.</summary>
		[DefaultValue(true), Description("Shows a short note on the screen when a device mapped to the current game disconnects or connects again.")]
		public bool DeviceChangeOverlay
		{
			get { return _DeviceChangeOverlay; }
			set { _DeviceChangeOverlay = value; OnPropertyChanged(); }
		}
		bool _DeviceChangeOverlay = true;

		/// <summary>Whether a recording for a bug report holds the options, the game's settings, each controller's mapping and the devices.</summary>
		[DefaultValue(true), Description("A recording for a bug report holds the options, the game’s settings, each controller’s mapping and every device.")]
		public bool RecordSettings { get { return _RecordSettings; } set { _RecordSettings = value; OnPropertyChanged(); } }
		bool _RecordSettings = true;

		/// <summary>Whether a recording holds what each device reports.</summary>
		[DefaultValue(true), Description("A recording for a bug report holds what each device reports: buttons, axes, sliders and hats as they change, and each failed read. Keyboards and mice are never recorded.")]
		public bool RecordInput { get { return _RecordInput; } set { _RecordInput = value; OnPropertyChanged(); } }
		bool _RecordInput = true;

		/// <summary>Whether a recording holds what each controller sends to the game.</summary>
		[DefaultValue(true), Description("A recording for a bug report holds what each controller sends to the game, its virtual controller, and what XInput reads back.")]
		public bool RecordOutput { get { return _RecordOutput; } set { _RecordOutput = value; OnPropertyChanged(); } }
		bool _RecordOutput = true;

		/// <summary>Whether a recording holds the vibration games ask for and the force sent to each device.</summary>
		[DefaultValue(true), Description("A recording for a bug report holds the vibration each game asks for, the force sent to each device, and how each device is held.")]
		public bool RecordForces { get { return _RecordForces; } set { _RecordForces = value; OnPropertyChanged(); } }
		bool _RecordForces = true;

		/// <summary>Whether a recording holds how fast the engine runs and each slow device call.</summary>
		[DefaultValue(true), Description("A recording for a bug report holds how fast the engine runs, and each device call that takes long or does not return.")]
		public bool RecordEngine { get { return _RecordEngine; } set { _RecordEngine = value; OnPropertyChanged(); } }
		bool _RecordEngine = true;

		/// <summary>Whether a recording holds whether the window answers, the program in front, the handles x360ce holds and its errors.</summary>
		[DefaultValue(true), Description("A recording for a bug report holds whether the window answers, which program is in front, the handles and memory x360ce holds, and its errors.")]
		public bool RecordWindow { get { return _RecordWindow; } set { _RecordWindow = value; OnPropertyChanged(); } }
		bool _RecordWindow = true;

		public BindingList<string> InternetDatabaseUrls { get; set; }

		public const string DefaultSdlDatabaseUrl = SdlUpdateClient.DefaultUrl;

		/// <summary>Where the [Update SDL Database] buttons download SDL's controller database from, as <see cref="SdlUpdateClient.Update"/> reads it.</summary>
		[DefaultValue(DefaultSdlDatabaseUrl), Description("Address every [Update SDL Database] button downloads SDL’s controller database from, SDL_GameControllerDB on GitHub unless you choose another. A raw.githubusercontent.com address of any fork or branch is checked against its newest commit; any other http or https address is downloaded as it is.")]
		public string SdlDatabaseUrl { get { return _SdlDatabaseUrl; } set { _SdlDatabaseUrl = value; OnPropertyChanged(); } }
		string _SdlDatabaseUrl;

		/// <summary>The addresses the SDL database address box offers.</summary>
		public BindingList<string> SdlDatabaseUrls { get; set; }

		[DefaultValue(null), Description("The locations to scan for games.")]
		public BindingList<string> GameScanLocations { get; set; }
		public string ComputerDisk { get; set; }
		public Guid ComputerId { get; set; }

		public string ProfilePath { get; set; }

		public Guid ProfileId { get; set; }

		public string LoginEnabled { get; set; }

		public string UserRsaPublicKey { get; set; }
		public string UserRsaPrivateKey { get; set; }

		public string CloudRsaPublicKey { get; set; }

		public string Username { get; set; }
		public string Password { get; set; }

		[DefaultValue(true)]
		public bool MinimizeToTray { get; set; }
		public bool ExcludeSupplementalDevices { get; set; }
		[DefaultValue(true), Description("Hide devices this program or another driver created, so it never reads its own output back as an input.")]
		public bool ExcludeVirtualDevices { get; set; }

		[DefaultValue(true), Description("Autodetect currently focussed game.")]
		public bool AutoDetectForegroundWindow
		{
			get { return _AutoDetectForegroundWindow; }
			set { _AutoDetectForegroundWindow = value; OnPropertyChanged(); }
		}
		bool _AutoDetectForegroundWindow = true;

		[DefaultValue(false), Description("Look for a newer version after the program starts. Off means no request is made.")]
		public bool CheckForUpdates
		{
			get { return _CheckForUpdates; }
			set { _CheckForUpdates = value; OnPropertyChanged(); }
		}
		bool _CheckForUpdates;

		[DefaultValue(true), Description("Install only an update whose program file carries a trusted digital signature.")]
		public bool CheckUpdateSignature
		{
			get { return _CheckUpdateSignature; }
			set { _CheckUpdateSignature = value; OnPropertyChanged(); }
		}
		bool _CheckUpdateSignature = true;

		[DefaultValue(true), Description("Install only an update whose program file is newer than this one and is the version its release announces.")]
		public bool CheckUpdateVersion
		{
			get { return _CheckUpdateVersion; }
			set { _CheckUpdateVersion = value; OnPropertyChanged(); }
		}
		bool _CheckUpdateVersion = true;

		[Description("When the program last looked for a newer version; the start-up look runs at most once a day.")]
		public DateTime LastUpdateCheck { get; set; }

		[Description("Tag of the releases list last seen with nothing newer on it, so the start-up look at an unchanged list downloads nothing.")]
		public string UpdateEtag { get; set; }

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

		#region Get Programs 

		[DefaultValue(2)]
		public int GetProgramsMinInstances { get { return _GetProgramsMinInstances; } set { _GetProgramsMinInstances = value; OnPropertyChanged(); } }
		int _GetProgramsMinInstances = 2;

		[DefaultValue(EnabledState.Enabled)]
		public EnabledState GetProgramsIncludeEnabled { get; set; } = EnabledState.Enabled;

		#endregion

		#region Options: Developing

		[DefaultValue(false), Description("Enable Form Info (CTRL+SHIFT+RMB)")]
		public bool EnableShowFormInfo { get { return _EnableShowFormInfo; } set { _EnableShowFormInfo = value; OnPropertyChanged(); } }
		bool _EnableShowFormInfo;


		[DefaultValue(false), Description("Show [Test...] Button.")]
		public bool ShowTestButton { get { return _ShowTestButton; } set { _ShowTestButton = value; OnPropertyChanged(); } }
		bool _ShowTestButton;

		#endregion

		/// <summary>Where the main window was left, so it opens there again.</summary>
		[Description("Where the main window was last left on screen.")]
		public JocysCom.ClassLibrary.Controls.PositionSettings WindowPosition { get; set; }
			= new JocysCom.ClassLibrary.Controls.PositionSettings();

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion


	}
}
