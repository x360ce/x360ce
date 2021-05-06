using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using x360ce.App.Controls;
using x360ce.App.Issues;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			ControlsHelper.InitInvokeContext();
			// Make sure setting changes updates UI on the same thread as UI.
			var scheduler = ControlsHelper.MainTaskScheduler;
			SettingsManager.SetSynchronizingObject(scheduler);
			// Disable some functionality in Visual Studio Interface design mode.
			if (!ControlsHelper.IsDesignMode(this))
			{
				PreviewKeyDown += MainWindow_PreviewKeyDown;
				Closing += MainWindow_Closing;
			}
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			StartHelper.Initialize();
			InitiInterfaceUpdate();
			// Check if app version changed.
			var o = SettingsManager.Options;
			var appVersion = new JocysCom.ClassLibrary.Configuration.AssemblyInfo().Version.ToString();
			AppVersionChanged = o.AppVersion != appVersion;
			o.AppVersion = appVersion;
		}

		/// <summary>
		/// This overrides the windows messaging processing. Be careful with this method,
		/// because this method is responsible for all the windows messages that are coming to the form.
		/// </summary>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			var source = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(this);
			source.AddHook(StartHelper.CustomWndProc);
			IsHandleCreated = true;
		}

		private bool IsHandleCreated;

		public MainBodyControl MainBodyPanel
			=> MainPanel.MainBodyPanel;

		public OptionsControl OptionsPanel
			=> MainBodyPanel.OptionsPanel;

		public UserProgramsControl UserProgramsPanel
			=> MainBodyPanel.GamesPanel;

		private readonly bool AppVersionChanged;

		/// <summary>
		/// Settings timer will be used to delay applying settings, which will heavy load application, as long as user is changing them.
		/// </summary>
		public System.Timers.Timer SettingsTimer;

		public System.Timers.Timer UpdateTimer;

		public System.Timers.Timer CleanStatusTimer;
		public int DefaultPoolingInterval = 50;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			StartHelper.OnClose += (sender1, e1)
				=> Close();
			StartHelper.OnRestore += (sender1, e1)
				=> Global._TrayManager.RestoreFromTray(true);
			AppHelper.InitializeHidGuardian();
			if (string.IsNullOrEmpty(System.Threading.Thread.CurrentThread.Name))
				System.Threading.Thread.CurrentThread.Name = "MainFormThread";
			Global.DHelper.DevicesUpdated += DHelper_DevicesUpdated;
			Global.DHelper.UpdateCompleted += DHelper_UpdateCompleted;
			Global.DHelper.FrequencyUpdated += DHelper_FrequencyUpdated;
			Global.DHelper.StatesRetrieved += DHelper_StatesRetrieved;
			Global.DHelper.XInputReloaded += DHelper_XInputReloaded;
			MainBodyPanel.SettingsPanel._ParentControl = MainPanel._bwm;
			MainBodyPanel.SettingsPanel.MainDataGrid.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Extended;
			MainBodyPanel.SettingsPanel.InitPanel();
			// NotifySettingsChange will be called on event suspension and resume.
			SettingsManager.Current.NotifySettingsStatus = NotifySettingsStatus;
			// NotifySettingsChange will be called on setting changes.
			SettingsManager.Current.SettingChanged += Current_SettingChanged;
			SettingsManager.Summaries.Items.ListChanged += Summaries_ListChanged;
			XInputMaskScanner.FileInfoCache.Load();
			UpdateTimer = new System.Timers.Timer
			{
				AutoReset = false,
				Interval = DefaultPoolingInterval
			};
			UpdateTimer.Elapsed += UpdateTimer_Elapsed;
			SettingsTimer = new System.Timers.Timer
			{
				AutoReset = false,
				Interval = 500
			};
			SettingsTimer.Elapsed += SettingsTimer_Elapsed;
			CleanStatusTimer = new System.Timers.Timer
			{
				AutoReset = false,
				Interval = 3000
			};
			CleanStatusTimer.Elapsed += CleanStatusTimer_Elapsed;
			Title = EngineHelper.GetProductFullName();
			MainBodyPanel.ShowProgramsTab(SettingsManager.Options.ShowProgramsTab);
			MainBodyPanel.ShowSettingsTab(SettingsManager.Options.ShowSettingsTab);
			MainBodyPanel.ShowDevicesTab(SettingsManager.Options.ShowDevicesTab);
			// Start Timers.
			UpdateTimer.Start();
			JocysCom.ClassLibrary.Win32.NativeMethods.CleanSystemTray();
			// If enabling first time and application version changed then...
			ErrorsHelper.InitErrorsHelper(AppVersionChanged, MainPanel.StatusErrorsLabel, MainPanel.StatusErrorsIcon, MainPanel);
			var game = SettingsManager.CurrentGame;
			if (SettingsManager.Options.HidGuardianConfigureAutomatically)
			{
				// Enable Reconfigure HID Guardian.
				var changed = SettingsManager.AutoHideShowMappedDevices(game);
				var mappedInstanceGuids = SettingsManager.GetMappedDevices(game?.FileName, true)
					.Select(x => x.InstanceGuid).ToArray();
				AppHelper.SynchronizeToHidGuardian(mappedInstanceGuids);
			}
		}

		private void DHelper_XInputReloaded(object sender, DInput.DInputEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				var label = MainPanel.StatusDllLabel;
				var vi = e.XInputVersionInfo;
				var fi = e.XInputFileInfo;
				var text = "";
				var show = vi != null && fi != null;
				if (show)
				{
					var v = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
					var company = (vi.CompanyName ?? "").Replace("Microsoft Corporation", "Microsoft");
					text = string.Format("{0} {1} ({2})", fi.Name, v, company);
				}
				ControlsHelper.SetText(label, text);
				ControlsHelper.SetVisible(label, show);
				if (e.Error != null)
					MainPanel._bwm.SetBodyError(e.Error.Message);
			});
		}

		/// <summary>
		/// Delay settings trough timer so interface will be more responsive on TrackBars.
		/// Or fast changes. Library will be reloaded as soon as user calms down (no setting changes in 500ms).
		/// </summary>
		private void Current_SettingChanged(object sender, SettingChangedEventArgs e)
		{
			var changed = false;
			changed |= SettingsManager.Current.ApplyAllSettingsToXML();
			//changed |= SettingsManager.Current.WriteSettingToIni(changedControl);
			// If settings changed then...
			if (changed)
			{
				// Stop updating forms and controls.
				// Update Timer will be started inside Settings timer.
				UpdateTimer.Stop();
				SettingsTimer.Stop();

				// Synchronize settings to HID Guardian.
				//AppHelper.SynchronizeToHidGuardian();
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
					MainPanel._bwm.SetBodyError(e.Error.Message);
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

		//private void AutoConfigure(Engine.Data.UserGame game)
		//{
		//	var list = SettingsManager.UserDevices.Items.ToList();
		//	// Filter devices.
		//	if (SettingsManager.Options.ExcludeSupplementalDevices)
		//	{
		//		// Supplemental devices are specialized device with functionality unsuitable for the main control of an application,
		//		// such as pedals used with a wheel.The following subtypes are defined.
		//		var supplementals = list.Where(x => x.CapType == (int)SharpDX.DirectInput.DeviceType.Supplemental).ToArray();
		//		foreach (var supplemental in supplementals)
		//			list.Remove(supplemental);
		//	}
		//	if (SettingsManager.Options.ExcludeVirtualDevices)
		//	{
		//		// Exclude virtual devices so application could feed them.
		//		var virtualDevices = list.Where(x => x.InstanceName.Contains("vJoy")).ToArray();
		//		foreach (var virtualDevice in virtualDevices)
		//			list.Remove(virtualDevice);
		//	}
		//	// Move gaming wheels to the top index position by default.
		//	// Games like GTA need wheel to be first device to work properly.
		//	var wheels = list.Where(x =>
		//		x.CapType == (int)SharpDX.DirectInput.DeviceType.Driving ||
		//		x.CapSubtype == (int)DeviceSubType.Wheel
		//	).ToArray();
		//	foreach (var wheel in wheels)
		//	{
		//		list.Remove(wheel);
		//		list.Insert(0, wheel);
		//	}
		//	// Get configuration of devices for the game.
		//	var settings = SettingsManager.GetSettings(game.FileName);
		//	var knownDevices = settings.Select(x => x.InstanceGuid).ToList();
		//	var newSettingsToProcess = new List<Engine.Data.UserSetting>();
		//	var i = 0;
		//	while (true)
		//	{
		//		i++;
		//		// If there are devices which occupies current position then do nothing.
		//		if (settings.Any(x => x.MapTo == i))
		//			continue;
		//		// Try to select first unknown device.
		//		var newDevice = list.FirstOrDefault(x => !knownDevices.Contains(x.InstanceGuid));
		//		// If no device found then break.
		//		if (newDevice == null)
		//			break;
		//		// Create new setting for game/device.
		//		var newSetting = AppHelper.GetNewSetting(newDevice, game, i <= 4 ? (MapTo)i : MapTo.Disabled);
		//		newSettingsToProcess.Add(newSetting);
		//		// Add device to known list.
		//		knownDevices.Add(newDevice.InstanceGuid);
		//	}
		//	foreach (var item in newSettingsToProcess)
		//		SettingsManager.UserSettings.Items.Add(item);
		//}

		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		private void UpdateSettingsMap()
		{
			// INI setting keys with controls.
			SettingsManager.Current.ConfigLoaded += Current_ConfigLoaded;
			OptionsPanel.GeneralPanel.UpdateSettingsMap();
			OptionsPanel.InternetPanel.UpdateSettingsMap();
		}

		private void Current_ConfigSaved(object sender, SettingEventArgs e)
		{
			MainPanel.StatusSaveLabel.Content = string.Format("S {0}", e.Count);
		}

		private void Current_ConfigLoaded(object sender, SettingEventArgs e)
		{
			MainPanel.StatusTimerLabel.Content = string.Format("'{0}' loaded.", e.Name);
		}

		private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			// If pad controls not initializes yet then return.
			if (MainPanel.MainBodyPanel.PadControls == null)
				return;
			for (var i = 0; i < MainPanel.MainBodyPanel.PadControls.Length; i++)
			{
				// If Escape key was pressed while recording then...
				if (e.Key == System.Windows.Input.Key.Escape)
				{
					var recordingWasStopped = MainPanel.MainBodyPanel.PadControls[i].StopRecording();
					if (recordingWasStopped)
						e.Handled = true;
				}
			}
			MainPanel.StatusTimerLabel.Content = "";
		}

		private void CleanStatusTimer_Elapsed(object sender, EventArgs e)
		{
			if (Program.IsClosing)
				return;
			ControlsHelper.BeginInvoke(() =>
			{
				MainPanel.StatusTimerLabel.Content = "";
			});
		}

		#region ■ Control Changed Events

		public void NotifySettingsStatus(int eventsSuspendCount)
		{
			MainPanel.StatusEventsLabel.Content = string.Format("Suspend: {0}", eventsSuspendCount);
		}

		private void SettingsTimer_Elapsed(object sender, EventArgs e)
		{
			if (Program.IsClosing)
				return;
			ControlsHelper.BeginInvoke(() =>
			{
				Global.DHelper.SettingsChanged = true;
				UpdateTimer.Start();
			});
		}

		#endregion

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			Program.IsClosing = true;
			ErrorsHelper.DisposeErrorsHelper();
			MainBodyPanel.Dispose();
			// Wrap into try catch so that the form will always close and
			// there will be no need to kill it by using task manager if exception is thrown.
			try
			{
				OnCloseAction(e);
			}
			catch (Exception) { }
		}

		private void OnCloseAction(CancelEventArgs e)
		{
			// Disable force feedback effect before closing application.
			if (UpdateTimer != null)
				UpdateTimer.Stop();
			lock (Controller.XInputLock)
			{
				for (var i = 0; i < 4; i++)
				{
					//if (PadControls[i].LeftMotorTestTrackBar.Value > 0 || PadControls[i].RightMotorTestTrackBar.Value > 0)
					//{
					var gamePad = Global.DHelper.LiveXiControllers[i];
					var isConected = Global.DHelper.LiveXiConnected[i];
					if (Controller.IsLoaded && isConected)
					{
						// Stop vibration.
						gamePad.SetVibration(new Vibration());
					}
					//}
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
					var form = new MessageBoxWindow();
					var result = form.ShowDialog(
					"Do you want to save changes you made to configuration?",
					"Save Changes?",
					System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.Yes);
					if (result == System.Windows.MessageBoxResult.Yes)
					{
						// Do nothing since INI contains latest updates.
					}
					else if (result == System.Windows.MessageBoxResult.No)
					{
						// Rename temp to INI.
						tmp.CopyTo(SettingsManager.IniFileName, true);
					}
					else if (result == System.Windows.MessageBoxResult.Cancel)
					{
						e.Cancel = true;
						return;
					}
				}
				// delete temp.
				tmp.Delete();
			}
			SettingsManager.SaveAll();
			AppHelper.UnInitializeHidGuardian();
		}

		#region ■ Timer

		public bool forceRecountDevices = true;

		public Guid AutoSelectControllerInstance = Guid.Empty;
		private readonly object formLoadLock = new object();
		public bool update1Enabled = true;
		public bool? update2Enabled;
		private bool update3Enabled;

		private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Program.IsClosing)
				return;
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.BeginInvoke(() => UpdateTimer_Elapsed(sender, e));
				return;
			}
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
				if (update3Enabled && IsHandleCreated)
				{
					update3Enabled = false;
					// Use this property to make sure that DHelper never starts unless all steps are fully initialised.
					Global.AllowDHelperStart = true;
					Global.DHelper.Start();
				}
			}
			UpdateTimer.Start();
		}

		private void UpdateForm1()
		{
			//if (DesignMode) return;
			OptionsPanel.GeneralPanel.InitOptions();
			// Set status.
			ControlsHelper.SetVisible(MainPanel.StatusSaveLabel, false);
			ControlsHelper.SetVisible(MainPanel.StatusEventsLabel, false);
			// Check for various issues.
			InitIssuesPanel();
			InitUpdateForm();
		}

		private void UpdateForm2()
		{
			CheckEncoding(SettingsManager.TmpFileName);
			CheckEncoding(SettingsManager.IniFileName);
			// Update settings manager with [Options] section.
			UpdateSettingsMap();
			// Load PAD controls.

			MainPanel.MainBodyPanel.PadControls = new PadControl[] {
				MainBodyPanel.Pad1Panel,
				MainBodyPanel.Pad2Panel,
				MainBodyPanel.Pad3Panel,
				MainBodyPanel.Pad4Panel,
			};
			for (var i = 0; i < MainPanel.MainBodyPanel.PadControls.Length; i++)
			{
				var pc = MainPanel.MainBodyPanel.PadControls[i];
				var mapTo = (MapTo)(i + 1);
				pc.InitControls(mapTo);
				pc.InitPadControl();
				// Update settings manager with [Mappings] section.
			}
			for (var i = 0; i < MainPanel.MainBodyPanel.PadControls.Length; i++)
				MainPanel.MainBodyPanel.PadControls[i].InitPadData();
			// Initialize pre-sets. Execute only after name of cIniFile is set.
			//SettingsDatabasePanel.InitPresets();
			// Allow events after PAD control are loaded.
			MainBodyPanel.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
			// Start capture setting change events.
			SettingsManager.Current.ResumeEvents();
		}

		/// <summary>
		/// This method will run continuously if form is not minimized.
		/// </summary>
		private void UpdateForm3()
		{
			// Allow if not testing or testing with option enabled.
			var o = SettingsManager.Options;
			var allow = !o.TestEnabled || o.TestUpdateInterface;
			if (!allow)
				return;
			Global.TriggerControlUpdates();
		}

		public void UpdateStatus(string message = "")
		{
			ControlsHelper.SetText(MainPanel.StatusTimerLabel, "Count: {0}, Reloads: {1}, Errors: {2} {3}",
				Program.TimerCount, Program.ReloadCount, Program.ErrorCount, message);
		}
		#endregion

		private bool HelpInit = false;

		private void MainTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (MainBodyPanel.MainTabControl.SelectedItem == MainBodyPanel.HelpTabPage && !HelpInit)
			{
				// Move this here so interface will load one second faster.
				HelpInit = true;
				ControlsHelper.SetTextFromResource(MainBodyPanel.HelpRichTextBox, "Documents.Help.rtf");
			}
			else if (MainBodyPanel.MainTabControl.SelectedItem == MainBodyPanel.SettingsTabPage)
			{
				var o = SettingsManager.Options;
				if (o.InternetFeatures && o.InternetAutoLoad)
				{
					//SettingsDatabasePanel.RefreshGrid(true);
				}
			}
			var tab = MainBodyPanel.MainTabControl.SelectedItem;
			//if (tab != null)
			//	SetHead(tab.);
		}

		#region ■ Check Files

		private void CheckEncoding(string path)
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

		#endregion

		#region ■ Issues Panel

		private readonly object issuesPanelLock = new object();

		private JocysCom.ClassLibrary.Controls.IssuesControl.IssuesControl IssuesPanel
			=> MainBodyPanel.IssuesPanel;

		private void InitIssuesPanel()
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
					new VirtualDeviceDriverIssue(),
					new HidGuardianDriverIssue()
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
		private bool FirstRunIsDone = false;

		private bool IssuesPanel_IsSuspended()
		{
			var o = SettingsManager.Options;
			var allow = (FormEventsEnabled || !FirstRunIsDone) && (!o.TestEnabled || o.TestCheckIssues);
			if (allow)
				FirstRunIsDone = true;
			return !allow;
		}

		// Remember previous has issues status.
		private int oldCriticalIssueCount;

		private void IssuesPanel_CheckCompleted(object sender, EventArgs e)
		{
			var checkDone = IssuesPanel.CriticalIssuesCount != null;
			// If check completed without issues then...
			var newCriticalIssuesCount = IssuesPanel.CriticalIssuesCount ?? 00;
			// If has new issues then...
			if (oldCriticalIssueCount == 0 && newCriticalIssuesCount > 0)
			{
				ControlsHelper.BeginInvoke(() =>
				{
					// Focus issues tab automatically.
					MainBodyPanel.MainTabControl.SelectedItem = MainBodyPanel.IssuesTabPage;
				});
			}
			oldCriticalIssueCount = newCriticalIssuesCount;
			// If Step 2 is still disabled and no critical issues found then...
			if (!update2Enabled.HasValue && checkDone && checkDone && newCriticalIssuesCount == 0)
				// Enabled update 2 step (Display PAD forms).
				update2Enabled = true;
		}

		#endregion

		#region ■ Device Form

		public UserDevice[] ShowDeviceForm()
		{
			var form = new Forms.UserDevicesWindow();
			var result = form.ShowDialog();
			return result.HasValue && result.Value
				? form.MainControl.GetSelected()
				: null;
		}

		#endregion

		#region ■ Update Form

		private Forms.UpdateWindow _UpdateWindow;
		private readonly object UpdateFormLock = new object();

		private void InitUpdateForm()
		{
			lock (UpdateFormLock)
				_UpdateWindow = new Forms.UpdateWindow();
		}

		private void DisposeUpdateForm()
		{
			lock (UpdateFormLock)
				_UpdateWindow = null;
		}

		public bool? ShowUpdateForm()
		{
			lock (UpdateFormLock)
			{
				if (_UpdateWindow == null)
					return null;
				var oldTab = MainBodyPanel.MainTabControl.SelectedItem;
				MainBodyPanel.MainTabControl.SelectedItem = MainBodyPanel.CloudTabPage;

				ControlsHelper.CenterWindowOnApplication(_UpdateWindow);
				_UpdateWindow.OpenDialog();
				ControlsHelper.CheckTopMost(_UpdateWindow);
				var result = _UpdateWindow.ShowDialog();
				_UpdateWindow.CloseDialog();
				MainBodyPanel.MainTabControl.SelectedItem = oldTab;
				return null;
			}
		}

		public void ProcessUpdateResults(CloudMessage results)
		{
			lock (UpdateFormLock)
			{
				if (_UpdateWindow == null)
					return;
				_UpdateWindow.Step2ProcessUpdateResults(results);
			}
		}

		#endregion

		public void Dispose()
		{
			StartHelper.Dispose();
			DisposeUpdateForm();
			DisposeInterfaceUpdate();
			if (Global.DHelper != null)
				Global.DHelper.Dispose();
		}

		#region ■ Update from DHelper

		private readonly object LockFormEvents = new object();

		// Set to false when main form is minimized, which will minimize CPU usage.
		public bool FormEventsEnabled;

		// Will be used to check it event handlers were called during form update period.
		private bool FormEventsDevicesUpdated;
		private bool FormEventsUpdateCompleted;
		private bool FormEventsFrequencyUpdated;

		public void EnableFormUpdates(bool enable)
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
			if (ControlsHelper.InvokeRequired)
			{
				var method = new EventHandler<EventArgs>(DHelper_DevicesUpdated);
				ControlsHelper.BeginInvoke(method, new object[] { sender, e });
				return;
			}
			SettingsManager.RefreshDeviceIsOnlineValueOnSettings(SettingsManager.UserSettings.Items.ToArray());
			ControlsHelper.SetText(MainPanel.UpdateDevicesStatusLabel, "D: {0}", Global.DHelper.RefreshDevicesCount);
		}

		private bool UpdateCompletedBusy;
		private readonly object UpdateCompletedLock = new object();
		private System.Diagnostics.Stopwatch InterfaceUpdateWatch;
		private long LastUpdateTime;

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
			if (ControlsHelper.InvokeRequired)
			{
				var method = new EventHandler<EventArgs>(DHelper_FrequencyUpdated);
				ControlsHelper.BeginInvoke(method, new object[] { sender, e });
				return;
			}
			ControlsHelper.SetText(MainPanel.UpdateFrequencyLabel, "Hz: {0}", Global.DHelper.CurrentUpdateFrequency);
		}

		#endregion

		#region ■ Update Interface

		private bool interfaceIsForeground;

		// Allow no more than 20 frames per second in foreground (make it look smooth and responsive).
		private readonly int interfaceUpdateForegroundFps = 20;

		// Allow no more than  5 frames per second in background (save CPU resources).
		private readonly int interfaceUpdateBackgroundFps = 5;

		private void InitiInterfaceUpdate()
		{
			Activated += MainForm_Activated;
			Deactivated += MainForm_Deactivated;
		}

		private void DisposeInterfaceUpdate()
		{
			Activated -= MainForm_Activated;
			Deactivated -= MainForm_Deactivated;
		}

		private void MainForm_Deactivated(object sender, EventArgs e)
		{
			interfaceIsForeground = false;
			ControlsHelper.SetText(MainPanel.FormUpdateFrequencyLabel, "Hz: {0}", interfaceUpdateBackgroundFps);
		}

		private void MainForm_Activated(object sender, EventArgs e)
		{
			interfaceIsForeground = true;
			ControlsHelper.SetText(MainPanel.FormUpdateFrequencyLabel, "Hz: {0}", interfaceUpdateForegroundFps);
		}

		#endregion

		public void ChangeCurrentGameEmulationType(EmulationType type)
		{
			var game = SettingsManager.CurrentGame;
			if (game == null)
				return;
			game.EmulationType = (int)type;
		}

		private Forms.ErrorReportWindow win;

		public void StatusErrorLabel_Click(object sender, EventArgs e)
		{
			win = new Forms.ErrorReportWindow();
			ControlsHelper.CheckTopMost(win);
			ControlsHelper.AutoSizeByOpenForms(win);
			win.Width = Math.Min(1450, SystemParameters.WorkArea.Width - 200);
			// Suspend displaying cloud queue results, because ShowDialog locks UI updates in the back.
			Global.DHelper.Stop();
			FormEventsEnabled = false;
			MainBodyPanel.CloudPanel.EnableDataSource(false);
			win.ErrorReportPanel.SendMessages += win.ErrorReportPanel_SendMessages;
			win.ErrorReportPanel.ClearErrors += ErrorReportPanel_ClearErrors;
			Global.CloudClient.TasksTimer.Queue.ListChanged += Queue_ListChanged;
			var result = win.ShowDialog();
			Global.CloudClient.TasksTimer.Queue.ListChanged -= Queue_ListChanged;
			win.ErrorReportPanel.SendMessages -= win.ErrorReportPanel_SendMessages;
			win.ErrorReportPanel.ClearErrors -= ErrorReportPanel_ClearErrors;
			MainBodyPanel.CloudPanel.EnableDataSource(true);
			if (Global.AllowDHelperStart)
			{
				FormEventsEnabled = true;
				Global.DHelper.Start();
			}
		}

		private void ErrorReportPanel_ClearErrors(object sender, EventArgs e)
		{
			ErrorsHelper.ClearErrors();
		}

		private void Queue_ListChanged(object sender, ListChangedEventArgs e)
		{
			var w = win;
			var q = Global.CloudClient.TasksTimer.Queue;
			if (win != null && q != null)
			{
				var item = q.FirstOrDefault(x => x.Action == CloudAction.SendMailMessage);
				ControlsHelper.Invoke(new Action(() =>
				{
					win.ErrorReportPanel.StatusLabel.Content = item == null ? "Message Delivered" : "Sending...";
				}));
			}
		}

		#region ■ Exception Handling and Reporting

		private void LogHelper_Current_NewException(object sender, EventArgs e)
		{
			ControlsHelper.BeginInvoke(ErrorsHelper.UpdateStatusErrorsLabel);
		}



		#endregion

	}
}
