using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace x360ce.App
{
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
		Regex frmRegex = new Regex("^(?<type>Axis|IAxis|HAxis|IHAxis|Slider|ISlider|HSlider|IHSlider|Button|DPad|DPadButton) (?<num>[0-9]+)[ ]*(?<ext>Up|Left|Right|Down)?$");
		Regex iniRegex = new Regex("^(?<type>[asxhpv])?(?<neg>[-]*)?(?<num>[0-9]+)$");

		int _Index;
		public int Index { get { return _Index; } }
		//string _Ext;
		//public string Ext { get { return _Ext; } }
		SettingType _Type;
		public SettingType Type { get { return _Type; } }

		public void FromSetting(string value)
		{
			FromSetting(value, string.Empty);
		}

		public void FromSetting(string value, string key)
		{
			Match m;
			if (frmRegex.IsMatch(value))
			{
				m = frmRegex.Match(value);
				_Type = (SettingType)Enum.Parse(typeof(SettingType), m.Groups["type"].Value);
				_Index = int.Parse(m.Groups["num"].Value);
				if (m.Groups["ext"].Success)
				{
					if (_Type == SettingType.DPad)
					{
						_Type = SettingType.DPadButton;
						switch (m.Groups["ext"].Value)
						{
							case "Up": _Index = (_Index - 1) * 4 + 1; break;
							case "Right": _Index = (_Index - 1) * 4 + 2; break;
							case "Down": _Index = (_Index - 1) * 4 + 3; break;
							case "Left": _Index = (_Index - 1) * 4 + 4; break;
						}
					}
				}
			}
			if (iniRegex.IsMatch(value))
			{
				m = iniRegex.Match(value);
				string t = m.Groups["type"].Value;
				string n = m.Groups["neg"].Value;
				_Index = int.Parse(m.Groups["num"].Value);
				if (key.Contains("Analog") && !key.Contains("Button")) t = SettingName.SType.Axis;
				if (key.Contains("D-pad POV")) t = SettingName.SType.DPad;
				if (t == SettingName.SType.Axis && Index == 7) _Index = 0;
				switch (t)
				{
					case SettingName.SType.Axis :
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

		public string ToIniSetting()
		{
			switch (Type)
			{
				case SettingType.Button: return string.Format("{0}", Index);
				case SettingType.Axis: return string.Format("a{0}", Index);
				case SettingType.IAxis: return string.Format("a-{0}", Index);
				case SettingType.HAxis: return string.Format("x{0}", Index);
				case SettingType.IHAxis: return string.Format("x-{0}", Index);
				case SettingType.Slider: return string.Format("s{0}", Index);
				case SettingType.ISlider: return string.Format("s-{0}", Index);
				case SettingType.HSlider: return string.Format("h{0}", Index);
				case SettingType.IHSlider: return string.Format("h-{0}", Index);
				case SettingType.DPad: return string.Format("p{0}", Index);
				case SettingType.DPadButton: return string.Format("v{0}", Index);
			}
			return string.Empty;
		}

		public string ToFrmSetting()
		{
			return Type == SettingType.None
				? string.Empty
				: string.Format("{0} {1}", Type, Index);
		}


	}
}
