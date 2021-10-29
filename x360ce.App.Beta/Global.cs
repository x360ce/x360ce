using JocysCom.ClassLibrary;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using x360ce.Engine;
using JocysCom.ClassLibrary.Controls;

namespace x360ce.App
{
	/// <summary>
	/// Global class to host all services except interface.
	/// </summary>
	public static partial class Global
	{

		public static Service.LocalService _LocalService;
		public static Service.TrayManager _TrayManager;
		public static MainWindow _MainWindow;

		public static InfoControl HMan
			=> _MainWindow?.MainPanel?.InfoPanel;

		public static CloudClient CloudClient;

		public static void InitializeCloudClient()
		{
			CloudClient = new CloudClient();
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
		}

		public static void DisposeCloudClient()
		{
			CloudClient.Dispose();
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
		}

		#region ■ Global Services

		public static Service.RemoteService RemoteServer;
		static Engine.ForegroundWindowHook WindowHook;


		public static void InitializeServices()
		{
			RemoteServer = new Service.RemoteService();
			WindowHook = new ForegroundWindowHook();
			WindowHook.OnActivate += WindowHook_OnActivate;
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
			// Add event which will start and stop UDP server depending on options.
			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
		}

		private static void WindowHook_OnActivate(object sender, EventArgs<Process> e)
		{
			var process = e.Data;
			FindAndSetOpenGame();
		}

		public static string LastActivePath;

		public static void FindAndSetOpenGame()
		{
			// Get selected process.
			var activeProcess = ForegroundWindowHook.GetActiveProcess();
			var activePath = ForegroundWindowHook.GetProcessFileName(activeProcess);
			var allPaths = System.Diagnostics.Process.GetProcesses().Select(x => ForegroundWindowHook.GetProcessFileName(x))
				.Distinct()
				.ToArray();
			// Get list of all configured user games.
			var userGames = SettingsManager.UserGames.ItemsToArraySynchronized().ToList();
			var currentApp = userGames.FirstOrDefault(x => x.IsCurrentApp());
			if (currentApp != null)
				userGames.Remove(currentApp);
			// Select all games which are running (except current app).
			var runningGames = userGames
				.Where(x => allPaths.Any(a => string.Equals(x.FullPath, a, StringComparison.OrdinalIgnoreCase)))
				.ToArray();
			// If game was selected and still running then...
			if (!string.IsNullOrEmpty(LastActivePath) && runningGames.Any(x => x.FullPath == LastActivePath))
			{
				// Do nothing, because user could be trying to adjust mapping on the running game.
				return;
			}
			// Try to get game by active window (except current app).
			var game = runningGames
				.FirstOrDefault(x => string.Equals(x.FullPath, activePath, StringComparison.OrdinalIgnoreCase));
			// If not found then...
			if (game == null)
				// Try to get first currently running game (except current app).
				game = runningGames.FirstOrDefault(x => !x.IsCurrentApp());
			// If not found then...
			if (game == null)
				// Select current app.
				game = currentApp;
			LastActivePath = game?.FullPath;
			SettingsManager.UpdateCurrentGame(game);
		}

		public static void DisposeServices()
		{
			SettingsManager.Options.PropertyChanged -= Options_PropertyChanged;
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
			RemoteServer.StopServer();
			WindowHook.IsEnabled = false;
		}

		#endregion

		private static void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = SettingsManager.Options;
			switch (e.PropertyName)
			{
				case nameof(Options.PollingRate):
					DHelper.Frequency = o.PollingRate;
					break;
				case nameof(Options.RemoteEnabled):
					// If UDP server must be enabled then...
					if (o.RemoteEnabled)
						RemoteServer.StartServer();
					else
						RemoteServer.StopServer();
					break;
				case nameof(Options.AutoDetectForegroundWindow):
					WindowHook.IsEnabled = o.AutoDetectForegroundWindow;
					break;
				default:
					break;
			}
		}

		#region ■ DInput Helper

		public static bool AllowDHelperStart;

		public static DInput.DInputHelper DHelper;

		#endregion

		#region ■ Public events

		/// <summary>
		/// This method called from UpdateTimer on main form.
		/// </summary>
		public static void TriggerControlUpdates()
		{
			UpdateControlFromStates?.Invoke(null, null);
		}

		/// <summary>
		/// Update Form or Control from DInput and XInput states.
		/// </summary>
		public static event EventHandler UpdateControlFromStates;

		#endregion

		// Game control monitors this event, when user wants to add new game.
		public static event EventHandler AddGame;

		public static void OnAddGame(object sender)
			=> AddGame?.Invoke(sender, EventArgs.Empty);

	}
}
