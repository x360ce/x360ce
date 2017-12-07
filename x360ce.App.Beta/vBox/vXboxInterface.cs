using SharpDX.XInput;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace x360ce.App.vBox
{
	public class vXboxInterface
	{

		static vXboxInterface()
		{
			var path = Program.GetResourceName("vXboxInterface", "vXboxInterface.dll");
			if (path == null)
				return;
			var assembly = Assembly.GetExecutingAssembly();
			var fullPath = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(path));
			var sr = assembly.GetManifestResourceStream(fullPath);

			if (sr == null)
				return;
			string tempPath = Path.GetTempPath();
			FileStream sw = null;
			var tempFile = Path.Combine(Path.GetTempPath(), "vXboxInterface.dll");
			sw = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
			var buffer = new byte[1024];
			while (true)
			{
				var count = sr.Read(buffer, 0, buffer.Length);
				if (count == 0) break;
				sw.Write(buffer, 0, count);
			}
			sr.Close();
			sw.Close();
			_LibraryName = tempFile;
			LoadLibrary();
			//File.Delete(tempFile);
		}

		private static Exception LastLoadException;

		static string _LibraryName;
		public static string LibraryName { get { return _LibraryName; } }

		internal static IntPtr libHandle;
		public static bool IsLoaded { get { return libHandle != IntPtr.Zero; } }

		static void LoadLibrary()
		{
			try
			{
				Exception loadException;
				libHandle = JocysCom.ClassLibrary.Win32.NativeMethods.LoadLibrary(_LibraryName, out loadException);
				if (libHandle == IntPtr.Zero)
				{
					LastLoadException = loadException;
				}
			}
			catch (Exception ex)
			{
				LastLoadException = ex;
			}
		}

		public static void FreeLibrary()
		{
			if (!IsLoaded) return;
			Exception error;
			JocysCom.ClassLibrary.Win32.NativeMethods.FreeLibrary(libHandle, out error);
			libHandle = IntPtr.Zero;
		}

		// Status
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool isVBusExists();
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool GetNumEmptyBusSlots(string nSlots);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool isControllerExists(uint UserIndex);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool isControllerOwned(uint UserIndex);


		// Virtual device Plug-In/Unplug
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool PlugIn(uint UserIndex);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool UnPlug(uint UserIndex);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool UnPlugForce(uint UserIndex);

		// Data Transfer (Data to the device)
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnA(uint UserIndex, bool Press);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnB(uint UserIndex, bool Press);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnX(uint UserIndex, bool Press);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnY(uint UserIndex, bool Press);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnStart(uint UserIndex, bool Press);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnBack(uint UserIndex, bool Press);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnLT(uint UserIndex, bool Press); // Left Thumb/Stick
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnRT(uint UserIndex, bool Press); // Right Thumb/Stick
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnLB(uint UserIndex, bool Press); // Left Bumper
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetBtnRB(uint UserIndex, bool Press); // Right Bumper
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetTriggerL(uint UserIndex, byte Value); // Left Trigger
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetTriggerR(uint UserIndex, byte Value); // Right Trigger
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetAxisX(uint UserIndex, short Value); // Left Stick X
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetAxisY(uint UserIndex, short Value); // Left Stick Y
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetAxisRx(uint UserIndex, short Value); // Right Stick X
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetAxisRy(uint UserIndex, short Value); // Right Stick Y
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetDpadUp(uint UserIndex);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetDpadRight(uint UserIndex);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetDpadDown(uint UserIndex);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetDpadLeft(uint UserIndex);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool SetDpadOff(uint UserIndex);

		// Data Transfer (Feedback from the device)
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool GetLedNumber(uint UserIndex, byte pLed);
		[DllImport("vXboxInterface.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)] public static extern bool GetVibration(uint UserIndex, Vibration pVib);

	}
}
