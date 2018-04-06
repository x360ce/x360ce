using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using JocysCom.ClassLibrary.Win32;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace JocysCom.ClassLibrary.IO
{

	public partial class DeviceDetector : IDisposable
	{

		private const int ERROR_INSUFFICIENT_BUFFER = 122;
		private const int ERROR_INVALID_DATA = 13;
		private const int ERROR_NO_MORE_ITEMS = 259;

		public const int INVALID_HANDLE_VALUE = -1;
		public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
		public const int DEVICE_NOTIFY_SERVICE_HANDLE = 0x00000001;
		public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004;
		public const int WM_DEVICECHANGE = 0x0219;
		public const int DIF_PROPERTYCHANGE = 0x00000012;
		public const int DIF_REMOVE = 0x00000005;
		public const uint DICS_FLAG_GLOBAL = 0x00000001;
		public const uint DICS_FLAG_CONFIGSPECIFIC = 0x00000002;
		public const uint DICS_ENABLE = 0x00000001;
		public const uint DICS_DISABLE = 0x00000002;
		//public const int MAX_DEVICE_LEN = 1000;
		public const int MAX_DEVICE_ID_LEN = 200;
		private const int ERROR_INVALID_HANDLE_VALUE = -1;
		const int BROADCAST_QUERY_DENY = 0x424D5144;

		/// <summary>
		/// Handle of the window which receives messages from Windows. This will be a form.
		/// </summary>
		IntPtr _RecipientHandle;

		public delegate void DeviceDetectorEventHandler(object sender, DeviceDetectorEventArgs e);

		/// <summary>
		/// Events signalized to the client app.
		/// Add handlers for these events in your form to be notified of removable device events 
		/// </summary>
		public event DeviceDetectorEventHandler DeviceChanged;

		public DeviceDetectorForm DetectorForm;

		/// <summary>
		/// Create hidden form for processing Windows messages about USB drives. You do not need to override WndProc in your form.
		/// </summary>
		public DeviceDetector(bool showForm = false)
		{
			DetectorForm = new DeviceDetectorForm(this);
			if (showForm)
				DetectorForm.Show();
			_RecipientHandle = DetectorForm.Handle;

			//uint DEVICE_NOTIFY_WINDOW_HANDLE = 0;
			//var notificationFilter = new DEV_BROADCAST_DEVICEINTERFACE();
			//notificationFilter.Initialize();
			//// Request to receive notifications about a class of devices.
			//notificationFilter.dbch_devicetype = DBCH_DEVICETYPE.DBT_DEVTYP_DEVICEINTERFACE;
			//// Specify the interface class to receive notifications about.
			//notificationFilter.dbch_classguid = Guid.Empty;
			//// Allocate memory for the buffer that holds the DEV_BROADCAST_DEVICEINTERFACE structure.
			//IntPtr devBroadcastDeviceInterfaceBuffer;
			//devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(notificationFilter.dbch_size);
			//// Copy the DEV_BROADCAST_DEVICEINTERFACE structure to the buffer.
			//// Set fDeleteOld True to prevent memory leaks.
			//Marshal.StructureToPtr(notificationFilter, devBroadcastDeviceInterfaceBuffer, true);
			//NativeMethods.RegisterDeviceNotification(_RecipientHandle, devBroadcastDeviceInterfaceBuffer, DEVICE_NOTIFY_WINDOW_HANDLE);
		}

		/// <summary>
		/// Message handler which must be called from client form. Processes Windows messages and calls event handlers. 
		/// </summary>
		/// <param name="m"></param>
		public void WndProc(ref Message m)
		{
			// Please note that only top-level window of the form will receive a set of default WM_DEVICECHANGE messages
			// when new devices added, become available or removed.
			// You do not need to register to receive these default messages.
			if (m.Msg == WM_DEVICECHANGE)
			{
				object deviceInfo = null;
				DBCH_DEVICETYPE? deviceType = null;
				if (m.LParam != IntPtr.Zero)
				{
					var hdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));
					deviceType = hdr.dbch_devicetype;
					switch (deviceType)
					{
						case DBCH_DEVICETYPE.DBT_DEVTYP_DEVICEINTERFACE:
							var di = (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));
							IntPtr dName = new IntPtr(m.LParam.ToInt64() + Marshal.OffsetOf(typeof(DEV_BROADCAST_DEVICEINTERFACE), "dbcc_name").ToInt64());
							di.dbcc_name = Marshal.PtrToStringAuto(dName).ToCharArray();
							deviceInfo = di;
							break;
						case DBCH_DEVICETYPE.DBT_DEVTYP_HANDLE:
							deviceInfo = (DEV_BROADCAST_HANDLE)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HANDLE));
							break;
						case DBCH_DEVICETYPE.DBT_DEVTYP_OEM:
							deviceInfo = (DEV_BROADCAST_OEM)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_OEM));
							break;
						case DBCH_DEVICETYPE.DBT_DEVTYP_PORT:
							var pi = (DEV_BROADCAST_PORT)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_PORT));
							IntPtr pName = new IntPtr(m.LParam.ToInt64() + Marshal.OffsetOf(typeof(DEV_BROADCAST_PORT), "dbcc_name").ToInt64());
							pi.dbcc_name = Marshal.PtrToStringAuto(pName).ToCharArray();
							deviceInfo = pi;
							break;
						case DBCH_DEVICETYPE.DBT_DEVTYP_VOLUME:
							deviceInfo = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));
							break;
						default:
							break;
					}
				}
				var changeType = (DBT)m.WParam.ToInt32();
				var e = new DeviceDetectorEventArgs(changeType, deviceType, deviceInfo);
				RaiseDeviceChanged(this, e);
				switch (changeType)
				{
					// Device is about to be removed. Any application can cancel the removal.
					case DBT.DBT_DEVICEQUERYREMOVE:
						if (deviceType == DBCH_DEVICETYPE.DBT_DEVTYP_HANDLE)
						{
							// If the client wants to cancel, let Windows know.
							if (e.Cancel)
								m.Result = (IntPtr)BROADCAST_QUERY_DENY;
						}
						break;
				}

			}
		}

		#region Raise Events Asynchronously

		public interface IDelegate<V>
		{
			IAsyncResult BeginInvoke(object sender, V e, AsyncCallback callback, object @object);
		}

		void RaiseDeviceChanged(object sender, DeviceDetectorEventArgs e)
		{
			var ev = DeviceChanged;
			if (ev != null)
			{
				var eventListeners = ev.GetInvocationList();
				for (int i = 0; i < eventListeners.Count(); i++)
				{
					var methodToInvoke = (DeviceDetectorEventHandler)eventListeners[i];
					methodToInvoke.BeginInvoke(sender, e, EndAsyncEvent, null);
				}
			}
		}

		private void EndAsyncEvent(IAsyncResult iar)
		{
			var ar = (System.Runtime.Remoting.Messaging.AsyncResult)iar;
			var invokedMethod = (DeviceDetectorEventHandler)ar.AsyncDelegate;
			try
			{
				// Important note: Whenever you call BeginInvoke you must call the corresponding EndInvoke,
				// otherwise if the invoked method threw an exception or returned a value then
				// the ThreadPool thread will never be released back to the pool, resulting in a thread-leak!
				invokedMethod.EndInvoke(iar);
			}
			catch
			{
				// Handle any exceptions that were thrown by the invoked method
			}
		}

		#endregion

		public static string GetDeviceId(uint deviceInstance)
		{
			var sb = new StringBuilder(MAX_DEVICE_ID_LEN);
			var CRResult = NativeMethods.CM_Get_Device_ID(deviceInstance, sb, sb.Capacity, 0);
			if (CRResult != CR.CR_SUCCESS) throw new Exception("Error calling CM_Get_Device_ID: " + CRResult.ToString());
			return sb.ToString();
		}

		public static string GetDeviceDescription(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
		{
			var deviceDescription = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_DEVICEDESC);
			if (!string.IsNullOrEmpty(deviceDescription)) return deviceDescription.Trim();
			var deviceFriendlyName = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_FRIENDLYNAME);
			return (deviceFriendlyName ?? "").Trim();
		}

		public static string GetDeviceManufacturer(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
		{
			var deviceManufacturer = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_MFG);
			return (deviceManufacturer ?? "").Trim();
		}

		static Regex VidPidRx;

		static string GetVidPidRev(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData, out uint vid, out uint pid, out uint rev)
		{
			VidPidRx = VidPidRx ?? new Regex("(VID|VEN)_(?<vid>[0-9A-F]{4})&PID_(?<pid>[0-9A-F]{4})(&REV_(?<rev>[0-9A-F]{4}))?");
			vid = 0;
			pid = 0;
			rev = 0;
			var value = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_HARDWAREID) ?? "";
			if (string.IsNullOrEmpty(value)) return value;
			var m = VidPidRx.Match(value.ToUpper());
			if (m.Success)
			{
				vid = System.Convert.ToUInt32(m.Groups["vid"].Value, 16);
				pid = System.Convert.ToUInt32(m.Groups["pid"].Value, 16);
				if (!string.IsNullOrEmpty(m.Groups["rev"].Value))
				{
					rev = System.Convert.ToUInt32(m.Groups["rev"].Value, 16);
				}
			}
			return value;
		}

		private static string GetStringPropertyForDevice(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData, SPDRP propId)
		{
			uint proptype;
			uint outsize = 0;
			// Get buffer size.
			var result = NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, (uint)propId, out proptype, null, 0, out outsize);
			if (!result)
			{
				var errorCode = Marshal.GetLastWin32Error();
				if (errorCode == ERROR_INVALID_DATA) return null;
				// We can safely ignore other errors when retrieving buffer size.
			}
			var buffer = new byte[outsize];
			// Get data.
			result = NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, (uint)propId, out proptype, buffer, buffer.Length, out outsize);
			if (!result)
			{
				var errorCode = Marshal.GetLastWin32Error();
				if (errorCode == ERROR_INVALID_DATA) return null;
				var error = new Win32Exception(errorCode);
				throw new Exception("Error calling SetupDiGetDeviceRegistryPropertyW: " + error.ToString());
			}
			var o = "";
			if (outsize > 0)
			{
				var type = (REG)proptype;
				switch (type)
				{
					case REG.REG_SZ:
						o = Encoding.Unicode.GetString(buffer, 0, (int)outsize - 2);
						break;
					case REG.REG_MULTI_SZ:
						o = Encoding.Unicode.GetString(buffer, 0, (int)outsize - 2);
						o = o.Trim('\0').Replace("\0", "\r\n");
						break;
					default:
						o = string.Format("{0} 0x{1}", type, string.Join("", buffer.Take((int)outsize).Select(x => x.ToString("X2"))));
						break;
				}
			}
			return o;
		}

		private static SP_DEVINFO_DATA? GetDeviceInfo(string deviceInstanceId)
		{
			Guid classGuid = System.Guid.Empty;
			IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_ALLCLASSES);
			SP_DEVINFO_DATA? di = null;
			if (deviceInfoSet.ToInt64() != ERROR_INVALID_HANDLE_VALUE)
			{
				di = GetDeviceInfo(deviceInfoSet, deviceInstanceId);
				NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
			}
			return di;
		}

		public static SP_DEVINFO_DATA? GetDeviceInfo(IntPtr deviceInfoSet, string deviceInstanceId)
		{
			var da = new SP_DEVINFO_DATA();
			da.Initialize();
			var result = NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref da);
			if (!result) return null;
			return da;
		}

		public static System.Drawing.Icon GetClassIcon(Guid classGuid)
		{
			IntPtr hIcon;
			int ix;
			System.Drawing.Icon icon = null;
			if (NativeMethods.SetupDiLoadClassIcon(ref classGuid, out hIcon, out ix) != 0)
			{
				icon = System.Drawing.Icon.FromHandle(hIcon);
			}
			return icon;
		}

		public static DeviceInfo[] GetInterfaces(string[] devicePaths = null)
		{
			var list = new List<DeviceInfo>();
			Guid hidGuid = Guid.Empty;
			NativeMethods.HidD_GetHidGuid(ref hidGuid);
			int requiredSize3 = 0;
			List<string> devicePathNames3 = new List<string>();
			var interfaceData = new SP_DEVICE_INTERFACE_DATA();
			interfaceData.Initialize();
			List<string> serials = new List<string>();
			var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_DEVICEINTERFACE);
			for (int i2 = 0; NativeMethods.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, i2, ref interfaceData); i2++)
			{
				var deviceInfoData = new SP_DEVINFO_DATA();
				deviceInfoData.Initialize();
				bool success = NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, ref requiredSize3, IntPtr.Zero);
				IntPtr ptrDetails = Marshal.AllocHGlobal(requiredSize3);
				Marshal.WriteInt32(ptrDetails, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);
				success = NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, ptrDetails, requiredSize3, ref requiredSize3, ref deviceInfoData);
				var interfaceDetail = (SP_DEVICE_INTERFACE_DETAIL_DATA)Marshal.PtrToStructure(ptrDetails, typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));
				var devicePath = interfaceDetail.DevicePath;
				if (devicePaths != null && !devicePaths.Contains(devicePath))
				{
					continue;
				}
				var deviceHandle = deviceInfoData.DevInst;
				var deviceId = GetDeviceId(deviceHandle);
				devicePathNames3.Add(devicePath);
				Marshal.FreeHGlobal(ptrDetails);
				// Open the device as a file so that we can query it with HID and read/write to it.
				var devHandle = NativeMethods.CreateFile(
					interfaceDetail.DevicePath,
					0,
					FileShare.ReadWrite,
					IntPtr.Zero,
					FileMode.Open,
					0, //WinNT.Overlapped
					IntPtr.Zero
				);

				//fileHandle = NativeApi.CreateFile(FilePath, 0, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

				if (devHandle.IsInvalid)
					continue;
				var ha = new HIDD_ATTRIBUTES();
				ha.Size = Marshal.SizeOf(ha);
				var success2 = NativeMethods.HidD_GetAttributes(devHandle, ref ha);
				string serial = "";
				string vendor = "";
				string product = "";
				string phdesc = "";
				if (success2)
				{
					IntPtr preparsedDataPtr = new IntPtr();
					HIDP_CAPS caps = new HIDP_CAPS();
					// Read out the 'pre-parsed data'.
					NativeMethods.HidD_GetPreparsedData(devHandle, ref preparsedDataPtr);
					// feed that to GetCaps.
					NativeMethods.HidP_GetCaps(preparsedDataPtr, ref caps);
					// Free the 'pre-parsed data'.
					NativeMethods.HidD_FreePreparsedData(ref preparsedDataPtr);
					// This could fail if the device was recently attached.
					// Maximum string length is 126 wide characters (2 bytes each) (not including the terminating NULL character).
					var sb = new StringBuilder(256);
					vendor = NativeMethods.HidD_GetManufacturerString(devHandle, sb, sb.Capacity)
						? sb.ToString() : "";
					if (string.IsNullOrEmpty(vendor))
						vendor = GetDeviceManufacturer(deviceInfoSet, deviceInfoData);
					product = NativeMethods.HidD_GetProductString(devHandle, sb, sb.Capacity)
						? sb.ToString() : "";
					if (string.IsNullOrEmpty(product))
						product = GetDeviceDescription(deviceInfoSet, deviceInfoData);
					serial = NativeMethods.HidD_GetSerialNumberString(devHandle, sb, sb.Capacity)
						? sb.ToString() : "";
					phdesc = NativeMethods.HidD_GetPhysicalDescriptor(devHandle, sb, sb.Capacity)
						? sb.ToString() : "";
				}
				uint parentDeviceInstance = 0;
				string parentDeviceId = null;
				var CRResult = NativeMethods.CM_Get_Parent(out parentDeviceInstance, deviceHandle, 0);
				if (CRResult == CR.CR_SUCCESS)
				{
					parentDeviceId = GetDeviceId(parentDeviceInstance);
				}
				var di = new DeviceInfo(deviceId, deviceHandle, parentDeviceId, devicePath, "", vendor, product, hidGuid, "", DeviceNodeStatus.DN_MANUAL, ha.VendorID, ha.ProductID, ha.VersionNumber);
				list.Add(di);
				serials.Add(phdesc);
				devHandle.Close();
			}
			NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
			deviceInfoSet = IntPtr.Zero;
			return list.ToArray();
		}

		public static DeviceInfo[] GetDevices()
		{
			return GetDevices(Guid.Empty, DIGCF.DIGCF_ALLCLASSES);
		}

		static object GetDevicesLock = new object();

		/// <summary>
		/// Get list of devices.
		/// </summary>
		/// <returns>List of devices</returns>
		/// <remarks>
		/// This is code I cobbled together from a number of newsgroup threads
		/// as well as some C++ stuff I translated off of MSDN.  Seems to work.
		/// The idea is to come up with a list of devices, same as the device
		/// manager does.  Currently it uses the actual "system" names for the
		/// hardware.  It is also possible to use hardware IDs.  See the docs
		/// for SetupDiGetDeviceRegistryProperty in the MS SDK for more details.
		/// Errors:   This method may throw the following errors.
		///           Failed to enumerate device tree!
		///           Invalid handle!
		/// </remarks>		
		public static DeviceInfo[] GetDevices(Guid classGuid, DIGCF flags)
		{
			return GetDevices(classGuid, flags, null, 0, 0, 0);
		}

		public static DeviceInfo GetDevice(Guid classGuid, DIGCF flags, string deviceId)
		{
			return GetDevices(classGuid, flags, deviceId, 0, 0, 0).FirstOrDefault();
		}

		public static DeviceInfo[] GetDevices(Guid classGuid, DIGCF flags, string deviceId, int vid, int pid, int rev)
		{
			lock (GetDevicesLock)
			{
				// https://msdn.microsoft.com/en-us/library/windows/hardware/ff541247%28v=vs.85%29.aspx
				//
				// [Device Information Set] 
				//  ├──[Device Information]
				//  │   ├──[Device Node]
				//  │   └──[Device Interface], [Device Interface], ...
				//  ├──[Device Information]
				//  │   ├──[Device Node]
				//  │   └──[Device Interface], [Device Interface], ...
				//
				// Create a device information set composed of all devices associated with a specified device setup class or device interface class.
				IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, flags); //  | DIGCF.DIGCF_PRESENT
				if (deviceInfoSet.ToInt64() == ERROR_INVALID_HANDLE_VALUE)
				{
					throw new Exception("Invalid Handle");
				}
				var list = new List<DeviceInfo>();
				var deviceInfoData = new SP_DEVINFO_DATA();
				deviceInfoData.Initialize();
				int i;
				// Enumerating Device Nodes.
				for (i = 0; NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
				{
					var currentDeviceId = GetDeviceId(deviceInfoData.DevInst);
					if (!string.IsNullOrEmpty(deviceId) && deviceId != currentDeviceId) continue;
					var device = GetDeviceInfo(deviceInfoSet, deviceInfoData, currentDeviceId);
					if (vid > 0 && device.VendorId != vid) continue;
					if (pid > 0 && device.ProductId != pid) continue;
					if (rev > 0 && device.Revision != rev) continue;
					list.Add(device);
				}
				NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
				return list.OrderBy(x => x.ClassDescription).ThenBy(x => x.Description).ToArray();
			}
		}

		static DeviceInfo GetDeviceInfo(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData, string deviceId)
		{
			var deviceName = GetDeviceDescription(deviceInfoSet, deviceInfoData);
			var deviceManufacturer = GetDeviceManufacturer(deviceInfoSet, deviceInfoData);
			var deviceClassGuid = deviceInfoData.ClassGuid;
			var classDescription = GetClassDescription(deviceClassGuid);
			var hardwareId = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_HARDWAREID);
			Win32.DeviceNodeStatus status;
			var deviceHandle = deviceInfoData.DevInst;
			NativeMethods.GetDeviceNodeStatus(deviceHandle, IntPtr.Zero, out status);
			uint vid;
			uint pid;
			uint rev;
			var hwid = GetVidPidRev(deviceInfoSet, deviceInfoData, out vid, out pid, out rev);
			// Get device information.
			uint parentDeviceInstance = 0;
			string parentDeviceId = null;
			var CRResult = NativeMethods.CM_Get_Parent(out parentDeviceInstance, deviceHandle, 0);
			if (CRResult == CR.CR_SUCCESS)
			{
				parentDeviceId = GetDeviceId(parentDeviceInstance);
			}
			var device = new DeviceInfo(deviceId, deviceHandle, parentDeviceId, "", hardwareId, deviceManufacturer, deviceName, deviceClassGuid, classDescription, status, vid, pid, rev);
			return device;
		}






		public static string GetAllDeviceProperties(string deviceId)
		{
			var sb = new StringBuilder();
			Guid classGuid = System.Guid.Empty;
			IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_ALLCLASSES);
			if (deviceInfoSet.ToInt64() != ERROR_INVALID_HANDLE_VALUE)
			{
				var deviceInfoData = GetDeviceInfo(deviceInfoSet, deviceId);
				if (deviceInfoData.HasValue)
				{
					var di = deviceInfoData.Value;
					var props = (SPDRP[])Enum.GetValues(typeof(SPDRP));
					foreach (var item in props)
					{
						if (new[] { SPDRP.SPDRP_UNUSED0, SPDRP.SPDRP_UNUSED1, SPDRP.SPDRP_UNUSED2 }.Contains(item))
						{
							continue;
						}
						try
						{
							var value = GetStringPropertyForDevice(deviceInfoSet, di, item);
							sb.AppendFormat("{0}={1}\r\n", item, value);
						}
						catch (Exception ex)
						{
							sb.AppendFormat("{0}={1}\r\n", item, ex.ToString());
						}
					}
				}
				NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
			}
			return sb.ToString();
		}

		public static DeviceInfo GetParentDevice(Guid classGuid, DIGCF flags, string deviceId)
		{
			lock (GetDevicesLock)
			{
				IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, flags); //  | DIGCF.DIGCF_PRESENT
				if (deviceInfoSet.ToInt64() == ERROR_INVALID_HANDLE_VALUE)
				{
					throw new Exception("Invalid Handle");
				}
				DeviceInfo device = null;
				var deviceInfoData = new SP_DEVINFO_DATA();
				deviceInfoData.Initialize();
				int i;
				for (i = 0; NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
				{
					if (deviceId == GetDeviceId(deviceInfoData.DevInst))
					{
						uint parentDeviceInstance = 0;
						var CRResult = NativeMethods.CM_Get_Parent(out parentDeviceInstance, deviceInfoData.DevInst, 0);
						if (CRResult == CR.CR_NO_SUCH_DEVNODE) break;
						if (CRResult != CR.CR_SUCCESS) break;
						var parentDeviceId = GetDeviceId(parentDeviceInstance);
						device = GetDevice(classGuid, flags, parentDeviceId);
						break;
					}
				}
				NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
				return device;
			}
		}

		public static string GetClassDescription(Guid classGuid)
		{
			StringBuilder deviceClassDescription = new StringBuilder(256);
			UInt32 retLength = 0;
			NativeMethods.SetupDiGetClassDescription(ref classGuid, deviceClassDescription, deviceClassDescription.Capacity, ref retLength);
			return deviceClassDescription.ToString();
		}


		#region Manipulate Devices

		public static bool ScanForHardwareChanges()
		{
			UInt32 devInst = 0;
			CR status;
			var CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
			//var CM_LOCATE_DEVNODE_PHANTOM = 0x00000001;
			//var CM_LOCATE_DEVNODE_CANCELREMOVE = 0x00000002;
			//var CM_LOCATE_DEVNODE_NOVALIDATION = 0x00000004;
			//var CM_LOCATE_DEVNODE_BITS = 0x00000007;
			// Get the root DEV node.
			status = NativeMethods.CM_Locate_DevNode(out devInst, IntPtr.Zero, CM_LOCATE_DEVNODE_NORMAL);
			if (status != CR.CR_SUCCESS)
			{
				return false;
			}
			status = NativeMethods.CM_Reenumerate_DevNode(devInst, 0);
			if (status != CR.CR_SUCCESS)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Set device state.
		/// </summary>
		/// <param name="match"></param>
		/// <param name="enable"></param>
		/// <returns>Success state.</returns>
		/// <remarks>
		/// This is nearly identical to the method above except it
		/// tries to match the hardware description against the criteria
		/// passed in.  If a match is found, that device will the be
		/// enabled or disabled based on bEnable.
		/// Errors:   This method may throw the following exceptions.
		///           Failed to enumerate device tree!
		/// </remarks>
		public static bool SetDeviceState(string deviceId, bool enable)
		{
			try
			{
				Guid classGuid = System.Guid.Empty;
				IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
				if (deviceInfoSet.ToInt32() == INVALID_HANDLE_VALUE)
				{
					return false;
				}
				var deviceInfoData = new SP_DEVINFO_DATA();
				deviceInfoData.Initialize();
				deviceInfoData.Reserved = IntPtr.Zero;
				for (var i = 0; NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
				{
					var currentDeviceId = GetDeviceId(deviceInfoData.DevInst);
					if (deviceId == currentDeviceId)
					{
						SetDeviceState(deviceInfoSet, deviceInfoData, enable);
					}
				}
				NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to enumerate device tree!", ex);
				//return false;
			}
			return true;
		}

		/// <summary>
		/// Attempts to enable or disable a device driver.
		/// </summary>
		/// <param name="deviceInfoSet">Pointer to device.</param>
		/// <param name="deviceInfoData"></param>
		/// <param name="bEnable"></param>
		/// <returns>State of success.</returns>
		/// <remarks>
		/// IMPORTANT NOTE!!!
		/// This code currently does not check the reboot flag.
		/// Some devices require you reboot the OS for the change
		/// to take affect.  If this describes your device, you 
		/// will need to look at the SDK call:
		/// SetupDiGetDeviceInstallParams.  You can call it 
		/// directly after ChangeIt to see whether or not you need 
		/// to reboot the OS for you change to go into effect.
		/// Errors:   This method may throw the following exceptions.
		///           Unable to change device state!
		/// </remarks>
		private static bool SetDeviceState(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData, bool bEnable)
		{
			try
			{
				SP_CLASSINSTALL_HEADER header = new SP_CLASSINSTALL_HEADER();
				header.cbSize = (UInt32)Marshal.SizeOf(header);
				header.InstallFunction = DIF_PROPERTYCHANGE;
				SP_PROPCHANGE_PARAMS classInstallParams = new SP_PROPCHANGE_PARAMS();
				classInstallParams.ClassInstallHeader = header;
				classInstallParams.StateChange = bEnable ? DICS_ENABLE : DICS_DISABLE;
				classInstallParams.Scope = DICS_FLAG_GLOBAL;
				classInstallParams.HwProfile = 0;
				var classInstallParamsSize = (UInt32)Marshal.SizeOf(classInstallParams);
				bool result = NativeMethods.SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, classInstallParams, classInstallParamsSize);
				if (result) result = NativeMethods.SetupDiChangeState(deviceInfoSet, ref deviceInfoData);
				if (!result)
				{
					var ex = new Win32Exception();
					NativeMethods.SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);
				}
				return result;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static Win32Exception RemoveDevice(string deviceId)
		{
			bool needReboot;
			return RemoveDevice(deviceId, 1, out needReboot);
		}

		public static Win32Exception RemoveDevice(string deviceId, int method, out bool needReboot)
		{
			Guid classGuid = System.Guid.Empty;
			IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_ALLCLASSES);
			Win32Exception ex = null;
			needReboot = false;
			var success = false;
			if (deviceInfoSet.ToInt64() != ERROR_INVALID_HANDLE_VALUE)
			{
				var deviceInfoData = GetDeviceInfo(deviceInfoSet, deviceId);
				if (deviceInfoData.HasValue)
				{
					var di = deviceInfoData.Value;
					switch (method)
					{
						case 1:
							SP_CLASSINSTALL_HEADER header = new SP_CLASSINSTALL_HEADER();
							header.cbSize = (UInt32)Marshal.SizeOf(header);
							header.InstallFunction = DIF_REMOVE;
							var classInstallParams = new SP_REMOVEDEVICE_PARAMS();
							classInstallParams.ClassInstallHeader = header;
							classInstallParams.Scope = DICS_FLAG_GLOBAL;
							classInstallParams.HwProfile = 0;
							var classInstallParamsSize = (UInt32)Marshal.SizeOf(classInstallParams);
							success = NativeMethods.SetupDiSetClassInstallParams(deviceInfoSet, ref di, classInstallParams, classInstallParamsSize);
							if (success)
							{
								success = NativeMethods.SetupDiSetSelectedDevice(deviceInfoSet, ref di);
								if (success)
								{
									success = NativeMethods.SetupDiCallClassInstaller(DIF_REMOVE, deviceInfoSet, ref di);
									// ex.ErrorCode = 0xE0000235: SetupDiCallClassInstaller throws ERROR_IN_WOW64 when compiled for 32 bit on a 64 bit machine.
									if (!success) ex = new Win32Exception();
								}
								else ex = new Win32Exception();
							}
							else ex = new Win32Exception();
							break;
						case 2:
							success = NativeMethods.SetupDiRemoveDevice(deviceInfoSet, ref di);
							if (!success) ex = new Win32Exception();
							break;
						case 3:
							success = NativeMethods.DiUninstallDevice(IntPtr.Zero, deviceInfoSet, ref di, 0, out needReboot);
							if (!success) ex = new Win32Exception();
							break;
						default:
							break;
					}
				}
				NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
			}
			else
			{
				ex = new Win32Exception();
			}
			return ex;
		}


		#endregion

		#region IDisposable

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				//NativeMethods.UnregisterDeviceNotification(_RecipientHandle);
				DetectorForm.Dispose();
				DetectorForm = null;
			}
		}

		#endregion

	}
}
