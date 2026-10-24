using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace x360ce.TestGame
{
	/// <summary>
	/// The keyboard and mouse as games read them, through Raw Input. A move is reported as the device or the program sent
	/// it, before Windows turns it into a pointer position. Input with no device behind it came from a program, such as
	/// AutoHotkey, through SendInput. Registered to receive while behind, it hears every program's input, and each message
	/// says whether the window was in front.
	/// </summary>
	public static class RawInput
	{
		public const int WM_INPUT = 0x00FF;

		/// <summary>wParam of WM_INPUT: the input arrived while the window was in front, or behind.</summary>
		const int RIM_INPUT = 0;

		/// <summary>Receive while the window is behind as well.</summary>
		const uint RIDEV_INPUTSINK = 0x00000100;

		const uint RID_INPUT = 0x10000003;
		const uint RIM_TYPEMOUSE = 0;
		const uint RIM_TYPEKEYBOARD = 1;
		const ushort RI_KEY_BREAK = 1;
		const ushort MOUSE_MOVE_ABSOLUTE = 1;

		[StructLayout(LayoutKind.Sequential)]
		struct RAWINPUTDEVICE
		{
			public ushort UsagePage;
			public ushort Usage;
			public uint Flags;
			public IntPtr Target;
		}

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);

		[DllImport("user32.dll")]
		static extern uint GetRawInputData(IntPtr rawInput, uint command, byte[] data, ref uint size, uint headerSize);

		[DllImport("user32.dll")]
		static extern bool GetKeyboardState(byte[] state);

		[DllImport("user32.dll")]
		static extern IntPtr GetKeyboardLayout(uint threadId);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern int ToUnicodeEx(uint virtualKey, uint scanCode, byte[] keyState, StringBuilder buffer, int size, uint flags, IntPtr layout);

		/// <summary>Asks for the keyboard and the mouse: only while the window is in front, as most games ask, or behind as well.</summary>
		public static void Register(IntPtr window, bool behindToo)
		{
			var flags = behindToo ? RIDEV_INPUTSINK : 0;
			var devices = new[]
			{
				new RAWINPUTDEVICE { UsagePage = 1, Usage = 6, Flags = flags, Target = window },
				new RAWINPUTDEVICE { UsagePage = 1, Usage = 2, Flags = flags, Target = window },
			};
			if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
				throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
		}

		/// <summary>What one WM_INPUT message says, as events: a key, a move, or the buttons and wheel of one mouse report.</summary>
		public static List<InputEvent> Read(IntPtr wParam, IntPtr lParam)
		{
			var events = Read(lParam);
			var inFront = wParam.ToInt64() == RIM_INPUT;
			foreach (var e in events)
			{
				e.Api = "RawInput";
				e.InFront = inFront;
			}
			return events;
		}

		static List<InputEvent> Read(IntPtr lParam)
		{
			var events = new List<InputEvent>();
			var headerSize = (uint)(8 + 2 * IntPtr.Size);
			uint size = 0;
			GetRawInputData(lParam, RID_INPUT, null, ref size, headerSize);
			if (size == 0)
				return events;
			var data = new byte[size];
			if (GetRawInputData(lParam, RID_INPUT, data, ref size, headerSize) != size)
				return events;
			var type = BitConverter.ToUInt32(data, 0);
			var device = IntPtr.Size == 8 ? BitConverter.ToInt64(data, 8) : BitConverter.ToInt32(data, 8);
			var fromProgram = device == 0;
			var o = (int)headerSize;
			if (type == RIM_TYPEKEYBOARD)
			{
				var makeCode = BitConverter.ToUInt16(data, o);
				var flags = BitConverter.ToUInt16(data, o + 2);
				var vkey = BitConverter.ToUInt16(data, o + 6);
				// 255 is the half of a key pair Windows sends for an extended key; the key itself follows.
				if (vkey == 0 || vkey == 255)
					return events;
				var up = (flags & RI_KEY_BREAK) != 0;
				// VK_PACKET: a character sent as itself, as AutoHotkey's SendText sends it, carried in the scan code.
				var packet = vkey == (ushort)Keys.Packet;
				events.Add(new InputEvent
				{
					Source = "Keyboard",
					Kind = up ? "Up" : "Down",
					Name = packet ? "Packet" : ((Keys)vkey).ToString(),
					Text = up ? null : packet ? ((char)makeCode).ToString() : KeyText(vkey, makeCode),
					FromProgram = fromProgram,
				});
			}
			else if (type == RIM_TYPEMOUSE)
			{
				var moveFlags = BitConverter.ToUInt16(data, o);
				var buttons = BitConverter.ToUInt16(data, o + 4);
				var buttonData = BitConverter.ToInt16(data, o + 6);
				var x = BitConverter.ToInt32(data, o + 12);
				var y = BitConverter.ToInt32(data, o + 16);
				var absolute = (moveFlags & MOUSE_MOVE_ABSOLUTE) != 0;
				if (x != 0 || y != 0 || absolute)
					events.Add(new InputEvent { Source = "Mouse", Kind = "Move", Name = "Move", X = x, Y = y, Absolute = absolute, FromProgram = fromProgram });
				AddButton(events, buttons, 0x0001, 0x0002, "LeftButton", fromProgram);
				AddButton(events, buttons, 0x0004, 0x0008, "RightButton", fromProgram);
				AddButton(events, buttons, 0x0010, 0x0020, "MiddleButton", fromProgram);
				AddButton(events, buttons, 0x0040, 0x0080, "XButton1", fromProgram);
				AddButton(events, buttons, 0x0100, 0x0200, "XButton2", fromProgram);
				if ((buttons & 0x0400) != 0)
					events.Add(new InputEvent { Source = "Mouse", Kind = "Wheel", Name = "Wheel", Value = buttonData, FromProgram = fromProgram });
				if ((buttons & 0x0800) != 0)
					events.Add(new InputEvent { Source = "Mouse", Kind = "Wheel", Name = "HorizontalWheel", Value = buttonData, FromProgram = fromProgram });
			}
			return events;
		}

		static void AddButton(List<InputEvent> events, ushort buttons, ushort down, ushort up, string name, bool fromProgram)
		{
			if ((buttons & down) != 0)
				events.Add(new InputEvent { Source = "Mouse", Kind = "Down", Name = name, FromProgram = fromProgram });
			if ((buttons & up) != 0)
				events.Add(new InputEvent { Source = "Mouse", Kind = "Up", Name = name, FromProgram = fromProgram });
		}

		/// <summary>What a key types with the window's keyboard layout and the Shift, Ctrl and Caps Lock state, or null.</summary>
		public static string KeyText(ushort vkey, ushort makeCode)
		{
			var state = new byte[256];
			GetKeyboardState(state);
			var text = new StringBuilder(8);
			// Flag 4: the call leaves the keyboard's state as it was, so a dead key still works for what follows.
			var count = ToUnicodeEx(vkey, makeCode, state, text, text.Capacity, 4, GetKeyboardLayout(0));
			if (count <= 0)
				return null;
			var s = text.ToString(0, count);
			return s.Length > 0 && !char.IsControl(s[0]) ? s : null;
		}
	}
}
