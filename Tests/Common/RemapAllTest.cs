// @under-test: App.v4/Common/DInput/Recorder.cs, App.v4/Controls/PadControl.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Windows.Forms;
using x360ce.App;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>Remap All records one control after another, and moves on as soon as the controller is at rest.</summary>
	/// <remarks>
	/// The recorder compares each state with the first one it took and records any difference, a release included.
	/// Remap All used to wait a fixed second before each next control, so that a button still held was not taken for
	/// it: sixteen seconds of waiting in a run of seventeen controls. The next control now starts at once and takes
	/// its first state when the device is back at rest, judged against the state Remap All began with, or after the
	/// second for a device that never settles.
	/// </remarks>
	[TestClass]
	public class RemapAllTest
	{
		/// <summary>A controller at rest: nothing pressed, the hats centred, the axes and sliders in the middle.</summary>
		static SourceState AtRest()
		{
			var state = new SourceState();
			for (var i = 0; i < state.Povs.Length; i++)
				state.Povs[i] = -1;
			for (var i = 0; i < state.Axis.Length; i++)
				state.Axis[i] = 32767;
			for (var i = 0; i < state.Sliders.Length; i++)
				state.Sliders[i] = 32767;
			return state;
		}

		/// <summary>A box to record into, on a page of its own, so recording clears no box on the program's pages.</summary>
		static SettingsMapItem Box(Panel page, MapCode code)
		{
			var box = new ComboBox();
			page.Controls.Add(box);
			return new SettingsMapItem { Code = code, Control = box, MapTo = MapTo.Controller1 };
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The control after one still held waits for it to be let go, then records the next press at once")]
		public void The_next_control_waits_for_the_held_button_to_be_let_go()
		{
			using (var page = new Panel())
			using (var recorder = new Recorder())
			{
				var a = Box(page, MapCode.ButtonA);
				var b = Box(page, MapCode.ButtonB);
				var rest = AtRest();
				var first = rest.Clone();
				first.Buttons[0] = true;
				var second = rest.Clone();
				second.Buttons[1] = true;
				recorder.StartRecording(a, rest);
				Assert.IsFalse(recorder.StopRecording(rest), "The first state is the one the next ones are compared with.");
				Assert.IsTrue(recorder.StopRecording(first), "Button 1 was not recorded.");
				Assert.AreEqual("Button 1", a.Control.Text);
				// As Remap All does: the next control at once, while button 1 is still held.
				recorder.StartRecording(b, rest);
				for (var tick = 0; tick < 5; tick++)
					Assert.IsFalse(recorder.StopRecording(first), "Button 1, still held, was recorded as the next control.");
				Assert.IsFalse(recorder.StopRecording(rest), "Letting go of button 1 was recorded as the next control.");
				Assert.IsTrue(recorder.StopRecording(second), "The press after the device came to rest was not recorded at once.");
				Assert.AreEqual("Button 2", b.Control.Text);
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A trigger pressed for one control is not taken for the next until it is back within the recorder's own threshold")]
		public void The_next_control_waits_for_the_axis_to_come_back()
		{
			using (var page = new Panel())
			using (var recorder = new Recorder())
			{
				var trigger = Box(page, MapCode.LeftTrigger);
				var b = Box(page, MapCode.ButtonB);
				var rest = AtRest();
				var pressed = rest.Clone();
				pressed.Axis[2] = 65535;
				// Still more than a tenth of the range from where it rests.
				var easing = rest.Clone();
				easing.Axis[2] = 50000;
				// Within a tenth: at rest, as the recorder counts a move.
				var nearly = rest.Clone();
				nearly.Axis[2] = 34000;
				var second = rest.Clone();
				second.Buttons[1] = true;
				recorder.StartRecording(trigger, rest);
				Assert.IsFalse(recorder.StopRecording(rest));
				Assert.IsTrue(recorder.StopRecording(pressed), "The trigger was not recorded.");
				Assert.AreEqual("HAxis 3", trigger.Control.Text);
				recorder.StartRecording(b, rest);
				Assert.IsFalse(recorder.StopRecording(pressed), "The trigger, still pressed, was recorded as the next control.");
				Assert.IsFalse(recorder.StopRecording(easing), "The trigger coming back was recorded as the next control.");
				Assert.IsFalse(recorder.StopRecording(nearly), "The trigger coming back was recorded as the next control.");
				Assert.IsTrue(recorder.StopRecording(second), "The press after the trigger came back was not recorded.");
				Assert.AreEqual("Button 2", b.Control.Text);
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A device that never comes to rest is compared, once the wait is over, with the state it has then")]
		public void A_device_that_never_settles_is_taken_as_it_is_after_the_wait()
		{
			var wait = Recorder.RestWait;
			Recorder.RestWait = TimeSpan.Zero;
			try
			{
				using (var page = new Panel())
				using (var recorder = new Recorder())
				{
					var b = Box(page, MapCode.ButtonB);
					var rest = AtRest();
					var held = rest.Clone();
					held.Buttons[0] = true;
					var both = held.Clone();
					both.Buttons[1] = true;
					recorder.StartRecording(b, rest);
					Assert.IsFalse(recorder.StopRecording(held), "The state after the wait is the one compared with.");
					Assert.IsTrue(recorder.StopRecording(both), "A press on a device that never came to rest was not recorded.");
					Assert.AreEqual("Button 2", b.Control.Text);
				}
			}
			finally
			{
				Recorder.RestWait = wait;
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Remap All starts the next control at once, with the state it began with")]
		public void Remap_all_starts_the_next_control_at_once()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "PadControl.cs"));
			Assert.IsFalse(source.Contains("StartRecording(), 1000"), "Remap All still waits a fixed second before the next control.");
			StringAssert.Contains(Ui.Between(source, "void StartRecording(SettingsMapItem map = null)", "XboxImage.SetHelpText(helpText);"),
				"RemapRest", "The next control is not told the state Remap All began with.");
		}
	}
}
