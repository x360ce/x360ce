using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Runtime.InteropServices;

namespace x360ce.Engine
{
	/// <summary>A DirectInput device: who it is, the SharpDX device it is read and driven through, and the changes DirectInput keeps for it between reads.</summary>
	/// <remarks>
	/// The engine reads the device's state, then the changes DirectInput kept since the read before
	/// (<see cref="ReadChanges"/>), so a change that came and went between two passes is still seen. Force feedback is
	/// driven through <see cref="Joystick"/>. The input thread reads the device, and a read makes nothing after the first.
	/// </remarks>
	public sealed class DirectInputDevice : IInputSourceDevice, IDisposable
	{
		/// <summary>How many changes DirectInput keeps for the device between two reads; more are dropped, which only a pass held up for a long while would see.</summary>
		public const int BufferSize = 256;

		/// <summary>IDirectInputDevice8::GetDeviceData, called through the device's own table: SharpDX's GetBufferedData makes a new array on every call.</summary>
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate int GetDeviceDataMethod(IntPtr device, int objectDataSize, IntPtr objectData, ref int count, int flags);

		/// <summary>The place of GetDeviceData in the table of IDirectInputDevice8: after IUnknown's three methods, GetCapabilities, EnumObjects, GetProperty, SetProperty, Acquire, Unacquire and GetDeviceState.</summary>
		const int GetDeviceDataSlot = 10;

		/// <summary>The size of DIDEVICEOBJECTDATA: four DWORDs and a UINT_PTR, aligned to 8 bytes in a 64-bit process.</summary>
		static readonly int ObjectDataSize = IntPtr.Size == 8 ? 24 : 20;

		/// <summary>Opens the device DirectInput lists under <paramref name="instanceGuid"/>, and reads who it is.</summary>
		/// <exception cref="SharpDXException">DirectInput could not open the device or answer for it, such as one gone since it was listed.</exception>
		public DirectInputDevice(DirectInput manager, Guid instanceGuid)
		{
			Joystick = new Joystick(manager, instanceGuid);
			var information = Joystick.Information;
			InstanceGuid = information.InstanceGuid;
			ProductGuid = information.ProductGuid;
			ProductName = information.ProductName;
			InterfacePath = information.IsHumanInterfaceDevice ? Joystick.Properties.InterfacePath ?? "" : "";
			// A HID device's product GUID carries its numbers: the product ID in the high word, the vendor ID in the low, and "PIDVID" in the last six bytes.
			var bytes = ProductGuid.ToByteArray();
			if (bytes[10] == 'P' && bytes[11] == 'I' && bytes[12] == 'D' && bytes[13] == 'V' && bytes[14] == 'I' && bytes[15] == 'D')
			{
				var data1 = BitConverter.ToInt32(bytes, 0);
				VendorId = data1 & 0xFFFF;
				ProductId = (data1 >> 16) & 0xFFFF;
			}
		}

		/// <summary>The SharpDX device, which the engine acquires, reads and drives force feedback through.</summary>
		public Joystick Joystick { get; }

		#region Identity

		public InputSourceType Source { get { return InputSourceType.DirectInput; } }

		public Guid InstanceGuid { get; }

		public Guid ProductGuid { get; }

		/// <summary>The device interface path; empty for a device that is not HID.</summary>
		public string InterfacePath { get; }

		public string ProductName { get; }

		/// <summary>The vendor ID from the product GUID; 0 for a device that is not HID.</summary>
		public int VendorId { get; }

		/// <summary>The product ID from the product GUID; 0 for a device that is not HID.</summary>
		public int ProductId { get; }

		#endregion

		#region Kept changes

		/// <summary>What DirectInput kept for the device since the read before, as <see cref="ReadChanges"/> left it.</summary>
		public DirectInputChanges Changes { get; } = new DirectInputChanges();

		GetDeviceDataMethod getDeviceData;
		IntPtr getDeviceDataPointer;

		/// <summary>The entries GetDeviceData fills, pinned only for the call, so a device let go of takes it with it.</summary>
		readonly byte[] data = new byte[BufferSize * ObjectDataSize];

		/// <summary>Asks DirectInput to keep the device's changes between reads. Called while the device is let go of, which is when DirectInput takes it.</summary>
		/// <remarks>A device that refuses is read by its state alone.</remarks>
		public void KeepChanges()
		{
			try
			{
				Joystick.Properties.BufferSize = BufferSize;
			}
			catch (SharpDXException)
			{
				// Read by its state alone.
			}
		}

		/// <summary>Reads what DirectInput kept for the device since the last read into <see cref="Changes"/>. Input thread only.</summary>
		/// <remarks>Read after the device's state, so a change the state has not caught yet is in it. Makes nothing after the first call.</remarks>
		/// <returns>False when the device keeps no changes, so only its state is known.</returns>
		public unsafe bool ReadChanges()
		{
			var native = Joystick.NativePointer;
			var method = Marshal.ReadIntPtr(Marshal.ReadIntPtr(native), GetDeviceDataSlot * IntPtr.Size);
			if (method != getDeviceDataPointer)
			{
				getDeviceData = Marshal.GetDelegateForFunctionPointer<GetDeviceDataMethod>(method);
				getDeviceDataPointer = method;
			}
			var count = BufferSize;
			int result;
			fixed (byte* entries = data)
				result = getDeviceData(native, ObjectDataSize, (IntPtr)entries, ref count, 0);
			// A failure, such as a device that keeps no buffer, leaves the state as the whole answer.
			if (result < 0)
			{
				Changes.Clear();
				return false;
			}
			Changes.Load(data, count, ObjectDataSize);
			return true;
		}

		#endregion

		public void Dispose()
		{
			Joystick.Dispose();
		}
	}
}
