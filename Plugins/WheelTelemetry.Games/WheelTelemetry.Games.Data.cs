using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	/// <summary>A game's rows in x360ce's telemetry data, which x360ce gives the plugin: the plugin keeps no list of games of its own.</summary>
	internal static class GameData
	{
		/// <summary>Stands for the page's port in a step and in a file the plugin installs.</summary>
		public const string PortText = "{Port}";

		/// <summary>The port a game sends to unless the person sets another: the Wheel Telemetry plugin's.</summary>
		public const int DefaultPort = 37150;

		/// <summary>The game whose program file this is, by its name; null for a file the data does not name.</summary>
		public static ITelemetryGame Find(IPluginTelemetryData data, string gamePath)
		{
			var name = Path.GetFileName(gamePath ?? "");
			return data == null ? null : data.Games.FirstOrDefault(x => x.IsEnabled && Is(x.FileName, name));
		}

		public static ITelemetryGameStep[] Warnings(IPluginTelemetryData data, ITelemetryGame game)
		{
			return data.GameSteps.Where(x => x.TelemetryGameId == game.TelemetryGameId && x.IsWarning).OrderBy(x => x.StepOrder).ToArray();
		}

		public static ITelemetryGameStep[] Steps(IPluginTelemetryData data, ITelemetryGame game)
		{
			return data.GameSteps.Where(x => x.TelemetryGameId == game.TelemetryGameId && !x.IsWarning).OrderBy(x => x.StepOrder).ToArray();
		}

		public static ITelemetryGameMod[] Mods(IPluginTelemetryData data, ITelemetryGame game)
		{
			return data.GameMods.Where(x => x.TelemetryGameId == game.TelemetryGameId).OrderBy(x => x.ModOrder).ToArray();
		}

		public static ITelemetryGameFile[] Files(IPluginTelemetryData data, ITelemetryGame game)
		{
			return data.GameFiles.Where(x => x.TelemetryGameId == game.TelemetryGameId).ToArray();
		}

		/// <summary>The telemetry the game sends; null when the data has no such format.</summary>
		public static ITelemetryFormat Format(IPluginTelemetryData data, ITelemetryGame game)
		{
			return data.Formats.FirstOrDefault(x => x.TelemetryFormatId == game.TelemetryFormatId);
		}

		/// <summary>The first official page of the format's specification; null when its publisher has none.</summary>
		public static ITelemetryFormatSpec Spec(IPluginTelemetryData data, ITelemetryGame game)
		{
			return data.FormatSpecs.Where(x => x.TelemetryFormatId == game.TelemetryFormatId && !string.IsNullOrEmpty(x.Url)).OrderBy(x => x.SpecOrder).FirstOrDefault();
		}

		/// <summary>Program files are compared as Windows compares file names: without regard to case.</summary>
		static bool Is(string a, string b)
		{
			return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>The text of a file the plugin carries in its Resources folder, or null when it carries none so named.</summary>
		public static string ResourceText(string name)
		{
			var assembly = typeof(GameData).Assembly;
			var full = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(".Resources." + name, StringComparison.Ordinal));
			if (full == null)
				return null;
			using (var stream = assembly.GetManifestResourceStream(full))
			using (var reader = new StreamReader(stream))
				return reader.ReadToEnd();
		}

		/// <summary>
		/// Whether a mod is in the game's folder or parked out of it: found or parked with its version, not found, or found but not
		/// the one this game needs.
		/// </summary>
		/// <param name="parked">The folder the game's mods are parked in, or null when the game has none.</param>
		public static string ModState(ITelemetryGameMod mod, string folder, string parked)
		{
			var path = Path.Combine(folder, mod.DetectFile);
			var where = "Found ";
			if (!File.Exists(path) && parked != null && File.Exists(Path.Combine(parked, mod.DetectFile)))
			{
				path = Path.Combine(parked, mod.DetectFile);
				where = "Parked ";
			}
			if (!File.Exists(path))
				return "Not found: " + mod.DetectFile + ".";
			var info = FileVersionInfo.GetVersionInfo(path);
			var product = (info.ProductName ?? "").Trim();
			if (!string.IsNullOrEmpty(mod.DetectProduct) && product.IndexOf(mod.DetectProduct, StringComparison.OrdinalIgnoreCase) < 0)
				return where + mod.DetectFile + (product.Length == 0 ? "" : " of " + product) + ", which is not " + mod.Name + ". This game needs " + mod.Name + ".";
			var version = (info.FileVersion ?? "").Trim();
			return where + mod.DetectFile + (version.Length == 0 ? "" : " " + version) + ".";
		}
	}

	/// <summary>
	/// Keeps a game's mods out of its folder while it is not played with them, and puts them back to play story mode: for a game
	/// whose anti-cheat refuses mods, such as GTA V's BattlEye in GTA Online.
	/// </summary>
	/// <remarks>
	/// The files moved are those x360ce's data names: each mod's file, such as ScriptHookV.dll, its loader xinput1_4.dll and
	/// ScriptHookVDotNet.asi for GTA V Enhanced, and the files Install writes. Without the loader the game loads no mod at all, so
	/// a mod's other files, which the data does not name, stay where they are, untouched and unused.
	///
	/// They are parked in the plugin's own folder in x360ce's settings folder, outside the game's folder. BattlEye checks what
	/// the game loads, and finds nothing there to load; Steam's Verify integrity of game files checks only the game's own
	/// files, and never sees a file outside its folder. On another drive a move is a copy, then the original deleted.
	///
	/// A file is never lost: a move that Windows refuses, such as of a file the game still holds open, leaves it where it was,
	/// and it is moved again later. Only file paths are used here, never x360ce's data, so the moves run on any thread.
	/// </remarks>
	internal static class GameParking
	{
		/// <summary>The files moved, as paths in the game's folder: the mods' files, then the files Install writes.</summary>
		public static string[] Paths(IPluginTelemetryData data, ITelemetryGame game)
		{
			return GameData.Mods(data, game).Select(x => x.DetectFile)
				.Concat(GameData.Files(data, game).Select(x => x.TargetPath))
				.Where(x => !string.IsNullOrEmpty(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		/// <summary>The files Install writes, as paths in the game's folder.</summary>
		public static string[] InstalledPaths(IPluginTelemetryData data, ITelemetryGame game)
		{
			return GameData.Files(data, game).Select(x => x.TargetPath).Where(x => !string.IsNullOrEmpty(x)).ToArray();
		}

		/// <summary>Where a game's files are parked: a folder for the game's folder in the plugin's own folder, or null when x360ce gave it none.</summary>
		public static string Folder(string dataFolder, string gameFolder)
		{
			if (string.IsNullOrEmpty(dataFolder) || string.IsNullOrEmpty(gameFolder))
				return null;
			// The folder's name, and a short number made from its whole path, so two copies of a game in two libraries are kept
			// apart and the paths stay short: Grand Theft Auto V Enhanced 1A2B3C4D.
			var full = Path.GetFullPath(gameFolder).TrimEnd('\\');
			byte[] hash;
			using (var md5 = System.Security.Cryptography.MD5.Create())
				hash = md5.ComputeHash(Encoding.UTF8.GetBytes(full.ToUpperInvariant()));
			return Path.Combine(dataFolder, "Parked", Path.GetFileName(full) + " " + BitConverter.ToString(hash, 0, 4).Replace("-", ""));
		}

		/// <summary>True when Install put x360ce's files there: in the game's folder, or parked.</summary>
		public static bool WasInstalled(string[] installedPaths, string gameFolder, string parked)
		{
			return Count(gameFolder, installedPaths) > 0 || (parked != null && Count(parked, installedPaths) > 0);
		}

		/// <summary>How many of the files are in the folder.</summary>
		public static int Count(string folder, string[] paths)
		{
			return paths.Count(x =>
			{
				var path = FullPath(folder, x);
				return path != null && File.Exists(path);
			});
		}

		/// <summary>Moves the files from the game's folder into the parking folder; a copy parked before is replaced by the one from the game.</summary>
		/// <returns>The files Windows did not let x360ce move, which stay in the game's folder; empty when every file moved.</returns>
		public static string[] Park(string[] paths, string gameFolder, string parked)
		{
			return paths.Where(x => !Move(FullPath(gameFolder, x), FullPath(parked, x), true)).ToArray();
		}

		/// <summary>Moves the parked files back into the game's folder; a file already there is newer, so it stays, and its parked copy waits to be replaced.</summary>
		/// <returns>The files Windows did not let x360ce move, which stay parked; empty when every file moved.</returns>
		public static string[] Unpark(string[] paths, string gameFolder, string parked)
		{
			return paths.Where(x => !Move(FullPath(parked, x), FullPath(gameFolder, x), false)).ToArray();
		}

		/// <summary>Moves one file, or leaves both places as they were.</summary>
		/// <returns>True when the file moved, or there was nothing to move; false when Windows refused and the file stays where it was.</returns>
		static bool Move(string source, string target, bool replace)
		{
			if (source == null || target == null || !File.Exists(source))
				return true;
			if (File.Exists(target) && !replace)
				return true;
			Directory.CreateDirectory(Path.GetDirectoryName(target));
			// Moved beside the old copy first, so the old copy goes only once the new one is whole.
			var moving = File.Exists(target) ? target + ".x360ce-moving" : target;
			try
			{
				if (File.Exists(moving))
					File.Delete(moving);
				File.Move(source, moving);
				if (File.Exists(source))
				{
					// Onto another drive Windows copies, then cannot delete a file held open: the original stays, and the copy goes.
					File.Delete(moving);
					return false;
				}
				if (moving != target)
				{
					File.Delete(target);
					File.Move(moving, target);
				}
				return true;
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}

		/// <summary>A path in a folder; null for a path that leaves it.</summary>
		static string FullPath(string folder, string path)
		{
			if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
				return null;
			var root = Path.GetFullPath(folder).TrimEnd('\\') + "\\";
			var full = Path.GetFullPath(Path.Combine(root, path));
			return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
		}

		/// <summary>True while a program with the game's file name runs.</summary>
		public static bool IsRunning(string gamePath)
		{
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gamePath ?? ""));
			foreach (var process in processes)
				process.Dispose();
			return processes.Length > 0;
		}
	}

	/// <summary>Steam's copy of a game: its app id, from Steam's own record in the library, and the address that starts it.</summary>
	internal static class SteamGame
	{
		/// <summary>The Steam app id of the copy at this path, such as 3240220 for GTA V Enhanced; 0 when Steam did not install it.</summary>
		/// <remarks>
		/// Steam keeps a game in {library}\steamapps\common\{folder}, and records it in {library}\steamapps\appmanifest_{id}.acf,
		/// whose installdir is the folder's name.
		/// </remarks>
		public static int AppId(string gamePath)
		{
			if (string.IsNullOrEmpty(gamePath))
				return 0;
			var folder = Path.GetDirectoryName(gamePath);
			var common = Path.GetDirectoryName(folder ?? "");
			var steamapps = string.IsNullOrEmpty(common) ? null : Path.GetDirectoryName(common);
			if (string.IsNullOrEmpty(steamapps) || !Is(Path.GetFileName(common), "common") || !Is(Path.GetFileName(steamapps), "steamapps") || !Directory.Exists(steamapps))
				return 0;
			var name = Path.GetFileName(folder);
			foreach (var manifest in Directory.GetFiles(steamapps, "appmanifest_*.acf"))
			{
				string text;
				try
				{
					text = File.ReadAllText(manifest);
				}
				catch (IOException)
				{
					continue;
				}
				int id;
				if (Is(Value(text, "installdir"), name) && int.TryParse(Value(text, "appid"), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
					return id;
			}
			return 0;
		}

		/// <summary>The address that has Steam start the game with the arguments, such as steam://run/3240220//-nobattleye/.</summary>
		public static string RunUrl(int appId, string arguments)
		{
			var id = appId.ToString(CultureInfo.InvariantCulture);
			return string.IsNullOrEmpty(arguments) ? "steam://run/" + id : "steam://run/" + id + "//" + Uri.EscapeDataString(arguments) + "/";
		}

		/// <summary>A key's value in a Steam .acf file, such as "installdir"		"Grand Theft Auto V Enhanced"; empty when it has none.</summary>
		static string Value(string acf, string key)
		{
			var match = System.Text.RegularExpressions.Regex.Match(acf, "\"" + key + "\"\\s+\"([^\"]*)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			return match.Success ? match.Groups[1].Value : "";
		}

		static bool Is(string a, string b)
		{
			return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>
	/// Plays a game's story mode with its mods: puts them back, has Steam start the game with the game's launch arguments, waits
	/// until the game has closed, and parks the mods again. The wait outlives the page, which another game selected disposes.
	/// </summary>
	internal static class StoryMode
	{
		/// <summary>How long Steam may take to start the game: it can ask first, update the game, and start its launcher.</summary>
		/// <remarks>Not longer: until it has passed, the mods are in the game's folder, where a game started from Steam as usual would find them.</remarks>
		public static TimeSpan StartWait = TimeSpan.FromMinutes(5);

		/// <summary>How often the wait looks whether the game runs, and how long it must stay closed before the mods are parked.</summary>
		public static TimeSpan Poll = TimeSpan.FromSeconds(2);

		/// <summary>How many times a file Windows still holds after the game closed is tried again, a poll apart.</summary>
		public static int ParkTries = 30;

		static readonly object Lock = new object();

		/// <summary>What happens to each game, by its file's path: the wait under way or what went wrong last.</summary>
		static readonly Dictionary<string, string> States = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		static readonly HashSet<string> Waiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>Why the game's mods cannot be parked and put back for story mode, or null when they can.</summary>
		/// <remarks>Only for a game with mods, in a copy Steam installed: x360ce starts story mode through Steam.</remarks>
		public static string Unavailable(IPluginTelemetryData data, ITelemetryGame game, string gamePath, string dataFolder)
		{
			if (!GameInstaller.IsGame(game, gamePath))
				return PluginText.T("WheelTelemetry.Games.NoGameFolder", "No game folder.");
			if (GameData.Mods(data, game).Length == 0)
				return PluginText.T("WheelTelemetry.Games.NoMods", "The game needs no mods.");
			if (SteamGame.AppId(gamePath) == 0)
				return PluginText.T("WheelTelemetry.Games.NotSteam", "Only for a copy installed by Steam, and Steam has no record of this one. Start the game from its own launcher, as the warnings say.");
			if (string.IsNullOrEmpty(dataFolder))
				return PluginText.T("WheelTelemetry.Games.NoDataFolder", "x360ce gave the plugin no folder to park the mods in.");
			return null;
		}

		/// <summary>Where the game's mods are parked, or null when they are not: see <see cref="Unavailable"/>.</summary>
		public static string ParkingFolder(IPluginTelemetryData data, ITelemetryGame game, string gamePath, string dataFolder)
		{
			return Unavailable(data, game, gamePath, dataFolder) != null ? null : GameParking.Folder(dataFolder, Path.GetDirectoryName(gamePath));
		}

		/// <summary>What happens to the game now, or null when nothing does.</summary>
		public static string State(string gamePath)
		{
			lock (Lock)
			{
				string state;
				return States.TryGetValue(gamePath ?? "", out state) ? state : null;
			}
		}

		/// <summary>True while x360ce waits for the game to start or to close.</summary>
		public static bool IsWaiting(string gamePath)
		{
			lock (Lock)
				return Waiting.Contains(gamePath ?? "");
		}

		static void SetState(string gamePath, string state)
		{
			lock (Lock)
			{
				if (state == null)
					States.Remove(gamePath);
				else
					States[gamePath] = state;
			}
		}

		/// <summary>Parks the files now, and says what Windows did not let x360ce move.</summary>
		public static void Park(string gamePath, string[] paths, string parked)
		{
			var left = GameParking.Park(paths, Path.GetDirectoryName(gamePath), parked);
			SetState(gamePath, left.Length == 0 ? null : InUse(left));
		}

		/// <summary>
		/// Puts the parked files back and starts the game; then, on a thread of its own, waits until it has closed and parks them
		/// again. <see cref="State"/> says how it goes.
		/// </summary>
		/// <param name="start">Starts the game, such as by opening its Steam address; an exception from it says why it did not.</param>
		public static void Play(string gamePath, string[] paths, string parked, Action start)
		{
			bool again;
			lock (Lock)
			{
				again = Waiting.Contains(gamePath);
				Waiting.Add(gamePath);
			}
			var folder = Path.GetDirectoryName(gamePath);
			// Pressed again while the game has not started, such as after Steam's question was cancelled: the mods are back
			// already, and the wait goes on.
			if (!again)
			{
				var left = GameParking.Unpark(paths, folder, parked);
				if (left.Length > 0)
				{
					// Back into the parking folder with what did move, so the game never starts with half of its mods.
					GameParking.Park(paths, folder, parked);
					Done(gamePath, InUse(left));
					return;
				}
			}
			try
			{
				start();
			}
			catch (Exception ex)
			{
				if (!again)
				{
					GameParking.Park(paths, folder, parked);
					Done(gamePath, PluginText.T("WheelTelemetry.Games.NotStarted", "The game was not started:") + " " + ex.Message);
				}
				else
					SetState(gamePath, PluginText.T("WheelTelemetry.Games.NotStarted", "The game was not started:") + " " + ex.Message);
				return;
			}
			if (again)
				return;
			SetState(gamePath, PluginText.T("WheelTelemetry.Games.WaitingForStart", "Waiting for the game to start. When Steam asks whether to launch it with custom arguments, press Continue."));
			Wait(gamePath, paths, parked, true);
		}

		/// <summary>Waits, on a thread of its own, until a game that runs now has closed, then parks the files.</summary>
		public static void ParkAfterExit(string gamePath, string[] paths, string parked)
		{
			lock (Lock)
			{
				if (Waiting.Contains(gamePath))
					return;
				Waiting.Add(gamePath);
			}
			Wait(gamePath, paths, parked, false);
		}

		static void Wait(string gamePath, string[] paths, string parked, bool starting)
		{
			var thread = new System.Threading.Thread(() =>
			{
				if (starting)
				{
					var until = DateTime.UtcNow + StartWait;
					while (!GameParking.IsRunning(gamePath) && DateTime.UtcNow < until)
						System.Threading.Thread.Sleep(Poll);
				}
				if (GameParking.IsRunning(gamePath))
					SetState(gamePath, PluginText.T("WheelTelemetry.Games.Running", "The game runs with its mods. x360ce takes them out of its folder when it closes."));
				// Closed, and still closed a poll later: a launcher can start the game again at once.
				while (true)
				{
					while (GameParking.IsRunning(gamePath))
						System.Threading.Thread.Sleep(Poll);
					System.Threading.Thread.Sleep(Poll);
					if (!GameParking.IsRunning(gamePath))
						break;
				}
				var folder = Path.GetDirectoryName(gamePath);
				var left = GameParking.Park(paths, folder, parked);
				for (var i = 0; left.Length > 0 && i < ParkTries; i++)
				{
					System.Threading.Thread.Sleep(Poll);
					left = GameParking.Park(paths, folder, parked);
				}
				Done(gamePath, left.Length == 0 ? null : InUse(left));
			})
			{
				IsBackground = true,
				Name = "WheelTelemetry.Games story mode",
			};
			thread.Start();
		}

		static void Done(string gamePath, string state)
		{
			lock (Lock)
			{
				Waiting.Remove(gamePath);
				if (state == null)
					States.Remove(gamePath);
				else
					States[gamePath] = state;
			}
		}

		static string InUse(string[] left)
		{
			return string.Format(PluginText.T("WheelTelemetry.Games.FilesInUse", "Windows did not let x360ce move {0}. A program may still hold it open: x360ce tries again when it starts next."), string.Join(", ", left));
		}
	}

	/// <summary>Installs and removes a game's files, touching nothing else: only the files its rows name.</summary>
	internal static class GameInstaller
	{
		/// <summary>Where a file goes; null for a path that leaves the game's folder.</summary>
		public static string FullPath(string folder, ITelemetryGameFile file)
		{
			if (string.IsNullOrEmpty(file.TargetPath) || Path.IsPathRooted(file.TargetPath))
				return null;
			var root = Path.GetFullPath(folder).TrimEnd('\\') + "\\";
			var full = Path.GetFullPath(Path.Combine(root, file.TargetPath));
			return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
		}

		/// <summary>True for the game's own program file, there where the Games tab says it is.</summary>
		public static bool IsGame(ITelemetryGame game, string gamePath)
		{
			return game != null && File.Exists(gamePath) && string.Equals(game.FileName, Path.GetFileName(gamePath), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Writes the game's files into its folder, the port put in where a file asks for it.</summary>
		/// <returns>Null when written, or why the game was refused. Windows' refusals, such as a folder that needs administrator rights, are thrown.</returns>
		public static string Install(IPluginTelemetryData data, ITelemetryGame game, string gamePath, int port)
		{
			if (!IsGame(game, gamePath))
				return "No " + (game == null ? "game x360ce knows" : game.FileName) + " at " + gamePath + ".";
			var folder = Path.GetDirectoryName(gamePath);
			var files = GameData.Files(data, game);
			// Every file checked before any is written, so a refusal writes nothing.
			foreach (var file in files)
			{
				if (FullPath(folder, file) == null)
					return file.TargetPath + " is not in the game’s folder.";
				if (GameData.ResourceText(file.ResourceName) == null)
					return "The plugin does not carry " + file.ResourceName + ".";
			}
			foreach (var file in files)
			{
				var path = FullPath(folder, file);
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				var text = GameData.ResourceText(file.ResourceName).Replace(GameData.PortText, port.ToString(CultureInfo.InvariantCulture));
				File.WriteAllText(path, text, new UTF8Encoding(false));
			}
			return null;
		}

		/// <summary>
		/// Deletes the game's files from its folder and from where they are parked, and nothing else; parked mods go back into the
		/// game's folder, where the person put them.
		/// </summary>
		/// <param name="parked">The folder the game's files are parked in, or null when the game has none.</param>
		/// <returns>Null when removed or not there, or why the game was refused.</returns>
		public static string Remove(IPluginTelemetryData data, ITelemetryGame game, string gamePath, string parked)
		{
			if (!IsGame(game, gamePath))
				return "No " + (game == null ? "game x360ce knows" : game.FileName) + " at " + gamePath + ".";
			var folder = Path.GetDirectoryName(gamePath);
			foreach (var file in GameData.Files(data, game))
			{
				foreach (var path in new[] { FullPath(folder, file), parked == null ? null : FullPath(parked, file) })
					if (path != null && File.Exists(path))
						File.Delete(path);
			}
			if (parked != null)
			{
				var left = GameParking.Unpark(GameParking.Paths(data, game), folder, parked);
				if (left.Length > 0)
					return string.Format(PluginText.T("WheelTelemetry.Games.NotMovedBack", "Windows did not let x360ce move {0} back into the game’s folder. It is in {1}."), string.Join(", ", left), parked);
			}
			return null;
		}

		/// <summary>How many of the game's files are in its folder.</summary>
		public static int Installed(IPluginTelemetryData data, ITelemetryGame game, string folder)
		{
			return GameData.Files(data, game).Count(x =>
			{
				var path = FullPath(folder, x);
				return path != null && File.Exists(path);
			});
		}

		/// <summary>The port the installed files send to: the number where a file the plugin carries has the port; the default when none says.</summary>
		/// <param name="parked">The folder the game's files are parked in, read when a file is not in the game's folder; null when the game has none.</param>
		public static int ReadPort(IPluginTelemetryData data, ITelemetryGame game, string folder, string parked)
		{
			foreach (var file in GameData.Files(data, game))
			{
				var path = FullPath(folder, file);
				if (path != null && !File.Exists(path) && parked != null)
					path = FullPath(parked, file);
				var template = GameData.ResourceText(file.ResourceName);
				if (path == null || template == null || !File.Exists(path))
					continue;
				// The text before the port on its line, such as "Port=", finds the line in the installed file.
				var line = template.Replace("\r", "").Split('\n').FirstOrDefault(x => x.Contains(GameData.PortText));
				if (line == null)
					continue;
				var before = line.Substring(0, line.IndexOf(GameData.PortText, StringComparison.Ordinal));
				foreach (var installed in File.ReadAllLines(path))
				{
					int port;
					if (before.Length > 0 && installed.StartsWith(before, StringComparison.Ordinal)
						&& int.TryParse(installed.Substring(before.Length).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out port))
						return port;
				}
			}
			return GameData.DefaultPort;
		}
	}
}
