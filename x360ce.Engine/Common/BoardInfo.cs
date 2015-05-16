namespace x360ce.Engine
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Management;
	using System.Runtime.InteropServices;
	using Microsoft.Win32.SafeHandles;
	using System.ComponentModel;

	public class BoardInfo
	{

		static string _DiskId;
		static Guid? _HashedDiskId;
		static object HashedDiskIdLock = new object();
		static object DiskIdLock = new object();

		/// <summary>
		/// Get manufacturer-allocated number used to identify the physical element.
		/// http://msdn.microsoft.com/en-us/library/aa394132%28VS.85%29.aspx
		/// </summary>
		public static string GetDiskId()
		{
			lock (DiskIdLock)
			{
				if (!string.IsNullOrEmpty(_DiskId)) return _DiskId;
				string vendorId = "";
				string productId = "";
				string productRevision = "";
				string serialNumber = "";
				GetDiskDriveId(0, out vendorId, out productId, out productRevision, out serialNumber);
				var values = new List<string>();
				if (!string.IsNullOrEmpty(vendorId)) values.Add(string.Format("VN={0}", vendorId));
				if (!string.IsNullOrEmpty(productId)) values.Add(string.Format("PR={0}", productId));
				if (!string.IsNullOrEmpty(productRevision)) values.Add(string.Format("RV={0}", productRevision));
				if (!string.IsNullOrEmpty(serialNumber)) values.Add(string.Format("SN={0}", serialNumber));
				_DiskId = string.Join(",", values);
			}
			return _DiskId;
		}

		/// <summary>
		/// This anonymous guid will be used as link betwen disk and game settings on online database.
		/// </summary>
		public static Guid GetHashedDiskId()
		{
			lock (HashedDiskIdLock)
			{
				if (_HashedDiskId.HasValue) return _HashedDiskId.Value;
				var diskId = GetDiskId();
				if (string.IsNullOrEmpty(diskId)) return Guid.Empty;
				var serialBytes = System.Text.Encoding.ASCII.GetBytes(diskId);
				var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
				byte[] retVal = md5.ComputeHash(serialBytes);
				_HashedDiskId = new Guid(retVal);
			}
			return _HashedDiskId.Value;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern SafeFileHandle CreateFile(
			string lpFileName,
			UInt32 dwDesiredAccess,
			UInt32 dwShareMode,
			IntPtr lpSecurityAttributes,
			UInt32 dwCreationDisposition,
			UInt32 dwFlagsAndAttributes,
			IntPtr hTemplateFile
			);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool DeviceIoControl(
			SafeFileHandle hDevice,
			uint dwIoControlCode,
			IntPtr lpInBuffer,
			int nInBufferSize,
			IntPtr lpOutBuffer,
			int nOutBufferSize,
			ref UInt32 lpBytesReturned,
			IntPtr lpOverlapped
			);

		public const UInt32 DISK_BASE = 0x00000007;
		public const UInt32 METHOD_BUFFERED = 0;
		public const UInt32 FILE_ANY_ACCESS = 0;

		public const UInt32 GENERIC_READ = 0x80000000;
		public const UInt32 FILE_SHARE_WRITE = 0x2;
		public const UInt32 FILE_SHARE_READ = 0x1;
		public const UInt32 OPEN_EXISTING = 0x3;

		public static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
		{
			return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
		}

		static uint IOCTL_STORAGE_QUERY_PROPERTY = CTL_CODE(IOCTL_STORAGE_BASE, 0x500, METHOD_BUFFERED, FILE_ANY_ACCESS);

		private const uint FILE_DEVICE_MASS_STORAGE = 0x2d;
		private const uint IOCTL_STORAGE_BASE = FILE_DEVICE_MASS_STORAGE;


		[StructLayout(LayoutKind.Sequential)]
		private struct STORAGE_PROPERTY_QUERY
		{
			public Int32 PropertyId;
			public Int32 QueryType;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public byte[] AdditionalParameters;
		}


		public static Exception GetDiskDriveId(int id, out string vendorId, out string productId, out string productRevision, out string serialNumber)
		{
			vendorId = "";
			productId = "";
			productRevision = "";
			serialNumber = "";
			// Open handle to specified physical drive.
			string lpFileName = string.Format(@"\\.\PhysicalDrive{0}", id);
			UInt32 dwDesiredAccess = 0;
			UInt32 dwShareMode = FILE_SHARE_WRITE | FILE_SHARE_READ;
			IntPtr lpSecurityAttributes = default(IntPtr);
			UInt32 dwCreationDisposition = OPEN_EXISTING;
			UInt32 dwFlagsAndAttributes = 0;
			IntPtr hTemplateFile = default(IntPtr);
			Exception error = null;
			var deviceHandle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
			// If open handle failed then...
			if (deviceHandle == null || deviceHandle.IsInvalid)
			{
				error = new Win32Exception(Marshal.GetLastWin32Error());
			}
			else
			{
				// Define and store input parameters for header retrieval.
				STORAGE_PROPERTY_QUERY query = new STORAGE_PROPERTY_QUERY() { PropertyId = 0, QueryType = 0 };
				var inputBufferSize = Marshal.SizeOf(query.GetType());
				var inputBuffer = Marshal.AllocHGlobal(inputBufferSize);
				Marshal.StructureToPtr(query, inputBuffer, true);
				uint ioControlCode = IOCTL_STORAGE_QUERY_PROPERTY;
				// Define and store output parameters.
				var headerBufferSize = Marshal.SizeOf(typeof(STORAGE_DESCRIPTOR_HEADER));
				var headerBuffer = Marshal.AllocHGlobal(headerBufferSize);
				var headerBytesReturned = default(UInt32);
				// Get only header information from the device.
				var success = DeviceIoControl(deviceHandle, ioControlCode, inputBuffer, inputBufferSize, headerBuffer, headerBufferSize, ref headerBytesReturned, IntPtr.Zero);
				if (!success)
				{
					error = new Win32Exception(Marshal.GetLastWin32Error());
				}
				else if (headerBytesReturned != headerBufferSize)
				{
					error = new InvalidOperationException("Bad header structure declaration");
				}
				else
				{
					// Get header data,
					var header = (STORAGE_DESCRIPTOR_HEADER)Marshal.PtrToStructure(headerBuffer, typeof(STORAGE_DESCRIPTOR_HEADER));
					// Define and store intput parameters for full data retrieval.
					var descriptorBufferSize = header.Size;
					var descriptorBufferPointer = Marshal.AllocHGlobal(descriptorBufferSize);
					var descriptorBytesReturned = default(UInt32);
					success = DeviceIoControl(deviceHandle, ioControlCode, inputBuffer, inputBufferSize, descriptorBufferPointer, descriptorBufferSize, ref descriptorBytesReturned, IntPtr.Zero);
					if (!success)
					{
						error = new Win32Exception(Marshal.GetLastWin32Error());
					}
					else if (headerBytesReturned != headerBufferSize)
					{
						error = new InvalidOperationException("Bad descriptor structure declaration");
					}
					else
					{
						var descriptor = (STORAGE_DEVICE_DESCRIPTOR)Marshal.PtrToStructure(descriptorBufferPointer, typeof(STORAGE_DEVICE_DESCRIPTOR));
						byte[] descriptorBuffer = new byte[descriptorBufferSize];
						Marshal.Copy(descriptorBufferPointer, descriptorBuffer, 0, descriptorBuffer.Length);
						vendorId = GetString(descriptorBuffer, descriptor.VendorIdOffset);
						productId = GetString(descriptorBuffer, descriptor.ProductIdOffset);
						productRevision = GetString(descriptorBuffer, descriptor.ProductRevisionOffset);
						serialNumber = GetString(descriptorBuffer, descriptor.SerialNumberOffset);
					}
					// Dispose descriptor buffer.
					Marshal.FreeHGlobal(descriptorBufferPointer);
				}
				// Dispose header buffer.
				Marshal.FreeHGlobal(headerBuffer);
				// Dispose input buffer
				Marshal.FreeHGlobal(inputBuffer);
			}
			// Dispose device handle.
			deviceHandle.Dispose();
			deviceHandle = null;
			return error;
		}

		static string GetString(byte[] bytes, int index, bool reverse = false)
		{
			if (bytes == null || bytes.Length == 0 || index <= 0 || index >= bytes.Length) return "";
			int i;
			for (i = index; i < bytes.Length; i++)
			{
				// Break on first zero byte terminator.
				if (bytes[i] == 0) break;
			}
			if (index == i) return "";
			// Create array to store value.
			var valueBytes = new byte[i - index];
			Array.Copy(bytes, index, valueBytes, 0, valueBytes.Length);
			if (reverse) Array.Reverse(valueBytes);
			return System.Text.Encoding.ASCII.GetString(valueBytes).Trim();
		}


		[StructLayout(LayoutKind.Sequential)]
		private struct STORAGE_DESCRIPTOR_HEADER
		{
			public Int32 Version;
			public Int32 Size;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct STORAGE_DEVICE_DESCRIPTOR
		{
			public Int32 Version;
			public Int32 Size;
			public byte DeviceType;
			public byte DeviceTypeModifier;
			public byte RemovableMedia;
			public byte CommandQueueing;
			public Int32 VendorIdOffset;
			public Int32 ProductIdOffset;
			public Int32 ProductRevisionOffset;
			public Int32 SerialNumberOffset;
			public byte BusType;
			public Int32 RawPropertiesLength;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public byte[] RawDeviceProperties;
		}

	}

}

