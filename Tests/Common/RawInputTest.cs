// @under-test: Engine/Input/Devices/RawInputDevice.cs, Engine/Input/Native/RawInputNative.cs, Engine/Input/Layouts/RawInputLayout.cs, Engine/Input/Processors/RawInputReader.cs, Engine/Input/Devices/RawInputControl.cs, Engine/Input/Devices/RawInputDescription.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A controller read through Raw Input gives the state DirectInput gives for it: the same values, in the same
	/// slots, so a mapping made on either works for the other.
	/// </summary>
	/// <remarks>
	/// The recorded tests replay reports real controllers sent (see <see cref="RawInputFixtures"/>) and need no
	/// hardware. The attached-controller test only lists devices and reads their paths: it opens no device for
	/// reading, acquires nothing and sends nothing.
	/// </remarks>
	[TestClass]
	public class RawInputTest
	{
		const int GenericDesktopPage = 1;
		const int ButtonPage = 9;

		static RawInputDevice Device(RawInputFixture fixture)
		{
			var device = RawInputDevice.FromPreparsedData(fixture.PreparsedBytes());
			Assert.IsNotNull(device, fixture.Name + ": the HID parser refused the recorded preparsed data.");
			return device;
		}

		static RawInputReader TwinReader(RawInputDevice device, RawInputFixture fixture)
		{
			return new RawInputReader(device, RawInputLayout.FromTwin(device.Controls, fixture.Objects));
		}

		/// <summary>The one control with the usage given; the test fails when there is not exactly one.</summary>
		static RawInputControl Control(RawInputDevice device, int usagePage, int usage, bool isButton = false)
		{
			var found = device.Controls.Where(x => x != null && x.UsagePage == usagePage && x.Usage == usage && x.IsButton == isButton).ToArray();
			Assert.AreEqual(1, found.Length, string.Format("Controls with usage {0:X}:{1:X2}.", usagePage, usage));
			return found[0];
		}

		static SourceState Read(RawInputReader reader, RawInputSample sample)
		{
			var state = new SourceState();
			RawInputReader.Reset(state);
			var report = sample.Bytes();
			Assert.IsTrue(reader.ReadReport(report, 0, report.Length, state), sample.Shows + ": the HID parser refused the report.");
			return state;
		}

		/// <summary>Asserts every slot of <paramref name="state"/> holds what the sample recorded, and every slot it does not name holds what DirectInput gives a control a device lacks.</summary>
		internal static void AssertState(RawInputFixture fixture, RawInputSample sample, SourceState state)
		{
			var where = fixture.Name + ", " + sample.Shows + " (" + sample.Report + ")";
			var axis = new int[SourceState.MaxAxis];
			Array.Copy(sample.Axes, axis, sample.Axes.Length);
			var sliders = new int[SourceState.MaxSliders];
			Array.Copy(sample.Sliders, sliders, sample.Sliders.Length);
			var buttons = new bool[state.Buttons.Length];
			foreach (var b in sample.Buttons)
				buttons[b] = true;
			CollectionAssert.AreEqual(axis, state.Axis, where + ": axes differ.");
			CollectionAssert.AreEqual(sliders, state.Sliders, where + ": sliders differ.");
			CollectionAssert.AreEqual(sample.Povs, state.Povs, where + ": hat switches differ.");
			CollectionAssert.AreEqual(buttons, state.Buttons, where + ": buttons differ.");
		}

		/// <summary>A RAWINPUT as GetRawInputData copies it, carrying <paramref name="reports"/> in one WM_INPUT.</summary>
		internal static byte[] RawInput(params byte[][] reports)
		{
			var header = RawInputNative.RAWINPUTHEADER_Size;
			var size = reports[0].Length;
			var input = new byte[header + 8 + size * reports.Length];
			BitConverter.GetBytes(RawInputNative.RIM_TYPEHID).CopyTo(input, 0);
			BitConverter.GetBytes(input.Length).CopyTo(input, 4);
			BitConverter.GetBytes(size).CopyTo(input, header);
			BitConverter.GetBytes(reports.Length).CopyTo(input, header + 4);
			for (var i = 0; i < reports.Length; i++)
				reports[i].CopyTo(input, header + 8 + i * size);
			return input;
		}

		static RawInputSample Sample(RawInputFixture fixture, string shows)
		{
			return fixture.Samples.Single(x => x.Shows == shows);
		}

		#region Recorded controllers

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Every recorded RumblePad 2 and G27 report reads as the state DirectInput read for its twin, slot for slot")]
		public void Recorded_reports_read_as_DirectInput_read_them()
		{
			foreach (var fixture in new[] { RawInputFixtures.RumblePad2, RawInputFixtures.G27 })
			{
				using (var device = Device(fixture))
				{
					var reader = TwinReader(device, fixture);
					foreach (var sample in fixture.Samples)
						AssertState(fixture, sample, Read(reader, sample));
				}
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each data index of a recorded controller is one control, a second report ID's and a vendor page's included")]
		public void Each_data_index_is_one_control()
		{
			var fixtures = new[] { RawInputFixtures.RumblePad2, RawInputFixtures.XboxOne, RawInputFixtures.G27 };
			// NumberInputDataIndices of each, as the HID parser reports it.
			var indexes = new[] { 19, 22, 31 };
			for (var f = 0; f < fixtures.Length; f++)
			{
				using (var device = Device(fixtures[f]))
				{
					Assert.AreEqual(indexes[f], device.Controls.Length, fixtures[f].Name + ": controls.");
					for (var i = 0; i < device.Controls.Length; i++)
					{
						Assert.IsNotNull(device.Controls[i], fixtures[f].Name + ": data index " + i + " has no control.");
						Assert.AreEqual(i, device.Controls[i].DataIndex, fixtures[f].Name + ": control out of place.");
					}
				}
			}
			using (var pad = Device(RawInputFixtures.RumblePad2))
			{
				Assert.AreEqual(8, pad.InputReportLength, "RumblePad 2 input report length.");
				Assert.AreEqual(12, pad.Controls.Count(x => x.IsButton && x.UsagePage == ButtonPage), "RumblePad 2 buttons.");
				var second = pad.Controls.Single(x => x.ReportId == 2);
				Assert.AreEqual(0xFF00, second.UsagePage, "The RumblePad 2 sends its report 2 for a vendor-defined value.");
				Assert.AreEqual(7, second.ReportCount, "Report 2 carries a usage value array of seven bytes.");
			}
			using (var wheel = Device(RawInputFixtures.G27))
			{
				var wheelAxis = Control(wheel, GenericDesktopPage, 0x30);
				Assert.AreEqual(14, wheelAxis.BitSize, "The G27 wheel is a 14-bit value.");
				Assert.AreEqual(16383, wheelAxis.LogicalMax, "The G27 wheel's range.");
				Assert.AreEqual(23, wheel.Controls.Count(x => x.IsButton && x.UsagePage == ButtonPage), "G27 buttons: a range of 22 and one more.");
				Assert.AreEqual(2, wheel.Controls.Count(x => x.UsagePage == 0xFF00), "G27 vendor-defined controls: a value array and a button.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The G27's twin puts the accelerator in Y, the combined pedals and clutch in the sliders and the brake in RotationZ, which its usages alone would not")]
		public void The_G27_takes_its_slots_from_its_twin()
		{
			using (var wheel = Device(RawInputFixtures.G27))
			{
				var twin = RawInputLayout.FromTwin(wheel.Controls, RawInputFixtures.G27.Objects);
				var standard = RawInputLayout.Standard(wheel.Controls);
				var wheelAxis = Control(wheel, GenericDesktopPage, 0x30).DataIndex;
				var combined = Control(wheel, GenericDesktopPage, 0x31).DataIndex;
				var accelerator = Control(wheel, GenericDesktopPage, 0x32).DataIndex;
				var brake = Control(wheel, GenericDesktopPage, 0x35).DataIndex;
				var clutch = Control(wheel, GenericDesktopPage, 0x36).DataIndex;
				var hat = Control(wheel, GenericDesktopPage, 0x39).DataIndex;
				AssertSlot(twin, wheelAxis, MapType.Axis, 0, "wheel");
				AssertSlot(twin, combined, MapType.Slider, 0, "combined pedals");
				AssertSlot(twin, accelerator, MapType.Axis, 1, "accelerator");
				AssertSlot(twin, brake, MapType.Axis, 5, "brake");
				AssertSlot(twin, clutch, MapType.Slider, 1, "clutch");
				AssertSlot(twin, hat, MapType.POV, 0, "hat switch");
				foreach (var c in wheel.Controls.Where(x => x.UsagePage == ButtonPage))
					AssertSlot(twin, c.DataIndex, MapType.Button, c.Usage - 1, "button " + c.Usage);
				foreach (var c in wheel.Controls.Where(x => x.UsagePage == 0xFF00))
					Assert.AreEqual(MapType.None, twin.Types[c.DataIndex], "A vendor-defined control has a slot.");
				// Placed by usage, three of the pedals would land elsewhere: why the twin is asked.
				AssertSlot(standard, combined, MapType.Axis, 1, "combined pedals, by usage");
				AssertSlot(standard, accelerator, MapType.Axis, 2, "accelerator, by usage");
				AssertSlot(standard, clutch, MapType.Slider, 0, "clutch, by usage");
				AssertSlot(standard, brake, MapType.Axis, 5, "brake, by usage");
				foreach (var c in wheel.Controls.Where(x => x.UsagePage == 0xFF00))
					Assert.AreEqual(MapType.None, standard.Types[c.DataIndex], "A vendor-defined control has a slot by usage.");
			}
		}

		static void AssertSlot(RawInputLayout layout, int dataIndex, MapType type, int index, string what)
		{
			Assert.AreEqual(type, layout.Types[dataIndex], what + ": kind of slot.");
			Assert.AreEqual(index, layout.Indexes[dataIndex], what + ": slot.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The Xbox One controller's 16-bit axes, declared 0 to -1, read 0 to 65535; its hat reads in 4500 steps and centred at rest; button N is Buttons[N - 1]")]
		public void The_Xbox_One_controller_reads_its_full_range()
		{
			var fixture = RawInputFixtures.XboxOne;
			using (var pad = Device(fixture))
			{
				var x = Control(pad, GenericDesktopPage, 0x30);
				Assert.AreEqual(16, x.BitSize, "The Xbox One controller's X is a 16-bit value.");
				Assert.AreEqual(0, x.LogicalMin, "X declares 0 as its minimum.");
				Assert.AreEqual(-1, x.LogicalMax, "X declares -1 as its maximum, meaning 65535.");
				var twin = RawInputLayout.FromTwin(pad.Controls, fixture.Objects);
				var standard = RawInputLayout.Standard(pad.Controls);
				CollectionAssert.AreEqual(standard.Types, twin.Types, "Its twin places its controls where their usages do.");
				CollectionAssert.AreEqual(standard.Indexes, twin.Indexes, "Its twin places its controls where their usages do.");
				foreach (var c in pad.Controls.Where(b => b.IsButton))
					AssertSlot(twin, c.DataIndex, MapType.Button, c.Usage - 1, "button " + c.Usage);
				var reader = new RawInputReader(pad, twin);
				var states = fixture.Samples.Select(s => Read(reader, s)).ToArray();
				for (var i = 0; i < states.Length; i++)
					AssertState(fixture, fixture.Samples[i], states[i]);
				// X, Y, RotationX and RotationY each reach both ends.
				foreach (var axis in new[] { 0, 1, 3, 4 })
				{
					Assert.AreEqual(0, states.Min(s => s.Axis[axis]), "Axis " + axis + " never reads 0.");
					Assert.AreEqual(65535, states.Max(s => s.Axis[axis]), "Axis " + axis + " never reads 65535.");
				}
				var povs = states.Select(s => s.Povs[0]).Distinct().OrderBy(p => p).ToArray();
				CollectionAssert.AreEqual(new[] { -1, 0, 4500, 9000, 13500, 18000, 22500, 27000, 31500 }, povs, "Hat readings.");
				Assert.AreEqual(-1, Read(reader, Sample(fixture, "at rest")).Povs[0], "The hat at rest, 0, reads centred.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A WM_INPUT carrying two reports is read as two, in order, so the second wins; a report of another ID leaves the buttons alone")]
		public void A_batched_input_is_read_report_by_report()
		{
			var fixture = RawInputFixtures.RumblePad2;
			var pressed = Sample(fixture, "Button 0 9:01 pressed");
			var moved = Sample(fixture, "X Axis 1:30 = 0");
			using (var pad = Device(fixture))
			{
				var reader = TwinReader(pad, fixture);
				var state = new SourceState();
				RawInputReader.Reset(state);
				Assert.AreEqual(2, reader.ReadRawInput(RawInput(pressed.Bytes(), moved.Bytes()), state), "Reports read.");
				AssertState(fixture, moved, state);
				Assert.AreEqual(2, reader.ReadRawInput(RawInput(moved.Bytes(), pressed.Bytes()), state), "Reports read.");
				AssertState(fixture, pressed, state);
				// Report 2 carries only a vendor-defined value: the buttons of report 1 stay as they were.
				var other = new byte[] { 2, 1, 2, 3, 4, 5, 6, 7 };
				Assert.IsTrue(reader.ReadReport(other, 0, other.Length, state), "The HID parser refused report 2.");
				AssertState(fixture, pressed, state);
				var shortReport = new byte[] { 1, 0x7F, 0x7F };
				Assert.IsFalse(reader.ReadReport(shortReport, 0, shortReport.Length, state), "A report shorter than the device's was read.");
				AssertState(fixture, pressed, state);
				Assert.AreEqual(0, reader.ReadRawInput(new byte[RawInputNative.RAWINPUTHEADER_Size + 8], state), "Input that is not from a HID device was read.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("engine"), TestCategory("performance")]
		[Description("Reading a report, alone or in a batch, makes nothing")]
		public void Reading_a_report_makes_nothing()
		{
			var fixture = RawInputFixtures.G27;
			var a = Sample(fixture, "Wheel axis 1:30 = 0").Bytes();
			var b = Sample(fixture, "Button 22 9:17 pressed").Bytes();
			var batch = RawInput(a, b);
			using (var wheel = Device(fixture))
			{
				var reader = TwinReader(wheel, fixture);
				var state = new SourceState();
				RawInputReader.Reset(state);
				var allocated = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < 1000; i++)
					{
						reader.ReadReport(a, 0, a.Length, state);
						reader.ReadReport(b, 0, b.Length, state);
						reader.ReadRawInput(batch, state);
					}
				});
				Assert.AreEqual(0L, allocated, "Bytes handed to the collector by 4000 reports.");
				Assert.IsTrue(state.Buttons[22], "The last report read was not the one with button 22 pressed.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each report that presses or lets go of a button counts one change, so the engine can tell a press that came and went between two of its reads")]
		public void Each_button_change_is_counted()
		{
			var fixture = RawInputFixtures.G27;
			var pressed = Sample(fixture, "Button 6 9:07 pressed").Bytes();
			var left = Sample(fixture, "Wheel axis 1:30 = 0").Bytes();
			using (var wheel = Device(fixture))
			{
				var reader = TwinReader(wheel, fixture);
				var state = new SourceState();
				RawInputReader.Reset(state);
				reader.ReadReport(pressed, 0, pressed.Length, state);
				reader.ReadReport(pressed, 0, pressed.Length, state);
				Assert.AreEqual(1, state.ButtonChanges[6], "A button held through two reports changed once.");
				// A press and release in one input, as Windows batches reports it has not delivered yet.
				Assert.AreEqual(3, reader.ReadRawInput(RawInput(left, pressed, left), state));
				Assert.IsFalse(state.Buttons[6], "The last report lets the button go.");
				Assert.AreEqual(4, state.ButtonChanges[6], "Changes inside one input were not each counted.");
				Assert.AreEqual(0, state.ButtonChanges.Where((x, i) => i != 6).Sum(), "A button that did not move was counted.");
				var copy = new SourceState();
				state.CopyTo(copy);
				CollectionAssert.AreEqual(state.ButtonChanges, copy.ButtonChanges, "A copy of a state loses its counts, which the engine reads them from.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each hat move is counted with where the hat was before, and each axis turning back with where it turned")]
		public void Each_hat_move_and_axis_turn_is_counted()
		{
			var fixture = RawInputFixtures.RumblePad2;
			var rest = Sample(fixture, "Y Axis 1:31 = 128").Bytes();
			var right = Sample(fixture, "Hat Switch 1:39 = 2").Bytes();
			var centre = Sample(fixture, "X Axis 1:30 = 128").Bytes();
			var left = Sample(fixture, "X Axis 1:30 = 0").Bytes();
			using (var pad = Device(fixture))
			{
				var reader = TwinReader(pad, fixture);
				var state = new SourceState();
				reader.Rest(state);
				Assert.AreEqual(3, reader.ReadRawInput(RawInput(rest, right, rest), state));
				Assert.AreEqual(-1, state.Povs[0], "The hat is back at the centre.");
				Assert.AreEqual(2, state.PovChanges[0], "The hat's move there and back is not two moves.");
				Assert.AreEqual(9000, state.PovsBefore[0], "Where the hat was before it came back is not kept.");
				var turns = state.AxisTurns[0];
				Assert.AreEqual(3, reader.ReadRawInput(RawInput(centre, left, centre), state));
				Assert.AreEqual(32767, state.Axis[0], "The stick is back at the centre.");
				Assert.AreEqual(turns + 1, state.AxisTurns[0], "The stick flicked left and back did not turn once.");
				Assert.AreEqual(0, state.AxisTurnValues[0], "The turn is not where the stick went.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device whose description names no control is read from its row: the G13's stick as X and Y, its keys as buttons")]
		public void An_undescribed_device_is_read_from_its_row()
		{
			var fixture = RawInputFixtures.G13;
			// Why the row exists: the G13 describes its report as seven vendor bytes and nothing else.
			using (var own = RawInputDevice.FromPreparsedData(fixture.PreparsedBytes()))
				Assert.IsTrue(own.Controls.All(x => x == null || x.UsagePage == 0xFF00), "The G13 names a control of its own now; read it by its description.");
			Assert.IsNotNull(fixture.Description, "The G13 has no row.");
			Assert.IsNull(RawInputDescription.Find(0x046D, 0xC21C, GenericDesktopPage),
				"A collection that describes itself, such as a joystick the maker's software adds, is read by its own description.");
			using (var g13 = fixture.Device(1, @"\\?\HID#VID_046D&PID_C21C#test", "G13", 0))
			{
				Assert.AreSame(fixture.Description, g13.Description);
				Assert.AreEqual(2 + 40, g13.Controls.Length, "The stick's two axes and the 40 key bits.");
				Assert.AreEqual(8, Control(g13, GenericDesktopPage, 0x30).BitOffset, "X is the byte after the report ID.");
				Assert.AreEqual(16, Control(g13, GenericDesktopPage, 0x31).BitOffset);
				Assert.AreEqual(24, Control(g13, ButtonPage, 1, true).BitOffset, "The keys start after the stick.");
				Assert.AreEqual(63, Control(g13, ButtonPage, 40, true).BitOffset);
				// No DirectInput twin: the usages place it, as they place any device without one.
				var layout = RawInputLayout.Standard(g13.Controls);
				CollectionAssert.AreEqual(new[] { "X Axis", "Y Axis" }, layout.GetDeviceObjects().Take(2).Select(x => x.Name).ToArray());
				Assert.AreEqual(40, layout.GetDeviceObjects().Count(x => x.Type == ObjectGuid.Button));
				var reader = new RawInputReader(g13, layout);
				foreach (var sample in fixture.Samples)
					AssertState(fixture, sample, Read(reader, sample));
				// Every report carries every key, so one let go reads as up, and both changes are counted.
				var state = Read(reader, Sample(fixture, "G1 pressed"));
				var rest = Sample(fixture, "at rest").Bytes();
				Assert.IsTrue(reader.ReadReport(rest, 0, rest.Length, state));
				AssertState(fixture, Sample(fixture, "at rest"), state);
				Assert.AreEqual(2, state.ButtonChanges[0], "A press and its release are not counted as two changes.");
				Assert.AreEqual(0, state.ButtonChanges[1], "A key that did not move was counted.");
				var other = new byte[] { 2, 0, 0, 0, 0, 0, 0, 0 };
				Assert.IsFalse(reader.ReadReport(other, 0, other.Length, state), "A report of another ID was read as the stick and keys.");
				AssertState(fixture, Sample(fixture, "at rest"), state);
				var pressed = Sample(fixture, "G1 pressed").Bytes();
				Assert.AreEqual(0L, Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < 1000; i++)
					{
						reader.ReadReport(pressed, 0, pressed.Length, state);
						reader.ReadReport(rest, 0, rest.Length, state);
					}
				}), "Bytes handed to the collector by 2000 reports of a described device.");
			}
		}

		#endregion

		#region Conversions

		static RawInputControl Value(int bitSize, int logicalMin, int logicalMax)
		{
			return new RawInputControl { BitSize = bitSize, LogicalMin = logicalMin, LogicalMax = logicalMax, ReportCount = 1 };
		}

		static void AssertAxis(RawInputControl c, uint raw, int expected)
		{
			Assert.AreEqual(expected, RawInputReader.ToAxis(c, raw), string.Format("{0}-bit {1}..{2}, raw 0x{3:X}", c.BitSize, c.LogicalMin, c.LogicalMax, raw));
		}

		static void AssertPov(RawInputControl c, uint raw, int expected)
		{
			Assert.AreEqual(expected, RawInputReader.ToPov(c, raw), string.Format("hat {0}..{1}, raw {2}", c.LogicalMin, c.LogicalMax, raw));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Unsigned axes scale to 0..65535 as DirectInput scales them: centre 32767, the next step 32768, halves rounded up")]
		public void Unsigned_axes_scale_as_DirectInput_does()
		{
			// Values DirectInput read for the RumblePad 2's and G27's 8-bit axes.
			var u8 = Value(8, 0, 255);
			AssertAxis(u8, 0, 0);
			AssertAxis(u8, 64, 16384);
			AssertAxis(u8, 127, 32511);
			AssertAxis(u8, 128, 32767);
			AssertAxis(u8, 129, 32768);
			AssertAxis(u8, 156, 39790);
			// Halves that rounding to even would take down: DirectInput takes them up.
			AssertAxis(u8, 138, 35109);
			AssertAxis(u8, 174, 44471);
			AssertAxis(u8, 210, 53833);
			AssertAxis(u8, 246, 63195);
			AssertAxis(u8, 255, 65535);
			// Values DirectInput read for the G27's 14-bit wheel.
			var u14 = Value(14, 0, 16383);
			AssertAxis(u14, 0, 0);
			AssertAxis(u14, 8076, 32303);
			AssertAxis(u14, 8192, 32767);
			AssertAxis(u14, 8193, 32768);
			AssertAxis(u14, 8204, 32812);
			AssertAxis(u14, 16383, 65535);
			// A 16-bit axis declared 0 to -1 is 0 to 65535.
			var u16 = Value(16, 0, -1);
			AssertAxis(u16, 0, 0);
			AssertAxis(u16, 1, 1);
			AssertAxis(u16, 32768, 32767);
			AssertAxis(u16, 32769, 32768);
			AssertAxis(u16, 65535, 65535);
			// Bits above the value's size are not part of it.
			AssertAxis(u8, 0xFFFFFF80, 32767);
			// Outside the declared range reads as its ends.
			var narrow = Value(8, 10, 200);
			AssertAxis(narrow, 5, 0);
			AssertAxis(narrow, 10, 0);
			AssertAxis(narrow, 200, 65535);
			AssertAxis(narrow, 255, 65535);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Signed axes are sign-extended from their bit size, then scaled like unsigned ones")]
		public void Signed_axes_are_sign_extended()
		{
			var s16 = Value(16, -32768, 32767);
			AssertAxis(s16, 0x8000, 0);
			AssertAxis(s16, 0x8001, 1);
			AssertAxis(s16, 0xFFFF, 32766);
			AssertAxis(s16, 0, 32767);
			AssertAxis(s16, 1, 32768);
			AssertAxis(s16, 0x7FFF, 65535);
			// The same value already sign-extended to 32 bits.
			AssertAxis(s16, 0xFFFF8000, 0);
			AssertAxis(s16, 0xFFFFFFFF, 32766);
			var s8 = Value(8, -127, 127);
			AssertAxis(s8, 0x81, 0);
			AssertAxis(s8, 0xFF, 32509);
			AssertAxis(s8, 0, 32767);
			AssertAxis(s8, 1, 32768);
			AssertAxis(s8, 0x7F, 65535);
			// -128 is below the declared range.
			AssertAxis(s8, 0x80, 0);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A hat reads in equal steps from up, and centred when its value is outside its range, whichever end the device uses for centred")]
		public void Hats_read_in_steps_and_centre_outside_their_range()
		{
			// Logitech: 0 to 7, and 8 when centred.
			var logitech = Value(4, 0, 7);
			for (var v = 0u; v <= 7; v++)
				AssertPov(logitech, v, (int)v * 4500);
			AssertPov(logitech, 8, -1);
			AssertPov(logitech, 15, -1);
			// Xbox: 1 to 8, and 0 when centred.
			var xbox = Value(4, 1, 8);
			AssertPov(xbox, 0, -1);
			for (var v = 1u; v <= 8; v++)
				AssertPov(xbox, v, (int)(v - 1) * 4500);
			// Four directions.
			var four = Value(2, 0, 3);
			AssertPov(four, 1, 9000);
			AssertPov(four, 3, 27000);
		}

		#endregion

		#region Identity

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The instance GUID is the same for a path in either case and differs between paths; the product GUID is DirectInput's")]
		public void Identity_follows_the_interface_path_and_DirectInput()
		{
			const string path = @"\\?\HID#VID_046D&PID_C29B#b&3586936b&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
			Assert.AreEqual(RawInputDevice.GetInstanceGuid(path), RawInputDevice.GetInstanceGuid(path.ToLowerInvariant()), "The instance GUID depends on the path's case.");
			Assert.AreNotEqual(RawInputDevice.GetInstanceGuid(path), RawInputDevice.GetInstanceGuid(path.Replace("0000#", "0001#")), "Two paths share an instance GUID.");
			// What DirectInput reports for the G27 and the Xbox One controller.
			Assert.AreEqual(new Guid("c29b046d-0000-0000-0000-504944564944"), RawInputDevice.GetProductGuid(0x046D, 0xC29B));
			Assert.AreEqual(new Guid("02ff045e-0000-0000-0000-504944564944"), RawInputDevice.GetProductGuid(0x045E, 0x02FF));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Raw Input lists each attached HID game controller with the path and product GUID DirectInput gives it, and the same instance GUID every time")]
		public void Attached_controllers_match_their_DirectInput_twins()
		{
			var first = RawInputDevice.GetGameControllers();
			var second = RawInputDevice.GetGameControllers();
			try
			{
				if (first.Count == 0)
					Assert.Inconclusive("No HID game controller is attached.");
				var twins = new Dictionary<string, DeviceInstance>(StringComparer.OrdinalIgnoreCase);
				using (var manager = new DirectInput())
				{
					foreach (var instance in manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
					{
						// Opened only to read its path, never acquired.
						using (var joystick = new Joystick(manager, instance.InstanceGuid))
							twins[joystick.Properties.InterfacePath] = instance;
					}
				}
				foreach (var device in first)
				{
					Console.WriteLine("{0:X4}:{1:X4} usage {2:X}:{3:X2} {4} controls, \"{5}\" {6}", device.VendorId, device.ProductId,
						device.UsagePage, device.Usage, device.Controls.Length, device.ProductName, device.InterfacePath);
					var again = second.SingleOrDefault(x => x.InterfacePath == device.InterfacePath);
					Assert.IsNotNull(again, device.InterfacePath + " was not listed the second time.");
					Assert.AreEqual(device.InstanceGuid, again.InstanceGuid, device.InterfacePath + ": instance GUID changed between two listings.");
					Assert.IsTrue(device.Controls.Length > 0, device.InterfacePath + " has no controls.");
					DeviceInstance twin;
					var listed = twins.TryGetValue(device.InterfacePath, out twin);
					// A device read from its row is one DirectInput lists as no game controller, which is why it has a row.
					if (device.Description != null)
					{
						Assert.IsFalse(listed, twin?.ProductName + " is a game controller to DirectInput, so it can be read by its own description.");
						continue;
					}
					Assert.IsTrue(listed, device.InterfacePath + " has no DirectInput device with its path.");
					Assert.AreEqual(twin.ProductGuid, device.ProductGuid, twin.ProductName + ": product GUID.");
				}
				foreach (var path in twins.Keys.Where(x => x.StartsWith(@"\\?\hid#", StringComparison.OrdinalIgnoreCase)))
					Assert.IsTrue(first.Any(x => string.Equals(x.InterfacePath, path, StringComparison.OrdinalIgnoreCase)), twins[path].ProductName + " is not listed by Raw Input.");
			}
			finally
			{
				foreach (var device in first.Concat(second))
					device.Dispose();
			}
		}

		#endregion
	}
}
