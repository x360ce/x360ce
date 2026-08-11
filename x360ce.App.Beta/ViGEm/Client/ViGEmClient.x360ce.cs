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
			if (t == null || i < 1 || i > connected.Length || i > t.Length)
				return false;
			try
			{
				t[i - 1].Disconnect();
				connected[i - 1] = false;
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				connected[i - 1] = false;
				return false;
			}
			return true;
		}

		public bool PlugIn(uint userIndex)
		{
			var t = Targets;
			if (t == null || userIndex < 1 || userIndex > connected.Length || userIndex > t.Length)
				return false;
			var tempDevices = new bool[4];
			try
			{
				// In order to assign virtual device at specific XInput position, must connect all devices with lower position first.
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
				return true;
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				connected[userIndex - 1] = false;
				return false;
			}
			finally
			{
				// Never leave placeholder targets attached when a later connect fails.
				for (int i = 0; i < tempDevices.Length; i++)
				{
					if (!tempDevices[i])
						continue;
					try
					{
						t[i].Disconnect();
					}
					catch (Exception ex)
					{
						JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					}
					connected[i] = false;
				}
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
			return i >= 1 && i <= connected.Length && connected[i - 1];
		}

		#region ■ Static Members

		public static ViGEmClient Current;
		public static object ClientLock = new object();
		static ViGEmBusHealthResult CachedHealth;
		static DateTime CachedHealthTime;
		static string LastLoggedHealth;

		public static void DisposeCurrent()
		{
			lock (ClientLock)
			{
				// If virtual client is initialized then...
				if (Current != null)
				{
					try
					{
						Current.Dispose();
					}
					catch (Exception ex)
					{
						JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					}
					Current = null;
				}
				CachedHealth = null;
				return;
			}
		}

		/// <summary>
		/// Return the most recent staged ViGEm health result.
		/// </summary>
		public static ViGEmBusHealthResult GetBusHealth(bool forceRefresh = false)
		{
			lock (ClientLock)
			{
				if (!forceRefresh && CachedHealth != null &&
					DateTime.UtcNow.Subtract(CachedHealthTime).TotalSeconds < 5)
					return CachedHealth;
			}

			var health = new ViGEmBusHealthDetector(new WindowsViGEmBusProbe()).Detect();
			var healthSignature = $"{health.Installed}|{health.DriverVersion}|{health.ServiceState}|{health.ClientConnectionState}|{health.ErrorMessage}";
			if (!string.Equals(healthSignature, LastLoggedHealth, StringComparison.Ordinal))
				x360ce.App.Diagnostics.OperationalLog.Current?.Write("vigem_health_detected", fields:
				new Dictionary<string, object>
				{
					["installed"] = health.Installed,
					["driverVersion"] = health.DriverVersion,
					["servicePresent"] = health.ServicePresent,
					["serviceState"] = health.ServiceState,
					["clientConnection"] = health.ClientConnectionState,
					["versionIncompatible"] = health.VersionIncompatible,
					["usable"] = health.IsUsable,
					["error"] = health.ErrorMessage,
				});
			lock (ClientLock)
			{
				CachedHealth = health;
				CachedHealthTime = DateTime.UtcNow;
				LastLoggedHealth = healthSignature;
			}
			return health;
		}

		/// <summary>
		/// Return the last probe result without ever performing native driver work.
		/// The high-frequency controller path uses this accessor; issue/UI checks own
		/// refreshes and failed target submissions force a fresh probe.
		/// </summary>
		public static ViGEmBusHealthResult GetCachedBusHealth()
		{
			lock (ClientLock)
			{
				if (CachedHealth != null)
					return CachedHealth;
			}
			return GetBusHealth();
		}

		/// <summary>Compatibility shim for existing callers.</summary>
		public static bool isVBusExists() => GetBusHealth().IsUsable;

		public static ViGEmClientProbeResult ProbeConnection()
		{
			lock (ClientLock)
			{
				if (Current != null && !Current.Disposing && !Current.IsDisposed)
					return new ViGEmClientProbeResult(ViGEmClientConnectionState.Successful);

				if (!IsLoaded)
					LoadLibrary();
				if (!IsLoaded)
					return new ViGEmClientProbeResult(
						ViGEmClientConnectionState.ClientUnavailable,
						LastLoadException?.Message ?? "ViGEm client library could not be loaded.");

				ViGEmClient client = null;
				try
				{
					client = new ViGEmClient();
					var error = client.Initialize();
					if (error == VIGEM_ERROR.VIGEM_ERROR_NONE)
					{
						Current = client;
						return new ViGEmClientProbeResult(ViGEmClientConnectionState.Successful);
					}

					DisposeFailedClient(client);
					return MapConnectionError(error);
				}
				catch (DllNotFoundException ex)
				{
					DisposeFailedClient(client);
					return new ViGEmClientProbeResult(ViGEmClientConnectionState.ClientUnavailable, ex.Message);
				}
				catch (BadImageFormatException ex)
				{
					DisposeFailedClient(client);
					return new ViGEmClientProbeResult(ViGEmClientConnectionState.ClientUnavailable, ex.Message);
				}
				catch (Exception ex)
				{
					DisposeFailedClient(client);
					return new ViGEmClientProbeResult(ViGEmClientConnectionState.Failed, ex.Message);
				}
			}
		}

		static ViGEmClientProbeResult MapConnectionError(VIGEM_ERROR error)
		{
			var state = ViGEmClientConnectionState.Failed;
			switch (error)
			{
				case VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
					state = ViGEmClientConnectionState.BusNotFound;
					break;
				case VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED:
					state = ViGEmClientConnectionState.AccessDenied;
					break;
				case VIGEM_ERROR.VIGEM_ERROR_BUS_VERSION_MISMATCH:
					state = ViGEmClientConnectionState.VersionIncompatible;
					break;
			}
			return new ViGEmClientProbeResult(state, error.ToString());
		}

		static void DisposeFailedClient(ViGEmClient client)
		{
			if (client == null)
				return;
			try
			{
				client.Dispose();
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
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
