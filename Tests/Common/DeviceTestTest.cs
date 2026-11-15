// @under-test: App.v4/Common/DInput/DeviceTest.cs, Engine/Input/Processors/DirectInputChanges.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>What the engine records for the Test Device window, and what the window takes out of it.</summary>
	[TestClass]
	public class DeviceTestTest
	{
		static DeviceTest NewTest()
		{
			return new DeviceTest(new UserDevice { InstanceGuid = Guid.NewGuid(), IsOnline = true });
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Every read is counted, only a read that changed a value, a button or a hat is recorded, and what was recorded, its buttons too, reaches the steps and their statistics")]
		public void Records_the_reads_that_changed_and_counts_every_read()
		{
			var test = NewTest();
			var state = new SourceState();
			state.Axis[0] = 100;
			test.Record(state, 10);
			test.Record(state, 20);
			state.Axis[0] = 200;
			test.Record(state, 30);
			state.Buttons[3] = true;
			test.Record(state, 40);
			Assert.AreEqual(4L, test.Reads);
			var steps = new DeviceTestSteps(new DeviceTestLayout { Channels = new[] { 0 }, ButtonCount = 4 }, 1000000);
			var statistics = steps.Statistics;
			statistics.Start(0, 0);
			steps.Start(0);
			test.TakeInto(steps);
			StringAssert.StartsWith(steps.Hint, "Still moving", "The button pressed in the last read did not reach the steps.");
			Assert.AreEqual(100, statistics.Channels[0].Min.Value);
			Assert.AreEqual(200, statistics.Channels[0].Max.Value);
			Assert.AreEqual(2, statistics.Channels[0].Distinct, "The read that changed only a button gave the same value again.");
			var latest = new int[DeviceTest.Channels];
			Assert.IsTrue(test.Latest(latest));
			Assert.AreEqual(200, latest[0]);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Sticks turned with no button ever pressed go through the engine's ring and fill: every read that moves an axis alone is recorded, taken and measured, X and Y Rotation as a stick too")]
		public void Axes_alone_fill_the_sticks_through_the_ring()
		{
			var test = NewTest();
			var steps = new DeviceTestSteps(new DeviceTestLayout { Channels = new[] { 0, 1, 2, 3, 4 }, ButtonCount = 4 }, 1000);
			var state = new SourceState();
			for (var i = 0; i < SourceState.MaxAxis; i++)
				state.Axis[i] = 32768;
			long now = 0;
			// One read a millisecond, as the engine records them; the window takes them out every 100.
			Action<int, Action<int>> run = (milliseconds, move) =>
			{
				for (var t = 1; t <= milliseconds; t++)
				{
					now++;
					if (move != null)
						move(t);
					test.Record(state, now);
					if (now % 100 == 0)
					{
						test.TakeInto(steps);
						steps.SetReads(now, test.Reads);
						steps.Tick(now);
					}
				}
			};
			Func<int, int, Action<int>> circle = (x, y) => t =>
			{
				var radius = Math.Min(1, t / 300.0);
				state.Axis[x] = (int)Math.Round(32768 + radius * 32767 * Math.Cos(2 * Math.PI * t / 2000));
				state.Axis[y] = (int)Math.Round(32768 + radius * 32767 * Math.Sin(2 * Math.PI * t / 2000));
			};
			steps.Start(now);
			run(20100, null);
			Assert.AreEqual(DeviceTestStep.Sticks, steps.Step);
			run(4000, circle(0, 1));
			state.Axis[0] = state.Axis[1] = 32768;
			run(4000, circle(3, 4));
			state.Axis[3] = state.Axis[4] = 32768;
			run(4000, t => state.Axis[2] = (int)Math.Round(32768 + 32767 * Math.Sin(2 * Math.PI * t / 2000)));
			state.Axis[2] = 32768;
			run(200, null);
			Assert.IsTrue(state.Buttons.All(x => !x), "A button was pressed.");
			Assert.AreEqual(0L, test.Dropped);
			Assert.AreEqual(DeviceTestStep.Buttons, steps.Step, "Sticks turned with no button pressed did not fill: " + string.Join("; ", steps.Controls.Select(x => x.Name + " " + Math.Round(x.Progress * 100) + " %")));
			CollectionAssert.AreEqual(new[] { "Left stick (X Axis, Y Axis)", "Right stick (X Rotation, Y Rotation)", "Z Axis" }, steps.Controls.Select(x => x.Name).ToArray());
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A read that finds the ring full is dropped and counted, never waited for")]
		public void A_full_ring_drops_and_counts()
		{
			var test = NewTest();
			var state = new SourceState();
			for (var i = 0; i < DeviceTest.Capacity + 5; i++)
			{
				state.Axis[0] = i + 1;
				test.Record(state, i);
			}
			Assert.AreEqual(5L, test.Dropped);
			test.Skip();
			state.Axis[0] = -1;
			test.Record(state, 0);
			Assert.AreEqual(5L, test.Dropped, "Room taken out was not given back.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The change buffer's entries keep their time and sequence number, reach the statistics as the axis or slider they changed, and give values per report from the sequence numbers")]
		public void Change_buffer_entries_carry_channel_time_and_sequence()
		{
			const int size = 24;
			var data = new byte[3 * size];
			// X and Y in report 7, a button in report 8.
			int[][] entries = { new[] { 0, 10, 1000, 7 }, new[] { 4, 20, 1000, 7 }, new[] { 48, 0x80, 1001, 8 } };
			for (var i = 0; i < entries.Length; i++)
				for (var f = 0; f < 4; f++)
					BitConverter.GetBytes(entries[i][f]).CopyTo(data, i * size + f * 4);
			var changes = new DirectInputChanges();
			changes.Load(data, 3, size);
			Assert.AreEqual(3, changes.EntryCount);
			CollectionAssert.AreEqual(new[] { 7, 7, 8 }, new[] { changes.EntrySequences[0], changes.EntrySequences[1], changes.EntrySequences[2] });
			CollectionAssert.AreEqual(new[] { 1000, 1000, 1001 }, new[] { changes.EntryTimes[0], changes.EntryTimes[1], changes.EntryTimes[2] });
			changes.Clear();
			Assert.AreEqual(0, changes.EntryCount, "Cleared, the entries of the last read stay counted.");
			changes.Load(data, 3, size);
			var test = NewTest();
			test.Record(changes, 5);
			Assert.IsTrue(test.HasChangeBuffer);
			var steps = new DeviceTestSteps(new DeviceTestLayout(), 1000000);
			var statistics = steps.Statistics;
			statistics.Start(0, 0);
			statistics.StartMoving(1, 0);
			test.TakeInto(steps);
			statistics.Stop(10, 0);
			var summary = statistics.Summary(10);
			Assert.IsTrue(summary.FromSequence);
			Assert.AreEqual(1.5, summary.ValuesPerReport.Value, 0.0001, "Three entries in two reports.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A change buffer entry's offset in DirectInput's joystick state names X to Z Rotation and the two sliders; buttons and hats name none")]
		public void Entry_offsets_name_axes_and_sliders()
		{
			Assert.AreEqual(0, DeviceTest.Channel(0));
			Assert.AreEqual(1, DeviceTest.Channel(4));
			Assert.AreEqual(5, DeviceTest.Channel(20));
			Assert.AreEqual(SourceState.MaxAxis, DeviceTest.Channel(24));
			Assert.AreEqual(SourceState.MaxAxis + 1, DeviceTest.Channel(28));
			Assert.AreEqual(-1, DeviceTest.Channel(32), "A hat.");
			Assert.AreEqual(-1, DeviceTest.Channel(48), "A button.");
		}
	}
}
