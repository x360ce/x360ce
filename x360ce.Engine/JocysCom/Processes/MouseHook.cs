using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace JocysCom.ClassLibrary.Processes
{
	public class MouseHook : BaseHook
	{

		/// <summary>
		/// Start Monitoring.
		/// </summary>
		/// <param name="global">False - monitor current application only. True - monitor all.</param>
		public override void Start(bool global = false)
		{
			InstallHook(HookType.WH_MOUSE, global);
		}

		//=====================================================================
		// Other Functions
		//---------------------------------------------------------------------

		public event MouseEventHandler OnMouseMove;
		public event MouseEventHandler OnMouseDown;
		public event MouseEventHandler OnMouseUp;
		public event MouseEventHandler OnMouseActivity;
		public event MouseEventHandler OnMouseWheel;
		public event EventHandler<MouseHookEventArgs> OnMouseHook;
		public event EventHandler<MouseHookEventArgs> OnMouseUnknown;
		public event EventHandler<MouseHookEventArgs> OnCursorChanged;

		// Touch event handlers
		public event EventHandler<MouseTouchEventArgs> OnTouchDown;
		public event EventHandler<MouseTouchEventArgs> OnTouchUp;
		public event EventHandler<MouseTouchEventArgs> OnTouchMove;

		public event UnhandledExceptionEventHandler OnError;

		// https://www.autoitscript.com/autoit3/docs/appendix/WinMsgCodes.htm
		private const int WM_SETCURSOR = 0x0020;

		private const int WM_MOUSEMOVE = 0x200;

		private const int WM_LBUTTONDOWN = 0x201;
		private const int WM_LBUTTONUP = 0x202;
		private const int WM_LBUTTONDBLCLK = 0x203;

		private const int WM_RBUTTONDOWN = 0x204;
		private const int WM_RBUTTONUP = 0x205;
		private const int WM_RBUTTONDBLCLK = 0x206;

		private const int WM_MBUTTONDOWN = 0x207;
		private const int WM_MBUTTONUP = 0x208;
		private const int WM_MBUTTONDBLCLK = 0x209;

		private const int WM_MOUSEWHEEL = 0x20A;

		private const int WM_XBUTTONDOWN = 0x020B;
		private const int WM_XBUTTONUP = 0x020C;
		private const int WM_XBUTTONDBLCLK = 0x020D;

		private const int WM_NCXBUTTONDOWN = 0x00AB;
		private const int WM_NCXBUTTONUP = 0x00AC;
		private const int WM_NCXBUTTONDBLCLK = 0x00AD;

		private const int WM_MOUSEHWHEEL = 0x020E;

		private const int WM_TOUCH = 0x0240;

		private const int WM_GESTURE = 0x0119;
		private const int WM_GESTURENOTIFY = 0x011A;

		// Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
		private const int TOUCHEVENTF_MOVE = 0x0001;
		private const int TOUCHEVENTF_DOWN = 0x0002;
		private const int TOUCHEVENTF_UP = 0x0004;
		private const int TOUCHEVENTF_INRANGE = 0x0008;
		private const int TOUCHEVENTF_PRIMARY = 0x0010;
		private const int TOUCHEVENTF_NOCOALESCE = 0x0020;
		private const int TOUCHEVENTF_PEN = 0x0040;

		/// <summary>Retrieves information about the global cursor.</summary>
		/// <param name="info">A pointer to a CURSORINFO structure that receives the information.</param>
		/// <returns>If the function succeeds, the return value is non-zero.</returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetCursorInfo(out CURSORINFO info);

		/// <summary>
		/// Switch the left and right mouse buttons: SwapMouseButton(1);
		/// Restore the normal mouse button settings: SwapMouseButton(0);
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern int SwapMouseButton(int bSwap);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SetCursorPos(int X, int Y);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetTouchInputInfo(System.IntPtr hTouchInput, int cInputs, [In, Out] TOUCHINPUT[] pInputs, int cbSize);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern void CloseTouchInputHandle(System.IntPtr lParam);

		//---------------------------------------------------------------------

		int scrnX = -1;
		int scrnY = -1;
		int prevX = -1;
		int prevY = -1;

		protected override IntPtr Hook1Procedure(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (EnableEvents)
			{

				// If OK and someone listens to our events
				if (nCode >= 0)
				{
					var info = new CURSORINFO();
					info.Size = Marshal.SizeOf(info.GetType());
					if (!GetCursorInfo(out info))
					{
						var ex = new System.ComponentModel.Win32Exception();
						throw new Exception(ex.Message);
					}
					var button = MouseButtons.None;
					var param = wParam.ToInt32();
					// Marshall the data from callback.
					var mStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
					var delta = 0;
					var tu = new TestUnion(mStruct.mouseData);
					MouseKey mk = 0;
					int lastX = 0;
					int lastY = 0;
					bool handled;
					var unknownAction = false;
					switch (param)
					{
						case WM_MOUSEMOVE:
							var x = mStruct.pt.x;
							var y = mStruct.pt.y;
							if (prevX == -1 || prevY == -1)
							{
								scrnX = SystemInformation.VirtualScreen.Width - 1;
								scrnY = SystemInformation.VirtualScreen.Height - 1;
								prevX = x;
								prevY = y;
							}
							lastX = x - prevX;
							lastY = y - prevY;
							var fX = (int)Math.Max(Math.Min(scrnX, x), 0);
							var fY = (int)Math.Max(Math.Min(scrnY, y), 0);
							if (fX != x || fY != y) SetCursorPos(fX, fY);
							prevX = fX;
							prevY = fY;
							break;
						case WM_LBUTTONDOWN:
						case WM_LBUTTONUP:
						case WM_LBUTTONDBLCLK:
							button = MouseButtons.Left;
							break;
						case WM_MBUTTONDOWN:
						case WM_MBUTTONUP:
						case WM_MBUTTONDBLCLK:
							button = MouseButtons.Middle;
							break;
						case WM_RBUTTONDOWN:
						case WM_RBUTTONUP:
						case WM_RBUTTONDBLCLK:
							button = MouseButtons.Right;
							break;
						case WM_XBUTTONDOWN:
						case WM_XBUTTONUP:
						case WM_XBUTTONDBLCLK:
							button = MouseButtons.XButton1;
							if (tu.High == 1) mk = MouseKey.MK_XBUTTON1;
							if (tu.High == 2) mk = MouseKey.MK_XBUTTON2;
							break;
						case WM_NCXBUTTONDOWN:
						case WM_NCXBUTTONUP:
						case WM_NCXBUTTONDBLCLK:
							button = MouseButtons.XButton2;
							if (tu.High == 1) mk = MouseKey.MK_XBUTTON1;
							if (tu.High == 2) mk = MouseKey.MK_XBUTTON2;
							break;
						case WM_MOUSEWHEEL:
						case WM_MOUSEHWHEEL:
							delta = (int)tu.High;
							mk = (MouseKey)tu.ULow;
							break;
						case WM_TOUCH:
							try
							{
								handled = DecodeTouch(wParam, lParam);
							}
							catch (Exception ex)
							{
								if (OnError != null) OnError(this, new UnhandledExceptionEventArgs(ex, false));
							}
							break;
						default:
							unknownAction = true;

							break;
					}
					var ea = new MouseHookEventArgs(mStruct, info, mk, param, lastX, lastY, null);
					if (OnMouseHook != null) OnMouseHook(this, ea);
					int clickCount = 0;
					if (button != MouseButtons.None) clickCount = (param == WM_LBUTTONDBLCLK || param == WM_RBUTTONDBLCLK) ? 2 : 1;
					var e = new MouseEventArgs(button, clickCount, mStruct.pt.x, mStruct.pt.y, delta);
					// Raise Events.
					if (OnMouseUp != null && (param == WM_LBUTTONUP || param == WM_MBUTTONUP || param == WM_RBUTTONUP || param == WM_XBUTTONUP || param == WM_NCXBUTTONUP)) OnMouseUp(this, e);
					else if (OnMouseDown != null && (param == WM_LBUTTONDOWN || param == WM_MBUTTONDOWN || param == WM_RBUTTONDOWN || param == WM_XBUTTONDOWN || param == WM_NCXBUTTONDOWN)) OnMouseDown(this, e);
					else if (OnMouseMove != null && (param == WM_MOUSEMOVE)) OnMouseMove(this, e);
					else if (OnMouseWheel != null) OnMouseWheel(this, e);
					else if (OnMouseActivity != null) OnMouseActivity(this, e);
					if (unknownAction)
					{
						var ev = OnMouseUnknown;
						if (ev != null)
						{
							ev(this, ea);
						}
					}
				}
			}
			return NativeMethods.CallNextHookEx(hook1handleRef, nCode, wParam, lParam);
		}

		const int OBJID_CURSOR = -9;
		const int CHILDID_SELF = 0;

		protected override void Hook2Procedure(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hwnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime
		)
		{
			if (hwnd == IntPtr.Zero && idObject == OBJID_CURSOR && idChild == CHILDID_SELF)
			{
				if (eventType == EVENT_OBJECT_NAMECHANGE || eventType == EVENT_OBJECT_SHOW)
				{
					Console.WriteLine("Hook2Procedure hwnd = {0:x8}, eventType = {1:x8}, idChild = {2:x8}", hwnd.ToInt32(), eventType, idChild);
					var ev = OnCursorChanged;
					if (ev != null)
					{
						var ci = default(CURSORINFO);
						ci.Size = Marshal.SizeOf(ci);
						var success = GetCursorInfo(out ci);
						Bitmap image = null;
						if (success)
						{
							image = MouseHelper.GetCurrentCursorImage();
						}
						var e = new MouseHookEventArgs(null, ci, 0, 0, 0, 0, image);
						ev(this, e);
					}
				}
			}
		}

		/// <summary>
		/// Extracts lower 16-bit word from an 32-bit int.
		/// </summary>
		private static int LoWord(int number)
		{
			return (number & 0xffff);
		}

		/// <summary>
		/// http://social.msdn.microsoft.com/Forums/en-US/netfxbcl/thread/5c8ff964-c087-43c0-a88d-0ce7719e033c
		/// c:\Program Files\Microsoft SDKs\Windows\v7.1\Samples\Touch\MTScratchpadWMTouch\CS\
		/// </summary>
		private bool DecodeTouch(IntPtr wParam, IntPtr lParam)
		{
			// More than one touch input may be associated with a touch message,
			// so an array is needed to get all event information.
			int inputCount = LoWord(wParam.ToInt32()); // Number of touch inputs, actual per-contact messages
			var inputs = new TOUCHINPUT[inputCount]; // Allocate the storage for the parameters of the per-contact messages
													 // Unpack message parameters into the array of TOUCHINPUT structures, each
													 // representing a message for one single contact.
			var touchInputSize = Marshal.SizeOf(new TOUCHINPUT());
			if (!GetTouchInputInfo(lParam, inputCount, inputs, touchInputSize))
			{
				// Get touch info failed.
				return false;
			}
			// For each contact, dispatch the message to the appropriate message
			// handler.
			bool handled = false; // Boolean, is message handled
			for (int i = 0; i < inputCount; i++)
			{
				TOUCHINPUT ti = inputs[i];

				// Assign a handler to this message.
				EventHandler<MouseTouchEventArgs> handler = null;     // Touch event handler
				if ((ti.dwFlags & TOUCHEVENTF_DOWN) != 0)
				{
					handler = OnTouchDown;
				}
				else if ((ti.dwFlags & TOUCHEVENTF_UP) != 0)
				{
					handler = OnTouchUp;
				}
				else if ((ti.dwFlags & TOUCHEVENTF_MOVE) != 0)
				{
					handler = OnTouchMove;
				}
				// Convert message parameters into touch event arguments and handle the event.
				if (handler != null)
				{
					// Convert the raw touch input message into a touch event.
					var te = new MouseTouchEventArgs(); // Touch event arguments
														// TOUCHINFO point coordinates and contact size is in 1/100 of a pixel; convert it to pixels.
														// Also convert screen to client coordinates.
					te.ContactY = ti.cyContact / 100;
					te.ContactX = ti.cxContact / 100;
					te.Id = ti.dwID;
					{
						te.LocationX = ti.x / 100;
						te.LocationY = ti.y / 100;
					}
					te.Time = ti.dwTime;
					te.Mask = ti.dwMask;
					te.Flags = ti.dwFlags;
					// Invoke the event handler.
					handler(this, te);
					// Mark this event as handled.
					handled = true;
				}
			}
			CloseTouchInputHandle(lParam);
			return handled;
		}
	}

}
