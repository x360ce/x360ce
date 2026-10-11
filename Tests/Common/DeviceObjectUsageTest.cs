// @under-test: Engine/Common/DeviceObjectItem.cs, App.v4/Common/AppHelper.cs, App.v4/Common/TestDeviceHelper.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.App;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Every control carries the HID usage page and usage DirectInput reports for it. A control read
	/// through Raw Input is known only by that usage, so the usage is what puts it in the same place
	/// of the state as the same control read through DirectInput.
	/// </summary>
	/// <remarks>
	/// The attached-controller test opens each controller only to list its objects: it does not
	/// acquire it, change its settings or send it force feedback.
	/// </remarks>
	[TestClass]
	public class DeviceObjectUsageTest
	{
		/// <summary>The HID Generic Desktop page, where axes and hat switches live.</summary>
		const int GenericDesktopPage = 1;
		/// <summary>The HID Button page, where button N is usage N.</summary>
		const int ButtonPage = 9;
		/// <summary>The Generic Desktop usage of a hat switch.</summary>
		const int HatSwitchUsage = 0x39;

		/// <summary>The Generic Desktop usage a HID device gives each axis type.</summary>
		static readonly Dictionary<Guid, int> AxisUsages = new Dictionary<Guid, int>
		{
			{ ObjectGuid.XAxis, 0x30 },
			{ ObjectGuid.YAxis, 0x31 },
			{ ObjectGuid.ZAxis, 0x32 },
			{ ObjectGuid.RxAxis, 0x33 },
			{ ObjectGuid.RyAxis, 0x34 },
			{ ObjectGuid.RzAxis, 0x35 },
			{ ObjectGuid.Slider, 0x36 },
		};

		static bool IsAxis(DeviceObjectItem o)
		{
			return (o.Flags & DeviceObjectTypeFlags.Axis) != 0;
		}

		static bool IsButton(DeviceObjectItem o)
		{
			return (o.Flags & DeviceObjectTypeFlags.Button) != 0;
		}

		static bool IsPov(DeviceObjectItem o)
		{
			return (o.Flags & DeviceObjectTypeFlags.PointOfViewController) != 0;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Every axis, button and hat switch of the test controller carries the HID usage a gamepad reports for it")]
		public void The_test_controller_carries_a_hid_usage_for_every_control()
		{
			var objects = TestDeviceHelper.GetDeviceObjects();
			Assert.IsTrue(objects.Any(IsAxis), "The test controller describes no axis.");
			Assert.IsTrue(objects.Any(IsButton), "The test controller describes no button.");
			Assert.IsTrue(objects.Any(IsPov), "The test controller describes no hat switch.");
			foreach (var o in objects.Where(x => IsAxis(x) || IsButton(x) || IsPov(x)))
			{
				Assert.AreNotEqual(0, o.UsagePage, o.Name + " has no usage page.");
				Assert.AreNotEqual(0, o.Usage, o.Name + " has no usage.");
				if (IsAxis(o))
				{
					Assert.AreEqual(GenericDesktopPage, o.UsagePage, o.Name + " is an axis, which lives on the Generic Desktop page.");
					Assert.IsTrue(AxisUsages.ContainsKey(o.Type), o.Name + " is of a type no HID axis usage names: " + o.TypeName + ".");
					Assert.AreEqual(AxisUsages[o.Type], o.Usage, o.Name + " carries a usage that names a different axis than its type, " + o.TypeName + ".");
				}
				else if (IsButton(o))
				{
					Assert.AreEqual(ButtonPage, o.UsagePage, o.Name + " is a button, which lives on the Button page.");
					Assert.AreEqual(o.DiIndex + 1, o.Usage, o.Name + " must carry usage " + (o.DiIndex + 1) + ", the HID number of the button at index " + o.DiIndex + ".");
				}
				else
				{
					Assert.AreEqual(GenericDesktopPage, o.UsagePage, o.Name + " is a hat switch, which lives on the Generic Desktop page.");
					Assert.AreEqual(HatSwitchUsage, o.Usage, o.Name + " is a hat switch and must carry the hat switch usage.");
				}
			}
		}

		[TestMethod, TestCategory("devices")]
		[Description("DirectInput reports a HID usage for every axis and button of each attached controller, and the object list keeps it")]
		public void Attached_controllers_keep_a_hid_usage_for_every_axis_and_button()
		{
			using (var manager = new DirectInput())
			{
				var instances = manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
				if (instances.Count == 0)
					Assert.Inconclusive("No game controller is attached.");
				foreach (var instance in instances)
				{
					// Opened only to list its objects, never acquired.
					using (var device = new Joystick(manager, instance.InstanceGuid))
					{
						var objects = AppHelper.GetDeviceObjects(device);
						var controls = objects.Where(x => IsAxis(x) || IsButton(x)).ToArray();
						Console.WriteLine("{0}: {1} axes, {2} buttons", instance.ProductName, controls.Count(IsAxis), controls.Count(IsButton));
						foreach (var o in objects.Where(x => IsAxis(x) || IsButton(x) || IsPov(x)))
							Console.WriteLine("  {0,-24} {1,-12} page {2}, usage 0x{3:X2}", o.Name, o.TypeName, o.UsagePage, o.Usage);
						foreach (var o in controls)
						{
							Assert.AreNotEqual(0, o.UsagePage, instance.ProductName + ": " + o.Name + " has no usage page.");
							Assert.AreNotEqual(0, o.Usage, instance.ProductName + ": " + o.Name + " has no usage.");
							if (IsAxis(o))
								Assert.AreEqual(GenericDesktopPage, o.UsagePage, instance.ProductName + ": " + o.Name + " is an axis, which lives on the Generic Desktop page.");
							else
								Assert.AreEqual(ButtonPage, o.UsagePage, instance.ProductName + ": " + o.Name + " is a button, which lives on the Button page.");
						}
					}
				}
			}
		}
	}
}
