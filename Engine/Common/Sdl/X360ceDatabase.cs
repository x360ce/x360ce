using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>One controller of x360ce's controller database: its product, how many people use its setting, and the setting.</summary>
	public sealed class X360ceDatabaseEntry
	{
		/// <summary>DirectInput's product GUID, vendor and product in front.</summary>
		public Guid ProductGuid;
		public string ProductName;
		public int Users;
		public PadSetting Setting;
	}

	/// <summary>
	/// x360ce's own controller database, <c>gamecontrollerdb_x360ce.windows.txt</c>: for each controller, the complete
	/// setting most people use with it, mapping, force feedback and dead zones, built in like SDL's database.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A row is written as SDL writes one, so <see cref="SdlMapping"/> reads it and <see cref="SdlDatabase"/> finds it:
	/// <c>{SDL USB GUID},{product name},users:{n},{x360ce.ini key}:{value},...,platform:Windows,</c>. The keys and values
	/// are x360ce.ini's (<see cref="PadSettingIni"/>), only those not at their default, so the xinput library reads a row
	/// with the same code that reads a controller's section. One row for each controller, sorted by vendor and product.
	/// </para>
	/// <para>
	/// The file is Engine\Resources\gamecontrollerdb_x360ce.windows.txt: the engine carries it for both programs, and the
	/// xinput library embeds the same file. scripts\Update-X360ceDatabase.ps1 writes it; the server builds it with
	/// <see cref="Write"/>. A copy the Update button saved in the settings folder wins, row by row.
	/// </para>
	/// </remarks>
	public static class X360ceDatabase
	{
		/// <summary>The database's file name, built in and in the settings folder.</summary>
		public const string FileName = "gamecontrollerdb_x360ce.windows.txt";

		/// <summary>The field that says how many people use a row's setting.</summary>
		public const string UsersField = "users";

		/// <summary>The line that says when and from where the file was made: "# Made: {date} from {source}".</summary>
		public const string MadePrefix = "# Made: ";

		#region Rows

		/// <summary>A controller's row.</summary>
		public static SdlMapping ToRow(X360ceDatabaseEntry entry)
		{
			var bytes = entry.ProductGuid.ToByteArray();
			var vendor = BitConverter.ToUInt16(bytes, 0);
			var product = BitConverter.ToUInt16(bytes, 2);
			var fields = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>(UsersField, entry.Users.ToString(CultureInfo.InvariantCulture)),
			};
			fields.AddRange(PadSettingIni.NonDefault(entry.Setting));
			return SdlMapping.Create(SdlGuid.ForDevice(vendor, product), entry.ProductName, fields);
		}

		/// <summary>A row's setting; the fields that are not x360ce.ini keys are left out.</summary>
		public static PadSetting ToPadSetting(SdlMapping row)
		{
			var ps = new PadSetting();
			if (row != null)
				foreach (var field in row.Fields)
					PadSettingIni.Set(ps, field.Key, field.Value);
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		/// <summary>A row as a preset of the program's lists: its controller, how many people use its setting, and the setting's checksum.</summary>
		public static Preset ToPreset(SdlMapping row, PadSetting setting)
		{
			return new Preset
			{
				ProductName = row.Name,
				ProductGuid = new Guid(string.Format(CultureInfo.InvariantCulture, "{0:x4}{1:x4}-0000-0000-0000-504944564944", row.Guid.ProductId, row.Guid.VendorId)),
				PadSettingChecksum = setting.PadSettingChecksum,
				Users = Users(row),
			};
		}

		/// <summary>How many people use a row's setting; nought when it does not say.</summary>
		public static int Users(SdlMapping row)
		{
			int users;
			return row != null && int.TryParse(row.Get(UsersField), NumberStyles.None, CultureInfo.InvariantCulture, out users) ? users : 0;
		}

		/// <summary>
		/// The database's text: a header, then one row for each controller that names a vendor and product and whose setting
		/// sets something, the one most people use when two have the same, sorted by vendor and product.
		/// </summary>
		/// <param name="entries">The controllers, each with its most used setting.</param>
		/// <param name="source">Where the settings came from, for the header.</param>
		/// <param name="date">When they were read.</param>
		/// <param name="note">A line more for the header, such as why the copy is partial; null for none.</param>
		public static string Write(IEnumerable<X360ceDatabaseEntry> entries, string source, DateTime date, string note = null)
		{
			var rows = new Dictionary<int, KeyValuePair<X360ceDatabaseEntry, SdlMapping>>();
			foreach (var entry in entries)
			{
				if (entry == null || entry.Setting == null)
					continue;
				// Only a product GUID that carries the vendor and product: theirs, then "PIDVID".
				var bytes = entry.ProductGuid.ToByteArray();
				if (Encoding.ASCII.GetString(bytes, 10, 6) != "PIDVID")
					continue;
				var key = BitConverter.ToUInt16(bytes, 0) << 16 | BitConverter.ToUInt16(bytes, 2);
				if (key == 0)
					continue;
				var setting = Copy(entry.Setting);
				Clean(setting);
				var clean = new X360ceDatabaseEntry { ProductGuid = entry.ProductGuid, ProductName = entry.ProductName, Users = entry.Users, Setting = setting };
				var row = ToRow(clean);
				// A setting with nothing set maps nothing: such a controller is left to SDL's database.
				if (row.Fields.Count < 2)
					continue;
				KeyValuePair<X360ceDatabaseEntry, SdlMapping> kept;
				if (!rows.TryGetValue(key, out kept) || kept.Key.Users < entry.Users)
					rows[key] = new KeyValuePair<X360ceDatabaseEntry, SdlMapping>(clean, row);
			}
			var sb = new StringBuilder();
			sb.Append("# x360ce's controller database: for each controller, the complete setting most people use with it.\n");
			sb.Append("# Rows as SDL's gamecontrollerdb.txt writes them: GUID,name,users:{people},{x360ce.ini key}:{value},...\n");
			sb.Append("# Only settings not at their default; a controller whose setting sets nothing is left to SDL's database.\n");
			sb.Append("# Force feedback on at strength 0 is written at 100; a missing motor period with force feedback on is\n");
			sb.Append("# written at x360ce 4's default, 160 ms left and 64 ms right.\n");
			sb.Append("# Do not edit by hand: scripts\\Update-X360ceDatabase.ps1 writes it from x360ce's server.\n");
			sb.Append(MadePrefix).Append(date.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(" from ").Append(source).Append('\n');
			sb.Append("# Rows: ").Append(rows.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
			if (!string.IsNullOrEmpty(note))
				sb.Append("# ").Append(note).Append('\n');
			foreach (var row in rows.OrderBy(x => (uint)x.Key))
				sb.Append(row.Value.Value.ToString()).Append('\n');
			return sb.ToString();
		}

		/// <summary>A copy of a setting.</summary>
		static PadSetting Copy(PadSetting ps)
		{
			var copy = new PadSetting();
			foreach (var p in typeof(PadSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance))
				if (p.PropertyType == typeof(string) && p.CanWrite)
					p.SetValue(copy, p.GetValue(ps, null) ?? "", null);
			return copy;
		}

		#endregion

		#region Cleanup

		/// <summary>
		/// Puts right what makes a stored setting misbehave: force feedback on at strength 0 runs at 100, and with force
		/// feedback on, a missing motor period takes x360ce 4's default.
		/// </summary>
		/// <remarks>The xinput library's own default period, 120 ms left and 60 ms right, differs; a row says its period.</remarks>
		public static void Clean(PadSetting ps)
		{
			if (ps.ForceEnable != "1")
				return;
			if (ps.ForceOverall == "0")
				ps.ForceOverall = "100";
			if (string.IsNullOrEmpty(ps.LeftMotorPeriod) || ps.LeftMotorPeriod == "0")
				ps.LeftMotorPeriod = SettingName.DefaultLeftMotorPeriod;
			if (string.IsNullOrEmpty(ps.RightMotorPeriod) || ps.RightMotorPeriod == "0")
				ps.RightMotorPeriod = SettingName.DefaultRightMotorPeriod;
		}

		#endregion

		#region Merge

		/// <summary>The settings of the layers, key by key: each setting from the first layer that holds it.</summary>
		/// <remarks>The order is a person's own settings, then x360ce's database, then SDL's; a null layer is passed over.</remarks>
		public static PadSetting Merge(params PadSetting[] layers)
		{
			var ps = new PadSetting();
			foreach (var p in typeof(PadSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (p.PropertyType != typeof(string) || !p.CanWrite)
					continue;
				foreach (var layer in layers)
				{
					var value = layer == null ? null : (string)p.GetValue(layer, null);
					if (!string.IsNullOrEmpty(value))
					{
						p.SetValue(ps, value, null);
						break;
					}
				}
			}
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		/// <summary>
		/// The setting a device gets when nobody has set it: x360ce's database row for it, with SDL's mapping in the boxes
		/// the row leaves empty; for a device only SDL knows, SDL's mapping. When that leaves force feedback off and the
		/// device has motors, force feedback is switched on at x360ce's defaults. Null when neither database knows the device.
		/// </summary>
		/// <remarks>
		/// A row that leaves force feedback off says nothing about it: its strengths and periods, often nought, were set while
		/// nothing used them, so they are left out. The xinput library does the same (Sdl::ReadRowKeys,
		/// Controller::ApplyAutoMapping) with its own defaults.
		/// </remarks>
		/// <param name="device">The device.</param>
		/// <param name="forceFeedback">Whether the device reports force feedback.</param>
		/// <param name="formulas">False for x360ce 3, which has no formulas (<see cref="SdlConverter.ToPadSetting"/>).</param>
		/// <param name="note">What filled the setting, in a sentence.</param>
		/// <param name="x360ce">x360ce's database; the program's own when null.</param>
		/// <param name="sdl">SDL's database; the program's own when null.</param>
		public static PadSetting AutoSetting(SdlDevice device, bool forceFeedback, bool formulas, out string note, SdlDatabase x360ce = null, SdlDatabase sdl = null)
		{
			note = "";
			if (device == null)
				return null;
			var row = (x360ce ?? Current).Find(device);
			var sdlRow = (sdl ?? SdlDatabase.Current).Find(device);
			var translation = sdlRow == null ? null : SdlConverter.ToPadSetting(sdlRow, device, formulas);
			var fromSdl = translation == null || translation.Count == 0 ? null : translation.Setting;
			if (row == null && fromSdl == null)
				return null;
			var skipped = translation == null ? "" : translation.SkippedText();
			var fromRow = row == null ? null : ToPadSetting(row);
			if (fromRow != null && fromRow.ForceEnable != "1")
				ClearForceFeedback(fromRow);
			note = row == null
				? "Filled from SDL's controller database, as " + sdlRow.Name + "." + (skipped.Length == 0 ? "" : " " + skipped)
				: "Filled from x360ce's controller database, as " + row.Name + (fromSdl == null ? "." : ", and from SDL's where it maps nothing.");
			var setting = Merge(fromRow, fromSdl);
			if (forceFeedback && setting.ForceEnable != "1")
			{
				setting.ForceEnable = "1";
				Clean(setting);
				setting.PadSettingChecksum = setting.CleanAndGetCheckSum();
			}
			return setting;
		}

		/// <summary>Empties a setting's force feedback values but whether it is on, so they read as their defaults.</summary>
		static void ClearForceFeedback(PadSetting ps)
		{
			ps.ForceType = "";
			ps.ForceOverall = "";
			ps.ForceSwapMotor = "";
			ps.LeftMotorPeriod = "";
			ps.LeftMotorStrength = "";
			ps.LeftMotorDirection = "";
			ps.RightMotorPeriod = "";
			ps.RightMotorStrength = "";
			ps.RightMotorDirection = "";
		}

		#endregion

		#region The database the programs use

		static readonly object LoadLock = new object();
		static volatile SdlDatabase _current;

		/// <summary>The database the program uses, read the first time it is asked for: the built-in rows, and the Update button's copy, which wins.</summary>
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

		/// <summary>Reads the database again the next time it is asked for, after the Update button saved a copy.</summary>
		public static void Reload()
		{
			_current = null;
			_index = null;
		}

		/// <summary>The rows of <see cref="Current"/> as the programs' preset lists hold them.</summary>
		sealed class Index
		{
			public Preset[] Presets;
			public Dictionary<Guid, PadSetting> Settings;
		}

		static volatile Index _index;

		static Index GetIndex()
		{
			var index = _index;
			if (index != null)
				return index;
			lock (LoadLock)
			{
				if (_index == null)
				{
					var settings = new Dictionary<Guid, PadSetting>();
					var presets = new List<Preset>();
					foreach (var row in Current.Mappings)
					{
						var ps = ToPadSetting(row);
						settings[ps.PadSettingChecksum] = ps;
						presets.Add(ToPreset(row, ps));
					}
					_index = new Index { Presets = presets.OrderByDescending(x => x.Users).ToArray(), Settings = settings };
				}
				return _index;
			}
		}

		/// <summary>One preset for each controller of the database, the most used first, as both programs' Most Popular lists show them.</summary>
		/// <remarks>Read from the database each time the program starts: the lists are not stored with the settings.</remarks>
		public static Preset[] Presets()
		{
			return GetIndex().Presets;
		}

		/// <summary>A copy of the database's setting with this checksum, to load; null when no row has it.</summary>
		/// <remarks>
		/// The first call reads every row's setting, a few hundred milliseconds. The empty checksum, a controller with no
		/// settings, is answered at once: no row sets nothing.
		/// </remarks>
		public static PadSetting GetSetting(Guid checksum)
		{
			PadSetting ps;
			if (checksum == Guid.Empty || !GetIndex().Settings.TryGetValue(checksum, out ps))
				return null;
			var copy = Copy(ps);
			copy.PadSettingChecksum = ps.PadSettingChecksum;
			return copy;
		}

		/// <summary>The Update button's copy in the settings folder.</summary>
		public static string SettingsFilePath
		{
			get { return Path.Combine(EngineHelper.AppDataPath, "Settings", FileName); }
		}

		/// <summary>The built-in copy's text, read from the engine's resource; empty when it does not carry it.</summary>
		public static string ReadBuiltIn()
		{
			using (var stream = EngineHelper.GetResourceStream(FileName))
			{
				if (stream == null)
					return "";
				using (var reader = new StreamReader(stream, Encoding.UTF8))
					return reader.ReadToEnd();
			}
		}

		/// <summary>The built-in rows and the settings folder's copy, merged; a file that cannot be read is left out.</summary>
		public static SdlDatabase Load()
		{
			var builtIn = SdlSource.Parse(ReadBuiltIn(), SdlSourceKind.BuiltIn);
			SdlSource file = null;
			try
			{
				if (File.Exists(SettingsFilePath))
					file = SdlSource.Parse(File.ReadAllText(SettingsFilePath), SdlSourceKind.Downloaded, SettingsFilePath);
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
			return SdlDatabase.Merge(builtIn, file);
		}

		/// <summary>When the file says it was made, from its header; null when it does not say.</summary>
		public static DateTime? MadeOn(string text)
		{
			foreach (var line in (text ?? "").Split('\n'))
			{
				var t = line.Trim();
				if (!t.StartsWith(MadePrefix, StringComparison.Ordinal))
					continue;
				DateTime date;
				var word = t.Substring(MadePrefix.Length).Split(' ')[0];
				return DateTime.TryParseExact(word, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out date)
					? date : (DateTime?)null;
			}
			return null;
		}

		#endregion
	}
}
