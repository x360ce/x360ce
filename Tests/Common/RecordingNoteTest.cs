// @under-test: App.v4/Common/DInput/Recorder.cs, Engine/Maps/SettingsConverter.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System.IO;
using x360ce.App;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// What the status bar says after a switch or a stick is recorded onto a button, and the Invert that
	/// turns it round.
	/// </summary>
	/// <remarks>
	/// Recording keeps the way a control was moved. An arm switch flipped from armed to disarmed while
	/// recording gives a bumper held while disarmed, so the note says where the button is pressed, and
	/// Invert turns it round without looking for the same slider again under Inverted.
	/// </remarks>
	[TestClass]
	public class RecordingNoteTest
	{
		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A switch or a stick recorded onto a button says which position presses it")]
		public void A_control_recorded_onto_a_button_says_which_position_presses_it()
		{
			StringAssert.Contains(Recorder.PressedPositionNote(MapCode.LeftShoulder, "Slider 2"), "reading high");
			StringAssert.Contains(Recorder.PressedPositionNote(MapCode.LeftShoulder, "ISlider 2"), "reading low");
			StringAssert.Contains(Recorder.PressedPositionNote(MapCode.ButtonA, "HAxis 3"), "reading above the middle");
			StringAssert.Contains(Recorder.PressedPositionNote(MapCode.DPadUp, "IHAxis 3"), "reading below the middle");
			foreach (var code in new[] { MapCode.ButtonGuide, MapCode.ButtonBack, MapCode.LeftThumbButton, MapCode.RightShoulder })
			{
				var note = Recorder.PressedPositionNote(code, "Slider 1");
				Assert.IsNotNull(note, code + " is a button and nothing is said.");
				StringAssert.StartsWith(note, "Slider 1 recorded");
				StringAssert.Contains(note, "[Invert]", "The note does not say how to turn it round.");
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Nothing is said where there is no position to explain")]
		public void Nothing_is_said_where_there_is_no_position()
		{
			Assert.IsNull(Recorder.PressedPositionNote(MapCode.ButtonA, "Button 3"), "A button onto a button has no position.");
			Assert.IsNull(Recorder.PressedPositionNote(MapCode.ButtonA, "POV 1 Up"), "A D-Pad direction has no position.");
			Assert.IsNull(Recorder.PressedPositionNote(MapCode.LeftTrigger, "Slider 2"), "A trigger follows the slider; it has no press position.");
			Assert.IsNull(Recorder.PressedPositionNote(MapCode.LeftThumbAxisX, "Axis 1"), "A stick follows the axis; it has no press position.");
			Assert.IsNull(Recorder.PressedPositionNote(MapCode.ButtonA, null));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The position the note names is the one the engine presses the button in")]
		public void The_note_names_the_position_the_engine_presses_in()
		{
			// Each recorded text, a reading where the note says the button is pressed, and one where it is not.
			var cases = new[]
			{
				new { Text = "Slider 2", Pressed = 65535, Released = 0 },
				new { Text = "ISlider 2", Pressed = 0, Released = 65535 },
				new { Text = "HSlider 2", Pressed = 65535, Released = 32767 },
				new { Text = "IHSlider 2", Pressed = 0, Released = 32767 },
			};
			foreach (var c in cases)
			{
				var stored = SettingsConverter.ToIniValue(c.Text, MapCode.LeftShoulder);
				var map = new Map(MapCode.LeftShoulder, stored, GamepadButtonFlags.LeftShoulder, SettingName.DefaultButtonDeadZone);
				Assert.IsTrue(ConvertHelper.IsAxisButtonPressed(c.Pressed, map.IsInverted, map.IsHalf, map.DeadZone, false),
					c.Text + " is not pressed where the note says it is.");
				Assert.IsFalse(ConvertHelper.IsAxisButtonPressed(c.Released, map.IsInverted, map.IsHalf, map.DeadZone, false),
					c.Text + " is pressed where the note says it is not.");
				var note = Recorder.PressedPositionNote(MapCode.LeftShoulder, c.Text);
				if (c.Pressed == 65535)
					Assert.IsTrue(note.Contains("high") || note.Contains("above"), c.Text + " does not name the pressed position.");
				else
					Assert.IsTrue(note.Contains("low") || note.Contains("below"), c.Text + " does not name the pressed position.");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Invert swaps the I in front of a button, an axis or a slider, and has nothing to swap elsewhere")]
		public void Invert_swaps_the_I_and_back()
		{
			Assert.AreEqual("ISlider 2", SettingsConverter.InvertTextValue("Slider 2"));
			Assert.AreEqual("Slider 2", SettingsConverter.InvertTextValue("ISlider 2"));
			Assert.AreEqual("IHAxis 1", SettingsConverter.InvertTextValue("HAxis 1"));
			Assert.AreEqual("HSlider 4", SettingsConverter.InvertTextValue("IHSlider 4"));
			Assert.AreEqual("IButton 3", SettingsConverter.InvertTextValue("Button 3"));
			Assert.AreEqual("Button 3", SettingsConverter.InvertTextValue("IButton 3"));
			foreach (var text in new[] { "POV 1", "POV 1 Up", "=s1", "", null })
				Assert.IsNull(SettingsConverter.InvertTextValue(text), "'" + (text ?? "(null)") + "' has no other way round.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("StopRecording asks for the note and writes it to the status bar")]
		public void StopRecording_wires_the_note_to_the_status_bar()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "Recorder.cs");
			var source = File.ReadAllText(path);
			StringAssert.Contains(source, "PressedPositionNote(code, action)", "StopRecording does not ask for the note.");
			StringAssert.Contains(source, "StatusTimerLabel.Text = note;", "StopRecording does not write the note to the status bar.");
		}
	}
}
