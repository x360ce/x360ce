using System;
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

		/// <summary>Whether a mod is in the game's folder: found with its version, not found, or found but not the one this game needs.</summary>
		public static string ModState(ITelemetryGameMod mod, string folder)
		{
			var path = Path.Combine(folder, mod.DetectFile);
			if (!File.Exists(path))
				return "Not found: " + mod.DetectFile + ". Unzip it into the game’s folder from its official page.";
			var info = FileVersionInfo.GetVersionInfo(path);
			var product = (info.ProductName ?? "").Trim();
			if (!string.IsNullOrEmpty(mod.DetectProduct) && product.IndexOf(mod.DetectProduct, StringComparison.OrdinalIgnoreCase) < 0)
				return "Found " + mod.DetectFile + (product.Length == 0 ? "" : " of " + product) + ", which is not " + mod.Name + ". This game needs " + mod.Name + ".";
			var version = (info.FileVersion ?? "").Trim();
			return "Found " + mod.DetectFile + (version.Length == 0 ? "" : " " + version) + ".";
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

		/// <summary>Deletes the game's files from its folder, and nothing else.</summary>
		/// <returns>Null when removed or not there, or why the game was refused.</returns>
		public static string Remove(IPluginTelemetryData data, ITelemetryGame game, string gamePath)
		{
			if (!IsGame(game, gamePath))
				return "No " + (game == null ? "game x360ce knows" : game.FileName) + " at " + gamePath + ".";
			var folder = Path.GetDirectoryName(gamePath);
			foreach (var file in GameData.Files(data, game))
			{
				var path = FullPath(folder, file);
				if (path != null && File.Exists(path))
					File.Delete(path);
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
		public static int ReadPort(IPluginTelemetryData data, ITelemetryGame game, string folder)
		{
			foreach (var file in GameData.Files(data, game))
			{
				var path = FullPath(folder, file);
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
