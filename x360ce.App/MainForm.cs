using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Win32;
using x360ce.App.Properties;
using System.ComponentModel;
using JocysCom.ClassLibrary.IO;

namespace x360ce.App
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		DeviceDetector detector;

		public static MainForm Current { get; set; }

		public int oldIndex;

		public int ControllerIndex
		{
			get
			{
				int newIndex = -1;
				if (MainTabControl.SelectedTab == Pad1TabPage) newIndex = 0;
				if (MainTabControl.SelectedTab == Pad2TabPage) newIndex = 1;
				if (MainTabControl.SelectedTab == Pad3TabPage) newIndex = 2;
				if (MainTabControl.SelectedTab == Pad4TabPage) newIndex = 3;
				return newIndex;
			}
			set
			{
				switch (value)
				{
					case 0: MainTabControl.SelectedTab = Pad1TabPage; break;
					case 1: MainTabControl.SelectedTab = Pad2TabPage; break;
					case 2: MainTabControl.SelectedTab = Pad3TabPage; break;
					case 3: MainTabControl.SelectedTab = Pad4TabPage; break;
				}
			}
		}

		public AboutControl ControlAbout;
		public PadControl[] ControlPads;
		public TabPage[] ControlPages;

		public System.Timers.Timer UpdateTimer;
		public System.Timers.Timer SettingsTimer;
		public System.Timers.Timer CleanStatusTimer;
		public int DefaultPoolingInterval = 50;

		public Controller[] GamePads = new Controller[4];

		void MainForm_Load(object sender, EventArgs e)
		{
			if (IsDesignMode) return;
			SettingManager.Settings.Load();
			SettingManager.Summaries.Load();
			SettingManager.Summaries.Items.ListChanged += Summaries_ListChanged;
			// Make sure that data will be filtered before loading.
			// Note: Make sure to load Programs before Games.
			SettingManager.Programs.FilterList = Programs_FilterList;
			SettingManager.Programs.Load();
			// Make sure that data will be filtered before loading.
			SettingManager.Games.FilterList = Games_FilterList;
			SettingManager.Games.Load();


			for (int i = 0; i < 4; i++)
			{
				GamePads[i] = new Controller((UserIndex)i);
			}
			GameToCustomizeComboBox.DataSource = SettingManager.Games.Items;
			GameToCustomizeComboBox.DisplayMember = "DisplayName";
			UpdateTimer = new System.Timers.Timer();
			UpdateTimer.AutoReset = false;
			UpdateTimer.SynchronizingObject = this;
			UpdateTimer.Interval = DefaultPoolingInterval;
			UpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(UpdateTimer_Elapsed);
			SettingsTimer = new System.Timers.Timer();
			SettingsTimer.AutoReset = false;
			SettingsTimer.SynchronizingObject = this;
			SettingsTimer.Interval = 500;
			SettingsTimer.Elapsed += new System.Timers.ElapsedEventHandler(SettingsTimer_Elapsed);
			CleanStatusTimer = new System.Timers.Timer();
			CleanStatusTimer.AutoReset = false;
			CleanStatusTimer.SynchronizingObject = this;
			CleanStatusTimer.Interval = 3000;
			CleanStatusTimer.Elapsed += new System.Timers.ElapsedEventHandler(CleanStatusTimer_Elapsed);
			Text = EngineHelper.GetProductFullName();
			SetMinimizeToTray(Settings.Default.MinimizeToTray);
			// Start Timers.
			UpdateTimer.Start();
		}

		IList<Engine.Data.Program> Programs_FilterList(IList<Engine.Data.Program> items)
		{
			// Make sure default settings have unique by file name.
			var distinctItems = items
				.GroupBy(p => p.FileName.ToLower())
				.Select(g => g.First())
				.ToList();
			return distinctItems;
		}

		IList<Engine.Data.Game> Games_FilterList(IList<Engine.Data.Game> items)
		{
			// Make sure default settings have unique by file name.
			var distinctItems = items
				.GroupBy(p => p.FileName.ToLower())
				.Select(g => g.First())
				.ToList();

			// Check if current app doesn't exist in the list then...
			var appFile = new FileInfo(Application.ExecutablePath);
			var appItem = distinctItems.FirstOrDefault(x => x.FileName.ToLower() == appFile.Name.ToLower());
			if (appItem == null)
			{
				// Add x360ce.exe
				var item = x360ce.Engine.Data.Game.FromDisk(appFile.Name);
				var program = SettingManager.Programs.Items.FirstOrDefault(x => x.FileName.ToLower() == appFile.Name.ToLower());
				item.LoadDefault(program);
				distinctItems.Add(item);
			}
			else
			{
				appItem.FullPath = appFile.FullName;
			}
			return distinctItems;
		}

		private void Summaries_ListChanged(object sender, ListChangedEventArgs e)
		{
			// If map to changed then re-detect devices.
			var pd = e.PropertyDescriptor;
			if (pd != null && pd.Name == "MapTo")
			{
				forceRecountDevices = true;
			}
		}

		internal bool IsDesignMode
		{
			get
			{
				if (DesignMode) return true;
				if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return true;
				var pa = this.ParentForm;
				if (pa != null && pa.GetType().FullName.Contains("VisualStudio")) return true;
				return false;
			}
		}

		void detector_DeviceChanged(object sender, DeviceDetectorEventArgs e)
		{
			forceRecountDevices = true;
		}

		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		void UpdateSettingsMap()
		{
			// INI setting keys with controls.
			SettingManager.Current.ConfigSaved += new EventHandler<SettingEventArgs>(Current_ConfigSaved);
			SettingManager.Current.ConfigLoaded += new EventHandler<SettingEventArgs>(Current_ConfigLoaded);
			OptionsPanel.InitSettingsManager();
			var sm = SettingManager.Current.SettingsMap;
			for (int i = 0; i < ControlPads.Length; i++)
			{
				var map = ControlPads[i].SettingsMap;
				foreach (var key in map.Keys) sm.Add(key, map[key]);
			}
		}

		void Current_ConfigSaved(object sender, SettingEventArgs e)
		{
			StatusSaveLabel.Text = string.Format("S {0}", e.Count);
		}

		void Current_ConfigLoaded(object sender, SettingEventArgs e)
		{
			StatusTimerLabel.Text = string.Format("'{0}' loaded.", e.Name);
		}

		public void CopyElevated(string source, string dest)
		{
			if (!WinAPI.IsVista)
			{
				File.Copy(source, dest);
				return;
			}
			var di = new DirectoryInfo(System.IO.Path.GetDirectoryName(dest));
			var security = di.GetAccessControl();
			var fi = new FileInfo(dest);
			var fileSecurity = fi.GetAccessControl();
			// Allow Users to Write.
			//SecurityIdentifier SID = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
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

		void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			for (int i = 0; i < ControlPads.Length; i++)
			{
				// If Escape key was pressed while recording then...
				if (e.KeyCode == Keys.Escape)
				{
					var recordingWasStopped = ControlPads[i].StopRecording();
					if (recordingWasStopped)
					{
						e.Handled = true;
						e.SuppressKeyPress = true;
					};
				}
			}
			StatusTimerLabel.Text = "";
		}

		void CleanStatusTimer_Elapsed(object sender, EventArgs e)
		{
			if (Program.IsClosing) return;
			StatusTimerLabel.Text = "";
		}

		#region Setting Events

		public void LoadPreset(string name, int index)
		{
			// exit if "Presets:" or "Embedded:".
			if (name.Contains(":")) return;
			var prefix = Path.GetFileNameWithoutExtension(SettingManager.IniFileName);
			var ext = Path.GetExtension(SettingManager.IniFileName);
			string resourceName = string.Format("{0}.{1}{2}", prefix, name, ext);
			var resource = EngineHelper.GetResource("Presets." + resourceName);
			// If internal preset was found.
			if (resource != null)
			{
				// Export file.
				var sr = new StreamReader(resource);
				File.WriteAllText(resourceName, sr.ReadToEnd());
			}
			SuspendEvents();
			// Preset will be stored in inside [PAD1] section;
			SettingManager.Current.ReadPadSettings(resourceName, "PAD1", index);
			ResumeEvents();
			// Save setting and notify if value changed.
			if (SettingManager.Current.SaveSettings()) NotifySettingsChange();
			// remove file if it was from resource.
			if (resource != null) File.Delete(resourceName);
			//CleanStatusTimer.Start();
		}

		int resumed = 0;
		int suspended = 0;
		object eventsLock = new object();
		object eventsEnabled = false;

		public void SuspendEvents()
		{
			lock (eventsLock)
			{
				StatusEventsLabel.Text = "OFF...";
				// Don't allow controls to fire events.
				foreach (var control in SettingManager.Current.SettingsMap.Values)
				{
					if (control is TrackBar) ((TrackBar)control).ValueChanged -= new EventHandler(Control_ValueChanged);
					if (control is ListBox) ((ListBox)control).SelectedIndexChanged -= new EventHandler(Control_SelectedIndexChanged);
					if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged -= new EventHandler(Control_ValueChanged);
					if (control is CheckBox) ((CheckBox)control).CheckedChanged -= new EventHandler(Control_CheckedChanged);
					if (control is ComboBox)
					{
						var cbx = (ComboBox)control;
						if (cbx.DropDownStyle == ComboBoxStyle.DropDownList)
						{
							cbx.SelectedIndexChanged -= new EventHandler(this.Control_TextChanged);
						}
						else
						{
							control.TextChanged -= new EventHandler(this.Control_TextChanged);
						}
					}
					// || control is TextBox
				}
				suspended++;
				StatusEventsLabel.Text = string.Format("OFF {0} {1}", suspended, resumed);
			}
		}

		public void ResumeEvents()
		{
			lock (eventsLock)
			{
				StatusEventsLabel.Text = "ON...";
				// Allow controls to fire events.
				foreach (var control in SettingManager.Current.SettingsMap.Values)
				{
					if (control is TrackBar) ((TrackBar)control).ValueChanged += new EventHandler(Control_ValueChanged);
					if (control is ListBox) ((ListBox)control).SelectedIndexChanged += new EventHandler(Control_SelectedIndexChanged);
					if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged += new EventHandler(Control_ValueChanged);
					if (control is CheckBox) ((CheckBox)control).CheckedChanged += new EventHandler(Control_CheckedChanged);
					if (control is ComboBox)
					{
						var cbx = (ComboBox)control;
						if (cbx.DropDownStyle == ComboBoxStyle.DropDownList)
						{
							cbx.SelectedIndexChanged += new EventHandler(this.Control_TextChanged);
						}
						else
						{
							control.TextChanged += new EventHandler(this.Control_TextChanged);
						}
					}
					//  || control is TextBox
				}
				resumed++;
				StatusEventsLabel.Text = string.Format("ON {0} {1}", suspended, resumed);
			}
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

		void Control_TextChanged(object sender, EventArgs e)
		{
			// Save setting and notify if value changed.
			if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
		}

		Dictionary<string, int> ListBoxCounts = new Dictionary<string, int>();

		/// <summary>Monitor changes remove/add inside ListBoxes.</summary>
		void Control_SelectedIndexChanged(object sender, EventArgs e)
		{
			lock (ListBoxCounts)
			{
				var lb = (ListBox)sender;
				// If list contains count of ListBoxes items.			
				if (ListBoxCounts.ContainsKey(lb.Name))
				{
					// If ListBoxe haven't changed then return;
					if (ListBoxCounts[lb.Name] == lb.Items.Count) return;
					ListBoxCounts[lb.Name] = lb.Items.Count;
				}
				else
				{
					ListBoxCounts.Add(lb.Name, lb.Items.Count);
				}
			}
			// Save setting and notify if value changed.
			if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
		}

		void Control_ValueChanged(object sender, EventArgs e)
		{
			// Save setting and notify if value changed.
			if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
		}

		void Control_CheckedChanged(object sender, EventArgs e)
		{
			// Save setting and notify if value changed.
			if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
		}

		public void ReloadXinputSettings()
		{
			SuspendEvents();
			SettingManager.Current.ReadSettings();
			ResumeEvents();
		}

		public void SaveSettings()
		{
			UpdateTimer.Stop();
			// Save settings to INI file.
			SettingManager.Current.SaveSettings();
			// Overwrite Temp file.
			var ini = new FileInfo(SettingManager.IniFileName);
			ini.CopyTo(SettingManager.TmpFileName, true);
			StatusTimerLabel.Text = "Settings saved";
			UpdateTimer.Start();
		}

		#endregion

		public static object XInputLock = new object();

		void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Program.IsClosing = true;
			if (UpdateTimer != null) UpdateTimer.Stop();
			// Disable force feedback effect before closing app.
			try
			{
				lock (XInputLock)
				{
					for (int i = 0; i < 4; i++)
					{
						if (ControlPads[i].LeftMotorTestTrackBar.Value > 0 || ControlPads[i].RightMotorTestTrackBar.Value > 0)
						{
							var gamePad = GamePads[i];
							if (XInput.IsLoaded && gamePad.IsConnected)
							{
								gamePad.SetVibration(new Vibration());
							}
						}

					}
					//BeginInvoke((MethodInvoker)delegate()
					//{
					//	XInput.FreeLibrary();    
					//});
				}
				System.Threading.Thread.Sleep(100);
			}
			catch (Exception) { }
			var tmp = new FileInfo(SettingManager.TmpFileName);
			var ini = new FileInfo(SettingManager.IniFileName);
			if (tmp.Exists)
			{
				// Before renaming file check for changes.
				var changed = false;
				if (tmp.Length != ini.Length) { changed = true; }
				else
				{
					var tmpChecksum = EngineHelper.GetFileChecksum(tmp.FullName);
					var iniChecksum = EngineHelper.GetFileChecksum(ini.FullName);
					changed = !tmpChecksum.Equals(iniChecksum);
				}
				if (changed)
				{
					var form = new MessageBoxForm();
					form.StartPosition = FormStartPosition.CenterParent;
					var result = form.ShowForm(
					"Do you want to save changes you made to configuration?",
					"Save Changes?",
					MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
					if (result == DialogResult.Yes)
					{
						// Do nothing since INI contains latest updates.
					}
					else if (result == DialogResult.No)
					{
						// Rename temp to INI.
						tmp.CopyTo(SettingManager.IniFileName, true);
					}
					else if (result == DialogResult.Cancel)
					{
						e.Cancel = true;
						return;
					}
				}
				// delete temp.
				tmp.Delete();
			}
			Settings.Default.Save();
			SettingManager.Settings.Save();
			SettingManager.Summaries.Save();
			SettingManager.Programs.Save();
			SettingManager.Games.Save();
		}

		#region Timer

		DeviceInstance[] diInstancesOld = new DeviceInstance[4];
		DeviceInstance[] diInstances = new DeviceInstance[4];

		Joystick[] diDevices = new Joystick[4];
		DeviceInfo[] diInfos = new DeviceInfo[4];

		public bool forceRecountDevices = true;

		string deviceInstancesOld = "";
		string deviceInstancesNew = "";
		public Guid AutoSelectControllerInstance = Guid.Empty;

		public DirectInput Manager = new DirectInput();

		/// <summary>
		/// Get array[4] of direct input devices.
		/// </summary>
		DeviceInstance[] GetDevices()
		{
			var devices = Manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices).ToList();
			if (SettingManager.Current.ExcludeSuplementalDevices)
			{
				// Supplemental devices are specialized device with functionality unsuitable for the main control of an application,
				// such as pedals used with a wheel.The following subtypes are defined.
				var supplementals = devices.Where(x => x.Type == SharpDX.DirectInput.DeviceType.Supplemental).ToArray();
				foreach (var supplemental in supplementals)
				{
					devices.Remove(supplemental);
				}
			}
			if (SettingManager.Current.ExcludeVirtualDevices)
			{
				// Exclude virtual devices so application could feed them.
				var virtualDevices = devices.Where(x => x.InstanceName.Contains("vJoy")).ToArray();
				foreach (var virtualDevice in virtualDevices)
				{
					devices.Remove(virtualDevice);
				}
			}
			// Move gaming wheels to the top index position by default.
			// Games like GTA need wheel to be first device to work properly.
			var wheels = devices.Where(x => x.Type == SharpDX.DirectInput.DeviceType.Driving || x.Subtype == (int)DeviceSubType.Wheel).ToArray();
			foreach (var wheel in wheels)
			{
				devices.Remove(wheel);
				devices.Insert(0, wheel);
			}
			var orderedDevices = new DeviceInstance[4];
			// Assign devices to their positions.
			for (int d = 0; d < devices.Count; d++)
			{
				var ig = devices[d].InstanceGuid;
				var section = SettingManager.Current.GetInstanceSection(ig);
				var ini2 = new Ini(SettingManager.IniFileName);
				string v = ini2.GetValue(section, SettingName.MapToPad);
				int mapToPad = 0;
				if (int.TryParse(v, out mapToPad) && mapToPad > 0 && mapToPad <= 4)
				{
					// If position is not occupied then...
					if (orderedDevices[mapToPad - 1] == null)
					{
						orderedDevices[mapToPad - 1] = devices[d];
					}
				}
			}
			// Get list of unassigned devices.
			var unassignedDevices = devices.Except(orderedDevices).ToArray();
			for (int i = 0; i < unassignedDevices.Length; i++)
			{
				// Assign to first empty slot.
				for (int d = 0; d < orderedDevices.Length; d++)
				{
					// If position is not occupied then...
					if (orderedDevices[d] == null)
					{
						orderedDevices[d] = unassignedDevices[i];
						break;
					}
				}
			}
			return orderedDevices;
		}


		/// <summary>
		/// Access this only inside Timer_Click!
		/// </summary>
		bool RefreshCurrentInstances(bool forceReload = false)
		{
			// If you encounter "LoaderLock was detected" Exception when debugging then:
			// Make sure that you have reference to Microsoft.Directx.dll. 
			bool instancesChanged = false;
			DeviceInstance[] devices = null;
			//var types = DeviceType.Driving | DeviceType.Flight | DeviceType.Gamepad | DeviceType.Joystick | DeviceType.FirstPerson;
			if (forceRecountDevices || forceReload)
			{
				devices = GetDevices();
				// Sore device instances and their order here.
				deviceInstancesNew = string.Join(",", devices.Select(x => x == null ? "" : x.InstanceGuid.ToString()));
				forceRecountDevices = false;
			}
			//Populate All devices
			if (deviceInstancesNew != deviceInstancesOld)
			{
				deviceInstancesOld = deviceInstancesNew;
				if (devices == null) devices = GetDevices();
				var instances = devices;
				// Dispose from previous list of devices.
				for (int i = 0; i < 4; i++)
				{
					if (diDevices[i] != null)
					{
						// Dispose current device.
						diDevices[i].Unacquire();
						diDevices[i].Dispose();
					}
				}
				// Create new list of devices.
				for (int i = 0; i < 4; i++)
				{
					var inst = instances[i];
					if (inst == null)
					{
						diDevices[i] = null;
						diInfos[i] = null;
					}
					else
					{

						var j = new Joystick(Manager, inst.InstanceGuid);
						diDevices[i] = j;
						var classGuid = j.Properties.ClassGuid;
						var interfacePath = j.Properties.InterfacePath;
						// Must find better way to find Device than by Vendor ID and Product ID.
						var devs = DeviceDetector.GetDevices(classGuid, JocysCom.ClassLibrary.Win32.DIGCF.DIGCF_ALLCLASSES, null, j.Properties.VendorId, j.Properties.ProductId, 0);
						diInfos[i] = devs.FirstOrDefault();
					}
				}
				SettingsDatabasePanel.BindDevices(instances);
				SettingsDatabasePanel.BindFiles();
				for (int i = 0; i < 4; i++)
				{
					// Backup old instance.
					diInstancesOld[i] = diInstances[i];
					// Assign new instance.
					diInstances[i] = instances[i];
				}
				instancesChanged = true;
			}
			// Return true if instances changed.
			return instancesChanged;
		}

		void SettingsTimer_Elapsed(object sender, EventArgs e)
		{
			if (Program.IsClosing) return;
			settingsChanged = true;
			UpdateTimer.Start();
		}

		bool settingsChanged = false;
		State emptyState = new State();

		bool[] cleanPadStatus = new bool[4];

		object formLoadLock = new object();
		public bool update1Enabled = true;
		public bool? update2Enabled;
		public bool update3Enabled = false;

		void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Program.IsClosing) return;
			Program.TimerCount++;
			lock (formLoadLock)
			{
				if (update1Enabled)
				{
					update1Enabled = false;
					UpdateForm1();
					// Update 2 part will be enabled after all issues are checked.
				}
				if (update2Enabled.HasValue && update2Enabled.Value)
				{
					update2Enabled = false;
					UpdateForm2();
					update3Enabled = true;
				}
				if (update3Enabled)
				{
					UpdateForm3();
				}
			}
			UpdateTimer.Start();
		}

		void UpdateForm1()
		{
			detector = new DeviceDetector(false);
			detector.DeviceChanged += new DeviceDetector.DeviceDetectorEventHandler(detector_DeviceChanged);
			BusyLoadingCircle.Visible = false;
			BusyLoadingCircle.Top = HeaderPictureBox.Top;
			BusyLoadingCircle.Left = HeaderPictureBox.Left;
			defaultBody = HelpBodyLabel.Text;
			//if (DesignMode) return;
			OptionsPanel.InitOptions();
			// Set status.
			StatusSaveLabel.Visible = false;
			StatusEventsLabel.Visible = false;
			// Load Tab pages.
			ControlPages = new TabPage[4];
			ControlPages[0] = Pad1TabPage;
			ControlPages[1] = Pad2TabPage;
			ControlPages[2] = Pad3TabPage;
			ControlPages[3] = Pad4TabPage;
			//BuletImageList.Images.Add("bullet_square_glass_blue.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_blue.png")));
			//BuletImageList.Images.Add("bullet_square_glass_green.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_green.png")));
			//BuletImageList.Images.Add("bullet_square_glass_grey.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_grey.png")));
			//BuletImageList.Images.Add("bullet_square_glass_red.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_red.png")));
			//BuletImageList.Images.Add("bullet_square_glass_yellow.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_yellow.png")));
			foreach (var item in ControlPages) item.ImageKey = "bullet_square_glass_grey.png";
			// Hide status values.
			StatusDllLabel.Text = "";
			MainStatusStrip.Visible = false;
			// Check if INI and DLL is on disk.
			WarningsForm.CheckAndOpen();
		}

		void UpdateForm2()
		{
			// Set status labels.
			StatusIsAdminLabel.Text = WinAPI.IsVista
				? string.Format("Elevated: {0}", WinAPI.IsElevated())
				: "";
			StatusIniLabel.Text = SettingManager.IniFileName;
			CheckEncoding(SettingManager.TmpFileName);
			CheckEncoding(SettingManager.IniFileName);
			// Show status values.
			MainStatusStrip.Visible = true;
			// Load PAD controls.
			ControlPads = new PadControl[4];
			for (int i = 0; i < ControlPads.Length; i++)
			{
				ControlPads[i] = new Controls.PadControl(i);
				ControlPads[i].Name = string.Format("ControlPad{0}", i + 1);
				ControlPads[i].Dock = DockStyle.Fill;
				ControlPages[i].Controls.Add(ControlPads[i]);
				ControlPads[i].InitPadControl();
			}
			// Initialize pre-sets. Execute only after name of cIniFile is set.
			SettingsDatabasePanel.InitPresets();
			// Allow events after PAD control are loaded.
			MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
			// Load about control.
			ControlAbout = new AboutControl();
			ControlAbout.Dock = DockStyle.Fill;
			AboutTabPage.Controls.Add(ControlAbout);
			// Update settings map.
			UpdateSettingsMap();
			ReloadXinputSettings();
			//// start capture events.
			if (WinAPI.IsVista && WinAPI.IsElevated() && WinAPI.IsInAdministratorRole) this.Text += " (Administrator)";
		}

		void UpdateForm3()
		{
			bool instancesChanged = RefreshCurrentInstances(settingsChanged);
			// Load direct input data.
			for (int i = 0; i < 4; i++)
			{
				var currentPadControl = ControlPads[i];
				var currentDevice = diDevices[i];
				var currentDeviceInfo = diInfos[i];
				// If current device is empty then..
				if (currentDevice == null)
				{
					// but form contains data then...
					if (!cleanPadStatus[i])
					{
						// Clear all settings.
						SuspendEvents();
						SettingManager.Current.ClearPadSettings(i);
						ResumeEvents();
						cleanPadStatus[i] = true;
					}
				}
				else
				{
					cleanPadStatus[i] = false;
				}
				currentPadControl.UpdateFromDirectInput(currentDevice, currentDeviceInfo);
			}
			// If settings changed or directInput instances changed then...
			if (settingsChanged || instancesChanged)
			{
				if (instancesChanged)
				{
					var updated = SettingManager.Current.CheckSettings(diInstances, diInstancesOld);
					if (updated) SettingManager.Current.SaveSettings();
				}
				ReloadLibrary();
			}
			else
			{
				for (int i = 0; i < 4; i++)
				{
					// DInput instance is ON.
					var diOn = diInstances[i] != null;
					// XInput instance is ON.
					//XInput.Controllers[i].PollState();
					var xiOn = false;
					State currentPad = emptyState;
					var currentPadControl = ControlPads[i];
					lock (XInputLock)
					{
						var gamePad = GamePads[i];
						if (XInput.IsLoaded && gamePad.IsConnected)
						{
							currentPad = gamePad.GetState();
							xiOn = true;
						}
					}
					currentPadControl.UpdateFromXInput(currentPad, xiOn);
					// Update LED of GamePad state.
					string image = diOn
						// DInput ON, XInput ON 
						? xiOn ? "green"
						// DInput ON, XInput OFF
						: "red"
						// DInput OFF, XInput ON
						: xiOn ? "yellow"
						// DInput OFF, XInput OFF
						: "grey";
					string bullet = string.Format("bullet_square_glass_{0}.png", image);
					if (ControlPages[i].ImageKey != bullet) ControlPages[i].ImageKey = bullet;
				}
				UpdateStatus("");
			}
		}

		public void ReloadLibrary()
		{
			Program.ReloadCount++;
			settingsChanged = false;
			var dllInfo = EngineHelper.GetDefaultDll();
			if (dllInfo != null && dllInfo.Exists)
			{
				bool byMicrosoft;
				var dllVersion = EngineHelper.GetDllVersion(dllInfo.FullName, out byMicrosoft);
				StatusDllLabel.Text = dllInfo.Name + " " + dllVersion.ToString() + (byMicrosoft ? " (Microsoft)" : "");
				// If fast reload of settings is supported then...
				lock (XInputLock)
				{
					if (XInput.IsResetSupported)
					{
						XInput.Reset();
					}
					// Slow: Reload whole x360ce.dll.
					Exception error;
					//forceRecountDevices = true;
					XInput.ReLoadLibrary(dllInfo.Name, out error);
					if (!XInput.IsLoaded)
					{
						var caption = string.Format("Failed to load '{0}'", dllInfo.Name);
						var text = string.Format("{0}", error == null ? "Unknown error" : error.Message);
						var form = new MessageBoxForm();
						form.StartPosition = FormStartPosition.CenterParent;
						form.ShowForm(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					else
					{
						for (int i = 0; i < 4; i++)
						{

							var currentPadControl = ControlPads[i];
							currentPadControl.UpdateForceFeedBack();
						}
					}
				}
			}
			else
			{
				StatusDllLabel.Text = "";
			}
		}

		public void UpdateStatus(string message)
		{
			StatusTimerLabel.Text = string.Format("Count: {0}, Reloads: {1}, Errors: {2} {3}",
				Program.TimerCount, Program.ReloadCount, Program.ErrorCount, message);
		}
		#endregion

		bool HelpInit = false;

		void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (MainTabControl.SelectedTab == HelpTabPage && !HelpInit)
			{
				// Move this here so interface will load one second faster.
				HelpInit = true;
				var stream = EngineHelper.GetResource("Documents.Help.htm");
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
			else if (MainTabControl.SelectedTab == ControllerSettingsTabPage)
			{
				if (OptionsPanel.InternetCheckBox.Checked && OptionsPanel.InternetAutoloadCheckBox.Checked)
				{
					SettingsDatabasePanel.RefreshGrid(true);
				}
			}
			UpdateHelpHeader();
		}

		public void XInputEnable(bool enable)
		{
			lock (XInputLock)
			{
				XInput.XInputEnable(enable);
			}
		}

		#region Help Header

		string defaultBody;

		public void UpdateHelpHeader(string message, MessageBoxIcon icon)
		{
			HelpSubjectLabel.Text = MainTabControl.SelectedTab.Text;
			if (ControllerIndex > -1)
			{
				var currentPadControl = ControlPads[ControllerIndex];
				HelpSubjectLabel.Text += " - " + currentPadControl.PadTabControl.SelectedTab.Text;
			}
			HelpBodyLabel.Text = string.IsNullOrEmpty(message) ? defaultBody : message;
			if (icon == MessageBoxIcon.Error) HelpBodyLabel.ForeColor = System.Drawing.Color.DarkRed;
			else if (icon == MessageBoxIcon.Information) HelpBodyLabel.ForeColor = System.Drawing.Color.DarkGreen;
			else HelpBodyLabel.ForeColor = System.Drawing.SystemColors.ControlText;
		}

		public void UpdateHelpHeader(string message)
		{
			UpdateHelpHeader(message, MessageBoxIcon.None);
		}

		public void UpdateHelpHeader()
		{
			UpdateHelpHeader(defaultBody, MessageBoxIcon.None);
		}

		#endregion

		#region Check Files

		void CheckEncoding(string path)
		{
			if (!File.Exists(path)) return;
			var sr = new StreamReader(path, true);
			var content = sr.ReadToEnd();
			sr.Close();
			if (sr.CurrentEncoding != System.Text.Encoding.Unicode)
			{
				File.WriteAllText(path, content, System.Text.Encoding.Unicode);
			}
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

		public bool CreateFile(string resourceName, string destinationFileName, ProcessorArchitecture oldArchitecture, ProcessorArchitecture newArchitecture)
		{
			if (destinationFileName == null) destinationFileName = resourceName;
			DialogResult answer;
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var oldDesc = EngineHelper.GetProcessorArchitectureDescription(oldArchitecture);
			var newDesc = EngineHelper.GetProcessorArchitectureDescription(newArchitecture);
			var fileName = new FileInfo(destinationFileName).Name;
			answer = form.ShowForm(
				string.Format("You are running {2} application but {0} on the disk was built for {1} architecture.\r\n\r\nDo you want to replace {0} file with {2} version?", fileName, oldDesc, newDesc),
				"Processor architecture mismatch.",
				MessageBoxButtons.YesNo, MessageBoxIcon.Information);
			if (answer == DialogResult.Yes)
			{
				return AppHelper.WriteFile(resourceName, destinationFileName);
			}
			return true;
		}

		public bool CreateFile(string resourceName, string destinationFileName, Version oldVersion = null, Version newVersion = null)
		{
			if (destinationFileName == null) destinationFileName = resourceName;
			DialogResult answer;
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var fileName = new FileInfo(destinationFileName).FullName;
			if (newVersion == null)
			{
				answer = form.ShowForm(
					string.Format("'{0}' was not found.\r\nThis file is required for emulator to function properly.\r\n\r\nDo you want to create this file?", fileName),
					string.Format("'{0}' was not found.", destinationFileName),
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
			}
			else
			{
				answer = form.ShowForm(
					string.Format("New version of this file is available:\r\n{0}\r\n\r\nOld version: {1}\r\nNew version: {2}\r\n\r\nDo you want to update this file?", fileName, oldVersion, newVersion),
					string.Format("New version of '{0}' file is available.", destinationFileName),
					MessageBoxButtons.YesNo, MessageBoxIcon.Information);
			}
			if (answer == DialogResult.Yes)
			{
				return AppHelper.WriteFile(resourceName, destinationFileName);
			}
			return true;
		}

		#endregion

		#region WebService loading circle

		public bool LoadingCircle
		{
			get { return BusyLoadingCircle.Active; }
			set
			{
				if (value)
				{
					BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
					BusyLoadingCircle.InnerCircleRadius = 12;
					BusyLoadingCircle.NumberSpoke = 100;
					BusyLoadingCircle.OuterCircleRadius = 18;
					BusyLoadingCircle.RotationSpeed = 10;
					BusyLoadingCircle.SpokeThickness = 3;
					//this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
					BusyLoadingCircle.Active = value;
					BusyLoadingCircle.Visible = value;
				}
				else
				{
					LoadinngCircleTimeout.Enabled = true;
				}
			}
		}

		void LoadinngCircleTimeout_Tick(object sender, EventArgs e)
		{
			LoadinngCircleTimeout.Enabled = false;
			BusyLoadingCircle.Active = false;
			BusyLoadingCircle.Visible = false;
		}

		#endregion

		#region Allow only one copy of Application at a time

		/// <summary>Stores the unique windows message id from the RegisterWindowMessage call.</summary>
		int _WindowMessage;
		/// <summary>Used to determine if the application is already open.</summary>
		System.Threading.Mutex _Mutex;

		public const int wParam_Restore = 1;
		public const int wParam_Close = 2;

		/// <summary>
		/// Broadcast message to other instances of this application.
		/// </summary>
		/// <param name="wParam">Send parameter to other instances of this application.</param>
		/// <returns>True - other instances exists; False - other instances doesn't exist.</returns>
		public bool BroadcastMessage(int wParam)
		{
			Exception error;
			// Check for previous instance of this app.
			var uid = Application.ProductName;
			_Mutex = new System.Threading.Mutex(false, uid);
			// Register the windows message
			_WindowMessage = NativeMethods.RegisterWindowMessage(uid, out error);
			var firsInstance = _Mutex.WaitOne(1, true);
			// If this is not the first instance then...
			if (!firsInstance)
			{
				// Broadcast a message with parameters to another instance.
				var recipients = (int)BSM.BSM_APPLICATIONS;
				var flags = BSF.BSF_IGNORECURRENTTASK | BSF.BSF_POSTMESSAGE;
				var ret = NativeMethods.BroadcastSystemMessage((int)flags, ref recipients, _WindowMessage, wParam, 0, out error);
			}
			return !firsInstance;
		}

		/// <summary>
		/// NOTE: you must be careful with this method, because this method is responsible for all the
		/// windows messages that are coming to the form.
		/// </summary>
		/// <param name="m"></param>
		/// <remarks>This overrides the windows messaging processing</remarks>
		protected override void DefWndProc(ref Message m)
		{
			// If message value was found then...
			if (m.Msg == _WindowMessage)
			{
				// Show currently running instance.
				if (m.WParam.ToInt32() == wParam_Restore)
				{
					// Note: FormWindowState.Minimized and FormWindowState.Normal was used to make sure that Activate() wont fail because of this:
					// Windows NT 5.0 and later: An application cannot force a window to the foreground while the user is working with another window.
					// Instead, SetForegroundWindow will activate the window (see SetActiveWindow) and call theFlashWindowEx function to notify the user.
					if (WindowState != FormWindowState.Minimized) WindowState = FormWindowState.Minimized;
					this.Activate();
					if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
				}
				//  Close currently running instance.
				if (m.WParam.ToInt32() == wParam_Close) Close();
			}
			// Let the normal windows messaging process it.
			base.DefWndProc(ref m);
		}

		#endregion

		/// <summary>
		/// Clean up any 
		/// being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				if (_Mutex != null) _Mutex.Dispose();
				Manager.Dispose();
				Manager = null;
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void OpenApplicationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RestoreFromTray();
		}

		private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#region Restore and Minimize

		FormWindowState? oldWindowState;
		object lastStateLock = new object();

		/// <summary>Will be used to prevent form flickering when restoring from tray.</summary>
		bool ignoreMinimizeToTray;

		private void MainForm_Resize(object sender, EventArgs e)
		{
			// Track window state changes.
			lock (lastStateLock)
			{
				var newWindowState = WindowState;
				if (!oldWindowState.HasValue || oldWindowState.Value != newWindowState)
				{
					oldWindowState = newWindowState;
					UpdateStatusBar(newWindowState);
				}
			}
		}

		void UpdateStatusBar(FormWindowState state)
		{
			if (state == FormWindowState.Minimized)
			{
				if (!ignoreMinimizeToTray)
				{
					if (ShowInTaskbar == Settings.Default.MinimizeToTray)
					{
						ShowInTaskbar = !Settings.Default.MinimizeToTray;
					}
				}
			}
			else
			{
				ignoreMinimizeToTray = false;
				if (ShowInTaskbar == false)
				{
					ShowInTaskbar = true;
				}
			}
		}

		public bool MinimizeToTrayChanging;
		public void SetMinimizeToTray(bool value)
		{
			if (MinimizeToTrayChanging) return;
			MinimizeToTrayChanging = true;
			if (OptionsPanel.MinimizeToTrayCheckBox.Checked != value)
			{
				OptionsPanel.MinimizeToTrayCheckBox.Checked = value;
			}
			if (Settings.Default.MinimizeToTray != value)
			{
				Settings.Default.MinimizeToTray = value;
			}
			UpdateStatusBar(WindowState);
			MinimizeToTrayChanging = false;
		}

		/// <summary>
		/// Method to Minimize the window and Hide the window item in the TaskBar. 
		/// </summary>
		public void MinimizeToTray(bool showBalloonTip)
		{
			// Show only first time.
			if (showBalloonTip)
			{
				TrayNotifyIcon.BalloonTipText = "Password Generator...";
				// Show balloon tip for 2 seconds.
				TrayNotifyIcon.ShowBalloonTip(2);
			}
			// hold - program.
			// NOTE: also it would be possible to track which direction mouse will move in or move out on TrayIcon.
			// For example: open program if mouse moves in from left and moves out from top.
			TrayNotifyIcon.Text = "Click: double - program, left - generate, right - menu.";
			if (WindowState != FormWindowState.Minimized) WindowState = FormWindowState.Minimized;
		}

		/// <summary>
		/// Restores the window.
		/// </summary>
		public void RestoreFromTray()
		{
			ignoreMinimizeToTray = true;
			// Show in task bar before restoring windows state in order to prevent flickering.
			ShowInTaskbar = true;
			if (WindowState != FormWindowState.Normal)
			{
				WindowState = FormWindowState.Normal;
			}
			BringToFront();
		}

		void MinimizeToTrayToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetMinimizeToTray(!Settings.Default.MinimizeToTray);
		}

		#endregion

		private void TrayNotifyIcon_DoubleClick(object sender, EventArgs e)
		{
			RestoreFromTray();
		}
	}
}
