using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.IO;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.App.Issues;
using System.Linq;

namespace Nefarius.ViGEm.Client
{

	[SuppressUnmanagedCodeSecurity]
	partial class ViGEmClient
	{

		public Xbox360Controller[] Targets;
		public Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[] Feedbacks = new Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[4];
		public bool[] connected = new bool[4];

		public bool isControllerExists(uint userIndex)
		{
			// Not properly implemented yet.
			var t = Targets;
			return (t != null && (userIndex - 1) < t.Length && t[userIndex - 1] != null);
		}

		public bool UnPlug(uint i)
		{
			// Not properly implemented yet.
			var t = Targets;
			if (t == null)
				return false;
			try
			{
				t[i - 1].Disconnect();
				connected[i - 1] = false;
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
			}
			return true;
		}

		public bool PlugIn(uint userIndex)
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
					if (!connected[i])
					{
						tempDevices[i] = true;
						t[i].Connect();
					}
				}
				// Connect specified device.
				t[userIndex - 1].Connect();
				connected[userIndex - 1] = true;
				// Disconnect temporary connected devices.
				for (int i = 0; i < 4; i++)
				{
					if (tempDevices[i])
						t[i].Disconnect();
				}
				return true;
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return false;
			}
		}

		public void UnplugAllControllers()
		{
			for (uint i = 1; i <= 4; i++)
			{
				// Unplug device if connected.
				if (IsControllerConnected(i))
					UnPlug(i);
			}
		}

		public bool IsControllerConnected(uint i)
		{
			// Not properly implemented yet.
			return connected[i - 1];
		}

		#region ■ Static Members

		public static ViGEmClient Current;
		public static object ClientLock = new object();
		static VIGEM_ERROR? PendingError;
		static DateTime PendingErrorTime;

		public static void DisposeCurrent()
		{
			lock (ClientLock)
			{
				// If virtual client is initialized then...
				if (Current != null)
					Current.Dispose();
				return;
			}
		}

		static bool? RuntimeInstalled;

		/// <summary>
		/// Check ViGEm client. Create if not exists.
		/// </summary>
		/// <returns></returns>
		public static bool isVBusExists()
		{
			lock (ClientLock)
			{
				// If Visual Studio C++ 2015 Redistributable installation unknown then...
				if (!RuntimeInstalled.HasValue)
				{
					var issue = Environment.Is64BitProcess
						? new CppX64RuntimeInstallIssue()
						: (IssueItem)new CppX86RuntimeInstallIssue();
					issue.Check();
					RuntimeInstalled = issue.Severity == IssueSeverity.None;
				}
				if (!RuntimeInstalled.Value)
					return false;
				// Keep error for 5 seconds.
				if (DateTime.Now.Subtract(PendingErrorTime).TotalSeconds > 5)
					PendingError = null;
				// Do not process until user dealt with the error.
				if (PendingError.HasValue)
					return PendingError.Value == VIGEM_ERROR.VIGEM_ERROR_NONE;
				// If client exists and it was not disposed then...
				if (Current != null && !Current.Disposing && !Current.IsDisposed)
					return true;
				if (!IsLoaded)
					LoadLibrary();
				var client = new ViGEmClient();
				var error = client.Initialize();
				if (error == VIGEM_ERROR.VIGEM_ERROR_NONE)
				{
					PendingError = null;
					Current = client;
				}
				else
				{
					PendingError = error;
					PendingErrorTime = DateTime.Now;
					client.Dispose();
					FreeLibrary();
				}
				return error == VIGEM_ERROR.VIGEM_ERROR_NONE;
			}
		}

		static Exception LastLoadException;

		public static string LibraryName { get { return _LibraryName; } }
		static string _LibraryName;

		static IntPtr libHandle;
		public static bool IsLoaded { get { return libHandle != IntPtr.Zero; } }

		static void LoadLibrary()
		{
			try
			{
				// Extract ViGEm library from Embedded resource.
				var name = "ViGEmClient.dll";
				var chName = x360ce.Engine.EngineHelper.GetResourceChecksumFile(name);
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
						if (count == 0)
							break;
						sw.Write(buffer, 0, count);
					}
					sr.Close();
					sw.Close();
				}
				_LibraryName = fileName;
				// Load library into memory.
				Exception loadException;
				libHandle = JocysCom.ClassLibrary.Win32.NativeMethods.LoadLibrary(_LibraryName, out loadException);
				if (libHandle == IntPtr.Zero)
					LastLoadException = loadException;
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				LastLoadException = ex;
			}
		}

		public static void FreeLibrary()
		{
			if (!IsLoaded)
				return;
			Exception error;
			JocysCom.ClassLibrary.Win32.NativeMethods.FreeLibrary(libHandle, out error);
			libHandle = IntPtr.Zero;
		}

		public static DeviceInfo[] GetVirtualDevices()
		{
			var list = new List<DeviceInfo>();
			var devices = DeviceDetector.GetInterfaces();
			for (int i = 0; i < devices.Length; i++)
			{
				var isVirtual = false;
				var device = devices[i];
				DeviceInfo p = device;
				do
				{
					p = DeviceDetector.GetParentDevice(p.DeviceId);
					if (p != null && VirtualDriverInstaller.ViGEmBusHardwareIds.Any(x => string.Compare(p.HardwareIds, x, true) == 0))
					{
						isVirtual = true;
						break;
					}
				} while (p != null);
				if (isVirtual)
				{
					list.Add(device);
				}
			}
			return list.ToArray();
		}

		#endregion

	}
}
