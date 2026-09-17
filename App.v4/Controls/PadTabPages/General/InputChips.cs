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
	/// The lit rules are the ones v5's General tab uses, so the two lines agree on what "moved"
	/// means.
	/// </remarks>
	public static class InputChips
	{
		/// <summary>An axis at rest reads 32767 in the middle of its 0 to 65535 travel.</summary>
		public const int AxisCentre = 32767;

		/// <summary>How far from the centre an axis must move to light up.</summary>
		public const int AxisDeadZone = 8000;

		/// <summary>How far a slider must move from zero to light up.</summary>
		public const int SliderThreshold = 8000;

		/// <summary>A POV reads -1 while it is not pressed.</summary>
		public const int PovRest = -1;

		static readonly int[] PovDirectionValues = { 0, 9000, 18000, 27000 };
		static readonly string[] PovDirectionCaptions = { "U", "R", "D", "L" };

		/// <summary>The chips for a device with these controls, in the order the panel shows them.</summary>
		/// <param name="buttonCount">Buttons, from the device's capabilities.</param>
		/// <param name="axisMask">Bit i set when the device answers to axis slot i (24 slots).</param>
		/// <param name="sliderMask">Bit i set when the device answers to slider slot i (8 slots).</param>
		/// <param name="povCount">POVs, from the device's capabilities.</param>
		public static List<InputChip> Create(int buttonCount, int axisMask, int sliderMask, int povCount)
		{
			var chips = new List<InputChip>();
			var buttons = System.Math.Min(System.Math.Max(buttonCount, 0), CustomDiHelper.ButtonOffsets.Count);
			for (var i = 0; i < buttons; i++)
				chips.Add(New(InputChipKind.Button, i, 0, (i + 1).ToString(), SettingsConverter.ToTextValue(MapType.Button, i + 1)));
			for (var i = 0; i < CustomDiState.MaxAxis; i++)
				if ((axisMask & (1 << i)) != 0)
					chips.Add(New(InputChipKind.Axis, i, 0, (i + 1).ToString(), SettingsConverter.ToTextValue(MapType.Axis, i + 1)));
			for (var i = 0; i < CustomDiState.MaxSliders; i++)
				if ((sliderMask & (1 << i)) != 0)
					chips.Add(New(InputChipKind.Slider, i, 0, (i + 1).ToString(), SettingsConverter.ToTextValue(MapType.Slider, i + 1)));
			var povs = System.Math.Min(System.Math.Max(povCount, 0), CustomDiHelper.PovOffsets.Count);
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
		public static bool Update(IList<InputChip> chips, CustomDiState state)
		{
			var changed = false;
			for (var i = 0; i < chips.Count; i++)
			{
				var chip = chips[i];
				int value;
				bool lit;
				Read(chip, state, out value, out lit);
				if (chip.Value != value || chip.Lit != lit)
				{
					chip.Value = value;
					chip.Lit = lit;
					changed = true;
				}
			}
			return changed;
		}

		static void Read(InputChip chip, CustomDiState state, out int value, out bool lit)
		{
			switch (chip.Kind)
			{
				case InputChipKind.Button:
					lit = state != null && chip.Index < state.Buttons.Length && state.Buttons[chip.Index];
					value = lit ? 1 : 0;
					return;
				case InputChipKind.Axis:
					value = state != null && chip.Index < state.Axis.Length ? state.Axis[chip.Index] : AxisCentre;
					lit = state != null && (value < AxisCentre - AxisDeadZone || value > AxisCentre + AxisDeadZone);
					return;
				case InputChipKind.Slider:
					value = state != null && chip.Index < state.Sliders.Length ? state.Sliders[chip.Index] : 0;
					lit = value > SliderThreshold;
					return;
				case InputChipKind.Pov:
					value = state != null && chip.Index < state.Povs.Length ? state.Povs[chip.Index] : PovRest;
					lit = value > PovRest;
					return;
				default:
					var pov = state != null && chip.Index < state.Povs.Length ? state.Povs[chip.Index] : PovRest;
					lit = pov == PovDirectionValues[chip.Direction];
					value = lit ? pov : PovRest;
					return;
			}
		}
	}
}
