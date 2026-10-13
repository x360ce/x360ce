using JocysCom.ClassLibrary;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using x360ce.Engine;
using System.Runtime.CompilerServices;

namespace x360ce.App
{
	/// <summary>
	/// Global class to host all services except interface.
	/// </summary>
	public static partial class Global
	{

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

		#region Global Services

		public static Service.RemoteService RemoteServer;
		static Engine.ForegroundWindowHook WindowHook;


		public static void InitializeServices()
		{
			RemoteServer = new Service.RemoteService();
			WindowHook = new ForegroundWindowHook();
			WindowHook.OnActivate += WindowHook_OnActivate;
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
			StartPlugins();
			// Add event which will start and stop UDP server depending on options.
			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
			// The door and the tunnel open and close as the AI settings change.
			SettingsManager.AiOptions.PropertyChanged += AiOptions_PropertyChanged;
		}

		/// <summary>Loads the plugins, before the first device list is read, so the devices they add are found online.</summary>
		static void StartPlugins()
		{
			var manager = new Plugins.PluginManager(
				System.IO.Path.Combine(EngineHelper.AppDataPath, Plugins.PluginManager.FolderName),
				Plugins.PluginManager.GetSigner(System.Windows.Forms.Application.ExecutablePath),
				() => SettingsManager.Options.AllowThirdPartyPlugins,
				typeof(Global).Assembly);
			Plugins.PluginManager.Current = manager;
			// A plugin found, loaded or switched off changes which devices are online, which the next device refresh says,
			// and what the AI skill's plugin documents say.
			manager.Changed += (s, e) =>
			{
				if (DHelper != null)
					DHelper.UpdateDevicesEnabled = true;
				Plugins.PluginDocs.UpdateReferences(manager);
			};
			manager.Start();
			Plugins.PluginDocs.UpdateReferences(manager);
		}

		private static void WindowHook_OnActivate(object sender, EventArgs<Process> e)
		{
			// The window in front keeps changing while this program shuts down, and each change used
			// to select a game into controls that were already being disposed.
			if (Program.IsClosing)
				return;
			SelectOpenGame();
		}

		public static string LastActivePath;

		public static void SelectOpenGame()
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
			// Select all games which are running (execept current app).
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
			SettingsManager.AiOptions.PropertyChanged -= AiOptions_PropertyChanged;
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
			RemoteServer.StopServer();
			WindowHook.IsEnabled = false;
		}

		#endregion

		/// <summary>
		/// Opens or closes the assistant's door to match the AI settings, then the tunnel that leads to it, then
		/// writes the AI settings file so the switches read the same level and token the window shows, switched off
		/// included. Safe to call again; it restarts the door. The door opens before the file is written, so a folder
		/// that cannot be written leaves the door usable and the Issues tab saying why the switches are not.
		/// </summary>
		public static void ApplyAiAccess()
		{
			var o = SettingsManager.AiOptions;
			JocysCom.ClassLibrary.Mcp.McpListener.Stop();
			if (o.AiAccessEnabled)
			{
				o.EnsureAiAccessToken();
				JocysCom.ClassLibrary.Mcp.McpListener.Start(o.AiAccessAddress, o.AiAccessPort, o.AiAccessToken, o.AiAccessTrustLocal);
			}
			ApplyAiTunnel();
		}

		/// <summary>
		/// Opens or closes the tunnel for ChatGPT on the web to match the AI settings, then writes the AI settings
		/// file. A tunnel already open with the same settings is left open.
		/// </summary>
		public static void ApplyAiTunnel()
		{
			JocysCom.ClassLibrary.Mcp.McpTunnel.Apply(new Mcp.AiAccessSettings());
			try
			{
				SettingsManager.AiOptionsData.Save();
			}
			catch (Exception ex)
			{
				// This runs on the interface thread from the settings-changed event; an exception here
				// would be a crash report for a folder that is read-only, which the Issues tab explains better.
				JocysCom.ClassLibrary.Mcp.McpListener.LastError = "the AI settings file could not be written (" + ex.Message + "). The /Mcp and /Ai switches read the level and token from it.";
			}
		}

		private static void AiOptions_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(AiOptions.AiAccessEnabled):
				case nameof(AiOptions.AiAccess):
				case nameof(AiOptions.AiAccessAddress):
				case nameof(AiOptions.AiAccessTrustLocal):
				case nameof(AiOptions.AiAccessPort):
					ApplyAiAccess();
					break;
				case nameof(AiOptions.TunnelEnabled):
				case nameof(AiOptions.TunnelId):
				case nameof(AiOptions.TunnelOrganization):
				case nameof(AiOptions.TunnelApiKey):
					ApplyAiTunnel();
					break;
			}
		}

		private static void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = SettingsManager.Options;
			switch (e.PropertyName)
			{
				case nameof(Options.RemoteEnabled):
					// If UDP server must be enabled then...
					if (o.RemoteEnabled)
						RemoteServer.StartServer();
					else
						RemoteServer.StopServer();
					break;
				case nameof(Options.AllowThirdPartyPlugins):
					// The plugins waiting for permission load at once. Taking it back takes effect on the next start:
					// a loaded library cannot be unloaded.
					if (o.AllowThirdPartyPlugins && Plugins.PluginManager.Current != null)
						Plugins.PluginManager.Current.Scan();
					break;
				case nameof(Options.PollingRate):
					// Applied here rather than from the options page, because the rate belongs to
					// the device loop and not to the control that happens to set it.
					if (DHelper != null)
						DHelper.Frequency = o.PollingRate;
					break;
				case nameof(Options.AutoDetectForegroundWindow):
					WindowHook.IsEnabled = o.AutoDetectForegroundWindow;
					break;
				default:
					break;
			}
		}

		#region Dinput Helper

		public static DInput.DInputHelper DHelper;

		public static void InitDHelperHelper()
		{
			// Initialize DInput Helper.
			DHelper = new DInput.DInputHelper();
			// The saved rate is applied here as well as on change: a change raised before the
			// loop exists cannot reach it, so without this the choice is forgotten on restart.
			DHelper.Frequency = SettingsManager.Options.PollingRate;
		}

		#endregion

		#region Public events

		/// <summary>
		/// Called from the main form's interface timer, when the engine has run a pass since the last drawing.
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

	}
}
