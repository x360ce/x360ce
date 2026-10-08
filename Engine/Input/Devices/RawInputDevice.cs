using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace x360ce.Engine
{
	/// <summary>One control of a HID device as the HID parser describes it: one data index.</summary>
	public class RawInputControl
	{
		/// <summary>The index HidP_GetData reports the control by.</summary>
		public int DataIndex { get; set; }

		/// <summary>True for a button, which is on or off; false for a value.</summary>
		public bool IsButton { get; set; }

		public int UsagePage { get; set; }

		public int Usage { get; set; }

		/// <summary>The link collection that holds the control; 0 is the top-level collection.</summary>
		public int LinkCollection { get; set; }

		/// <summary>The ID of the report that carries the control; 0 when the device numbers no reports.</summary>
		public int ReportId { get; set; }

		/// <summary>The size of the value in bits; 0 for a button.</summary>
		public int BitSize { get; set; }

		/// <summary>The fields the control has in its report. More than one is a usage value array, which HidP_GetData does not return.</summary>
		public int ReportCount { get; set; }

		/// <summary>The smallest value the device declares; 0 for a button.</summary>
		public int LogicalMin { get; set; }

		/// <summary>The largest value the device declares; 0 for a button. Less than <see cref="LogicalMin"/> when the device declares an unsigned range as signed numbers.</summary>
		public int LogicalMax { get; set; }

		/// <summary>The bit the control starts at in its report, the report ID's eight bits first; -1 for a control the HID parser reads.</summary>
		public int BitOffset { get; set; } = -1;
	}

	/// <summary>
	/// Where a device whose HID description names none of its controls carries them in its report: the description it
	/// should have given. Its axes are X, Y, Z, X Rotation, Y Rotation and Z Rotation in that order, then a run of
	/// buttons one bit each, so it is read like any device that describes itself.
	/// </summary>
	public sealed class RawInputDescription
	{
		public int VendorId;
		public int ProductId;
		/// <summary>The usage page of the top-level collection the report comes from, which Raw Input is registered for whole.</summary>
		public int UsagePage;
		public int ReportId;
		/// <summary>The bit each axis starts at, X first; the report ID is bits 0 to 7.</summary>
		public int[] AxisBits;
		/// <summary>The size of each axis in bits, an unsigned value from 0.</summary>
		public int AxisBitSize;
		/// <summary>The bit the first button is at; the others follow it.</summary>
		public int ButtonBit;
		public int ButtonCount;

		/// <summary>The controls as the HID parser would have listed them, each with its place in the report.</summary>
		public RawInputControl[] Controls()
		{
			var controls = new List<RawInputControl>();
			for (var i = 0; i < AxisBits.Length; i++)
				controls.Add(new RawInputControl
				{
					DataIndex = controls.Count,
					UsagePage = 0x01,
					Usage = 0x30 + i,
					ReportId = ReportId,
					ReportCount = 1,
					BitSize = AxisBitSize,
					LogicalMax = (1 << AxisBitSize) - 1,
					BitOffset = AxisBits[i],
				});
			for (var i = 0; i < ButtonCount; i++)
				controls.Add(new RawInputControl
				{
					DataIndex = controls.Count,
					IsButton = true,
					UsagePage = 0x09,
					Usage = i + 1,
					ReportId = ReportId,
					ReportCount = 1,
					BitOffset = ButtonBit + i,
				});
			return controls.ToArray();
		}
	}

	/// <summary>A HID game controller as Raw Input lists it: who it is, and its controls.</summary>
	/// <remarks>
	/// The device owns a copy of its preparsed data, which the HID parser reads every report with, and frees it when
	/// disposed. Nothing else is held open: the device itself is opened only for a moment, to read its product name.
	/// </remarks>
	public sealed class RawInputDevice : IDisposable
	{
		/// <summary>The Generic Desktop page, on which the game controller collections are.</summary>
		const int GenericDesktopPage = 0x01;
		const int JoystickUsage = 0x04;
		const int GamepadUsage = 0x05;
		const int MultiAxisControllerUsage = 0x08;

		/// <summary>
		/// The devices whose HID description names none of their controls, with where their reports carry them. One row
		/// a device: a device that is not here and does not describe itself is not read.
		/// </summary>
		public static readonly RawInputDescription[] Descriptions =
		{
			// Logitech G13: the stick, then 40 key bits, as the Linux G13 driver reads them.
			new RawInputDescription { VendorId = 0x046D, ProductId = 0xC21C, UsagePage = 0xFF00, ReportId = 1, AxisBits = new[] { 8, 16 }, AxisBitSize = 8, ButtonBit = 24, ButtonCount = 40 },
		};

		/// <summary>The row of <see cref="Descriptions"/> for a device with that identity and top-level collection page, or null when it has none.</summary>
		public static RawInputDescription DescriptionOf(int vendorId, int productId, int usagePage)
		{
			foreach (var d in Descriptions)
				if (d.VendorId == vendorId && d.ProductId == productId && d.UsagePage == usagePage)
					return d;
			return null;
		}

		/// <summary>A device described by its preparsed data alone, as recorded from a real one; it has no handle, path or name.</summary>
		/// <param name="description">Where the controls are, for a device whose preparsed data names none; null for one that describes itself.</param>
		/// <returns>The device, or null when the HID parser does not accept the data.</returns>
		public static RawInputDevice FromPreparsedData(byte[] preparsedData, RawInputDescription description = null)
		{
			var device = new RawInputDevice(preparsedData);
			if (device.Controls != null)
			{
				if (description != null)
				{
					device.Description = description;
					device.Controls = description.Controls();
				}
				return device;
			}
			device.Dispose();
			return null;
		}

		/// <summary>Where the device's controls are in its report, when its own description names none; null for a device the HID parser reads.</summary>
		public RawInputDescription Description { get; private set; }

		RawInputDevice(byte[] preparsedData)
		{
			PreparsedDataSize = preparsedData.Length;
			PreparsedData = Marshal.AllocHGlobal(PreparsedDataSize);
			Marshal.Copy(preparsedData, 0, PreparsedData, PreparsedDataSize);
			ReadCapabilities();
		}

		~RawInputDevice()
		{
			Free();
		}

		public void Dispose()
		{
			Free();
			GC.SuppressFinalize(this);
		}

		void Free()
		{
			if (PreparsedData == IntPtr.Zero)
				return;
			Marshal.FreeHGlobal(PreparsedData);
			PreparsedData = IntPtr.Zero;
		}

		#region Identity

		/// <summary>The handle Raw Input gives the device for this session. It changes when the device is reconnected.</summary>
		public IntPtr Handle { get; private set; }

		/// <summary>The device interface path, which DirectInput reports for the same device too.</summary>
		public string InterfacePath { get; private set; }

		public int VendorId { get; private set; }

		public int ProductId { get; private set; }

		public int Version { get; private set; }

		/// <summary>The usage page of the device's top-level collection.</summary>
		public int UsagePage { get; private set; }

		/// <summary>The usage of the device's top-level collection: joystick, gamepad or multi-axis controller, or its vendor's for a device of <see cref="Descriptions"/>.</summary>
		public int Usage { get; private set; }

		/// <summary>The product name the device reports, or null when it reports none.</summary>
		public string ProductName { get; private set; }

		/// <summary>The same for the device on every connection and every start: see <see cref="GetInstanceGuid"/>.</summary>
		public Guid InstanceGuid { get; private set; }

		/// <summary>The product GUID DirectInput gives the same device: see <see cref="GetProductGuid"/>.</summary>
		public Guid ProductGuid { get; private set; }

		/// <summary>An instance GUID made from the device's interface path, so it stays the same across reconnects and restarts.</summary>
		/// <remarks>An MD5 hash of the path in upper case, since Windows reports the same path in either case.</remarks>
		public static Guid GetInstanceGuid(string interfacePath)
		{
			return EngineHelper.ComputeMd5Hash(interfacePath.ToUpperInvariant());
		}

		/// <summary>The product GUID DirectInput gives a HID device: {PPPPVVVV-0000-0000-0000-504944564944}.</summary>
		/// <remarks>The product ID is the high word of the first field and the vendor ID the low; the last six bytes spell "PIDVID".</remarks>
		public static Guid GetProductGuid(int vendorId, int productId)
		{
			var data1 = unchecked((int)(((uint)productId << 16) | ((uint)vendorId & 0xFFFF)));
			return new Guid(data1, 0, 0, 0, 0, (byte)'P', (byte)'I', (byte)'D', (byte)'V', (byte)'I', (byte)'D');
		}

		#endregion

		#region Capabilities

		/// <summary>The device's preparsed data, in memory the device owns; zero once it is disposed.</summary>
		public IntPtr PreparsedData { get; private set; }

		public int PreparsedDataSize { get; private set; }

		/// <summary>The length of an input report in bytes, report ID included.</summary>
		public int InputReportLength { get; private set; }

		/// <summary>The device's input controls, one for each data index and in that order, or null when the HID parser does not accept the preparsed data.</summary>
		/// <remarks>
		/// A range of usages is one control per data index, each with its own usage. Of a set of aliased usages, which
		/// share one data index, the one not flagged as an alias is kept. Nothing is merged or left out: a second hat
		/// switch or a vendor-defined value is a control of its own.
		/// </remarks>
		public RawInputControl[] Controls { get; private set; }

		void ReadCapabilities()
		{
			var caps = new HIDP_CAPS();
			if (NativeMethods.HidP_GetCaps(PreparsedData, ref caps) != RawInputNative.HIDP_STATUS_SUCCESS)
				return;
			InputReportLength = (ushort)caps.InputReportByteLength;
			int valueCount;
			int buttonCount;
			var values = ReadCaps(false, (ushort)caps.NumberInputValueCaps, out valueCount);
			var buttons = ReadCaps(true, (ushort)caps.NumberInputButtonCaps, out buttonCount);
			var length = Math.Max(0, (int)(ushort)caps.NumberInputDataIndices);
			length = Math.Max(length, LastDataIndex(values, valueCount) + 1);
			length = Math.Max(length, LastDataIndex(buttons, buttonCount) + 1);
			var controls = new RawInputControl[length];
			var aliased = new bool[length];
			AddControls(values, valueCount, false, controls, aliased);
			AddControls(buttons, buttonCount, true, controls, aliased);
			Controls = controls;
		}

		/// <summary>HIDP_VALUE_CAPS or HIDP_BUTTON_CAPS entries for input reports, as bytes.</summary>
		byte[] ReadCaps(bool buttons, int count, out int read)
		{
			read = 0;
			if (count == 0)
				return new byte[0];
			var length = (ushort)count;
			var caps = new byte[count * RawInputNative.HIDP_CAPS_Entry_Size];
			var status = buttons
				? RawInputNative.HidP_GetButtonCaps(RawInputNative.HidP_Input, caps, ref length, PreparsedData)
				: RawInputNative.HidP_GetValueCaps(RawInputNative.HidP_Input, caps, ref length, PreparsedData);
			if (status == RawInputNative.HIDP_STATUS_SUCCESS)
				read = Math.Min(length, count);
			return caps;
		}

		// Offsets of the HIDP_VALUE_CAPS and HIDP_BUTTON_CAPS fields used here.
		const int CapUsagePage = 0;
		const int CapReportId = 2;
		const int CapIsAlias = 3;
		const int CapLinkCollection = 6;
		const int CapIsRange = 12;
		const int ButtonCapReportCount = 16;
		const int ValueCapBitSize = 18;
		const int ValueCapReportCount = 20;
		const int ValueCapLogicalMin = 40;
		const int ValueCapLogicalMax = 44;
		const int CapUsageMin = 56;
		const int CapUsageMax = 58;
		const int CapDataIndexMin = 68;
		const int CapDataIndexMax = 70;

		static int LastDataIndex(byte[] caps, int count)
		{
			var last = -1;
			for (var i = 0; i < count; i++)
			{
				var o = i * RawInputNative.HIDP_CAPS_Entry_Size;
				var isRange = caps[o + CapIsRange] != 0;
				last = Math.Max(last, BitConverter.ToUInt16(caps, o + (isRange ? CapDataIndexMax : CapDataIndexMin)));
			}
			return last;
		}

		static void AddControls(byte[] caps, int count, bool buttons, RawInputControl[] controls, bool[] aliased)
		{
			for (var i = 0; i < count; i++)
			{
				var o = i * RawInputNative.HIDP_CAPS_Entry_Size;
				var isRange = caps[o + CapIsRange] != 0;
				var isAlias = caps[o + CapIsAlias] != 0;
				int usageMin = BitConverter.ToUInt16(caps, o + CapUsageMin);
				int usageMax = isRange ? BitConverter.ToUInt16(caps, o + CapUsageMax) : usageMin;
				int indexMin = BitConverter.ToUInt16(caps, o + CapDataIndexMin);
				int indexMax = isRange ? BitConverter.ToUInt16(caps, o + CapDataIndexMax) : indexMin;
				for (var index = indexMin; index <= indexMax; index++)
				{
					if (controls[index] != null && !(aliased[index] && !isAlias))
						continue;
					aliased[index] = isAlias;
					controls[index] = new RawInputControl
					{
						DataIndex = index,
						IsButton = buttons,
						UsagePage = BitConverter.ToUInt16(caps, o + CapUsagePage),
						// A range has as many usages as data indexes; if it has fewer, the last usage repeats.
						Usage = Math.Min(usageMin + index - indexMin, usageMax),
						LinkCollection = BitConverter.ToUInt16(caps, o + CapLinkCollection),
						ReportId = caps[o + CapReportId],
						ReportCount = BitConverter.ToUInt16(caps, o + (buttons ? ButtonCapReportCount : ValueCapReportCount)),
						BitSize = buttons ? 0 : BitConverter.ToUInt16(caps, o + ValueCapBitSize),
						LogicalMin = buttons ? 0 : BitConverter.ToInt32(caps, o + ValueCapLogicalMin),
						LogicalMax = buttons ? 0 : BitConverter.ToInt32(caps, o + ValueCapLogicalMax),
					};
				}
			}
		}

		#endregion

		#region Enumeration

		/// <summary>The HID game controllers attached: devices whose top-level collection is a joystick, a gamepad or a multi-axis controller, and those of <see cref="Descriptions"/>.</summary>
		/// <remarks>Nothing else is listed, keyboards and mice included. The caller disposes the devices.</remarks>
		public static List<RawInputDevice> GetGameControllers()
		{
			var devices = new List<RawInputDevice>();
			foreach (var entry in GetDeviceList())
			{
				if (entry.dwType != RawInputNative.RIM_TYPEHID)
					continue;
				var device = FromHandle(entry.hDevice);
				if (device != null)
					devices.Add(device);
			}
			return devices;
		}

		/// <summary>The HID game controller Raw Input knows by <paramref name="handle"/>, as <see cref="GetGameControllers"/> lists it.</summary>
		/// <returns>The device, or null when the handle is not a HID game controller's or Windows no longer knows it. The caller disposes the device.</returns>
		public static RawInputDevice FromHandle(IntPtr handle)
		{
			var info = GetDeviceInfo(handle);
			if (info == null)
				return null;
			// RID_DEVICE_INFO_HID follows cbSize and dwType.
			var vendorId = BitConverter.ToInt32(info, 8);
			var productId = BitConverter.ToInt32(info, 12);
			var usagePage = BitConverter.ToUInt16(info, 20);
			var usage = BitConverter.ToUInt16(info, 22);
			var description = DescriptionOf(vendorId, productId, usagePage);
			if (description == null && (usagePage != GenericDesktopPage || (usage != JoystickUsage && usage != GamepadUsage && usage != MultiAxisControllerUsage)))
				return null;
			var path = GetDeviceName(handle);
			var preparsed = GetPreparsedData(handle);
			if (string.IsNullOrEmpty(path) || preparsed == null)
				return null;
			var device = FromPreparsedData(preparsed, description);
			if (device == null)
				return null;
			device.Handle = handle;
			device.InterfacePath = path;
			device.VendorId = vendorId;
			device.ProductId = productId;
			device.Version = BitConverter.ToInt32(info, 16);
			device.UsagePage = usagePage;
			device.Usage = usage;
			device.ProductName = GetProductName(path);
			device.InstanceGuid = GetInstanceGuid(path);
			device.ProductGuid = GetProductGuid(device.VendorId, device.ProductId);
			return device;
		}

		static RawInputNative.RAWINPUTDEVICELIST[] GetDeviceList()
		{
			var entrySize = (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTDEVICELIST));
			// A device that arrives between counting and listing makes the list too small, so it is counted again.
			for (var attempt = 0; attempt < 3; attempt++)
			{
				uint count = 0;
				if (RawInputNative.GetRawInputDeviceList(null, ref count, entrySize) == uint.MaxValue || count == 0)
					break;
				var list = new RawInputNative.RAWINPUTDEVICELIST[count];
				var listed = RawInputNative.GetRawInputDeviceList(list, ref count, entrySize);
				if (listed != uint.MaxValue)
				{
					if (listed < list.Length)
						Array.Resize(ref list, (int)listed);
					return list;
				}
				if (Marshal.GetLastWin32Error() != RawInputNative.ERROR_INSUFFICIENT_BUFFER)
					break;
			}
			return new RawInputNative.RAWINPUTDEVICELIST[0];
		}

		/// <summary>The device's RID_DEVICE_INFO as bytes, or null when Windows does not give it.</summary>
		static byte[] GetDeviceInfo(IntPtr handle)
		{
			var info = new byte[RawInputNative.RID_DEVICE_INFO_Size];
			BitConverter.GetBytes(RawInputNative.RID_DEVICE_INFO_Size).CopyTo(info, 0);
			var size = (uint)info.Length;
			var copied = RawInputNative.GetRawInputDeviceInfo(handle, RawInputNative.RIDI_DEVICEINFO, info, ref size);
			if (copied == uint.MaxValue || copied < info.Length || BitConverter.ToUInt32(info, 4) != RawInputNative.RIM_TYPEHID)
				return null;
			return info;
		}

		static string GetDeviceName(IntPtr handle)
		{
			uint chars = 0;
			if (RawInputNative.GetRawInputDeviceInfo(handle, RawInputNative.RIDI_DEVICENAME, null, ref chars) == uint.MaxValue || chars == 0)
				return null;
			var name = new byte[chars * 2];
			var copied = RawInputNative.GetRawInputDeviceInfo(handle, RawInputNative.RIDI_DEVICENAME, name, ref chars);
			if (copied == uint.MaxValue)
				return null;
			var text = Encoding.Unicode.GetString(name);
			var end = text.IndexOf('\0');
			return end < 0 ? text : text.Substring(0, end);
		}

		static byte[] GetPreparsedData(IntPtr handle)
		{
			uint size = 0;
			if (RawInputNative.GetRawInputDeviceInfo(handle, RawInputNative.RIDI_PREPARSEDDATA, null, ref size) == uint.MaxValue || size == 0)
				return null;
			var data = new byte[size];
			var copied = RawInputNative.GetRawInputDeviceInfo(handle, RawInputNative.RIDI_PREPARSEDDATA, data, ref size);
			if (copied == uint.MaxValue || copied != data.Length)
				return null;
			return data;
		}

		/// <summary>The product name the device reports, or null when it reports none.</summary>
		/// <remarks>Opened with no access asked, which needs no rights and does not disturb whoever reads the device.</remarks>
		static string GetProductName(string path)
		{
			using (var handle = NativeMethods.CreateFile(path, 0, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
			{
				if (handle.IsInvalid)
					return null;
				// A USB string holds at most 126 characters.
				var name = new StringBuilder(127);
				if (!NativeMethods.HidD_GetProductString(handle, name, name.Capacity * 2))
					return null;
				var text = name.ToString().Trim();
				return text.Length == 0 ? null : text;
			}
		}

		#endregion
	}
}
