using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
// using System.Runtime.Serialization.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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
		private const int BROADCAST_QUERY_DENY = 0x424D5144;
		private const int CM_PROB_DISABLED = 22;

		/// <summary>
		/// Handle of the window which receives messages from Windows. This will be a form.
		/// </summary>
		private readonly IntPtr _RecipientHandle;

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
							var dName = new IntPtr(m.LParam.ToInt64() + Marshal.OffsetOf(typeof(DEV_BROADCAST_DEVICEINTERFACE), "dbcc_name").ToInt64());
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
							var pName = new IntPtr(m.LParam.ToInt64() + Marshal.OffsetOf(typeof(DEV_BROADCAST_PORT), "dbcc_name").ToInt64());
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

		public interface IDelegate<Tv>
		{
			IAsyncResult BeginInvoke(object sender, Tv e, AsyncCallback callback, object o);
		}

		private void RaiseDeviceChanged(object sender, DeviceDetectorEventArgs e)
		{
			var ev = DeviceChanged;
			if (ev is null)
				return;
			var eventListeners = ev.GetInvocationList();
			for (var i = 0; i < eventListeners.Length; i++)
			{
				var methodToInvoke = (DeviceDetectorEventHandler)eventListeners[i];
				methodToInvoke.BeginInvoke(sender, e, EndAsyncEvent, null);
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
			if (CRResult != CR.CR_SUCCESS)
				throw new Exception("Error calling CM_Get_Device_ID: " + CRResult.ToString());
			return sb.ToString();
		}

		public static string GetDeviceManufacturer(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
		{
			var deviceManufacturer = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_MFG);
			return (deviceManufacturer ?? "").Trim();
		}

		private static Regex VidPidRx;

		private static string GetVidPidRev(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData, out uint vid, out uint pid, out uint rev)
		{
			VidPidRx = VidPidRx ?? new Regex("(VID|VEN)_(?<vid>[0-9A-F]{4})&PID_(?<pid>[0-9A-F]{4})(&REV_(?<rev>[0-9A-F]{4}))?");
			vid = 0;
			pid = 0;
			rev = 0;
			var value = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_HARDWAREID) ?? "";
			if (string.IsNullOrEmpty(value))
				return value;
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

		private static string GetStringPropertyForDevice(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData, SPDRP propId, System.Collections.IDictionary exData = null)
		{
			// Get buffer size.
			uint propertyRegDataType;
			uint requiredSize;
			var result = NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, (uint)propId, out propertyRegDataType, null, 0, out requiredSize);
			if (!result)
			{
				var errorCode = Marshal.GetLastWin32Error();
				if (errorCode == ERROR_INVALID_DATA)
					return null;
				// We can safely ignore other errors when retrieving buffer size.
			}
			// If no description then return;
			if (requiredSize <= 0)
				return "";
			var buffer = new byte[requiredSize];
			// Get data.
			result = NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, (uint)propId, out propertyRegDataType, buffer, buffer.Length, out requiredSize);
			if (!result)
			{
				var errorCode = Marshal.GetLastWin32Error();
				if (errorCode == ERROR_INVALID_DATA)
					return null;
				var error = new Win32Exception(errorCode);
				var ex = new Exception("Error calling " + nameof(NativeMethods.SetupDiGetDeviceRegistryProperty) + ":" + error.ToString());
				var prefix = nameof(GetStringPropertyForDevice) + ".";
				ex.Data.Add(prefix + nameof(propId), propId);
				ex.Data.Add(prefix + "propertyRegDataType", propertyRegDataType);
				ex.Data.Add(prefix + "requiredSize", requiredSize);
				ex.Data.Add(prefix + "deviceInfoData.ClassGuid", deviceInfoData.ClassGuid);
				if (exData != null)
					foreach (var key in exData.Keys)
						ex.Data.Add(key, exData[key]);
				throw ex;
			}
			var o = "";
			if (requiredSize > 0)
			{
				var type = (REG)propertyRegDataType;
				switch (type)
				{
					case REG.REG_SZ:
						o = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize - 2);
						break;
					case REG.REG_MULTI_SZ:
						o = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize - 2);
						o = o.Trim('\0').Replace("\0", "\r\n");
						break;
					default:
						o = string.Format("{0} 0x{1}", type, string.Join("", buffer.Take((int)requiredSize).Select(x => x.ToString("X2"))));
						break;
				}
			}
			return o;
		}

		private static SP_DEVINFO_DATA? GetDeviceInfo(string deviceInstanceId)
		{
			SP_DEVINFO_DATA? di = null;
			_EnumDeviceInfo(null, null, deviceInstanceId, (infoSet, infoData) =>
			{
				di = infoData;
				return true;
			});
			return di;
		}

		public static SP_DEVINFO_DATA? GetDeviceInfo(string deviceInstanceId, IntPtr deviceInfoSet)
		{
			var di = new SP_DEVINFO_DATA();
			di.Initialize();
			var result = NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref di);
			if (!result)
				return null;
			return di;
		}


		#region Cached class icons

		private static Dictionary<int, Dictionary<Guid, Icon>> _cacheIcons = new Dictionary<int, Dictionary<Guid, Icon>>();

		public static Icon GetClassIcon(Guid classGuid, int size = 16)
		{
			Dictionary<Guid, Icon> dic;
			lock (_cacheIcons)
			{
				if (_cacheIcons.ContainsKey(size))
				{
					dic = _cacheIcons[size];
				}
				else
				{
					dic = new Dictionary<Guid, Icon>();
					_cacheIcons.Add(size, dic);
				}
			}
			lock (dic)
			{
				if (dic.ContainsKey(classGuid))
					return dic[classGuid];
				IntPtr hIcon;
				int index;
				if (NativeMethods.SetupDiLoadClassIcon(ref classGuid, out hIcon, out index) != 0)
				{
					var icon = Icon.FromHandle(hIcon);
					// If size doesn't match then...
					if (icon.Width != size || icon.Height != size)
					{
						// Resize image with high quality.
						var orgImage = icon.ToBitmap();
						var newImage = Drawing.Effects.Resize(orgImage, size, size);
						var thumb = (Bitmap)newImage.GetThumbnailImage(size, size, null, IntPtr.Zero);
						thumb.MakeTransparent();
						icon = Icon.FromHandle(thumb.GetHicon());
					}
					dic.Add(classGuid, icon);
					return icon;
				}
				return null;
			}
		}

		#endregion


		private static readonly object GetDevicesLock = new object();

		/// <summary>
		/// Enumerate devices.
		/// </summary>
		/// <param name="classGuid">Filter devices by class.</param>
		/// <param name="flags">Filter devices by options.</param>
		public static void _EnumDeviceInfo(Guid? classGuid, DIGCF? flags, string deviceInstanceId, Func<IntPtr, SP_DEVINFO_DATA, bool> callback)
		{
			if (!classGuid.HasValue)
				classGuid = Guid.Empty;
			if (!flags.HasValue)
				flags = DIGCF.DIGCF_ALLCLASSES;
			lock (GetDevicesLock)
			{
				// https://docs.microsoft.com/en-gb/windows-hardware/drivers/install/device-information-sets
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
				var infoSet = NativeMethods.SetupDiGetClassDevs(classGuid.Value, IntPtr.Zero, IntPtr.Zero, flags.Value);
				if (infoSet.ToInt64() == ERROR_INVALID_HANDLE_VALUE)
					throw new Exception("Invalid Handle");
				var infoData = new SP_DEVINFO_DATA();
				infoData.Initialize();
				// If one device was requersted then...
				if (!string.IsNullOrEmpty(deviceInstanceId))
				{
					var result = NativeMethods.SetupDiOpenDeviceInfo(infoSet, deviceInstanceId, IntPtr.Zero, 0, ref infoData);
					if (result)
						callback.Invoke(infoSet, infoData);
				}
				else
				{
					// Enumerating Device Nodes.
					for (var i = 0; NativeMethods.SetupDiEnumDeviceInfo(infoSet, i, ref infoData); i++)
						if (!callback.Invoke(infoSet, infoData))
							break;
				}
				NativeMethods.SetupDiDestroyDeviceInfoList(infoSet);
			}
		}

		/// <summary>
		/// Enumerate Interfaces.
		/// </summary>
		static void _EnumDeviceInterfaces(Func<IntPtr, SP_DEVICE_INTERFACE_DATA, bool> callback)
		{
			var hidGuid = Guid.Empty;
			NativeMethods.HidD_GetHidGuid(ref hidGuid);
			lock (GetDevicesLock)
			{
				var infoSet = NativeMethods.SetupDiGetClassDevs(hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_DEVICEINTERFACE);
				if (infoSet.ToInt64() == ERROR_INVALID_HANDLE_VALUE)
					throw new Exception("Invalid Handle");
				var interfaceData = new SP_DEVICE_INTERFACE_DATA();
				interfaceData.Initialize();
				for (var i = 0; NativeMethods.SetupDiEnumDeviceInterfaces(infoSet, IntPtr.Zero, ref hidGuid, i, ref interfaceData); i++)
					if (!callback.Invoke(infoSet, interfaceData))
						break;
				NativeMethods.SetupDiDestroyDeviceInfoList(infoSet);
			}
		}

		// INTERFACES.
		public static DeviceInfo[] GetInterfaces(bool DiDevicesOnly = false)
		{
			var stopwatchInt = Stopwatch.StartNew();

			var list = new List<DeviceInfo>();
			var hidGuid = Guid.Empty;
			NativeMethods.HidD_GetHidGuid(ref hidGuid);
			var requiredSize3 = 0;
			// serialNumbers and physicalDescriptors for debug purposes only.
			var serialNumbers = new List<string>();
			var physicalDescriptors = new List<string>();
			_EnumDeviceInterfaces((deviceInfoSet, interfaceData) =>
			{
				bool success;
				var deviceInfoData = new SP_DEVINFO_DATA();
				deviceInfoData.Initialize();
				// Call 1: Retrieve data size. Note: Returns ERROR_INSUFFICIENT_BUFFER = 122, which is normal.
				success = NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, ref requiredSize3, ref deviceInfoData);

				// Filter devices.
				if (DiDevicesOnly && !PnPDeviceIDs.Contains(GetDeviceId(deviceInfoData.DevInst))) { return true; }

				// Allocate memory for results. 
				var ptrDetails = Marshal.AllocHGlobal(requiredSize3);
				Marshal.WriteInt32(ptrDetails, IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8);
				// Call 2: Retrieve data.
				success = NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, ptrDetails, requiredSize3, ref requiredSize3, ref deviceInfoData);
				var interfaceDetail = (SP_DEVICE_INTERFACE_DETAIL_DATA)Marshal.PtrToStructure(ptrDetails, typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));
				var di = GetDeviceInfo(deviceInfoSet, deviceInfoData);
				di.DevicePath = interfaceDetail.DevicePath;
				Marshal.FreeHGlobal(ptrDetails);
				// Note: Interfaces don't have vendor or product, therefore must get from parent device.
				// Open the device as a file so that we can query it with HID and read/write to it.
				var devHandle = NativeMethods.CreateFile(
					interfaceDetail.DevicePath,
					0,
					FileShare.ReadWrite,
					IntPtr.Zero,
					FileMode.Open,
					0,
					/*WinNT.Overlapped,*/
					IntPtr.Zero);

				if (devHandle.IsInvalid)
					return true;
				// Get vendor product and version from device.
				var ha = new HIDD_ATTRIBUTES();
				ha.Size = Marshal.SizeOf(ha);
				var success2 = NativeMethods.HidD_GetAttributes(devHandle, ref ha);
				di.VendorId = ha.VendorID;
				di.ProductId = ha.ProductID;
				di.Revision = ha.VersionNumber;
				// Get other options.
				if (success2)
				{
					var preparsedDataPtr = new IntPtr();
					var caps = new HIDP_CAPS();
					// Read out the 'pre-parsed data'.
					NativeMethods.HidD_GetPreparsedData(devHandle, ref preparsedDataPtr);
					// feed that to GetCaps.
					NativeMethods.HidP_GetCaps(preparsedDataPtr, ref caps);
					// Free the 'pre-parsed data'.
					NativeMethods.HidD_FreePreparsedData(ref preparsedDataPtr);
					// This could fail if the device was recently attached.
					// Maximum string length is 126 wide characters (2 bytes each) (not including the terminating NULL character).
					var capacity = (uint)(126 * Marshal.SystemDefaultCharSize + 2);
					var sb = new StringBuilder((int)capacity, (int)capacity);
					// Override manufacturer if found.
					if (NativeMethods.HidD_GetManufacturerString(devHandle, sb, sb.Capacity) && sb.Length > 0)
						di.Manufacturer = sb.ToString();
					// Override ProductName if Found.
					if (NativeMethods.HidD_GetProductString(devHandle, sb, sb.Capacity) && sb.Length > 0)
						di.Description = sb.ToString();
					// Get Serial number.
					var serialNumber = NativeMethods.HidD_GetSerialNumberString(devHandle, sb, sb.Capacity)
					? sb.ToString() : "";
					serialNumbers.Add(serialNumber);
					// Get physical descriptor.
					var physicalDescriptor = NativeMethods.HidD_GetPhysicalDescriptor(devHandle, sb, sb.Capacity)
					? sb.ToString() : "";
					physicalDescriptors.Add(physicalDescriptor);
				}
				list.Add(di);
				devHandle.Close();
				return true;
			});

			var listOrdered = list.OrderBy(x => x.DeviceId).ToArray();
			Debug.WriteLine($"");
			if (listOrdered.Count() > 0)
			{
				foreach (var device in listOrdered)
				{
					Debug.WriteLine($"PnPDeviceInterface:" +
						$" InstanceGuid ({PnPDeviceIsInDiDevicesList(device.DeviceId).Item3})." +
						$" ProductId {device.ProductId}." +
						$" Revision {device.Revision}." +
						$" DeviceId {device.DeviceId}." +
						$" InstanceName ({PnPDeviceIsInDiDevicesList(device.DeviceId).Item2})." +
						$" ClassGuid: {device.ClassGuid} ({ContainsGuid(device.ClassGuid).Item2})." +
						$" Description {device.Description}." +
						$" ClassDescription {device.ClassDescription}.");
				}
			}
			else
			{
				Debug.WriteLine($"No PnPDevice.");
			}
				stopwatchInt.Stop();
			Debug.WriteLine($"PnPDeviceInterface: Stopwatch: {stopwatchInt.Elapsed.TotalMilliseconds} ms\n");

			return list.ToArray();
		}

		/// <summary>
		/// Get list of devices.
		/// </summary>
		/// <param name="classGuid">Filter devices by class.</param>
		/// <param name="flags">Filter devices by options.</param>
		/// <param name="deviceId">Filter results by Device ID.</param>
		/// <param name="vid">Filter results by Vendor ID.</param>
		/// <param name="pid">Filter results by Product ID.</param>
		/// <param name="rev">Filter results by Revision ID.</param>
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

		// Connected PnP Device Id list.
		private static List<string> PnPDeviceIDs = new List<string>();
		public static IEnumerable<(
			object DeviceInstance, // (DeviceInstance)DeviceInstance
			object DeviceClass, // (DeviceInstance)DeviceClass
			int Usage,
			string DiDeviceID,
			string ProductName,
			Guid InstanceGuid
		)> DiDevices = null;

		//public static IEnumerable<(DeviceInstance Device, DeviceClass Class, int Usage, string DiDeviceID)> DiDevices = null;

		// DEVICES.

		static (bool, string, Guid) PnPDeviceIsInDiDevicesList(string PnPDeviceID)
		{
			foreach (var item in DiDevices)
			{
				if (PnPDeviceID.StartsWith(item.DiDeviceID, StringComparison.OrdinalIgnoreCase))
				{
					return (true, item.ProductName, item.InstanceGuid);
				}
			}
			return (false, string.Empty, Guid.Empty);
		}

		public static DeviceInfo[] GetDevices(Guid? classGuid = null, DIGCF? flags = null, string parentDeviceId = null, int vid = 0, int pid = 0, int rev = 0, bool DiDevicesOnly = false)
		{
			var stopwatchPnP = Stopwatch.StartNew();
			var list = new List<DeviceInfo>();

			_EnumDeviceInfo(classGuid, flags, null, (infoSet, infoData) =>
			{
				var currentDeviceId = GetDeviceId(infoData.DevInst);

				if (string.IsNullOrEmpty(currentDeviceId))
					return true;

				// If parent device is requested.
				if (!string.IsNullOrEmpty(parentDeviceId))
				{
					if (currentDeviceId == parentDeviceId)
					{
						var device = GetDeviceInfo(infoSet, infoData);
						list.Add(device);
						return true;
					}
					return true;
				}
				// if devices are requested.
				else
				{
					// MI_00 = Keyboard, MI_01 = Mouse, MI_02 = HID.
					if (DiDevicesOnly && !PnPDeviceIsInDiDevicesList(currentDeviceId.ToString()).Item1 || !currentDeviceId.EndsWith("0")) return true;

					var device = GetDeviceInfo(infoSet, infoData);
					if (device.IsRemovable
						|| (vid > 0 && device.VendorId != vid)
						|| (pid > 0 && device.ProductId != pid)
						|| (rev > 0 && device.Revision != rev))
						return true;

					list.Add(device);
					return true;
				}
			});

			var listOrdered = list.OrderBy(x => x.DeviceId).ToArray();
			PnPDeviceIDs.Clear();
			Debug.WriteLine($"\n");

			if (listOrdered.Count() > 0)
			{
				foreach (var device in listOrdered)
				{
					Debug.WriteLine($"PnPDeviceInfo:" +
						$" InstanceGuid ({PnPDeviceIsInDiDevicesList(device.DeviceId).Item3})." +
						$" ProductId {device.ProductId}." +
						$" Revision {device.Revision}." +
						$" DeviceId {device.DeviceId}." +
						$" InstanceName ({PnPDeviceIsInDiDevicesList(device.DeviceId).Item2})." +
						$" ClassGuid: {device.ClassGuid} ({ContainsGuid(device.ClassGuid).Item2})." +
						$" Description {device.Description}." +
						$" ClassDescription {device.ClassDescription}.");
					PnPDeviceIDs.Add(device.DeviceId);
				}
			}
			else
			{
				Debug.WriteLine($"No PnPDevice.");
			}

				stopwatchPnP.Stop();
			Debug.WriteLine($"PnPDeviceInfo: Stopwatch {stopwatchPnP.Elapsed.TotalMilliseconds} ms\n");

			return listOrdered;
		}

		public static Dictionary<Guid, string> PnPDeviceClassGuids = new Dictionary<Guid, string>
		{
			{ DEVCLASS.KEYBOARD, "Keyboard" },
			{ DEVCLASS.MOUSE, "Mouse" },
			{ DEVCLASS.HIDCLASS, "HID" },
		};

		static (bool, string) ContainsGuid(Guid PnPDeviceClassGuid)
		{
			return PnPDeviceClassGuids.TryGetValue(PnPDeviceClassGuid, out string deviceType) ? (true, deviceType) : (false, "NoGuid");
		}

		//public static DeviceInfo[] GetDevices(Guid? classGuid = null, DIGCF? flags = null, string deviceId = null, int vid = 0, int pid = 0, int rev = 0)
		//{
		//	var list = new List<DeviceInfo>();
		//	_EnumDeviceInfo(classGuid, flags, null, (infoSet, infoData) =>
		//	{
		//		var currentDeviceId = GetDeviceId(infoData.DevInst);
		//		if (!string.IsNullOrEmpty(deviceId) && deviceId != currentDeviceId)
		//			return true;
		//		var device = GetDeviceInfo(infoSet, infoData);
		//			if (vid > 0 && device.VendorId != vid)
		//			return true;
		//		if (pid > 0 && device.ProductId != pid)
		//			return true;
		//		if (rev > 0 && device.Revision != rev)
		//			return true;
		//		Debug.WriteLine($"ClassGuidOld {device.ClassGuid}. ProductId {device.ProductId}. HardwareId {device.HardwareIds}. DeviceId {device.DeviceId}. Removable {device.IsRemovable} Name {device.FriendlyName}. Description {device.Description}. ClassDescription {device.ClassDescription} ");
		//		list.Add(device);

		//		return true;
		//	});

		//	return list.OrderBy(x => x.ClassDescription).ThenBy(x => x.Description).ToArray();
		//}

		public static string GetAllDeviceProperties(string deviceId)
		{
			var sb = new StringBuilder();
			_EnumDeviceInfo(null, null, deviceId, (infoSet, infoData) =>
			{
				var props = (SPDRP[])Enum.GetValues(typeof(SPDRP));
				foreach (var item in props)
				{
					if (new[] { SPDRP.SPDRP_UNUSED0, SPDRP.SPDRP_UNUSED1, SPDRP.SPDRP_UNUSED2 }.Contains(item))
						continue;
					try
					{
						var value = GetStringPropertyForDevice(infoSet, infoData, item);
						sb.AppendFormat("{0}={1}\r\n", item, value);
					}
					catch (Exception ex)
					{
						sb.AppendFormat("{0}={1}\r\n", item, ex.ToString());
					}
				}
				return true;
			});
			return sb.ToString();
		}

		public static string GetDeviceDescription(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
		{
			var deviceDescription = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_DEVICEDESC);
			if (!string.IsNullOrEmpty(deviceDescription))
				return deviceDescription;
			var deviceFriendlyName = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_FRIENDLYNAME);
			return deviceDescription ?? "";
		}

		private static DeviceInfo GetDeviceInfo(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
		{
			var di = new DeviceInfo
			{
				DeviceId = GetDeviceId(deviceInfoData.DevInst)
			};
			// Get device status.
			DeviceNodeStatus status;
			NativeMethods.GetDeviceNodeStatus(deviceInfoData.DevInst, IntPtr.Zero, out status);
			di.Status = status;
			var exData = new Dictionary<object, object>();
			exData.Add("GetDeviceNodeStatus", status);
			var deviceDescription = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_DEVICEDESC, exData);
			var deviceFriendlyName = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_FRIENDLYNAME, exData);
			di.Description = deviceDescription ?? deviceFriendlyName ?? "";
			di.FriendlyName = deviceFriendlyName ?? "";
			di.Manufacturer = GetDeviceManufacturer(deviceInfoSet, deviceInfoData);
			di.ClassGuid = deviceInfoData.ClassGuid;
			di.ClassDescription = GetClassDescription(di.ClassGuid);
			di.HardwareIds = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_HARDWAREID, exData);
			// Get device Vendor, Product and Revision ID.
			uint vid;
			uint pid;
			uint rev;
			var hwid = GetVidPidRev(deviceInfoSet, deviceInfoData, out vid, out pid, out rev);
			di.VendorId = vid;
			di.ProductId = pid;
			di.Revision = rev;
			// Get Parent Device.
			uint parentDeviceInstance;
			var CRResult = NativeMethods.CM_Get_Parent(out parentDeviceInstance, deviceInfoData.DevInst, 0);
			if (CRResult == CR.CR_SUCCESS)
				di.ParentDeviceId = GetDeviceId(parentDeviceInstance);
			return di;
		}

		/// <summary>
		/// Fill parent devices. Destination list will contain current device on top.
		/// </summary>
		/// <param name="deviceId">Current device instance id.</param>
		/// <param name="source">List of all devices.</param>
		/// <param name="destination">Destintion list to fill.</param>
		public static void FillParents(DeviceInfo device, IEnumerable<DeviceInfo> source, IList<DeviceInfo> destination)
		{
			// Note: used DeviceInfo as parameter and not deviceId, because source can contain InfoData and DeviceInterface objects with same DeviceId.
			if (destination.Contains(device))
				return;
			destination.Add(device);
			var deviceId = device.ParentDeviceId;
			DeviceInfo di = null;
			while (true)
			{
				di = source.FirstOrDefault(x => x.DeviceId == deviceId);
				if (di is null)
					return;
				if (destination.Contains(di))
					return;
				destination.Add(di);
				deviceId = di.ParentDeviceId;
			}
		}

		public static DeviceInfo GetConnectionDevice(DeviceInfo device, IEnumerable<DeviceInfo> source)
		{
			var parents = new List<DeviceInfo>();
			FillParents(device, source, parents);
			DeviceInfo di;
			for (int i = 0; i < parents.Count; i++)
			{
				di = parents[i];
				if (di.ClassGuid == DEVCLASS.BLUETOOTH)
					return di;
				if (di.ClassGuid == DEVCLASS.USB)
					return di;
				// Probably virtual controller.
				if (di.ClassGuid == DEVCLASS.SYSTEM)
					return di;
			}
			return null;
		}

		public static DeviceInfo GetParentDevice(string deviceId)
		{
			string parentDeviceId = null;
			_EnumDeviceInfo(null, null, deviceId, (infoSet, infoData) =>
			{
				// If current device found then.
				if (GetDeviceId(infoData.DevInst) == deviceId)
				{
					uint parentDeviceInstance;
					var CRResult = NativeMethods.CM_Get_Parent(out parentDeviceInstance, infoData.DevInst, 0);
					if (CRResult == CR.CR_NO_SUCH_DEVNODE)
						return true;
					if (CRResult != CR.CR_SUCCESS)
						return true;
					parentDeviceId = GetDeviceId(parentDeviceInstance);
					return false;
				}
				return true;
			});
			return string.IsNullOrEmpty(parentDeviceId)
				? null : GetDevices(null, null, parentDeviceId).FirstOrDefault();
		}

		public static string GetClassDescription(Guid classGuid)
		{
			var deviceClassDescription = new StringBuilder(256);
			uint retLength = 0;
			NativeMethods.SetupDiGetClassDescription(ref classGuid, deviceClassDescription, deviceClassDescription.Capacity, ref retLength);
			return deviceClassDescription.ToString();
		}


		#region Manipulate Devices

		public static bool ScanForHardwareChanges()
		{
			CR status;
			var CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
			//var CM_LOCATE_DEVNODE_PHANTOM = 0x00000001;
			//var CM_LOCATE_DEVNODE_CANCELREMOVE = 0x00000002;
			//var CM_LOCATE_DEVNODE_NOVALIDATION = 0x00000004;
			//var CM_LOCATE_DEVNODE_BITS = 0x00000007;
			// Get the root DEV node.
			uint devInst;
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
				_EnumDeviceInfo(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT, deviceId, (infoSet, infoData) =>
				{
					var currentDeviceId = GetDeviceId(infoData.DevInst);
					if (deviceId == currentDeviceId)
					{
						SetDeviceState(infoSet, infoData, enable);
						// Job done. Stop.
						return false;
					}
					// Continue.
					return true;
				});
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to enumerate device tree!", ex);
				//return false;
			}
			return true;
		}

		public static bool? IsDeviceDisabled(string deviceId)
		{
			bool? isDisabled = null;
			try
			{
				_EnumDeviceInfo(null, null, deviceId, (infoSet, infoData) =>
				{
					uint status = 0;
					uint problem = 0;
					//after the call 'problem' variable will have the problem code
					var cr = NativeMethods.CM_Get_DevNode_Status(out status, out problem, infoData.DevInst, 0);
					if (cr == CR.CR_SUCCESS)
						isDisabled = problem == CM_PROB_DISABLED;
					return true;
				});
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to enumerate device tree!", ex);
				//return false;
			}
			return isDisabled;
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
				var header = new SP_CLASSINSTALL_HEADER();
				header.cbSize = (uint)Marshal.SizeOf(header);
				header.InstallFunction = DIF_PROPERTYCHANGE;
				var classInstallParams = new SP_PROPCHANGE_PARAMS
				{
					ClassInstallHeader = header,
					StateChange = bEnable ? DICS_ENABLE : DICS_DISABLE,
					Scope = DICS_FLAG_GLOBAL,
					HwProfile = 0
				};
				var classInstallParamsSize = (uint)Marshal.SizeOf(classInstallParams);
				var result = NativeMethods.SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, classInstallParams, classInstallParamsSize);
				if (result)
					result = NativeMethods.SetupDiChangeState(deviceInfoSet, ref deviceInfoData);
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

		public static Exception RemoveDevice(string deviceId)
		{
			bool needReboot;
			return RemoveDevice(deviceId, 1, out needReboot);
		}

		public static Exception RemoveDevice(string deviceId, int method, out bool needReboot)
		{
			Exception ex = null;
			needReboot = false;
			bool needRebootAny = false;
			var success = false;
			try
			{
				_EnumDeviceInfo(null, null, deviceId, (infoSet, infoData) =>
				{
					switch (method)
					{
						case 1:
							var header = new SP_CLASSINSTALL_HEADER();
							header.cbSize = (uint)Marshal.SizeOf(header);
							header.InstallFunction = DIF_REMOVE;
							var classInstallParams = new SP_REMOVEDEVICE_PARAMS
							{
								ClassInstallHeader = header,
								Scope = DICS_FLAG_GLOBAL,
								HwProfile = 0
							};
							var classInstallParamsSize = (uint)Marshal.SizeOf(classInstallParams);
							success = NativeMethods.SetupDiSetClassInstallParams(infoSet, ref infoData, classInstallParams, classInstallParamsSize);
							if (success)
							{
								success = NativeMethods.SetupDiSetSelectedDevice(infoSet, ref infoData);
								if (success)
								{
									success = NativeMethods.SetupDiCallClassInstaller(DIF_REMOVE, infoSet, ref infoData);
									// ex.ErrorCode = 0xE0000235: SetupDiCallClassInstaller throws ERROR_IN_WOW64 when compiled for 32 bit on a 64 bit machine.
									// Most of the SetupDi APIs run fine in a WOW64 process, but co-installer have to run from 64-bit process.
									if (!success)
										ex = new Win32Exception();
								}
								else
									ex = new Win32Exception();
							}
							else
								ex = new Win32Exception();
							break;
						case 2:
							success = NativeMethods.SetupDiRemoveDevice(infoSet, ref infoData);
							if (!success)
								ex = new Win32Exception();
							break;
						case 3:
							bool _needReboot;
							success = NativeMethods.DiUninstallDevice(IntPtr.Zero, infoSet, ref infoData, 0, out _needReboot);
							needRebootAny |= _needReboot;
							if (!success)
								ex = new Win32Exception();
							break;
						default:
							break;
					}
					return true;
				});
				needReboot = needRebootAny;
			}
			catch (Exception ex2)
			{
				ex = ex2;
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
