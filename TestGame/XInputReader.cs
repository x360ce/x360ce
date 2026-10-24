using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace x360ce.TestGame
{
	/// <summary>
	/// The four XInput places, read through Windows' own XInput as a game reads them. A place with no controller is asked
	/// again once a second, because asking an empty place is slow. Button presses and releases become events; sticks and
	/// triggers become events when they move far enough, and their exact values are in <see cref="States"/>.
	/// </summary>
	public sealed class XInputReader
	{
		const int ERROR_SUCCESS = 0;
		const int EmptyPlaceEveryMs = 1000;
		/// <summary>How far a stick or trigger moves before it is an event: one hundredth of its range.</summary>
		const int StickStep = 655, TriggerStep = 3;
		/// <summary>The fewest milliseconds between two events of one stick or trigger.</summary>
		const int AxisEveryMs = 50;

		[StructLayout(LayoutKind.Sequential)]
		public struct XINPUT_GAMEPAD
		{
			public ushort Buttons;
			public byte LeftTrigger;
			public byte RightTrigger;
			public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XINPUT_STATE
		{
			public uint PacketNumber;
			public XINPUT_GAMEPAD Gamepad;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct XINPUT_VIBRATION
		{
			public ushort LeftMotorSpeed;
			public ushort RightMotorSpeed;
		}

		[DllImport("xinput1_4.dll")]
		static extern int XInputGetState(int userIndex, out XINPUT_STATE state);

		[DllImport("xinput1_4.dll")]
		static extern int XInputSetState(int userIndex, ref XINPUT_VIBRATION vibration);

		/// <summary>The buttons by their bit, in the order the controller's picture lists them.</summary>
		public static readonly KeyValuePair<ushort, string>[] ButtonNames =
		{
			new KeyValuePair<ushort, string>(0x1000, "A"),
			new KeyValuePair<ushort, string>(0x2000, "B"),
			new KeyValuePair<ushort, string>(0x4000, "X"),
			new KeyValuePair<ushort, string>(0x8000, "Y"),
			new KeyValuePair<ushort, string>(0x0100, "LB"),
			new KeyValuePair<ushort, string>(0x0200, "RB"),
			new KeyValuePair<ushort, string>(0x0020, "Back"),
			new KeyValuePair<ushort, string>(0x0010, "Start"),
			new KeyValuePair<ushort, string>(0x0040, "LS"),
			new KeyValuePair<ushort, string>(0x0080, "RS"),
			new KeyValuePair<ushort, string>(0x0001, "Up"),
			new KeyValuePair<ushort, string>(0x0002, "Down"),
			new KeyValuePair<ushort, string>(0x0004, "Left"),
			new KeyValuePair<ushort, string>(0x0008, "Right"),
		};

		/// <summary>Each place's last reading, and whether a controller is there.</summary>
		public readonly XINPUT_STATE[] States = new XINPUT_STATE[4];
		public readonly bool[] Connected = new bool[4];

		readonly long[] _askedAt = new long[4];
		readonly int[][] _sentAxes = { new int[6], new int[6], new int[6], new int[6] };
		readonly long[][] _sentAt = { new long[6], new long[6], new long[6], new long[6] };
		static readonly string[] AxisNames = { "LX", "LY", "RX", "RY", "LT", "RT" };

		/// <summary>Reads every place that is due, and returns what changed as events. XInput answers in front and behind alike.</summary>
		public List<InputEvent> Poll(long now, bool inFront)
		{
			var events = Read(now);
			foreach (var e in events)
			{
				e.Api = "XInput";
				e.InFront = inFront;
				e.Ms = now;
			}
			return events;
		}

		List<InputEvent> Read(long now)
		{
			var events = new List<InputEvent>();
			for (var i = 0; i < 4; i++)
			{
				if (!Connected[i] && now - _askedAt[i] < EmptyPlaceEveryMs && _askedAt[i] != 0)
					continue;
				_askedAt[i] = now;
				XINPUT_STATE state;
				var connected = XInputGetState(i, out state) == ERROR_SUCCESS;
				var source = "XInput " + (i + 1);
				if (connected != Connected[i])
				{
					events.Add(new InputEvent { Source = source, Kind = connected ? "Connected" : "Disconnected", Name = "Device" });
					Connected[i] = connected;
				}
				if (!connected)
				{
					state = default(XINPUT_STATE);
				}
				var old = States[i].Gamepad.Buttons;
				var now16 = state.Gamepad.Buttons;
				if (old != now16)
				{
					foreach (var b in ButtonNames)
					{
						if ((old & b.Key) == 0 && (now16 & b.Key) != 0)
							events.Add(new InputEvent { Source = source, Kind = "Down", Name = b.Value });
						else if ((old & b.Key) != 0 && (now16 & b.Key) == 0)
							events.Add(new InputEvent { Source = source, Kind = "Up", Name = b.Value });
					}
				}
				var g = state.Gamepad;
				var axes = new[] { (int)g.ThumbLX, g.ThumbLY, g.ThumbRX, g.ThumbRY, g.LeftTrigger, g.RightTrigger };
				for (var a = 0; a < axes.Length; a++)
				{
					var step = a < 4 ? StickStep : TriggerStep;
					if (Math.Abs(axes[a] - _sentAxes[i][a]) >= step && now - _sentAt[i][a] >= AxisEveryMs
						|| axes[a] != _sentAxes[i][a] && (axes[a] == 0 || IsEnd(a, axes[a])) && now - _sentAt[i][a] >= AxisEveryMs)
					{
						_sentAxes[i][a] = axes[a];
						_sentAt[i][a] = now;
						events.Add(new InputEvent { Source = source, Kind = "Axis", Name = AxisNames[a], Value = axes[a] });
					}
				}
				States[i] = state;
			}
			return events;
		}

		/// <summary>A stick or trigger at the end of its travel, which is always reported, however small the last step.</summary>
		static bool IsEnd(int axis, int value)
		{
			return axis < 4 ? value <= short.MinValue + 1 || value >= short.MaxValue : value >= byte.MaxValue;
		}

		/// <summary>Sends rumble to a place, as a game does: each motor 0 to 65535.</summary>
		public static bool Rumble(int place, int left, int right)
		{
			var v = new XINPUT_VIBRATION
			{
				LeftMotorSpeed = (ushort)Math.Max(0, Math.Min(65535, left)),
				RightMotorSpeed = (ushort)Math.Max(0, Math.Min(65535, right)),
			};
			return XInputSetState(place - 1, ref v) == ERROR_SUCCESS;
		}

		/// <summary>One place as JSON: whether a controller is there, its buttons held, and its sticks and triggers in raw values.</summary>
		public Dictionary<string, object> Describe(int i)
		{
			var g = States[i].Gamepad;
			return new Dictionary<string, object>
			{
				{ "Place", i + 1 },
				{ "Connected", Connected[i] },
				{ "Buttons", ButtonNames.Where(b => (g.Buttons & b.Key) != 0).Select(b => b.Value).ToArray() },
				{ "LX", (int)g.ThumbLX },
				{ "LY", (int)g.ThumbLY },
				{ "RX", (int)g.ThumbRX },
				{ "RY", (int)g.ThumbRY },
				{ "LT", (int)g.LeftTrigger },
				{ "RT", (int)g.RightTrigger },
				{ "Packet", States[i].PacketNumber },
			};
		}
	}
}
