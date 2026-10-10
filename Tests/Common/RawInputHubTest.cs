// @under-test: Engine/Input/Processors/RawInputHub.cs, Engine/Input/Devices/RawInputDevice.cs, Engine/Input/Native/RawInputNative.cs, Engine/Input/States/SourceState.cs, Engine/Input/Layouts/RawInputLayout.cs, Engine/Input/Processors/RawInputHubDevice.cs, Engine/Input/Processors/TripleBuffer.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The Raw Input hub receives every game controller's reports on a thread of its own, and the engine copies each
	/// one's newest state without a lock, a wait or an allocation.
	/// </summary>
	/// <remarks>
	/// The triple buffer and the per-input path run on recorded reports (see <see cref="RawInputFixtures"/>) and need
	/// no hardware. The attached-controller test registers for Raw Input, which delivers reports without opening,
	/// acquiring or writing to a device, and unregisters at the end.
	/// </remarks>
	[TestClass]
	public class RawInputHubTest
	{
		/// <summary>The state a device reads before its first report, by <paramref name="layout"/>: what DirectInput reads then.</summary>
		static SourceState AtRest(RawInputDevice device, RawInputLayout layout)
		{
			var state = new SourceState();
			new RawInputReader(device, layout).Rest(state);
			return state;
		}

		static void AssertSame(SourceState expected, SourceState actual, string what)
		{
			CollectionAssert.AreEqual(expected.Axis, actual.Axis, what + ": axes differ.");
			CollectionAssert.AreEqual(expected.Sliders, actual.Sliders, what + ": sliders differ.");
			CollectionAssert.AreEqual(expected.Povs, actual.Povs, what + ": hat switches differ.");
			CollectionAssert.AreEqual(expected.Buttons, actual.Buttons, what + ": buttons differ.");
		}

		#region Triple buffer

		/// <summary>Writes <paramref name="n"/> into every value of <paramref name="state"/>, so a state that is not whole holds two numbers.</summary>
		static void Fill(SourceState state, int n)
		{
			for (var i = 0; i < state.Axis.Length; i++)
				state.Axis[i] = n;
			for (var i = 0; i < state.Sliders.Length; i++)
				state.Sliders[i] = n;
			for (var i = 0; i < state.Povs.Length; i++)
				state.Povs[i] = n;
			for (var i = 0; i < state.Buttons.Length; i++)
				state.Buttons[i] = ((n >> (i & 7)) & 1) != 0;
		}

		/// <summary>The number <see cref="Fill"/> wrote into every value of <paramref name="state"/>, or -1 when its values belong to different numbers.</summary>
		static int Whole(SourceState state)
		{
			var n = state.Axis[0];
			for (var i = 0; i < state.Axis.Length; i++)
				if (state.Axis[i] != n)
					return -1;
			for (var i = 0; i < state.Sliders.Length; i++)
				if (state.Sliders[i] != n)
					return -1;
			for (var i = 0; i < state.Povs.Length; i++)
				if (state.Povs[i] != n)
					return -1;
			for (var i = 0; i < state.Buttons.Length; i++)
				if (state.Buttons[i] != (((n >> (i & 7)) & 1) != 0))
					return -1;
			return n;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical"), TestCategory("engine")]
		[Description("States written on one thread and read on another are always whole and never older than the one read before; the reader ends on the newest, and neither side makes anything")]
		public void The_triple_buffer_hands_over_whole_states_newest_first()
		{
			var buffer = new TripleBuffer<SourceState>(new SourceState(), new SourceState(), new SourceState());
			var stop = 0;
			var written = 0;
			var writer = new Thread(() =>
			{
				var n = 0;
				while (Volatile.Read(ref stop) == 0)
				{
					n++;
					Fill(buffer.Back, n);
					buffer.Publish();
					Volatile.Write(ref written, n);
				}
			}) { IsBackground = true, Name = "Triple buffer writer" };
			var torn = 0;
			var older = 0;
			var taken = 0;
			var last = 0;
			long allocated;
			writer.Start();
			try
			{
				// The writer runs through every window, so what it makes is counted too.
				allocated = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < 200000; i++)
					{
						if (buffer.Take())
							taken++;
						var n = Whole(buffer.Front);
						if (n < 0)
							torn++;
						else if (n < last)
							older++;
						else
							last = n;
					}
				});
			}
			finally
			{
				Volatile.Write(ref stop, 1);
				writer.Join();
			}
			Console.WriteLine("Writer published {0} states; the reader took {1}.", written, taken);
			Assert.AreEqual(0, torn, "States read while the writer was filling them.");
			Assert.AreEqual(0, older, "States older than one read before them.");
			Assert.IsTrue(taken > 0, "The reader never took a state.");
			buffer.Take();
			Assert.AreEqual(written, Whole(buffer.Front), "The reader does not end on the newest state.");
			Assert.IsFalse(buffer.Take(), "A state was taken twice.");
			Assert.AreEqual(0L, allocated, "Bytes handed to the collector while one thread wrote and another read.");
		}

		#endregion

		#region Start and stop

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Stop before Start, Start twice, Stop twice and Start after Stop all work, and after Stop the window and the thread are gone")]
		public void Start_and_stop_can_be_repeated()
		{
			var hub = new RawInputHub();
			try
			{
				hub.Stop();
				Assert.IsFalse(hub.IsRunning, "Running after Stop without Start.");
				for (var round = 1; round <= 2; round++)
				{
					var where = "Round " + round;
					hub.Start();
					var window = hub.WindowHandle;
					Assert.IsTrue(hub.IsRunning, where + ": not running after Start.");
					Assert.AreNotEqual(IntPtr.Zero, window, where + ": no window after Start.");
					var processId = 0;
					var threadId = NativeMethods.GetWindowThreadProcessId(window, ref processId);
					Assert.AreEqual(Process.GetCurrentProcess().Id, processId, where + ": the window is another process's.");
					hub.Start();
					Assert.AreEqual(window, hub.WindowHandle, where + ": a second Start made another window.");
					hub.Stop();
					hub.Stop();
					Assert.IsFalse(hub.IsRunning, where + ": running after Stop.");
					Assert.AreEqual(IntPtr.Zero, hub.WindowHandle, where + ": a window handle after Stop.");
					var unused = 0;
					Assert.AreEqual(0, NativeMethods.GetWindowThreadProcessId(window, ref unused), where + ": the window outlived Stop.");
					using (var process = Process.GetCurrentProcess())
					{
						var alive = process.Threads.Cast<ProcessThread>().Any(x => x.Id == threadId && x.ThreadState != System.Diagnostics.ThreadState.Terminated);
						Assert.IsFalse(alive, where + ": the hub thread outlived Stop.");
					}
					Assert.IsNull(hub.Error, where + ": " + hub.Error);
				}
			}
			finally
			{
				hub.Stop();
			}
		}

		#endregion

		#region Attached controllers

		/// <summary>How long, in milliseconds, the attached-controller test waits for a report from a controller nobody moves.</summary>
		const int ReportWait = 10000;

		[TestMethod, TestCategory("devices")]
		[Description("Started, the hub reads every HID game controller Raw Input lists, those attached before it started included, and a controller that reports has a growing count and a state the engine copies")]
		public void The_hub_reads_the_attached_controllers()
		{
			var attached = RawInputDevice.GetGameControllers();
			var hub = new RawInputHub();
			var changes = 0;
			hub.DevicesChanged += (s, e) => Interlocked.Increment(ref changes);
			try
			{
				if (attached.Count == 0)
					Assert.Inconclusive("No HID game controller is attached.");
				var expected = attached.Select(x => x.InstanceGuid).ToArray();
				var clock = Stopwatch.StartNew();
				hub.Start();
				// The hub lists nothing itself: a device it knows arrived as a message.
				while (clock.ElapsedMilliseconds < 3000 && !expected.All(hub.Devices.ContainsKey))
					Thread.Sleep(10);
				Console.WriteLine("Arrivals after {0} ms: {1} of {2}, {3} change events.", clock.ElapsedMilliseconds, expected.Count(hub.Devices.ContainsKey), expected.Length, Volatile.Read(ref changes));
				foreach (var device in attached)
					Assert.IsTrue(hub.Devices.ContainsKey(device.InstanceGuid), "\"" + device.ProductName + "\" " + device.InterfacePath + " did not arrive within 3 s.");
				Assert.IsTrue(Volatile.Read(ref changes) >= attached.Count, "Fewer change events than arrivals.");
				// Most controllers report only when moved. A G27 at rest reports when its sensors' noise changes a value,
				// from none to several times a second, so the wait ends at the first report that arrives.
				var known = hub.Devices.Values.ToArray();
				var first = known.Select(x => x.ReportCount).ToArray();
				clock.Restart();
				while (clock.ElapsedMilliseconds < ReportWait && !known.Where((x, i) => x.ReportCount > first[i]).Any())
					Thread.Sleep(10);
				Thread.Sleep(200);
				var state = new SourceState();
				var grown = 0;
				for (var i = 0; i < known.Length; i++)
				{
					var d = known[i];
					var rest = AtRest(d.Device, d.Layout);
					Console.WriteLine("{0:X4}:{1:X4} \"{2}\": {3} reports, then {4}.", d.Device.VendorId, d.Device.ProductId, d.Device.ProductName, first[i], d.ReportCount);
					Assert.IsTrue(hub.TryCopyState(d.Device.InstanceGuid, state, out _), d.Device.ProductName + ": no state to copy.");
					if (d.ReportCount <= first[i])
						continue;
					grown++;
					Assert.IsTrue(d.LastReportTimestamp > 0, d.Device.ProductName + ": reports without a time.");
					var moved = !state.Axis.SequenceEqual(rest.Axis) || !state.Sliders.SequenceEqual(rest.Sliders) ||
						!state.Povs.SequenceEqual(rest.Povs) || !state.Buttons.SequenceEqual(rest.Buttons);
					Assert.IsTrue(moved, d.Device.ProductName + ": reports arrived, but the state the engine copies is still at rest.");
				}
				Assert.IsFalse(hub.TryCopyState(Guid.NewGuid(), state, out _), "A state for a device the hub does not read.");
				Assert.IsNull(hub.Error, "Hub error: " + hub.Error);
				if (grown == 0)
					Assert.Inconclusive("No controller reported within " + ReportWait / 1000 + " s; these report only when moved.");
			}
			finally
			{
				hub.Stop();
				foreach (var device in attached)
					device.Dispose();
			}
			Assert.AreEqual(0, hub.Devices.Count, "Devices left after Stop.");
		}

		#endregion

		#region Input

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A recorded controller's RAWINPUT reaches the state the engine copies; a layout set later starts the state at rest and then places the controls as asked; input from an unknown device is ignored")]
		public void Input_reaches_the_state_the_engine_copies()
		{
			var fixture = RawInputFixtures.G27;
			var sample = fixture.Samples.Single(x => x.Shows == "Wheel axis 1:30 = 0");
			var input = RawInputTest.RawInput(sample.Bytes());
			var hub = new RawInputHub();
			RawInputDevice device;
			try
			{
				// No hub thread runs, so the test stands in for it. A recorded device has no handle, so its input's hDevice is 0.
				device = RawInputDevice.FromPreparsedData(fixture.PreparsedBytes());
				Assert.IsNotNull(device, "The HID parser refused the recorded preparsed data.");
				var entry = hub.Add(device);
				var guid = device.InstanceGuid;
				Assert.AreSame(entry, hub.Devices[guid], "The device is not listed by its instance GUID.");
				var state = new SourceState();
				bool fresh;
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh), "No state before the first report.");
				Assert.IsFalse(fresh, "A state before the first report is new.");
				var usages = RawInputLayout.Standard(device.Controls);
				AssertSame(AtRest(device, usages), state, "Before the first report");
				// A pad reports only when moved, so before then it reads as DirectInput reads it: the axes it has at
				// the centre, not 0, which would hold a stick fully left and up. The G27 has no Rx axis.
				Assert.AreEqual(32767, state.Axis[0], "The wheel axis before the first report.");
				Assert.AreEqual(0, state.Axis[3], "An axis the device does not have before the first report.");
				Assert.AreEqual(-1, state.Povs[0], "The hat switch before the first report.");
				// Until a layout is set, the controls go where their usages put them.
				var standard = AtRest(device, usages);
				new RawInputReader(device, usages).ReadRawInput(input, standard);
				Assert.AreEqual(1, hub.ReadInput(input), "Reports read.");
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh));
				Assert.IsTrue(fresh, "The state a report left is not new.");
				AssertSame(standard, state, "Standard layout");
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh));
				Assert.IsFalse(fresh, "The same state copied twice is new the second time.");
				Assert.AreEqual(1L, entry.ReportCount, "Reports counted.");
				Assert.IsTrue(entry.LastReportTimestamp > 0, "The report has no time.");
				// The twin's layout, as the hub thread takes it on its message: at rest first, then DirectInput's slots.
				var twinLayout = RawInputLayout.FromTwin(device.Controls, fixture.Objects);
				Assert.IsTrue(hub.SetLayout(guid, twinLayout), "The layout was refused.");
				Assert.IsTrue(entry.ApplyLayout(), "The layout was not pending.");
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh));
				Assert.IsTrue(fresh, "The state a new layout started at rest is not new.");
				AssertSame(AtRest(device, twinLayout), state, "After the layout changed");
				hub.ReadInput(input);
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh));
				RawInputTest.AssertState(fixture, sample, state);
				// The same slots again change nothing: the state goes on as it is.
				Assert.IsTrue(hub.SetLayout(guid, RawInputLayout.FromTwin(device.Controls, fixture.Objects)));
				Assert.IsFalse(entry.ApplyLayout(), "A layout with the same slots was taken as another.");
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh));
				Assert.IsFalse(fresh, "A layout with the same slots started the state again.");
				RawInputTest.AssertState(fixture, sample, state);
				// A layout whose message has not been handled is taken before the next report.
				hub.SetLayout(guid, RawInputLayout.Standard(device.Controls));
				hub.ReadInput(input);
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh));
				AssertSame(standard, state, "Standard layout again");
				Assert.IsFalse(entry.ApplyLayout(), "The layout was taken twice.");
				// Input from a handle the hub does not read changes nothing.
				var stranger = (byte[])input.Clone();
				stranger[8] = 1;
				Assert.AreEqual(0, hub.ReadInput(stranger), "Input from an unknown device was read.");
				Assert.AreEqual(3L, entry.ReportCount, "Reports counted.");
				Assert.IsTrue(hub.TryCopyState(guid, state, out fresh));
				Assert.IsFalse(fresh, "Input from an unknown device made a new state.");
				AssertSame(standard, state, "After unknown input");
				Assert.IsFalse(hub.SetLayout(Guid.NewGuid(), RawInputLayout.Standard(device.Controls)), "A layout for a device the hub does not read.");
			}
			finally
			{
				hub.Stop();
			}
			Assert.AreEqual(0, hub.Devices.Count, "Devices left after Stop.");
			Assert.AreEqual(IntPtr.Zero, device.PreparsedData, "Stop did not dispose the device.");
		}

		const string WheelPath = @"\\?\HID#VID_046D&PID_C29B#7&2B3C4D5E&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
		const string PadPath = @"\\?\HID#VID_046D&PID_C219#7&1A2B3C4D&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A device that arrives again, after a reconnect or a stop of the hub, reads by the last layout it was given from its first report on; a device with other controls under the same GUID, or one never given a layout, reads by its usages")]
		public void A_device_that_arrives_again_reads_by_its_last_layout()
		{
			var fixture = RawInputFixtures.G27;
			var sample = fixture.Samples.Single(x => x.Shows == "Wheel axis 1:30 = 0");
			var input = RawInputTest.RawInput(sample.Bytes());
			var hub = new RawInputHub();
			try
			{
				// No hub thread runs, so the test stands in for it.
				var wheel = fixture.Device(0, WheelPath, null, 0x04);
				var guid = wheel.InstanceGuid;
				var usages = RawInputLayout.Standard(wheel.Controls);
				Assert.IsTrue(usages.SameSlots(hub.Add(wheel).Layout), "A device never given a layout does not read by its usages.");
				var twin = RawInputLayout.FromTwin(wheel.Controls, fixture.Objects);
				Assert.IsFalse(twin.SameSlots(usages), "The twin's slots are the usages', so the test would show nothing.");
				Assert.IsTrue(hub.SetLayout(guid, twin));
				var state = new SourceState();
				// Unplugged before the hub thread took the layout, and plugged in again: the twin's slots from the first report.
				Assert.IsTrue(hub.Remove(wheel.Handle));
				var back = hub.Add(fixture.Device(0, WheelPath, null, 0x04));
				Assert.AreSame(twin, back.Layout, "A device that came back does not read by its last layout.");
				Assert.IsTrue(hub.TryCopyState(guid, state, out _));
				AssertSame(AtRest(back.Device, twin), state, "Before the first report");
				Assert.AreEqual(1, hub.ReadInput(input), "Reports read.");
				Assert.IsTrue(hub.TryCopyState(guid, state, out _));
				RawInputTest.AssertState(fixture, sample, state);
				// The hub stopped and started, as it was on every change of Windows settings: its devices go, their layouts stay.
				hub.Stop();
				Assert.AreEqual(0, hub.Devices.Count, "Devices left after Stop.");
				var again = hub.Add(fixture.Device(0, WheelPath, null, 0x04));
				Assert.AreSame(twin, again.Layout, "A device read again after a stop of the hub does not read by its last layout.");
				hub.ReadInput(input);
				Assert.IsTrue(hub.TryCopyState(guid, state, out _));
				RawInputTest.AssertState(fixture, sample, state);
				// Other controls under the same path, as after a firmware or mode change: the old layout does not fit them.
				var pad = RawInputFixtures.RumblePad2.Device(0, WheelPath, null, 0x05);
				Assert.IsFalse(twin.Fits(pad.Controls), "The wheel's layout fits the pad's controls.");
				Assert.IsTrue(RawInputLayout.Standard(pad.Controls).SameSlots(hub.Add(pad).Layout), "A device with other controls took a layout made for others.");
				// Another device, never given a layout.
				hub.Remove(pad.Handle);
				var other = RawInputFixtures.RumblePad2.Device(1, PadPath, null, 0x05);
				Assert.IsTrue(RawInputLayout.Standard(other.Controls).SameSlots(hub.Add(other).Layout), "A device never given a layout took another's.");
			}
			finally
			{
				hub.Stop();
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("engine"), TestCategory("performance")]
		[Description("Reading a RAWINPUT on the hub's side and copying the state on the engine's makes nothing")]
		public void Reading_input_and_copying_the_state_makes_nothing()
		{
			var fixture = RawInputFixtures.G27;
			var a = RawInputTest.RawInput(fixture.Samples.Single(x => x.Shows == "Wheel axis 1:30 = 0").Bytes());
			var b = RawInputTest.RawInput(fixture.Samples.Single(x => x.Shows == "Button 22 9:17 pressed").Bytes());
			var stranger = (byte[])a.Clone();
			stranger[8] = 1;
			var hub = new RawInputHub();
			try
			{
				var device = RawInputDevice.FromPreparsedData(fixture.PreparsedBytes());
				Assert.IsNotNull(device, "The HID parser refused the recorded preparsed data.");
				var guid = device.InstanceGuid;
				var entry = hub.Add(device);
				hub.SetLayout(guid, RawInputLayout.FromTwin(device.Controls, fixture.Objects));
				entry.ApplyLayout();
				var state = new SourceState();
				var allocated = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < 1000; i++)
					{
						hub.ReadInput(a);
						hub.TryCopyState(guid, state, out _);
						hub.ReadInput(b);
						hub.ReadInput(stranger);
						hub.TryCopyState(guid, state, out _);
					}
				});
				Assert.AreEqual(0L, allocated, "Bytes handed to the collector by 3000 inputs and 2000 copies.");
				Assert.IsTrue(state.Buttons[22], "The last state copied is not the one with button 22 pressed.");
				Assert.AreEqual(10000L, entry.ReportCount, "Reports counted.");
			}
			finally
			{
				hub.Stop();
			}
		}

		#endregion
	}
}
