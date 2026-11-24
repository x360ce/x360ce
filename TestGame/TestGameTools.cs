using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Mcp;

namespace x360ce.TestGame
{
	/// <summary>
	/// What a test or an AI assistant asks the game: what it received and what it made of it. Each public static method
	/// marked McpTool is one tool, served with the shared interface tools in <see cref="McpUiTools"/>.
	/// </summary>
	public static class TestGameTools
	{
		/// <summary>The game's window, which holds the recorder and the controllers' readings.</summary>
		public static MainForm Window;

		const string EventFields = "Seq (numbered from 1 as received), Ms (milliseconds since the game started), Time, Api (how the game "
			+ "read it: RawInput, Messages, DirectInput or XInput; one key press arrives once each way that hears it), InFront "
			+ "(whether the window was in front: Messages arrive only then, an Xbox controller through DirectInput reports only then, "
			+ "and keyboard and mouse from behind are kept only while the person allows it), Source (Keyboard, Mouse, XInput 1 to "
			+ "XInput 4, a DirectInput controller by its name, Window, or Action for what the game made of its input), Kind (Down, "
			+ "Repeat for a key the keyboard repeats while held, Up, Move, Wheel, Axis, Hat, Connected, Disconnected, InFront, Behind, "
			+ "Jump, Double jump), Name (Space, Left, LeftButton, A, LX, b1, p1, X), Text (what a key types with the keyboard layout in "
			+ "use), X and Y (a mouse move: how far; when Absolute, where to: 0 to 65535 across the screen through RawInput, pixels "
			+ "through Messages), Value (a wheel's turn, an axis' raw value, a hat's angle in hundredths of a degree, a double jump's "
			+ "gap in milliseconds), FromProgram (RawInput only: true when no device sent it, so a program such as AutoHotkey did) "
			+ "and Note.";

		[McpTool(AiAccess.Read, "The newest event the game received and the newest action it made of its input, such as a double jump, "
			+ "as JSON: Last and LastAction, null while there is none, and InFront (keyboard and mouse input reaches the game only "
			+ "while it is the window in front). Each event has " + EventFields)]
		public static object InputLast()
		{
			var r = Window.Recorder;
			return new System.Collections.Generic.Dictionary<string, object>
			{
				{ "Last", r.Last == null ? null : r.Last.ToDictionary() },
				{ "LastAction", r.LastAction == null ? null : r.LastAction.ToDictionary() },
				{ "InFront", Window.InFront },
			};
		}

		[McpTool(AiAccess.Read, "The events after a sequence number, oldest first, at most count of them, as JSON: Events, and Next, the "
			+ "number the next event will have. Pass the Seq of the last event read as since to read on; since 0 reads from the "
			+ "oldest kept, and the game keeps the last 2000. Each event has " + EventFields)]
		public static object InputEvents(
			[Description("Read the events after this Seq; 0 for the oldest kept.")] int since = 0,
			[Description("The most events to return.")] int count = 200)
		{
			var r = Window.Recorder;
			return new System.Collections.Generic.Dictionary<string, object>
			{
				{ "Events", r.Since(since, count).Select(x => (object)x.ToDictionary()).ToArray() },
				{ "Next", r.Next },
			};
		}

		[McpTool(AiAccess.Read, "What is held and where right now, as JSON: InFront, RecordBehind (whether keyboard and mouse are kept "
			+ "while the window is behind; the person's choice alone), Held (keys and buttons held, as Api/Source/Name), Pointer (the "
			+ "mouse pointer on the screen, in pixels), DirectInput (the controllers DirectInput reads, by the names their events "
			+ "carry), XInput (the four places: Connected, Buttons held, LX LY RX RY from -32768 to 32767, LT RT from 0 to 255, "
			+ "Packet), JumpKey, JumpApi (the way of reading the keyboard the jump follows) and DoubleJumpMs (a second jump press "
			+ "within that many milliseconds of the first is a double jump; button A through XInput jumps too, in front only).")]
		public static object InputState()
		{
			var w = Window;
			string[] directInput;
			object[] xinput;
			string directInputError;
			lock (w.Devices.Sync)
			{
				directInput = w.Devices.DirectInput.Devices;
				directInputError = w.Devices.DirectInput.LastError;
				xinput = Enumerable.Range(0, 4).Select(i => (object)w.Devices.XInput.Describe(i)).ToArray();
			}
			return new System.Collections.Generic.Dictionary<string, object>
			{
				{ "InFront", w.InFront },
				{ "RecordBehind", w.RecordBehind },
				{ "Held", w.Recorder.Held.OrderBy(x => x).ToArray() },
				{ "Pointer", new[] { Cursor.Position.X, Cursor.Position.Y } },
				{ "DirectInput", directInput },
				{ "DirectInputError", directInputError },
				{ "XInput", xinput },
				{ "JumpKey", w.Recorder.JumpKey },
				{ "JumpApi", w.Recorder.JumpApi },
				{ "DoubleJumpMs", w.Recorder.DoubleJumpMs },
			};
		}

		[McpTool(AiAccess.Configure, "Forgets the events and the last action, as Clear does, and returns the Seq the next event will have, "
			+ "so a test reads only what follows.")]
		public static object InputClear()
		{
			return Window.ClearEvents();
		}

		[McpTool(AiAccess.Configure, "Sends rumble to an XInput place as a game does: each motor from 0 (off) to 65535 (full), until "
			+ "it is sent again. Returns whether a controller took it.")]
		public static object InputRumble(
			[Description("The XInput place, 1 to 4.")] int place,
			[Description("The left, heavy motor, 0 to 65535.")] int left,
			[Description("The right, light motor, 0 to 65535.")] int right)
		{
			return XInputReader.Rumble(place, left, right);
		}

		[McpTool(AiAccess.Read, "Reads the Virtual Car simulator's live physics and UDP telemetry transmission status.")]
		public static object CarState()
		{
			var panel = Window.VirtualCar;
			var car = panel.Car;
			return new System.Collections.Generic.Dictionary<string, object>
			{
				{ "SpeedKmh", car.SpeedKmh },
				{ "Gear", car.Gear },
				{ "Rpm", car.Rpm },
				{ "FrontGrip", car.FrontGrip },
				{ "AlignAngleDeg", car.AlignAngleDeg },
				{ "LateralG", car.LateralG },
				{ "CurbImpulse", car.CurbImpulse },
				{ "Surface", car.Surface.ToString() },
				{ "AutoDrive", panel.AutoDrive },
				{ "SendTelemetry", panel.SendTelemetry },
				{ "Protocol", panel.Protocol.ToString() },
				{ "TargetPort", panel.TargetPort },
				{ "PacketsSent", panel.PacketsSent },
				{ "SendRateHz", panel.SendRateHz },
			};
		}

		[McpTool(AiAccess.Configure, "Controls the Virtual Car tab: switches to it, toggles Auto-Drive or UDP telemetry, picks protocol/surface/port, or triggers a curb strike.")]
		public static object CarControl(
			[Description("Optional: true to enable Auto-Drive, false to disable.")] bool? autoDrive = null,
			[Description("Optional: true to send UDP telemetry, false to stop.")] bool? sendTelemetry = null,
			[Description("Optional protocol: ForzaDataOut, EaF1Udp, CodemastersUdp, OutSimUdp, ProjectCars2Udp.")] string protocol = null,
			[Description("Optional surface: DryTarmac, WetTarmac, Gravel, Ice.")] string surface = null,
			[Description("Optional target UDP port (1024 to 65535).")] int? port = null,
			[Description("Optional curb strike: -1 left, +1 right, 0 none.")] int curb = 0,
			[Description("Optional: true to reset the car to the start line.")] bool reset = false)
		{
			Window.SelectVirtualCarTab();
			var panel = Window.VirtualCar;
			if (autoDrive.HasValue)
				panel.AutoDrive = autoDrive.Value;
			if (sendTelemetry.HasValue)
				panel.SendTelemetry = sendTelemetry.Value;
			if (!string.IsNullOrEmpty(protocol))
			{
				VirtualCarProtocol parsed;
				if (System.Enum.TryParse(protocol, true, out parsed))
					panel.Protocol = parsed;
			}
			if (!string.IsNullOrEmpty(surface))
			{
				VirtualCarSurface parsed;
				if (System.Enum.TryParse(surface, true, out parsed))
					panel.Car.Surface = parsed;
			}
			if (port.HasValue)
				panel.TargetPort = port.Value;
			if (curb != 0)
				panel.Car.TriggerCurb(curb < 0 ? -1f : 1f);
			if (reset)
				panel.Car.Reset();
			return CarState();
		}
	}
}
