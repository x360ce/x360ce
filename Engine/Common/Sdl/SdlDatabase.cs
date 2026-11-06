using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace x360ce.Engine
{
	/// <summary>Where a set of SDL rows came from.</summary>
	public enum SdlSourceKind
	{
		/// <summary>The copy of SDL_GameControllerDB that ships inside the program.</summary>
		BuiltIn,
		/// <summary>A newer copy the update button fetched into the settings folder.</summary>
		Downloaded,
		/// <summary>A gamecontrollerdb.txt someone put in the settings folder.</summary>
		File,
		/// <summary>The SDL_GAMECONTROLLERCONFIG environment variable, which Steam sets for the games it starts.</summary>
		Environment,
	}

	/// <summary>One set of SDL rows: the text of a database file or of the environment variable, with the commit it was taken at.</summary>
	public sealed class SdlSource
	{
		public SdlSourceKind Kind { get; internal set; }

		/// <summary>The file read, or null.</summary>
		public string Path { get; private set; }

		/// <summary>The SDL_GameControllerDB commit the text was taken at, from its header line; null when it has none.</summary>
		public string Sha { get; private set; }

		/// <summary>When that commit was made, in UTC; null when the text has no header line.</summary>
		public DateTime? Date { get; private set; }

		/// <summary>
		/// The address the copy was downloaded from, from its address line; null when it has none: the built-in copy, a file a
		/// person put in the settings folder, or one a 4.29 build saved before the address was kept.
		/// </summary>
		public string Address { get; private set; }

		/// <summary>Its Windows rows, and those that name no platform, in the order written.</summary>
		public IList<SdlMapping> Mappings { get; private set; }

		/// <summary>The commit, shortened as GitHub shows it.</summary>
		public string ShortSha
		{
			get { return Sha == null ? null : Sha.Substring(0, Math.Min(7, Sha.Length)); }
		}

		/// <summary>The header line x360ce writes at the top of a database it ships or fetches: "# Commit: {sha} {date}".</summary>
		/// <remarks>
		/// A comment to SDL, which reads the rest of the file as it is. The built-in copy is the file the update button
		/// writes, compressed: to ship a newer one, press the button, then gzip the settings folder's gamecontrollerdb.txt,
		/// without its <see cref="AddressPrefix"/> line, into App.v4\Resources\gamecontrollerdb.txt.gz.
		/// </remarks>
		public const string HeaderPrefix = "# Commit: ";

		static readonly Regex HeaderRegex = new Regex(@"^#\s*Commit:\s*(?<sha>[0-9a-fA-F]{7,40})\s+(?<date>\S+)\s*$");

		/// <summary>The line x360ce writes under the header line of a database it downloads: "# Downloaded from: {address}".</summary>
		/// <remarks>
		/// A line of its own, so the header line reads as before and a copy saved without it still reads. Not "# Source:", which
		/// SDL_GameControllerDB writes at the top of its own file.
		/// </remarks>
		public const string AddressPrefix = "# Downloaded from: ";

		static readonly Regex AddressRegex = new Regex(@"^#\s*Downloaded from:\s*(?<address>\S+)\s*$");

		/// <summary>The text with its header line put first, and under it the address it was downloaded from, replacing any it had.</summary>
		public static string WithHeader(string text, string sha, DateTime date, string address = null)
		{
			var lines = (text ?? "").Replace("\r\n", "\n").Split('\n').Where(x => !HeaderRegex.IsMatch(x.Trim()) && !AddressRegex.IsMatch(x.Trim()));
			var header = HeaderPrefix + sha + " " + date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
			if (!string.IsNullOrEmpty(address))
				header += "\r\n" + AddressPrefix + address;
			return header + "\r\n" + string.Join("\r\n", lines);
		}

		/// <summary>Reads the rows of a database or of the environment variable.</summary>
		/// <remarks>
		/// The environment variable holds rows one to a line, as a file does. Rows for other platforms are left out
		/// before their fields are read, so the whole file costs little more than splitting it into lines.
		/// </remarks>
		public static SdlSource Parse(string text, SdlSourceKind kind, string path = null)
		{
			var source = new SdlSource { Kind = kind, Path = path };
			var mappings = new List<SdlMapping>();
			foreach (var raw in (text ?? "").Split('\n'))
			{
				var line = raw.Trim();
				if (line.Length == 0)
					continue;
				if (line[0] == '#')
				{
					var m = HeaderRegex.Match(line);
					DateTime date;
					if (m.Success && source.Sha == null && DateTime.TryParse(m.Groups["date"].Value, CultureInfo.InvariantCulture,
						DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out date))
					{
						source.Sha = m.Groups["sha"].Value.ToLowerInvariant();
						source.Date = date;
					}
					var a = AddressRegex.Match(line);
					if (a.Success && source.Address == null)
						source.Address = a.Groups["address"].Value;
					continue;
				}
				var at = line.IndexOf(",platform:", StringComparison.Ordinal);
				if (at >= 0 && string.Compare(line, at + 10, SdlMapping.WindowsPlatform, 0, SdlMapping.WindowsPlatform.Length, StringComparison.OrdinalIgnoreCase) != 0)
					continue;
				SdlMapping mapping;
				if (SdlMapping.TryParse(line, out mapping) && mapping.IsForWindows)
					mappings.Add(mapping);
			}
			source.Mappings = mappings.AsReadOnly();
			return source;
		}

		/// <summary>What the source is, in words: "built in, 2026-11-02, 07af1d5, 869 Windows rows", or for a copy downloaded, "downloaded from {address}, ...".</summary>
		public string Describe()
		{
			var parts = new List<string>();
			switch (Kind)
			{
				case SdlSourceKind.BuiltIn: parts.Add("built in"); break;
				case SdlSourceKind.Downloaded: parts.Add(Address == null ? "downloaded" : "downloaded from " + Address); break;
				case SdlSourceKind.File: parts.Add((Path == null ? SdlDatabase.FileName : System.IO.Path.GetFileName(Path)) + " in the settings folder"); break;
				case SdlSourceKind.Environment: parts.Add(SdlDatabase.EnvironmentVariable); break;
			}
			if (Date.HasValue)
				parts.Add(Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			if (Sha != null)
				parts.Add(ShortSha);
			parts.Add(string.Format(CultureInfo.InvariantCulture, "{0} Windows {1}", Mappings.Count, Mappings.Count == 1 ? "row" : "rows"));
			return string.Join(", ", parts);
		}
	}

	/// <summary>
	/// SDL's controller database, SDL_GameControllerDB: for each controller SDL knows, which of its buttons, axes and hats is
	/// which part of a gamepad. Only Windows rows are read.
	/// </summary>
	/// <remarks>
	/// Three sources are merged, a later one winning for a row with the same GUID (<see cref="SdlGuid.Key"/>): the copy
	/// built into the program; gamecontrollerdb.txt in the settings folder, when its commit is newer than the built-in's or
	/// it has no commit line, which means a person put it there; and the SDL_GAMECONTROLLERCONFIG environment variable,
	/// which Steam sets for the games it starts. The database is read once, the first time it is asked for, on whatever
	/// thread asks; the engine never asks.
	/// </remarks>
	public sealed class SdlDatabase
	{
		/// <summary>The database's file name, in SDL_GameControllerDB and in the settings folder.</summary>
		public const string FileName = "gamecontrollerdb.txt";

		/// <summary>The built-in copy's resource: the file, gzip compressed.</summary>
		public const string ResourceName = "gamecontrollerdb.txt.gz";

		/// <summary>The built-in copy's licence, as SDL_GameControllerDB gives it.</summary>
		public const string LicenceResourceName = "gamecontrollerdb.LICENSE";

		/// <summary>Where SDL_GameControllerDB lives.</summary>
		public const string ProjectUrl = "https://github.com/mdqinc/SDL_GameControllerDB";

		public const string EnvironmentVariable = "SDL_GAMECONTROLLERCONFIG";

		/// <summary>The sources merged, in the order merged: each later one wins.</summary>
		public IList<SdlSource> Sources { get; private set; }

		/// <summary>Every row, one for each GUID, from the source that won it, in the order the sources first wrote them.</summary>
		public IList<SdlMapping> Mappings { get; private set; }

		/// <summary>The newest dated source: the built-in copy, or a newer one downloaded. Null when none has a date.</summary>
		public SdlSource Version
		{
			get { return Sources.Where(x => x.Date.HasValue).OrderByDescending(x => x.Date.Value).FirstOrDefault(); }
		}

		/// <summary>The database in words, its version first: "built in, 2026-11-02, 07af1d5, 869 Windows rows; SDL_GAMECONTROLLERCONFIG, 2 Windows rows".</summary>
		public string Describe()
		{
			var version = Version;
			var sources = Sources.Where(x => x != version);
			if (version != null)
				sources = new[] { version }.Concat(sources);
			var text = string.Join("; ", sources.Select(x => x.Describe()));
			return text.Length == 0 ? "no rows" : text;
		}

		/// <summary>Merges sources, a later one winning for a row with the same GUID.</summary>
		public static SdlDatabase Merge(params SdlSource[] sources)
		{
			var list = sources.Where(x => x != null).ToList();
			var order = new List<string>();
			var rows = new Dictionary<string, SdlMapping>();
			foreach (var source in list)
			{
				foreach (var mapping in source.Mappings)
				{
					var key = mapping.Guid.Key;
					if (!rows.ContainsKey(key))
						order.Add(key);
					rows[key] = mapping;
				}
			}
			return new SdlDatabase
			{
				Sources = list.AsReadOnly(),
				Mappings = order.Select(x => rows[x]).ToList().AsReadOnly(),
			};
		}

		/// <summary>The source of a row: the last source that has it.</summary>
		public SdlSource SourceOf(SdlMapping mapping)
		{
			for (var i = Sources.Count - 1; i >= 0; i--)
				if (Sources[i].Mappings.Contains(mapping))
					return Sources[i];
			return null;
		}

		/// <summary>The row for a device: the one for its vendor and product whose name matches the device's best; null when SDL does not know it.</summary>
		public SdlMapping Find(SdlDevice device)
		{
			return FindAll(device).FirstOrDefault();
		}

		/// <summary>Every row for a device, the best first: the name nearest the device's, then its own GUID, then a later source, then the order written.</summary>
		public IList<SdlMapping> FindAll(SdlDevice device)
		{
			if (device == null)
				return new SdlMapping[0];
			var own = device.Guids();
			var found = new List<KeyValuePair<SdlMapping, int>>();
			for (var i = 0; i < Mappings.Count; i++)
			{
				var m = Mappings[i];
				var g = m.Guid;
				var exact = own.Contains(g.Key);
				var ids = g.HasIds && device.HasIds && g.VendorId == device.VendorId && g.ProductId == device.ProductId;
				if (exact || ids)
					found.Add(new KeyValuePair<SdlMapping, int>(m, i));
			}
			return found
				.OrderByDescending(x => NameScore(x.Key.Name, device.ProductName))
				.ThenByDescending(x => own.Contains(x.Key.Guid.Key))
				.ThenByDescending(x => Sources.IndexOf(SourceOf(x.Key)))
				.ThenBy(x => x.Value)
				.Select(x => x.Key)
				.ToList();
		}

		/// <summary>How alike two names are: equal, one holding the other, then the words they share.</summary>
		static int NameScore(string a, string b)
		{
			a = (a ?? "").Trim();
			b = (b ?? "").Trim();
			if (a.Length == 0 || b.Length == 0)
				return 0;
			if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
				return 1000;
			if (a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0)
				return 500;
			var separators = new[] { ' ', '-', '_', '(', ')', '/', '.' };
			var wordsA = a.ToLowerInvariant().Split(separators, StringSplitOptions.RemoveEmptyEntries);
			var wordsB = b.ToLowerInvariant().Split(separators, StringSplitOptions.RemoveEmptyEntries);
			return wordsA.Intersect(wordsB).Count();
		}

		#region The database the program uses

		static readonly object LoadLock = new object();
		static volatile SdlDatabase _current;

		/// <summary>The database the program uses, read the first time it is asked for.</summary>
		public static SdlDatabase Current
		{
			get
			{
				var current = _current;
				if (current != null)
					return current;
				lock (LoadLock)
				{
					if (_current == null)
						_current = Load();
					return _current;
				}
			}
		}

		/// <summary>Reads the database again the next time it is asked for, after the file in the settings folder changed.</summary>
		public static void Reload()
		{
			_current = null;
		}

		/// <summary>gamecontrollerdb.txt in the settings folder, beside the program's other settings files.</summary>
		public static string SettingsFilePath
		{
			get { return System.IO.Path.Combine(EngineHelper.AppDataPath, "Settings", FileName); }
		}

		/// <summary>The built-in copy's text, read straight from the program's resource; empty when the program does not carry it.</summary>
		public static string ReadBuiltIn()
		{
			using (var stream = EngineHelper.GetResourceStream(ResourceName))
			{
				if (stream == null)
					return "";
				using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
				using (var reader = new StreamReader(gzip, Encoding.UTF8))
					return reader.ReadToEnd();
			}
		}

		/// <summary>The built-in copy's licence; empty when the program does not carry it.</summary>
		public static string ReadLicence()
		{
			using (var stream = EngineHelper.GetResourceStream(LicenceResourceName))
			{
				if (stream == null)
					return "";
				using (var reader = new StreamReader(stream, Encoding.UTF8))
					return reader.ReadToEnd();
			}
		}

		/// <summary>Reads the three sources and merges them.</summary>
		/// <remarks>A file that cannot be read is left out, as one that is not there.</remarks>
		public static SdlDatabase Load()
		{
			var builtIn = SdlSource.Parse(ReadBuiltIn(), SdlSourceKind.BuiltIn);
			SdlSource file = null;
			var path = SettingsFilePath;
			try
			{
				if (File.Exists(path))
					file = ParseSettingsFile(File.ReadAllText(path), path, builtIn);
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
			var text = System.Environment.GetEnvironmentVariable(EnvironmentVariable);
			var environment = string.IsNullOrWhiteSpace(text) ? null : SdlSource.Parse(text, SdlSourceKind.Environment);
			return Merge(builtIn, file, environment);
		}

		/// <summary>The settings folder's file as a source: downloaded when it has a commit line, a person's own when not; null when its commit is no newer than the built-in's.</summary>
		/// <remarks>The file an older version of the program downloaded is passed over once the program carries a newer copy.</remarks>
		public static SdlSource ParseSettingsFile(string text, string path, SdlSource builtIn)
		{
			var source = SdlSource.Parse(text, SdlSourceKind.File, path);
			if (!source.Date.HasValue)
				return source;
			if (builtIn != null && builtIn.Date.HasValue && source.Date.Value <= builtIn.Date.Value)
				return null;
			source.Kind = SdlSourceKind.Downloaded;
			return source;
		}

		#endregion
	}
}
