using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Security.Principal;
using Microsoft.DirectX.DirectInput;
using System.Security.AccessControl;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace x360ce.App
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		string dllFile0 = "xinput9_1_0.dll";
		string dllFile1 = "xinput1_1.dll";
		string dllFile2 = "xinput1_2.dll";
		string dllFile3 = "xinput1_3.dll";
		Version newVersion = new Version("3.2.0.295");
		// Will be set to default values.
		string dllFile;

		public int oldIndex;

		public int ControllerIndex
		{
			get
			{
				int newIndex = 0;
				if (MainTabControl.SelectedTab == Pad1TabPage) newIndex = 0;
				if (MainTabControl.SelectedTab == Pad2TabPage) newIndex = 1;
				if (MainTabControl.SelectedTab == Pad3TabPage) newIndex = 2;
				if (MainTabControl.SelectedTab == Pad4TabPage) newIndex = 3;
				return newIndex;
			}
		}

		public Controls.AboutControl ControlAbout;
		public Controls.PadControl[] ControlPads;
		public TabPage[] ControlPages;

		private void MainForm_Load(object sender, EventArgs e)
		{
			// init default
			DebugModeCheckBox_CheckedChanged(DebugModeCheckBox, null);
			// Fill FakeWmi ComboBox.
			var fakeModeOptions = new List<KeyValuePair>();
			var fakeModeTypes = (FakeMode[])Enum.GetValues(typeof(FakeMode));
			foreach (var item in fakeModeTypes) fakeModeOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
			FakeModeComboBox.DataSource = fakeModeOptions;
			FakeModeComboBox.DisplayMember = "Key";
			FakeModeComboBox.ValueMember = "Value";
			// Set status.
			StatusSaveLabel.Visible = false;
			StatusEventsLabel.Visible = false;

			// Set default cXinputFile.
			if (System.IO.File.Exists(dllFile3)) dllFile = dllFile3;
			else if (System.IO.File.Exists(dllFile2)) dllFile = dllFile2;
			else if (System.IO.File.Exists(dllFile1)) dllFile = dllFile1;
			else if (System.IO.File.Exists(dllFile0)) dllFile = dllFile0;
			else dllFile = dllFile3;
			// Init presets. Execute only after name of cIniFile is set.
			InitPresets();
			// Load about control.
			ControlAbout = new Controls.AboutControl();
			ControlAbout.Dock = DockStyle.Fill;
			AboutTabPage.Controls.Add(ControlAbout);
			// Load Tab pages.
			ControlPages = new TabPage[4];
			ControlPages[0] = Pad1TabPage;
			ControlPages[1] = Pad2TabPage;
			ControlPages[2] = Pad3TabPage;
			ControlPages[3] = Pad4TabPage;
			BuletImageList.Images.Add("bullet_square_glass_blue.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_blue.png")));
			BuletImageList.Images.Add("bullet_square_glass_green.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_green.png")));
			BuletImageList.Images.Add("bullet_square_glass_grey.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_grey.png")));
			BuletImageList.Images.Add("bullet_square_glass_red.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_red.png")));
			BuletImageList.Images.Add("bullet_square_glass_yellow.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_yellow.png")));
			foreach (var item in ControlPages) item.ImageKey = "bullet_square_glass_grey.png";
			// Load PAD controls.
			ControlPads = new Controls.PadControl[4];
			for (int i = 0; i < ControlPads.Length; i++)
			{
				ControlPads[i] = new Controls.PadControl(i);
				ControlPads[i].Name = string.Format("ControlPad{0}", i + 1);
				ControlPads[i].Dock = DockStyle.Fill;
				ControlPages[i].Controls.Add(ControlPads[i]);
				ControlPads[i].InitPadControl();
			}
			// Check if ini and dll is on disk.
			if (!CheckFiles(true)) return;
			// INI setting keys with controls.
			var sm = SettingManager.Current.SettingsMap;
			SettingManager.Current.ConfigSaved += new EventHandler<SettingEventArgs>(Current_ConfigSaved);
			SettingManager.Current.ConfigLoaded += new EventHandler<SettingEventArgs>(Current_ConfigLoaded);
			sm.Add(@"Options\" + SettingName.UseInitBeep, UseInitBeepCheckBox);
			sm.Add(@"Options\" + SettingName.DebugMode, DebugModeCheckBox);
			sm.Add(@"Options\" + SettingName.Log, EnableLoggingCheckBox);
			sm.Add(@"FakeAPI\" + SettingName.FakeMode, FakeModeComboBox);
			for (int i = 0; i < ControlPads.Length; i++)
			{
				var map = ControlPads[i].SettingsMap;
				foreach (var key in map.Keys) sm.Add(key, map[key]);
			}
			ReloadXinputSettings();
			Version v = new Version(Application.ProductVersion);
			this.Text = string.Format(this.Text, Application.ProductVersion);
			// Version = major.minor.build.revision
			switch (v.Build)
			{
				case 0: this.Text += " Alpha"; break;  // Alpha Release (AR)
				case 1: this.Text += " Beta 1"; break; // Master Beta (MB)
				case 2: this.Text += " Beta 2"; break; // Feature Complete (FC)
				case 3: this.Text += " Beta 3"; break; // Technical Preview (TP)
				case 4: this.Text += " RC"; break;     // Release Candidate (RC)
				// case 5: this.Text += " RTM"; break; // Release to Manufacturing (RTM)
				// case 6: this.Text += " GM"; break;  // General Availability (GA) / Gold
			}
			////InitDirectInputTab();
			//// Timer will execute ReloadXInputLibrary();
			////XInput.ReLoadLibrary(cXinput3File);
			////XInput.ReLoadLibrary(cXinput3File);
			//// start capture events.
			if (Win32.WinAPI.IsVista && Win32.WinAPI.IsElevated && Win32.WinAPI.IsInAdministratorRole) this.Text += " (Administrator)";
			UpdateTimer.Start();
			////ReloadXInputLibrary();
		}

		void Current_ConfigSaved(object sender, SettingEventArgs e)
		{
			StatusSaveLabel.Text = string.Format("S {0}", e.Count);
		}

		void Current_ConfigLoaded(object sender, SettingEventArgs e)
		{
			toolStripStatusLabel1.Text = string.Format("'{0}' loaded.", e.Name);
		}

		public void CopyElevated(string source, string dest)
		{
			if (!Win32.WinAPI.IsVista)
			{
				System.IO.File.Copy(source, dest);
				return;
			}
			var di = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(dest));
			var security = di.GetAccessControl();
			var fi = new FileInfo(dest);
			var fileSecurity = fi.GetAccessControl();
			// Allow Users to Write.
			//SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			//fileSecurity.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.Write, AccessControlType.Allow));
			//fi.SetAccessControl(fileSecurity);
			var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
			string message = string.Empty;
			foreach (var myacc in rules)
			{
				var acc = (FileSystemAccessRule)myacc;
				message += string.Format("IdentityReference: {0}\r\n", acc.IdentityReference.Value);
				message += string.Format("Access Control Type: {0}\r\n", acc.AccessControlType.ToString());
				message += string.Format("\t{0}\r\n", acc.FileSystemRights.ToString());
				//if ((acc.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl)
				//{
				//    Console.Write("FullControl");
				//}
				//if ((acc.FileSystemRights & FileSystemRights.ReadData) == FileSystemRights.ReadData)
				//{
				//    Console.Write("ReadData");
				//}
				//if ((acc.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData)
				//{
				//    Console.Write("WriteData");
				//}
				//if ((acc.FileSystemRights & FileSystemRights.ListDirectory) == FileSystemRights.ListDirectory)
				//{
				//    Console.Write("ListDirectory");
				//}
				//if ((acc.FileSystemRights & FileSystemRights.ExecuteFile) == FileSystemRights.ExecuteFile)
				//{
				//    Console.Write("ExecuteFile");
				//}
			}
			MessageBox.Show(message);
			//WindowsIdentity self = System.Security.Principal.WindowsIdentity.GetCurrent();
			//			 FileSystemAccessRule rule = new FileSystemAccessRule(
			//    self.Name, 
			//    FileSystemRights.FullControl,
			//    AccessControlType.Allow);
		}


		void InitPresets()
		{
			PresetComboBox.Items.Clear();
			var prefix = System.IO.Path.GetFileNameWithoutExtension(SettingManager.Current.iniFile);
			var ext = System.IO.Path.GetExtension(SettingManager.Current.iniFile);
			string name;
			// Presets: Embedded.
			var embeddedPresets = new List<string>();
			var assembly = Assembly.GetExecutingAssembly();
			string[] files = assembly.GetManifestResourceNames();
			var pattern = string.Format("Presets\\.{0}\\.(?<name>.*?){1}", prefix, ext);
			Regex rx = new Regex(pattern);
			for (int i = 0; i < files.Length; i++)
			{
				if (rx.IsMatch(files[i]))
				{
					name = rx.Match(files[i]).Groups["name"].Value.Replace("_", " ");
					embeddedPresets.Add(name);
				}
			}
			// Presets: Custom.
			var dir = new System.IO.DirectoryInfo(".");
			FileInfo[] fis = dir.GetFiles(string.Format("{0}.*{1}", prefix, ext));
			List<string> customPresets = new List<string>();
			for (int i = 0; i < fis.Length; i++)
			{
				name = fis[i].Name.Substring(prefix.Length + 1);
				name = name.Substring(0, name.Length - ext.Length);
				name = name.Replace("_", " ");
				if (!embeddedPresets.Contains(name)) customPresets.Add(name);
			}
			PresetComboBox.Items.Add("Presets:");
			string[] cNames = customPresets.ToArray();
			string[] eNames = embeddedPresets.ToArray();
			Array.Sort(cNames);
			Array.Sort(eNames);
			foreach (var item in cNames) PresetComboBox.Items.Add(item);
			if (cNames.Length > 0) PresetComboBox.Items.Add("Embeded:");
			foreach (var item in eNames) PresetComboBox.Items.Add(item);
			PresetComboBox.SelectedIndex = 0;
		}

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			for (int i = 0; i < ControlPads.Length; i++)
			{
				// Stop recording on all panels.
				if (e.KeyCode == System.Windows.Forms.Keys.Escape && ControlPads[i].Recording)
				{
					ControlPads[i].RecordingStop(null);
					e.Handled = true;
					e.SuppressKeyPress = true;
				}
			}
			toolStripStatusLabel1.Text = "";
		}

		private void CleanStatusTimer_Tick(object sender, EventArgs e)
		{
			toolStripStatusLabel1.Text = "";
			CleanStatusTimer.Stop();
		}

		private void LoadPresetButton_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(PresetComboBox.Text)) return;
			string name = PresetComboBox.Text.Replace(" ", "_");
			LoadPreset(name);
		}

		#region Setting Events

		void LoadPreset(string name)
		{
			// exit if "Presets:" or "Embedded:".
			if (name.Contains(":")) return;
			var prefix = System.IO.Path.GetFileNameWithoutExtension(SettingManager.Current.iniFile);
			var ext = System.IO.Path.GetExtension(SettingManager.Current.iniFile);
			string resourceName = string.Format("{0}.{1}{2}", prefix, name, ext);
			var resource = Helper.GetResource("Presets." + resourceName);
			// If internal preset was found.
			if (resource != null)
			{
				// Export file.
				var sr = new StreamReader(resource);
				System.IO.File.WriteAllText(resourceName, sr.ReadToEnd());
			}
			SuspendEvents();
			// preset will be stored in inside [PAD1] section;
			SettingManager.Current.ReadPadSettings(resourceName, "PAD1", ControllerIndex);
			ResumeEvents();
			// Save setting and notify if vaue changed.
			if (SettingManager.Current.SaveSettings()) NotifySettingsChange();
			// remove file if it was from resource.
			if (resource != null) System.IO.File.Delete(resourceName);
			//CleanStatusTimer.Start();
		}

		private void ResetButton_Click(object sender, EventArgs e)
		{
			ReloadXinputSettings();
		}

		int resumed = 0;
		int suspended = 0;

		public void SuspendEvents()
		{
			StatusEventsLabel.Text = "OFF...";
			// Don't allow controls to fire events.
			foreach (var control in SettingManager.Current.SettingsMap.Values)
			{
				if (control is TrackBar) ((TrackBar)control).ValueChanged -= new EventHandler(Control_ValueChanged);
				if (control is CheckBox) ((CheckBox)control).CheckedChanged -= new EventHandler(Control_CheckedChanged);
				if (control is ComboBox) ((ComboBox)control).SelectedIndexChanged -= new EventHandler(this.Control_TextChanged);
				if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged -= new EventHandler(Control_ValueChanged);
				if (control is ComboBox || control is TextBox) control.TextChanged -= new System.EventHandler(this.Control_TextChanged);
			}
			suspended++;
			StatusEventsLabel.Text = string.Format("OFF {0} {1}", suspended, resumed);
		}

		public void ResumeEvents()
		{
			StatusEventsLabel.Text = "ON...";
			// Allow controls to fire events.
			foreach (var control in SettingManager.Current.SettingsMap.Values)
			{
				if (control is TrackBar) ((TrackBar)control).ValueChanged += new EventHandler(Control_ValueChanged);
				if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged += new EventHandler(Control_ValueChanged);
				if (control is CheckBox) ((CheckBox)control).CheckedChanged += new EventHandler(Control_CheckedChanged);
				if (control is ComboBox) ((ComboBox)control).SelectedIndexChanged += new EventHandler(this.Control_TextChanged);
				if (control is ComboBox || control is TextBox) control.TextChanged += new System.EventHandler(this.Control_TextChanged);
			}
			resumed++;
			StatusEventsLabel.Text = string.Format("ON {0} {1}", suspended, resumed);
		}

		/// <summary>
		/// Delay settings trough timer so interface will be more responsive on TrackBars.
		/// Or fast changes. Library will be reloaded as soon as user calms down (no setting changes in 500ms).
		/// </summary>
		public void NotifySettingsChange()
		{
			UpdateTimer.Stop();
			SettingsTimer.Stop();
			// Timer will be started inside Settings timer.
			SettingsTimer.Start();
		}

		private void Control_TextChanged(object sender, EventArgs e)
		{
			// Save setting and notify if vaue changed.
			if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
		}

		private void Control_ValueChanged(object sender, EventArgs e)
		{
			// Save setting and notify if vaue changed.
			if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
		}

		private void Control_CheckedChanged(object sender, EventArgs e)
		{
			// Save setting and notify if vaue changed.
			if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
		}

		void ReloadXinputSettings()
		{
			SuspendEvents();
			SettingManager.Current.ReadSettings();
			ResumeEvents();
		}

		#endregion


		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			UpdateTimer.Stop();
			// Disable force feedback effect before closing app.
			try
			{
				for (int i = 0; i < 4; i++)
				{
					if (ControlPads[i].LeftMotorTestTrackBar.Value > 0 || ControlPads[i].RightMotorTestTrackBar.Value > 0)
						GamePad.SetVibration((PlayerIndex)i, 0, 0);
				}
				UnsafeNativeMethods.FreeLibrary();
				System.Threading.Thread.Sleep(100);

			}
			catch (Exception) { }
			FileInfo tmp = new FileInfo(SettingManager.Current.iniTmpFile);
			if (tmp.Exists)
			{
				// Rename temp to ini.
				tmp.CopyTo(SettingManager.Current.iniFile, true);
				// delete temp.
				tmp.Delete();
			}
		}

		private void SaveButton_Click(object sender, EventArgs e)
		{
			UpdateTimer.Stop();
			// store unique instance settings.
			for (int i = 0; i < 4; i++)
			{
				string guidString = SettingManager.Current.SettingsMap[string.Format("PAD{0}\\" + SettingName.InstanceGuid, i + 1)].Text;
				if (!Helper.IsGuid(guidString)) continue;
				Guid ig = new Guid(guidString);
				if (ig.Equals(Guid.Empty)) continue;
				string section = string.Format("IG_{0}", ig.ToString("N"));
				SettingManager.Current.SavePadSettings(SettingManager.Current.iniFile, section, i);
			}
			// Owerwrite temp file.
			FileInfo ini = new FileInfo(SettingManager.Current.iniFile);
			ini.CopyTo(SettingManager.Current.iniTmpFile, true);
			toolStripStatusLabel1.Text = "Settings saved";
			UpdateTimer.Start();
		}

		#region Timer

		List<DeviceInstance> _diInstances;
		List<DeviceInstance> diInstances
		{
			get { return _diInstances = _diInstances ?? new List<DeviceInstance>(); }
			set { _diInstances = value; }
		}

		/// <summary>
		/// Access this only insite Timer_Click!
		/// </summary>
		List<DeviceInstance> GetCurrentInstances(ref bool instancesChanged)
		{
			//Populate All devices
			var list = new List<DeviceInstance>();
			var dl = Manager.Devices.GetEnumerator();
			while (dl.MoveNext())
			{
				var di = (DeviceInstance)dl.Current;
				if (di.DeviceType == DeviceType.Driving
					|| di.DeviceType == DeviceType.Flight
					|| di.DeviceType == DeviceType.Gamepad
					|| di.DeviceType == DeviceType.Joystick
				) list.Add(di);
			}
			instancesChanged = false;
			if (diInstances.Count != list.Count)
			{
				instancesChanged = true;
			}
			else
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (!diInstances[i].InstanceGuid.Equals(list[i].InstanceGuid))
					{
						instancesChanged = true;
					}
				}
			}
			diInstances = list;
			return diInstances;
		}

		Device _CurrentDiDevice;
		/// <summary>
		/// Access this only insite Timer_Click!
		/// </summary>
		Device GetCurrentDiDevice(List<DeviceInstance> instances)
		{
			// Assign instance guid (empty guid is instance is unavailable)
			Guid ig = ControllerIndex < instances.Count
				? instances[ControllerIndex].InstanceGuid
				: Guid.Empty;
			// Current is not empty and instance is different or empty then...
			if (_CurrentDiDevice != null &&
				(!_CurrentDiDevice.DeviceInformation.InstanceGuid.Equals(ig) || Guid.Empty.Equals(ig)))
			{
				// Dispose current device.
				_CurrentDiDevice.Unacquire();
				_CurrentDiDevice.Dispose();
				_CurrentDiDevice = null;
			}
			// If current device is empty and instance guid sugested then...
			if (_CurrentDiDevice == null && !ig.Equals(Guid.Empty))
			{
				_CurrentDiDevice = new Device(ig);
				_CurrentDiDevice.SetCooperativeLevel(this, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
				_CurrentDiDevice.Acquire();
			}
			//if (!attached) return null;
			return _CurrentDiDevice;
		}

		private void SettingsTimer_Tick(object sender, EventArgs e)
		{
			settingsChanged = true;
			SettingsTimer.Stop();
			UpdateTimer.Start();
		}

		bool settingsChanged = false;

		private void UpdateTimer_Tick(object sender, EventArgs e)
		{
			Program.TimerCount++;
			bool instancesChanged = false;
			if (settingsChanged)
			{
				Program.ReloadCount++;
				//FixConfig(instances);
				settingsChanged = false;
				ReloadLibrary();
				return;
			}
			var instances = new List<DeviceInstance>();
			try { instances = GetCurrentInstances(ref instancesChanged); }
			catch (Exception)
			{
				// Update GamePad state LED's (disable since instances list is empty).
				UpdateGamePadStatus(instances);
				throw;
			}
			if (instancesChanged)
			{
				Program.ReloadCount++;
				//FixConfig(instances);
				settingsChanged = false;
				ReloadLibrary();
				return;
			}
			if (instances.Count > 0)
			{
				// Check all pads.
				UpdateGamePadStatus(instances);
				var currentPadControl = ControlPads[ControllerIndex];
				var currentDevice = GetCurrentDiDevice(instances);
				currentPadControl.UpdateFromDirectInput(currentDevice);
				var currentPad = GamePad.GetState((PlayerIndex)ControllerIndex);
				currentPadControl.UpdateFromXInput(currentPad);
			}
			UpdateStatus("");
		}

		Version _dllVersion;
		Version dllVersion
		{
			get {
				if (_dllVersion != null) return _dllVersion;
				_dllVersion = GetDllVersion(dllFile);
				return _dllVersion;
			}
			set { _dllVersion = value; }
		}


		private Version GetDllVersion(string fileName)
		{
			var dllInfo = new System.IO.FileInfo(fileName);
			if (dllInfo.Exists)
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllInfo.FullName);
				return vi.FileVersion == null ? new Version("0.0.0.0") : new Version(vi.FileVersion);
			}
			return new Version("0.0.0.0");
		}

		public void ReloadLibrary()
		{
			var dllInfo = new System.IO.FileInfo(dllFile);
			if (dllInfo.Exists)
			{
				dllVersion = GetDllVersion(dllInfo.FullName);
				StatusDllLabel.Text = dllFile + " " + dllVersion.ToString();
				UnsafeNativeMethods.ReLoadLibrary(dllFile);
			}
		}

		public void UpdateStatus(string message)
		{
			toolStripStatusLabel1.Text = string.Format("Count: {0}, Reloads: {1}, Errors: {2} {3}",
				Program.TimerCount, Program.ReloadCount, Program.ErrorCount, message);
		}

		/// <summary>
		/// Update GamePad state LED's.
		/// </summary>
		/// <param name="instances"></param>
		void UpdateGamePadStatus(List<DeviceInstance> instances)
		{
			for (int i = 0; i < 4; i++)
			{
				//XInput.Controllers[i].PollState();
				var on = GamePad.GetState((PlayerIndex)i).IsConnected;
				string image = on ? "green" : "grey";
				// If DirectInput device exist but controller doesn't work then error.
				if (i < instances.Count) if (!on) image = "red";
				string bullet = string.Format("bullet_square_glass_{0}.png", image);
				if (ControlPages[i].ImageKey != bullet) ControlPages[i].ImageKey = bullet;
			}
		}

		void FixConfig(List<DeviceInstance> instances)
		{

			// Fix INI File.
			var ini = new Ini(SettingManager.Current.iniFile);
			for (int i = 0; i < instances.Count; i++)
			{
				string curInstance = instances[i].InstanceGuid.ToString("B").ToLower();
				string padInstance = ini.GetValue(string.Format("PAD{0}", i + 1), SettingName.InstanceGuid).ToLower();
				if (curInstance != padInstance)
				{
					SettingManager.Current.ReadPadSettings(SettingManager.Current.iniFile, "IG_" + instances[i].InstanceGuid.ToString("N"), i);
					SettingManager.Current.SavePadSettings(SettingManager.Current.iniFile, string.Format("PAD{0}", i + 1), i);
				}
			}
			for (int i = instances.Count; i < 4; i++)
			{
				string curInstance = Guid.Empty.ToString("B").ToLower();
				string padInstance = ini.GetValue(string.Format("PAD{0}", i + 1), "Instance").ToLower();
				if (curInstance != padInstance)
				{
					SettingManager.Current.ReadPadSettings(SettingManager.Current.iniFile, "IG_" + Guid.Empty.ToString("N"), i);
					SettingManager.Current.SavePadSettings(SettingManager.Current.iniFile, string.Format("PAD{0}", i + 1), i);
				}
			}
		}

		#endregion

		bool HelpInit = false;

		private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (MainTabControl.SelectedTab == HelpTabPage && !HelpInit)
			{
				// Move this here so interface will load one second faster.
				HelpInit = true;
				var stream = Helper.GetResource("Documents.Help.htm");
				var sr = new StreamReader(stream);
				NameValueCollection list = new NameValueCollection();
				list.Add("font-name-default", "'Microsoft Sans Serif'");
				list.Add("font-size-default", "16");
				HelpRichTextBox.Rtf = Html2Rtf.Converter.Html2Rtf(sr.ReadToEnd(), list);
				HelpRichTextBox.SelectAll();
				HelpRichTextBox.SelectionIndent = 8;
				HelpRichTextBox.SelectionRightIndent = 8;
				HelpRichTextBox.DeselectAll();
			}
		}

		private void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var cbx = (CheckBox)sender;
			if (!cbx.Checked) Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
			else Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
		}

		#region Check Files

		bool CheckFiles(bool createIfNotExist)
		{
			InstallFilesX360ceCheckBox.Checked = System.IO.File.Exists(SettingManager.Current.iniFile);
			InstallFilesXinput13CheckBox.Checked = System.IO.File.Exists(dllFile3);
			InstallFilesXinput12CheckBox.Checked = System.IO.File.Exists(dllFile2);
			InstallFilesXinput11CheckBox.Checked = System.IO.File.Exists(dllFile1);
			InstallFilesXinput910CheckBox.Checked = System.IO.File.Exists(dllFile0);
			InstallFilesX360ceCheckBox.Enabled = IsFileSame(SettingManager.Current.iniFile);
			InstallFilesXinput910CheckBox.Enabled = IsFileSame(dllFile0);
			InstallFilesXinput11CheckBox.Enabled = IsFileSame(dllFile1);
			InstallFilesXinput12CheckBox.Enabled = IsFileSame(dllFile2);
			InstallFilesXinput13CheckBox.Enabled = IsFileSame(dllFile3);
			if (createIfNotExist)
			{
				// If ini file doesn't exists.
				if (!System.IO.File.Exists(SettingManager.Current.iniFile))
				{
					if (!CreateFile(SettingManager.Current.iniFile, null)) return false;
				}
				// If xinput file doesn't exists.
				if (!System.IO.File.Exists(dllFile))
				{
					if (!CreateFile(dllFile, null)) return false;
				}
				else if (dllVersion < newVersion)
				{
					CreateFile(dllFile, newVersion);
					return true;
				}
			}
			// Can't run witout ini.
			if (!File.Exists(SettingManager.Current.iniFile))
			{
				MessageBox.Show(
					string.Format("Configuration file '{0}' is required for application to run!", SettingManager.Current.iniFile),
					"Error", MessageBoxButtons.OK);
				this.Close();
				return false;
			}
			// If temp file exist then.
			FileInfo iniTmp = new FileInfo(SettingManager.Current.iniTmpFile);
			if (iniTmp.Exists)
			{
				// It means that application crashed. Restore ini from temp.
				if (!CopyFile(iniTmp.FullName, SettingManager.Current.iniFile)) return false;
			}
			else
			{
				// Create temp file to store original settings.
				if (!CopyFile(SettingManager.Current.iniFile, SettingManager.Current.iniTmpFile)) return false;
			}
			// Set status labels.
			StatusIsAdminLabel.Text = Win32.WinAPI.IsVista
				? string.Format("Elevated: {0}", Win32.WinAPI.IsElevated)
				: "";
			StatusIniLabel.Text = SettingManager.Current.iniFile;
			return true;
		}

		bool IsFileSame(string fileName)
		{
			return false;
			//if (!System.IO.File.Exists(fileName)) return false;
			//var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			//StreamReader sr;
			//// Get MD5 of file on the disk.
			//sr = new StreamReader(fileName);
			//var dMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
			//// Get MD5 of resource file.
			//if (fileName == dllFile0) fileName = dllFile;
			//if (fileName == dllFile1) fileName = dllFile;
			//if (fileName == dllFile2) fileName = dllFile;
			//if (fileName == dllFile3) fileName = dllFile;
			//var assembly = Assembly.GetExecutingAssembly();
			//sr = new StreamReader(assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName));
			//var rMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
			//// return result.
			//return rMd5.Equals(dMd5);
		}

		bool CopyFile(string sourceFileName, string destFileName)
		{
			try
			{
				File.Copy(sourceFileName, destFileName, true);
			}
			catch (Exception)
			{
				Elevate();
				return false;
			}
			return true;
		}

		bool CreateFile(string fileName, Version newVersion)
		{
			DialogResult answer;
			if (newVersion == null)
			{
				answer = MessageBox.Show(
					string.Format("'{0}' was not found. This file is required for emulator to function properly.\r\n\r\nDo you want to create this file?", new System.IO.FileInfo(fileName).FullName),
					string.Format("'{0}' was not found.", fileName),
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
			}
			else
			{
				answer = MessageBox.Show(
					string.Format("New version of this file is available:\r\n{0}\r\n\r\nOld version: {1}\r\nNew version: {2}\r\n\r\nDo you want to update this file?",  new System.IO.FileInfo(fileName).FullName, dllVersion, newVersion),
					string.Format("New version of '{0}' file is available.", fileName),
					MessageBoxButtons.YesNo, MessageBoxIcon.Information);
			}
			if (answer == DialogResult.Yes)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var sr = assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName);
				FileStream sw = null;
				try
				{
					sw = new FileStream(fileName, FileMode.Create, FileAccess.Write);
				}
				catch (Exception)
				{
					Elevate();
					return false;
				}
				var buffer = new byte[1024];
				while (true)
				{
					var count = sr.Read(buffer, 0, buffer.Length);
					if (count == 0) break;
					sw.Write(buffer, 0, count);
				}
				sr.Close();
				sw.Close();
			}
			return true;
		}

		void Elevate()
		{
			// If this is Vista/7 and is not elevated then elevate.
			if (x360ce.App.Win32.WinAPI.IsVista && !x360ce.App.Win32.WinAPI.IsElevated) x360ce.App.Win32.WinAPI.RunElevated();
		}

		#endregion

		private void XInputEnableCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UnsafeNativeMethods.Enable(XInputEnableCheckBox.Checked);
		}


	}
}
