using Nefarius.ViGEm.Client.Targets;
using System;
using System.IO;
using System.Security;
using x360ce.App;

namespace Nefarius.ViGEm.Client
{

	[SuppressUnmanagedCodeSecurity]
	partial class ViGEmClient
	{

		public static ViGEmClient Client;
		public static object ClientLock = new object();
		public static Xbox360Controller[] Targets;
		public static bool[] owned = new bool[4];

		public ViGEmClient(out VIGEM_ERROR error)
		{
			Init_x360ce();
			NativeHandle = vigem_alloc();
			error = vigem_connect(NativeHandle);
		}

		static VIGEM_ERROR? PendingError;
		static DateTime PendingErrorTime;

		public static VIGEM_ERROR UpdateClient()
		{
			lock (ClientLock)
			{
				// Keep error for 5 seconds.
				if (DateTime.Now.Subtract(PendingErrorTime).TotalSeconds > 5)
					PendingError = null;
				// Do not process untill user dealed with the error.
				if (PendingError.HasValue)
					return PendingError.Value;
				if (Client != null)
					return VIGEM_ERROR.VIGEM_ERROR_NONE;
				VIGEM_ERROR error;
				var client = new ViGEmClient(out error);
				if (error == VIGEM_ERROR.VIGEM_ERROR_NONE)
				{
					PendingError = null;
					Client = client;
				}
				else
				{
					PendingError = error;
					PendingErrorTime = DateTime.Now;
					client.Dispose();
					Client = null;

				}
				return error;
			}
		}

		public static bool isVBusExists()
		{
			var error = UpdateClient();
			return error == VIGEM_ERROR.VIGEM_ERROR_NONE;
		}

		public static bool isControllerExists(uint i)
		{
			// Not properly implemented yet.
			var t = Targets;
			return (t != null && (i - 1) < t.Length && t[i - 1] != null);
		}

		public static bool UnPlugForce(uint i)
		{
			return UnPlug(i);
		}

		public static bool UnPlug(uint i)
		{
			// Not properly implemented yet.
			var t = Targets;
			if (t == null)
				return false;
			try
			{
				Targets[i - 1].Disconnect();
			}
			catch (Exception) { }
			return true;
		}

		public static bool PlugIn(uint i)
		{
			var t = Targets;
			if (t == null)
				return false;
			try
			{
				Targets[i - 1].Connect();
				owned[i - 1] = true;
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool isControllerOwned(uint i)
		{
			// Not properly implemented yet.
			return owned[i - 1];
		}


		void Init_x360ce()
		{
			var tempFile = Path.Combine(Path.GetTempPath(), "ViGEmClient.dll");
			if (!File.Exists(tempFile))
			{
				var sr = Program.GetResourceStream("ViGEmClient.dll");
				if (sr == null)
					return;
				string tempPath = Path.GetTempPath();
				FileStream sw = null;

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
			}
			_LibraryName = tempFile;
			LoadLibrary();
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
	}
}
