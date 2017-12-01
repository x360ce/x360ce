using System;
using System.Text.RegularExpressions;

namespace x360ce.Engine
{

	/// <summary>
	/// Convert setting value between Text (used to display in Controls) and INI.
	/// </summary>
	public static class SettingsConverter
	{

		// D-Pads Buttons:
		//  D-PAD 1  D-PAD2   D-PAD 3
		// [1,2,3,4][5,6,7,8][9,10,11,12]...
		static Regex textValueRegex = new Regex("^(?<type>Axis|IAxis|HAxis|IHAxis|Slider|ISlider|HSlider|IHSlider|Button|IButton|POV|IPOV) (?<num>[1-9][0-9]*)[ ]*(?<ext>Up|Left|Right|Down)?$");
		// Axis - a, HAxis - x, Slider - s, HSlider - h, Button - none, DPad - p, DPadButton - d;
		static Regex iniValueRegex = new Regex("^(?<type>[axshpd])?(?<neg>[-]*)?(?<num>[1-9][0-9]*)$");

		public static bool TryParseTextValue(string value, out SettingType type, out int index)
		{
			index = 0;
			type = SettingType.None;
			if (string.IsNullOrEmpty(value))
				return false;
			var m = textValueRegex.Match(value);
			if (m.Success)
			{
				index = int.Parse(m.Groups["num"].Value);
				// Index must be non zero.
				if (index == 0)
					return false;
				type = (SettingType)Enum.Parse(typeof(SettingType), m.Groups["type"].Value);
				// If type is DPad with extension then...
				if (type == SettingType.POV && m.Groups["ext"].Success)
				{
					// This is DPad button.
					type = SettingType.DPOVButton;
					switch (m.Groups["ext"].Value)
					{
						case "Up": index = (index - 1) * 4 + 1; break;
						case "Right": index = (index - 1) * 4 + 2; break;
						case "Down": index = (index - 1) * 4 + 3; break;
						case "Left": index = (index - 1) * 4 + 4; break;
					}
				}
			}
			return m.Success;
		}

		public static bool TryParseIniValue(string value, out SettingType type, out int index)
		{
			index = 0;
			type = SettingType.None;
			if (string.IsNullOrEmpty(value))
				return false;
			// Try to convert setting from ini value.
			var m = iniValueRegex.Match(value);
			if (m.Success)
			{
				index = int.Parse(m.Groups["num"].Value);
				// Index must be non zero.
				if (index == 0)
					return false;
				string t = m.Groups["type"].Value;
				string n = m.Groups["neg"].Value;
				switch (t)
				{
					case SettingName.SType.Axis:
						type = n == "-" ? SettingType.IAxis : SettingType.Axis;
						break;
					case SettingName.SType.Slider:
						type = n == "-" ? SettingType.ISlider : SettingType.Slider;
						break;
					case SettingName.SType.HAxis:
						type = n == "-" ? SettingType.IHAxis : SettingType.HAxis;
						break;
					case SettingName.SType.HSlider:
						type = n == "-" ? SettingType.IHSlider : SettingType.HSlider;
						break;
					case SettingName.SType.POV:
						type = n == "-" ? SettingType.IPOV : SettingType.POV;
						break;
					case SettingName.SType.POVButton:
						type = n == "-" ? SettingType.IPOVButton : SettingType.DPOVButton;
						break;
					default:
						type = n == "-" ? SettingType.IButton : SettingType.Button;
						break;
				}
			}
			return m.Success;
		}

		/// <summary>
		/// Convert setting to INI value.
		/// </summary>
		public static string ToIniValue(SettingType type, int index)
		{
			switch (type)
			{
				case SettingType.Button: return string.Format("{0}{1}", SettingName.SType.Button, index);
				case SettingType.IButton: return string.Format("{0}{1}", SettingName.SType.Button, -index);
				case SettingType.Axis: return string.Format("{0}{1}", SettingName.SType.Axis, index);
				case SettingType.IAxis: return string.Format("{0}{1}", SettingName.SType.Axis, -index);
				case SettingType.HAxis: return string.Format("{0}{1}", SettingName.SType.HAxis, index);
				case SettingType.IHAxis: return string.Format("{0}{1}", SettingName.SType.HAxis, -index);
				case SettingType.Slider: return string.Format("{0}{1}", SettingName.SType.Slider, index);
				case SettingType.ISlider: return string.Format("{0}{1}", SettingName.SType.Slider, -index);
				case SettingType.HSlider: return string.Format("{0}{1}", SettingName.SType.HSlider, index);
				case SettingType.IHSlider: return string.Format("{0}{1}", SettingName.SType.HSlider, -index);
				case SettingType.POV: return string.Format("{0}{1}", SettingName.SType.POV, index);
				case SettingType.IPOV: return string.Format("{0}{1}", SettingName.SType.POV, -index);
				case SettingType.DPOVButton: return string.Format("{0}{1}", SettingName.SType.POVButton, index);
				case SettingType.IPOVButton: return string.Format("{0}{1}", SettingName.SType.POVButton, -index);
				default: return "";
			}
		}

		/// <summary>Convert Text value to INI value.</summary>
		public static string ToIniValue(string textValue)
		{
			var index = 0;
			var type = SettingType.None;
			return TryParseTextValue(textValue, out type, out index)
				? ToIniValue(type, index)
				: "";
		}


		/// <summary>Convert INI value to Text value.</summary>
		public static string ToTextValue(string iniValue)
		{
			var index = 0;
			var type = SettingType.None;
			return TryParseIniValue(iniValue, out type, out index)
				? ToTextValue(type, index)
				: "";
		}


		/// <summary>
		/// Convert setting to text format for display to the user.
		/// </summary>
		/// <returns></returns>
		public static string ToTextValue(SettingType type, int index)
		{
			var s = "";
			if (type == SettingType.DPOVButton)
			{
				var dPadNames = Enum.GetNames(typeof(DPadEnum));
				// Zero-based D-Pad Button Index [0-3];
				var dPadButtonIndex = ((index - 1) % 4);
				var dPadButtonName = dPadNames[dPadButtonIndex];
				// Zero based D-Pad Index.
				var dPadIndex = ((index - 1) - dPadButtonIndex) / dPadNames.Length;
				s = string.Format("{0} {1} {2}", SettingType.POV, dPadIndex + 1, dPadButtonName);
			}
			else if (type != SettingType.None)
			{
				s = string.Format("{0} {1}", type, index);
			}
			return s;
		}

	}
}
