using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SharpDX.DirectInput;
using x360ce.App.Controls;
using System.Linq;
using x360ce.Engine.Data;
using x360ce.Engine;
using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;
using System.IO;

namespace x360ce.App
{
	public class SettingManager
	{

		// Products - DInput Devices
		// Programs - Games of all Users.
		// Settings - Links Product, Game and PadSettings
		// Summaries - Summary of Settings of all Users.
		//
		//            [Vendor]    
		//             ↓
		//            [Product]   (export) → [Summaries]
		//                   ↓     ↑
		//  [PadSetting] ←  [Setting] → [Games] → (export) → [Programs]
		//

		/// <summary>User Settings.</summary>
		public static SettingsData<Engine.Data.Setting> Settings = new SettingsData<Engine.Data.Setting>("Settings");

		/// <summary>Summary of most popular Settings.</summary>
		public static SettingsData<Engine.Data.Summary> Summaries = new SettingsData<Engine.Data.Summary>("Summaries");

		/// <summary>User Games.</summary>
		public static SettingsData<Engine.Data.UserGame> Games = new SettingsData<Engine.Data.UserGame>("UserGames");

		/// <summary>Most popular Programs and Games.</summary>
		public static SettingsData<Engine.Data.Program> Programs = new SettingsData<Engine.Data.Program>("Programs");

		/// <summary>Presets</summary>
		public static SettingsData<Preset> Presets = new SettingsData<Preset>("Presets");

		/// <summary>Preset PadSettings</summary>
		public static SettingsData<Engine.Data.PadSetting> PadSettings = new SettingsData<Engine.Data.PadSetting>("PadSettings");

		static object saveReadFileLock = new object();

		public static void Save(bool updateGameDatabase = false)
		{
			if (updateGameDatabase)
			{
				GameDatabaseManager.Current.SetPrograms(Programs.Items, Games.Items);
			}
			lock (saveReadFileLock)
			{
				Programs.Save();
				Games.Save();
			}
		}

		public static void ProcessExecutable(string filePath)
		{
			var fi = new FileInfo(filePath);
			if (!fi.Exists) return;
			// Check if item already exists.
			var game = Games.Items.FirstOrDefault(x => x.FileName.ToLower() == fi.Name.ToLower());
			if (game == null)
			{
				game = x360ce.Engine.Data.UserGame.FromDisk(fi.FullName);
				// Load default settings.
				var program = Programs.Items.FirstOrDefault(x => x.FileName.ToLower() == game.FileName.ToLower());
				game.LoadDefault(program);
				Games.Items.Add(game);
			}
			else
			{
				game.FullPath = fi.FullName;
			}
			Save();
		}

		#region Static Version
		#region Constants
		public const string IniFileName = "x360ce.ini";
		public const string TmpFileName = "x360ce.tmp";
		#endregion // Constants

		#region Member Variables
		static SettingManager _current;
		#endregion // Member Variables

		#region Public Methods
		/// <summary>
		/// Adds an entry in the control-setting map and also generates a tool-tip for the setting.
		/// </summary>
		/// <param name="sectionName">
		/// The name of the section.
		/// </param>
		/// <param name="setting">
		/// The name of the setting.
		/// </param>
		/// <param name="control">
		/// The control used to edit the setting.
		/// </param>
		/// <param name="settingsMap">
		/// The settings map to add the entry in.
		/// </param>
		static public void AddMap<T>(string sectionName, Expression<Func<T>> setting, Control control, Dictionary<string, Control> settingsMap)
		{
			// Validate
			if (settingsMap == null) throw new ArgumentNullException("settingsMap");

			// Get the member expression
			var me = (MemberExpression)setting.Body;

			// Get the property
			var prop = (PropertyInfo)me.Member;

			// Get the setting name by reading the property
			var settingName = (string)prop.GetValue(null, null);

			// Get the description attribute
			var descAttr = GetCustomAttribute<DescriptionAttribute>(prop);
			var desc = (descAttr != null ? descAttr.Description : string.Empty);

			// Set the tool-tip
			// MainForm.Current.ToolTip.SetToolTip(control, desc);

			// Alternative (a little bit less obstructive) way to display help inside yellow header.
			// We could add settings EnableHelpTooltips=1, EnableHelpHeader=1
			control.MouseHover += control_MouseEnter;
			control.MouseLeave += control_MouseLeave;
			Descriptions.Add(control, desc);

			// Add to the map
			settingsMap.Add(sectionName + settingName, control);
		}

		static Dictionary<Control, string> Descriptions = new Dictionary<Control, string>();

		static void control_MouseLeave(object sender, EventArgs e)
		{
			MainForm.Current.UpdateHelpHeader();
		}

		static void control_MouseEnter(object sender, EventArgs e)
		{
			var control = (Control)sender;
			if (Descriptions.ContainsKey(control))
			{
				MainForm.Current.HelpBodyLabel.Text = Descriptions[control];
			}
		}

		/// <summary>
		/// Adds an entry in the global control-setting map and also generates a tool-tip for the setting.
		/// </summary>
		/// <param name="sectionName">
		/// The name of the section.
		/// </param>
		/// <param name="setting">
		/// The name of the setting.
		/// </param>
		/// <param name="control">
		/// The control used to edit the setting.
		/// </param>
		static public void AddMap<T>(string sectionName, Expression<Func<T>> setting, Control control)
		{
			AddMap(sectionName, setting, control, SettingManager.Current.SettingsMap);
		}
		#endregion // Public Methods

		#region Public Properties
		/// <summary>
		/// Gets the SettingManager singleton instance.
		/// </summary>
		public static SettingManager Current
		{
			get { return _current = _current ?? new SettingManager(); }
		}
		#endregion // Public Properties
		#endregion // Static Version
		#region Instance Version

		public SettingManager()
		{
		}

		public int saveCount = 0;
		public int loadCount = 0;

		public event EventHandler<SettingEventArgs> ConfigSaved;
		public event EventHandler<SettingEventArgs> ConfigLoaded;

		public bool IsDebugMode { get { return ((CheckBox)SettingsMap[@"Options\" + SettingName.DebugMode]).Checked; } }
		public bool ExcludeSuplementalDevices { get { return ((CheckBox)SettingsMap[@"Options\" + SettingName.ExcludeSupplementalDevices]).Checked; } }
		public bool ExcludeVirtualDevices { get { return ((CheckBox)SettingsMap[@"Options\" + SettingName.ExcludeVirtualDevices]).Checked; } }

		Dictionary<string, Control> _settingsMap;
		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		public Dictionary<string, Control> SettingsMap
		{
			get { return _settingsMap = _settingsMap ?? new Dictionary<string, Control>(); }
		}

		public void ReadSettings()
		{
			ReadSettings(IniFileName);
		}

		static private T GetCustomAttribute<T>(PropertyInfo prop)
		{
			if (prop == null)
			{
				return default(T);
			}

			return (T)prop.GetCustomAttributes(typeof(T), false).FirstOrDefault();
		}

		/// <summary>
		/// Read setting from INI file into windows form control.
		/// </summary>
		public void ReadSettingTo(Control control, string key, string value)
		{
			if (key == SettingName.HookMode ||
				key.EndsWith(SettingName.DeviceSubType) ||
				key.EndsWith(SettingName.ForceType) ||
				key.EndsWith(SettingName.LeftMotorDirection) ||
				key.EndsWith(SettingName.RightMotorDirection) ||
				key.EndsWith(SettingName.PassThroughIndex) ||
				key.EndsWith(SettingName.CombinedIndex))
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
			// If Di menu strip attached.
			else if (control is ComboBox)
			{
				var cbx = (ComboBox)control;
				if (control.ContextMenuStrip == null)
				{
					control.Text = value;
				}
				else
				{
					var text = new SettingsConverter(value, key).ToTextValue();
					SetComboBoxValue(cbx, text);
				}
			}
			else if (control is TextBox)
			{
				// if setting is read-only.
				if (key == SettingName.ProductName) return;
				if (key == SettingName.ProductGuid) return;
				if (key == SettingName.InstanceGuid) return;
				if (key == SettingName.InternetDatabaseUrl && string.IsNullOrEmpty(value)) value = SettingName.DefaultInternetDatabaseUrl;
				// Always override version.
				if (key == SettingName.Version) value = SettingName.DefaultVersion;
				control.Text = value;
			}
			else if (control is ListBox)
			{
				var lbx = (ListBox)control;
				lbx.Items.Clear();
				if (string.IsNullOrEmpty(value))
				{
					var folders = new List<string>();
					if (Environment.Is64BitOperatingSystem)
					{
						var pf = Environment.GetEnvironmentVariable("ProgramW6432");
						if (System.IO.Directory.Exists(pf)) folders.Add(pf);
					}
					var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
					if (System.IO.Directory.Exists(pf86)) folders.Add(pf86);
					lbx.Items.AddRange(folders.ToArray());
				}
				else
				{
					lbx.Items.AddRange(value.Split(','));
				}
			}
			else if (control is NumericUpDown)
			{
				var nud = (NumericUpDown)control;
				decimal n = 0;
				decimal.TryParse(value, out n);
				if (n < nud.Minimum) n = nud.Minimum;
				if (n > nud.Maximum) n = nud.Maximum;
				nud.Value = n;
			}
			else if (control is TrackBar)
			{
				TrackBar tc = (TrackBar)control;
				int n = 0;
				int.TryParse(value, out n);
				// convert 256  to 100%
				if (key == SettingName.AxisToDPadDeadZone || key == SettingName.AxisToDPadOffset || key == SettingName.LeftTriggerDeadZone || key == SettingName.RightTriggerDeadZone)
				{
					if (key == SettingName.AxisToDPadDeadZone && value == "") n = 256;
					n = System.Convert.ToInt32((float)n / 256F * 100F);
				}
				// Convert 500 to 100%
				else if (key == SettingName.LeftMotorPeriod || key == SettingName.RightMotorPeriod)
				{
					n = System.Convert.ToInt32((float)n / 500F * 100F);
				}
				// Convert 32767 to 100%
				else if (key == SettingName.LeftThumbDeadZoneX || key == SettingName.LeftThumbDeadZoneY || key == SettingName.RightThumbDeadZoneX || key == SettingName.RightThumbDeadZoneY)
				{
					n = System.Convert.ToInt32((float)n / ((float)Int16.MaxValue) * 100F);
				}
				if (n < tc.Minimum) n = tc.Minimum;
				if (n > tc.Maximum) n = tc.Maximum;
				tc.Value = n;
			}
			else if (control is CheckBox)
			{
				CheckBox tc = (CheckBox)control;
				int n = 0;
				int.TryParse(value, out n);
				tc.Checked = n != 0;
			}
		}

		/// <summary>
		/// Read settings from INI file into windows form controls.
		/// </summary>
		/// <param name="file">INI file containing settings.</param>
		/// <param name="iniSection">Read settings from specified section only. Null - read from all sections.</param>
		public void ReadSettings(string file)
		{
			var ini2 = new Ini(file);
			foreach (string path in SettingsMap.Keys)
			{
				string section = path.Split('\\')[0];
				// If this is PAD section.
				var padIndex = SettingName.GetPadIndex(path);
				if (padIndex > -1)
				{
					section = GetInstanceSection(padIndex);
					// If destination section is empty because controller is not connected then skip.
					if (string.IsNullOrEmpty(section)) continue;
				}
				Control control = SettingsMap[path];
				string key = path.Split('\\')[1];
				string v = ini2.GetValue(section, key);
				ReadSettingTo(control, key, v);
			}
			loadCount++;
			if (ConfigLoaded != null) ConfigLoaded(this, new SettingEventArgs(ini2.File.Name, loadCount));
			// Read XML too.
			//SettingsFile.Current.Load();
		}

		/// <summary>
		/// Get PadSetting object from INI by device Instance GUID.
		/// </summary>
		/// <param name="instanceGuid">Instance GUID.</param>
		/// <returns>PadSettings object.</returns>
		public PadSetting GetPadSetting(string padSectionName)
		{
			var ini2 = new Ini(IniFileName);
			var ps = new PadSetting();
			ps.PadSettingChecksum = Guid.Empty;
			ps.AxisToDPadDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToDPadDeadZone);
			ps.AxisToDPadEnabled = ini2.GetValue(padSectionName, SettingName.AxisToDPadEnabled);
			ps.AxisToDPadOffset = ini2.GetValue(padSectionName, SettingName.AxisToDPadOffset);
			ps.ButtonA = ini2.GetValue(padSectionName, SettingName.ButtonA);
			ps.ButtonB = ini2.GetValue(padSectionName, SettingName.ButtonB);
			ps.ButtonGuide = ini2.GetValue(padSectionName, SettingName.ButtonGuide);
			ps.ButtonBack = ini2.GetValue(padSectionName, SettingName.ButtonBack);
			ps.ButtonStart = ini2.GetValue(padSectionName, SettingName.ButtonStart);
			ps.ButtonX = ini2.GetValue(padSectionName, SettingName.ButtonX);
			ps.ButtonY = ini2.GetValue(padSectionName, SettingName.ButtonY);
			ps.DPad = ini2.GetValue(padSectionName, SettingName.DPad);
			ps.DPadDown = ini2.GetValue(padSectionName, SettingName.DPadDown);
			ps.DPadLeft = ini2.GetValue(padSectionName, SettingName.DPadLeft);
			ps.DPadRight = ini2.GetValue(padSectionName, SettingName.DPadRight);
			ps.DPadUp = ini2.GetValue(padSectionName, SettingName.DPadUp);
			ps.ForceEnable = ini2.GetValue(padSectionName, SettingName.ForceEnable);
			ps.ForceOverall = ini2.GetValue(padSectionName, SettingName.ForceOverall);
			ps.ForceSwapMotor = ini2.GetValue(padSectionName, SettingName.ForceSwapMotor);
			ps.ForceType = ini2.GetValue(padSectionName, SettingName.ForceType);
			ps.GamePadType = ini2.GetValue(padSectionName, SettingName.DeviceSubType);
			ps.LeftMotorPeriod = ini2.GetValue(padSectionName, SettingName.LeftMotorPeriod);
			ps.LeftMotorStrength = ini2.GetValue(padSectionName, SettingName.LeftMotorStrength);
			ps.LeftMotorDirection = ini2.GetValue(padSectionName, SettingName.LeftMotorDirection);
			ps.LeftShoulder = ini2.GetValue(padSectionName, SettingName.LeftShoulder);
			ps.LeftThumbAntiDeadZoneX = ini2.GetValue(padSectionName, SettingName.LeftThumbAntiDeadZoneX);
			ps.LeftThumbAntiDeadZoneY = ini2.GetValue(padSectionName, SettingName.LeftThumbAntiDeadZoneY);
			ps.LeftThumbLinearX = ini2.GetValue(padSectionName, SettingName.LeftThumbLinearX);
			ps.LeftThumbLinearY = ini2.GetValue(padSectionName, SettingName.LeftThumbLinearY);
			ps.LeftThumbAxisX = ini2.GetValue(padSectionName, SettingName.LeftThumbAxisX);
			ps.LeftThumbAxisY = ini2.GetValue(padSectionName, SettingName.LeftThumbAxisY);
			ps.LeftThumbButton = ini2.GetValue(padSectionName, SettingName.LeftThumbButton);
			ps.LeftThumbDeadZoneX = ini2.GetValue(padSectionName, SettingName.LeftThumbDeadZoneX);
			ps.LeftThumbDeadZoneY = ini2.GetValue(padSectionName, SettingName.LeftThumbDeadZoneY);
			ps.LeftThumbDown = ini2.GetValue(padSectionName, SettingName.LeftThumbDown);
			ps.LeftThumbLeft = ini2.GetValue(padSectionName, SettingName.LeftThumbLeft);
			ps.LeftThumbRight = ini2.GetValue(padSectionName, SettingName.LeftThumbRight);
			ps.LeftThumbUp = ini2.GetValue(padSectionName, SettingName.LeftThumbUp);
			ps.LeftTrigger = ini2.GetValue(padSectionName, SettingName.LeftTrigger);
			ps.LeftTriggerDeadZone = ini2.GetValue(padSectionName, SettingName.LeftTriggerDeadZone);
			ps.PassThrough = ini2.GetValue(padSectionName, SettingName.PassThrough);
			ps.RightMotorPeriod = ini2.GetValue(padSectionName, SettingName.RightMotorPeriod);
			ps.RightMotorStrength = ini2.GetValue(padSectionName, SettingName.RightMotorStrength);
			ps.RightMotorDirection = ini2.GetValue(padSectionName, SettingName.RightMotorDirection);
			ps.RightShoulder = ini2.GetValue(padSectionName, SettingName.RightShoulder);
			ps.RightThumbAntiDeadZoneX = ini2.GetValue(padSectionName, SettingName.RightThumbAntiDeadZoneX);
			ps.RightThumbAntiDeadZoneY = ini2.GetValue(padSectionName, SettingName.RightThumbAntiDeadZoneY);
			ps.RightThumbAxisX = ini2.GetValue(padSectionName, SettingName.RightThumbAxisX);
			ps.RightThumbAxisY = ini2.GetValue(padSectionName, SettingName.RightThumbAxisY);
			ps.RightThumbButton = ini2.GetValue(padSectionName, SettingName.RightThumbButton);
			ps.RightThumbDeadZoneX = ini2.GetValue(padSectionName, SettingName.RightThumbDeadZoneX);
			ps.RightThumbDeadZoneY = ini2.GetValue(padSectionName, SettingName.RightThumbDeadZoneY);
			ps.RightThumbLinearX = ini2.GetValue(padSectionName, SettingName.RightThumbLinearX);
			ps.RightThumbLinearY = ini2.GetValue(padSectionName, SettingName.RightThumbLinearY);
			ps.RightThumbDown = ini2.GetValue(padSectionName, SettingName.RightThumbDown);
			ps.RightThumbLeft = ini2.GetValue(padSectionName, SettingName.RightThumbLeft);
			ps.RightThumbRight = ini2.GetValue(padSectionName, SettingName.RightThumbRight);
			ps.RightThumbUp = ini2.GetValue(padSectionName, SettingName.RightThumbUp);
			ps.RightTrigger = ini2.GetValue(padSectionName, SettingName.RightTrigger);
			ps.RightTriggerDeadZone = ini2.GetValue(padSectionName, SettingName.RightTriggerDeadZone);
			// Axis to button dead-zones.
			ps.ButtonADeadZone = ini2.GetValue(padSectionName, SettingName.AxisToButtonADeadZone);
			ps.ButtonBDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToButtonBDeadZone);
			ps.ButtonBackDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToButtonBackDeadZone);
			ps.ButtonStartDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToButtonStartDeadZone);
			ps.ButtonXDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToButtonXDeadZone);
			ps.ButtonYDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToButtonYDeadZone);
			ps.LeftThumbButtonDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToLeftThumbButtonDeadZone);
			ps.RightThumbButtonDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToRightThumbButtonDeadZone);
			ps.LeftShoulderDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToLeftShoulderDeadZone);
			ps.RightShoulderDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToRightShoulderDeadZone);
			ps.DPadDownDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToDPadDownDeadZone);
			ps.DPadLeftDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToDPadLeftDeadZone);
			ps.DPadRightDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToDPadRightDeadZone);
			ps.DPadUpDeadZone = ini2.GetValue(padSectionName, SettingName.AxisToDPadUpDeadZone);
			return ps;
		}

		public void SetPadSetting(string padSectionName, DeviceInstance di)
		{
			var ini2 = new Ini(IniFileName);
			//ps.PadSettingChecksum = Guid.Empty;
			ini2.SetValue(padSectionName, SettingName.ProductName, di.ProductName);
			ini2.SetValue(padSectionName, SettingName.ProductGuid, di.ProductGuid.ToString());
			ini2.SetValue(padSectionName, SettingName.InstanceGuid, di.InstanceGuid.ToString());
		}

		/// <summary>
		/// Set INI settings from PadSetting object by device Instance GUID.
		/// </summary>
		/// <param name="instanceGuid">Instance GUID.</param>
		/// <returns>PadSettings object.</returns>
		public void SetPadSetting(string padSectionName, PadSetting ps)
		{
			var ini2 = new Ini(IniFileName);
			//ps.PadSettingChecksum = Guid.Empty;
			ini2.SetValue(padSectionName, SettingName.AxisToDPadDeadZone, ps.AxisToDPadDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToDPadEnabled, ps.AxisToDPadEnabled);
			ini2.SetValue(padSectionName, SettingName.AxisToDPadOffset, ps.AxisToDPadOffset);
			ini2.SetValue(padSectionName, SettingName.ButtonA, ps.ButtonA);
			ini2.SetValue(padSectionName, SettingName.ButtonB, ps.ButtonB);
			ini2.SetValue(padSectionName, SettingName.ButtonGuide, ps.ButtonGuide);
			ini2.SetValue(padSectionName, SettingName.ButtonBack, ps.ButtonBack);
			ini2.SetValue(padSectionName, SettingName.ButtonStart, ps.ButtonStart);
			ini2.SetValue(padSectionName, SettingName.ButtonX, ps.ButtonX);
			ini2.SetValue(padSectionName, SettingName.ButtonY, ps.ButtonY);
			ini2.SetValue(padSectionName, SettingName.DPad, ps.DPad);
			ini2.SetValue(padSectionName, SettingName.DPadDown, ps.DPadDown);
			ini2.SetValue(padSectionName, SettingName.DPadLeft, ps.DPadLeft);
			ini2.SetValue(padSectionName, SettingName.DPadRight, ps.DPadRight);
			ini2.SetValue(padSectionName, SettingName.DPadUp, ps.DPadUp);
			ini2.SetValue(padSectionName, SettingName.ForceEnable, ps.ForceEnable);
			ini2.SetValue(padSectionName, SettingName.ForceOverall, ps.ForceOverall);
			ini2.SetValue(padSectionName, SettingName.ForceSwapMotor, ps.ForceSwapMotor);
			ini2.SetValue(padSectionName, SettingName.ForceType, ps.ForceType);
			ini2.SetValue(padSectionName, SettingName.DeviceSubType, ps.GamePadType);
			ini2.SetValue(padSectionName, SettingName.LeftMotorPeriod, ps.LeftMotorPeriod);
			ini2.SetValue(padSectionName, SettingName.LeftMotorStrength, ps.LeftMotorStrength);
			ini2.SetValue(padSectionName, SettingName.LeftMotorDirection, ps.LeftMotorDirection);
			ini2.SetValue(padSectionName, SettingName.LeftShoulder, ps.LeftShoulder);
			ini2.SetValue(padSectionName, SettingName.LeftThumbAntiDeadZoneX, ps.LeftThumbAntiDeadZoneX);
			ini2.SetValue(padSectionName, SettingName.LeftThumbAntiDeadZoneY, ps.LeftThumbAntiDeadZoneY);
			ini2.SetValue(padSectionName, SettingName.LeftThumbLinearX, ps.LeftThumbLinearX);
			ini2.SetValue(padSectionName, SettingName.LeftThumbLinearY, ps.LeftThumbLinearY);
			ini2.SetValue(padSectionName, SettingName.LeftThumbAxisX, ps.LeftThumbAxisX);
			ini2.SetValue(padSectionName, SettingName.LeftThumbAxisY, ps.LeftThumbAxisY);
			ini2.SetValue(padSectionName, SettingName.LeftThumbButton, ps.LeftThumbButton);
			ini2.SetValue(padSectionName, SettingName.LeftThumbDeadZoneX, ps.LeftThumbDeadZoneX);
			ini2.SetValue(padSectionName, SettingName.LeftThumbDeadZoneY, ps.LeftThumbDeadZoneY);
			ini2.SetValue(padSectionName, SettingName.LeftThumbDown, ps.LeftThumbDown);
			ini2.SetValue(padSectionName, SettingName.LeftThumbLeft, ps.LeftThumbLeft);
			ini2.SetValue(padSectionName, SettingName.LeftThumbRight, ps.LeftThumbRight);
			ini2.SetValue(padSectionName, SettingName.LeftThumbUp, ps.LeftThumbUp);
			ini2.SetValue(padSectionName, SettingName.LeftTrigger, ps.LeftTrigger);
			ini2.SetValue(padSectionName, SettingName.LeftTriggerDeadZone, ps.LeftTriggerDeadZone);
			ini2.SetValue(padSectionName, SettingName.PassThrough, ps.PassThrough);
			ini2.SetValue(padSectionName, SettingName.RightMotorPeriod, ps.RightMotorPeriod);
			ini2.SetValue(padSectionName, SettingName.RightMotorStrength, ps.RightMotorStrength);
			ini2.SetValue(padSectionName, SettingName.RightMotorDirection, ps.RightMotorDirection);
			ini2.SetValue(padSectionName, SettingName.RightShoulder, ps.RightShoulder);
			ini2.SetValue(padSectionName, SettingName.RightThumbAntiDeadZoneX, ps.RightThumbAntiDeadZoneX);
			ini2.SetValue(padSectionName, SettingName.RightThumbAntiDeadZoneY, ps.RightThumbAntiDeadZoneY);
			ini2.SetValue(padSectionName, SettingName.RightThumbLinearX, ps.RightThumbLinearX);
			ini2.SetValue(padSectionName, SettingName.RightThumbLinearY, ps.RightThumbLinearY);
			ini2.SetValue(padSectionName, SettingName.RightThumbAxisX, ps.RightThumbAxisX);
			ini2.SetValue(padSectionName, SettingName.RightThumbAxisY, ps.RightThumbAxisY);
			ini2.SetValue(padSectionName, SettingName.RightThumbButton, ps.RightThumbButton);
			ini2.SetValue(padSectionName, SettingName.RightThumbDeadZoneX, ps.RightThumbDeadZoneX);
			ini2.SetValue(padSectionName, SettingName.RightThumbDeadZoneY, ps.RightThumbDeadZoneY);
			ini2.SetValue(padSectionName, SettingName.RightThumbDown, ps.RightThumbDown);
			ini2.SetValue(padSectionName, SettingName.RightThumbLeft, ps.RightThumbLeft);
			ini2.SetValue(padSectionName, SettingName.RightThumbRight, ps.RightThumbRight);
			ini2.SetValue(padSectionName, SettingName.RightThumbUp, ps.RightThumbUp);
			ini2.SetValue(padSectionName, SettingName.RightTrigger, ps.RightTrigger);
			ini2.SetValue(padSectionName, SettingName.RightTriggerDeadZone, ps.RightTriggerDeadZone);
			// Axis to button dead-zones.
			ini2.SetValue(padSectionName, SettingName.AxisToButtonADeadZone, ps.ButtonADeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToButtonBDeadZone, ps.ButtonBDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToButtonBackDeadZone, ps.ButtonBackDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToButtonStartDeadZone, ps.ButtonStartDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToButtonXDeadZone, ps.ButtonXDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToButtonYDeadZone, ps.ButtonYDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToLeftThumbButtonDeadZone, ps.LeftThumbButtonDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToRightThumbButtonDeadZone, ps.RightThumbButtonDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToLeftShoulderDeadZone, ps.LeftShoulderDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToRightShoulderDeadZone, ps.RightShoulderDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToDPadDownDeadZone, ps.DPadDownDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToDPadLeftDeadZone, ps.DPadLeftDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToDPadRightDeadZone, ps.DPadRightDeadZone);
			ini2.SetValue(padSectionName, SettingName.AxisToDPadUpDeadZone, ps.DPadUpDeadZone);
		}

		/// <summary>
		/// Read PAD settings from INI file to form.
		/// </summary>
		/// <param name="file">INI file name.</param>
		/// <param name="iniSection">Source INI pad section.</param>
		/// <param name="padIndex">Destination pad index.</param>
		public void ReadPadSettings(string file, string iniSection, int padIndex)
		{
			var ini2 = new Ini(file);
			var pad = string.Format("PAD{0}", padIndex + 1);
			foreach (string path in SettingsMap.Keys)
			{
				string section = path.Split('\\')[0];
				if (section != pad) continue;
				string key = path.Split('\\')[1];
				Control control = SettingsMap[path];
				string dstPath = string.Format("{0}\\{1}", pad, key);
				control = SettingsMap[dstPath];
				string v = ini2.GetValue(iniSection, key);
				ReadSettingTo(control, key, v);
			}
			loadCount++;
			if (ConfigLoaded != null) ConfigLoaded(this, new SettingEventArgs(ini2.File.Name, loadCount));
		}


		/// <summary>
		/// Clear Pad settings.
		/// </summary>
		/// <param name="file">INI file name.</param>
		/// <param name="iniSection">Source INI pad section.</param>
		/// <param name="padIndex">Destination pad index.</param>
		public void ClearPadSettings(int padIndex)
		{
			var pad = string.Format("PAD{0}", padIndex + 1);
			foreach (string path in SettingsMap.Keys)
			{
				string section = path.Split('\\')[0];
				if (section != pad) continue;
				string key = path.Split('\\')[1];
				Control control = SettingsMap[path];
				string dstPath = string.Format("{0}\\{1}", pad, key);
				control = SettingsMap[dstPath];
				ReadSettingTo(control, key, "");
			}
			loadCount++;
		}

		public void SetComboBoxValue(ComboBox cbx, string text)
		{
			// Remove value from other box.
			foreach (Control control in SettingsMap.Values)
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

		/// <summary>
		/// Save all setting values to INI file.
		/// </summary>
		/// <returns></returns>
		public bool SaveSettings()
		{
			var ini = new Ini(IniFileName);
			var saved = false;
			foreach (string path in SettingsMap.Keys)
			{
				var r = SaveSetting(ini, path);
				if (r) saved = true;
			}
			return saved;
		}

		public List<Control> DisabledControls = new List<Control>();

		/// <summary>
		/// Save control value to INI file.
		/// </summary>
		public bool SaveSetting(Control control)
		{
			if (DisabledControls.Contains(control)) return false;
			var ini = new Ini(IniFileName);
			var saved = false;
			foreach (string path in SettingsMap.Keys)
			{
				if (SettingsMap[path] == control)
				{
					var r = SaveSetting(ini, path, true);
					if (r) saved = r;
					break;
				}
			}
			return saved;
		}


		static Guid GetInstanceGuid(int padIndex)
		{
			string pad = string.Format("PAD{0}", padIndex + 1);
			string guidString = SettingManager.Current.SettingsMap[pad + "\\" + SettingName.InstanceGuid].Text;
			// If instanceGuid value is not a GUID then exit.
			if (!EngineHelper.IsGuid(guidString)) return Guid.Empty;
			Guid ig = new Guid(guidString);
			return ig;
		}

		static string GetInstanceSection(int padIndex)
		{
			var ig = GetInstanceGuid(padIndex);
			// If InstanceGuid value is empty then exit.
			if (ig.Equals(Guid.Empty)) return null;
			// Save settings to unique Instance section.
			return SettingManager.Current.GetInstanceSection(ig);
		}

		/// <summary>
		/// Save pad settings.
		/// </summary>
		/// <param name="padIndex">Source PAD index.</param>
		/// <param name="file">Destination INI file name.</param>
		/// <param name="iniSection">Destination INI section to save.</param>
		public bool SavePadSettings(int padIndex, string file)
		{
			var ini = new Ini(file);
			var saved = false;
			var pad = string.Format("PAD{0}", padIndex + 1);
			foreach (string path in SettingsMap.Keys)
			{
				string section = path.Split('\\')[0];
				// If this is not PAD section then skip.
				if (section != pad) continue;
				var r = SaveSetting(ini, path);
				if (r) saved = true;
			}
			return saved;
		}

		/// <summary>
		/// Save setting from windows form control to current INI file.
		/// </summary>
		/// <param name="path">path of parameter (related to actual control)</param>
		/// <param name="dstIniSection">if not null then section will be different inside INI file than specified in path</param>
		public bool SaveSetting(Ini ini, string path, bool single = false)
		{
			var control = SettingsMap[path];
			var section = path.Split('\\')[0];
			string key = path.Split('\\')[1];
			var padIndex = SettingName.GetPadIndex(path);
			string v = string.Empty;
			if (key == SettingName.HookMode ||
				key.EndsWith(SettingName.DeviceSubType) ||
				key.EndsWith(SettingName.ForceType) ||
				key.EndsWith(SettingName.LeftMotorDirection) ||
				key.EndsWith(SettingName.RightMotorDirection) ||
				key.EndsWith(SettingName.PassThroughIndex) ||
				key.EndsWith(SettingName.CombinedIndex))
			{
				var v1 = ((ComboBox)control).SelectedItem;
				if (v1 == null) { v = "0"; }
				else if (v1 is KeyValuePair) { v = ((KeyValuePair)v1).Value; }
				else { v = System.Convert.ToInt32(v1).ToString(); }
			}
			// If di menu strip attached.
			else if (control is ComboBox)
			{
				var cbx = (ComboBox)control;
				if (control.ContextMenuStrip == null)
				{
					v = control.Text;
				}
				else
				{
					v = new SettingsConverter(control.Text, key).ToIniValue();
					// make sure that disabled button value is "0".
					if (SettingName.IsButton(key) && string.IsNullOrEmpty(v)) v = "0";
				}
				// If mapping setting changed then...
				if (single && key.EndsWith(SettingName.MapToPad))
				{
					// Remember device which needs to be restored.
					MainForm.Current.AutoSelectControllerInstance = GetInstanceGuid(padIndex);
					MainForm.Current.ControllerIndex = padIndex;
				}
			}
			else if (control is TextBox)
			{
				// if setting is read-only.
				if (key == SettingName.InstanceGuid || key == SettingName.ProductGuid)
				{
					v = string.IsNullOrEmpty(control.Text) ? Guid.Empty.ToString("D") : control.Text;
				}
				else v = control.Text;
			}
			else if (control is ListBox)
			{
				var lbx = (ListBox)control;
				v = string.Join(",", lbx.Items.Cast<string>().ToArray());
			}
			else if (control is NumericUpDown)
			{
				NumericUpDown nud = (NumericUpDown)control;
				v = nud.Value.ToString();
			}
			else if (control is TrackBar)
			{
				TrackBar tc = (TrackBar)control;
				// convert 100%  to 256
				if (key == SettingName.AxisToDPadDeadZone || key == SettingName.AxisToDPadOffset || key == SettingName.LeftTriggerDeadZone || key == SettingName.RightTriggerDeadZone)
				{
					v = System.Convert.ToInt32((float)tc.Value / 100F * 256F).ToString();
				}
				// convert 100%  to 500
				else if (key == SettingName.LeftMotorPeriod || key == SettingName.RightMotorPeriod)
				{
					v = System.Convert.ToInt32((float)tc.Value / 100F * 500F).ToString();
				}
				// Convert 100% to 32767
				else if (key == SettingName.LeftThumbDeadZoneX || key == SettingName.LeftThumbDeadZoneY || key == SettingName.RightThumbDeadZoneX || key == SettingName.RightThumbDeadZoneY)
				{
					v = System.Convert.ToInt32((float)tc.Value / 100F * ((float)Int16.MaxValue)).ToString();
				}
				else v = tc.Value.ToString();
			}
			else if (control is CheckBox)
			{
				CheckBox tc = (CheckBox)control;
				v = tc.Checked ? "1" : "0";
			}
			if (SettingName.IsThumbAxis(key))
			{
				v = v.Replace(SettingName.SType.Axis, "");
			}
			// If this is DPad setting then remove prefix.
			if (key == SettingName.DPad) v = v.Replace(SettingName.SType.DPad, "");
			//if (v == "v1") v = "UP";
			//if (v == "v2") v = "RIGHT";
			//if (v == "v3") v = "DOWN";
			//if (v == "v4") v = "LEFT";
			//if (v == "")
			//{
			//	if (key == SettingName.DPadUp) v = "UP";
			//	if (key == SettingName.DPadDown) v = "DOWN";
			//	if (key == SettingName.DPadLeft) v = "LEFT";
			//	if (key == SettingName.DPadRight) v = "RIGHT";
			//}
			// add comment.
			//var l = SettingName.MaxNameLength - key.Length + 24;
			//v = string.Format("{0, -" + l + "} # {1} Default: '{2}'.", v, SettingName.GetDescription(key), SettingName.GetDefaultValue(key));
			// If this is PAD section then
			if (padIndex > -1)
			{
				section = GetInstanceSection(padIndex);
				// If destination section is empty because controller is not connected then skip.
				if (string.IsNullOrEmpty(section)) return false;
			}
			var oldValue = ini.GetValue(section, key);
			var saved = false;
			if (oldValue != v)
			{
				ini.SetValue(section, key, v);
				saveCount++;
				saved = true;
				if (ConfigSaved != null) ConfigSaved(this, new SettingEventArgs(IniFileName, saveCount));
			}
			// Flush XML too.
			Save();
			return saved;
		}

		public string GetInstanceSection(Guid instanceGuid)
		{
			return string.Format("IG_{0:N}", instanceGuid);
		}

		public string GetProductSection(Guid productGuid)
		{
			return string.Format("PG_{0:N}", productGuid);
		}


		public bool ContainsProductSection(Guid productGuid, string iniFileName, out string sectionName)
		{
			var ini2 = new Ini(iniFileName);
			var section = GetInstanceSection(productGuid);
			var contains = !string.IsNullOrEmpty(ini2.GetValue(section, "ProductGUID"));
			sectionName = contains ? section : null;
			return contains;
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
				if (string.IsNullOrEmpty(v)) continue;
				if (v.ToLower() == instanceGuid.ToString("D").ToLower())
				{
					sectionName = section;
					return true;
				}
			}
			sectionName = null;
			return false;
		}


		/// <summary>
		/// Check settings.
		/// </summary>
		/// <returns>True if settings changed.</returns>
		public bool CheckSettings(DeviceInstance[] diInstances, DeviceInstance[] diInstancesOld)
		{
			var updated = false;
			var ini2 = new Ini(IniFileName);
			var oldCount = diInstancesOld.Length;
			for (int i = 0; i < 4; i++)
			{
				var pad = string.Format("PAD{0}", i + 1);
				var section = "";
				var di = diInstances[i];
				// If direct Input instance is connected.
				if (di != null)
				{
					var ig = di.InstanceGuid;
					section = GetInstanceSection(ig);
					// If INI file contain settings for this device then...
					string sectionName = null;
					if (ContainsInstanceSection(ig, IniFileName, out sectionName))
					{
						var diOld = diInstancesOld[i];
						var samePosition = diOld != null && diOld.InstanceGuid.Equals(ig);
						// Load settings.
						if (!samePosition)
						{
							MainForm.Current.SuspendEvents();
							ReadPadSettings(IniFileName, section, i);
							MainForm.Current.ResumeEvents();
						}
					}
					else
					{
						MainForm.Current.MainTabControl.SelectedIndex = i;
						MainForm.Current.SuspendEvents();
						ClearPadSettings(i);
						MainForm.Current.ResumeEvents();
						var f = new NewDeviceForm();
						f.LoadData(di, i);
						f.StartPosition = FormStartPosition.CenterParent;
						var result = f.ShowDialog(MainForm.Current);
						f.Dispose();
						updated = (result == DialogResult.OK);
					}
				}
				else
				{
					MainForm.Current.SuspendEvents();
					ClearPadSettings(i);
					MainForm.Current.ResumeEvents();
				}
				// Update Mappings.
				ini2.SetValue(SettingName.Mappings, pad, section);
			}
			return updated;
		}

		#endregion // Instance Version
	}
}
