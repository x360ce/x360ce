using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>How a pad labels its four face buttons, from the bottom one round: SDL's face styles.</summary>
	public enum SdlFaceStyle
	{
		/// <summary>A, B, X, Y: Xbox and most pads.</summary>
		Abxy,
		/// <summary>A, X, B, Y: GameCube.</summary>
		Axby,
		/// <summary>B, A, Y, X: Nintendo.</summary>
		Bayx,
		/// <summary>Cross, Circle, Square, Triangle: PlayStation.</summary>
		Sony,
	}

	/// <summary>One place on the Xbox controller as the SDL page lists it: what SDL maps there for the device, and what the box holds now.</summary>
	public sealed class SdlPositionRow
	{
		public MapCode Code { get; set; }
		/// <summary>SDL's name for the place, such as "a" or "-lefty".</summary>
		public string Field { get; set; }
		/// <summary>The place in words, such as "Bottom face button".</summary>
		public string Position { get; set; }
		/// <summary>What the place is labelled on this pad, such as "A" or "Cross".</summary>
		public string Label { get; set; }
		/// <summary>The SDL row's binding, such as "b0", "+a2" or "h0.1"; empty when the row maps nothing there.</summary>
		public string Binding { get; set; }
		/// <summary>The device's control that binding names, in x360ce's words, such as "Button 1" or "HAxis 3".</summary>
		public string Control { get; set; }
		/// <summary>What the box holds now, in the same words.</summary>
		public string Box { get; set; }
		/// <summary>Whether the box holds something other than what the SDL row gives.</summary>
		public bool Differs { get; set; }
	}

	/// <summary>The Xbox controller's places in words, their labels on other pads, and the SDL page's list.</summary>
	public static class SdlPositions
	{
		/// <summary>The places the page always lists; a stick's direction boxes and the D-Pad's buttons are listed when something fills them.</summary>
		static readonly MapCode[] Always =
		{
			MapCode.ButtonA, MapCode.ButtonB, MapCode.ButtonX, MapCode.ButtonY,
			MapCode.LeftShoulder, MapCode.RightShoulder, MapCode.LeftTrigger, MapCode.RightTrigger,
			MapCode.ButtonBack, MapCode.ButtonStart, MapCode.ButtonGuide,
			MapCode.LeftThumbButton, MapCode.RightThumbButton,
			MapCode.DPad,
			MapCode.LeftThumbAxisX, MapCode.LeftThumbAxisY,
			MapCode.RightThumbAxisX, MapCode.RightThumbAxisY,
		};

		/// <summary>Every place, in the order the page lists them.</summary>
		static readonly MapCode[] Order =
		{
			MapCode.ButtonA, MapCode.ButtonB, MapCode.ButtonX, MapCode.ButtonY,
			MapCode.LeftShoulder, MapCode.RightShoulder, MapCode.LeftTrigger, MapCode.RightTrigger,
			MapCode.ButtonBack, MapCode.ButtonStart, MapCode.ButtonGuide,
			MapCode.LeftThumbButton, MapCode.RightThumbButton,
			MapCode.DPad, MapCode.DPadUp, MapCode.DPadRight, MapCode.DPadDown, MapCode.DPadLeft,
			MapCode.LeftThumbAxisX, MapCode.LeftThumbAxisY,
			MapCode.LeftThumbUp, MapCode.LeftThumbRight, MapCode.LeftThumbDown, MapCode.LeftThumbLeft,
			MapCode.RightThumbAxisX, MapCode.RightThumbAxisY,
			MapCode.RightThumbUp, MapCode.RightThumbRight, MapCode.RightThumbDown, MapCode.RightThumbLeft,
		};

		/// <summary>A place in words.</summary>
		public static string Position(MapCode code)
		{
			switch (code)
			{
				case MapCode.ButtonA: return "Bottom face button";
				case MapCode.ButtonB: return "Right face button";
				case MapCode.ButtonX: return "Left face button";
				case MapCode.ButtonY: return "Top face button";
				case MapCode.LeftShoulder: return "Left bumper";
				case MapCode.RightShoulder: return "Right bumper";
				case MapCode.LeftTrigger: return "Left trigger";
				case MapCode.RightTrigger: return "Right trigger";
				case MapCode.ButtonBack: return "Left middle button";
				case MapCode.ButtonStart: return "Right middle button";
				case MapCode.ButtonGuide: return "Centre button";
				case MapCode.LeftThumbButton: return "Left stick, pressed";
				case MapCode.RightThumbButton: return "Right stick, pressed";
				case MapCode.DPad: return "D-Pad";
				case MapCode.DPadUp: return "D-Pad up";
				case MapCode.DPadRight: return "D-Pad right";
				case MapCode.DPadDown: return "D-Pad down";
				case MapCode.DPadLeft: return "D-Pad left";
				case MapCode.LeftThumbAxisX: return "Left stick, left-right";
				case MapCode.LeftThumbAxisY: return "Left stick, up-down";
				case MapCode.LeftThumbUp: return "Left stick, up";
				case MapCode.LeftThumbRight: return "Left stick, right";
				case MapCode.LeftThumbDown: return "Left stick, down";
				case MapCode.LeftThumbLeft: return "Left stick, left";
				case MapCode.RightThumbAxisX: return "Right stick, left-right";
				case MapCode.RightThumbAxisY: return "Right stick, up-down";
				case MapCode.RightThumbUp: return "Right stick, up";
				case MapCode.RightThumbRight: return "Right stick, right";
				case MapCode.RightThumbDown: return "Right stick, down";
				case MapCode.RightThumbLeft: return "Right stick, left";
				default: return code.ToString();
			}
		}

		/// <summary>How a row says its pad labels the face buttons.</summary>
		/// <remarks>
		/// As SDL decides it: the row's face field; then its hint for GameCube labels or Nintendo's; then its type field;
		/// then the kind of pad, which x360ce tells from the vendor and the name: Sony, or a name naming a PlayStation pad,
		/// labels as PlayStation; a GameCube pad as GameCube; Nintendo, or a Switch or Joy-Con, as Nintendo; any other as Xbox.
		/// </remarks>
		/// <param name="mapping">The row, or null.</param>
		/// <param name="vendorId">The device's vendor.</param>
		/// <param name="name">The device's name, or the row's.</param>
		public static SdlFaceStyle FaceStyle(SdlMapping mapping, int vendorId, string name)
		{
			var face = mapping == null ? null : mapping.Get("face");
			switch (face)
			{
				case "abxy": return SdlFaceStyle.Abxy;
				case "axby": return SdlFaceStyle.Axby;
				case "bayx": return SdlFaceStyle.Bayx;
				case "sony": return SdlFaceStyle.Sony;
			}
			var text = mapping == null ? "" : mapping.ToString();
			if (text.IndexOf("USE_GAMECUBE_LABELS", StringComparison.Ordinal) >= 0)
				return SdlFaceStyle.Axby;
			if (text.IndexOf("USE_BUTTON_LABELS", StringComparison.Ordinal) >= 0)
				return SdlFaceStyle.Bayx;
			var type = mapping == null ? null : mapping.Get("type");
			if (type != null)
			{
				if (type.StartsWith("ps", StringComparison.OrdinalIgnoreCase))
					return SdlFaceStyle.Sony;
				if (type.StartsWith("switch", StringComparison.OrdinalIgnoreCase) || type.StartsWith("joycon", StringComparison.OrdinalIgnoreCase))
					return SdlFaceStyle.Bayx;
				if (type.Equals("gamecube", StringComparison.OrdinalIgnoreCase))
					return SdlFaceStyle.Axby;
			}
			var n = (name ?? "").ToUpperInvariant();
			if (vendorId == 0x054C || new[] { "PS3", "PS4", "PS5", "DUALSHOCK", "DUALSENSE", "PLAYSTATION" }.Any(x => n.Contains(x)))
				return SdlFaceStyle.Sony;
			if (n.Contains("GAMECUBE"))
				return SdlFaceStyle.Axby;
			if (vendorId == 0x057E || n.Contains("SWITCH") || n.Contains("JOY-CON") || n.Contains("JOYCON"))
				return SdlFaceStyle.Bayx;
			return SdlFaceStyle.Abxy;
		}

		/// <summary>A face style in words: "an Xbox pad (A B X Y)".</summary>
		public static string Describe(SdlFaceStyle style)
		{
			switch (style)
			{
				case SdlFaceStyle.Sony: return "a PlayStation pad (Cross, Circle, Square, Triangle)";
				case SdlFaceStyle.Bayx: return "a Nintendo pad (B A Y X)";
				case SdlFaceStyle.Axby: return "a GameCube pad (A X B Y)";
				default: return "an Xbox pad (A B X Y)";
			}
		}

		/// <summary>What a place is labelled on a pad of this style; SDL's "a" is the bottom face button whatever it is labelled.</summary>
		public static string Label(SdlFaceStyle style, MapCode code)
		{
			var sony = style == SdlFaceStyle.Sony;
			var nintendo = style == SdlFaceStyle.Bayx;
			switch (code)
			{
				case MapCode.ButtonA: return sony ? "Cross" : nintendo ? "B" : "A";
				case MapCode.ButtonB: return sony ? "Circle" : nintendo ? "A" : style == SdlFaceStyle.Axby ? "X" : "B";
				case MapCode.ButtonX: return sony ? "Square" : nintendo ? "Y" : style == SdlFaceStyle.Axby ? "B" : "X";
				case MapCode.ButtonY: return sony ? "Triangle" : nintendo ? "X" : "Y";
				case MapCode.LeftShoulder: return sony ? "L1" : nintendo ? "L" : "LB";
				case MapCode.RightShoulder: return sony ? "R1" : nintendo ? "R" : "RB";
				case MapCode.LeftTrigger: return sony ? "L2" : nintendo ? "ZL" : "LT";
				case MapCode.RightTrigger: return sony ? "R2" : nintendo ? "ZR" : "RT";
				case MapCode.ButtonBack: return sony ? "Select / Share" : nintendo ? "Minus" : "Back / View";
				case MapCode.ButtonStart: return sony ? "Start / Options" : nintendo ? "Plus" : "Start / Menu";
				case MapCode.ButtonGuide: return sony ? "PS" : nintendo ? "Home" : "Guide / Xbox";
				case MapCode.LeftThumbButton: return sony ? "L3" : "LS";
				case MapCode.RightThumbButton: return sony ? "R3" : "RS";
				default: return "";
			}
		}

		/// <summary>A box's value in the boxes' own words, such as "Button 1", "HAxis 3" or a formula.</summary>
		public static string Words(string value, MapCode code)
		{
			if (string.IsNullOrEmpty(value))
				return "";
			return MapExpression.IsExpression(value) ? value : SettingsConverter.FromIniValue(value, code);
		}

		/// <summary>A box's value written one way, so two ways of writing one control compare equal.</summary>
		static string Same(string value, MapCode code)
		{
			if (string.IsNullOrEmpty(value))
				return "";
			if (MapExpression.IsExpression(value))
				return value.Replace(" ", "");
			MapType type;
			int index;
			return SettingsConverter.TryParseIniValue(value, out type, out index, code)
				? SettingsConverter.ToIniValue(type, index, code)
				: value;
		}

		/// <summary>The SDL page's list: each place, what the SDL row maps there for the device and what the box holds now.</summary>
		/// <param name="translation">The device's SDL row translated, or null when SDL does not know the device.</param>
		/// <param name="current">The mapping the controller has now.</param>
		/// <param name="style">How the pad labels its buttons.</param>
		public static List<SdlPositionRow> Compare(SdlTranslation translation, PadSetting current, SdlFaceStyle style)
		{
			var rows = new List<SdlPositionRow>();
			foreach (var code in Order)
			{
				var sdl = translation == null ? "" : SdlConverter.GetValue(translation.Setting, code);
				var box = SdlConverter.GetValue(current, code);
				if (!Always.Contains(code) && sdl.Length == 0 && box.Length == 0)
					continue;
				string binding = null;
				if (translation != null)
					translation.Sources.TryGetValue(code, out binding);
				rows.Add(new SdlPositionRow
				{
					Code = code,
					Field = SdlConverter.FieldOf(code),
					Position = Position(code),
					Label = Label(style, code),
					Binding = Bindings(binding),
					Control = Words(sdl, code),
					Box = Words(box, code),
					Differs = translation != null && Same(sdl, code) != Same(box, code),
				});
			}
			return rows;
		}

		/// <summary>"-righty:-a2, +righty:+a5" as the bindings alone, "-a2, +a5", where one field gives them; as written where several do.</summary>
		static string Bindings(string sources)
		{
			if (string.IsNullOrEmpty(sources))
				return "";
			var parts = sources.Split(new[] { ", " }, StringSplitOptions.None);
			var fields = parts.Select(x => x.Substring(0, Math.Max(0, x.IndexOf(':')))).Distinct().Count();
			return fields == 1 ? string.Join(", ", parts.Select(x => x.Substring(x.IndexOf(':') + 1))) : sources;
		}
	}
}
