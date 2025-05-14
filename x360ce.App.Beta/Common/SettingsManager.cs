using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public partial class SettingsManager
	{

		// Products  - DInput Devices.
		// Programs  - Games of all Users.
		// Settings  - Links Product, Game and PadSettings.
		// Summaries - Summary of Settings of all Users.
		// UserControllers - Detail information about user Controllers.
		//
		//            [Vendor]    
		//             ↓
		//            [Product]   (export) → [Summaries]
		//                   ↓     ↑
		//  [PadSetting] ←  [Setting] → [Games] → (export) → [Programs]
		//

		public static UserGame CurrentGame;
		public static object CurrentGameLock = new object();

		public static event PropertyChangedEventHandler CurrentGame_PropertyChanged;

		/// <summary>
		/// Apply all settings to XML.
		/// </summary>
		public bool ApplyAllSettingsToXML()
		{
			var padControls = Global._MainWindow.MainPanel.MainBodyPanel.PadControls;
			for (int i = 0; i < padControls.Length; i++)
			{
				// Get pad control with settings.
				var padControl = Global._MainWindow.MainPanel.MainBodyPanel.PadControls[i];
				var setting = padControl.CurrentUserSetting;
				// Skip if not selected.
				if (setting == null)
					continue;
				SavePadSetting(setting, padControl.CurrentPadSetting);
			}
			return true;
		}
		public static void UpdateCurrentGame(UserGame game)
		{
			lock (CurrentGameLock)
			{
				// If nothing changed then...
				if (Equals(game, CurrentGame))
					return;
				// Detach event from old game.
				if (CurrentGame != null)
					CurrentGame.PropertyChanged -= CurrentGame_PropertyChanged;
				// Attach event to new game.
				if (game != null)
					game.PropertyChanged += CurrentGame_PropertyChanged;
				// Assign new game.
				CurrentGame = game;
				Global.DHelper.SettingsChanged = true;
				CurrentGame_PropertyChanged?.Invoke(null, null);
				//// If pad controls not initializes yet then return.
				//if (PadControls == null)
				//	return;
				//// Update PAD Control.
				//foreach (var ps in PadControls)
				//{
				//	if (ps != null)
				//		ps.UpdateFromCurrentGame();
				//}
			}
		}


		/// <summary>
		/// This method will be called during manual saving and automatically when form is closing.
		/// </summary>
		public static void SaveAll()
		{
			Properties.Settings.Default.Save();
			OptionsData.Save();
			UserSettings.Save();
			Summaries.Save();
			Programs.Save();
			UserGames.Save();
			Presets.Save();
			Layouts.Save();
			UserDevices.Save();
			UserMacros.Save();
			PadSettings.Save();
			UserInstances.Save();
			XInputMaskScanner.FileInfoCache.Save();
		}

		// Main application options

		/// <summary>Main application options.</summary>
		public static XSettingsData<Options> OptionsData = new XSettingsData<Options>("Options.xml", "x360ce Options");

		/// <summary>Property can be used as shorter access to main options.</summary>
		public static Options Options { get { return OptionsData.Items.FirstOrDefault(); } }

		// Global settings.

		/// <summary>Programs - Default settings for most popular Games.</summary>
		/// <remarks>Used to create PDB file.</remarks>
		public static XSettingsData<Engine.Data.Program> Programs = new XSettingsData<Engine.Data.Program>("Programs.xml", "Default settings for most popular Games.");

		/// <summary>Summaries - Default PadSettings for most popular Game, Device and Controller combination.</summary>
		public static XSettingsData<Engine.Data.Summary> Summaries = new XSettingsData<Engine.Data.Summary>("Summaries.xml", "Default PadSettings for most popular Game, Device and Controller combination.");

		/// <summary>Presets - Default PadSettings for most popular Devices (Products).</summary>
		public static XSettingsData<Preset> Presets = new XSettingsData<Preset>("Presets.xml", "Default PadSettings for most popular Devices (Products).");

		/// <summary>MapNames - Most popular layouts for Games.</summary>
		public static XSettingsData<Layout> Layouts = new XSettingsData<Layout>("Layouts.xml", "Most popular layouts for Games.");

		// User settings.

		/// <summary>User Games.</summary>
		public static XSettingsData<Engine.Data.UserGame> UserGames = new XSettingsData<Engine.Data.UserGame>("UserGames.xml", "User Games.");

		/// <summary>User Devices. Contains hardware details about Direct Input Instances (Devices).</summary>
		public static XSettingsData<Engine.Data.UserDevice> UserDevices = new XSettingsData<Engine.Data.UserDevice>("UserDevices.xml", "User Devices (Direct Input).");

		/// <summary>User Macro Maps. Advanced maps to keyboard, mouse and XInput.</summary>
		public static XSettingsData<Engine.Data.UserMacro> UserMacros = new XSettingsData<Engine.Data.UserMacro>("UserMacros.xml", "Keyboard, mouse and XInput macro maps.");

		/// <summary>User Instances. Map different Instance IDs to a single physical controller.</summary>
		public static XSettingsData<Engine.Data.UserInstance> UserInstances = new XSettingsData<Engine.Data.UserInstance>("UserInstances.xml", "User Controller Instances. Maps same device to multiple instance GUIDs it has on multiple PCs.");

		/// <summary>User Settings. Contains link between Game, DInput Device Instance, PadSetting and XInput Controller Index.</summary>
		public static XSettingsData<Engine.Data.UserSetting> UserSettings = new XSettingsData<Engine.Data.UserSetting>("UserSettings.xml", "User Settings.");

		// Property below is shared between User and Global settings:

		/// <summary>Contains PadSettings for Summaries, Presets and Settings.</summary>
		public static XSettingsData<Engine.Data.PadSetting> PadSettings = new XSettingsData<Engine.Data.PadSetting>("PadSettings.xml", "User and Preset PadSettings.");

		/// <summary>
		/// Update IsOnline value on the setting from the state for DirectInput Device.
		/// IsOnline will be set to "True" if device is connected, otherwise to "False".
		/// </summary>
		public static void RefreshDeviceIsOnlineValueOnSettings(params UserSetting[] settings)
		{
			foreach (var item in settings)
			{
				// Always online if test device or get actual online state.
				var isOnline =
					TestDeviceHelper.ProductGuid.Equals(item.ProductGuid) ||
					(GetDevice(item.InstanceGuid)?.IsOnline ?? false);
				if (item.IsOnline != isOnline)
					item.IsOnline = isOnline;
			}
		}

		public static Engine.Data.UserSetting GetSetting(Guid instanceGuid, string fileName)
		{
			lock (UserSettings.SyncRoot)
				return UserSettings.Items.FirstOrDefault(x =>
					x.InstanceGuid.Equals(instanceGuid) &&
					string.Compare(x.FileName, fileName, true) == 0
				);
		}

		public static UserDevice[] GetMappedDevices(string fileName, bool includeOffline = false)
		{
			// Get all mapped user device instance GUIDs for the specified game.
			var instanceGuids = UserSettings.ItemsToArraySynchronized()
				// Filter by game name.
				.Where(x => string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase))
				// Include only mapped devices.
				.Where(x => x.MapTo > (int)MapTo.None)
				// Select device instance GUIDs only.
				.Select(x => x.InstanceGuid)
				.ToArray();

			// Get all connected devices using the filtered instance GUIDs.
			UserDevice[] userDevices;
			lock (UserDevices.SyncRoot)
			{
				userDevices = UserDevices.Items
				// Filter by instance.
				.Where(x => instanceGuids.Contains(x.InstanceGuid))
				// Include only currently connected devices.
				.Where(x => includeOffline || x.IsOnline)
				.ToArray();
			}
			return userDevices;
		}

		/// <summary>
		/// Get settings by file name and optionally filter by mapped to XInput controller.
		/// </summary>
		public static List<UserSetting> GetSettings(string fileName, MapTo? mapTo = null)
		{
			lock (UserSettings.SyncRoot)
			{
				return UserSettings.Items
					// Filter by game.
					.Where(x => string.Compare(x.FileName, fileName, true) == 0)
					// Filter by map.
					.Where(x => !mapTo.HasValue || x.MapTo == (int)mapTo.Value)
					.ToList();
			}
		}

		public static UserDevice GetDevice(Guid instanceGuid)
		{
			return UserDevices.Items.FirstOrDefault(x =>
				x.InstanceGuid.Equals(instanceGuid));
		}

		public static PadSetting GetPadSetting(Guid padSettingChecksum)
		{
			// Convert to array in order to prevent selection while modified.
			lock (PadSettings.SyncRoot)
				return PadSettings.Items
				.FirstOrDefault(x => x.PadSettingChecksum.Equals(padSettingChecksum));
		}

		public static List<UserDevice> GetDevices(string fileName, MapTo mapTo)
		{
			var settings = GetSettings(fileName);
			// Get all mapped to specific index.
			var instances = settings
				.Where(x => x.MapTo == (int)mapTo)
				.Select(x => x.InstanceGuid).ToArray();
			var devices = UserDevices.Items
				.Where(x => instances.Contains(x.InstanceGuid))
				.ToList();
			// Return available devices.
			return devices;
		}

		static object saveReadFileLock = new object();

		#region ■ Load and Validate Data

		public static void Load()
		{
			// Load main application options first.
			OptionsData.ValidateData = Options_ValidateData;
			OptionsData.Load();
			// Load user settings second.
			UserSettings.ValidateData = UserSettings_ValidateData;
			UserSettings.Load();
			// Load settings which do not require validation.
			Presets.Load();
			Summaries.Load();
			PadSettings.Load();
			UserMacros.Load();
			UserInstances.Load();
			// Load settings which must be validated.
			Programs.ValidateData = Programs_ValidateData;
			Programs.Load();
			UserGames.ValidateData = Games_ValidateData;
			UserGames.Load();
			Layouts.ValidateData = Layouts_ValidateData;
			Layouts.Load();
			// Load user devices and attach event which will hide them with HID Guardian when IsHidden property modified.
			UserDevices.Load();
			UserDevices.Items.ListChanged += UserDevices_Items_ListChanged;
			UserDevices.Items.RaiseListChangedEvents = true;
		}

		public static void SetSynchronizingObject(TaskScheduler so = null)
		{
			// Make sure that all GridViews are updated on the same thread as MainForm when data changes.
			// For example User devices will be removed and added on separate thread.
			UserSettings.Items.SynchronizingObject = so;
			Summaries.Items.SynchronizingObject = so;
			UserDevices.Items.SynchronizingObject = so;
			UserMacros.Items.SynchronizingObject = so;
			UserGames.Items.SynchronizingObject = so;
			UserInstances.Items.SynchronizingObject = so;
			Layouts.Items.SynchronizingObject = so;
			Programs.Items.SynchronizingObject = so;
			Presets.Items.SynchronizingObject = so;
			PadSettings.Items.SynchronizingObject = so;
			OptionsData.Items.SynchronizingObject = so;
		}

		//public void FixLeaks()
		//{
		//	UserSettings.FixLeaks();
		//	Summaries.FixLeaks();
		//	UserDevices.FixLeaks();
		//	UserMacros.FixLeaks();
		//	UserGames.FixLeaks();
		//	UserInstances.FixLeaks();
		//	Layouts.FixLeaks();
		//	Programs.FixLeaks();
		//	Presets.FixLeaks();
		//	PadSettings.FixLeaks();
		//	OptionsData.FixLeaks();
		//}

		static IList<Engine.Data.UserSetting> UserSettings_ValidateData(IList<Engine.Data.UserSetting> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				// If setting is not set then...
				if (item.SettingId == Guid.Empty)
					// Assign unique id because it must be linked with UserMacro and other records.
					item.SettingId = Guid.NewGuid();
			}
			return items;
		}

		static IList<Engine.Data.Program> Programs_ValidateData(IList<Engine.Data.Program> items)
		{
			// Make sure default settings have unique by file name.
			var distinctItems = items
				.GroupBy(p => p.FileName.ToLower())
				.Select(g => g.First())
				.ToList();
			return distinctItems;
		}

		static IList<Engine.Data.Layout> Layouts_ValidateData(IList<Engine.Data.Layout> items)
		{
			var def = Guid.Empty;
			var defaultItem = items.FirstOrDefault(x => x.Id == def);
			// If default item was not found then...
			if (defaultItem == null)
			{
				var item = new Layout();
				item.Id = def;
				item.Name = "Default";
				item.ButtonA = "A Button";
				item.ButtonB = "B Button";
				item.ButtonBack = "Back";
				item.ButtonGuide = "Guide";
				item.ButtonStart = "StartDInputService";
				item.ButtonX = "X Button";
				item.ButtonY = "Y Button";
				item.DPad = "D-Pad";
				item.DPadDown = "D-Pad Down";
				item.DPadLeft = "D-Pad Left";
				item.DPadRight = "D-Pad Right";
				item.DPadUp = "D-Pad Up";
				item.LeftShoulder = "Bumper";
				item.LeftThumbAxisX = "Stick Axis X";
				item.LeftThumbAxisY = "Stick Axis Y";
				item.LeftThumbButton = "Stick Button";
				item.LeftThumbDown = "Stick Down";
				item.LeftThumbLeft = "Stick Left";
				item.LeftThumbRight = "Stick Right";
				item.LeftThumbUp = "Stick Up";
				item.LeftTrigger = "Trigger";
				item.RightShoulder = "Bumper";
				item.RightThumbAxisX = "Stick Axis X";
				item.RightThumbAxisY = "Stick Axis Y";
				item.RightThumbButton = "Stick Button";
				item.RightThumbDown = "Stick Down";
				item.RightThumbLeft = "Stick Left";
				item.RightThumbRight = "Stick Right";
				item.RightThumbUp = "Stick Up";
				item.RightTrigger = "Trigger";
				items.Add(item);
			}
			return items;
		}

		static IList<Options> Options_ValidateData(IList<Options> items)
		{
			// If options empty then...
			if (items.Count == 0)
			{
				var o = new Options();
				items.Add(o);
			}
			// Set missing values to defaults.
			items[0].InitializeDefaults();
			return items;
		}

		static IList<Engine.Data.UserGame> Games_ValidateData(IList<Engine.Data.UserGame> items)
		{
			// Make sure default settings have unique by file name.
			var distinctItems = items
				.GroupBy(p => p.FileName.ToLower())
				.Select(g => g.First())
				.ToList();

			// Check if current application doesn't exist in the list then...
			var appFile = new FileInfo(Application.ExecutablePath);
			var appItem = distinctItems.FirstOrDefault(x => string.Compare(x.FileName, appFile.Name, true) == 0);
			if (appItem == null)
			{
				// Add x360ce.exe
				var scanner = new XInputMaskScanner();
				var item = scanner.FromDisk(appFile.FullName);
				var program = Programs.Items.FirstOrDefault(x => string.Compare(x.FileName, appFile.Name, true) == 0);
				item.LoadDefault(program);
				// Append to top.
				distinctItems.Insert(0, item);
			}
			else
			{
				appItem.FullPath = appFile.FullName;
				// Make sure it is on top.
				if (distinctItems.IndexOf(appItem) > 0)
				{
					distinctItems.Remove(appItem);
					distinctItems.Insert(0, appItem);
				}
			}
			for (int i = 0; i < distinctItems.Count; i++)
			{
				var item = distinctItems[i];
				// If emulation is enabled but type is not set, then set to virtual (one which can be done by this app).
				if (item.EnableMask > 0 && item.EmulationType == (int)EmulationType.None)
					item.EmulationType = (int)EmulationType.Virtual;
			}
			return distinctItems;
		}

		#endregion

		public static void Save()
		{
			lock (saveReadFileLock)
			{
				Programs.Save();
				UserGames.Save();
			}
		}

		public static UserGame ProcessExecutable(string filePath)
		{
			var fi = new FileInfo(filePath);
			if (!fi.Exists)
				return null;
			// Check if item already exists.
			var game = UserGames.Items.FirstOrDefault(x => x.FileName.ToLower() == fi.Name.ToLower());
			if (game == null)
			{
				var scanner = new XInputMaskScanner();
				game = scanner.FromDisk(fi.FullName);
				// Load default settings.
				var program = Programs.Items.FirstOrDefault(x => x.FileName.ToLower() == game.FileName.ToLower());
				game.LoadDefault(program);
				UserGames.Items.Add(game);
			}
			else
			{
				game.FullPath = fi.FullName;
			}
			Save();
			return game;
		}

		#region ■ Static Version

		#region ■ Constants

		public const string MappingsSection = "Mappings";
		public const string OptionsSection = "Options";

		#endregion // Constants

		#region ■ Member Variables
		static SettingsManager _current;
		#endregion // Member Variables

		#region ■ Public Methods
		/// <summary>
		/// Adds an entry in the control-setting map, generates a tool-tip for the setting.
		/// </summary>
		/// <param name="setting">The name of the setting.</param>
		/// <param name="control">The control used to edit the setting.</param>
		static public void AddMap<T>(Expression<Func<T, object>> setting, object control)
		{
			// Get the member expression
			var me = setting.Body as MemberExpression ?? ((UnaryExpression)setting.Body).Operand as MemberExpression;
			// Get the property
			var prop = (PropertyInfo)me.Member;
			// Get the setting name by reading the property
			//var keyName = prop.Name;
			// Get the description attribute
			var descAttr = GetCustomAttribute<DescriptionAttribute>(prop);
			var desc = descAttr?.Description ?? string.Empty;
			// Get the default value attribute
			var dvalAttr = GetCustomAttribute<DefaultValueAttribute>(prop);
			// Display help inside yellow header.
			// We could add settings EnableHelpTooltips=1, EnableHelpHeader=1
			if (control is Control c)
			{
				c.MouseHover += control_MouseEnter;
				c.MouseLeave += control_MouseLeave;
			}
			var item = new SettingsMapItem();
			item.Description = desc;
			//item.IniSection = sectionName;
			//item.MapTo = mapTo;
			//item.IniKey = keyName;
			item.Control = control;
			item.PropertyName = prop.Name;
			item.DefaultValue = dvalAttr?.Value;
			item.Property = prop;
			// Add to the map
			Current.SettingsMap.Add(item);
		}


		static public SettingsMapItem AddMap<T>(Expression<Func<T>> setting, Control control, MapTo mapTo = MapTo.None, MapCode code = default)
		{
			// Get the member expression
			var me = (MemberExpression)setting.Body;
			// Get the property
			var prop = (PropertyInfo)me.Member;
			// Get the description attribute
			var descAttr = GetCustomAttribute<DescriptionAttribute>(prop);
			var desc = descAttr != null
				? descAttr.Description
				: string.Empty;
			// Get the default value attribute
			var dvalAttr = GetCustomAttribute<DefaultValueAttribute>(prop);
			var dval = descAttr != null
				? (string)dvalAttr.Value
				: null;
			// Display help inside yellow header.
			// We could add settings EnableHelpTooltips=1, EnableHelpHeader=1
			control.MouseHover += control_MouseEnter;
			control.MouseLeave += control_MouseLeave;
			var item = new SettingsMapItem();
			item.Description = desc;
			item.Code = code;
			item.Control = control;
			item.MapTo = mapTo;
			item.PropertyName = prop.Name;
			item.DefaultValue = dval;
			item.Property = prop;
			// Add to the map
			Current.SettingsMap.Add(item);
			return item;
		}


		static void control_MouseLeave(object sender, EventArgs e)
		{
			//Console.WriteLine(string.Format("Mouse Leave: {0}", sender));
			Global.HMan.SetBody(System.Windows.MessageBoxImage.None);
		}

		static void control_MouseEnter(object sender, EventArgs e)
		{
			//Console.WriteLine(string.Format("Mouse Enter: {0}", sender));
			var control = (Control)sender;
			var item = Current.SettingsMap.FirstOrDefault(x => x.Control == control);
			if (item != null && !string.IsNullOrEmpty(item.Description))
			{
				Global.HMan.SetBodyInfo(item.Description);
			}
		}

		#endregion // Public Methods

		#region ■ Public Properties
		/// <summary>
		/// Gets the SettingManager singleton instance.
		/// </summary>
		public static SettingsManager Current
		{
			get { return _current = _current ?? new SettingsManager(); }
		}
		#endregion // Public Properties
		#endregion // Static Version
		#region ■ Instance Version

		public int saveCount = 0;
		public int loadCount = 0;

		public event EventHandler<SettingEventArgs> ConfigLoaded;

		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		object settingsMapLock = new object();

		List<SettingsMapItem> _SettingsMap;
		public List<SettingsMapItem> SettingsMap
		{
			get
			{
				lock (settingsMapLock)
				{
					if (_SettingsMap == null)
					{
						_SettingsMap = new List<SettingsMapItem>();
					}
					return _SettingsMap;
				}
			}
		}



		//public void ReadSettings()
		//{
		//	ReadSettings(IniFileName);
		//}

		static private T GetCustomAttribute<T>(PropertyInfo prop)
		{
			if (prop == null)
			{
				return default(T);
			}

			return (T)prop.GetCustomAttributes(typeof(T), false).FirstOrDefault();
		}

		//public void SetPadSetting(string padSectionName, DeviceInstance di)
		//{
		//	var ini2 = new Ini(IniFileName);
		//	//ps.PadSettingChecksum = Guid.Empty;
		//	ini2.SetValue(padSectionName, SettingName.ProductName, di.ProductName);
		//	ini2.SetValue(padSectionName, SettingName.ProductGuid, di.ProductGuid.ToString());
		//	ini2.SetValue(padSectionName, SettingName.InstanceGuid, di.InstanceGuid.ToString());
		//}

		public void SetComboBoxValue(ComboBox cbx, string text)
		{
			// Remove value from other box.
			var controls = SettingsMap.Select(x => x.Control).ToArray();
			foreach (Control control in controls)
			{
				if (
					// Control is ComboBox.
					control is ComboBox
					// controls belong to same parent.
					&& cbx.Parent == control.Parent
					// This is not same control.
					&& control != cbx
					// Text value is same.
					&& control.Text == text
					// Text value is not empty.
					&& !string.IsNullOrEmpty(text))
				{
					((ComboBox)control).SelectedIndex = -1;
					((ComboBox)control).Items.Clear();
					//SaveSettings(control);
				}
			}
			cbx.Items.Clear();
			cbx.Items.Add(text);
			cbx.SelectedIndex = 0;
		}

		#region ■ Load Settings

		/// <summary>
		/// Read setting from INI file into windows form control.
		/// </summary>
		public void LoadSetting(object control, string p = null, string value = null)
		{
			if (p != null && (
				p == nameof(PadSetting.GamePadType) ||
				p == nameof(PadSetting.ForceType) ||
				p == nameof(PadSetting.LeftMotorDirection) ||
				p == nameof(PadSetting.RightMotorDirection))
			)
			{
				var cbx = (ComboBox)control;
				for (int i = 0; i < cbx.Items.Count; i++)
				{
					if (cbx.Items[i] is KeyValuePair)
					{
						var kv = (KeyValuePair)cbx.Items[i];
						if (kv.Value == value)
						{
							cbx.SelectedIndex = i;
							break;
						}
					}
					else
					{
						var kv = System.Convert.ToInt32(cbx.Items[i]);
						if (kv.ToString() == value)
						{
							cbx.SelectedIndex = i;
							break;
						}
					}
				}
			}
			// If DI menu strip attached.
			else if (control is ComboBox cbx)
			{
				var map = SettingsMap.FirstOrDefault(x => x.Control == control);
				if (map != null && map.Code != default)
				{
					var text = SettingsConverter.FromIniValue(value);
					SetComboBoxValue(cbx, text);
				}
				else
				{
					cbx.Text = value;
				}
			}
			else if (control is TextBox tbx)
			{
				// If setting is read-only.
				if (p == nameof(UserSetting.ProductName))
					return;
				if (p == nameof(UserSetting.ProductGuid))
					return;
				if (p == nameof(UserSetting.InstanceGuid))
					return;
				if (p == nameof(Options.SettingsVersion))
					return;
				tbx.Text = value;
			}
			else if (control is NumericUpDown nud)
			{
				decimal n = 0;
				decimal.TryParse(value, out n);
				if (n < nud.Minimum)
					n = nud.Minimum;
				if (n > nud.Maximum)
					n = nud.Maximum;
				nud.Value = n;
			}
			else if (control is TrackBar tc)
			{
				int n = 0;
				int.TryParse(value, out n);
				// convert 256  to 100%
				if (p == nameof(PadSetting.AxisToDPadDeadZone) ||
					p == nameof(PadSetting.AxisToDPadOffset) ||
					p == nameof(PadSetting.LeftTriggerDeadZone) ||
					p == nameof(PadSetting.RightTriggerDeadZone))
				{
					if (p == nameof(PadSetting.AxisToDPadDeadZone) && value == "")
						n = 256;
						n = System.Convert.ToInt32(n / 256F * 100F);
				}
				// Convert 500 to 100%
				else if (p == nameof(PadSetting.LeftMotorPeriod) || p == nameof(PadSetting.RightMotorPeriod))
				{
					n = System.Convert.ToInt32(n / 500F * 100F);
				}
				// Convert 32767 to 100%
				else if (p == nameof(PadSetting.LeftThumbDeadZoneX) ||
						 p == nameof(PadSetting.LeftThumbDeadZoneY) ||
						 p == nameof(PadSetting.RightThumbDeadZoneX) ||
						 p == nameof(PadSetting.RightThumbDeadZoneY))
				{
					n = System.Convert.ToInt32(n / ((float)Int16.MaxValue) * 100F);
				}
				if (n < tc.Minimum)
					n = tc.Minimum;
				if (n > tc.Maximum)
					n = tc.Maximum;
				tc.Value = n;
			}
			else if (control is CheckBox chb)
			{
				int n = 0;
				int.TryParse(value, out n);
				chb.Checked = n != 0;
			}
		}

		#endregion

		#region ■ Save Settings

		public string GetSettingValue(object control)
		{
			var item = SettingsMap.First(x => x.Control == control);
			var p = item.PropertyName;
			string v = string.Empty;
			if (p == nameof(PadSetting.GamePadType) ||
				p == nameof(PadSetting.ForceType) ||
				p == nameof(PadSetting.LeftMotorDirection) ||
				p == nameof(PadSetting.RightMotorDirection))
			{
				var v1 = ((ComboBox)control).SelectedItem;
				if (v1 == null)
					v = "0";
				else if (v1 is KeyValuePair)
					v = ((KeyValuePair)v1).Value;
				else
					v = System.Convert.ToInt32(v1).ToString();
			}
			// If DI menu strip attached.
			else if (control is ComboBox cbx)
			{
				var map = SettingsMap.FirstOrDefault(x => x.Control == control);
				if (map != null && map.Code != default)
				{
					v = SettingsConverter.ToIniValue(cbx.Text);
					// make sure that disabled button value is "0".
					if (SettingName.IsButton(p) && string.IsNullOrEmpty(v))
						v = "0";
				}
				else
				{
					v = cbx.Text;
				}
			}
			else if (control is TextBox tbx)
			{
				// if setting is read-only.
				if (p == nameof(UserSetting.InstanceGuid) || p == nameof(UserSetting.ProductGuid))
				{
					v = string.IsNullOrEmpty(tbx.Text) ? Guid.Empty.ToString("D") : tbx.Text;
				}
				else
					v = tbx.Text;
			}
			else if (control is NumericUpDown nud)
			{
				v = nud.Value.ToString();
			}
			else if (control is TrackBar)
			{
				TrackBar tc = (TrackBar)control;
				// convert 100%  to 256
				if (p == nameof(PadSetting.AxisToDPadDeadZone) ||
					p == nameof(PadSetting.AxisToDPadOffset) ||
					p == nameof(PadSetting.LeftTriggerDeadZone) ||
					p == nameof(PadSetting.RightTriggerDeadZone))
				{
					v = System.Convert.ToInt32(tc.Value / 100F * 256F).ToString();
				}
				// convert 100%  to 500
				else if (p == nameof(PadSetting.LeftMotorPeriod) ||
						 p == nameof(PadSetting.RightMotorPeriod))
				{
					v = System.Convert.ToInt32(tc.Value / 100F * 500F).ToString();
				}
				// Convert 100% to 32767
				else if (p == nameof(PadSetting.LeftThumbDeadZoneX) ||
						 p == nameof(PadSetting.LeftThumbDeadZoneY) ||
						 p == nameof(PadSetting.RightThumbDeadZoneX) ||
						 p == nameof(PadSetting.RightThumbDeadZoneY))
				{
					v = System.Convert.ToInt32(tc.Value / 100F * short.MaxValue).ToString();
				}
				else
					v = tc.Value.ToString();
			}
			else if (control is CheckBox)
			{
				CheckBox tc = (CheckBox)control;
				v = tc.Checked ? "1" : "0";
			}
			else if (control is DataGridView)
			{
				var grid = (DataGridView)control;
				if (grid.Enabled)
				{
					var data = grid.Rows.Cast<DataGridViewRow>().Where(x => x.Visible).Select(x => x.DataBoundItem as UserSetting)
						// Make sure that only enabled controllers are added.
						.Where(x => x != null && x.IsEnabled).ToArray();
					data = FilterSettings(data);
					var sections = data.Select(x => GetInstanceSection(x.InstanceGuid)).ToArray();
					// x360ce.dll must combine devices separated by comma.
					//v = string.Join(",", sections);
					// Use backwards compatible mode (one device at the time).
					v = sections.FirstOrDefault() ?? "";
					// Note: Must code device combine workaround.
				}
				else
				{
					v = "AUTO";
				}
			}
			return v;
		}

		public static UserSetting[] FilterSettings(UserSetting[] settings)
		{
			// Make sure that non-supported keyboard, mouse and test devices are excluded.
			var exludeTypes = new[]
			{
				(int)SharpDX.DirectInput.DeviceType.Mouse,
				(int)SharpDX.DirectInput.DeviceType.Keyboard,
			};
			return settings
				.Where(x => !exludeTypes.Contains(x.DeviceType) && x.ProductGuid != TestDeviceHelper.ProductGuid)
				.ToArray();
		}

		#endregion

		public string GetInstanceSection(Guid instanceGuid)
		{
			return string.Format("IG_{0:N}", instanceGuid).ToUpper();
		}

		public bool ContainsInstanceSection(Guid instanceGuid, string iniFileName, out string sectionName)
		{
			var ini2 = new Ini(iniFileName);
			var section = GetInstanceSection(instanceGuid);
			var contains = !string.IsNullOrEmpty(ini2.GetValue(section, "InstanceGUID"));
			sectionName = contains ? section : null;
			return contains;
		}

		public bool ContainsInstanceSectionOld(Guid instanceGuid, string iniFileName, out string sectionName)
		{
			var ini2 = new Ini(iniFileName);
			for (int i = 1; i <= 4; i++)
			{
				var section = string.Format("PAD{0}", i);
				var v = ini2.GetValue(section, "InstanceGUID");
				if (string.IsNullOrEmpty(v))
					continue;
				if (v.ToLower() == instanceGuid.ToString("D").ToLower())
				{
					sectionName = section;
					return true;
				}
			}
			sectionName = null;
			return false;
		}

		#endregion // Instance Version

		public void FillSearchParameterWithInstances(List<SearchParameter> sp)
		{
			// Select user devices as parameters to search.
			var userDevices = UserSettings.ItemsToArraySynchronized()
				.Select(x => x.InstanceGuid).Distinct()
				// Do not add empty records.
				.Where(x => x != Guid.Empty)
				.Select(x => new SearchParameter() { InstanceGuid = x })
				.ToArray();
			sp.AddRange(userDevices);
		}

		public void FillSearchParameterWithFiles(List<SearchParameter> sp)
		{
			// Select enabled user game/device as parameters to search.
			var settings = UserSettings.ItemsToArraySynchronized()
				.Where(x => x.MapTo > 0).ToArray();
			foreach (var setting in settings)
			{
				var fileName = Path.GetFileName(setting.FileName);
				// Do not add empty records.
				if (string.IsNullOrEmpty(fileName))
					continue;
				var fileTitle = EngineHelper.FixName(setting.ProductName, setting.FileName);
				// If search doesn't contain game then...
				if (!sp.Any(x => x.FileName == fileName && x.FileProductName == fileTitle))
				{
					// Add it to search parameters.
					var p = new SearchParameter();
					p.FileName = fileName;
					p.FileProductName = fileTitle;
					sp.Add(p);
				}
			}
		}

		public static void MapGamePadDevices(UserGame game, MapTo mappedTo, UserDevice[] devices, bool configureHidGuardian)
		{
			foreach (var ud in devices)
			{
				// Try to get existing setting by instance GUID and file name.
				var setting = GetSetting(ud.InstanceGuid, game.FileName);
				// If device setting for the game was not found then.
				if (setting == null)
				{
					// Create new setting.
					setting = AppHelper.GetNewSetting(ud, game, mappedTo);
					// Get auto-configured pad setting.
					var ps = AutoMapHelper.GetAutoPreset(ud);
					Current.LoadPadSettingAndCleanup(setting, ps, true);
					Current.SyncFormFromPadSetting(mappedTo, ps);
					// Refresh online status
					RefreshDeviceIsOnlineValueOnSettings(setting);
					// Load created setting.
					//SettingsManager.Current.LoadPadSettings(MappedTo, ps);
				}
				else
				{
					// Enable if not enabled.
					if (!setting.IsEnabled)
						setting.IsEnabled = true;
					// Map setting to current pad.
					setting.MapTo = (int)mappedTo;
				}
			}
			if (configureHidGuardian)
			{
				var instanceGuids = devices.Select(x => x.InstanceGuid).ToArray();
				var changed = AutoHideShowMappedDevices(game, instanceGuids);
				if (changed)
					AppHelper.SynchronizeToHidGuardian(instanceGuids);
			}
		}

		public static void UnMapGamePadDevices(UserGame game, UserSetting setting, bool configureHidGuardian)
		{
			// Disable map.
			if (setting != null)
				setting.MapTo = (int)MapTo.Disabled;
			if (configureHidGuardian)
			{
				// Unhide device if no longer mapped.
				var changed = SettingsManager.AutoHideShowMappedDevices(game, new Guid[] { setting.InstanceGuid });
				if (changed)
					AppHelper.SynchronizeToHidGuardian(setting.InstanceGuid);
			}
		}

		/// <summary>
		/// Hide devices if they are mapped to the game, unhide devices if they are not mapped.
		/// </summary>
		/// <returns>True if device hide/show state changed.</returns>
		public static bool AutoHideShowMappedDevices(UserGame game, Guid[] instanceGuids = null)
		{
			var changed = false;
			// Get affected devices.
			UserDevice[] devices;
			lock (UserDevices.SyncRoot)
				devices = instanceGuids == null
					? UserDevices.Items.ToArray()
					: UserDevices.Items.Where(x => instanceGuids.Contains(x.InstanceGuid)).ToArray();
			// Get devices instances mapped to the game.
			var mappedInstanceGuids = GetMappedDevices(game?.FileName, true)
				.Select(x => x.InstanceGuid).ToArray();
			for (int i = 0; i < devices.Length; i++)
			{
				var ud = devices[i];
				// Skip Keyboards and mice.
				if (ud.IsKeyboard || ud.IsMouse)
					continue;
				// Mapped devices must be hidden.
				var isHidden = mappedInstanceGuids.Contains(ud.InstanceGuid);
				// If value is different then change.
				if (ud.IsHidden != isHidden)
				{
					ud.IsHidden = isHidden;
					changed = true;
				}
			}
			return changed;
		}

		public void InitNewUserKeyboardMapForGame(UserSetting userSetting)
		{
			if (userSetting == null)
				return;
			var item = UserMacros.Items.FirstOrDefault(x => x.SettingId == userSetting.SettingId);
			if (item != null)
				return;
			item = new UserMacro();
			item.SettingId = userSetting.SettingId;
			item.LoadGuideButton();
			UserMacros.Add(item);
		}

	}
}
