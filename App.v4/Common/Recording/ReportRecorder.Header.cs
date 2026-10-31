using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Recording
{
	public sealed partial class ReportRecorder
	{
		#region Device labels

		// Writer thread only.
		readonly Dictionary<UserDevice, string> _labels = new Dictionary<UserDevice, string>();
		readonly Dictionary<Guid, string> _guidLabels = new Dictionary<Guid, string>();

		/// <summary>The device's name in the file: D1, D2, and so on, in the order the file first names them.</summary>
		/// <remarks>A device named for the first time by an event, not by the header, is described where it is first named.</remarks>
		string Label(UserDevice ud)
		{
			string label;
			if (_labels.TryGetValue(ud, out label))
				return label;
			label = NewLabel(ud);
			WriteLine(Stamp(Stopwatch.GetTimestamp()) + " " + DescribeDevice(ud));
			return label;
		}

		string NewLabel(UserDevice ud)
		{
			var label = "D" + (_labels.Count + 1).ToString(CultureInfo.InvariantCulture);
			_labels.Add(ud, label);
			_guidLabels[ud.InstanceGuid] = label;
			return label;
		}

		/// <summary>A device's label for a GUID in a message, or null when it names no device of the file.</summary>
		string LabelOf(Guid guid)
		{
			string label;
			return _guidLabels.TryGetValue(guid, out label) ? label : null;
		}

		/// <summary>One line on the device, by its label: what it is and how x360ce reads it, with no serial and no path.</summary>
		string DescribeDevice(UserDevice ud)
		{
			var text = new StringBuilder();
			text.Append(_labels[ud]).Append(": ").Append(ShareSafe.Clean(ud.ProductName));
			var vendor = ud.DevVendorId != 0 ? ud.DevVendorId : ud.HidVendorId;
			var product = ud.DevProductId != 0 ? ud.DevProductId : ud.HidProductId;
			if (vendor != 0 || product != 0)
				text.Append(", VID ").Append(vendor.ToString("X4", CultureInfo.InvariantCulture)).Append(" PID ").Append(product.ToString("X4", CultureInfo.InvariantCulture));
			text.Append(", ").Append(((SharpDX.DirectInput.DeviceType)ud.CapType).ToString().ToLowerInvariant());
			if (ud.IsKeyboard || ud.IsMouse)
				return text.Append(", its keys and buttons are not recorded").ToString();
			text.Append(", ").Append(Count(ud.CapAxeCount, "axis", "axes")).Append(", ").Append(Count(ud.CapButtonCount, "button", "buttons")).Append(", ").Append(Count(ud.CapPovCount, "hat", "hats"));
			if ((ud.CapFlags & (int)SharpDX.DirectInput.DeviceFlags.ForceFeedback) != 0)
				text.Append(", force feedback");
			text.Append(", read by ").Append(ud.InputSource);
			text.Append(ud.IsOnline ? ", online" : ", offline");
			if (!ud.IsEnabled)
				text.Append(", unticked on the Devices page");
			if (ud.IsHidden)
				text.Append(", hidden by HID Hide");
			var game = SettingsManager.CurrentGame;
			if (game != null)
			{
				var tabs = SettingsManager.GetDeviceTabs(game.FileName, ud.InstanceGuid);
				if (tabs.Length > 0)
					text.Append(", on ").Append(string.Join(" and ", tabs.Select(x => Pad((int)x - 1))));
			}
			var places = AppHelper.GetXInputPlaces(ud);
			if (!string.IsNullOrEmpty(places))
				text.Append(", XInput: ").Append(places);
			return text.ToString();
		}

		static string Count(int count, string one, string many)
		{
			return count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? one : many);
		}

		#endregion

		#region Header

		void WriteHeader()
		{
			WriteLine("x360ce recording, made to be posted with a bug report.");
			WriteLine("It leaves out names, folders, addresses and keys: devices are D1, D2 and so on, controllers C1 to C4,");
			WriteLine("and the keys and buttons of keyboards and mice are not recorded. Read it before you post it.");
			WriteLine("");
			WriteLine("Started: " + _startTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
			WriteLine("Holds: " + DescribeParts(Parts));
			var version = typeof(ReportRecorder).Assembly.GetName().Version;
			WriteLine("Program: x360ce " + version + ", " + (Environment.Is64BitProcess ? "64-bit" : "32-bit") + ", .NET " + Environment.Version);
			WriteLine("Windows: " + WindowsVersion() + ", " + Environment.ProcessorCount + " processors, display at " + _displayScale + " %");
			var vigem = VirtualDriverInstaller.GetInstalledViGEmBusVersion();
			WriteLine("Virtual controller driver: " + (vigem == null ? "not installed" : "ViGEmBus " + vigem));
			var hidHide = VirtualDriverInstaller.GetHidHideVersion();
			WriteLine("HID Hide: " + (hidHide == null ? "not installed" : ShareSafe.Clean(hidHide)));
			WriteLine("XInput read back with: " + (string.IsNullOrEmpty(SharpDX.XInput.Controller.LibraryName) ? "nothing yet" : ShareSafe.Clean(SharpDX.XInput.Controller.LibraryName))
				+ (SharpDX.XInput.Controller.IsLoaded ? ", loaded" : ", not loaded"));
			var engine = _engine;
			if (engine == null)
				WriteLine("Engine: not running");
			else
				WriteLine("Raw Input: " + (engine.RawInputError == null ? "reading" : "not started: " + ShareSafe.Clean(engine.RawInputError.Message)));
			WriteLine("Emulated controllers: " + (SettingsManager.Options.XInputEnabled ? "on" : "off (Enable XInput is unticked)"));
			var game = SettingsManager.CurrentGame;
			WriteLine("Game: " + (game == null ? "none selected" : ShareSafe.Clean(game.FileName) + ", emulation " + (EmulationType)game.EmulationType));
			// Every device, so a device that is not mapped but takes a controller's place shows too.
			var devices = SettingsManager.UserDevices.ItemsToArraySynchronized()
				.OrderByDescending(x => game != null && SettingsManager.GetDeviceTabs(game.FileName, x.InstanceGuid).Length > 0)
				.ThenByDescending(x => x.IsOnline)
				.ToArray();
			WriteLine("");
			WriteLine("Devices:");
			foreach (var ud in devices)
			{
				NewLabel(ud);
				WriteLine("  " + DescribeDevice(ud));
				_online[ud] = ud.IsOnline;
			}
			if (Has(RecordParts.Settings))
			{
				WriteLine("");
				WriteLine("Options:");
				foreach (var line in Values(SettingsManager.Options))
					WriteLine("  " + line);
				if (game != null)
				{
					WriteLine("");
					WriteLine("Game settings:");
					foreach (var line in Values(game))
						WriteLine("  " + line);
				}
				WriteControllers(game);
				_settingsKey = SettingsKey(game);
			}
			WriteLine("");
			WriteLine("Events, in seconds since the start:");
		}

		static string DescribeParts(RecordParts parts)
		{
			var names = new List<string>();
			if ((parts & RecordParts.Settings) != 0) names.Add("settings and devices");
			if ((parts & RecordParts.Input) != 0) names.Add("device input");
			if ((parts & RecordParts.Output) != 0) names.Add("controller output");
			if ((parts & RecordParts.Forces) != 0) names.Add("force feedback");
			if ((parts & RecordParts.Engine) != 0) names.Add("engine timing");
			if ((parts & RecordParts.Window) != 0) names.Add("window and errors");
			return names.Count == 0 ? "only this header" : string.Join(", ", names);
		}

		/// <summary>The Windows build, its version name and edition, read from the registry, where they are not altered for compatibility.</summary>
		static string WindowsVersion()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
			{
				if (key == null)
					return Environment.OSVersion.VersionString;
				var build = key.GetValue("CurrentBuild") as string;
				var ubr = key.GetValue("UBR");
				var name = key.GetValue("DisplayVersion") as string;
				var edition = key.GetValue("EditionID") as string;
				return "build " + build + (ubr == null ? "" : "." + ubr)
					+ (string.IsNullOrEmpty(name) ? "" : ", version " + name)
					+ (string.IsNullOrEmpty(edition) ? "" : ", " + edition);
			}
		}

		/// <summary>Each controller's rows and the mapping of each row, for the current game.</summary>
		void WriteControllers(UserGame game)
		{
			if (game == null)
				return;
			WriteLine("");
			WriteLine("Controllers:");
			for (var pad = 1; pad <= 4; pad++)
			{
				var rows = SettingsManager.GetSettings(game.FileName, (MapTo)pad);
				if (rows.Count == 0)
				{
					WriteLine("  " + Pad(pad - 1) + ": no device");
					continue;
				}
				foreach (var row in rows)
				{
					var ud = SettingsManager.UserDevices.ItemsToArraySynchronized().FirstOrDefault(x => x.InstanceGuid == row.InstanceGuid);
					var device = ud == null ? "a device not connected since" : Label(ud);
					WriteLine("  " + Pad(pad - 1) + ": " + device + (row.IsEnabled ? "" : " (row unticked)"));
					var ps = SettingsManager.GetPadSetting(row.PadSettingChecksum);
					if (ps != null)
						WriteLine("    " + string.Join("; ", Mapping(ps)));
				}
			}
		}

		/// <summary>Every setting of a controller row that is set, by name.</summary>
		static IEnumerable<string> Mapping(PadSetting ps)
		{
			foreach (var p in typeof(PadSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (p.PropertyType != typeof(string) || !p.CanRead || p.GetIndexParameters().Length > 0)
					continue;
				var value = p.GetValue(ps, null) as string;
				if (string.IsNullOrEmpty(value) || value == "0")
					continue;
				yield return p.Name + "=" + ShareSafe.Clean(value);
			}
		}

		/// <summary>Names of settings never written, whatever their value: they can hold a key, an address or a folder.</summary>
		static readonly Regex Private = new Regex("Token|Key$|Key[A-Z]|Password|Secret|Mail|Url|Uri|Path|Folder|Directory|User|Login|Account|Computer|Machine|Cookie|Session|Server|Address|Certificate|Etag", RegexOptions.CultureInvariant);

		/// <summary>The simple settings of an object, one "name = value" each, private ones left out.</summary>
		static IEnumerable<string> Values(object o)
		{
			return o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.OrderBy(x => x.Name, StringComparer.Ordinal)
				.Select(x => Value(o, x))
				.Where(x => x != null);
		}

		static string Value(object o, PropertyInfo p)
		{
			if (!p.CanRead || p.GetIndexParameters().Length > 0 || Private.IsMatch(p.Name))
				return null;
			var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
			if (!(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)))
				return null;
			object value;
			try
			{
				value = p.GetValue(o, null);
			}
			catch (TargetInvocationException)
			{
				return null;
			}
			var text = Convert.ToString(value, CultureInfo.InvariantCulture);
			if (text != null && text.Length > 100)
				text = text.Substring(0, 100) + "...";
			return p.Name + " = " + ShareSafe.Clean(text);
		}

		/// <summary>An option's value for a change line, or null for one that is private or not simple.</summary>
		static string OptionValue(string name)
		{
			var p = string.IsNullOrEmpty(name) ? null : typeof(Options).GetProperty(name);
			if (p == null)
				return null;
			var line = Value(SettingsManager.Options, p);
			return line == null ? null : line.Substring(name.Length + 3);
		}

		/// <summary>An error as its report gives it, with no path and no name, at most 40 lines.</summary>
		static string DescribeException(Exception ex)
		{
			var text = new StringBuilder(ex.ToString());
			foreach (System.Collections.DictionaryEntry item in ex.Data)
				text.AppendLine().Append(item.Key).Append(": ").Append(item.Value);
			var lines = ShareSafe.Clean(text.ToString()).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			return string.Join(Environment.NewLine + "    ", lines.Take(40));
		}

		#endregion

		#region Samples

		/// <summary>Whether each device was online when last looked at. Writer thread.</summary>
		readonly Dictionary<UserDevice, bool> _online = new Dictionary<UserDevice, bool>();

		/// <summary>What the settings were when last written, to see when they change. Writer thread.</summary>
		string _settingsKey;

		string _foreground;

		/// <summary>Writes each device that came, went or appeared since the last look. Writer thread.</summary>
		void SampleDevices()
		{
			foreach (var ud in SettingsManager.UserDevices.ItemsToArraySynchronized())
			{
				bool was;
				var known = _online.TryGetValue(ud, out was);
				if (known && was == ud.IsOnline)
					continue;
				_online[ud] = ud.IsOnline;
				var label = Label(ud);
				if (known)
					WriteLine(Stamp(Stopwatch.GetTimestamp()) + " " + label + (ud.IsOnline ? " connected" : " disconnected"));
			}
		}

		/// <summary>Writes the controllers again when a row or a mapping changed. Writer thread.</summary>
		void SampleSettings()
		{
			var game = SettingsManager.CurrentGame;
			var key = SettingsKey(game);
			if (key == _settingsKey)
				return;
			_settingsKey = key;
			WriteLine(Stamp(Stopwatch.GetTimestamp()) + " settings changed: game " + (game == null ? "none" : ShareSafe.Clean(game.FileName)));
			WriteControllers(game);
		}

		/// <summary>The game, and each of its rows with the checksum of its settings, which changes with any setting.</summary>
		static string SettingsKey(UserGame game)
		{
			if (game == null)
				return "";
			var rows = SettingsManager.GetSettings(game.FileName);
			return game.FileName + "|" + game.EmulationType + "|" + string.Join("|", rows.OrderBy(x => x.MapTo).ThenBy(x => x.InstanceGuid)
				.Select(x => x.MapTo + ":" + x.InstanceGuid + ":" + x.IsEnabled + ":" + x.PadSettingChecksum));
		}

		/// <summary>Writes the program in front when it changed, by its file name. Writer thread.</summary>
		void SampleForeground()
		{
			string name;
			try
			{
				var process = ForegroundWindowHook.GetActiveProcess();
				name = process == null ? "nothing" : process.ProcessName;
			}
			catch (InvalidOperationException)
			{
				name = "a program that has just closed";
			}
			catch (ArgumentException)
			{
				name = "a program that has just closed";
			}
			if (name == _foreground)
				return;
			_foreground = name;
			Note("in front: " + name);
		}

		[DllImport("user32.dll")]
		static extern int GetGuiResources(IntPtr hProcess, int uiFlags);

		/// <summary>Writes the window and drawing objects, handles, memory and threads x360ce holds. Writer thread.</summary>
		/// <remarks>Windows gives a program at most 10,000 window objects; past that, no window can be made and the program stops drawing.</remarks>
		void EveryTenSeconds()
		{
			if (!Has(RecordParts.Window))
				return;
			using (var process = Process.GetCurrentProcess())
			{
				var user = GetGuiResources(process.Handle, 1);
				var gdi = GetGuiResources(process.Handle, 0);
				Note("x360ce holds " + user.ToString("N0", CultureInfo.InvariantCulture) + " window objects, "
					+ gdi.ToString("N0", CultureInfo.InvariantCulture) + " drawing objects, "
					+ process.HandleCount.ToString("N0", CultureInfo.InvariantCulture) + " handles, "
					+ (process.PrivateMemorySize64 / 1024 / 1024).ToString("N0", CultureInfo.InvariantCulture) + " MB, "
					+ process.Threads.Count + " threads");
			}
		}

		#endregion
	}
}
