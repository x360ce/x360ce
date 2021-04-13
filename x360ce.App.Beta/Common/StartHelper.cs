using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using System;

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

		private const int WM_WININICHANGE = 0x001A;
		private const int WM_SETTINGCHANGE = WM_WININICHANGE;

		private static System.Timers.Timer _ResumeTimer;

		private static void _ResumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Global.AllowDHelperStart)
				Global.DHelper.Start();
		}

		public static IntPtr CustomWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			_WndProc(msg, wParam);
			return IntPtr.Zero;
		}


		public static void _WndProc(int msg, IntPtr wParam)
		{
			if (msg == WM_SETTINGCHANGE)
			{
				// Must stop all updates or interface will freeze during screen updates.
				Global.DHelper.Stop();
				_ResumeTimer.Stop();
				_ResumeTimer.Start();
			}
			if (msg == DeviceDetector.WM_DEVICECHANGE)
			{
				Global.DHelper.UpdateDevicesEnabled = true;
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
