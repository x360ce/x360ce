// @under-test: AutoHotkey/x360ce.ahk, Engine/Input/Script/ScriptChannel.cs
// @area: engine   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A real AutoHotkey v2 script, written with x360ce.ahk, answers the engine within a pass or two at a thousand passes
	/// a second, can press a button on another controller, and what it held is let go of when it is closed.
	/// </summary>
	/// <remarks>Runs through <see cref="AutoHotkeyRun"/>; inconclusive where AutoHotkey v2 is not installed.</remarks>
	[TestClass]
	public class AutoHotkeyScriptTest
	{
		const string Script =
			"dev := X360ce.c1.d1\r\n" +
			"dev.Hotkey(\"b1\", (*) => dev.b2 := 1)\r\n" +
			"dev.Hotkey(\"b1 up\", (*) => dev.b2 := 0)\r\n" +
			"dev.Hotkey(\"a1 > 80%\", (*) => dev.b3 := 1)\r\n" +
			"dev.Hotkey(\"a1 > 80% up\", (*) => dev.b3 := 0)\r\n" +
			"dev.Hotkey(\"b5\", (*) => X360ce.c2.d1.b3 := 1)\r\n" +
			"dev.Hotkey(\"b5 up\", (*) => X360ce.c2.d1.b3 := 0)\r\n";

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A real AutoHotkey script answers a button within a few passes, owns the button it names, and lets go when closed")]
		public void A_script_answers_within_a_few_passes_and_lets_go_when_closed()
		{
			using (var run = new AutoHotkeyRun(Script))
			{
				run.Start(() => run.SlotInt(run.Slot, ScriptChannel.SlotOwnButtons) == (1 | 1 << 4));
				// Press and let go of button 1, forty times each; count the passes until button 2 follows.
				var power = new MachinePower();
				var counts = new List<int>();
				for (var t = 0; t < 80; t++)
				{
					var press = t % 2 == 0;
					run.Device.Buttons[0] = press;
					var passes = 0;
					do
					{
						run.Pass();
						passes++;
						Assert.IsFalse(run.Mapped.Buttons[0], "Button 1, which the script owns, reached the controller.");
						Assert.IsTrue(passes < 1000, "The script did not answer within a second.");
					}
					while (run.Mapped.Buttons[1] != press);
					counts.Add(passes);
					run.Passes(25);
				}
				counts.Sort();
				var median = counts[counts.Count / 2];
				Console.WriteLine("Passes from a press of button 1 until button 2 followed, 80 changes: median " + median
					+ ", 90 % " + counts[counts.Count * 9 / 10] + ", most " + counts.Last() + ". " + power);
				Assert.IsTrue(median <= 3, "Half the answers took more than 3 passes: median " + median + ".");

				// The stick pushed past 80 % presses button 3, and back lets go of it.
				run.Device.Axis[0] = 65535;
				Assert.IsTrue(run.PassUntil(() => run.Mapped.Buttons[2], 1000), "The stick pushed right did not press button 3.");
				run.Device.Axis[0] = 32767;
				Assert.IsTrue(run.PassUntil(() => !run.Mapped.Buttons[2], 1000), "The stick back at the centre did not let go of button 3.");

				// A press on Controller 1 presses a button on Controller 2's device, and nothing on Controller 1's.
				run.Device.Buttons[4] = true;
				Assert.IsTrue(run.PassUntil(() => run.Mapped2.Buttons[2], 1000), "c1.b5 did not press c2.b3.");
				Assert.IsFalse(run.Mapped.Buttons[2] || run.Mapped.Buttons[4], "The press reached Controller 1's device.");
				run.Device.Buttons[4] = false;
				Assert.IsTrue(run.PassUntil(() => !run.Mapped2.Buttons[2], 1000), "Letting go of c1.b5 did not let go of c2.b3.");

				// Closed while it holds button 2: the controller gets button 1 back and loses button 2 within the lease.
				run.Device.Buttons[0] = true;
				Assert.IsTrue(run.PassUntil(() => run.Mapped.Buttons[1], 1000));
				run.Process.Kill();
				run.Process.WaitForExit();
				var watch = Stopwatch.StartNew();
				Assert.IsTrue(run.PassUntil(() => run.Mapped.Buttons[0] && !run.Mapped.Buttons[1], 2000),
					"The controller still read what the closed script held.");
				Console.WriteLine("Let go of " + watch.ElapsedMilliseconds + " ms after the script was closed.");
				Assert.IsFalse(run.Channel.Connected);
				Assert.IsTrue(watch.ElapsedMilliseconds < 100, "A closed script was let go of only when its heartbeat ran out, "
					+ watch.ElapsedMilliseconds + " ms after it closed.");
			}
		}
	}
}
