using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>What SDL needs to know about a device: its ids and which of its controls it has.</summary>
	/// <remarks>
	/// SDL's DirectInput reader sorts a device's objects by where DirectInput puts them in its state, so SDL axis
	/// <c>aK</c> is the K-th one present of X, Y, Z, Rx, Ry, Rz, Slider 0 and Slider 1: x360ce's Axis 1 to 6, from the
	/// bits of <see cref="UserDevice.DiAxeMask"/>, then Slider 1 and 2, from <see cref="UserDevice.DiSliderMask"/>.
	/// Button <c>bK</c> is x360ce's Button K+1, hat <c>hH</c> its POV H+1.
	/// </remarks>
	public sealed class SdlDevice
	{
		public int VendorId;
		public int ProductId;
		public string ProductName;
		/// <summary>DirectInput's product GUID, which older SDL rows start with.</summary>
		public Guid ProductGuid;
		/// <summary>X, Y, Z, Rx, Ry and Rz: x360ce's Axis 1 to 6, one bit each.</summary>
		public int AxisMask;
		/// <summary>Slider 0 and 1: x360ce's Slider 1 and 2, one bit each.</summary>
		public int SliderMask;
		/// <summary>How many buttons; nought when not known, which lets any button through.</summary>
		public int ButtonCount;
		/// <summary>How many hats; nought when not known, which lets any hat through.</summary>
		public int PovCount;

		/// <summary>The axes SDL counts: X to Rz, then the two sliders.</summary>
		public const int MaxSdlAxes = 8;

		/// <summary>A device with every axis, slider, button and hat SDL can name, and no ids: what text is read for when no device is selected.</summary>
		public static SdlDevice Full
		{
			get { return new SdlDevice { AxisMask = 0x3F, SliderMask = 0x3 }; }
		}

		/// <summary>Whether the device names a vendor and product.</summary>
		public bool HasIds
		{
			get { return VendorId != 0 || ProductId != 0; }
		}

		/// <summary>The device as SDL sees it; null for none.</summary>
		/// <remarks>
		/// The ids come from the device node, else its HID interface, else DirectInput's product GUID, which for a HID
		/// device is "PIDVID" with the vendor and product in front.
		/// </remarks>
		public static SdlDevice From(UserDevice ud)
		{
			if (ud == null)
				return null;
			var device = new SdlDevice
			{
				VendorId = ud.DevVendorId,
				ProductId = ud.DevProductId,
				ProductName = ud.ProductName,
				ProductGuid = ud.ProductGuid,
				AxisMask = ud.DiAxeMask & 0x3F,
				SliderMask = ud.DiSliderMask & 0x3,
				ButtonCount = ud.CapButtonCount,
				PovCount = ud.CapPovCount,
			};
			if (!device.HasIds)
			{
				device.VendorId = ud.HidVendorId;
				device.ProductId = ud.HidProductId;
			}
			if (!device.HasIds)
			{
				var g = SdlGuid.Parse(ProductGuidText(ud.ProductGuid));
				if (g != null && g.Form == SdlGuidForm.ProductGuid)
				{
					device.VendorId = g.VendorId;
					device.ProductId = g.ProductId;
				}
			}
			return device;
		}

		/// <summary>DirectInput's product GUID as SDL writes it: its bytes in order, as hex.</summary>
		static string ProductGuidText(Guid productGuid)
		{
			return string.Concat(productGuid.ToByteArray().Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
		}

		/// <summary>The device's GUID as the database writes one: the standard form when the ids are known, otherwise DirectInput's product GUID.</summary>
		public string Guid
		{
			get { return HasIds ? SdlGuid.ForDevice(VendorId, ProductId) : ProductGuidText(ProductGuid); }
		}

		/// <summary>The keys of every GUID a row for this device could start with.</summary>
		internal HashSet<string> Guids()
		{
			var keys = new HashSet<string>();
			if (HasIds)
				keys.Add(SdlGuid.Parse(SdlGuid.ForDevice(VendorId, ProductId)).Key);
			if (ProductGuid != System.Guid.Empty)
				keys.Add(ProductGuidText(ProductGuid));
			return keys;
		}

		/// <summary>The x360ce control SDL axis <c>aK</c> is on this device: Axis 1 to 6 or Slider 1 to 2; false when the device has no K-th axis.</summary>
		public bool TryGetAxis(int sdlIndex, out MapType type, out int index)
		{
			type = MapType.None;
			index = 0;
			var n = 0;
			for (var i = 0; i < MaxSdlAxes; i++)
			{
				var slider = i >= 6;
				var present = slider ? (SliderMask & (1 << (i - 6))) != 0 : (AxisMask & (1 << i)) != 0;
				if (!present)
					continue;
				if (n == sdlIndex)
				{
					type = slider ? MapType.Slider : MapType.Axis;
					index = slider ? i - 5 : i + 1;
					return true;
				}
				n++;
			}
			return false;
		}

		/// <summary>The SDL axis number of an x360ce axis or slider on this device; -1 when SDL has no number for it.</summary>
		public int SdlAxisOf(bool slider, int index)
		{
			var bit = slider ? index + 5 : index - 1;
			if (index < 1 || bit < 0 || bit >= MaxSdlAxes || (slider ? (SliderMask & (1 << (index - 1))) == 0 : (AxisMask & (1 << bit)) == 0))
				return -1;
			var n = 0;
			for (var i = 0; i < bit; i++)
				if (i >= 6 ? (SliderMask & (1 << (i - 6))) != 0 : (AxisMask & (1 << i)) != 0)
					n++;
			return n;
		}
	}

	/// <summary>An SDL row turned into x360ce's mapping for one device.</summary>
	public sealed class SdlTranslation
	{
		public SdlTranslation()
		{
			Setting = new PadSetting();
			Sources = new Dictionary<MapCode, string>();
			Skipped = new List<string>();
		}

		/// <summary>The row translated.</summary>
		public SdlMapping Mapping;

		/// <summary>The mapping boxes the row fills; nothing else is set.</summary>
		public PadSetting Setting { get; private set; }

		/// <summary>For each box filled, the SDL fields and bindings it came from, such as "-righty:-a2, +righty:+a5".</summary>
		public Dictionary<MapCode, string> Sources { get; private set; }

		/// <summary>The row's fields x360ce has no place for, or whose control the device does not have, as "field:binding".</summary>
		public List<string> Skipped { get; private set; }

		/// <summary>How many boxes the row fills.</summary>
		public int Count
		{
			get { return Sources.Count; }
		}

		/// <summary>The fields left out in one line, or empty when none were.</summary>
		public string SkippedText()
		{
			return Skipped.Count == 0 ? "" : "Left out, with no place in x360ce or no such control on the device: " + string.Join(", ", Skipped) + ".";
		}
	}

	/// <summary>Translates between SDL rows and x360ce's mapping boxes, for one device's controls.</summary>
	/// <remarks>
	/// Bindings: <c>bK</c> is Button K+1; <c>hH.V</c> is POV H+1 Up, Right, Down or Left for V 1, 2, 4 or 8; <c>aK</c> is
	/// the device's K-th axis (<see cref="SdlDevice.TryGetAxis"/>), <c>+aK</c> its upper half (HAxis), <c>-aK</c> its lower
	/// half (IHAxis) and <c>aK~</c> the axis inverted (IAxis). SDL's stick Y grows downward and the Xbox controller's
	/// upward, so <c>lefty:a1</c> is IAxis 2. A stick half, <c>-leftx</c> or <c>+lefty</c>, from a button or a hat is the
	/// stick's direction box; halves from two different axes, as <c>-righty:-a2,+righty:+a5</c>, become a formula in the
	/// stick's box. Four <c>dp*</c> fields on one hat are the D-Pad box. Values are written by
	/// <see cref="SettingsConverter.ToIniValue(MapType, int, MapCode)"/>.
	/// </remarks>
	public static class SdlConverter
	{
		#region Fields

		/// <summary>SDL's gamepad buttons that have an x360ce box, by SDL name.</summary>
		static readonly Dictionary<string, MapCode> ButtonFields = new Dictionary<string, MapCode>
		{
			{ "a", MapCode.ButtonA },
			{ "b", MapCode.ButtonB },
			{ "x", MapCode.ButtonX },
			{ "y", MapCode.ButtonY },
			{ "back", MapCode.ButtonBack },
			{ "guide", MapCode.ButtonGuide },
			{ "start", MapCode.ButtonStart },
			{ "leftstick", MapCode.LeftThumbButton },
			{ "rightstick", MapCode.RightThumbButton },
			{ "leftshoulder", MapCode.LeftShoulder },
			{ "rightshoulder", MapCode.RightShoulder },
			{ "dpup", MapCode.DPadUp },
			{ "dpdown", MapCode.DPadDown },
			{ "dpleft", MapCode.DPadLeft },
			{ "dpright", MapCode.DPadRight },
		};

		static readonly Dictionary<string, MapCode> TriggerFields = new Dictionary<string, MapCode>
		{
			{ "lefttrigger", MapCode.LeftTrigger },
			{ "righttrigger", MapCode.RightTrigger },
		};

		/// <summary>The stick axes, each with its box and its two direction boxes, the low end's first.</summary>
		static readonly Dictionary<string, MapCode[]> StickFields = new Dictionary<string, MapCode[]>
		{
			{ "leftx", new[] { MapCode.LeftThumbAxisX, MapCode.LeftThumbLeft, MapCode.LeftThumbRight } },
			{ "lefty", new[] { MapCode.LeftThumbAxisY, MapCode.LeftThumbUp, MapCode.LeftThumbDown } },
			{ "rightx", new[] { MapCode.RightThumbAxisX, MapCode.RightThumbLeft, MapCode.RightThumbRight } },
			{ "righty", new[] { MapCode.RightThumbAxisY, MapCode.RightThumbUp, MapCode.RightThumbDown } },
		};

		/// <summary>SDL's gamepad parts x360ce has no box for.</summary>
		static readonly HashSet<string> NoPlaceFields = new HashSet<string>
		{
			"misc1", "misc2", "misc3", "misc4", "misc5", "misc6",
			"paddle1", "paddle2", "paddle3", "paddle4",
			"touchpad",
		};

		/// <summary>The D-Pad directions, in SDL's hat bit order: 1 up, 2 right, 4 down, 8 left.</summary>
		static readonly string[] DPadFields = { "dpup", "dpright", "dpdown", "dpleft" };

		/// <summary>The SDL field a box is filled from, with its sign for a stick half: "a", "leftx", "-lefty". Empty for the D-Pad box, which four fields fill.</summary>
		public static string FieldOf(MapCode code)
		{
			foreach (var f in ButtonFields)
				if (f.Value == code)
					return f.Key;
			foreach (var f in TriggerFields)
				if (f.Value == code)
					return f.Key;
			foreach (var f in StickFields)
			{
				if (f.Value[0] == code) return f.Key;
				if (f.Value[1] == code) return "-" + f.Key;
				if (f.Value[2] == code) return "+" + f.Key;
			}
			return code == MapCode.DPad ? string.Join(" ", DPadFields) : "";
		}

		#endregion

		#region Bindings

		enum InputKind { Button, Axis, Hat }

		/// <summary>One binding: the control of the device a field reads.</summary>
		sealed class Binding
		{
			public string Field;
			public string Text;
			public InputKind Kind;
			/// <summary>Button or axis number from 0, or the hat's.</summary>
			public int Index;
			/// <summary>The hat's direction bit: 1 up, 2 right, 4 down, 8 left.</summary>
			public int HatMask;
			/// <summary>+1 the upper half of the axis, -1 the lower half, 0 all of it.</summary>
			public int Half;
			public bool Inverted;

			public override string ToString()
			{
				return Field + ":" + Text;
			}
		}

		static Binding ParseBinding(string field, string text)
		{
			var t = (text ?? "").Trim();
			var b = new Binding { Field = field, Text = t };
			if (t.Length > 0 && (t[0] == '+' || t[0] == '-'))
			{
				b.Half = t[0] == '+' ? 1 : -1;
				t = t.Substring(1);
			}
			if (t.EndsWith("~", StringComparison.Ordinal))
			{
				b.Inverted = true;
				t = t.Substring(0, t.Length - 1);
			}
			if (t.Length < 2)
				return null;
			int n;
			if (t[0] == 'a' && int.TryParse(t.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out n))
			{
				b.Kind = InputKind.Axis;
				b.Index = n;
				return b;
			}
			if (b.Half != 0 || b.Inverted)
				return null;
			if (t[0] == 'b' && int.TryParse(t.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out n))
			{
				b.Kind = InputKind.Button;
				b.Index = n;
				return b;
			}
			var dot = t.IndexOf('.');
			int mask;
			if (t[0] == 'h' && dot > 1
				&& int.TryParse(t.Substring(1, dot - 1), NumberStyles.None, CultureInfo.InvariantCulture, out n)
				&& int.TryParse(t.Substring(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out mask))
			{
				b.Kind = InputKind.Hat;
				b.Index = n;
				b.HatMask = mask;
				return b;
			}
			return null;
		}

		/// <summary>The x360ce number of a hat direction from 1: POV H+1's Up, Right, Down, Left; nought for no single direction.</summary>
		static int HatButton(Binding b)
		{
			var direction = Array.IndexOf(new[] { 1, 2, 4, 8 }, b.HatMask);
			return direction < 0 ? 0 : b.Index * 4 + direction + 1;
		}

		/// <summary>Whether the device has the control a button or hat binding reads.</summary>
		static bool Has(SdlDevice device, Binding b)
		{
			if (b.Kind == InputKind.Button)
				return device.ButtonCount <= 0 || b.Index < device.ButtonCount;
			if (b.Kind == InputKind.Hat)
				return HatButton(b) > 0 && (device.PovCount <= 0 || b.Index < device.PovCount);
			MapType type;
			int index;
			return device.TryGetAxis(b.Index, out type, out index);
		}

		/// <summary>The value a button box takes from a binding, or null when it cannot take it.</summary>
		/// <remarks>A whole axis presses a button over its upper half, as SDL reads it; inverted, over its lower half.</remarks>
		static string ButtonValue(SdlDevice device, Binding b, MapCode code)
		{
			if (b.Kind == InputKind.Button)
				return SettingsConverter.ToIniValue(MapType.Button, b.Index + 1, code);
			if (b.Kind == InputKind.Hat)
				return SettingsConverter.ToIniValue(MapType.DPOVButton, HatButton(b), code);
			MapType type;
			int index;
			device.TryGetAxis(b.Index, out type, out index);
			var slider = type == MapType.Slider;
			if (b.Half != 0 && b.Inverted)
				return null;
			var upper = b.Half > 0 || (b.Half == 0 && !b.Inverted);
			return SettingsConverter.ToIniValue(upper
				? (slider ? MapType.HSlider : MapType.HAxis)
				: (slider ? MapType.IHSlider : MapType.IHAxis), index, code);
		}

		/// <summary>The value a trigger box takes from a binding, or null when it cannot take it.</summary>
		static string TriggerValue(SdlDevice device, Binding b, MapCode code)
		{
			if (b.Kind != InputKind.Axis)
				return ButtonValue(device, b, code);
			MapType type;
			int index;
			device.TryGetAxis(b.Index, out type, out index);
			var slider = type == MapType.Slider;
			if (b.Half != 0 && b.Inverted)
				return null;
			MapType t;
			if (b.Half > 0)
				t = slider ? MapType.HSlider : MapType.HAxis;
			else if (b.Half < 0)
				t = slider ? MapType.IHSlider : MapType.IHAxis;
			else if (b.Inverted)
				t = slider ? MapType.ISlider : MapType.IAxis;
			else
				t = slider ? MapType.Slider : MapType.Axis;
			return SettingsConverter.ToIniValue(t, index, code);
		}

		/// <summary>A stick half's reading in a formula, 0 at rest to 1 at the end, or null when a formula cannot read it.</summary>
		static string HalfTerm(SdlDevice device, Binding b)
		{
			if (b.Kind == InputKind.Button)
				return "b" + (b.Index + 1).ToString(CultureInfo.InvariantCulture);
			if (b.Kind == InputKind.Hat)
				return "d" + HatButton(b).ToString(CultureInfo.InvariantCulture);
			if (b.Half == 0 || b.Inverted)
				return null;
			MapType type;
			int index;
			device.TryGetAxis(b.Index, out type, out index);
			var n = index.ToString(CultureInfo.InvariantCulture);
			if (type == MapType.Slider)
				return b.Half > 0 ? "h" + n : "max(1-2*s" + n + ",0)";
			return b.Half > 0 ? "x" + n : "max(-a" + n + ",0)";
		}

		#endregion

		#region SDL to x360ce

		/// <summary>The mapping a row gives a device: the boxes it fills, where each came from, and what was left out.</summary>
		/// <param name="formulas">
		/// False for x360ce 3, which has no formulas: a stick split over two axes fills its two direction boxes with half
		/// axes instead, and its library reads the stick as the one less the other, as the formula does.
		/// </param>
		public static SdlTranslation ToPadSetting(SdlMapping mapping, SdlDevice device, bool formulas = true)
		{
			var t = new SdlTranslation { Mapping = mapping };
			if (mapping == null)
				return t;
			device = device ?? SdlDevice.Full;
			var buttons = new List<KeyValuePair<MapCode, Binding>>();
			var triggers = new List<KeyValuePair<MapCode, Binding>>();
			// Each stick axis: the whole of it, its low half and its high half, in SDL's direction.
			var sticks = new Dictionary<string, Binding[]>();
			foreach (var field in mapping.Fields)
			{
				var name = field.Key;
				var sign = 0;
				if (name.Length > 1 && (name[0] == '+' || name[0] == '-'))
				{
					sign = name[0] == '+' ? 1 : -1;
					name = name.Substring(1);
				}
				var known = ButtonFields.ContainsKey(name) || TriggerFields.ContainsKey(name) || StickFields.ContainsKey(name);
				if (!known)
				{
					// The row's own notes, such as its face labels or its CRC, are not parts of the pad.
					if (NoPlaceFields.Contains(name))
						t.Skipped.Add(field.Key + ":" + field.Value);
					continue;
				}
				var b = ParseBinding(field.Key, field.Value);
				if (b == null || !Has(device, b) || (sign != 0 && !StickFields.ContainsKey(name) && !(sign > 0 && TriggerFields.ContainsKey(name))))
				{
					t.Skipped.Add(field.Key + ":" + field.Value);
					continue;
				}
				if (ButtonFields.ContainsKey(name))
					buttons.Add(new KeyValuePair<MapCode, Binding>(ButtonFields[name], b));
				else if (TriggerFields.ContainsKey(name))
					triggers.Add(new KeyValuePair<MapCode, Binding>(TriggerFields[name], b));
				else
				{
					Binding[] parts;
					if (!sticks.TryGetValue(name, out parts))
						sticks[name] = parts = new Binding[3];
					var slot = sign == 0 ? 0 : sign < 0 ? 1 : 2;
					if (parts[slot] != null)
						t.Skipped.Add(field.Key + ":" + field.Value);
					else
						parts[slot] = b;
				}
			}
			foreach (var b in buttons)
				Set(t, b.Key, ButtonValue(device, b.Value, b.Key), b.Value);
			foreach (var b in triggers)
				Set(t, b.Key, TriggerValue(device, b.Value, b.Key), b.Value);
			JoinDPad(t, buttons.Select(x => x.Value).ToList());
			foreach (var stick in sticks)
				SetStick(t, device, stick.Key, stick.Value, formulas);
			return t;
		}

		/// <summary>Fills a box, or names the binding as left out when the box cannot take it or another binding filled it.</summary>
		static void Set(SdlTranslation t, MapCode code, string value, params Binding[] from)
		{
			if (string.IsNullOrEmpty(value) || t.Sources.ContainsKey(code))
			{
				t.Skipped.AddRange(from.Select(x => x.ToString()));
				return;
			}
			SetValue(t.Setting, code, value);
			t.Sources[code] = string.Join(", ", from.Select(x => x.ToString()));
		}

		/// <summary>Four D-Pad directions on one hat, as a hat reports them, become the D-Pad box.</summary>
		static void JoinDPad(SdlTranslation t, List<Binding> buttons)
		{
			var hats = DPadFields.Select(f => buttons.FirstOrDefault(x => x.Field == f)).ToArray();
			if (hats.Any(x => x == null || x.Kind != InputKind.Hat) || hats.Select(x => x.Index).Distinct().Count() != 1)
				return;
			for (var i = 0; i < hats.Length; i++)
				if (hats[i].HatMask != 1 << i)
					return;
			foreach (var f in DPadFields)
			{
				var code = ButtonFields[f];
				SetValue(t.Setting, code, null);
				t.Sources.Remove(code);
			}
			SetValue(t.Setting, MapCode.DPad, SettingsConverter.ToIniValue(MapType.POV, hats[0].Index + 1, MapCode.DPad));
			t.Sources[MapCode.DPad] = string.Join(", ", hats.Select(x => x.ToString()));
		}

		/// <summary>Fills a stick axis's box, or its direction boxes, from the whole axis and its two halves.</summary>
		static void SetStick(SdlTranslation t, SdlDevice device, string name, Binding[] parts, bool formulas)
		{
			var codes = StickFields[name];
			// SDL's Y grows downward, the Xbox controller's upward.
			var flip = name.EndsWith("y", StringComparison.Ordinal);
			var whole = parts[0];
			var low = parts[1];
			var high = parts[2];
			// Both halves from the two halves of one axis are that axis.
			if (whole == null && low != null && high != null && low.Kind == InputKind.Axis && high.Kind == InputKind.Axis
				&& low.Index == high.Index && !low.Inverted && !high.Inverted && low.Half == -high.Half)
			{
				var axis = new Binding { Field = name, Text = "a" + low.Index, Kind = InputKind.Axis, Index = low.Index, Inverted = low.Half > 0 };
				var value = AxisValue(device, axis, codes[0], flip);
				if (value != null)
				{
					SetValue(t.Setting, codes[0], value);
					t.Sources[codes[0]] = low + ", " + high;
					return;
				}
			}
			if (whole != null)
			{
				Set(t, codes[0], whole.Kind == InputKind.Axis && whole.Half == 0 ? AxisValue(device, whole, codes[0], flip) : null, whole);
				// Halves beside the whole axis can only be buttons that push the stick to its end.
				if (low != null)
					Set(t, codes[1], low.Kind == InputKind.Axis ? null : ButtonValue(device, low, codes[1]), low);
				if (high != null)
					Set(t, codes[2], high.Kind == InputKind.Axis ? null : ButtonValue(device, high, codes[2]), high);
				return;
			}
			var axes = (low != null && low.Kind == InputKind.Axis) || (high != null && high.Kind == InputKind.Axis);
			if (!axes)
			{
				// Buttons and hat directions push the stick to its end: its direction boxes.
				if (low != null)
					Set(t, codes[1], ButtonValue(device, low, codes[1]), low);
				if (high != null)
					Set(t, codes[2], ButtonValue(device, high, codes[2]), high);
				return;
			}
			var lowTerm = low == null ? null : HalfTerm(device, low);
			var highTerm = high == null ? null : HalfTerm(device, high);
			var from = new[] { low, high }.Where(x => x != null).ToArray();
			if ((low != null && lowTerm == null) || (high != null && highTerm == null))
			{
				t.Skipped.AddRange(from.Select(x => x.ToString()));
				return;
			}
			if (!formulas)
			{
				// Each half in its direction box: a half axis there reads as far as it is turned.
				if (low != null)
					Set(t, codes[1], ButtonValue(device, low, codes[1]), low);
				if (high != null)
					Set(t, codes[2], ButtonValue(device, high, codes[2]), high);
				return;
			}
			// The stick's value is its high half less its low half, turned round for Y.
			var plus = flip ? lowTerm : highTerm;
			var minus = flip ? highTerm : lowTerm;
			var formula = MapExpression.Prefix + (plus ?? "") + (minus == null ? "" : "-" + minus);
			Set(t, codes[0], formula, from);
		}

		/// <summary>The value a stick axis box takes from a whole axis.</summary>
		static string AxisValue(SdlDevice device, Binding b, MapCode code, bool flip)
		{
			MapType type;
			int index;
			if (!device.TryGetAxis(b.Index, out type, out index))
				return null;
			var inverted = b.Inverted != flip;
			var slider = type == MapType.Slider;
			return SettingsConverter.ToIniValue(slider
				? (inverted ? MapType.ISlider : MapType.Slider)
				: (inverted ? MapType.IAxis : MapType.Axis), index, code);
		}

		#endregion

		#region x360ce to SDL

		/// <summary>Every mapping box, the D-Pad box last, so a D-Pad direction with a box of its own keeps it.</summary>
		static readonly MapCode[] Boxes =
		{
			MapCode.ButtonA, MapCode.ButtonB, MapCode.ButtonX, MapCode.ButtonY,
			MapCode.ButtonBack, MapCode.ButtonGuide, MapCode.ButtonStart,
			MapCode.LeftThumbButton, MapCode.RightThumbButton, MapCode.LeftShoulder, MapCode.RightShoulder,
			MapCode.DPadUp, MapCode.DPadDown, MapCode.DPadLeft, MapCode.DPadRight,
			MapCode.LeftTrigger, MapCode.RightTrigger,
			MapCode.LeftThumbAxisX, MapCode.LeftThumbAxisY, MapCode.RightThumbAxisX, MapCode.RightThumbAxisY,
			MapCode.LeftThumbLeft, MapCode.LeftThumbRight, MapCode.LeftThumbUp, MapCode.LeftThumbDown,
			MapCode.RightThumbLeft, MapCode.RightThumbRight, MapCode.RightThumbUp, MapCode.RightThumbDown,
			MapCode.DPad,
		};

		/// <summary>An x360ce control as an SDL binding of a button box; null when SDL cannot say it.</summary>
		/// <remarks>A whole axis presses an x360ce button almost all its way, SDL's over its upper half; the copy gives SDL's nearest.</remarks>
		static string ButtonBinding(SdlDevice device, MapType type, int index)
		{
			switch (type)
			{
				case MapType.Button:
					return "b" + (index - 1).ToString(CultureInfo.InvariantCulture);
				case MapType.DPOVButton:
					return "h" + ((index - 1) / 4).ToString(CultureInfo.InvariantCulture) + "." + (1 << ((index - 1) % 4)).ToString(CultureInfo.InvariantCulture);
			}
			var k = AxisNumber(device, type, index);
			if (k < 0)
				return null;
			switch (type)
			{
				case MapType.HAxis:
				case MapType.HSlider:
					return "+a" + k;
				case MapType.IHAxis:
				case MapType.IHSlider:
					return "-a" + k;
				case MapType.Axis:
				case MapType.Slider:
					return "a" + k;
				case MapType.IAxis:
				case MapType.ISlider:
					return "a" + k + "~";
			}
			return null;
		}

		/// <summary>SDL's number for an x360ce axis or slider of the device, or -1.</summary>
		static int AxisNumber(SdlDevice device, MapType type, int index)
		{
			if (SettingsConverter.IsAxis(type))
				return device.SdlAxisOf(false, index);
			if (SettingsConverter.IsSlider(type))
				return device.SdlAxisOf(true, index);
			return -1;
		}

		/// <summary>
		/// The device's mapping as an SDL row: what SDL can say of it, buttons, axes, half axes and hat directions. Formulas,
		/// inverted buttons and every setting that is not a mapping are left out.
		/// </summary>
		/// <param name="setting">The mapping.</param>
		/// <param name="device">The device the mapping reads, whose controls give SDL's numbers.</param>
		/// <param name="name">The name the row gives the controller; the device's product name when null.</param>
		public static SdlMapping ToMapping(PadSetting setting, SdlDevice device, string name = null)
		{
			device = device ?? SdlDevice.Full;
			var fields = new List<KeyValuePair<string, string>>();
			var taken = new HashSet<string>();
			foreach (var code in Boxes)
			{
				var value = GetValue(setting, code);
				MapType type;
				int index;
				if (string.IsNullOrEmpty(value) || MapExpression.IsExpression(value) || !SettingsConverter.TryParseIniValue(value, out type, out index, code))
					continue;
				var field = FieldOf(code);
				string binding = null;
				if (code == MapCode.DPad)
				{
					if (type != MapType.POV)
						continue;
					for (var d = 0; d < DPadFields.Length; d++)
						if (!taken.Contains(DPadFields[d]))
							fields.Add(new KeyValuePair<string, string>(DPadFields[d], "h" + (index - 1).ToString(CultureInfo.InvariantCulture) + "." + (1 << d).ToString(CultureInfo.InvariantCulture)));
					continue;
				}
				var stick = StickFields.FirstOrDefault(x => x.Value[0] == code);
				if (stick.Key != null)
				{
					if (type != MapType.Axis && type != MapType.IAxis && type != MapType.Slider && type != MapType.ISlider)
						continue;
					var k = AxisNumber(device, type, index);
					if (k < 0)
						continue;
					var inverted = SettingsConverter.IsInverted(type) != stick.Key.EndsWith("y", StringComparison.Ordinal);
					binding = "a" + k + (inverted ? "~" : "");
				}
				else if (type != MapType.IButton && type != MapType.POV && type != MapType.IPOV)
				{
					// A stick's direction box takes the same controls as a button box, and a whole axis there means nothing to SDL.
					var direction = StickFields.Any(x => x.Value[1] == code || x.Value[2] == code);
					if (direction && (type == MapType.Axis || type == MapType.IAxis || type == MapType.Slider || type == MapType.ISlider))
						continue;
					binding = ButtonBinding(device, type, index);
				}
				if (binding == null)
					continue;
				fields.Add(new KeyValuePair<string, string>(field, binding));
				taken.Add(field);
			}
			fields.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
			name = string.IsNullOrWhiteSpace(name) ? device.ProductName : name;
			return SdlMapping.Create(device.Guid, string.IsNullOrWhiteSpace(name) ? "Controller" : name, fields);
		}

		#endregion

		#region Boxes

		/// <summary>A mapping box's value in a preset; empty when unset.</summary>
		public static string GetValue(PadSetting setting, MapCode code)
		{
			var p = typeof(PadSetting).GetProperty(code.ToString());
			return setting == null || p == null ? "" : (string)p.GetValue(setting, null) ?? "";
		}

		/// <summary>Sets a mapping box's value in a preset; null or empty clears it. The model keeps an empty box as empty text, never null.</summary>
		public static void SetValue(PadSetting setting, MapCode code, string value)
		{
			var p = typeof(PadSetting).GetProperty(code.ToString());
			if (p != null)
				p.SetValue(setting, value ?? "", null);
		}

		/// <summary>Every mapping box.</summary>
		public static MapCode[] MappingBoxes
		{
			get { return Boxes.ToArray(); }
		}

		#endregion
	}
}
