using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
//using x360ce.App.Common.DInput;
using x360ce.App.Controls;
//using x360ce.App.DInput;
using x360ce.App.Issues;
using x360ce.Engine;
using x360ce.Engine.Data;
//using static JocysCom.ClassLibrary.Processes.MouseHelper;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
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
			InitHelper.InitTimer(this, InitializeComponent);
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
			StartHelper.Register(new WindowInteropHelper(this).Handle);
			var source = (HwndSource)PresentationSource.FromVisual(this);
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

		private void StartHelper_OnRestore(object sender, EventArgs e)
		{
			Global._TrayManager.RestoreFromTray(true);
		}

		private void StartHelper_OnClose(object sender, EventArgs e)
		{
			Close();
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
					Global.HMan.SetBodyError(e.Error.Message);
			});
		}

		/// <summary>
		/// Delay settings trough timer so interface will be more responsive on TrackBars.
		/// Or fast changes. Library will be reloaded as soon as user calms down (no setting changes in 500ms).
		/// </summary>
		private void Current_SettingChanged(object sender, SettingChangedEventArgs e)
		{
			var changed = SettingsManager.Current.ApplyAllSettingsToXML();
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
					Global.HMan.SetBodyError(e.Error.Message);
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
				if (update2Enabled == true)
				{
					update2Enabled = false;
					UpdateForm2();
					update3Enabled = true;
				}
				if (update3Enabled && IsHandleCreated)
				{
					update3Enabled = false;
					// Use this property to make sure that DHelper never starts unless all steps are fully initialized.
					Global.AllowDHelperStart = true;
					Global.DHelper.StartDInputService();
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
			// Update settings manager with [Options] section.
			UpdateSettingsMap();
			// Load PAD controls.
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
				IssuesPanel.IsSuspended = IssuesPanel_IsSuspended;
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
			form.Width = 900;
			form.Height = 900;
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
			Global.DHelper.StopDInputService();
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
				Global.DHelper.StartDInputService();
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

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			Global.HMan.SetBody(MessageBoxImage.Information, "Useful Tip: Minimize X360CE Application during game to reduce its load on CPU and GPU. Minimized application automatically turns off CPU heavy tasks like Interface updates or state requests from XInput.");
			StartHelper.OnClose += StartHelper_OnClose;
			StartHelper.OnRestore += StartHelper_OnRestore;
			AppHelper.InitializeHidGuardian();
			if (string.IsNullOrEmpty(System.Threading.Thread.CurrentThread.Name))
				System.Threading.Thread.CurrentThread.Name = "MainFormThread";
			Global.DHelper.DevicesUpdated += DHelper_DevicesUpdated;
			Global.DHelper.UpdateCompleted += DHelper_UpdateCompleted;
			Global.DHelper.FrequencyUpdated += DHelper_FrequencyUpdated;
			Global.DHelper.StatesRetrieved += DHelper_StatesRetrieved;
			Global.DHelper.XInputReloaded += DHelper_XInputReloaded;
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

		// PadControl:
		private PadControl _PadControl;
		private PadListControl _PadListControl;
		private PadItemControl _PadItemControl;
		private PadFootControl _PadFootControl;
		// PadItemControl:
		private PadItem_GeneralControl _PadItem_GeneralControl;
		private PadItem_DPadControl _PadItem_DPadControl;
		private AxisMapControl _AxisMapControl;
		private PadItem_ForceFeedbackControl _PadItem_ForceFeedbackControl;
		private PadItem_MacrosControl _PadItem_MacrosControl;
		private PadItem_DInputControl _PadItem_DInputControl;
		private PadItem_ForceFeedback_MotorControl _PadItem_ForceFeedback_MotorControl;
		private PadItem_General_XboxImageControl _PadItem_General_XboxImageControl;
		// Other:
		private UserProgramsControl _UserProgramsControl;
		private ProgramsControl _ProgramsControl;
		private UserDevicesControl _UserDevicesControl;
		private UserSettingListControl _UserSettingListControl;
		private AxisToButtonControl _AxisToButtonControl;
		private CloudControl _CloudControl;
		private DebugControl _DebugControl;
		private LogControl _LogControl;
		private OptionsGeneralControl _OptionsGeneralControl;
		private OptionsHidGuardianControl _OptionsHidGuardianControl;
		private OptionsInternetControl _OptionsInternetControl;
		private OptionsRemoteControllerControl _OptionsRemoteControllerControl;
		private OptionsVirtualDeviceControl _OptionsVirtualDeviceControl;
		private PresetsControl _PresetsControl;
		private PresetsListControl _PresetsListControl;
		private ProgramListControl _ProgramListControl;
		private SummariesListControl _SummariesListControl;
		private MainBodyControl _MainBodyControl;
		private MainControl _MainControl;

		private void Window_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;

			// Dispose managed resources of child UserControls:
			// PadControl:
			_PadListControl?.ParentWindow_Unloaded();
			_PadItemControl?.ParentWindow_Unloaded();
			_PadFootControl?.ParentWindow_Unloaded();
			_PadControl?.ParentWindow_Unloaded();
			// PadItemControl:
			_PadItem_GeneralControl?.ParentWindow_Unloaded();
			_PadItem_DPadControl?.ParentWindow_Unloaded();
			_AxisMapControl?.ParentWindow_Unloaded();
			_PadItem_ForceFeedbackControl?.ParentWindow_Unloaded();
			_PadItem_MacrosControl?.ParentWindow_Unloaded();
			_PadItem_DInputControl?.ParentWindow_Unloaded();
			_PadItem_ForceFeedback_MotorControl?.ParentWindow_Unloaded();
			_PadItem_General_XboxImageControl?.ParentWindow_Unloaded();
			// Other:
			_UserProgramsControl?.ParentWindow_Unloaded();
			_ProgramsControl?.ParentWindow_Unloaded();
			_UserDevicesControl?.ParentWindow_Unloaded();
			_UserSettingListControl?.ParentWindow_Unloaded();
			_AxisToButtonControl?.ParentWindow_Unloaded();
			_CloudControl?.ParentWindow_Unloaded();
			_DebugControl?.ParentWindow_Unloaded();
			_LogControl?.ParentWindow_Unloaded();
			_OptionsGeneralControl?.ParentWindow_Unloaded();
			_OptionsHidGuardianControl?.ParentWindow_Unloaded();
			_OptionsInternetControl?.ParentWindow_Unloaded();
			_OptionsRemoteControllerControl?.ParentWindow_Unloaded();
			_OptionsVirtualDeviceControl?.ParentWindow_Unloaded();
			_PresetsControl?.ParentWindow_Unloaded();
			_PresetsListControl?.ParentWindow_Unloaded();
			_ProgramListControl?.ParentWindow_Unloaded();
			_SummariesListControl?.ParentWindow_Unloaded();
			// Main:
			_MainBodyControl?.ParentWindow_Unloaded();
			_MainControl?.ParentWindow_Unloaded();

			// Cleanup references which prevents disposal.
			StartHelper.OnClose -= StartHelper_OnClose;
			StartHelper.OnRestore -= StartHelper_OnRestore;
			SettingsManager.Current.SettingChanged -= Current_SettingChanged;
			SettingsManager.Summaries.Items.ListChanged -= Summaries_ListChanged;
			SettingsManager.Current.ConfigLoaded -= Current_ConfigLoaded;
			Global.DHelper.DevicesUpdated -= DHelper_DevicesUpdated;
			Global.DHelper.UpdateCompleted -= DHelper_UpdateCompleted;
			Global.DHelper.FrequencyUpdated -= DHelper_FrequencyUpdated;
			Global.DHelper.StatesRetrieved -= DHelper_StatesRetrieved;
			Global.DHelper.XInputReloaded -= DHelper_XInputReloaded;
			SettingsManager.Current.NotifySettingsStatus = null;
			SettingsManager.SetSynchronizingObject(null);
			IssuesPanel.IsSuspended = null;
			IssuesPanel.CheckCompleted -= IssuesPanel_CheckCompleted;
			UpdateTimer.Elapsed -= UpdateTimer_Elapsed;
			UpdateTimer.Dispose();
			SettingsTimer.Elapsed -= SettingsTimer_Elapsed;
			SettingsTimer.Dispose();
			CleanStatusTimer.Elapsed -= CleanStatusTimer_Elapsed;
			CleanStatusTimer.Dispose();
			StartHelper.Dispose();
			DisposeUpdateForm();
			DisposeInterfaceUpdate();
			CollectGarbage();
		}

		static void CollectGarbage()
		{
			for (int i = 0; i < 4; i++)
			{
				GC.Collect(GC.MaxGeneration);
				GC.WaitForPendingFinalizers();
				GC.WaitForFullGCComplete();
				GC.Collect();
			}
		}

	}
}
