using System.Collections.Generic;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>What kind of control a chip stands for.</summary>
	public enum InputChipKind
	{
		Button,
		Axis,
		Slider,
		Pov,
		/// <summary>One of the four directions of a POV, shown beside it as U, R, D, L.</summary>
		PovDirection,
	}

	/// <summary>
	/// One cell of the input panel: which control of the device it stands for, what that control
	/// reads now, and the text that maps it.
	/// </summary>
	public sealed class InputChip
	{
		public InputChipKind Kind;

		/// <summary>Zero-based index into the state array of its kind; for a direction, the POV's index.</summary>
		public int Index;

		/// <summary>For a direction: 0 up, 1 right, 2 down, 3 left.</summary>
		public int Direction;

		/// <summary>What the chip shows: the one-based number, or the direction letter.</summary>
		public string Caption;

		/// <summary>What lands in a mapping box: "Button 6", "Axis 1", "Slider 2", "POV 1", "POV 1 Up".</summary>
		public string Payload;

		/// <summary>True while the control is pressed or moved.</summary>
		public bool Lit;

		/// <summary>The raw reading. Buttons read 0 or 1.</summary>
		public int Value;

		/// <summary>For an axis or a slider: the reading it last moved from.</summary>
		public int Anchor;

		/// <summary>Whether <see cref="Anchor"/> has been taken from a reading.</summary>
		public bool Anchored;

		/// <summary>For an axis or a slider: when it last moved, in milliseconds of <see cref="InputChips.Now"/>.</summary>
		public long MovedAt = long.MinValue / 2;

		/// <summary>Whether the reading is drawn under the chip. A button's is not: lit says it all.</summary>
		public bool ShowsValue { get { return Kind != InputChipKind.Button; } }
	}

	/// <summary>
	/// Builds the chips a device has and keeps them in step with what it reads.
	/// </summary>
	/// <remarks>
	/// Which chips exist comes from the device, not from fixed maximums: buttons and POVs are
	/// counted, axes and sliders are the slots the device answers to, read from the same masks
	/// the mapping list uses, so a stick with X, Y and RZ shows axes 1, 2 and 6 in both places.
	///
	/// An axis or a slider lights while it moves and for <see cref="HoldMs"/> after, wherever it rests.
	/// A pedal or a throttle rests at one end, a stick in the middle and a switch anywhere, and nothing
	/// says which, so a rule about where a control rests lit pedals and sliders all the time. Wobble
	/// smaller than <see cref="MoveStep"/> is not movement. v5's General tab
	/// still lights an axis away from the middle and a slider above an eighth of its travel.
	///
	/// v5's General tab over-counts axes from <c>CapAxeCount</c>, numbers a sparse device's axes
	/// contiguously and reads the wrong POV for the buttons of a second POV
	/// (<c>PadItem_GeneralControl.xaml.cs</c> lines 449-453). None of that is copied here. v5
	/// also has a RawInput HID capability parser, which v4 does not.
	/// </remarks>
	public static class InputChips
	{
		/// <summary>What an axis chip shows with no device: the middle of the 0 to 65535 travel.</summary>
		public const int AxisCentre = 32767;

		/// <summary>A POV reads -1 while it is not pressed.</summary>
		public const int PovRest = -1;

		/// <summary>How long an axis or a slider stays lit after it last moved, in milliseconds.</summary>
		/// <remarks>
		/// The time Windows blinks the text cursor by default. It still spans two interface ticks while the window is
		/// behind, five a second, so a control moving slowly does not blink.
		/// </remarks>
		public const int HoldMs = 530;

		/// <summary>How far an axis or a slider must go from where it last moved from to count as moving, of 65535.</summary>
		/// <remarks>
		/// Just over two steps of an 8-bit axis, 257 each, so a reading that wobbles a step either side of where it rests
		/// never reaches it, while a slow turn or press does: on a 900-degree wheel it is about 7 degrees.
		/// </remarks>
		public const int MoveStep = 520;

		static readonly string[] PovDirectionCaptions = { "U", "R", "D", "L" };

		static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

		/// <summary>The time chips are lit by, in milliseconds since the panel first asked.</summary>
		public static long Now { get { return Clock.ElapsedMilliseconds; } }

		/// <summary>The chips for a device with these controls, in the order the panel shows them.</summary>
		/// <param name="buttonCount">Buttons, from the device's capabilities.</param>
		/// <param name="axisMask">Bit i set when the device answers to axis slot i (24 slots).</param>
		/// <param name="sliderMask">Bit i set when the device answers to slider slot i (8 slots).</param>
		/// <param name="povCount">POVs, from the device's capabilities.</param>
		public static List<InputChip> Create(int buttonCount, int axisMask, int sliderMask, int povCount)
		{
			var chips = new List<InputChip>();
			var buttons = System.Math.Min(System.Math.Max(buttonCount, 0), DirectInputLayout.ButtonOffsets.Count);
			for (var i = 0; i < buttons; i++)
				chips.Add(New(InputChipKind.Button, i, 0, (i + 1).ToString(), SettingsConverter.ToTextValue(MapType.Button, i + 1)));
			for (var i = 0; i < SourceState.MaxAxis; i++)
				if ((axisMask & (1 << i)) != 0)
					chips.Add(New(InputChipKind.Axis, i, 0, (i + 1).ToString(), SettingsConverter.ToTextValue(MapType.Axis, i + 1)));
			for (var i = 0; i < SourceState.MaxSliders; i++)
				if ((sliderMask & (1 << i)) != 0)
					chips.Add(New(InputChipKind.Slider, i, 0, (i + 1).ToString(), SettingsConverter.ToTextValue(MapType.Slider, i + 1)));
			var povs = System.Math.Min(System.Math.Max(povCount, 0), DirectInputLayout.PovOffsets.Count);
			for (var i = 0; i < povs; i++)
			{
				chips.Add(New(InputChipKind.Pov, i, 0, (i + 1).ToString(), SettingsConverter.ToTextValue(MapType.POV, i + 1)));
				for (var d = 0; d < PovDirectionCaptions.Length; d++)
					chips.Add(New(InputChipKind.PovDirection, i, d, PovDirectionCaptions[d], SettingsConverter.ToTextValue(MapType.DPOVButton, i * 4 + d + 1)));
			}
			return chips;
		}

		static InputChip New(InputChipKind kind, int index, int direction, string caption, string payload)
		{
			return new InputChip { Kind = kind, Index = index, Direction = direction, Caption = caption, Payload = payload, Value = kind == InputChipKind.Pov || kind == InputChipKind.PovDirection ? PovRest : 0 };
		}

		/// <summary>
		/// Brings every chip in line with the state. Returns true when any chip's light or reading
		/// changed, so the caller repaints only then. A null state puts every chip at rest.
		/// </summary>
		/// <param name="nowMs">The time, in milliseconds that never go back: <see cref="Now"/>.</param>
		public static bool Update(IList<InputChip> chips, SourceState state, long nowMs)
		{
			var changed = false;
			for (var i = 0; i < chips.Count; i++)
			{
				var chip = chips[i];
				int value;
				bool lit;
				Read(chip, state, nowMs, out value, out lit);
				if (chip.Value != value || chip.Lit != lit)
				{
					chip.Value = value;
					chip.Lit = lit;
					changed = true;
				}
			}
			return changed;
		}

		static void Read(InputChip chip, SourceState state, long nowMs, out int value, out bool lit)
		{
			switch (chip.Kind)
			{
				case InputChipKind.Button:
					lit = state != null && chip.Index < state.Buttons.Length && state.Buttons[chip.Index];
					value = lit ? 1 : 0;
					return;
				case InputChipKind.Axis:
					value = state != null && chip.Index < state.Axis.Length ? state.Axis[chip.Index] : AxisCentre;
					lit = state != null && Moved(chip, value, nowMs);
					return;
				case InputChipKind.Slider:
					value = state != null && chip.Index < state.Sliders.Length ? state.Sliders[chip.Index] : 0;
					lit = state != null && Moved(chip, value, nowMs);
					return;
				case InputChipKind.Pov:
					value = state != null && chip.Index < state.Povs.Length ? state.Povs[chip.Index] : PovRest;
					lit = value > PovRest;
					return;
				default:
					var pov = state != null && chip.Index < state.Povs.Length ? state.Povs[chip.Index] : PovRest;
					lit = ConvertHelper.IsPovDirectionPressed(pov, chip.Direction);
					value = lit ? pov : PovRest;
					return;
			}
		}

		/// <summary>Whether an axis or a slider has moved within the last <see cref="HoldMs"/>.</summary>
		/// <remarks>
		/// A move is a reading further than <see cref="MoveStep"/> from where the control
		/// last moved from, which then becomes the new place to move from. A slow slide adds up until it is a move; a
		/// wobble around one place never is. The first reading only sets that place.
		/// </remarks>
		static bool Moved(InputChip chip, int value, long nowMs)
		{
			if (!chip.Anchored)
			{
				chip.Anchor = value;
				chip.Anchored = true;
			}
			else if (System.Math.Abs(value - chip.Anchor) > MoveStep)
			{
				chip.Anchor = value;
				chip.MovedAt = nowMs;
			}
			return nowMs - chip.MovedAt < HoldMs;
		}
	}
}
