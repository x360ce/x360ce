using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.App
{

	/// <summary>
	/// Convert setting value between Enums and INI string.
	/// </summary>
	public partial class SettingsConverter
	{

		public SettingsConverter(string value)
		{
			this.FromSetting(value);
		}

		public SettingsConverter(string value, string key)
		{
			this.FromSetting(value, key);
		}

		// Maybe index D-Pads buttons like [1,2,3,4][5,6,7,8][9,10,11,12]... and use 'BDpad' type. 
		static Regex textValueRegex = new Regex("^(?<type>Axis|IAxis|HAxis|IHAxis|Slider|ISlider|HSlider|IHSlider|Button|DPad) (?<num>[1-9][0-9]?)[ ]*(?<ext>Up|Left|Right|Down)?$");
		static Regex iniValueRegex = new Regex("^(?<type>[asxhpd])?(?<neg>[-]*)?(?<num>[1-9][0-9]?)$");

		int _Index;
		public int Index { get { return _Index; } }

		SettingType _Type;
		public SettingType Type { get { return _Type; } }

		public void FromSetting(string value)
		{
			FromSetting(value, string.Empty);
		}

		public static bool TryParseIndexAndType(string value, out int index, out SettingType type)
		{
			var m = textValueRegex.Match(value);
			index = 0;
			type = SettingType.None;
			if (m.Success)
			{
				type = (SettingType)Enum.Parse(typeof(SettingType), m.Groups["type"].Value);
				index = int.Parse(m.Groups["num"].Value);
				if (m.Groups["ext"].Success)
				{
					if (type == SettingType.DPad)
					{
						type = SettingType.DPadButton;
						switch (m.Groups["ext"].Value)
						{
							case "Up": index = (index - 1) * 4 + 1; break;
							case "Right": index = (index - 1) * 4 + 2; break;
							case "Down": index = (index - 1) * 4 + 3; break;
							case "Left": index = (index - 1) * 4 + 4; break;
						}
					}
				}
			}
			return m.Success;
		}

		public void FromSetting(string value, string key)
		{
			int index = 0;
			SettingType type = SettingType.None;
			var success = TryParseIndexAndType(value, out index, out type);
			if (success)
			{
				_Type = type;
				_Index = index;
			}
			// Try to convert setting from ini value.
			var m = iniValueRegex.Match(value);
			if (m.Success)
			{
				string t = m.Groups["type"].Value;
				string n = m.Groups["neg"].Value;
				_Index = int.Parse(m.Groups["num"].Value);
				if (key.Contains("Analog") && !key.Contains("Button") && t == "") t = SettingName.SType.Axis;
				if (key.Contains("D-pad POV")) t = SettingName.SType.DPad;
				if (t == SettingName.SType.Axis && Index == 7) _Index = 0;
				switch (t)
				{
					case SettingName.SType.Axis:
						_Type = n == "-" ? SettingType.IAxis : SettingType.Axis;
						break;
					case SettingName.SType.Slider:
						_Type = n == "-" ? SettingType.ISlider : SettingType.Slider;
						break;
					case SettingName.SType.HAxis:
						_Type = n == "-" ? SettingType.IHAxis : SettingType.HAxis;
						break;
					case SettingName.SType.HSlider:
						_Type = n == "-" ? SettingType.IHSlider : SettingType.HSlider;
						break;
					case SettingName.SType.DPad:
						_Type = SettingType.DPad;
						break;
					case SettingName.SType.DPadButton:
						_Type = SettingType.DPadButton;
						break;
					default:
						_Type = SettingType.Button;
						break;
				}
				if (_Index == 0) _Type = SettingType.None;
			}
		}

		/// <summary>
		/// Convert setting to INI value.
		/// </summary>
		public string ToIniValue()
		{
			switch (Type)
			{
				case SettingType.Button: return string.Format("{0}{1}", SettingName.SType.Button, Index);
				case SettingType.Axis: return string.Format("{0}{1}", SettingName.SType.Axis, Index);
				case SettingType.IAxis: return string.Format("{0}{1}", SettingName.SType.Axis, -Index);
				case SettingType.HAxis: return string.Format("{0}{1}", SettingName.SType.HAxis, Index);
				case SettingType.IHAxis: return string.Format("{0}{1}", SettingName.SType.HAxis, -Index);
				case SettingType.Slider: return string.Format("{0}{1}", SettingName.SType.Slider, Index);
				case SettingType.ISlider: return string.Format("{0}{1}", SettingName.SType.Slider, -Index);
				case SettingType.HSlider: return string.Format("{0}{1}", SettingName.SType.HSlider, Index);
				case SettingType.IHSlider: return string.Format("{0}{1}", SettingName.SType.HSlider, -Index);
				case SettingType.DPad: return string.Format("{0}{1}", SettingName.SType.DPad, Index);
				case SettingType.DPadButton: return string.Format("{0}{1}", SettingName.SType.DPadButton, Index);
			}
			return string.Empty;
		}

		/// <summary>
		/// Convert setting to text format for display to the user.
		/// </summary>
		/// <returns></returns>
		public string ToTextValue()
		{
			var s = "";
			if (Type == SettingType.DPadButton)
			{
				var dPadNames = Enum.GetNames(typeof(DPadEnum));
				// Zero-based D-Pad Button Index [0-3];
				var dPadButtonIndex = ((Index - 1) % 4);
				var dPadButtonName = dPadNames[dPadButtonIndex];
				// Zero based D-Pad Index.
				var dPadIndex = ((Index - 1) - dPadButtonIndex) / dPadNames.Length;
				s = string.Format("{0} {1} {2}", SettingType.DPad, dPadIndex + 1, dPadButtonName);
			}
			else if (Type != SettingType.None)
			{
				s = string.Format("{0} {1}", Type, Index);
			}
			return s;
		}

	}
}
