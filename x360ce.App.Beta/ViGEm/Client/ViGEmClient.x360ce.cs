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
		public static Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[] Feedbacks = new Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[4];
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

		public static bool isControllerExists(uint userIndex)
		{
			// Not properly implemented yet.
			var t = Targets;
			return (t != null && (userIndex - 1) < t.Length && t[userIndex - 1] != null);
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
				owned[i - 1] = false;
			}
			catch (Exception) { }
			return true;
		}

		public static bool PlugIn(uint userIndex)
		{
			var t = Targets;
			if (t == null)
				return false;
			try
			{
				// In order to assign virtual device at specific XInput position, must connect all devices with lower position first.
				var tempDevices = new bool[4];
				for (int i = 0; i < userIndex - 1; i++)
				{
					if (!owned[i])
					{
						tempDevices[i] = true;
						Targets[i].Connect();
					}
				}
				// Connect specified device.
				Targets[userIndex - 1].Connect();
				owned[userIndex - 1] = true;
				// Disconnect temporary connected devices.
				for (int i = 0; i < 4; i++)
				{
					if (tempDevices[i])
						Targets[i].Disconnect();
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static void UnplugAllControllers()
		{
			// If targets are installed then...
			if (Targets != null)
			{
				for (uint i = 1; i <= 4; i++)
				{
					// Unplug device if owned.
					if (IsControllerOwned(i))
						UnPlug(i);
				}
			}
			return;
		}

		public static bool IsControllerOwned(uint i)
		{
			// Not properly implemented yet.
			return owned[i - 1];
		}


		void Init_x360ce()
		{
			var name = "ViGEmClient.dll";
			var chName = Program.GetResourceChecksumFile(name);
			var fileName = System.IO.Path.Combine(x360ce.Engine.EngineHelper.AppDataPath, "Temp", chName);
			var fi = new FileInfo(fileName);
			if (!fi.Exists)
			{
				if (!fi.Directory.Exists)
					fi.Directory.Create();
				var sr = Program.GetResourceStream(name);
				if (sr == null)
					return;
				FileStream sw = null;
				sw = new FileStream(fileName, FileMode.Create, FileAccess.Write);
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
			_LibraryName = fileName;
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
			UnplugAllControllers();
			Exception error;
			JocysCom.ClassLibrary.Win32.NativeMethods.FreeLibrary(libHandle, out error);
			libHandle = IntPtr.Zero;
		}
	}
}
