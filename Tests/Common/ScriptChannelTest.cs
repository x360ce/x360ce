// @under-test: Engine/Input/Script/ScriptChannel.cs, AutoHotkey/x360ce.ahk
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// An AutoHotkey script changes what a device's controller reads through shared memory, and the engine never waits
	/// for it: what the script sent is applied on the next pass, the delays it asks for play on the engine's clock, one
	/// control changes at most once a pass, and a script that stops beating loses everything it held.
	/// </summary>
	/// <remarks>The script's side is written here the way x360ce.ahk writes it, through the same offsets.</remarks>
	[TestClass]
	public class ScriptChannelTest
	{
		/// <summary>The script's side of the shared memory.</summary>
		sealed class FakeScript : IDisposable
		{
			readonly MemoryMappedFile _map;
			readonly MemoryMappedViewAccessor _view;
			int _beat;

			public FakeScript(string name)
			{
				_map = MemoryMappedFile.OpenExisting(name);
				_view = _map.CreateViewAccessor(0, ScriptChannel.Size);
				_view.Write(ScriptChannel.OffScriptWindow, 1L);
			}

			public void Beat()
			{
				_view.Write(ScriptChannel.OffHeartbeat, ++_beat);
			}

			public int Read(int offset) { return _view.ReadInt32(offset); }

			public int ReadSlot(int slot, int offset) { return Read(ScriptChannel.OffSlots + slot * ScriptChannel.SlotSize + offset); }

			public void SetBit(int slot, int offset, int bit)
			{
				var at = ScriptChannel.OffSlots + slot * ScriptChannel.SlotSize + offset + bit / 32 * 4;
				_view.Write(at, _view.ReadInt32(at) | 1 << (bit & 31));
			}

			public void Send(int slot, int kind, int index, int value, int delay = 0, bool fromNow = true)
			{
				var written = Read(ScriptChannel.OffCommandsWritten);
				var at = ScriptChannel.OffCommands + (written & (ScriptChannel.RingLength - 1)) * ScriptChannel.CommandSize;
				_view.Write(at, slot << 24 | kind << 16 | index);
				_view.Write(at + 4, value);
				_view.Write(at + 8, delay);
				_view.Write(at + 12, fromNow ? ScriptChannel.FlagFromNow : 0);
				_view.Write(ScriptChannel.OffCommandsWritten, written + 1);
			}

			/// <summary>The events not read yet, as code and value pairs, marked read.</summary>
			public List<int[]> TakeEvents()
			{
				var events = new List<int[]>();
				var read = Read(ScriptChannel.OffEventsRead);
				var written = Read(ScriptChannel.OffEventsWritten);
				for (; read != written; read++)
				{
					var at = ScriptChannel.OffEvents + (read & (ScriptChannel.RingLength - 1)) * ScriptChannel.EventSize;
					events.Add(new[] { Read(at), Read(at + 4) });
				}
				_view.Write(ScriptChannel.OffEventsRead, read);
				return events;
			}

			/// <summary>What the script does when it starts reading: says the message arrived.</summary>
			public void ClearPending()
			{
				_view.Write(ScriptChannel.OffPending, 0);
			}

			/// <summary>Marks every event read without making anything.</summary>
			public void SkipEvents()
			{
				_view.Write(ScriptChannel.OffEventsRead, Read(ScriptChannel.OffEventsWritten));
			}

			public void Dispose()
			{
				_view.Dispose();
				_map.Dispose();
			}
		}

		static readonly long Start = Stopwatch.GetTimestamp();

		static long At(int ms) { return Start + ms * Stopwatch.Frequency / 1000; }

		int _nudges;

		/// <summary>A channel of its own, a script on it, and one device on Controller 1 in slot <paramref name="slot"/>.</summary>
		ScriptChannel Open(out FakeScript script, out int slot)
		{
			var name = @"Local\X360CE.Script.Test." + Guid.NewGuid().ToString("N");
			var channel = new ScriptChannel(name);
			channel.Nudge = w => _nudges++;
			script = new FakeScript(name);
			var slots = new int[1];
			channel.Route(new[] { new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Test pad" } }, new[] { 1 }, slots);
			slot = slots[0];
			return channel;
		}

		/// <summary>One engine pass at <paramref name="ms"/>: what the controller reads.</summary>
		static SourceState Pass(ScriptChannel channel, int slot, SourceState device, int ms)
		{
			channel.BeginPass(At(ms));
			var state = channel.Apply(slot, device) ?? device;
			channel.EndPass(At(ms));
			return state;
		}

		/// <summary>A connected script: the engine took its first beat on the pass at 0 ms.</summary>
		static void Connect(ScriptChannel channel, FakeScript script, int slot, SourceState device)
		{
			Pass(channel, slot, device, -1);
			script.Beat();
			Pass(channel, slot, device, 0);
			Assert.IsTrue(channel.Connected, "The script did not connect.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Nothing a script left in the shared memory counts until the script beats; then what it sends is applied")]
		public void Nothing_changes_until_a_script_beats()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				script.Send(slot, ScriptChannel.KindButton, 2, 1);
				Assert.IsFalse(Pass(channel, slot, device, 0).Buttons[2], "A press sent before the script beat was applied.");
				Assert.IsFalse(Pass(channel, slot, device, 10).Buttons[2], "A heartbeat left in the memory counted as a beat.");
				Assert.IsFalse(channel.Connected);
				script.Beat();
				Pass(channel, slot, device, 20);
				script.Send(slot, ScriptChannel.KindButton, 2, 1);
				Assert.IsTrue(Pass(channel, slot, device, 21).Buttons[2], "A press sent by a beating script was not applied.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A button the script owns reads only what the script sends; the others read the device plus the script's presses")]
		public void An_owned_button_reads_only_what_the_script_sends()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				Connect(channel, script, slot, device);
				script.SetBit(slot, ScriptChannel.SlotOwnButtons, 0);
				device.Buttons[0] = true;
				device.Buttons[1] = true;
				var state = Pass(channel, slot, device, 1);
				Assert.IsFalse(state.Buttons[0], "An owned button read the device.");
				Assert.IsTrue(state.Buttons[1], "A button nobody owns did not read the device.");
				script.Send(slot, ScriptChannel.KindButton, 0, 1);
				script.Send(slot, ScriptChannel.KindButton, 5, 1);
				state = Pass(channel, slot, device, 2);
				Assert.IsTrue(state.Buttons[0], "An owned button did not read the script's press.");
				Assert.IsTrue(state.Buttons[5], "The script's press on a button it does not own was not added.");
				Assert.IsTrue(state.Buttons[1]);
				Assert.IsTrue(device.Buttons[0] && !device.Buttons[5], "The device's own state was changed.");
				script.Send(slot, ScriptChannel.KindButton, 5, 0);
				device.Buttons[5] = true;
				Assert.IsTrue(Pass(channel, slot, device, 3).Buttons[5], "The script letting go hid the device's own press.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A press and a release sent together land on two passes, so a tap is never lost")]
		public void A_press_and_release_sent_together_land_on_two_passes()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				Connect(channel, script, slot, device);
				script.Send(slot, ScriptChannel.KindButton, 3, 1);
				script.Send(slot, ScriptChannel.KindButton, 3, 0);
				script.Send(slot, ScriptChannel.KindButton, 4, 1);
				var first = Pass(channel, slot, device, 1);
				Assert.IsTrue(first.Buttons[3], "The press was not shown.");
				Assert.IsTrue(first.Buttons[4], "A press of another button waited behind the first button's release.");
				Assert.IsFalse(Pass(channel, slot, device, 2).Buttons[3], "The release was not shown on the next pass.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Delays in a sequence play on the engine's clock, each after the command before it")]
		public void Delays_play_on_the_engine_clock()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				Connect(channel, script, slot, device);
				// b1 down, 40, b1 up, 40, b1 down: sent at once, as Send("b1 down, 40, b1 up, 40, b1 down") does.
				script.Send(slot, ScriptChannel.KindButton, 0, 1, 0, true);
				script.Send(slot, ScriptChannel.KindButton, 0, 0, 40, false);
				script.Send(slot, ScriptChannel.KindButton, 0, 1, 40, false);
				Assert.IsTrue(Pass(channel, slot, device, 100).Buttons[0]);
				Assert.IsTrue(Pass(channel, slot, device, 139).Buttons[0], "The release came before its 40 ms.");
				Assert.IsFalse(Pass(channel, slot, device, 140).Buttons[0], "The release did not come at 40 ms.");
				Assert.IsFalse(Pass(channel, slot, device, 179).Buttons[0]);
				Assert.IsTrue(Pass(channel, slot, device, 180).Buttons[0], "The second press did not come 40 ms after the release.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("An axis, a slider and a hat hold the script's value until it lets go, and then read the device again")]
		public void Axes_sliders_and_hats_are_held_until_released()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				device.Axis[1] = 32767;
				device.Sliders[0] = 100;
				device.Povs[0] = -1;
				Connect(channel, script, slot, device);
				script.Send(slot, ScriptChannel.KindAxis, 1, 65535);
				script.Send(slot, ScriptChannel.KindSlider, 0, 70000);
				script.Send(slot, ScriptChannel.KindPov, 0, 9000);
				var state = Pass(channel, slot, device, 1);
				Assert.AreEqual(65535, state.Axis[1]);
				Assert.AreEqual(65535, state.Sliders[0], "A value past the end was not kept to the range.");
				Assert.AreEqual(9000, state.Povs[0]);
				script.Send(slot, ScriptChannel.KindAxis, 1, ScriptChannel.ReleaseValue);
				script.Send(slot, ScriptChannel.KindSlider, 0, ScriptChannel.ReleaseValue);
				script.Send(slot, ScriptChannel.KindPov, 0, ScriptChannel.ReleaseValue);
				channel.BeginPass(At(2));
				Assert.IsNull(channel.Apply(slot, device), "A device the script holds nothing on was not given back to its own state.");
				channel.EndPass(At(2));
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A script that stops beating lets go of everything it held, and of every control it watched or owned")]
		public void A_script_that_stops_beating_lets_go_of_everything()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				Connect(channel, script, slot, device);
				script.SetBit(slot, ScriptChannel.SlotOwnButtons, 0);
				script.SetBit(slot, ScriptChannel.SlotWatchButtons, 0);
				script.Send(slot, ScriptChannel.KindButton, 7, 1);
				script.Send(slot, ScriptChannel.KindAxis, 0, 0);
				Assert.IsTrue(Pass(channel, slot, device, 1).Buttons[7]);
				Assert.IsTrue(Pass(channel, slot, device, ScriptChannel.DefaultLeaseMilliseconds - 1).Buttons[7], "The script was dropped before its lease ran out.");
				device.Buttons[0] = true;
				var state = Pass(channel, slot, device, ScriptChannel.DefaultLeaseMilliseconds);
				Assert.IsFalse(channel.Connected);
				Assert.IsFalse(state.Buttons[7], "A button the script pressed stayed pressed after it stopped.");
				Assert.IsTrue(state.Buttons[0], "A button the script owned still ignored the device.");
				Assert.AreEqual(0, script.Read(ScriptChannel.OffConnected));
				Assert.AreEqual(0, script.ReadSlot(slot, ScriptChannel.SlotOwnButtons), "What the script owned was kept for its return.");
				Assert.AreEqual(0, script.ReadSlot(slot, ScriptChannel.SlotWatchButtons), "What the script watched was kept for its return.");
				script.Beat();
				Pass(channel, slot, device, ScriptChannel.DefaultLeaseMilliseconds + 1);
				Assert.IsTrue(channel.Connected, "A script that beat again did not come back.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A watched change becomes an event and one message; more news waits for the script or for 20 ms")]
		public void Watched_changes_become_events_and_one_message()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				device.Povs[0] = -1;
				Connect(channel, script, slot, device);
				script.SetBit(slot, ScriptChannel.SlotWatchButtons, 33);
				script.SetBit(slot, ScriptChannel.SlotWatchPovs, 0);
				_nudges = 0;
				device.Buttons[33] = true;
				device.Buttons[34] = true;
				Pass(channel, slot, device, 1);
				Assert.AreEqual(1, _nudges, "The script was not told once.");
				device.Povs[0] = 27000;
				Pass(channel, slot, device, 2);
				Assert.AreEqual(1, _nudges, "The script was told again while it had not started reading.");
				var events = script.TakeEvents();
				Assert.AreEqual(2, events.Count, "A change nobody watched was listed, or a watched one was not.");
				Assert.AreEqual(slot << 24 | ScriptChannel.KindButton << 16 | 33, events[0][0]);
				Assert.AreEqual(1, events[0][1]);
				Assert.AreEqual(slot << 24 | ScriptChannel.KindPov << 16 | 0, events[1][0]);
				Assert.AreEqual(27000, events[1][1]);
				Assert.AreEqual(1 << 1 | 1 << 2, script.ReadSlot(slot, ScriptChannel.SlotButtons + 4), "The device's buttons were not shared.");
				device.Buttons[33] = false;
				Pass(channel, slot, device, 30);
				Assert.AreEqual(2, _nudges, "News 20 ms after a message the script never answered was not told again.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The heartbeat interval is written for the script, and a changed wait for a hung script is kept")]
		public void The_heartbeat_and_the_hang_wait_can_be_set()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				Assert.AreEqual(ScriptChannel.DefaultHeartbeatMilliseconds, script.Read(ScriptChannel.OffHeartbeatInterval),
					"The script was not told how often to beat.");
				channel.HeartbeatMilliseconds = 40;
				Assert.AreEqual(40, script.Read(ScriptChannel.OffHeartbeatInterval));
				channel.LeaseMilliseconds = 100;
				var device = new SourceState();
				Connect(channel, script, slot, device);
				Pass(channel, slot, device, 99);
				Assert.IsTrue(channel.Connected, "The script was dropped before the wait set.");
				Pass(channel, slot, device, 100);
				Assert.IsFalse(channel.Connected, "The script was kept past the wait set.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A move while a message is in flight is told once the script has started reading, not lost")]
		public void News_while_a_message_is_in_flight_is_told_later()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				device.Axis[0] = 32767;
				Connect(channel, script, slot, device);
				script.SetBit(slot, ScriptChannel.SlotWatchAxes, 0);
				_nudges = 0;
				Pass(channel, slot, device, 1);
				Assert.AreEqual(1, _nudges, "Watching an axis did not tell the script where it is.");
				// The script read where the axis was, and the engine's next read of the flag still sees the message in flight.
				device.Axis[0] = 65535;
				Pass(channel, slot, device, 2);
				Assert.AreEqual(1, _nudges, "The script was told again while a message was in flight.");
				script.ClearPending();
				Pass(channel, slot, device, 3);
				Assert.AreEqual(2, _nudges, "The move that met a message in flight was never told.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device keeps its slot while it stays routed; a slot freed by a device that left starts empty")]
		public void A_routed_device_keeps_its_slot()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var a = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "A" };
				var b = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "B" };
				var slots = new int[2];
				channel.Route(new[] { a, b }, new[] { 1, 2 }, slots);
				var slotA = slots[0];
				var slotB = slots[1];
				Assert.AreNotEqual(slotA, slotB);
				script.SetBit(slotB, ScriptChannel.SlotOwnButtons, 3);
				channel.Route(new[] { a }, new[] { 4 }, slots);
				Assert.AreEqual(slotA, slots[0], "A device still routed moved to another slot.");
				Assert.AreEqual(4, script.ReadSlot(slotA, ScriptChannel.SlotNumbers), "The device's numbers were not written again.");
				Assert.AreEqual(0, script.ReadSlot(slotB, ScriptChannel.SlotInUse), "The slot of a device that left is still in use.");
				Assert.AreEqual(0, script.ReadSlot(slotB, ScriptChannel.SlotOwnButtons), "The next device would inherit what the script owned.");
				Assert.AreEqual(-1, script.ReadSlot(slotB, ScriptChannel.SlotPovs), "A freed slot's hat reads as pushed up.");
				Assert.AreEqual(MapExpressionUnits.RawCentre, script.ReadSlot(slotB, ScriptChannel.SlotAxes), "A freed slot's axis reads as pushed to one end.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical"), TestCategory("performance")]
		[Description("A pass with a script connected, watching and holding controls, hands nothing to the collector")]
		public void A_pass_makes_nothing()
		{
			FakeScript script;
			int slot;
			using (var channel = Open(out script, out slot))
			using (script)
			{
				var device = new SourceState();
				Connect(channel, script, slot, device);
				script.SetBit(slot, ScriptChannel.SlotOwnButtons, 0);
				script.SetBit(slot, ScriptChannel.SlotWatchButtons, 1);
				script.SetBit(slot, ScriptChannel.SlotWatchAxes, 0);
				var ms = 1;
				var bytes = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < 1000; i++, ms++)
					{
						device.Buttons[1] = (i & 1) == 0;
						device.Axis[0] = (i * 977) & 0xFFFF;
						if ((i & 63) == 0)
						{
							script.Beat();
							script.Send(slot, ScriptChannel.KindButton, 0, i & 1);
							script.Send(slot, ScriptChannel.KindAxis, 1, i);
							script.SkipEvents();
						}
						channel.BeginPass(At(ms));
						channel.Apply(slot, device);
						channel.EndPass(At(ms));
					}
				});
				Assert.AreEqual(0, bytes, "A thousand passes handed the collector " + bytes + " bytes.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("x360ce.ahk uses the engine's layout: every value it names equals the engine's constant of the same name")]
		public void The_library_and_the_engine_share_one_layout()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "AutoHotkey", "x360ce.ahk");
			var text = File.ReadAllText(path).Replace("\r", "");
			// The layout is the block under its comment, up to the first empty line; the library's own state follows it.
			var start = text.IndexOf("; Layout shared with x360ce", StringComparison.Ordinal);
			Assert.IsTrue(start > 0, "The library's layout block was not found.");
			var end = text.IndexOf("\n\n", start, StringComparison.Ordinal);
			text = text.Substring(start, (end < 0 ? text.Length : end) - start);
			var values = Regex.Matches(text,@"^\tstatic (\w+) := (""[^""]*""|-?(?:0x[0-9A-Fa-f]+|\d+))\s*$", RegexOptions.Multiline)
				.Cast<Match>()
				.ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
			Assert.IsTrue(values.Count > 40, "The library's layout was not found.");
			var constants = typeof(ScriptChannel).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.IsLiteral)
				.ToDictionary(f => f.Name, f => f.GetValue(null));
			foreach (var pair in values)
			{
				Assert.IsTrue(constants.ContainsKey(pair.Key), "x360ce.ahk names " + pair.Key + ", which the engine does not have.");
				var engine = constants[pair.Key];
				var library = pair.Value;
				object parsed;
				if (library.StartsWith("\""))
					parsed = library.Trim('"');
				else if (library.StartsWith("0x"))
					parsed = long.Parse(library.Substring(2), NumberStyles.HexNumber);
				else
					parsed = long.Parse(library, CultureInfo.InvariantCulture);
				var expected = engine is string ? engine : (object)Convert.ToInt64(engine);
				Assert.AreEqual(expected, parsed, pair.Key + " differs between x360ce.ahk and the engine.");
			}
		}
	}
}
