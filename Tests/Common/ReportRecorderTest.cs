// @under-test: App.v4/Common/Recording/ReportRecorder.cs, App.v4/Common/Recording/ReportRecorder.Header.cs, App.v4/Common/DInput/DInputHelper.Recording.cs
// @area: recording   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.App.Recording;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A recording for a bug report writes each layer where its value is made, only when it changes, and nothing that
	/// names a person: no keyboard or mouse input, no user, PC or folder.
	/// </summary>
	[TestClass]
	public class ReportRecorderTest
	{
		static readonly UserGame Game = new UserGame { FileName = "recording-test.exe", FileProductName = "Recording test", EnableMask = (int)MapToMask.Controller1 };

		string _folder;

		[TestInitialize]
		public void MakeFolder()
		{
			_folder = Path.Combine(Path.GetTempPath(), "x360ce-recording-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_folder);
		}

		[TestCleanup]
		public void RemoveFolder()
		{
			if (Directory.Exists(_folder))
				Directory.Delete(_folder, true);
		}

		/// <summary>A recording into the test's folder, started with <paramref name="engine"/>.</summary>
		ReportRecorder Start(RecordParts parts, DInputHelper engine = null)
		{
			var recorder = new ReportRecorder(parts, Path.Combine(_folder, "recording.txt"), 100);
			recorder.Start(engine);
			return recorder;
		}

		/// <summary>Stops the recording and gives its lines.</summary>
		static string[] Finish(ReportRecorder recorder)
		{
			recorder.Stop();
			Assert.IsTrue(recorder.IsStopped, "The recording did not end.");
			Assert.IsNull(recorder.StopReason, "The recording ended early.");
			var lines = File.ReadAllLines(recorder.FilePath);
			recorder.Dispose();
			return lines;
		}

		static UserDevice Device(string name, SharpDX.DirectInput.DeviceType type)
		{
			return new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				ProductName = name,
				CapType = (int)type,
				CapAxeCount = 2,
				CapButtonCount = 8,
				DiAxeMask = 0x3,
				IsOnline = true,
				IsEnabled = true,
				SourceState = new SourceState(),
			};
		}

		/// <summary>The device's label in the file, from its line in the header or where it was first named.</summary>
		static string LabelOf(string[] lines, string product)
		{
			var pattern = new System.Text.RegularExpressions.Regex(@"\b(D\d+): " + System.Text.RegularExpressions.Regex.Escape(product) + ",");
			var match = lines.Select(x => pattern.Match(x)).FirstOrDefault(x => x.Success);
			Assert.IsNotNull(match, "The device is not described in the file.");
			return match.Groups[1].Value;
		}

		[TestMethod, TestCategory("recording")]
		[Description("A device's axes are written at the start and then only when they move a step; a button each time it changes")]
		public void Input_is_written_when_it_changes()
		{
			var wheel = Device("Recording Test Wheel", SharpDX.DirectInput.DeviceType.Driving);
			var recorder = Start(RecordParts.Input);
			wheel.SourceState.Axis[0] = 32939;
			recorder.Device(wheel, System.Diagnostics.Stopwatch.GetTimestamp());
			// Less than a step: nothing.
			wheel.SourceState.Axis[0] = 32939 + ReportRecorder.AxisStep - 1;
			recorder.Device(wheel, System.Diagnostics.Stopwatch.GetTimestamp());
			// A step: written.
			wheel.SourceState.Axis[0] = 40000;
			wheel.SourceState.Buttons[5] = true;
			recorder.Device(wheel, System.Diagnostics.Stopwatch.GetTimestamp());
			wheel.SourceState.Buttons[5] = false;
			recorder.Device(wheel, System.Diagnostics.Stopwatch.GetTimestamp());
			var lines = Finish(recorder);
			var d = LabelOf(lines, "Recording Test Wheel");
			Assert.IsTrue(lines.Any(x => x.EndsWith(" " + d + " Axis 1 32939 (at start)")), "The axis at the start is missing.");
			Assert.IsFalse(lines.Any(x => x.Contains(" " + d + " Axis 1 " + (32939 + ReportRecorder.AxisStep - 1))), "A move under a step was written.");
			Assert.IsTrue(lines.Any(x => x.EndsWith(" " + d + " Axis 1 40000")), "A move of a step is missing.");
			Assert.AreEqual(1, lines.Count(x => x.EndsWith(" " + d + " Button 6 down")), "The press is not written once.");
			Assert.AreEqual(1, lines.Count(x => x.EndsWith(" " + d + " Button 6 up")), "The release is not written once.");
		}

		[TestMethod, TestCategory("recording")]
		[Description("No key of a keyboard and no button of a mouse is ever written, so nothing a person types is in the file")]
		public void Keyboards_and_mice_are_never_recorded()
		{
			var keyboard = Device("Recording Test Keyboard", SharpDX.DirectInput.DeviceType.Keyboard);
			var mouse = Device("Recording Test Mouse", SharpDX.DirectInput.DeviceType.Mouse);
			var recorder = Start(RecordParts.All);
			for (var i = 0; i < 10; i++)
			{
				keyboard.SourceState.Buttons[i] = true;
				mouse.SourceState.Axis[0] = i * 5000;
				recorder.Device(keyboard, System.Diagnostics.Stopwatch.GetTimestamp());
				recorder.Device(mouse, System.Diagnostics.Stopwatch.GetTimestamp());
			}
			var lines = Finish(recorder);
			Assert.IsFalse(lines.Any(x => x.Contains("Button") && x.Contains("down")), "A key or mouse button was written.");
			Assert.IsFalse(lines.Any(x => x.Contains(" Axis 1 ")), "The mouse's movement was written.");
		}

		[TestMethod, TestCategory("recording")]
		[Description("The file holds no user name, PC name, folder or AI access token, even with every part recorded")]
		public void The_file_names_no_person()
		{
			var recorder = Start(RecordParts.All);
			recorder.Note("error at C:\\Users\\" + Environment.UserName + "\\AppData\\x360ce.exe on " + Environment.MachineName);
			var text = string.Join("\n", Finish(recorder));
			if (Environment.UserName.Length >= 3)
				StringAssert.DoesNotMatch(text, new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(Environment.UserName), System.Text.RegularExpressions.RegexOptions.IgnoreCase), "The user's name is in the file.");
			if (Environment.MachineName.Length >= 3)
				StringAssert.DoesNotMatch(text, new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(Environment.MachineName), System.Text.RegularExpressions.RegexOptions.IgnoreCase), "The PC's name is in the file.");
			StringAssert.DoesNotMatch(text, new System.Text.RegularExpressions.Regex(@"[A-Za-z]:\\"), "A folder path is in the file.");
			var token = SettingsManager.AiOptions.AiAccessToken;
			if (!string.IsNullOrEmpty(token))
				Assert.IsFalse(text.Contains(token), "The AI access token is in the file.");
			StringAssert.Contains(text, "error at x360ce.exe on <pc>");
			StringAssert.Contains(text, "Program: x360ce ");
		}

		[TestMethod, TestCategory("recording")]
		[Description("What a controller sends, its virtual controller and its game's vibration are written as they change")]
		public void Controller_output_and_vibration_are_written()
		{
			var recorder = Start(RecordParts.Output | RecordParts.Forces);
			var now = System.Diagnostics.Stopwatch.GetTimestamp();
			var g = new Gamepad { Buttons = GamepadButtonFlags.A | GamepadButtonFlags.Start, LeftThumbX = 4078, RightThumbY = -32768, RightTrigger = 255 };
			recorder.PadOutput(0, true, g, now);
			recorder.PadOutput(0, true, g, now);
			recorder.PadFeed(0, 1, VirtualError.None, 0, now);
			recorder.PadFeed(1, 0, VirtualError.PlaceTaken, -1, now);
			recorder.GameForce(0, 200, 50, now);
			recorder.GameForce(0, 200, 50, now);
			var lines = Finish(recorder);
			Assert.AreEqual(1, lines.Count(x => x.EndsWith(" C1 out: Start+A, LT 0, RT 255, LX 4078, LY 0, RX 0, RY -32768")), "The output is not written once.");
			Assert.IsTrue(lines.Any(x => x.EndsWith(" C1 virtual controller fed, XInput place 1")), "The fed controller is missing.");
			Assert.IsTrue(lines.Any(x => x.EndsWith(" C2 virtual controller not fed, PlaceTaken, no XInput place")), "The refused controller is missing.");
			Assert.AreEqual(1, lines.Count(x => x.EndsWith(" C1 game asks for vibration: large 200, small 50")), "The vibration is not written once.");
		}

		[TestMethod, TestCategory("recording")]
		[Description("A device call that takes long is written with its time, and one that does not return is said while it runs")]
		public void Slow_and_stuck_device_calls_are_written()
		{
			var wheel = Device("Recording Test Slow Wheel", SharpDX.DirectInput.DeviceType.Driving);
			var recorder = Start(RecordParts.Engine);
			recorder.Calling(wheel, "Acquire (Exclusive)");
			Thread.Sleep(60);
			recorder.Calling(wheel, "device.GetCurrentState()");
			// Left in the call for longer than the recording waits before saying so.
			Thread.Sleep(2500);
			recorder.Calling(wheel, null);
			var lines = Finish(recorder);
			var d = LabelOf(lines, "Recording Test Slow Wheel");
			Assert.IsTrue(lines.Any(x => x.Contains(" " + d + " Acquire (Exclusive) took ")), "The slow call is missing.");
			Assert.IsTrue(lines.Any(x => x.Contains(" " + d + " has been in device.GetCurrentState() for ")), "The stuck call was not said while it ran.");
		}

		[TestMethod, TestCategory("recording")]
		[Description("A window that stops answering is written once it has been silent a second, and again when it answers")]
		public void A_window_that_stops_answering_is_written()
		{
			var recorder = Start(RecordParts.Window);
			recorder.Beat("in front");
			Thread.Sleep(2500);
			recorder.Beat("in front");
			Thread.Sleep(1200);
			var lines = Finish(recorder);
			Assert.IsTrue(lines.Any(x => x.Contains(" window stopped answering ")), "The silent window is missing.");
			Assert.IsTrue(lines.Any(x => x.Contains(" window answers again after ")), "The window answering again is missing.");
		}

		[TestMethod, TestCategory("recording")]
		[Description("An error the program logs is written with its type and message, and without its folders")]
		public void A_logged_error_is_written_without_folders()
		{
			var recorder = Start(RecordParts.Window);
			var logged = typeof(ReportRecorder).GetMethod("LogHelper_WritingException", BindingFlags.NonPublic | BindingFlags.Instance);
			var error = new InvalidOperationException(@"Could not open C:\Users\Someone\Documents\pad.xml");
			logged.Invoke(recorder, new object[] { null, new JocysCom.ClassLibrary.Runtime.LogHelperEventArgs { Exception = error } });
			var lines = Finish(recorder);
			Assert.IsTrue(lines.Any(x => x.Contains(" error System.InvalidOperationException: Could not open pad.xml")), "The error is missing, or keeps its folders.");
		}

		[TestMethod, TestCategory("recording"), TestCategory("engine")]
		[Description("The engine hands a recording what a pass read from each mapped device and sent to each controller")]
		public void The_engine_hands_a_pass_to_the_recording()
		{
			var helper = new DInputHelper();
			var wheel = Device("Recording Test Engine Wheel", SharpDX.DirectInput.DeviceType.Driving);
			var ps = new PadSetting { LeftThumbAxisX = "a1", PadSettingChecksum = Guid.NewGuid() };
			var row = new UserSetting { InstanceGuid = wheel.InstanceGuid, FileName = Game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			var routing = DeviceRouting.Build(Game, new[] { row }, new[] { ps }, new[] { wheel });
			var recordPass = typeof(DInputHelper).GetMethod("RecordPass", BindingFlags.NonPublic | BindingFlags.Instance);
			var recorder = Start(RecordParts.Input | RecordParts.Output | RecordParts.Forces, helper);
			try
			{
				Assert.AreSame(recorder, helper.Recording, "The engine was not given the recording.");
				wheel.SourceState.Axis[0] = 12345;
				helper.CombinedXiConencted[0] = true;
				helper.CombinedXiStates[0].Gamepad.LeftThumbX = -20000;
				recordPass.Invoke(helper, new object[] { recorder, routing, false });
			}
			finally
			{
				helper.Dispose();
			}
			var lines = Finish(recorder);
			Assert.IsNull(helper.Recording, "The engine kept the recording after it stopped.");
			var d = LabelOf(lines, "Recording Test Engine Wheel");
			Assert.IsTrue(lines.Any(x => x.EndsWith(" " + d + " Axis 1 12345 (at start)")), "The device's axis is missing.");
			Assert.IsTrue(lines.Any(x => x.EndsWith(" C1 out: no buttons, LT 0, RT 0, LX -20000, LY 0, RX 0, RY 0")), "The controller's output is missing.");
		}
	}
}
