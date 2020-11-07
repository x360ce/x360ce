using System;
using System.Collections.Generic;
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

		public static bool TryParseTextValue(string value, out MapType type, out int index)
		{
			index = 0;
			type = MapType.None;
			if (string.IsNullOrEmpty(value))
				return false;
			var m = textValueRegex.Match(value);
			if (m.Success)
			{
				index = int.Parse(m.Groups["num"].Value);
				// Index must be non zero.
				if (index == 0)
					return false;
				type = (MapType)Enum.Parse(typeof(MapType), m.Groups["type"].Value);
				// If type is DPad with extension then...
				if (type == MapType.POV && m.Groups["ext"].Success)
				{
					// This is DPad button.
					type = MapType.DPOVButton;
					switch (m.Groups["ext"].Value)
					{
						case "Up":
							index = (index - 1) * 4 + 1;
							break;
						case "Right":
							index = (index - 1) * 4 + 2;
							break;
						case "Down":
							index = (index - 1) * 4 + 3;
							break;
						case "Left":
							index = (index - 1) * 4 + 4;
							break;
					}
				}
			}
			return m.Success;
		}

		public static bool TryParseIniValue(string value, out MapType type, out int index)
		{
			index = 0;
			type = MapType.None;
			if (string.IsNullOrEmpty(value))
				return false;
			// Try to convert setting from ini value.
			var m = iniValueRegex.Match(value);
			if (!m.Success)
				return false;
			index = int.Parse(m.Groups["num"].Value);
			// Index must be non zero.
			if (index == 0)
				return false;
			string t = m.Groups["type"].Value;
			string n = m.Groups["neg"].Value;
			switch (t)
			{
				case SettingName.SType.Axis:
					type = n == "-" ? MapType.IAxis : MapType.Axis;
					break;
				case SettingName.SType.Slider:
					type = n == "-" ? MapType.ISlider : MapType.Slider;
					break;
				case SettingName.SType.HAxis:
					type = n == "-" ? MapType.IHAxis : MapType.HAxis;
					break;
				case SettingName.SType.HSlider:
					type = n == "-" ? MapType.IHSlider : MapType.HSlider;
					break;
				case SettingName.SType.POV:
					type = n == "-" ? MapType.IPOV : MapType.POV;
					break;
				case SettingName.SType.POVButton:
					type = n == "-" ? MapType.IPOVButton : MapType.DPOVButton;
					break;
				default:
					type = n == "-" ? MapType.IButton : MapType.Button;
					break;
			}
			return true;
		}

		/// <summary>
		/// Convert setting to INI value.
		/// </summary>
		public static string ToIniValue(MapType type, int index)
		{
			switch (type)
			{
				case MapType.Button:
					return string.Format("{0}{1}", SettingName.SType.Button, index);
				case MapType.IButton:
					return string.Format("{0}{1}", SettingName.SType.Button, -index);
				case MapType.Axis:
					return string.Format("{0}{1}", SettingName.SType.Axis, index);
				case MapType.IAxis:
					return string.Format("{0}{1}", SettingName.SType.Axis, -index);
				case MapType.HAxis:
					return string.Format("{0}{1}", SettingName.SType.HAxis, index);
				case MapType.IHAxis:
					return string.Format("{0}{1}", SettingName.SType.HAxis, -index);
				case MapType.Slider:
					return string.Format("{0}{1}", SettingName.SType.Slider, index);
				case MapType.ISlider:
					return string.Format("{0}{1}", SettingName.SType.Slider, -index);
				case MapType.HSlider:
					return string.Format("{0}{1}", SettingName.SType.HSlider, index);
				case MapType.IHSlider:
					return string.Format("{0}{1}", SettingName.SType.HSlider, -index);
				case MapType.POV:
					return string.Format("{0}{1}", SettingName.SType.POV, index);
				case MapType.IPOV:
					return string.Format("{0}{1}", SettingName.SType.POV, -index);
				case MapType.DPOVButton:
					return string.Format("{0}{1}", SettingName.SType.POVButton, index);
				case MapType.IPOVButton:
					return string.Format("{0}{1}", SettingName.SType.POVButton, -index);
				default:
					return "";
			}
		}

		/// <summary>Convert Text value to INI value.</summary>
		public static string ToIniValue(string textValue)
		{
			var index = 0;
			var type = MapType.None;
			return TryParseTextValue(textValue, out type, out index)
				? ToIniValue(type, index)
				: "";
		}


		/// <summary>Convert INI value to Text value.</summary>
		public static string FromIniValue(string iniValue)
		{
			var index = 0;
			var type = MapType.None;
			return TryParseIniValue(iniValue, out type, out index)
				? ToTextValue(type, index)
				: "";
		}


		/// <summary>
		/// Convert setting to text format for display to the user.
		/// </summary>
		/// <returns></returns>
		public static string ToTextValue(MapType type, int index)
		{
			var s = "";
			if (type == MapType.DPOVButton)
			{
				var dPadNames = Enum.GetNames(typeof(DPadEnum));
				// Zero-based D-Pad Button Index [0-3];
				var dPadButtonIndex = ((index - 1) % 4);
				var dPadButtonName = dPadNames[dPadButtonIndex];
				// Zero based D-Pad Index.
				var dPadIndex = ((index - 1) - dPadButtonIndex) / dPadNames.Length;
				s = string.Format("{0} {1} {2}", MapType.POV, dPadIndex + 1, dPadButtonName);
			}
			else if (type != MapType.None)
			{
				s = string.Format("{0} {1}", type, index);
			}
			return s;
		}

		#region Setting Type Methods

		public static bool IsButton(MapType type)
			=> SettingButton.Contains(type);

		static List<MapType> SettingButton = new List<MapType>
		{
			MapType.Button,
			MapType.IButton,
		};

		public static bool IsAxis(MapType type)
			=> SettingAxis.Contains(type);

		static List<MapType> SettingAxis = new List<MapType>
		{
			MapType.Axis,
			MapType.IAxis,
			MapType.HAxis,
			MapType.IHAxis,
		};

		public static bool IsSlider(MapType type)
			=> SettingSlider.Contains(type);

		static List<MapType> SettingSlider = new List<MapType>
		{
			MapType.Slider,
			MapType.ISlider,
			MapType.HSlider,
			MapType.IHSlider,
		};

		public static bool IsHalf(MapType type)
			=> SettingHalf.Contains(type);

		public static MapType ToFull(MapType type)
		{
			var i = SettingHalf.IndexOf(type);
			if (i > -1)
				return SettingFull[i];
			return type;
		}

		static List<MapType> SettingHalf = new List<MapType>
		{
			MapType.HAxis,
			MapType.IHAxis,
			MapType.HSlider,
			MapType.IHSlider,
		};

		static List<MapType> SettingFull = new List<MapType>
		{
			MapType.Axis,
			MapType.IAxis,
			MapType.Slider,
			MapType.ISlider,
		};

		public static bool IsInverted(MapType type)
			=> SettingInverted.Contains(type);

		public static MapType Invert(MapType type)
		{
			var i = SettingInverted.IndexOf(type);
			if (i > -1)
				return SettingNonInverted[i];
			i = SettingNonInverted.IndexOf(type);
			if (i > -1)
				return SettingInverted[i];
			return type;
		}

		static List<MapType> SettingInverted = new List<MapType>
		{
			MapType.IAxis,
			MapType.IHAxis,
			MapType.ISlider,
			MapType.IHSlider,
		};

		static List<MapType> SettingNonInverted = new List<MapType>
		{
			MapType.Axis,
			MapType.HAxis,
			MapType.Slider,
			MapType.HSlider,
		};

		#endregion

		#region Layout Code Methods

		public static List<MapCode> LeftThumbCodes = new List<MapCode>
		{
			MapCode.LeftThumbAxisX,
			MapCode.LeftThumbAxisY,
			MapCode.LeftThumbButton,
			MapCode.LeftThumbDown,
			MapCode.LeftThumbLeft,
			MapCode.LeftThumbRight,
			MapCode.LeftThumbUp,
		};

		public static List<MapCode> RightThumbCodes = new List<MapCode>
		{
			MapCode.RightThumbAxisX,
			MapCode.RightThumbAxisY,
			MapCode.RightThumbButton,
			MapCode.RightThumbDown,
			MapCode.RightThumbLeft,
			MapCode.RightThumbRight,
			MapCode.RightThumbUp,
		};

		public static List<MapCode> DPadCodes = new List<MapCode>
		{
			MapCode.DPad,
			MapCode.DPadUp,
			MapCode.DPadLeft,
			MapCode.DPadRight,
			MapCode.DPadDown,
		};

		public static List<MapCode> MenuButtonCodes = new List<MapCode>
		{
			MapCode.ButtonBack,
			MapCode.ButtonGuide,
			MapCode.ButtonStart,
		};

		public static List<MapCode> MainButtonCodes = new List<MapCode>
		{
			MapCode.ButtonA,
			MapCode.ButtonB,
			MapCode.ButtonX,
			MapCode.ButtonY,
		};

		public static List<MapCode> TriggerButtonCodes = new List<MapCode>
		{
			MapCode.LeftTrigger,
			MapCode.RightTrigger,
		};

		public static List<MapCode> ShoulderButtonCodes = new List<MapCode>
		{
			MapCode.LeftShoulder,
			MapCode.RightShoulder,
		};

		public static List<MapCode> AxisCodes = new List<MapCode>
		{
			MapCode.LeftTrigger,
			MapCode.RightTrigger,
			MapCode.LeftThumbAxisX,
			MapCode.LeftThumbAxisY,
			MapCode.LeftThumbUp,
			MapCode.LeftThumbLeft,
			MapCode.LeftThumbRight,
			MapCode.LeftThumbDown,
			MapCode.RightThumbAxisX,
			MapCode.RightThumbAxisY,
			MapCode.RightThumbUp,
			MapCode.RightThumbLeft,
			MapCode.RightThumbRight,
			MapCode.RightThumbDown,
		};

		public static List<MapCode> ThumbDirections = new List<MapCode>
		{
			MapCode.LeftThumbUp,
			MapCode.LeftThumbLeft,
			MapCode.LeftThumbRight,
			MapCode.LeftThumbDown,
			MapCode.RightThumbUp,
			MapCode.RightThumbLeft,
			MapCode.RightThumbRight,
			MapCode.RightThumbDown,
		};

		public static List<MapCode> DPadDirections = new List<MapCode>
		{
			MapCode.DPadUp,
			MapCode.DPadLeft,
			MapCode.DPadRight,
			MapCode.DPadDown,
		};


		public static bool IsButtonOrDirection(MapCode code)
		{
			return
				MenuButtonCodes.Contains(code) ||
				MainButtonCodes.Contains(code) ||
				ShoulderButtonCodes.Contains(code) ||
				TriggerButtonCodes.Contains(code) ||
				ThumbDirections.Contains(code) ||
				DPadDirections.Contains(code);
		}

		#endregion

	}
}
