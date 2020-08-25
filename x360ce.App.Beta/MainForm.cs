using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Mail;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Win32;
using Nefarius.ViGEm.Client;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Issues;
using x360ce.App.Properties;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public partial class MainForm : BaseFormWithHeader
	{
		public MainForm()
		{
			AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			//AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			//AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ControlsHelper.InitInvokeContext();
			// Disable some functionality in Visual Studio Interface design mode.
			if (!IsDesignMode)
			{
				// Initialize exception handlers
				LogHelper.Current.LogExceptions = true;
				LogHelper.Current.LogToFile = true;
				//LogHelper.Current.LogFirstChanceExceptions = false;
				LogHelper.Current.InitExceptionHandlers(EngineHelper.AppDataPath + "\\Errors");
				LogHelper.Current.WritingException += LogHelper_Current_WritingException;
				// Fix access rights to configuration folder.
				var di = new DirectoryInfo(EngineHelper.AppDataPath);
				// Create configuration folder if not exists.
				if (!di.Exists)
					di.Create();
				var rights = FileSystemRights.Modify;
				var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
				// Check if users in non elevated mode have rights to modify the file.
				var hasRights = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(di.FullName, rights, users, false);
				if (!hasRights && WinAPI.IsElevated())
				{
					// Allow users to modify file when in non elevated mode.
					JocysCom.ClassLibrary.Security.PermissionHelper.SetRights(di.FullName, rights, users, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
					hasRights = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(di.FullName, rights, users, false);
				}
			}
			// Initialize interface.
			InitializeComponent();
			if (IsDesignMode)
				return;
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			GameToCustomizeComboBox.Font = Font;
			_ResumeTimer.Elapsed += _ResumeTimer_Elapsed;
			Pad1TabPage.Text = "Controller 1";
			Pad2TabPage.Text = "Controller 2";
			Pad3TabPage.Text = "Controller 3";
			Pad4TabPage.Text = "Controller 4";
			InitMinimize();
			InitiInterfaceUpdate();
			GamesToolStrip_Resize(null, null);
			ControlsHelper.ApplyBorderStyle(GamesToolStrip);
		}

		private void LogHelper_Current_NewException(object sender, EventArgs e)
		{
			ControlsHelper.BeginInvoke(new Action(() => UpdateStatusErrorsLabel()));
		}

		private void LogHelper_Current_WritingException(object sender, LogHelperEventArgs e)
		{
			if (Disposing)
				e.Cancel = true;
			var ex = e.Exception as SharpDX.SharpDXException;
			var d = ex?.Descriptor;
			if (d != null)
			{
				// If exception when getting Joystic properties in
				// CustomDiState.cs class: var o = device.GetObjectInfoByOffset((int)list[i]);
				if (d.ApiCode == "NotFound" && d.Code == -2147024894 &&
					d.Module == "SharpDX.DirectInput" &&
					d.NativeApiCode == "DIERR_NOTFOUND"
				)
				{
					// Cancel reporting error.
					e.Cancel = true;
				}
			}
			var fex = e.Exception as FileNotFoundException;
			// If serializer warning then...
			if (fex != null && fex.HResult == unchecked((int)0x80070002) && fex.FileName.Contains(".XmlSerializers"))
				// Cancel reporting error.
				e.Cancel = true;
		}

		public DInput.DInputHelper DHelper;

		public static MainForm Current { get; set; }


		public int oldIndex;

		public int ControllerIndex
		{
			get
			{
				int newIndex = -1;
				if (MainTabControl.SelectedTab == Pad1TabPage)
					newIndex = 0;
				if (MainTabControl.SelectedTab == Pad2TabPage)
					newIndex = 1;
				if (MainTabControl.SelectedTab == Pad3TabPage)
					newIndex = 2;
				if (MainTabControl.SelectedTab == Pad4TabPage)
					newIndex = 3;
				return newIndex;
			}
			set
			{
				switch (value)
				{
					case 0:
						MainTabControl.SelectedTab = Pad1TabPage;
						break;
					case 1:
						MainTabControl.SelectedTab = Pad2TabPage;
						break;
					case 2:
						MainTabControl.SelectedTab = Pad3TabPage;
						break;
					case 3:
						MainTabControl.SelectedTab = Pad4TabPage;
						break;
				}
			}
		}

		public AboutControl ControlAbout;
		public PadControl[] PadControls;
		public TabPage[] ControlPages;

		/// <summary>
		/// Settings timer will be used to delay applying settings, which will heavy load application, as long as user is changing them.
		/// </summary>
		public System.Timers.Timer SettingsTimer;

		public System.Timers.Timer UpdateTimer;

		public System.Timers.Timer CleanStatusTimer;
		public int DefaultPoolingInterval = 50;

		Forms.DebugForm DebugPanel;

		void MainForm_Load(object sender, EventArgs e)
		{
			if (IsDesignMode)
				return;
			if (ViGEm.HidGuardianHelper.CanModifyParameters(true))
			{
				ViGEm.HidGuardianHelper.InsertCurrentProcessToWhiteList();
				ViGEm.HidGuardianHelper.ClearWhiteList(true, true);
			}
			System.Threading.Thread.CurrentThread.Name = "MainFormThread";
			// Initialize Debug panel.
			DebugPanel = new Forms.DebugForm();
			// Initialize DInput Helper.
			DHelper = new DInput.DInputHelper();
			DHelper.DevicesUpdated += DHelper_DevicesUpdated;
			DHelper.UpdateCompleted += DHelper_UpdateCompleted;
			DHelper.FrequencyUpdated += DHelper_FrequencyUpdated;
			DHelper.StatesRetrieved += DHelper_StatesRetrieved;
			DHelper.XInputReloaded += DHelper_XInputReloaded;
			SettingsGridPanel._ParentForm = this;
			SettingsGridPanel.SettingsDataGridView.MultiSelect = true;
			SettingsGridPanel.InitPanel();
			// NotifySettingsChange will be called on event suspension and resume.
			SettingsManager.Current.NotifySettingsStatus = NotifySettingsStatus;
			// NotifySettingsChange will be called on setting changes.
			var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			SettingsManager.Current.SettingChanged += Current_SettingChanged;
			SettingsManager.Load(scheduler);
			SettingsManager.Summaries.Items.ListChanged += Summaries_ListChanged;
			XInputMaskScanner.FileInfoCache.Load();
			InitGameToCustomizeComboBox();
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
			ShowProgramsTab(SettingsManager.Options.ShowProgramsTab);
			ShowSettingsTab(SettingsManager.Options.ShowSettingsTab);
			ShowDevicesTab(SettingsManager.Options.ShowDevicesTab);
			// Start Timers.
			UpdateTimer.Start();
			JocysCom.ClassLibrary.Win32.NativeMethods.CleanSystemTray();
			MonitorErrors(true);
		}

		#region Process Monitor

		ProcessMonitor _ProcessMonitor;

		void InitProcessMonitor()
		{
			_ProcessMonitor = new ProcessMonitor();
			_ProcessMonitor.Start();
		}

		void DisposeProcessMonitor()
		{
			if (_ProcessMonitor != null)
				_ProcessMonitor.Dispose();
		}

		#endregion


		private void DHelper_XInputReloaded(object sender, DInput.DInputEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				var vi = e.XInputVersionInfo;
				var fi = e.XInputFileInfo;
				if (vi != null && fi != null)
				{
					var v = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
					var company = (vi.CompanyName ?? "").Replace("Microsoft Corporation", "Microsoft");
					ControlsHelper.SetText(StatusDllLabel, "{0} {1} ({2})", fi.Name, v, company);
					StatusDllLabel.Visible = true;
				}
				else
				{
					ControlsHelper.SetText(StatusDllLabel, "");
					StatusDllLabel.Visible = false;
				}
				if (Controller.IsLoaded)
				{
					if (PadControls != null)
					{
						for (int i = 0; i < 4; i++)
						{

							var currentPadControl = PadControls[i];
							currentPadControl.UpdateForceFeedBack();
						}
					}
				}
				if (e.Error != null)
				{
					SetHeaderError(e.Error.Message);
				}
			});
		}

		/// <summary>
		/// Delay settings trough timer so interface will be more responsive on TrackBars.
		/// Or fast changes. Library will be reloaded as soon as user calms down (no setting changes in 500ms).
		/// </summary>
		private void Current_SettingChanged(object sender, SettingChangedEventArgs e)
		{
			bool changed = false;
			changed |= SettingsManager.Current.ApplyAllSettingsToXML();
			//changed |= SettingsManager.Current.WriteSettingToIni(changedControl);
			// If settings changed then...
			if (changed)
			{
				// Stop updating forms and controls.
				// Update Timer will be started inside Settings timer.
				UpdateTimer.Stop();
				SettingsTimer.Stop();
				SettingsTimer.Start();
			}
		}

		private void DHelper_StatesRetrieved(object sender, DInput.DInputEventArgs e)
		{
			if (e.Error != null)
			{
				ControlsHelper.BeginInvoke(() =>
				{
					SettingsManager.Options.GetXInputStates = false;
					SetHeaderError(e.Error.Message);
				});
			}
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


		void AutoConfigure(Engine.Data.UserGame game)
		{
			var list = SettingsManager.UserDevices.Items.ToList();
			// Filter devices.
			if (SettingsManager.Options.ExcludeSupplementalDevices)
			{
				// Supplemental devices are specialized device with functionality unsuitable for the main control of an application,
				// such as pedals used with a wheel.The following subtypes are defined.
				var supplementals = list.Where(x => x.CapType == (int)SharpDX.DirectInput.DeviceType.Supplemental).ToArray();
				foreach (var supplemental in supplementals)
				{
					list.Remove(supplemental);
				}
			}
			if (SettingsManager.Options.ExcludeVirtualDevices)
			{
				// Exclude virtual devices so application could feed them.
				var virtualDevices = list.Where(x => x.InstanceName.Contains("vJoy")).ToArray();
				foreach (var virtualDevice in virtualDevices)
				{
					list.Remove(virtualDevice);
				}
			}
			// Move gaming wheels to the top index position by default.
			// Games like GTA need wheel to be first device to work properly.
			var wheels = list.Where(x =>
				x.CapType == (int)SharpDX.DirectInput.DeviceType.Driving ||
				x.CapSubtype == (int)DeviceSubType.Wheel
			).ToArray();
			foreach (var wheel in wheels)
			{
				list.Remove(wheel);
				list.Insert(0, wheel);
			}
			// Get configuration of devices for the game.
			var settings = SettingsManager.GetSettings(game.FileName);
			var knownDevices = settings.Select(x => x.InstanceGuid).ToList();
			var newSettingsToProcess = new List<Engine.Data.UserSetting>();
			int i = 0;
			while (true)
			{
				i++;
				// If there are devices which occupies current position then do nothing.
				if (settings.Any(x => x.MapTo == i))
					continue;
				// Try to select first unknown device.
				var newDevice = list.FirstOrDefault(x => !knownDevices.Contains(x.InstanceGuid));
				// If no device found then break.
				if (newDevice == null)
					break;
				// Create new setting for game/device.
				var newSetting = AppHelper.GetNewSetting(newDevice, game, i <= 4 ? (MapTo)i : MapTo.Disabled);
				newSettingsToProcess.Add(newSetting);
				// Add device to known list.
				knownDevices.Add(newDevice.InstanceGuid);
			}
			foreach (var item in newSettingsToProcess)
			{
				SettingsManager.UserSettings.Items.Add(item);
			}
		}

		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		void UpdateSettingsMap()
		{
			// INI setting keys with controls.
			SettingsManager.Current.ConfigLoaded += Current_ConfigLoaded;
			OptionsPanel.UpdateSettingsMap();
			OptionsPanel.InternetPanel.UpdateSettingsMap();
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
			// If pad controls not initializes yet then return.
			if (PadControls == null)
				return;
			for (int i = 0; i < PadControls.Length; i++)
			{
				// If Escape key was pressed while recording then...
				if (e.KeyCode == Keys.Escape)
				{
					var recordingWasStopped = PadControls[i]._recorder.StopRecording();
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
			if (Program.IsClosing)
				return;
			StatusTimerLabel.Text = "";
		}

		#region Control Changed Events

		public void NotifySettingsStatus(int eventsSuspendCount)
		{
			StatusEventsLabel.Text = string.Format("Suspend: {0}", eventsSuspendCount);
		}

		void SettingsTimer_Elapsed(object sender, EventArgs e)
		{
			if (Program.IsClosing)
				return;
			DHelper.SettingsChanged = true;
			UpdateTimer.Start();
		}

		#endregion

		void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Program.IsClosing = true;
			MonitorErrors(false);
			// Wrap into try catch so that the form will always close and
			// there will be no need to kill it by using task manager if exception is thrown.
			try
			{
				OnCloseAction(e);
			}
			catch (Exception) { }
		}

		void OnCloseAction(FormClosingEventArgs e)
		{
			// Disable force feedback effect before closing application.
			if (UpdateTimer != null)
				UpdateTimer.Stop();
			lock (Controller.XInputLock)
			{
				for (int i = 0; i < 4; i++)
				{
					if (PadControls[i].LeftMotorTestTrackBar.Value > 0 || PadControls[i].RightMotorTestTrackBar.Value > 0)
					{
						var gamePad = DHelper.LiveXiControllers[i];
						var isConected = DHelper.LiveXiConnected[i];
						if (Controller.IsLoaded && isConected)
						{
							// Stop vibration.
							gamePad.SetVibration(new Vibration());
						}
					}
				}
				//BeginInvoke((Action)delegate()
				//{
				//	XInput.FreeLibrary();    
				//});
			}
			// Logical delay without blocking the current thread.
			System.Threading.Tasks.Task.Delay(100).Wait();
			var tmp = new FileInfo(SettingsManager.TmpFileName);
			var ini = new FileInfo(SettingsManager.IniFileName);
			if (tmp.Exists && ini.Exists)
			{
				// Before renaming file check for changes.
				var changed = false;
				if (tmp.Length != ini.Length)
				{ changed = true; }
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
						tmp.CopyTo(SettingsManager.IniFileName, true);
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
			SaveAll();
			if (ViGEm.HidGuardianHelper.CanModifyParameters())
				ViGEm.HidGuardianHelper.RemoveCurrentProcessFromWhiteList();
		}

		#region Timer

		public bool forceRecountDevices = true;

		public Guid AutoSelectControllerInstance = Guid.Empty;

		object formLoadLock = new object();
		public bool update1Enabled = true;
		public bool? update2Enabled;
		bool update3Enabled;

		void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Program.IsClosing)
				return;
			Program.TimerCount++;
			lock (formLoadLock)
			{
				if (update1Enabled)
				{
					update1Enabled = false;
					InitIssuesIcon();
					UpdateForm1();
					// Update 2 part will be enabled after all issues are checked.
				}
				if (update2Enabled.HasValue && update2Enabled.Value)
				{
					update2Enabled = false;
					UpdateForm2();
					update3Enabled = true;
				}
				if (update3Enabled && IsHandleCreated)
				{
					InitProcessMonitor();
					update3Enabled = false;
					DHelper.Start();
				}
			}
			UpdateTimer.Start();
		}

		#region Issue Icon Timer

		public System.Timers.Timer IssueIconTimer;

		void InitIssuesIcon()
		{
			IssueIconTimer = new System.Timers.Timer();
			IssueIconTimer.SynchronizingObject = this;
			IssueIconTimer.AutoReset = false;
			IssueIconTimer.Interval = 1000;
			IssueIconTimer.Elapsed += IssueIconTimer_Elapsed;
			IssueIconTimer.Start();
		}

		private void IssueIconTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var key = IssuesTabPage.ImageKey;
			var hasIssues = IssuesPanel.HasIssues;
			// If unknown then...
			if (!hasIssues.HasValue)
			{
				key = "refresh_16x16.png";
			}
			else if (hasIssues.Value)
			{
				key = key == "fix_16x16.png"
					? "fix_off_16x16.png"
					: "fix_16x16.png";
			}
			else
			{
				key = "ok_off_16x16.png";
			}
			if (IssuesTabPage.ImageKey != key)
				IssuesTabPage.ImageKey = key;
			if (Program.IsClosing)
				return;
			IssueIconTimer.Start();
		}

		#endregion

		void UpdateForm1()
		{
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
			foreach (var item in ControlPages)
				item.ImageKey = "bullet_square_glass_grey.png";
			// Hide status values.
			StatusDllLabel.Text = "";
			MainStatusStrip.Visible = false;
			// Check for various issues.
			InitIssuesPanel();
			InitDeviceForm();
			InitUpdateForm();
		}

		void UpdateForm2()
		{
			// Set status labels.
			StatusIsAdminLabel.Text = WinAPI.IsVista
				? string.Format("Elevated: {0}", WinAPI.IsElevated())
				: "";
			CheckEncoding(SettingsManager.TmpFileName);
			CheckEncoding(SettingsManager.IniFileName);
			// Show status values.
			MainStatusStrip.Visible = true;
			// Update settings manager with [Options] section.
			UpdateSettingsMap();
			// Load PAD controls.
			PadControls = new PadControl[4];
			for (int i = 0; i < PadControls.Length; i++)
			{
				var mapTo = (MapTo)(i + 1);
				PadControls[i] = new Controls.PadControl(mapTo);
				PadControls[i].Name = string.Format("ControlPad{0}", (int)mapTo);
				PadControls[i].Dock = DockStyle.Fill;
				ControlPages[i].Controls.Add(PadControls[i]);
				PadControls[i].InitPadControl();
				// Update settings manager with [Mappings] section.
			}
			SettingsManager.AddMap(SettingsManager.MappingsSection, () => SettingName.PAD1, PadControls[0].MappedDevicesDataGridView);
			SettingsManager.AddMap(SettingsManager.MappingsSection, () => SettingName.PAD2, PadControls[1].MappedDevicesDataGridView);
			SettingsManager.AddMap(SettingsManager.MappingsSection, () => SettingName.PAD3, PadControls[2].MappedDevicesDataGridView);
			SettingsManager.AddMap(SettingsManager.MappingsSection, () => SettingName.PAD4, PadControls[3].MappedDevicesDataGridView);
			// Update settings manager with [PAD1], [PAD2], [PAD3], [PAD4] sections.
			// Note: There must be no such sections in new config.
			for (int i = 0; i < PadControls.Length; i++)
			{
				PadControls[i].UpdateSettingsMap();
				PadControls[i].InitPadData();
			}
			// Initialize pre-sets. Execute only after name of cIniFile is set.
			//SettingsDatabasePanel.InitPresets();
			// Allow events after PAD control are loaded.
			MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
			// Load about control.
			ControlAbout = new AboutControl();
			ControlAbout.Dock = DockStyle.Fill;
			AboutTabPage.Controls.Add(ControlAbout);
			// Start capture setting change events.
			SettingsManager.Current.ResumeEvents();
		}

		/// <summary>
		/// This method will run continuously if form is not minimized.
		/// </summary>
		void UpdateForm3()
		{
			var game = CurrentGame;
			var currentFile = (game == null) ? null : game.FileName;
			// Allow if not testing or testing with option enabled.
			var o = SettingsManager.Options;
			var allow = !o.TestEnabled || o.TestUpdateInterface;
			if (!allow)
				return;
			var client = ViGEmClient.Current;
			for (int i = 0; i < 4; i++)
			{
				// Get devices mapped to game and specific controller index.
				var devices = SettingsManager.GetDevices(currentFile, (MapTo)(i + 1));
				// DInput instance is ON if active devices found.
				var diOn = devices.Count(x => x.IsOnline) > 0;
				// XInput instance is ON.
				var xiOn = client != null && client.IsControllerConnected((uint)i + 1);
				var padControl = PadControls[i];
				// Update Form from DInput state.
				padControl.UpdateFromDInput();
				// Update Form from XInput state.
				padControl.UpdateFromXInput();
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
				if (ControlPages[i].ImageKey != bullet)
					ControlPages[i].ImageKey = bullet;
			}
		}

		public void UpdateStatus(string message = "")
		{
			ControlsHelper.SetText(StatusTimerLabel, "Count: {0}, Reloads: {1}, Errors: {2} {3}",
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
				var stream = EngineHelper.GetResourceStream("Documents.Help.htm");
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
			else if (MainTabControl.SelectedTab == SettingsTabPage)
			{
				var o = SettingsManager.Options;
				if (o.InternetFeatures && o.InternetAutoLoad)
				{
					//SettingsDatabasePanel.RefreshGrid(true);
				}
			}
			var tab = MainTabControl.SelectedTab;
			if (tab != null)
				SetHeaderSubject(tab.Text);
		}

		#region Check Files

		void CheckEncoding(string path)
		{
			if (!File.Exists(path))
				return;
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
			if (destinationFileName == null)
				destinationFileName = resourceName;
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
			if (destinationFileName == null)
				destinationFileName = resourceName;
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

		const int WM_WININICHANGE = 0x001A;
		const int WM_SETTINGCHANGE = WM_WININICHANGE;

		System.Timers.Timer _ResumeTimer = new System.Timers.Timer() { AutoReset = false, Interval = 1000 };

		private void _ResumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			DHelper.Start();
		}

		/// <summary>
		/// NOTE: you must be careful with this method, because this method is responsible for all the
		/// windows messages that are coming to the form.
		/// </summary>
		/// <param name="m"></param>
		/// <remarks>This overrides the windows messaging processing</remarks>
		protected override void DefWndProc(ref Message m)
		{
			if (m.Msg == WM_SETTINGCHANGE)
			{
				// Must stop all updates or interface will freeze during screen updates.
				DHelper.Stop();
				_ResumeTimer.Stop();
				_ResumeTimer.Start();
			}
			if (m.Msg == DeviceDetector.WM_DEVICECHANGE)
			{
				DHelper.UpdateDevicesEnabled = true;
			}
			// If message value was found then...
			else if (m.Msg == _WindowMessage)
			{
				// Show currently running instance.
				if (m.WParam.ToInt32() == wParam_Restore)
				{
					RestoreFromTray(true);
				}
				//  Close currently running instance.
				if (m.WParam.ToInt32() == wParam_Close)
				{
					Close();
				}
			}
			// Let the normal windows messaging process it.
			base.DefWndProc(ref m);
		}

		#endregion

		#region Issues Panel

		object issuesPanelLock = new object();

		void InitIssuesPanel()
		{
			lock (issuesPanelLock)
			{
				IssuesPanel.AddIssues(
					new ExeFileIssue(),
					new ArchitectureIssue(),
					new CppX86RuntimeInstallIssue(),
					new CppX64RuntimeInstallIssue(),
					new HotfixIssue(),
					new XboxDriversIssue(),
					new VirtualDeviceDriverIssue()
				);
				IssuesPanel.IsSuspended = new Func<bool>(IssuesPanel_IsSuspended);
				IssuesPanel.CheckCompleted += IssuesPanel_CheckCompleted;
				// This will start execution of Tasks Timer.
				IssuesPanel.TasksTimer.ChangeSleepInterval(5000);
				IssuesPanel.TasksTimer.DoActionNow();
			}
		}

		// When Application is started minimized then FormEventsEnabled will be set to false
		// Which means that IssuePanel will be suspended and will never run at least once.
		// This 'FirstRunIsDone' property will allow to check for issues at least once.
		bool FirstRunIsDone = false;

		bool IssuesPanel_IsSuspended()
		{
			var o = SettingsManager.Options;
			var allow = (FormEventsEnabled || !FirstRunIsDone) && (!o.TestEnabled || o.TestCheckIssues);
			if (allow)
				FirstRunIsDone = true;
			return !allow;
		}

		// Remember previous has issues status.
		bool? PrevHasIssues;

		private void IssuesPanel_CheckCompleted(object sender, EventArgs e)
		{
			// If check completed without issued then...
			var hasIssues = IssuesPanel.HasIssues;
			// If has issues and there were no issues before then...
			if (hasIssues.HasValue && hasIssues.Value && (!PrevHasIssues.HasValue || !PrevHasIssues.Value))
			{
				ControlsHelper.BeginInvoke(() =>
				{
					// Focus issues tab automatically.
					MainTabControl.SelectedTab = IssuesTabPage;
				});
			}
			PrevHasIssues = hasIssues;
			if (!update2Enabled.HasValue && hasIssues.HasValue && !hasIssues.Value)
			{
				// Enabled update 2 step.
				update2Enabled = true;
			}
		}

		#endregion

		#region Device Form

		MapDeviceToControllerForm _DeviceForm;
		object DeviceFormLock = new object();

		void InitDeviceForm()
		{
			lock (DeviceFormLock)
			{
				_DeviceForm = new MapDeviceToControllerForm();
			}
		}

		void DisposeDeviceForm()
		{
			lock (DeviceFormLock)
			{
				if (_DeviceForm != null)
				{
					_DeviceForm.Dispose();
					_DeviceForm = null;
				}
			}
		}

		public UserDevice[] ShowDeviceForm()
		{
			lock (DeviceFormLock)
			{
				if (_DeviceForm == null)
					return null;
				_DeviceForm.StartPosition = FormStartPosition.CenterParent;
				ControlsHelper.CheckTopMost(_DeviceForm);
				var result = _DeviceForm.ShowDialog();
				return _DeviceForm.SelectedDevices;
			}
		}

		#endregion

		#region Update Form

		Forms.UpdateForm _UpdateForm;
		object UpdateFormLock = new object();

		void InitUpdateForm()
		{
			lock (UpdateFormLock)
			{
				_UpdateForm = new Forms.UpdateForm();
			}
		}

		void DisposeUpdateForm()
		{
			lock (UpdateFormLock)
			{
				if (_UpdateForm != null)
				{
					_UpdateForm.Dispose();
					_UpdateForm = null;
				}
			}
		}

		public bool? ShowUpdateForm()
		{
			lock (UpdateFormLock)
			{
				if (_UpdateForm == null)
					return null;
				var oldTab = MainTabControl.SelectedTab;
				MainTabControl.SelectedTab = CloudTabPage;
				_UpdateForm.StartPosition = FormStartPosition.CenterParent;
				_UpdateForm.OpenDialog();
				ControlsHelper.CheckTopMost(_UpdateForm);
				var result = _UpdateForm.ShowDialog();
				_UpdateForm.CloseDialog();
				MainTabControl.SelectedTab = oldTab;
				return null;
			}
		}

		public void ProcessUpdateResults(CloudMessage results)
		{
			lock (UpdateFormLock)
			{
				if (_UpdateForm == null)
					return;
				_UpdateForm.Step2ProcessUpdateResults(results);
			}
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
				if (_Mutex != null)
				{
					_Mutex.Dispose();
				}
				DisposeProcessMonitor();
				DisposeDeviceForm();
				DisposeUpdateForm();
				DisposeInterfaceUpdate();
				if (DHelper != null)
					DHelper.Dispose();
				components.Dispose();
				//lock (checkTimerLock)
				//{
				//	// If timer is disposed then return;
				//	if (checkTimer == null) return;
				//	CheckAll();
				//}
			}
			base.Dispose(disposing);
		}

		private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#region Current Game

		public UserGame CurrentGame;
		public object CurrentGameLock = new object();

		public void SelectCurrentOrDefaultGame(UserGame game = null)
		{
			// Get current if not found.
			if (game == null)
				game = SettingsManager.UserGames.Items.FirstOrDefault(x => x.IsCurrentApp());
			ControlsHelper.BeginInvoke(() =>
			{
				GameToCustomizeComboBox.SelectedItem = game;
			});
		}

		void InitGameToCustomizeComboBox()
		{
			GameToCustomizeComboBox.ComboBox.DataSource = SettingsManager.UserGames.Items;
			// Make sure that X360CE.exe is on top.
			GameToCustomizeComboBox.ComboBox.DisplayMember = "DisplayName";
			GameToCustomizeComboBox.SelectedIndexChanged += GameToCustomizeComboBox_SelectedIndexChanged;
			// Select game by manually trigger event.
			var selected = ProcessMonitor.SelectOpenGame();
			if (!selected)
			{
				// Select current application.
				var game = SettingsManager.UserGames.Items.FirstOrDefault(x => x.IsCurrentApp());
				GameToCustomizeComboBox.ComboBox.SelectedItem = game;
				UpdateCurrentGame(game);
			}
		}

		private void GameToCustomizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var game = GameToCustomizeComboBox.SelectedItem as Engine.Data.UserGame;
			UpdateCurrentGame(game);
		}

		private void UpdateCurrentGame(UserGame game)
		{
			lock (CurrentGameLock)
			{
				// If nothing changed then...
				if (Equals(game, CurrentGame))
				{
					return;
				}
				if (CurrentGame != null)
				{
					// Detach event from old game.
					CurrentGame.PropertyChanged -= CurrentGame_PropertyChanged;
				}
				if (game != null)
				{
					// Attach event to new game.
					game.PropertyChanged += CurrentGame_PropertyChanged;
				}
				CurrentGame = game;
				DHelper.SettingsChanged = true;
				// If pad controls not initializes yet then return.
				if (PadControls == null)
					return;
				// Update PAD Control.
				foreach (var ps in PadControls)
				{
					if (ps != null)
						ps.UpdateFromCurrentGame();
				}
			}
		}

		private void CurrentGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// If pad controls not initializes yet then return.
			if (PadControls == null)
				return;
			var game = CurrentGame;
			if (game == null)
				return;
			// Update PAD Control.
			foreach (var ps in PadControls)
			{
				if (ps != null)
					ps.UpdateFromCurrentGame();
			}
			// Update controls by specific property.
			if (e.PropertyName == nameof(UserGame.EmulationType))
			{
			}
			SettingsManager.Current.RaiseSettingsChanged(null);
		}

		private void GamesToolStrip_Resize(object sender, EventArgs e)
		{
			GameToCustomizeComboBox.AutoSize = false;
			int width = GamesToolStrip.DisplayRectangle.Width;
			foreach (ToolStripItem tsi in GamesToolStrip.Items)
			{
				if (!(tsi == GameToCustomizeComboBox))
				{
					width -= tsi.Width;
					width -= tsi.Margin.Horizontal;
				}
			}
			GameToCustomizeComboBox.Width = Math.Max(0, width - GameToCustomizeComboBox.Margin.Horizontal);
			// Resolve disappearing.
			GamesToolStrip.PerformLayout();
		}

		#endregion

		#region Save and Synchronize Settings

		private void SaveAllButton_Click(object sender, EventArgs e)
		{
			Save();
		}

		/// <summary>
		/// This method will be called during manual saving and automatically when form is closing.
		/// </summary>
		public void SaveAll()
		{
			Settings.Default.Save();
			SettingsManager.OptionsData.Save();
			SettingsManager.UserSettings.Save();
			SettingsManager.Summaries.Save();
			SettingsManager.Programs.Save();
			SettingsManager.UserGames.Save();
			SettingsManager.Presets.Save();
			SettingsManager.Layouts.Save();
			SettingsManager.UserDevices.Save();
			SettingsManager.PadSettings.Save();
			SettingsManager.UserDevices.Save();
			SettingsManager.UserInstances.Save();
			XInputMaskScanner.FileInfoCache.Save();
		}

		public void Save()
		{
			// Disable buttons to make sure that user is not pressing it twice.
			SaveAllButton.Enabled = false;
			// Update interface.
			Application.DoEvents();
			// Save application settings.
			SaveAll();
			// Use timer to enable Save buttons after 520 ms.
			var timer = new System.Timers.Timer();
			timer.AutoReset = false;
			timer.Interval = 520;
			timer.SynchronizingObject = this;
			timer.Elapsed += Timer_Elapsed;
			timer.Start();
		}

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			SaveAllButton.Enabled = true;
			// Dispose original timer.
			var timer = (System.Timers.Timer)sender;
			timer.Elapsed -= Timer_Elapsed;
			timer.Dispose();
		}

		#endregion

		#region Update from DHelper

		object LockFormEvents = new object();

		// Set to false when main form is minimized, which will minimize CPU usage.
		public bool FormEventsEnabled;

		// Will be used to check it event handlers were called during form update period.
		bool FormEventsDevicesUpdated;
		bool FormEventsUpdateCompleted;
		bool FormEventsFrequencyUpdated;

		void EnableFormUpdates(bool enable)
		{
			lock (LockFormEvents)
			{
				if (enable && !FormEventsEnabled)
				{
					FormEventsEnabled = true;
					if (FormEventsDevicesUpdated)
						DHelper_DevicesUpdated(null, null);
					if (FormEventsUpdateCompleted)
						DHelper_UpdateCompleted(null, null);
					if (FormEventsFrequencyUpdated)
						DHelper_FrequencyUpdated(null, null);
				}
				else if (!enable && FormEventsEnabled)
				{
					FormEventsEnabled = false;
					FormEventsDevicesUpdated = false;
					FormEventsUpdateCompleted = false;
					FormEventsFrequencyUpdated = false;
				}
			}
		}

		private void DHelper_DevicesUpdated(object sender, EventArgs e)
		{
			lock (LockFormEvents)
			{
				FormEventsDevicesUpdated = true;
				if (!FormEventsEnabled)
					return;
			}
			// Make sure method is executed on the same thread as this control.
			if (InvokeRequired)
			{
				var method = new EventHandler<EventArgs>(DHelper_DevicesUpdated);
				BeginInvoke(method, new object[] { sender, e });
				return;
			}
			SettingsManager.RefreshDeviceIsOnlineValueOnSettings(SettingsManager.UserSettings.Items.ToArray());
			ControlsHelper.SetText(UpdateDevicesStatusLabel, "D: {0}", DHelper.RefreshDevicesCount);
		}

		bool UpdateCompletedBusy;
		object UpdateCompletedLock = new object();

		System.Diagnostics.Stopwatch InterfaceUpdateWatch;
		long LastUpdateTime;

		private void DHelper_UpdateCompleted(object sender, EventArgs e)
		{
			lock (LockFormEvents)
			{
				FormEventsUpdateCompleted = true;
				if (!FormEventsEnabled)
					return;
			}
			lock (UpdateCompletedLock)
			{
				if (InterfaceUpdateWatch == null)
				{
					InterfaceUpdateWatch = new System.Diagnostics.Stopwatch();
					InterfaceUpdateWatch.Start();
				}
				var delay = 1000 / (interfaceIsForeground ? interfaceUpdateForegroundFps : interfaceUpdateBackgroundFps);
				var currentTime = InterfaceUpdateWatch.ElapsedMilliseconds;
				// If not enough time passed then return.
				if ((currentTime - LastUpdateTime) < delay)
					return;
				// If still updating interface then return.
				if (UpdateCompletedBusy)
					return;
				UpdateCompletedBusy = true;
				LastUpdateTime = currentTime;
				if (!Program.IsClosing)
				{
					// Make sure method is executed on the same thread as this control.
					ControlsHelper.BeginInvoke(() =>
					{
						// Check again.
						if (!Program.IsClosing)
							UpdateForm3();
						UpdateCompletedBusy = false;
					});
				}
			}
		}

		private void DHelper_FrequencyUpdated(object sender, EventArgs e)
		{
			lock (LockFormEvents)
			{
				FormEventsFrequencyUpdated = true;
				if (!FormEventsEnabled)
					return;
			}
			// Make sure method is executed on the same thread as this control.
			if (InvokeRequired)
			{
				var method = new EventHandler<EventArgs>(DHelper_FrequencyUpdated);
				BeginInvoke(method, new object[] { sender, e });
				return;
			}
			ControlsHelper.SetText(UpdateFrequencyLabel, "Hz: {0}", DHelper.CurrentUpdateFrequency);
		}

		#endregion

		#region Update Interface

		bool interfaceIsForeground;
		// Allow no more than 20 frames per second in foreground (make it look smooth and responsive).
		int interfaceUpdateForegroundFps = 20;
		// Allow no more than  5 frames per second in background (save CPU resources).
		int interfaceUpdateBackgroundFps = 5;

		void InitiInterfaceUpdate()
		{
			Activated += MainForm_Activated;
			Deactivate += MainForm_Deactivate;
		}

		void DisposeInterfaceUpdate()
		{
			Activated -= MainForm_Activated;
			Deactivate -= MainForm_Deactivate;
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			interfaceIsForeground = false;
			ControlsHelper.SetText(FormUpdateFrequencyLabel, "Hz: {0}", interfaceUpdateBackgroundFps);
		}

		private void MainForm_Activated(object sender, EventArgs e)
		{
			interfaceIsForeground = true;
			ControlsHelper.SetText(FormUpdateFrequencyLabel, "Hz: {0}", interfaceUpdateForegroundFps);
		}

		#endregion

		#region Show/Hide tabs.


		public void ShowTab(bool show, TabPage page)
		{
			var tc = MainTabControl;
			// If must hide then...
			if (!show && tc.TabPages.Contains(page))
			{
				// Hide and return.
				tc.TabPages.Remove(page);
				return;
			}
			// If must show then..
			if (show && !tc.TabPages.Contains(page))
			{
				// Create list of tabs to maintain same order when hiding and showing tabs.
				var tabs = new List<TabPage>() { ProgramsTabPage, SettingsTabPage, DevicesTabPage };
				// Get index of always displayed tab.
				var index = tc.TabPages.IndexOf(GamesTabPage);
				// Get tabs in front of tab which must be inserted.
				var tabsBefore = tabs.Where(x => tabs.IndexOf(x) < tabs.IndexOf(page));
				// Count visible tabs.
				var countBefore = tabsBefore.Count(x => tc.TabPages.Contains(x));
				tc.TabPages.Insert(index + countBefore + 1, page);
			}
		}

		public void ShowProgramsTab(bool show)
		{
			ShowTab(show, ProgramsTabPage);
		}

		public void ShowSettingsTab(bool show)
		{
			ShowTab(show, SettingsTabPage);
		}

		public void ShowDevicesTab(bool show)
		{
			ShowTab(show, DevicesTabPage);
		}

		#endregion


		public void ChangeCurrentGameEmulationType(EmulationType type)
		{
			var game = CurrentGame;
			if (game == null)
				return;
			game.EmulationType = (int)type;
		}

		private void TestButton_Click(object sender, EventArgs e)
		{
			DebugPanel.ShowPanel();
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (SettingsManager.Options.ShowDebugPanel)
			{
				DebugPanel.ShowPanel();
			}
		}

		private void AddGameButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				MainTabControl.SelectedTab = GamesTabPage;
				Application.DoEvents();
				GameSettingsPanel.AddNewGame();
			});
		}

		Forms.ErrorReportWindow win;

		void StatusErrorLabel_Click(object sender, EventArgs e)
		{
			win = new Forms.ErrorReportWindow();
			ControlsHelper.CheckTopMost(win);
			ControlsHelper.AutoSizeByOpenForms(win);
			win.Width = Math.Min(1450, Screen.FromControl(this).WorkingArea.Width - 200);
			// Suspend displaying cloud queue results, because ShowDialog locks UI upates in the back.
			CloudPanel.EnableDataSource(false);
			win.ErrorReportPanel.SendMessages += ErrorReportPanel_SendMessages;
			win.ErrorReportPanel.ErrorsClearing += ErrorReportPanel_ErrorsClearing;
			win.ErrorReportPanel.ErrorsCleared += ErrorReportPanel_ErrorsCleared;
			Global.CloudClient.TasksTimer.Queue.ListChanged += Queue_ListChanged;
			var result = win.ShowDialog();
			Global.CloudClient.TasksTimer.Queue.ListChanged -= Queue_ListChanged;
			win.ErrorReportPanel.SendMessages -= ErrorReportPanel_SendMessages;
			win.ErrorReportPanel.ErrorsClearing -= ErrorReportPanel_ErrorsClearing;
			win.ErrorReportPanel.ErrorsCleared -= ErrorReportPanel_ErrorsCleared;

			CloudPanel.EnableDataSource(true);
		}

		private void ErrorReportPanel_ErrorsClearing(object sender, EventArgs e)
		{
			// Disable monitor while deleting files.
			MonitorErrors(false);
		}

		private void ErrorReportPanel_ErrorsCleared(object sender, EventArgs e)
		{
			// Enable monitor and show stats.
			MonitorErrors(true);
		}

		private void Queue_ListChanged(object sender, ListChangedEventArgs e)
		{
			var w = win;
			var q = Global.CloudClient.TasksTimer.Queue;
			if (win != null && q != null)
			{
				var item = q.FirstOrDefault(x => x.Action == CloudAction.SendMailMessage);
				Invoke(new Action(() =>
				{
					win.ErrorReportPanel.StatusLabel.Content = item == null ? "Message Delivered" : "Sending...";
				}));
			}
		}

		private void ErrorReportPanel_SendMessages(object sender, JocysCom.ClassLibrary.EventArgs<List<System.Net.Mail.MailMessage>> e)
		{
			var control = (ErrorReportControl)sender;
			// Create mail message.
			var win = (Forms.ErrorReportWindow)control.Parent;
			control.StatusLabel.Content = "Sending...";
			// Run cloud operation on a separate thread so that it won't freeze the app.
			Task.Run(new Action(() =>
			{
				var messages = e.Data.Select(x => new MailMessageSerializable(x)).ToArray();
				//var xml = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(messages.First());
				Global.CloudClient.Add(CloudAction.SendMailMessage, messages);
			}));
		}

		FileSystemWatcher errorsWatcher;
		object errorsWatcherLock = new object();

		void MonitorErrors(bool enable)
		{
			lock (errorsWatcherLock)
			{
				if (enable && errorsWatcher == null)
				{
					var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
					if (!dir.Exists)
						dir.Create();
					errorsWatcher = new FileSystemWatcher(dir.FullName, "*.htm");
					errorsWatcher.Deleted += ErrorsWatcher_Changed;
					errorsWatcher.Created += ErrorsWatcher_Changed;
					errorsWatcher.EnableRaisingEvents = true;
					ErrorsWatcher_Changed(null, null);
				}
				else if (!enable && errorsWatcher != null)
				{
					errorsWatcher.Deleted -= ErrorsWatcher_Changed;
					errorsWatcher.Created -= ErrorsWatcher_Changed;
					errorsWatcher.Dispose();
					errorsWatcher = null;
				}
			}
		}

		public static int ErrorFilesCount;

		private void ErrorsWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			ControlsHelper.BeginInvoke(new Action(() =>
			{
				var dir = new DirectoryInfo(LogHelper.Current.LogsFolder);
				ErrorFilesCount = dir.GetFiles("*.htm").Count();
				UpdateStatusErrorsLabel();
			}));
		}

		void UpdateStatusErrorsLabel()
		{
			StatusErrorsLabel.Text = string.Format("Errors: {0} | {1}", ErrorFilesCount, LogHelper.Current.ExceptionsCount);
			StatusErrorsLabel.ForeColor = ErrorFilesCount > 0
						? System.Drawing.Color.DarkRed
						: System.Drawing.SystemColors.ControlDark;
			StatusErrorsLabel.Image = ErrorFilesCount > 0
				? Resources.error_16x16
				: AppHelper.GetDisabledImage(Resources.error_16x16);
		}


	}
}

