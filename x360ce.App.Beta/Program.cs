using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using x360ce.App.Diagnostics;

namespace x360ce.App
{
	static partial class Program
	{

		public static bool IsDebug
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//TestMemoryLeak(typeof(JocysCom.ClassLibrary.Controls.IssuesControl.IssuesControl));
			//return;
			try
			{
				OperationalLog.InitializeDefault();
			}
			catch (Exception)
			{
				// A read-only or malformed profile must not prevent startup.
			}
			CaptureExceptions();
			// Fix: System.TimeoutException: The operation has timed out. at System.Windows.Threading.Dispatcher.InvokeImpl
			AppContext.SetSwitch("Switch.MS.Internal.DoNotInvokeInWeakEventTableShutdownListener", true);
			// First: Set working folder to the path of executable.
			FileInfo fi;
			using (MeasureStartup("working_directory"))
			{
				fi = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
				Directory.SetCurrentDirectory(fi.Directory.FullName);
			}
			// Prevent brave users from running this application from Windows folder.
			var winFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			if (fi.FullName.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
			{
				MessageBox.Show("Running from Windows folder is not allowed!\r\nPlease run this program from another folder.",
					"Windows Folder", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			// IMPORTANT: Make sure this class don't have any static references to x360ce.Engine library or
			// program tries to load x360ce.Engine.dll before AssemblyResolve event is available and fails.
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			try
			{
				using (MeasureStartup("start_app"))
					StartApp(args);
			}
			catch (Exception ex)
			{
				OperationalLog.Current?.WriteException("startup_unhandled_exception", ex);
				if (IsDebug)
					throw;
				var message = ExceptionToText(ex);
				if (message.Contains("Could not load file or assembly 'Microsoft.DirectX"))
				{
					message += "===============================================================\r\n";
					message += "You can download Microsoft DirectX from:\r\n";
					message += "http://www.microsoft.com/en-us/download/details.aspx?id=35";
				}
				var result = MessageBox.Show(message, "Exception!", MessageBoxButton.OKCancel, MessageBoxImage.Error, MessageBoxResult.OK);
				if (result == MessageBoxResult.Cancel)
					app?.Shutdown();
			}
			finally
			{
				OperationalLog.Current?.Dispose();
			}
		}

		public static void CaptureExceptions()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
		}

		private static void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
		{
			OperationalLog.Current?.WriteException("unobserved_task_exception", e.Exception);
			e.SetObserved();
		}

		private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
		{ // <- Put breakpoint here to capture exceptions during debug.
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			OperationalLog.Current?.WriteException("process_unhandled_exception", e.ExceptionObject as Exception);
		}

		public const string arg_WindowState = nameof(WindowState);

		internal class NativeMethods
		{
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			internal static extern bool SetProcessDPIAware();
		}

		static void StartApp(string[] args)
		{
			if (!RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully)
			{
				// Failed to enable useLegacyV2RuntimeActivationPolicy at runtime.
			}
			// Requires System.Configuration.Install reference.
			var ic = new System.Configuration.Install.InstallContext(null, args);
			// ------------------------------------------------
			// Administrator commands.
			// ------------------------------------------------
			bool executed;
			using (MeasureStartup("admin_commands"))
				executed = ProcessAdminCommands(args);
			// If valid command was executed then...
			if (executed)
				return;
			// ------------------------------------------------
			// If must open all setting folders then...
			if (ic.Parameters.ContainsKey("Settings"))
			{
				OpenSettingsFolder(ApplicationDataPath);
				OpenSettingsFolder(CommonApplicationDataPath);
				OpenSettingsFolder(LocalApplicationDataPath);
				return;
			}
			// If default application settings failed to load then... 
			using (MeasureStartup("default_settings"))
			{
				if (!CheckDefaultSettings())
					return;
			}
			// Options are small and required to establish process/window behavior.
			// All larger settings collections load after the first window is painted.
			using (MeasureStartup("options_load"))
				SettingsManager.LoadOptions();
			var o = SettingsManager.Options;
			// DPI aware property must be set before application window is created.
			if (Environment.OSVersion.Version.Major >= 6 && o.IsProcessDPIAware)
				NativeMethods.SetProcessDPIAware();
			using (MeasureStartup("global_services"))
				Global.InitializeServices();
			using (MeasureStartup("cloud_client"))
				Global.InitializeCloudClient();
			// Initialize DInput Helper.
			using (MeasureStartup("dinput_helper_construct"))
				Global.DHelper = new DInput.DInputHelper();
			if (ic.Parameters.ContainsKey("Exit"))
			{
				// Close all x360ce apps.
				StartHelper.BroadcastMessage(StartHelper.wParam_Close);
				return;
			}
			// Allow to run if multiple copies allowed or allow to restore window.
			var allowToRun = !o.AllowOnlyOneCopy || !StartHelper.BroadcastMessage(StartHelper.wParam_Restore);
			// If one copy is already opened then...
			if (allowToRun)
			{
				using (MeasureStartup("wpf_application"))
					app = new App();
				app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
				app.Startup += App_Startup;
				app.DispatcherUnhandledException += App_DispatcherUnhandledException;
				using (MeasureStartup("wpf_resources"))
					app.InitializeComponent();
				// Paint a dependency-free window before loading databases, permissions or devices.
				using (MeasureStartup("bootstrap_window"))
				{
					CreateStartupWindow();
				}
				// Now we can start the app.
				OperationalLog.Current?.Write("ui_dispatcher_started");
				app.Run();
			}
			Global.DisposeCloudClient();
			Global.DisposeServices();
		}

		static Window startupWindow;
		static TextBlock startupStatus;
		static readonly CancellationTokenSource startupCancellation = new CancellationTokenSource();

		static void CreateStartupWindow()
		{
			startupStatus = new TextBlock
			{
				Text = "Opening controller settings…",
				FontSize = 16,
				Foreground = Brushes.White,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
			};
			startupWindow = new Window
			{
				Title = "x360ce",
				Width = 440,
				Height = 150,
				ResizeMode = ResizeMode.NoResize,
				WindowStartupLocation = WindowStartupLocation.CenterScreen,
				Background = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
				Content = startupStatus,
			};
			startupWindow.Closing += (sender, e) => startupCancellation.Cancel();
			Application.Current.MainWindow = startupWindow;
			startupWindow.Show();
			OperationalLog.Current?.Write("startup_window_shown");
		}

		static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			OperationalLog.Current?.WriteException("ui_unhandled_exception", e.Exception);
		}

		static IDisposable MeasureStartup(string stage) =>
			OperationalLog.Current?.Measure(stage) ?? EmptyDisposable.Instance;

		sealed class EmptyDisposable : IDisposable
		{
			public static readonly EmptyDisposable Instance = new EmptyDisposable();
			public void Dispose() { }
		}

		// Application starts first time.
		private static async void App_Startup(object sender, StartupEventArgs e)
		{
			try
			{
				startupStatus.Text = "Loading mappings and controller settings…";
				await RunBackgroundStartupStage("settings_load", SettingsManager.LoadRemainingSettings);
				startupStatus.Text = "Preparing diagnostics…";
				await RunBackgroundStartupStage("local_service", InitializeServices);
				using (MeasureStartup("tray_icon"))
					InitializeTrayIcon();

				var o = SettingsManager.Options;
				var args = System.Environment.GetCommandLineArgs();
				var ic = new System.Configuration.Install.InstallContext(null, args);
				if (ic.Parameters.ContainsKey(arg_WindowState))
				{
					switch (ic.Parameters[arg_WindowState])
					{
						case nameof(WindowState.Maximized):
							RestoreMainWindow(true);
							break;
						case nameof(WindowState.Minimized):
							startupWindow.Hide();
							break;
						default:
							RestoreMainWindow(false);
							break;
					}
				}
				else
				{
					RestoreMainWindow(false);
				}
			}
			catch (OperationCanceledException)
			{
				app.Shutdown();
			}
			catch (Exception ex)
			{
				OperationalLog.Current?.WriteException("background_startup_failed", ex);
				startupStatus.Text = "Some settings could not be loaded. Opening with available data…";
				InitializeTrayIcon();
				RestoreMainWindow(false);
			}
		}

		static void RestoreMainWindow(bool maximize)
		{
			Global._TrayManager.MainWindowShown += (sender, e) => startupWindow.Hide();
			startupStatus.Text = "Opening mapping window…";
			Global._TrayManager.RestoreFromTray(false, maximize);
		}

		static async Task<bool> RunBackgroundStartupStage(string stage, Action<CancellationToken> action)
		{
			using (MeasureStartup(stage))
			{
				var completed = await StartupStageRunner.RunAsync(
					action, TimeSpan.FromSeconds(5), startupCancellation.Token);
				if (!completed)
				{
					OperationalLog.Current?.Write("startup_stage_slow", "warn",
						new System.Collections.Generic.Dictionary<string, object> { ["stage"] = stage });
					startupStatus.Text = "Continuing without " + stage.Replace('_', ' ') + "…";
				}
				return completed;
			}
		}

		#region Service, TrayIcon and UI

		static void InitializeServices(CancellationToken cancellationToken)
		{
			// Initialize non-UI service first.
			var localService = new Service.LocalService();
			localService.Start();
			if (cancellationToken.IsCancellationRequested)
			{
				localService.Stop();
				cancellationToken.ThrowIfCancellationRequested();
			}
			Global._LocalService = localService;
		}

		static void InitializeTrayIcon()
		{
			if (Global._TrayManager != null)
				return;
			// Initialize Tray Icon which will manage main window.
			Global._TrayManager = new Service.TrayManager();
			Global._TrayManager.OnExitClick += _TrayManager_OnExitClick;
			Global._TrayManager.OnWindowSizeChanged += _TrayManager_OnWindowSizeChanged;
			Global._TrayManager.InitMinimizeAndTopMost();
		}

		static void _TrayManager_OnWindowSizeChanged(object sender, System.EventArgs e)
		{
			if (app == null || Global._MainWindow == null)
				return;
			// Form GUI update is very heavy on CPU.
			// Enable form GUI update only if form is not minimized.
			var enableUpdates = Global._MainWindow.IsVisible &&
				Global._MainWindow.WindowState != WindowState.Minimized &&
				!Program.IsClosing;
			Global._MainWindow.EnableFormUpdates(enableUpdates);
		}

		static void _TrayManager_OnExitClick(object sender, System.EventArgs e)
		{
			IsClosing = true;
			// Remove tray icon first.
			Global._TrayManager.Dispose();
			app.Shutdown();
		}

		#endregion

		static App app;
		public static bool IsClosing;
		public static object DeviceLock = new object();
		public static int TimerCount = 0;
		public static int ReloadCount = 0;
		public static int ErrorCount = 0;

		public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			if (IsClosing)
				return;
			ErrorCount++;
			Global._MainWindow.UpdateTimer.Stop();
			Global._MainWindow.UpdateStatus("- " + e.Exception.Message);
			Global._MainWindow.UpdateTimer.Start();
		}

		static void OpenSettingsFolder(string path)
		{
			var di = new DirectoryInfo(path);
			//if (!di.Exists) return;
			//if (di.GetFiles().Length == 0) return;
			var psi = new ProcessStartInfo(di.Parent.Parent.FullName);
			psi.UseShellExecute = true;
			psi.ErrorDialog = true;
			Process.Start(psi);
		}

		static bool CheckDefaultSettings()
		{
			try
			{
				Properties.Settings.Default.Reload();
			}
			catch (ConfigurationErrorsException ex)
			{
				// Requires System.Configuration assembly.
				string filename = ((ConfigurationErrorsException)ex.InnerException).Filename;
				var title = "Corrupt user settings of " + Product;
				var text =
					"Program has detected that your user settings file has become corrupted. " +
					"This may be due to a crash or improper exiting of the program. " +
					"Program must reset your user settings in order to continue.\r\n" +
					"Click [Yes] to reset your user settings and continue.\r\n" +
					"Click [No] if you wish to exit and attempt manual repair.";
				var result = MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Error);
				if (result == MessageBoxResult.Yes)
				{
					File.Delete(filename);
					Properties.Settings.Default.Reload();
				}
				else
				{
					OpenSettingsFolder(ApplicationDataPath);
					OpenSettingsFolder(CommonApplicationDataPath);
					OpenSettingsFolder(LocalApplicationDataPath);
					return false;
				}
			}
			return true;
		}

		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
		{
			var dllName = e.Name.Contains(",") ? e.Name.Substring(0, e.Name.IndexOf(',')) : e.Name.Replace(".dll", "");
			Stream sr = null;
			switch (dllName)
			{
				case "ViGEmClient":
				case "x360ce.Engine":
				case "x360ce.Engine.XmlSerializers":
				case "SharpDX":
				case "SharpDX.DirectInput":
				case "SharpDX.RawInput":
					sr = GetResourceStream(dllName + ".dll");
					break;
				default:
					break;
			}
			if (sr == null)
				return null;
			var bytes = new byte[sr.Length];
			sr.Read(bytes, 0, bytes.Length);
			var asm = Assembly.Load(bytes);
			sr.Dispose();
			return asm;
		}

		/// <summary>
		/// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
		/// </summary>
		public static Stream GetResourceStream(string name)
		{
			var path = GetResourcePath(name);
			if (path == null)
				return null;
			var assembly = Assembly.GetEntryAssembly();
			if (assembly == null)
				return null;
			var sr = assembly.GetManifestResourceStream(path);
			return sr;
		}

		/// <summary>
		/// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
		/// </summary>
		public static string GetResourcePath(string name)
		{
			var assembly = Assembly.GetEntryAssembly();
			if (assembly == null)
				return null;
			var names = assembly.GetManifestResourceNames()
				.Where(x => x.EndsWith(name));
			var a = Environment.Is64BitProcess ? ".x64." : ".x86.";
			// Try to get by architecture first.
			var path = names.FirstOrDefault(x => x.Contains(a));
			if (!string.IsNullOrEmpty(path))
				return path;
			// Return first found.
			return names.FirstOrDefault();
		}

		#region ■ ExceptionToText

		// Exception to string needed here so that links to other references won't be an issue.

		static string ExceptionToText(Exception ex)
		{
			var message = "";
			AddExceptionMessage(ex, ref message);
			if (ex.InnerException != null) AddExceptionMessage(ex.InnerException, ref message);
			return message;
		}

		/// <summary>Add information about missing libraries and DLLs</summary>
		static void AddExceptionMessage(Exception ex, ref string message)
		{
			var ex1 = ex as ConfigurationErrorsException;
			var ex2 = ex as ReflectionTypeLoadException;
			var m = "";
			if (ex1 != null)
			{
				m += string.Format("FileName: {0}\r\n", ex1.Filename);
				m += string.Format("Line: {0}\r\n", ex1.Line);
			}
			else if (ex2 != null)
			{
				foreach (Exception x in ex2.LoaderExceptions) m += x.Message + "\r\n";
			}
			if (message.Length > 0)
			{
				message += "===============================================================\r\n";
			}
			message += ex.ToString() + "\r\n";
			foreach (var key in ex.Data.Keys)
			{
				m += string.Format("{0}: {1}\r\n", key, ex1.Data[key]);
			}
			if (m.Length > 0)
			{
				message += "===============================================================\r\n";
				message += m;
			}
		}

		#endregion

		#region GetInfo

		private static string ApplicationDataPath
			=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		private static string CommonApplicationDataPath
			=> Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
		private static string LocalApplicationDataPath
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		//private static string Company { get { return GetAttribute<AssemblyCompanyAttribute>(a => a.Company); } }
		private static string Product { get { return GetAttribute<AssemblyProductAttribute>(a => a.Product); } }

		// Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

		private static string GetAttribute<T>(Func<T, string> value) where T : Attribute
		{
			var asm = Assembly.GetExecutingAssembly();
			T attribute = (T)Attribute.GetCustomAttribute(asm, typeof(T));
			return attribute == null
				? ""
				: value.Invoke(attribute);
		}

		#endregion



	}
}
