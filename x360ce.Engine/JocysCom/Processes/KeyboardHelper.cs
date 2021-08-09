using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Processes
{
	public class KeyboardHelper
	{

		[DllImport("user32.dll", SetLastError = true)]
		static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

		const int KEYEVENTF_EXTENDEDKEY = 1; // Key Down Flag
		const int KEYEVENTF_KEYUP = 2; // Key Up Flag

		public static void SendKey(byte key)
		{
			keybd_event(key, 0, KEYEVENTF_EXTENDEDKEY, 0);
			keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
		}

		public static bool SendingKey;

		/// <summary>SHIFT: +(key), CTRL: ^(key), ALT %(key)</summary>
		public static void SendKey(string sKeys, string processName = null)
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
			else if (sKeys == "{RM}" && !string.IsNullOrEmpty(processName))
				MouseHelper.SendRMouseClick(processName);
			else if (sKeys == "{LM}" && !string.IsNullOrEmpty(processName))
				MouseHelper.SendLMouseClick(processName);
			else
				System.Windows.Forms.SendKeys.Send(sKeys);
			SendingKey = false;
		}

		public static void SendDown(params Keys[] keys)
			=> Send(true, false, keys);

		public static void SendUp(params Keys[] keys)
			=> Send(false, true, keys);

		public static void Send(params Keys[] keys)
			=> Send(true, true, keys);

		public static void Send(bool down, bool up, params Keys[] keys)
		{
			if (down)
				foreach (var key in keys)
					keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY, 0);
			if (up)
				foreach (var key in keys)
					keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
		}

	}
}
