using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
//using x360ce.App.Common.DInput;

namespace x360ce.App
{
	public static class StartHelper
	{

		/// <summary>Stores the unique windows message id from the RegisterWindowMessage call.</summary>
		private static int _WindowMessage;

		public static event EventHandler OnRestore;
		public static event EventHandler OnClose;

		/// <summary>Used to determine if the application is already open.</summary>
		private static System.Threading.Mutex _Mutex;

		public const int wParam_Restore = 1;
		public const int wParam_Close = 2;
		
		public static string uid;

		public static void Initialize()
		{
			var asm = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
			uid = asm.Product;
			_ResumeTimer = new System.Timers.Timer();
			_ResumeTimer.AutoReset = false;
			_ResumeTimer.Interval = 1000;
			_ResumeTimer.Elapsed += _ResumeTimer_Elapsed;
		}

		public static void Dispose()
		{
			if (_Mutex != null)
				_Mutex.Dispose();
			if (_ResumeTimer != null)
				_ResumeTimer.Dispose();
		}

		/// <summary>
		/// Broadcast message to other instances of this application.
		/// </summary>
		/// <param name="wParam">Send parameter to other instances of this application.</param>
		/// <returns>True - other instances exists; False - other instances doesn't exist.</returns>
		public static bool BroadcastMessage(int wParam)
		{
			// Check for previous instance of this app.
			_Mutex = new System.Threading.Mutex(false, uid);
			// Register the windows message
			_WindowMessage = NativeMethods.RegisterWindowMessage(uid, out var error);
			var firsInstance = _Mutex.WaitOne(1, true);
			// If this is not the first instance then...
			if (!firsInstance)
			{
				// Broadcast a message with parameters to another instance.
				var recipients = (int)BSM.BSM_APPLICATIONS;
				var flags = BSF.BSF_IGNORECURRENTTASK | BSF.BSF_POSTMESSAGE;
				var ret = NativeMethods.BroadcastSystemMessage((int)flags, ref recipients, _WindowMessage, wParam, 0, out error);
			}
			return !firsInstance;
		}

		//======================================================================
		// CustomWndProc(...)
		//=====================================================================

		// ----------------------------------------------------------
		//  Win32
		// ----------------------------------------------------------

		private const int WM_DEVICECHANGE = DeviceDetector.WM_DEVICECHANGE; // 0x0219;
		private const int DBT_DEVICEARRIVAL = 0x8000;
		private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
		private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
		private const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0;

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr RegisterDeviceNotification(
			IntPtr hRecipient,
			ref DEV_BROADCAST_DEVICEINTERFACE notificationFilter,
			uint flags);

		// ----------------------------------------------------------
		//  Structs
		// ----------------------------------------------------------

		[StructLayout(LayoutKind.Sequential)]
		private struct DEV_BROADCAST_HDR
		{
			public uint dbch_size;
			public uint dbch_devicetype;
			public uint dbch_reserved;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct DEV_BROADCAST_DEVICEINTERFACE
		{
			public uint dbcc_size;
			public uint dbcc_devicetype;
			public uint dbcc_reserved;
			public Guid dbcc_classguid;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string dbcc_name; // variable length; 256 is enough here
		}

		// ----------------------------------------------------------
		//  Interface-class GUIDs
		// ----------------------------------------------------------

		internal static readonly Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");
		internal static readonly Guid GUID_DEVINTERFACE_KEYBOARD = new Guid("884B96C3-56EF-11D1-BC8C-00A0C91405DD");
		internal static readonly Guid GUID_DEVINTERFACE_MOUSE = new Guid("378DE44C-56EF-11D1-BC8C-00A0C91405DD");

		private static readonly Guid[] _interestedGuids =
		{
			GUID_DEVINTERFACE_HID,
			GUID_DEVINTERFACE_KEYBOARD,
			GUID_DEVINTERFACE_MOUSE
		};

		// ----------------------------------------------------------
		//  Public API
		// ----------------------------------------------------------

		/// <summary>
		/// Registers the window for HID / keyboard / mouse interface notifications.
		/// </summary>
		internal static void Register(IntPtr hwnd)
		{
			foreach (var g in _interestedGuids)
			{
				var filter = new DEV_BROADCAST_DEVICEINTERFACE
				{
					dbcc_size = (uint)Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE>(),
					dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
					dbcc_classguid = g
				};

				var h = RegisterDeviceNotification(
							hwnd,
							ref filter,
							DEVICE_NOTIFY_WINDOW_HANDLE);

				if (h == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		//========================================================================
		//	CustomWndProc(...)
		//========================================================================

		private const int WM_WININICHANGE = 0x001A;
		private const int WM_SETTINGCHANGE = WM_WININICHANGE;

		private static System.Timers.Timer _ResumeTimer;

		private static void _ResumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Global.AllowDHelperStart)
				Global.DHelper.StartDInputService();
		}

		//------------------------------------------------------------------------

		public static IntPtr CustomWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_SETTINGCHANGE)
			{
				// Must stop all updates or interface will freeze during screen updates.
				Global.DHelper.StopDInputService();
				_ResumeTimer.Stop();
				_ResumeTimer.Start();
			}
			// Device connected or removed.
			else if (msg == WM_DEVICECHANGE)
			{
				HandleDeviceChange(wParam, lParam);
			}
			// If message value was found then...
			else if (msg == _WindowMessage)
			{
				// Show currently running instance.
				if (wParam.ToInt32() == wParam_Restore)
					OnRestore?.Invoke(null, null);
				//  Close currently running instance.
				if (wParam.ToInt32() == wParam_Close)
					OnClose?.Invoke(null, null);
			}
			return IntPtr.Zero;
		}

		//========================================================================
		// Helpers.	
		//========================================================================

		private static void HandleDeviceChange(IntPtr wParam, IntPtr lParam)
		{
			// 1) Only care about arrival or removal.
			var evt = wParam.ToInt32();
			if (evt != DBT_DEVICEARRIVAL && evt != DBT_DEVICEREMOVECOMPLETE)
				return;

			// 2) Only care about device‐interface notifications.
			var hdr = Marshal.PtrToStructure<DEV_BROADCAST_HDR>(lParam);
			if (hdr.dbch_devicetype != DBT_DEVTYP_DEVICEINTERFACE)
				return;

			// 3) Unmarshal the full structure
			var di = Marshal.PtrToStructure<DEV_BROADCAST_DEVICEINTERFACE>(lParam);
			var guid = di.dbcc_classguid;
			var path = di.dbcc_name ?? string.Empty;

			// 4) Only care about specific device interfaces.
			if (!IsKeyboardMouseHID(guid, path))
				return;

			// 5) Write information to debug output.
			Debug.WriteLine(
				$"DEV_BROADCAST_DEVICEINTERFACE: " +
				$"{(evt == DBT_DEVICEARRIVAL ? "Attached" : "Removed")} " +
				$"{DateTime.Now:HH:mm:ss.fff} {guid} {CleanPath(path, guid)} ({GetInterfaceClassName(guid)})");

			// 6) Update devices.
			Global.DHelper.DevicesNeedUpdating = true;
		}

		//-----------------------------------------------------------------------

		private static bool IsKeyboardMouseHID(Guid guid, string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			// All four interfaces must have “&0&0000”
			if (path.IndexOf("&0&0000", StringComparison.OrdinalIgnoreCase) < 0)
				return false;

			if (guid == GUID_DEVINTERFACE_KEYBOARD)
				return path.IndexOf("&MI_00", StringComparison.OrdinalIgnoreCase) >= 0;

			if (guid == GUID_DEVINTERFACE_MOUSE)
				return path.IndexOf("&MI_01", StringComparison.OrdinalIgnoreCase) >= 0;

			if (guid == GUID_DEVINTERFACE_HID)
				return path.IndexOf("&MI_02", StringComparison.OrdinalIgnoreCase) >= 0 ||
						path.IndexOf("&IG_00", StringComparison.OrdinalIgnoreCase) >= 0;

			return false;
		}

		//-----------------------------------------------------------------------

		static string GetInterfaceClassName(Guid g) =>
			g == GUID_DEVINTERFACE_KEYBOARD ? "Keyboard" :
			g == GUID_DEVINTERFACE_MOUSE ? "Mouse" :
			g == GUID_DEVINTERFACE_HID ? "HID" :
			g.ToString();

		//-----------------------------------------------------------------------

		private static string CleanPath(string raw, Guid guid)
		{
			if (string.IsNullOrEmpty(raw))
				return string.Empty;

			return raw
					.Replace(@"\\?\", string.Empty)
					.Replace('#', '\\')
					.Replace('\\' + guid.ToString("B"), "");
		}

		/* Windows Forms:

			/// <summary>
			/// This overrides the windows messaging processing. Be careful with this method,
			/// because this method is responsible for all the windows messages that are coming to the form.
			/// </summary>
			
			protected override void DefWndProc(ref Message m)
			{
				StartHelper._WndProc(m.Msg, m.WParam);
				// Let the normal windows messaging process it.
				base.DefWndProc(ref m);
			}

			WPF:

			protected override void OnSourceInitialized(EventArgs e)
			{
				base.OnSourceInitialized(e);
				var source = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(this);
				source.AddHook(StartHelper.CustomWndProc);
			}

		 */

	}
}
