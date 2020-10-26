using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using x360ce.Engine.Data;

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

		public static bool TryParseIniValue(string value, out SettingType type, out int index)
		{
			index = 0;
			type = SettingType.None;
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
			return true;
		}

		/// <summary>
		/// Convert setting to INI value.
		/// </summary>
		public static string ToIniValue(SettingType type, int index)
		{
			switch (type)
			{
				case SettingType.Button:
					return string.Format("{0}{1}", SettingName.SType.Button, index);
				case SettingType.IButton:
					return string.Format("{0}{1}", SettingName.SType.Button, -index);
				case SettingType.Axis:
					return string.Format("{0}{1}", SettingName.SType.Axis, index);
				case SettingType.IAxis:
					return string.Format("{0}{1}", SettingName.SType.Axis, -index);
				case SettingType.HAxis:
					return string.Format("{0}{1}", SettingName.SType.HAxis, index);
				case SettingType.IHAxis:
					return string.Format("{0}{1}", SettingName.SType.HAxis, -index);
				case SettingType.Slider:
					return string.Format("{0}{1}", SettingName.SType.Slider, index);
				case SettingType.ISlider:
					return string.Format("{0}{1}", SettingName.SType.Slider, -index);
				case SettingType.HSlider:
					return string.Format("{0}{1}", SettingName.SType.HSlider, index);
				case SettingType.IHSlider:
					return string.Format("{0}{1}", SettingName.SType.HSlider, -index);
				case SettingType.POV:
					return string.Format("{0}{1}", SettingName.SType.POV, index);
				case SettingType.IPOV:
					return string.Format("{0}{1}", SettingName.SType.POV, -index);
				case SettingType.DPOVButton:
					return string.Format("{0}{1}", SettingName.SType.POVButton, index);
				case SettingType.IPOVButton:
					return string.Format("{0}{1}", SettingName.SType.POVButton, -index);
				default:
					return "";
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
		public static string FromIniValue(string iniValue)
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

		#region Setting Type Methods

		public static bool IsButton(SettingType type)
			=> SettingButton.Contains(type);

		static List<SettingType> SettingButton = new List<SettingType>
		{
			SettingType.Button,
			SettingType.IButton,
		};

		public static bool IsAxis(SettingType type)
			=> SettingAxis.Contains(type);

		static List<SettingType> SettingAxis = new List<SettingType>
		{
			SettingType.Axis,
			SettingType.IAxis,
			SettingType.HAxis,
			SettingType.IHAxis,
		};

		public static bool IsSlider(SettingType type)
			=> SettingSlider.Contains(type);

		static List<SettingType> SettingSlider = new List<SettingType>
		{
			SettingType.Slider,
			SettingType.ISlider,
			SettingType.HSlider,
			SettingType.IHSlider,
		};

		public static bool IsHalf(SettingType type)
			=> SettingHalf.Contains(type);

		public static SettingType ToFull(SettingType type)
		{
			var i = SettingHalf.IndexOf(type);
			if (i > -1)
				return SettingFull[i];
			return type;
		}

		static List<SettingType> SettingHalf = new List<SettingType>
		{
			SettingType.HAxis,
			SettingType.IHAxis,
			SettingType.HSlider,
			SettingType.IHSlider,
		};

		static List<SettingType> SettingFull = new List<SettingType>
		{
			SettingType.Axis,
			SettingType.IAxis,
			SettingType.Slider,
			SettingType.ISlider,
		};

		public static bool IsInverted(SettingType type)
			=> SettingInverted.Contains(type);

		public static SettingType Invert(SettingType type)
		{
			var i = SettingInverted.IndexOf(type);
			if (i > -1)
				return SettingNonInverted[i];
			i = SettingNonInverted.IndexOf(type);
			if (i > -1)
				return SettingInverted[i];
			return type;
		}

		static List<SettingType> SettingInverted = new List<SettingType>
		{
			SettingType.IAxis,
			SettingType.IHAxis,
			SettingType.ISlider,
			SettingType.IHSlider,
		};

		static List<SettingType> SettingNonInverted = new List<SettingType>
		{
			SettingType.Axis,
			SettingType.HAxis,
			SettingType.Slider,
			SettingType.HSlider,
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
			MapCode.DPadDown,
			MapCode.DPadLeft,
			MapCode.DPadRight,
			MapCode.DPadUp,
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
			MapCode.LeftThumbDown,
			MapCode.LeftThumbLeft,
			MapCode.LeftThumbRight,
			MapCode.LeftThumbUp,
			MapCode.RightThumbAxisX,
			MapCode.RightThumbAxisY,
			MapCode.RightThumbDown,
			MapCode.RightThumbLeft,
			MapCode.RightThumbRight,
			MapCode.RightThumbUp,
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

		#endregion

	}
}
