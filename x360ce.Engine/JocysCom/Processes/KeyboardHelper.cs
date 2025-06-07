using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JocysCom.ClassLibrary.Processes
{
	public static class KeyboardHelper
	{

		internal class NativeMethods
		{
			internal const int INPUT_MOUSE = 0;
			internal const int INPUT_KEYBOARD = 1;
			internal const int INPUT_HARDWARE = 2;
			internal const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
			internal const uint KEYEVENTF_KEYUP = 0x0002;
			internal const uint KEYEVENTF_UNICODE = 0x0004;
			internal const uint KEYEVENTF_SCANCODE = 0x0008;
			internal const uint XBUTTON1 = 0x0001;
			internal const uint XBUTTON2 = 0x0002;
			internal const uint MOUSEEVENTF_MOVE = 0x0001;
			internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
			internal const uint MOUSEEVENTF_LEFTUP = 0x0004;
			internal const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
			internal const uint MOUSEEVENTF_RIGHTUP = 0x0010;
			internal const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
			internal const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
			internal const uint MOUSEEVENTF_XDOWN = 0x0080;
			internal const uint MOUSEEVENTF_XUP = 0x0100;
			internal const uint MOUSEEVENTF_WHEEL = 0x0800;
			internal const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
			internal const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

			internal struct INPUT
			{
				public int type;
				public InputUnion u;
			}

			[StructLayout(LayoutKind.Explicit)]
			internal struct InputUnion
			{
				[FieldOffset(0)]
				public MOUSEINPUT mi;
				[FieldOffset(0)]
				public KEYBDINPUT ki;
				[FieldOffset(0)]
				public HARDWAREINPUT hi;
			}

			[StructLayout(LayoutKind.Sequential)]
			internal struct MOUSEINPUT
			{
				public int dx;
				public int dy;
				public uint mouseData;
				public uint dwFlags;
				public uint time;
				public IntPtr dwExtraInfo;
			}

			[StructLayout(LayoutKind.Sequential)]
			internal struct KEYBDINPUT
			{
				/*Virtual Key code.  Must be from 1-254.  If the dwFlags member specifies KEYEVENTF_UNICODE, wVk must be 0.*/
				public ushort wVk;
				/*A hardware scan code for the key. If dwFlags specifies KEYEVENTF_UNICODE, wScan specifies a Unicode character which is to be sent to the foreground application.*/
				public ushort wScan;
				/*Specifies various aspects of a keystroke.  See the KEYEVENTF_ constants for more information.*/
				public uint dwFlags;
				/*The time stamp for the event, in milliseconds. If this parameter is zero, the system will provide its own time stamp.*/
				public uint time;
				/*An additional value associated with the keystroke. Use the GetMessageExtraInfo function to obtain this information.*/
				public IntPtr dwExtraInfo;
			}

			[StructLayout(LayoutKind.Sequential)]
			internal struct HARDWAREINPUT
			{
				public uint uMsg;
				public ushort wParamL;
				public ushort wParamH;
			}

			[DllImport("user32.dll")]
			internal static extern IntPtr GetMessageExtraInfo();

			[DllImport("user32.dll", SetLastError = true)]
			internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

		}


		#region Keyboard Event

		[DllImport("user32.dll", SetLastError = true)]
		private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

		const int KEYEVENTF_EXTENDEDKEY = 1; // Key Down Flag
		const int KEYEVENTF_KEYUP = 2; // Key Up Flag

		public static void SendKey(byte key)
		{
			keybd_event(key, 0, KEYEVENTF_EXTENDEDKEY, 0);
			keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
		}

		public static bool SendingKey;

		/// <summary>SHIFT: +(key), CTRL: ^(key), ALT %(key)</summary>
		public static void SendKey(string sKeys)
		{
			SendingKey = true;
			byte VK_NUMPAD0 = 0x60;
			byte VK_NUMPAD1 = 0x61;
			byte VK_NUMPAD2 = 0x62;
			byte VK_NUMPAD3 = 0x63;
			byte VK_NUMPAD4 = 0x64;
			byte VK_NUMPAD5 = 0x65;
			byte VK_NUMPAD6 = 0x66;
			byte VK_NUMPAD7 = 0x67;
			byte VK_NUMPAD8 = 0x68;
			byte VK_NUMPAD9 = 0x69;
			if (sKeys == "{NUM0}")
				SendKey(VK_NUMPAD0);
			else if (sKeys == "{NUM1}")
				SendKey(VK_NUMPAD1);
			else if (sKeys == "{NUM2}")
				SendKey(VK_NUMPAD2);
			else if (sKeys == "{NUM3}")
				SendKey(VK_NUMPAD3);
			else if (sKeys == "{NUM4}")
				SendKey(VK_NUMPAD4);
			else if (sKeys == "{NUM5}")
				SendKey(VK_NUMPAD5);
			else if (sKeys == "{NUM6}")
				SendKey(VK_NUMPAD6);
			else if (sKeys == "{NUM7}")
				SendKey(VK_NUMPAD7);
			else if (sKeys == "{NUM8}")
				SendKey(VK_NUMPAD8);
			else if (sKeys == "{NUM9}")
				SendKey(VK_NUMPAD9);
			//else if (sKeys == "{RM}" && !string.IsNullOrEmpty(processName))
			//	MouseHelper.SendRMouseClick(processName);
			//else if (sKeys == "{LM}" && !string.IsNullOrEmpty(processName))
			//	MouseHelper.SendLMouseClick(processName);
			else
				System.Windows.Forms.SendKeys.Send(sKeys);
			SendingKey = false;
		}

		#region Type Keys

		/// <summary>
		/// Sends keystrokes corresponding to the specified Unicode string to the currently active window.
		/// </summary>
		/// <param name="s">The string to send.</param>
		/// <param name="millisecondsDelay">
		///  Delay in milliseconds between typing each character:
		/// - 0: Use this to paste the text without any typing delay.
		/// - 20: Use this value to simulate typical AI typing speed.
		/// - 200: Use this value to simulate typical human typing speed.
		/// Default is 0, meaning it pastes immediately.
		/// </param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		public static async Task TypeKeys(string s, int millisecondsDelay = 0, CancellationToken cancellationToken = default)
		{
			// Loop through each Unicode character in the string.
			foreach (char c in s)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var inputs = new List<NativeMethods.INPUT>();
				// First send a key down, then a key up.
				foreach (bool keyUp in new bool[] { false, true })
				{
					// INPUT is a multi-purpose structure which can be used 
					// for synthesizing keystrokes, mouse motions, and button clicks.
					var input = new NativeMethods.INPUT
					{
						// Need a keyboard event.
						type = NativeMethods.INPUT_KEYBOARD,
						u = new NativeMethods.InputUnion
						{
							// KEYBDINPUT will contain all the information for a single keyboard event
							// (more precisely, for a single key-down or key-up).
							ki = new NativeMethods.KEYBDINPUT
							{
								// Virtual-key code must be 0 since we are sending Unicode characters.
								wVk = 0,
								// The Unicode character to be sent.
								wScan = c,
								// Indicate that we are sending a Unicode character.
								// Also indicate key-up on the second iteration.
								dwFlags = NativeMethods.KEYEVENTF_UNICODE | (keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0),
								dwExtraInfo = NativeMethods.GetMessageExtraInfo(),
							}
						}
					};
					// Add to the list (to be sent later).
					inputs.Add(input);
				}
				// Send the inputs for this character
				NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(NativeMethods.INPUT)));
				if (millisecondsDelay > 0)
				{
					// Randomization amount set to ±20% of the delay.
					var randomizeAmount = (int)(millisecondsDelay * 0.2);
					var randomDelay = millisecondsDelay +
						(randomizeAmount > 0 ? random.Next(-randomizeAmount, randomizeAmount + 1) : 0);
					if (randomDelay > 0)
						// Delay with cancellation support
						await Task.Delay(randomDelay, cancellationToken);
				}
				if (cancellationToken.IsCancellationRequested)
					return;
			}
		}

		private static readonly Random random = new Random();

		#endregion

		public static void SendDown(params Key[] keys)
			=> Send(true, false, keys);

		public static void SendUp(params Key[] keys)
			=> Send(false, true, keys);

		public static void Send(params Key[] keys)
			=> Send(true, true, keys);

		/// <summary>
		/// Press down and up all specified keys.
		/// </summary>
		public static void Send(bool down, bool up, params Key[] keys)
		{
			if (down)
				foreach (var key in keys)
					keybd_event((byte)GetVKey(key), 0, KEYEVENTF_EXTENDEDKEY, 0);
			if (up)
				foreach (var key in keys)
					keybd_event((byte)GetVKey(key), 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
		}

		#endregion

		#region SendMessage

		#region Win32 - user32

		/// <summary>Maps a virtual key to a key code with specified keyboard.</summary>
		private const uint MAPVK_VK_TO_VSC_EX = 0x04;
		private const ushort KEY_PRESSED = 0xF000;

		// (Winuser.h) - Win32 apps - Microsoft Docs
		private const int KEY_DOWN = 0x0100;
		private const int KEY_UP = 0x0101;
		private const int VM_CHAR = 0x0102;

		/// <summary>Gets the key state of the specified key.</summary>
		/// <param name="nVirtKey">The key to check.</param>
		[DllImport("user32.dll")]
		private static extern ushort GetKeyState(int nVirtKey);

		/// <summary>
		/// Returns a handle to the foreground window.
		/// </summary>
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetFocus(IntPtr hWnd);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool SendMessage(IntPtr hWnd, int wMsg, uint wParam, uint lParam);

		[DllImport("user32.dll")]
		private static extern uint MapVirtualKey(uint uCode, uint uMapType);

		#endregion

		private static uint GetVKey(Key key)
			=> (uint)KeyInterop.VirtualKeyFromKey(key);

		/// <summary>
		/// Send text to the application.
		/// </summary>
		/// <param name="hWnd">Window handle.</param>
		/// <param name="msg">Text message to send.</param>
		/// <param name="useShiftEnter">Use SHIFT+ENTER instead of '\n', and ignore '\r'. Used for chat apps.</param>
		/// <param name="pressEnter">Press enter at the end to submit message. Used for chat apps.</param>
		public static void SendText(IntPtr hWnd, string msg, bool useShiftEnter = false, bool pressEnter = false)
		{
			foreach (char c in msg)
			{
				// If return and new line must be replaced with SHIF+ENTER
				if (useShiftEnter)
				{
					if (c == '\r')
						continue;
					if (c == '\n')
					{
						// SendMessage or PostMessage can't be used to simulate the state of the modifiers keys properly.
						// Use keybd_event to press SHIFT+ENTER/RETURN.
						Send(Key.RightShift, Key.Return);
						// Give some time for the target program to process the keys.
						Task.Delay(50).Wait();
						continue;
					}
				}
				SendChar(hWnd, c, true);
			}
			if (pressEnter)
				SendMessage(hWnd, GetVKey(Key.Return), true);
		}

		public static bool SendChar(IntPtr hWnd, char c, bool checkKeyboardState)
		{
			if (checkKeyboardState)
				WaitWhileKeyModifierIsPressed();
			if (SendMessage(hWnd, VM_CHAR, c, 0))
				return false;
			return true;
		}

		public static bool SendKeyDown(IntPtr hWnd, Key key)
		{
			return !SendMessage(hWnd, KEY_DOWN, GetVKey(key), GetLParam(1, GetVKey(key), 0, 0, 0, 0));
		}

		public static bool SendKeyUp(IntPtr hWnd, Key key)
		{
			return !SendMessage(hWnd, KEY_UP, GetVKey(key), GetLParam(1, GetVKey(key), 0, 0, 1, 1));
		}

		public static bool SendMessage(IntPtr hWnd, uint key, bool checkKeyboardState, int delay = 100)
		{
			if (checkKeyboardState)
				WaitWhileKeyModifierIsPressed();
			if (SendMessage(hWnd, KEY_DOWN, key, GetLParam(1, key, 0, 0, 0, 0)))
				return false;
			Task.Delay(delay).Wait();
			if (SendMessage(hWnd, VM_CHAR, key, GetLParam(1, key, 0, 0, 0, 0)))
				return false;
			Task.Delay(delay).Wait();
			if (SendMessage(hWnd, KEY_UP, key, GetLParam(1, key, 0, 0, 1, 1)))
				return false;
			Task.Delay(delay).Wait();
			return true;
		}

		/// <summary>
		/// Send message and optionaly focus message and return window.
		/// </summary>
		/// <param name="message">Text message to send.</param>
		/// <param name="useShiftEnter">Use SHIFT+ENTER instead of '\n', and ignore '\r'. Used for chat apps.</param>
		/// <param name="pressEnter">Press enter at the end to submit message. Used for chat apps.</param>
		/// <param name="messageWindowHandle">Target window.</param>
		/// <param name="messageWindowFocusHandle">Control handle to focus specified.</param>
		/// <param name="returnWindowHandle">Handle of return window.</param>
		public static bool SendTextMessage(
			string message,
			bool useShiftEnter = false, bool pressEnter = false,
			IntPtr messageWindowHandle = default,
			IntPtr messageWindowFocusHandle = default,
			IntPtr returnWindowHandle = default
		)
		{
			// Try to set target message window as foreground if not set yet.
			if (messageWindowHandle != default || GetForegroundWindow() != messageWindowHandle)
			{
				if (!SetForegroundWindow(messageWindowHandle))
					return false;
			}
			if (messageWindowFocusHandle != IntPtr.Zero)
				SetFocus(messageWindowFocusHandle);
			SendText(messageWindowHandle, message, useShiftEnter, pressEnter);
			if (returnWindowHandle != default)
				SetForegroundWindow(returnWindowHandle);
			return true;
		}

		/// <summary>
		/// Wait untill all modifier keys are released.
		/// </summary>
		private static void WaitWhileKeyModifierIsPressed(CancellationToken cancellationToken = default)
		{
			while (IsPressed(
				Key.LeftAlt, Key.RightAlt,
				Key.LeftCtrl, Key.RightCtrl,
				Key.LeftShift, Key.RightShift,
				Key.LWin, Key.RWin)
			)
				Task.Delay(10, cancellationToken).Wait();
		}

		/// <summary>
		/// Return true if key is pressed.
		/// </summary>
		public static bool IsPressed(params Key[] keys)
		{
			foreach (var key in keys)
				if ((GetKeyState((int)GetVKey(key)) & KEY_PRESSED) == KEY_PRESSED)
					return true;
			return false;
		}

		/// <summary>
		/// Get lParam for SendMessage funcion.
		/// </summary>
		private static uint GetLParam(short repeatCount, uint vKey, byte extended, byte contextCode, byte previousState, byte transitionState)
		{
			var lParam = (uint)repeatCount;
			uint scanCode = MapVirtualKey(vKey, MAPVK_VK_TO_VSC_EX);
			lParam += scanCode * 0x10000;
			lParam += (uint)(extended * 0x1000000);
			lParam += (uint)(contextCode * 2 * 0x10000000);
			lParam += (uint)(previousState * 4 * 0x10000000);
			lParam += (uint)(transitionState * 8 * 0x10000000);
			return lParam;
		}

		#endregion

	}
}
