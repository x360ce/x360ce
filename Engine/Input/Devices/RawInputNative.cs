using System;
using System.Runtime.InteropServices;
using System.Security;

namespace x360ce.Engine
{
	/// <summary>The Windows functions that list Raw Input devices, describe their controls and read their reports, and the message loop of the window that receives them.</summary>
	/// <remarks>
	/// Structures with a fixed layout are read from byte arrays at the offsets Windows documents, which are the same
	/// in 32-bit and 64-bit processes: RID_DEVICE_INFO is 32 bytes, HIDP_CAPS 64, HIDP_VALUE_CAPS and HIDP_BUTTON_CAPS
	/// 72 each. The structures that hold a handle are declared, so their size follows the process: RAWINPUTDEVICELIST
	/// is 16 bytes in a 64-bit process and 8 in a 32-bit one, RAWINPUTDEVICE 16 and 12, and RAWINPUTHEADER 24 and 16.
	/// HidP_GetCaps and HIDP_CAPS are the ones in <see cref="JocysCom.ClassLibrary.Win32.NativeMethods"/>.
	/// </remarks>
	public static class RawInputNative
	{
		#region Constants

		/// <summary>The device type of a HID device that is neither a keyboard nor a mouse.</summary>
		public const uint RIM_TYPEHID = 2;

		/// <summary>Asks GetRawInputDeviceInfo for the device's preparsed data.</summary>
		public const uint RIDI_PREPARSEDDATA = 0x20000005;

		/// <summary>Asks GetRawInputDeviceInfo for the device's interface path, its size counted in characters.</summary>
		public const uint RIDI_DEVICENAME = 0x20000007;

		/// <summary>Asks GetRawInputDeviceInfo for a RID_DEVICE_INFO, whose cbSize must be set to <see cref="RID_DEVICE_INFO_Size"/> first.</summary>
		public const uint RIDI_DEVICEINFO = 0x2000000b;

		/// <summary>Asks GetRawInputData for the whole RAWINPUT.</summary>
		public const uint RID_INPUT = 0x10000003;

		/// <summary>Registration flag: deliver input while the window is not in the foreground.</summary>
		public const uint RIDEV_INPUTSINK = 0x00000100;

		/// <summary>Registration flag: send WM_INPUT_DEVICE_CHANGE when a device arrives or leaves.</summary>
		public const uint RIDEV_DEVNOTIFY = 0x00002000;

		/// <summary>Registration flag: stop delivering the usage; the window handle must be zero.</summary>
		public const uint RIDEV_REMOVE = 0x00000001;

		/// <summary>The size of RID_DEVICE_INFO in bytes, in 32-bit and 64-bit processes alike.</summary>
		public const int RID_DEVICE_INFO_Size = 32;

		/// <summary>The size of HIDP_VALUE_CAPS and of HIDP_BUTTON_CAPS in bytes.</summary>
		public const int HIDP_CAPS_Entry_Size = 72;

		/// <summary>The status a HidP_ function returns when it succeeds.</summary>
		public const int HIDP_STATUS_SUCCESS = 0x00110000;

		/// <summary>HIDP_REPORT_TYPE: an input report.</summary>
		public const int HidP_Input = 0;

		/// <summary>The error GetRawInputDeviceList and GetRawInputDeviceInfo give when the buffer is too small.</summary>
		public const int ERROR_INSUFFICIENT_BUFFER = 122;

		/// <summary>The size of RAWINPUTHEADER in this process: two 4-byte fields and two handles.</summary>
		public static readonly int RAWINPUTHEADER_Size = 8 + 2 * IntPtr.Size;

		/// <summary>Sent when a device of a registered usage arrives or leaves: wParam is <see cref="GIDC_ARRIVAL"/> or <see cref="GIDC_REMOVAL"/>, lParam the device handle.</summary>
		public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;

		/// <summary>Carries input from a registered device: lParam is the HRAWINPUT GetRawInputData reads.</summary>
		public const int WM_INPUT = 0x00FF;

		/// <summary>WM_INPUT_DEVICE_CHANGE: the device arrived.</summary>
		public const int GIDC_ARRIVAL = 1;

		/// <summary>WM_INPUT_DEVICE_CHANGE: the device left; its handle is no longer valid.</summary>
		public const int GIDC_REMOVAL = 2;

		/// <summary>The first message number a program may use for its own messages to its own windows.</summary>
		public const int WM_APP = 0x8000;

		/// <summary>The parent that makes a window message-only: it is never shown and receives no broadcasts.</summary>
		public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

		#endregion

		#region Structures

		/// <summary>One device in the list GetRawInputDeviceList returns.</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTDEVICELIST
		{
			public IntPtr hDevice;
			public uint dwType;
		}

		/// <summary>One top-level collection a window asks Raw Input to deliver.</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTDEVICE
		{
			public ushort usUsagePage;
			public ushort usUsage;
			public uint dwFlags;
			public IntPtr hwndTarget;
		}

		/// <summary>One control HidP_GetData found in a report: its data index, and its value or, for a button, that it is on.</summary>
		[StructLayout(LayoutKind.Explicit, Size = 8)]
		public struct HIDP_DATA
		{
			[FieldOffset(0)]
			public ushort DataIndex;
			[FieldOffset(2)]
			public ushort Reserved;
			[FieldOffset(4)]
			public uint RawValue;
			[FieldOffset(4)]
			public byte On;
		}

		/// <summary>One message taken from the thread's queue.</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MSG
		{
			public IntPtr hwnd;
			public uint message;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public int ptX;
			public int ptY;
			public uint lPrivate;
		}

		#endregion

		#region user32

		/// <summary>Lists the Raw Input devices attached; with a null list it only counts them.</summary>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetRawInputDeviceList([In, Out] RAWINPUTDEVICELIST[] pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

		/// <summary>Reads one fact about a Raw Input device; with a null buffer it only reports the size needed.</summary>
		[DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, [In, Out] byte[] pData, ref uint pcbSize);

		/// <summary>Copies the input a WM_INPUT message carries; with a null buffer it only reports the size needed.</summary>
		[DllImport("user32.dll"), SuppressUnmanagedCodeSecurity]
		public static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, [In, Out] byte[] pData, ref uint pcbSize, uint cbSizeHeader);

		/// <summary>Asks for, or stops, the delivery of input from the top-level collections named.</summary>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

		#endregion

		#region Message loop

		/// <summary>Waits for the next message of the calling thread's windows.</summary>
		/// <returns>Above 0 for a message, 0 for WM_QUIT, -1 for an error.</returns>
		[DllImport("user32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
		public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

		/// <summary>Hands a message to its window's procedure.</summary>
		[DllImport("user32.dll"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

		/// <summary>Queues a message for a window, from any thread, without waiting for it to be handled.</summary>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>Ends the calling thread's message loop: its next GetMessage returns 0.</summary>
		[DllImport("user32.dll")]
		public static extern void PostQuitMessage(int nExitCode);

		#endregion

		#region hid

		/// <summary>Describes the device's value controls: HIDP_VALUE_CAPS entries of <see cref="HIDP_CAPS_Entry_Size"/> bytes.</summary>
		[DllImport("hid.dll")]
		public static extern int HidP_GetValueCaps(int reportType, [In, Out] byte[] valueCaps, ref ushort valueCapsLength, IntPtr preparsedData);

		/// <summary>Describes the device's buttons: HIDP_BUTTON_CAPS entries of <see cref="HIDP_CAPS_Entry_Size"/> bytes.</summary>
		[DllImport("hid.dll")]
		public static extern int HidP_GetButtonCaps(int reportType, [In, Out] byte[] buttonCaps, ref ushort buttonCapsLength, IntPtr preparsedData);

		/// <summary>Lists the values and the buttons that are on in one report, each by its data index.</summary>
		/// <remarks>The report must be as long as the device's input report. A usage value array has no entry.</remarks>
		[DllImport("hid.dll"), SuppressUnmanagedCodeSecurity]
		public static extern unsafe int HidP_GetData(int reportType, HIDP_DATA* dataList, ref uint dataLength, IntPtr preparsedData, byte* report, uint reportLength);

		/// <summary>The most entries HidP_GetData can return for one report of the type.</summary>
		[DllImport("hid.dll")]
		public static extern uint HidP_MaxDataListLength(int reportType, IntPtr preparsedData);

		#endregion
	}
}
