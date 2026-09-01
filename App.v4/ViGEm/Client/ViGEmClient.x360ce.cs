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

		public ViGEmClient(out VIGEM_ERROR error)
		{
			try
			{
				NativeHandle = NativeMethods.vigem_alloc();
				error = NativeMethods.vigem_connect(NativeHandle);
			}
			catch (DllNotFoundException ex)
			{
				// System.DllNotFoundException:
				// Unable to load DLL 'vigemclient.dll':
				// The specified module could not be found.
				// (Exception from HRESULT: 0x8007007E)
				if (ex.HResult == unchecked((int)0x8007007E))
				{
					// Probably "Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019" is missing.
					// You can find official download links on Microsoft Page:
					// https://support.microsoft.com/en-gb/help/2977003/the-latest-supported-visual-c-downloads
					// Direct links:
					// 32-bit: https://aka.ms/vs/16/release/vc_redist.x86.exe
					// 64-bit: https://aka.ms/vs/16/release/vc_redist.x64.exe
					// You can also find it here:
					// https://visualstudio.microsoft.com/downloads/
					// Under "Other Tools and Frameworks", "Microsoft Visual C++ Redistributable for Visual Studio 2019"
				}
				throw;
			}
			catch (Exception)
			{
				throw;
			}
		}


		/// <summary>Bus numbers this program has put on the bus, connected now or not.</summary>
		/// <remarks>
		/// Asking the bus what is connected right now answers a different question. A controller is
		/// ours from the moment we create it and stays ours while Windows is still taking it away after
		/// we have let go. In the gap between those two moments the bus says no and Windows says yes,
		/// and reading that gap as somebody else's leftover made the program offer to remove the very
		/// controller it had just made.
		///
		/// Putting a controller in a chosen XInput place also means connecting the places below it and
		/// letting them go again, so those brief ones are recorded too. They have the same shape as a
		/// leftover and were reported as one every time emulation was switched on.
		/// </remarks>
		static readonly HashSet<uint> UsedSerialSet = new HashSet<uint>();

		/// <summary>Copy of the bus numbers this program has used.</summary>
		public static uint[] UsedSerials
		{
			get { lock (UsedSerialSet) return UsedSerialSet.ToArray(); }
		}

		/// <summary>Records a controller as ours, while its number can still be read.</summary>
		/// <remarks>
		/// The number comes from the handle the bus gives out, so it is readable only while connected.
		/// Asked for after the controller has gone it answers nothing, which is why it is taken here
		/// rather than when the question is later asked.
		/// </remarks>
		static void RememberSerial(ViGEmTarget target)
		{
			if (target == null)
				return;
			var serial = target.Serial;
			if (serial == 0)
				return;
			lock (UsedSerialSet)
				UsedSerialSet.Add(serial);
		}

		public Xbox360Controller[] Targets;
		public Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[] Feedbacks = new Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[4];
		/// <summary>How many places for controllers of this kind Windows offers.</summary>
		public const int PlaceCount = 4;

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
			if (t == null || !IsValidIndex(i) || i > t.Length)
				return false;
			try
			{
				t[i - 1].Disconnect();
			}
			catch (ViGEmException ex) when (ex.Code == VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN)
			{
				// Being told it is already unplugged is the thing that was wanted, not a failure. The bus
				// drops a controller by itself when the program that made it ends or the driver changes,
				// so asking whether one is still there before letting go answers about a moment that has
				// passed by the time the answer arrives. Guarding the call was the old approach and it
				// still lost that race.
				//
				// Reported as a fault, this filled the support mailbox with wishes already granted:
				// fifteen of twenty-nine reports over two days said nothing else. The placeholder clean-up
				// in PlugIn, below, has always treated it this way.
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				// The bus is asked what happened rather than told; a failed disconnect leaves the
				// controller in whatever state it is really in, and the next pass reads that.
				return false;
			}
			return true;
		}

		public bool PlugIn(uint userIndex)
		{
			var t = Targets;
			if (t == null || !IsValidIndex(userIndex) || userIndex > t.Length)
				return false;
			// In order to assign virtual device at specific XInput position, must connect all devices with lower position first.
			var tempDevices = new bool[PlaceCount];
			try
			{
				for (int i = 0; i < userIndex - 1; i++)
				{
					if (!t[i].IsAttached)
					{
						// Recorded after the fact, not before it. Marked first, a placeholder that
						// failed to connect was still taken away afterwards, and taking away what was
						// never there fails - so a controller that could not be made reported a second
						// fault about the tidying up, and that is the one the person saw.
						t[i].Connect();
						tempDevices[i] = true;
						RememberSerial(t[i]);
					}
				}
				// Connect specified device.
				t[userIndex - 1].Connect();
				RememberSerial(t[userIndex - 1]);
				return true;
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return false;
			}
			finally
			{
				// Disconnect temporary connected devices. Must run when connecting the
				// requested position failed too, or placeholder controllers stay plugged in.
				for (int i = 0; i < tempDevices.Length; i++)
				{
					if (!tempDevices[i])
						continue;
					try
					{
						t[i].Disconnect();
					}
					catch (ViGEmException ex) when (ex.Code == VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN)
					{
						// The point here is that the placeholder is gone. Being told it is already gone
						// is that, not a failure - the bus can drop a controller between making it and
						// tidying it away, and nobody needs a report about a wish already granted.
					}
					catch (Exception ex)
					{
						JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					}
				}
			}
		}

		public void UnplugAllControllers()
		{
			for (uint i = 1; i <= 4; i++)
			{
				// Asked for unconditionally. Whether one is connected is answered about a moment that has
				// passed by the time it is acted on, and letting go of one that is already gone is no longer
				// treated as a fault.
				UnPlug(i);
			}
		}

		/// <summary>Whether the bus is holding the controller in this place.</summary>
		/// <remarks>
		/// This used to answer from a flag the program set when it plugged one in. A flag cannot know
		/// that the controller was taken away afterwards, so it went on saying yes: the tab showed a
		/// green light for a controller that no longer existed, and the update loop skipped making a
		/// new one because it read the same flag and believed there already was one. Two faults, one
		/// cause, and the light was the part that lied.
		/// </remarks>
		public bool IsControllerConnected(uint i)
		{
			if (!IsValidIndex(i))
				return false;
			var t = Targets;
			if (t == null || i - 1 >= t.Length)
				return false;
			var target = t[i - 1];
			return target != null && target.IsAttached;
		}

		/// <summary>Controller positions are 1-4. Index outside the range must not throw.</summary>
		bool IsValidIndex(uint i)
			=> i >= 1 && i <= PlaceCount;

		#region Static Members

		public static ViGEmClient Current;
		public static object ClientLock = new object();
		static VIGEM_ERROR? PendingError;
		static DateTime PendingErrorTime;

		public static void DisposeCurrent()
		{
			lock (ClientLock)
			{
				// If virtual client is initialized then...
				if (Current == null)
					return;
				try
				{
					Current.Dispose();
				}
				catch (Exception ex)
				{
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				}
				// Clear the reference. A disposed instance left here makes every later
				// isVBusExists() call allocate and connect a brand new native client.
				Current = null;
			}
		}

		static bool? RuntimeInstalled;

		/// <summary>
		/// Check ViGEm client. Create if not exists.
		/// </summary>
		/// <returns></returns>
		public static bool isVBusExists(bool createIfMissing = false)
		{
			lock (ClientLock)
			{
				// If Visual Studio C++ 2015 Redistributable installation unknown then...
				if (!RuntimeInstalled.HasValue)
				{
					var issue = Environment.Is64BitProcess
						? (IssueItem)new CppX64RuntimeInstallIssue()
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
				VIGEM_ERROR error;
				if (!IsLoaded)
					LoadLibrary();
				// Without the native library there is nothing to allocate against, and the call
				// below would throw DllNotFoundException on the input thread and take the whole
				// program down. A missing library is reported the same way a missing C++ runtime
				// is: no bus, which the Issues tab already explains and offers to fix.
				if (!IsLoaded)
					return false;
				var client = new ViGEmClient(out error);
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
