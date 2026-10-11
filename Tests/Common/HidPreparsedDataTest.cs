// @under-test: Engine/JocysCom/Win32/NativeMethods.hid.cs, Engine/JocysCom/IO/DeviceDetector.cs, Engine/Common/LogitechWheel.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// The preparsed data Windows hands out for a HID device is given back, so reading a device's details leaks nothing.
	/// </summary>
	/// <remarks>
	/// HidD_FreePreparsedData takes the pointer itself. Declared to take it by reference, the call would hand Windows the
	/// address of the caller's own variable: memory it never gave out, while the block it did give out stays allocated.
	/// The device list reads every HID interface's details, and the Logitech wheel support reads them again.
	/// </remarks>
	[TestClass]
	public class HidPreparsedDataTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Freeing preparsed data takes the pointer itself, not a reference to it")]
		public void Freeing_takes_the_pointer_itself()
		{
			var free = typeof(NativeMethods).GetMethod(nameof(NativeMethods.HidD_FreePreparsedData));
			var parameter = free.GetParameters()[0];
			Assert.AreEqual(typeof(IntPtr), parameter.ParameterType,
				"HidD_FreePreparsedData must take the pointer by value; by reference it frees the address of the caller's variable.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The preparsed data of each attached controller is read and given back")]
		public void Preparsed_data_of_a_real_device_is_given_back()
		{
			using (var manager = new DirectInput())
			{
				var instances = manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
				var checkedCount = 0;
				foreach (var instance in instances)
				{
					string path;
					// Opened only to ask for its interface path: not acquired, nothing sent to it.
					using (var device = new Joystick(manager, instance.InstanceGuid))
						path = device.Properties.InterfacePath;
					if (string.IsNullOrEmpty(path))
						continue;
					// Opened the way the device list opens it: no read or write access.
					using (var handle = NativeMethods.CreateFile(path, 0, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
					{
						if (handle.IsInvalid)
							continue;
						var preparsed = IntPtr.Zero;
						Assert.IsTrue(NativeMethods.HidD_GetPreparsedData(handle, ref preparsed), instance.ProductName + ": no preparsed data.");
						Assert.AreNotEqual(IntPtr.Zero, preparsed);
						Assert.IsTrue(NativeMethods.HidD_FreePreparsedData(preparsed), instance.ProductName + ": Windows did not take its preparsed data back.");
						checkedCount++;
					}
				}
				if (checkedCount == 0)
					Assert.Inconclusive("No HID game controller is attached.");
				Console.WriteLine("{0} controllers' preparsed data read and given back.", checkedCount);
			}
		}
	}
}
