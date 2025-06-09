using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;

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
			CaptureExceptions();
			// Fix: System.TimeoutException: The operation has timed out. at System.Windows.Threading.Dispatcher.InvokeImpl
			AppContext.SetSwitch("Switch.MS.Internal.DoNotInvokeInWeakEventTableShutdownListener", true);
			// First: Set working folder to the path of executable.
			var fi = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
			Directory.SetCurrentDirectory(fi.Directory.FullName);
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
				StartApp(args);
			}
			catch (Exception ex)
			{
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
					app.Shutdown();
			}
		}

		public static void CaptureExceptions()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
		}

		private static void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
		{ // <- Put breakpoint here to capture exceptions during debug.
		}

		private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
		{ // <- Put breakpoint here to capture exceptions during debug.
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{ // <- Put breakpoint here to capture exceptions during debug.
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
			var executed = ProcessAdminCommands(args);
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
			if (!CheckDefaultSettings())
				return;
			// Load all settings.
			SettingsManager.Load();
			var o = SettingsManager.Options;
			// DPI aware property must be set before application window is created.
			if (Environment.OSVersion.Version.Major >= 6 && o.IsProcessDPIAware)
				NativeMethods.SetProcessDPIAware();
			Global.InitializeServices();
			Global.InitializeCloudClient();
			// Initialize DInput Helper.
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
				InitializeServices();
				InitializeTrayIcon();
				app = new App();
				//app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
				app.Startup += App_Startup;
				app.InitializeComponent();
				// Create the main application window which will take minimum amount of memory.
				// Main application window is impossible to dispose until the application closes.
				// Important: .Owner property must be set to Application.Current.MainWindow for sub-window to dispose.
				var appWindow = new Window();
				appWindow.Title = "x360ceAppWindow";
				// Make sure it contains handle.
				var awHelper = new WindowInteropHelper(appWindow);
				awHelper.EnsureHandle();
				Application.Current.MainWindow = appWindow;
				// Now we can start the app.
				app.Run();
			}
			Global.DisposeCloudClient();
			Global.DisposeServices();
		}

		// Application starts first time.
		private static void App_Startup(object sender, StartupEventArgs e)
		{
			var o = SettingsManager.Options;
			var args = System.Environment.GetCommandLineArgs();
			var ic = new System.Configuration.Install.InstallContext(null, args);
			// If windows state parameter was passed then...
			if (ic.Parameters.ContainsKey(arg_WindowState))
			{
				switch (ic.Parameters[arg_WindowState])
				{
					case nameof(WindowState.Maximized):
						Global._TrayManager.RestoreFromTray(false, true);
						break;
					case nameof(WindowState.Minimized):
						Global._TrayManager.MinimizeToTray(false, o.MinimizeToTray);
						break;
				}
			}
			else
			{
				Global._TrayManager.RestoreFromTray(false, false);
			}
		}

		#region Service, TrayIcon and UI

		static void InitializeServices()
		{
			// Initialize non-UI service first.
			Global._LocalService = new Service.LocalService();
			Global._LocalService.Start();
		}

		static void InitializeTrayIcon()
		{
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
			var enableUpdates = app.MainWindow.WindowState != WindowState.Minimized && !Program.IsClosing;
			Global._MainWindow.EnableFormUpdates(enableUpdates);
		}

		static void _TrayManager_OnExitClick(object sender, System.EventArgs e)
		{
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
