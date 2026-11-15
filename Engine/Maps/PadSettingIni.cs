using System;
using System.Collections.Generic;
using System.Reflection;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>
	/// A controller's setting as x360ce.ini keeps it: each <see cref="PadSetting"/> property under the key the
	/// <see cref="SettingName"/> property of the same name gives, such as ButtonA under "A".
	/// </summary>
	/// <remarks>
	/// One conversion for x360ce 3, which writes and reads a controller's section with it, for the controller database
	/// writer, which writes a database row with it, and for the server, which builds that database. Values are kept as
	/// they are: a setting's text is already what x360ce.ini holds.
	/// </remarks>
	public static class PadSettingIni
	{
		/// <summary>The settings x360ce 3 keeps in a controller's section of x360ce.ini, in the order it writes them.</summary>
		public static readonly string[] Names =
		{
			nameof(PadSetting.AxisToDPadDeadZone), nameof(PadSetting.AxisToDPadEnabled), nameof(PadSetting.AxisToDPadOffset),
			nameof(PadSetting.ButtonA), nameof(PadSetting.ButtonB), nameof(PadSetting.ButtonGuide), nameof(PadSetting.ButtonBack),
			nameof(PadSetting.ButtonStart), nameof(PadSetting.ButtonX), nameof(PadSetting.ButtonY),
			nameof(PadSetting.DPad), nameof(PadSetting.DPadDown), nameof(PadSetting.DPadLeft), nameof(PadSetting.DPadRight), nameof(PadSetting.DPadUp),
			nameof(PadSetting.ForceEnable), nameof(PadSetting.ForceOverall), nameof(PadSetting.ForceSwapMotor), nameof(PadSetting.ForceType),
			nameof(PadSetting.GamePadType),
			nameof(PadSetting.LeftMotorPeriod), nameof(PadSetting.LeftMotorStrength), nameof(PadSetting.LeftMotorDirection),
			nameof(PadSetting.LeftShoulder),
			nameof(PadSetting.LeftThumbAntiDeadZoneX), nameof(PadSetting.LeftThumbAntiDeadZoneY),
			nameof(PadSetting.LeftThumbLinearX), nameof(PadSetting.LeftThumbLinearY),
			nameof(PadSetting.LeftThumbAxisX), nameof(PadSetting.LeftThumbAxisY), nameof(PadSetting.LeftThumbButton),
			nameof(PadSetting.LeftThumbDeadZoneX), nameof(PadSetting.LeftThumbDeadZoneY),
			nameof(PadSetting.LeftThumbDown), nameof(PadSetting.LeftThumbLeft), nameof(PadSetting.LeftThumbRight), nameof(PadSetting.LeftThumbUp),
			nameof(PadSetting.LeftTrigger), nameof(PadSetting.LeftTriggerDeadZone),
			nameof(PadSetting.PassThrough),
			nameof(PadSetting.RightMotorPeriod), nameof(PadSetting.RightMotorStrength), nameof(PadSetting.RightMotorDirection),
			nameof(PadSetting.RightShoulder),
			nameof(PadSetting.RightThumbAntiDeadZoneX), nameof(PadSetting.RightThumbAntiDeadZoneY),
			nameof(PadSetting.RightThumbLinearX), nameof(PadSetting.RightThumbLinearY),
			nameof(PadSetting.RightThumbAxisX), nameof(PadSetting.RightThumbAxisY), nameof(PadSetting.RightThumbButton),
			nameof(PadSetting.RightThumbDeadZoneX), nameof(PadSetting.RightThumbDeadZoneY),
			nameof(PadSetting.RightThumbDown), nameof(PadSetting.RightThumbLeft), nameof(PadSetting.RightThumbRight), nameof(PadSetting.RightThumbUp),
			nameof(PadSetting.RightTrigger), nameof(PadSetting.RightTriggerDeadZone),
			nameof(PadSetting.ButtonADeadZone), nameof(PadSetting.ButtonBDeadZone), nameof(PadSetting.ButtonBackDeadZone),
			nameof(PadSetting.ButtonStartDeadZone), nameof(PadSetting.ButtonXDeadZone), nameof(PadSetting.ButtonYDeadZone),
			nameof(PadSetting.LeftThumbButtonDeadZone), nameof(PadSetting.RightThumbButtonDeadZone),
			nameof(PadSetting.LeftShoulderDeadZone), nameof(PadSetting.RightShoulderDeadZone),
			nameof(PadSetting.DPadDownDeadZone), nameof(PadSetting.DPadLeftDeadZone), nameof(PadSetting.DPadRightDeadZone), nameof(PadSetting.DPadUpDeadZone),
		};

		static readonly Dictionary<string, PropertyInfo> ByKey = BuildKeys();

		/// <summary>Every setting that has an x360ce.ini key, by key, which is matched without regard to case as Windows reads ini files.</summary>
		static Dictionary<string, PropertyInfo> BuildKeys()
		{
			var keys = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in typeof(PadSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (p.PropertyType != typeof(string))
					continue;
				var key = typeof(SettingName).GetProperty(p.Name, BindingFlags.Public | BindingFlags.Static);
				if (key != null)
					keys[(string)key.GetValue(null, null)] = p;
			}
			return keys;
		}

		/// <summary>The x360ce.ini key of a setting, such as "A" for ButtonA.</summary>
		public static string Key(string name)
		{
			return (string)typeof(SettingName).GetProperty(name, BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
		}

		/// <summary>The setting an x360ce.ini key holds, such as ButtonA for "A"; null for a key that holds none.</summary>
		public static string NameOf(string key)
		{
			PropertyInfo p;
			return key != null && ByKey.TryGetValue(key, out p) ? p.Name : null;
		}

		/// <summary>Writes the settings x360ce 3 keeps, each under its key; an empty value is written empty.</summary>
		public static void Write(PadSetting ps, Action<string, string> set)
		{
			foreach (var name in Names)
				set(Key(name), (string)typeof(PadSetting).GetProperty(name).GetValue(ps, null));
		}

		/// <summary>Reads the settings x360ce 3 keeps from their keys; a key with no value reads as empty.</summary>
		public static PadSetting Read(Func<string, string> get)
		{
			var ps = new PadSetting();
			foreach (var name in Names)
				typeof(PadSetting).GetProperty(name).SetValue(ps, get(Key(name)) ?? "", null);
			return ps;
		}

		/// <summary>The mapping of a setting by x360ce.ini key (<see cref="SettingName.IsMapping"/>), an empty value for a box it leaves empty.</summary>
		public static Dictionary<string, string> MappingKeys(PadSetting ps)
		{
			var values = new Dictionary<string, string>();
			foreach (var name in Names)
			{
				var key = Key(name);
				if (SettingName.IsMapping(key))
					values[key] = (string)typeof(PadSetting).GetProperty(name).GetValue(ps, null) ?? "";
			}
			return values;
		}

		/// <summary>Sets the setting an x360ce.ini key holds; false for a key that holds none.</summary>
		public static bool Set(PadSetting ps, string key, string value)
		{
			PropertyInfo p;
			if (key == null || !ByKey.TryGetValue(key, out p))
				return false;
			p.SetValue(ps, value ?? "", null);
			return true;
		}

		/// <summary>The settings that are not at their default, as x360ce.ini keys and values, in key order.</summary>
		/// <remarks>Which settings and which defaults are the checksum's (<see cref="PadSetting.CleanAndGetCheckSum"/>), so the same settings read back give the same checksum.</remarks>
		public static List<KeyValuePair<string, string>> NonDefault(PadSetting ps)
		{
			var copy = new PadSetting();
			foreach (var p in ByKey.Values)
				p.SetValue(copy, p.GetValue(ps, null) ?? "", null);
			var lines = new List<string>();
			copy.CleanAndGetCheckSum(lines);
			var values = new List<KeyValuePair<string, string>>();
			foreach (var line in lines)
			{
				var at = line.IndexOf('=');
				values.Add(new KeyValuePair<string, string>(Key(line.Substring(0, at)), line.Substring(at + 1)));
			}
			values.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
			return values;
		}
	}
}
