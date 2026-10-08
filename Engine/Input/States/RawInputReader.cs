using System;
using System.Collections.Generic;

namespace x360ce.Engine
{
	/// <summary>Turns the reports of one Raw Input device into its <see cref="SourceState"/>, in DirectInput's ranges and slots.</summary>
	/// <remarks>
	/// Everything a report needs is made with the reader: the list HidP_GetData fills, each control's slot and range,
	/// and the buttons each report ID carries. Reading a report makes nothing, takes no lock and throws nothing; a
	/// report the HID parser refuses leaves the state as it was. One reader serves one device on one thread, and the
	/// device must not be disposed while the reader is used.
	/// </remarks>
	public sealed class RawInputReader
	{
		/// <summary>DirectInput's range of an axis or slider: 0 to 65535.</summary>
		const int AxisMax = 65535;

		/// <summary>The value DirectInput gives the centre of an axis; the next step up reads one more.</summary>
		const int AxisCenter = 32767;

		/// <summary>A hat switch turned all the way round, in hundredths of a degree.</summary>
		const int PovTurn = 36000;

		/// <summary>The value DirectInput gives a centred hat switch.</summary>
		const int PovCentered = -1;

		readonly RawInputDevice device;
		readonly RawInputNative.HIDP_DATA[] data;
		readonly MapType[] types;
		readonly int[] indexes;
		readonly ValueRange[] ranges;

		/// <summary>The button slots each report ID carries, which a report of that ID clears before setting those that are on.</summary>
		readonly int[][] buttonsByReport = new int[256][];

		public RawInputReader(RawInputDevice device, RawInputLayout layout)
		{
			this.device = device;
			var controls = device.Controls;
			var count = Math.Min(controls.Length, layout.Types.Length);
			types = new MapType[count];
			indexes = new int[count];
			ranges = new ValueRange[count];
			var buttons = new List<int>[buttonsByReport.Length];
			for (var i = 0; i < count; i++)
			{
				var c = controls[i];
				var type = layout.Types[i];
				if (c == null)
					continue;
				// A button goes only into a button slot, and a value only into an axis, slider or hat.
				if (c.IsButton != (type == MapType.Button))
					continue;
				types[i] = type;
				indexes[i] = layout.Indexes[i];
				if (c.IsButton)
				{
					var id = c.ReportId & 0xFF;
					buttons[id] = buttons[id] ?? new List<int>();
					buttons[id].Add(indexes[i]);
				}
				else
				{
					ranges[i] = ValueRange.Of(c);
				}
			}
			for (var id = 0; id < buttons.Length; id++)
				if (buttons[id] != null)
					buttonsByReport[id] = buttons[id].ToArray();
			var max = (int)RawInputNative.HidP_MaxDataListLength(RawInputNative.HidP_Input, device.PreparsedData);
			data = new RawInputNative.HIDP_DATA[Math.Max(1, max)];
		}

		/// <summary>Puts every axis, slider and button at 0 and centres every hat, as DirectInput reads a control a device does not have.</summary>
		/// <remarks>A device's state starts here, so a control no report has carried yet reads as DirectInput would read it.</remarks>
		public static void Reset(SourceState state)
		{
			Array.Clear(state.Axis, 0, state.Axis.Length);
			Array.Clear(state.Sliders, 0, state.Sliders.Length);
			Array.Clear(state.Buttons, 0, state.Buttons.Length);
			for (var i = 0; i < state.Povs.Length; i++)
				state.Povs[i] = PovCentered;
		}

		/// <summary>Sets the state DirectInput reads before a device's first report: every axis and slider the device has at the centre, 32767.</summary>
		/// <remarks>
		/// A gamepad reports only when something moves, so until it is touched its state is this one. Left at 0, a stick
		/// that has sent nothing yet would read as held fully left and up. Slots the device does not have stay 0, hats
		/// centred and buttons up, as in <see cref="Reset"/>.
		/// </remarks>
		public void Rest(SourceState state)
		{
			Reset(state);
			for (var i = 0; i < types.Length; i++)
			{
				if (types[i] == MapType.Axis)
					state.Axis[indexes[i]] = AxisCenter;
				else if (types[i] == MapType.Slider)
					state.Sliders[indexes[i]] = AxisCenter;
			}
		}

		/// <summary>Writes one input report into <paramref name="state"/>.</summary>
		/// <remarks>
		/// Every control the report carries is written. The buttons of the report's ID are cleared and those that are on
		/// set, since HidP_GetData lists only the buttons that are on; controls of other report IDs keep their values.
		/// </remarks>
		/// <param name="buffer">Holds the report, which starts with its report ID, or 0 when the device numbers no reports.</param>
		/// <returns>False when the HID parser refuses the report, which leaves the state as it was.</returns>
		public unsafe bool ReadReport(byte[] buffer, int offset, int length, SourceState state)
		{
			var preparsedData = device.PreparsedData;
			if (preparsedData == IntPtr.Zero || buffer == null || offset < 0 || length <= 0 || length > buffer.Length - offset)
				return false;
			var count = (uint)data.Length;
			int status;
			fixed (RawInputNative.HIDP_DATA* list = data)
			fixed (byte* report = buffer)
				status = RawInputNative.HidP_GetData(RawInputNative.HidP_Input, list, ref count, preparsedData, report + offset, (uint)length);
			if (status != RawInputNative.HIDP_STATUS_SUCCESS)
				return false;
			var buttons = buttonsByReport[buffer[offset]];
			if (buttons != null)
				for (var i = 0; i < buttons.Length; i++)
					state.Buttons[buttons[i]] = false;
			var n = (int)count;
			for (var i = 0; i < n; i++)
			{
				int index = data[i].DataIndex;
				if (index >= types.Length)
					continue;
				switch (types[index])
				{
					case MapType.Axis:
						state.Axis[indexes[index]] = ranges[index].ToAxis(data[i].RawValue);
						break;
					case MapType.Slider:
						state.Sliders[indexes[index]] = ranges[index].ToAxis(data[i].RawValue);
						break;
					case MapType.POV:
						state.Povs[indexes[index]] = ranges[index].ToPov(data[i].RawValue);
						break;
					case MapType.Button:
						state.Buttons[indexes[index]] = true;
						break;
				}
			}
			return true;
		}

		/// <summary>Writes every report one WM_INPUT carries into <paramref name="state"/>, in the order they came, so the last one wins.</summary>
		/// <param name="rawInput">A RAWINPUT as GetRawInputData copies it: RAWINPUTHEADER, then RAWHID with its dwSizeHid, its dwCount, and dwCount reports of dwSizeHid bytes each.</param>
		/// <returns>The number of reports written; 0 when the input is not from a HID device.</returns>
		public int ReadRawInput(byte[] rawInput, SourceState state)
		{
			var header = RawInputNative.RAWINPUTHEADER_Size;
			if (rawInput == null || rawInput.Length < header + 8 || BitConverter.ToUInt32(rawInput, 0) != RawInputNative.RIM_TYPEHID)
				return 0;
			var end = Math.Min(BitConverter.ToInt32(rawInput, 4), rawInput.Length);
			var size = BitConverter.ToInt32(rawInput, header);
			var count = BitConverter.ToInt32(rawInput, header + 4);
			var offset = header + 8;
			var read = 0;
			for (var i = 0; i < count && size > 0 && offset + size <= end; i++, offset += size)
				if (ReadReport(rawInput, offset, size, state))
					read++;
			return read;
		}

		#region Conversion

		/// <summary>What DirectInput reads for a value control at <paramref name="rawValue"/>, as an axis or slider: 0 to 65535.</summary>
		public static int ToAxis(RawInputControl control, uint rawValue)
		{
			return ValueRange.Of(control).ToAxis(rawValue);
		}

		/// <summary>What DirectInput reads for a hat switch at <paramref name="rawValue"/>: hundredths of a degree clockwise from up, or -1 when centred.</summary>
		public static int ToPov(RawInputControl control, uint rawValue)
		{
			return ValueRange.Of(control).ToPov(rawValue);
		}

		/// <summary>A value control's range, worked out once, and the reading of a raw value against it.</summary>
		struct ValueRange
		{
			/// <summary>The bits the value has.</summary>
			uint Mask;

			/// <summary>The sign bit of a signed value; 0 for an unsigned one.</summary>
			long SignBit;

			long Min;
			long Max;

			/// <remarks>
			/// A range with a negative minimum is signed, and the value is sign-extended from its bit size. A maximum
			/// below the minimum is an unsigned range written as signed numbers, which the Xbox One controller declares
			/// as 0 to -1 for its 16-bit axes: both ends are read as unsigned numbers of the value's size, 0 to 65535.
			/// </remarks>
			public static ValueRange Of(RawInputControl c)
			{
				var bits = c.BitSize;
				var range = new ValueRange
				{
					Mask = bits > 0 && bits < 32 ? (1u << bits) - 1 : uint.MaxValue,
					Min = c.LogicalMin,
					Max = c.LogicalMax,
				};
				if (range.Max < range.Min)
				{
					range.Min = (uint)c.LogicalMin & range.Mask;
					range.Max = (uint)c.LogicalMax & range.Mask;
				}
				else if (range.Min < 0)
				{
					range.SignBit = 1L << ((bits > 0 && bits <= 32 ? bits : 32) - 1);
				}
				return range;
			}

			long ValueOf(uint rawValue)
			{
				long value = rawValue & Mask;
				if ((value & SignBit) != 0)
					value -= SignBit << 1;
				return value;
			}

			/// <remarks>
			/// DirectInput's own scaling, found from what it read for real controllers: the centre is
			/// (min + max + 1) / 2 and reads 32767. Below it, min to the centre scales to 0 to 32767; above it, the next
			/// value up to max scales to 32768 to 65535. Each piece rounds to the nearest, a half up. So an 8-bit
			/// 0 to 255 axis reads 127 as 32511, 128 as 32767, 129 as 32768, and 64 as 16384; a 14-bit wheel reads 8192
			/// as 32767. Values outside the range read as its ends.
			/// </remarks>
			public int ToAxis(uint rawValue)
			{
				var value = ValueOf(rawValue);
				if (value <= Min)
					return 0;
				if (value >= Max)
					return AxisMax;
				var center = (Min + Max + 1) >> 1;
				if (value <= center)
					return (int)(((value - Min) * 2 * AxisCenter + (center - Min)) / (2 * (center - Min)));
				var steps = Max - center - 1;
				return AxisCenter + 1 + (int)(((value - center - 1) * 2 * AxisCenter + steps) / (2 * steps));
			}

			/// <remarks>
			/// The range is one turn split into equal steps, the first pointing up. A value outside it is centred: the
			/// Logitech RumblePad 2 and G27 report 0 to 7 and 8 when centred, the Xbox One controller 1 to 8 and 0.
			/// </remarks>
			public int ToPov(uint rawValue)
			{
				var value = ValueOf(rawValue);
				if (value < Min || value > Max)
					return PovCentered;
				return (int)((value - Min) * PovTurn / (Max - Min + 1));
			}
		}

		#endregion
	}
}
