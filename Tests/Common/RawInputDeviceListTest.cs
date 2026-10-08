// @under-test: App.v4/Common/DInput/DInputHelper.Step1.UpdateDevices.cs, App.v4/Common/DInput/DInputHelper.cs, Engine/Data/UserDevice.cs, Engine/Input/States/RawInputLayout.cs, App.v4/Common/AutoMapHelper.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A Raw Input game controller is a device of its own on the device list, beside its DirectInput twin, described by
	/// objects of the slots it fills, and the Raw Input hub lives as long as the helper, through every stop of the update
	/// thread.
	/// </summary>
	/// <remarks>
	/// The list tests run the device list read's Raw Input step and its take-in on recorded controllers (see
	/// <see cref="RawInputFixtures"/>) and a made-up device tree, through a hub no thread runs: the test stands in for
	/// the hub thread. They take every listed device off the list while they run and put them back after. The
	/// attached-controller test reads the machine as the worker does, opening devices without acquiring any.
	/// </remarks>
	[TestClass]
	public class RawInputDeviceListTest
	{
		const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

		static readonly Type ReadType = typeof(DInputHelper).GetNestedType("DeviceListRead", BindingFlags.NonPublic);

		const string PadPath = @"\\?\HID#VID_046D&PID_C219#7&1A2B3C4D&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
		const string WheelPath = @"\\?\HID#VID_046D&PID_C29B#7&2B3C4D5E&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
		const string OursPath = @"\\?\HID#VID_045E&PID_028E&IG_00#8&2B33A220&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

		#region Made-up machine

		/// <summary>A device DirectInput lists. It cannot be opened, so the take-in lists it from what the instance says.</summary>
		/// <param name="type">Its DirectInput type, with the HID flag when it is a HID device.</param>
		static DeviceInstance Instance(string name, Guid productGuid, int type)
		{
			var instance = new DeviceInstance
			{
				InstanceGuid = Guid.NewGuid(),
				ProductGuid = productGuid,
				InstanceName = name,
				ProductName = name,
			};
			typeof(DeviceInstance).GetField("RawType", Private).SetValue(instance, type);
			return instance;
		}

		const int HidFlag = 0x10000;

		/// <summary>The HID interface of a controller on a USB port, as Windows reports it: its path in lower case.</summary>
		static DeviceInfo Interface(string path, string deviceId, string parentId, string hardwareIds)
		{
			return new DeviceInfo
			{
				DevicePath = path.ToLowerInvariant(),
				DeviceId = deviceId,
				ParentDeviceId = parentId,
				HardwareIds = hardwareIds,
				Description = "HID-compliant game controller",
				Manufacturer = "(Standard system devices)",
			};
		}

		static DeviceInfo Node(string id, string parentId, string hardwareIds)
		{
			return new DeviceInfo { DeviceId = id, ParentDeviceId = parentId, HardwareIds = hardwareIds };
		}

		const string Root = @"HTREE\ROOT\0";
		const string RootHub = @"USB\ROOT_HUB30\4&2C4A1B1&0";
		const string PadUsb = @"USB\VID_046D&PID_C219\5&0001";
		const string PadHid = @"HID\VID_046D&PID_C219\7&1A2B3C4D&0&0000";
		const string WheelUsb = @"USB\VID_046D&PID_C29B\5&0002";
		const string WheelHid = @"HID\VID_046D&PID_C29B\7&2B3C4D5E&0&0000";
		const string Bus = @"ROOT\SYSTEM\0001";
		const string OursUsb = @"USB\VID_045E&PID_028E\1&79F5D87&0&01";
		const string OursHid = @"HID\VID_045E&PID_028E&IG_00\8&2B33A220&0&0000";

		static readonly DeviceInfo[] Interfaces =
		{
			Interface(PadPath, PadHid, PadUsb, @"HID\VID_046D&PID_C219"),
			Interface(WheelPath, WheelHid, WheelUsb, @"HID\VID_046D&PID_C29B"),
			Interface(OursPath, OursHid, OursUsb, @"HID\VID_045E&PID_028E&IG_00"),
		};

		/// <summary>Real controllers reach the root through nodes that all exist; ours hangs off the virtual bus.</summary>
		static readonly DeviceInfo[] Nodes =
		{
			Node(Root, null, null),
			Node(RootHub, Root, @"USB\ROOT_HUB30"),
			Node(PadUsb, RootHub, @"USB\VID_046D&PID_C219"),
			Node(PadHid, PadUsb, @"HID\VID_046D&PID_C219"),
			Node(WheelUsb, RootHub, @"USB\VID_046D&PID_C29B"),
			Node(WheelHid, WheelUsb, @"HID\VID_046D&PID_C29B"),
			Node(Bus, null, @"Root\ViGEmBus"),
			Node(OursUsb, Bus, @"USB\VID_045E&PID_028E"),
			Node(OursHid, OursUsb, @"HID\VID_045E&PID_028E&IG_00"),
		};

		#endregion

		#region The device list's private steps

		/// <summary>A finished read of the machine that found <paramref name="devices"/> through DirectInput, and the made-up interfaces and nodes.</summary>
		static object NewRead(params DeviceInstance[] devices)
		{
			var read = Activator.CreateInstance(ReadType, true);
			ReadType.GetField("Devices").SetValue(read, devices.ToList());
			ReadType.GetField("IntInfos").SetValue(read, Interfaces);
			ReadType.GetField("DevInfos").SetValue(read, Nodes);
			return read;
		}

		/// <summary>The worker's Raw Input step: lists the hub's devices in the read.</summary>
		static void FindRawInputDevices(object read, RawInputHub hub, DirectInput manager, Dictionary<string, DeviceInstance> twins, Dictionary<RawInputHubDevice, RawInputLayout> layouts)
		{
			Call(null, "FindRawInputDevices", read, hub, hub.Devices.Values.ToArray(), manager, twins, layouts);
		}

		/// <summary>What the read lists of each Raw Input device: its device, layout, interface and twin.</summary>
		static object[] Listings(object read)
		{
			return ((IList)ReadType.GetField("RawInputDevices").GetValue(read)).Cast<object>().ToArray();
		}

		static T Field<T>(object listing, string name)
		{
			return (T)listing.GetType().GetField(name).GetValue(listing);
		}

		/// <summary>The device thread's take-in of a read.</summary>
		static void TakeIn(DInputHelper helper, DirectInput manager, object read)
		{
			Call(helper, "TakeInDeviceList", manager, read);
		}

		static object Call(object target, string name, params object[] args)
		{
			var method = typeof(DInputHelper).GetMethod(name, Private);
			Assert.IsNotNull(method, "DInputHelper." + name + " was not found.");
			try
			{
				return method.Invoke(target, args);
			}
			catch (TargetInvocationException ex)
			{
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}

		static UserDevice Listed(Guid instanceGuid)
		{
			var found = SettingsManager.UserDevices.ItemsToArraySynchronized().Where(x => x.InstanceGuid == instanceGuid).ToArray();
			Assert.AreEqual(1, found.Length, "Devices listed with instance GUID " + instanceGuid + ".");
			return found[0];
		}

		/// <summary>Takes every listed device off the list, so a test lists only its own; <see cref="PutBack"/> restores them.</summary>
		static UserDevice[] TakeAll()
		{
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				var items = SettingsManager.UserDevices.Items.ToArray();
				SettingsManager.UserDevices.Items.Clear();
				return items;
			}
		}

		static void PutBack(UserDevice[] items)
		{
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				SettingsManager.UserDevices.Items.Clear();
				foreach (var item in items)
					SettingsManager.UserDevices.Items.Add(item);
			}
		}

		static int Count(RawInputLayout layout, MapType type)
		{
			return layout.Types.Count(x => x == type);
		}

		static int Mask(RawInputLayout layout, MapType type)
		{
			var mask = 0;
			for (var i = 0; i < layout.Types.Length; i++)
				if (layout.Types[i] == type)
					mask |= 1 << layout.Indexes[i];
			return mask;
		}

		static void AssertSlot(RawInputLayout layout, RawInputDevice device, int usage, MapType type, int index, string what)
		{
			var control = device.Controls.Single(x => x != null && x.UsagePage == 1 && x.Usage == usage && !x.IsButton);
			Assert.AreEqual(type, layout.Types[control.DataIndex], what + ": kind of slot.");
			Assert.AreEqual(index, layout.Indexes[control.DataIndex], what + ": slot.");
		}

		static readonly Guid[] AxisTypes = { ObjectGuid.XAxis, ObjectGuid.YAxis, ObjectGuid.ZAxis, ObjectGuid.RxAxis, ObjectGuid.RyAxis, ObjectGuid.RzAxis };

		/// <summary>The kind DirectInput gives an object in a slot of <paramref name="type"/>.</summary>
		static Guid KindOf(MapType type, int slot)
		{
			switch (type)
			{
				case MapType.Axis: return AxisTypes[slot];
				case MapType.Slider: return ObjectGuid.Slider;
				case MapType.POV: return ObjectGuid.PovController;
				default: return ObjectGuid.Button;
			}
		}

		/// <summary>Asserts <paramref name="objects"/> holds one object for each control <paramref name="layout"/> places, in data index order: of the slot's kind, with the slot as its DiIndex and instance, and with the control's usage.</summary>
		static void AssertUsageObjects(RawInputLayout layout, RawInputDevice device, DeviceObjectItem[] objects, string what)
		{
			var placed = device.Controls.Where(x => x != null && layout.Types[x.DataIndex] != MapType.None).ToArray();
			Assert.IsNotNull(objects, what + " has no objects.");
			Assert.AreEqual(placed.Length, objects.Length, what + ": objects.");
			for (var i = 0; i < placed.Length; i++)
			{
				var c = placed[i];
				var o = objects[i];
				var slot = layout.Indexes[c.DataIndex];
				var where = what + ", \"" + o.Name + "\"";
				Assert.AreEqual(KindOf(layout.Types[c.DataIndex], slot), o.Type, where + ": kind.");
				Assert.AreEqual(slot, o.DiIndex, where + ": DiIndex is not the slot.");
				Assert.AreEqual(slot, o.Instance, where + ": instance is not the slot.");
				Assert.AreEqual(c.UsagePage, o.UsagePage, where + ": usage page.");
				Assert.AreEqual(c.Usage, o.Usage, where + ": usage.");
			}
		}

		/// <summary>Asserts <paramref name="objects"/> are the twin's objects, slot for slot, without the force feedback flags a Raw Input device cannot honour.</summary>
		static void AssertTwinCopies(DeviceObjectItem[] twin, DeviceObjectItem[] objects, string what)
		{
			const DeviceObjectTypeFlags force = DeviceObjectTypeFlags.ForceFeedbackActuator | DeviceObjectTypeFlags.ForceFeedbackEffectTrigger;
			Assert.IsNotNull(objects, what + " has no objects.");
			Assert.AreEqual(twin.Length, objects.Length, what + ": objects.");
			for (var i = 0; i < twin.Length; i++)
			{
				var where = what + ", \"" + twin[i].Name + "\"";
				Assert.AreEqual(twin[i].Name, objects[i].Name, where + ": name.");
				Assert.AreEqual(twin[i].Type, objects[i].Type, where + ": kind.");
				Assert.AreEqual(twin[i].DiIndex, objects[i].DiIndex, where + ": DiIndex.");
				Assert.AreEqual(twin[i].UsagePage, objects[i].UsagePage, where + ": usage page.");
				Assert.AreEqual(twin[i].Usage, objects[i].Usage, where + ": usage.");
				Assert.AreEqual(twin[i].Flags & ~force, objects[i].Flags, where + ": flags other than force feedback.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A Raw Input device described by its twin's objects counts no motors: their force feedback flags are left off")]
		public void Twin_objects_carry_no_force_feedback()
		{
			var fixture = RawInputFixtures.G27;
			// As DirectInput reports a force feedback wheel: actuator axes and effect trigger buttons.
			var twin = fixture.Objects.Select(o => new DeviceObjectItem
			{
				Type = o.Type, DiIndex = o.DiIndex, UsagePage = o.UsagePage, Usage = o.Usage, Name = o.Name,
				Flags = o.Type == ObjectGuid.Button
					? DeviceObjectTypeFlags.PushButton | DeviceObjectTypeFlags.ForceFeedbackEffectTrigger
					: DeviceObjectTypeFlags.AbsoluteAxis | DeviceObjectTypeFlags.ForceFeedbackActuator,
			}).ToArray();
			using (var wheel = fixture.Device(0, WheelPath, null, 0x04))
			{
				var objects = RawInputLayout.FromTwin(wheel.Controls, twin).GetDeviceObjects();
				AssertTwinCopies(twin, objects, "The wheel");
				Assert.IsFalse(objects.Any(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)), "A Raw Input device claims a motor.");
			}
		}

		/// <summary>Asserts every control <paramref name="layout"/> places has an object in <paramref name="objects"/> with its usage and its slot's kind, and, but for a hat, its slot as DiIndex.</summary>
		static void AssertTwinSlots(RawInputLayout layout, RawInputDevice device, DeviceObjectItem[] objects, string what)
		{
			foreach (var c in device.Controls.Where(x => x != null && layout.Types[x.DataIndex] != MapType.None))
			{
				var type = layout.Types[c.DataIndex];
				var slot = layout.Indexes[c.DataIndex];
				var found = objects.Any(o => o.UsagePage == c.UsagePage && o.Usage == c.Usage && o.Type == KindOf(type, slot)
					&& (type == MapType.POV || o.DiIndex == slot));
				Assert.IsTrue(found, string.Format("{0}: no object for {1:X}:{2:X2} in {3} {4}.", what, c.UsagePage, c.Usage, type, slot));
			}
		}

		#endregion

		#region Listing

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Raw Input devices are listed as devices of their own, with their identity, interface and masks; DirectInput neither lists them nor marks them offline; one that leaves goes offline and comes back to its row")]
		public void Raw_Input_devices_are_listed_beside_DirectInput_devices()
		{
			var saved = TakeAll();
			var hub = new RawInputHub();
			var helper = new DInputHelper();
			var manager = new DirectInput();
			try
			{
				var pad = RawInputFixtures.RumblePad2.Device(1, PadPath, "Cordless RumblePad 2", 0x05);
				var wheel = RawInputFixtures.G27.Device(2, WheelPath, null, 0x04);
				// No hub thread runs: the test stands in for it.
				hub.Add(pad);
				var wheelEntry = hub.Add(wheel);
				// DirectInput lists the wheel, as its twin, and a device Raw Input does not read.
				var wheelTwin = Instance("G27 Racing Wheel", wheel.ProductGuid, (int)DeviceType.Driving | HidFlag);
				var keyboard = Instance("Keyboard", Guid.NewGuid(), (int)DeviceType.Keyboard);
				var twins = new Dictionary<string, DeviceInstance>(StringComparer.OrdinalIgnoreCase) { { WheelPath.ToLowerInvariant(), wheelTwin } };
				// The wheel took its twin's layout on an earlier read; the pad has no twin, and reads by its usages.
				var layouts = new Dictionary<RawInputHubDevice, RawInputLayout>();
				var wheelLayout = RawInputLayout.FromTwin(wheel.Controls, RawInputFixtures.G27.Objects);
				layouts[wheelEntry] = wheelLayout;
				var padLayout = RawInputLayout.Standard(pad.Controls);
				var read = NewRead(keyboard, wheelTwin);
				FindRawInputDevices(read, hub, manager, twins, layouts);
				Assert.AreEqual(2, Listings(read).Length, "Raw Input devices the read found.");
				TakeIn(helper, manager, read);

				var padUd = Listed(pad.InstanceGuid);
				Assert.AreEqual((int)InputSourceType.RawInput, padUd.InputSourceType, "The pad's input source.");
				Assert.IsFalse(padUd.IsDirectInput, "The pad is taken for a DirectInput device.");
				Assert.IsTrue(padUd.IsOnline, "The pad is offline.");
				Assert.IsNull(padUd.Device, "A Raw Input device was given a DirectInput device.");
				Assert.AreEqual(pad.ProductGuid, padUd.ProductGuid, "The pad's product GUID.");
				Assert.AreEqual("Cordless RumblePad 2", padUd.InstanceName, "The pad's name is not the one it reports.");
				Assert.AreEqual(PadPath, padUd.HidDevicePath, "The pad's interface path is not Raw Input's.");
				Assert.AreEqual(PadHid, padUd.HidDeviceId, "The pad's interface, reported in lower case, was not found.");
				Assert.AreEqual(PadHid, padUd.DevDeviceId, "The pad's device node.");
				Assert.AreEqual(0x046D, padUd.HidVendorId, "The pad's vendor.");
				Assert.AreEqual(0xC219, padUd.HidProductId, "The pad's product.");
				Assert.AreEqual(0x0100, padUd.HidRevision, "The pad's revision.");
				Assert.AreEqual((int)DeviceType.Gamepad, padUd.CapType, "A gamepad with no twin is typed by its top-level collection.");
				Assert.AreEqual(Mask(padLayout, MapType.Axis), padUd.DiAxeMask, "The pad's axes.");
				Assert.AreEqual(Mask(padLayout, MapType.Slider), padUd.DiSliderMask, "The pad's sliders.");
				Assert.AreEqual(Count(padLayout, MapType.Button), padUd.CapButtonCount, "The pad's buttons.");
				Assert.AreEqual(Count(padLayout, MapType.POV), padUd.CapPovCount, "The pad's hats.");
				AssertUsageObjects(padLayout, pad, padUd.DeviceObjects, "The pad");
				CollectionAssert.AreEquivalent(RawInputFixtures.RumblePad2.Objects.Select(x => x.Name).ToArray(), padUd.DeviceObjects.Select(x => x.Name).ToArray(),
					"The pad's objects are not named as DirectInput names its twin's.");
				Assert.AreEqual(0, padUd.DeviceEffects.Length, "The pad was given force feedback effects.");
				Assert.IsTrue(padUd.DeviceChanged, "The Direct Input tab is not told to draw the pad's objects.");
				// Its objects are named and placed as DirectInput names and places its twin's, so the automatic preset is the same.
				var padTwin = new UserDevice { CapType = padUd.CapType, DeviceObjects = RawInputFixtures.RumblePad2.Objects };
				Assert.AreEqual(AutoMapHelper.GetAutoPreset(padTwin).PadSettingChecksum, AutoMapHelper.GetAutoPreset(padUd).PadSettingChecksum,
					"The automatic preset for the pad read through Raw Input is not the one for its DirectInput twin.");

				var wheelUd = Listed(wheel.InstanceGuid);
				Assert.AreEqual((int)InputSourceType.RawInput, wheelUd.InputSourceType, "The wheel's input source.");
				Assert.AreEqual(wheelTwin.InstanceName, wheelUd.InstanceName, "A device that reports no name is not given its twin's.");
				Assert.AreEqual(wheelTwin.ProductName, wheelUd.ProductName, "The twin's product name.");
				Assert.AreEqual((int)DeviceType.Driving, wheelUd.CapType, "The twin's type.");
				Assert.AreEqual(WheelPath, wheelUd.HidDevicePath, "The wheel's interface path.");
				// The twin's slots: the wheel in X, the accelerator in Y, the brake in RotationZ, both other pedals in the sliders.
				Assert.AreEqual(0x23, wheelUd.DiAxeMask, "The wheel's axes are not its twin's.");
				Assert.AreEqual(0x3, wheelUd.DiSliderMask, "The wheel's sliders are not its twin's.");
				Assert.AreEqual(5, wheelUd.CapAxeCount, "The wheel's axes and sliders.");
				Assert.AreEqual(23, wheelUd.CapButtonCount, "The wheel's buttons.");
				Assert.AreEqual(1, wheelUd.CapPovCount, "The wheel's hat.");
				// The twin's own objects, whose slots are the wheel's by construction.
				AssertTwinCopies(RawInputFixtures.G27.Objects, wheelUd.DeviceObjects, "The wheel");
				AssertTwinSlots(wheelLayout, wheel, wheelUd.DeviceObjects, "The wheel");
				Assert.AreEqual(0, wheelUd.DeviceEffects.Length, "The wheel was given force feedback effects.");

				// The DirectInput devices, the twin among them, are listed as before: rows of their own, at 0.
				foreach (var instance in new[] { keyboard, wheelTwin })
				{
					var ud = Listed(instance.InstanceGuid);
					Assert.AreEqual(0, ud.InputSourceType, instance.InstanceName + ": a DirectInput device's input source is not 0.");
					Assert.IsTrue(ud.IsOnline, instance.InstanceName + " is offline.");
				}
				Assert.AreEqual(4, SettingsManager.UserDevices.Items.Count, "Devices listed.");

				// The same machine read again. DirectInput does not list the Raw Input devices, and they stay online.
				var padObjects = padUd.DeviceObjects;
				padUd.DeviceChanged = false;
				read = NewRead(keyboard, wheelTwin);
				FindRawInputDevices(read, hub, manager, twins, layouts);
				TakeIn(helper, manager, read);
				Assert.AreSame(padUd, Listed(pad.InstanceGuid), "The pad was listed again.");
				Assert.IsTrue(padUd.IsOnline, "The DirectInput part marked the pad offline.");
				Assert.IsTrue(wheelUd.IsOnline, "The DirectInput part marked the wheel offline.");
				Assert.AreSame(padObjects, padUd.DeviceObjects, "The pad's objects were made again for the same layout.");
				Assert.IsFalse(padUd.DeviceChanged, "The Direct Input tab is told to draw objects that did not change.");
				Assert.AreEqual(4, SettingsManager.UserDevices.Items.Count, "Devices listed after a second read.");

				// The wheel leaves.
				hub.Remove(wheel.Handle);
				read = NewRead(keyboard, wheelTwin);
				FindRawInputDevices(read, hub, manager, twins, layouts);
				TakeIn(helper, manager, read);
				Assert.IsFalse(wheelUd.IsOnline, "A wheel the hub no longer reads is online.");
				Assert.IsTrue(padUd.IsOnline, "The pad went offline with the wheel.");
				Assert.IsTrue(Listed(wheelTwin.InstanceGuid).IsOnline, "The wheel's DirectInput twin went offline with it.");
				Assert.IsFalse(layouts.ContainsKey(wheelEntry), "The layout of a wheel that left is kept.");

				// It comes back, before DirectInput has opened its twin: its row again, online, read by its usages until the twin is read.
				var back = RawInputFixtures.G27.Device(3, WheelPath, null, 0x04);
				hub.Add(back);
				var unopened = new Dictionary<string, DeviceInstance>(StringComparer.OrdinalIgnoreCase) { { WheelPath, Instance("G27 Racing Wheel", wheel.ProductGuid, (int)DeviceType.Driving | HidFlag) } };
				read = NewRead(keyboard, wheelTwin);
				FindRawInputDevices(read, hub, manager, unopened, layouts);
				TakeIn(helper, manager, read);
				Assert.AreSame(wheelUd, Listed(wheel.InstanceGuid), "The wheel came back as another row.");
				Assert.IsTrue(wheelUd.IsOnline, "The wheel did not come back online.");
				Assert.AreEqual(0x27, wheelUd.DiAxeMask, "A wheel back with no twin read is not read by its usages.");
				Assert.AreEqual(0x1, wheelUd.DiSliderMask, "A wheel back with no twin read is not read by its usages.");
				AssertUsageObjects(RawInputLayout.Standard(back.Controls), back, wheelUd.DeviceObjects, "The wheel back");
				Assert.AreEqual(4, SettingsManager.UserDevices.Items.Count, "Devices listed after the wheel came back.");
			}
			finally
			{
				hub.Stop();
				manager.Dispose();
				helper.Dispose();
				PutBack(saved);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A Raw Input device whose interface belongs to a pad of ours is not listed and gets no layout from its twin; a real controller beside it is listed")]
		public void A_pad_of_ours_is_not_listed_through_Raw_Input()
		{
			var saved = TakeAll();
			var hub = new RawInputHub();
			var helper = new DInputHelper();
			var manager = new DirectInput();
			try
			{
				var ours = RawInputFixtures.RumblePad2.Device(1, OursPath, "Controller (XBOX 360 For Windows)", 0x05);
				var oursEntry = hub.Add(ours);
				var pad = RawInputFixtures.RumblePad2.Device(2, PadPath, "Cordless RumblePad 2", 0x05);
				hub.Add(pad);
				// DirectInput lists our pad too. Were its layout built, its twin would be opened and found missing.
				var twins = new Dictionary<string, DeviceInstance>(StringComparer.OrdinalIgnoreCase)
				{
					{ OursPath, Instance("Controller (XBOX 360 For Windows)", ours.ProductGuid, (int)DeviceType.Gamepad | HidFlag) },
				};
				var layouts = new Dictionary<RawInputHubDevice, RawInputLayout>();
				var read = NewRead();
				FindRawInputDevices(read, hub, manager, twins, layouts);
				var listed = Listings(read).Select(x => Field<RawInputDevice>(x, "Device")).ToArray();
				CollectionAssert.AreEqual(new[] { pad }, listed, "The devices the read lists.");
				Assert.IsFalse(layouts.ContainsKey(oursEntry), "A layout was kept for a pad of ours.");
				Assert.IsFalse(oursEntry.ApplyLayout(), "The hub was given a layout for a pad of ours.");
				TakeIn(helper, manager, read);
				Assert.IsFalse(SettingsManager.UserDevices.ItemsToArraySynchronized().Any(x => x.InstanceGuid == ours.InstanceGuid), "A pad of ours is listed.");
				Assert.IsTrue(Listed(pad.InstanceGuid).IsOnline, "The real controller is not listed online.");
			}
			finally
			{
				hub.Stop();
				manager.Dispose();
				helper.Dispose();
				PutBack(saved);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("An interface is found by its path in either case, as Windows reports the same path in either")]
		public void An_interface_is_found_by_its_path_in_either_case()
		{
			var interfaces = new[] { Interface(PadPath, PadHid, PadUsb, ""), Interface(WheelPath, WheelHid, WheelUsb, "") };
			Assert.AreSame(interfaces[0], Call(null, "FindInterface", interfaces, PadPath.ToUpperInvariant()), "Upper case.");
			Assert.AreSame(interfaces[1], Call(null, "FindInterface", interfaces, WheelPath), "Mixed case.");
			Assert.AreSame(interfaces[1], Call(null, "FindInterface", interfaces, WheelPath.ToLowerInvariant()), "Lower case.");
			Assert.IsNull(Call(null, "FindInterface", interfaces, OursPath), "A path no interface has.");
			Assert.IsNull(Call(null, "FindInterface", null, PadPath), "No interfaces read.");
		}

		#endregion

		#region Twin layout

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Given its twin's controls, a hub device is read in the twin's slots and the layout is kept; without a twin it stays in the slots the hub reads it by, its usages' at first; a later read does not take the layout again, and a device that comes back reads by it at once and asks its twin anew")]
		public void A_hub_device_takes_its_twin_s_layout_once_per_arrival()
		{
			var fixture = RawInputFixtures.G27;
			var hub = new RawInputHub();
			var manager = new DirectInput();
			try
			{
				// A recorded report carries no device handle, so the stand-in device has none either.
				var wheel = fixture.Device(0, WheelPath, null, 0x04);
				var entry = hub.Add(wheel);
				var usages = RawInputLayout.Standard(wheel.Controls);
				var layouts = new Dictionary<RawInputHubDevice, RawInputLayout>();
				// Without a twin: the slots the hub reads it by, its usages' as it arrived, nothing kept, and the hub not told.
				var none = new Dictionary<RawInputHubDevice, RawInputLayout>();
				var standard = (RawInputLayout)Call(null, "TakeTwinLayout", hub, entry, null, none);
				Assert.AreSame(entry.Layout, standard, "Without a twin, the layout is not the one the hub reads the device by.");
				CollectionAssert.AreEqual(usages.Types, standard.Types, "Without a twin, the kinds of slot are not the usages'.");
				CollectionAssert.AreEqual(usages.Indexes, standard.Indexes, "Without a twin, the slots are not the usages'.");
				Assert.AreEqual(0, none.Count, "A layout was kept for a device with no twin.");
				Assert.IsFalse(entry.ApplyLayout(), "The hub was given a layout for a device with no twin.");
				var layout = (RawInputLayout)Call(null, "TakeTwinLayout", hub, entry, fixture.Objects, layouts);
				AssertSlot(layout, wheel, 0x30, MapType.Axis, 0, "wheel");
				AssertSlot(layout, wheel, 0x31, MapType.Slider, 0, "combined pedals");
				AssertSlot(layout, wheel, 0x32, MapType.Axis, 1, "accelerator");
				AssertSlot(layout, wheel, 0x35, MapType.Axis, 5, "brake");
				AssertSlot(layout, wheel, 0x36, MapType.Slider, 1, "clutch");
				Assert.AreSame(layout, layouts[entry], "The twin's layout was not kept.");
				// The hub reads the next report in the twin's slots: the state DirectInput read for the twin.
				Assert.IsTrue(entry.ApplyLayout(), "The hub was not given the twin's layout.");
				var sample = fixture.Samples.Single(x => x.Shows == "Wheel axis 1:30 = 0");
				Assert.AreEqual(1, hub.ReadInput(RawInputTest.RawInput(sample.Bytes())), "Reports read.");
				var state = new SourceState();
				Assert.IsTrue(hub.TryCopyState(wheel.InstanceGuid, state, out _));
				RawInputTest.AssertState(fixture, sample, state);
				// Without a twin now, the device keeps the twin's slots the hub reads it by.
				Assert.AreSame(layout, Call(null, "TakeTwinLayout", hub, entry, null, none), "A device that lost sight of its twin went back to its usages.");

				// A later read keeps the layout it has: its twin, unknown to DirectInput, is not opened again.
				var twins = new Dictionary<string, DeviceInstance>(StringComparer.OrdinalIgnoreCase) { { WheelPath, Instance("G27 Racing Wheel", wheel.ProductGuid, (int)DeviceType.Driving | HidFlag) } };
				var read = NewRead();
				FindRawInputDevices(read, hub, manager, twins, layouts);
				var listing = Listings(read).Single();
				Assert.AreSame(layout, Field<RawInputLayout>(listing, "Layout"), "A later read took the layout again.");
				AssertTwinCopies(fixture.Objects, Field<DeviceObjectItem[]>(listing, "Objects"), "The device described by its twin's objects");
				// Back after leaving, the device is another entry: the hub reads it by the twin's slots from its first report,
				// and the twin is asked again. This one cannot be opened, so the device is described by the layout it has.
				hub.Remove(wheel.Handle);
				var back = hub.Add(fixture.Device(0, WheelPath, null, 0x04));
				Assert.AreSame(layout, back.Layout, "A device that came back does not read by its twin's slots.");
				read = NewRead();
				FindRawInputDevices(read, hub, manager, twins, layouts);
				Assert.IsFalse(layouts.ContainsKey(entry), "The layout of the device that left is kept.");
				listing = Listings(read).Single();
				Assert.AreSame(layout, Field<RawInputLayout>(listing, "Layout"), "A device back whose twin cannot be opened is not listed by the slots the hub reads it by.");
				AssertTwinCopies(fixture.Objects, Field<DeviceObjectItem[]>(listing, "Objects"), "A device back whose twin cannot be opened, described by its twin's objects");
				Assert.IsFalse(layouts.ContainsKey(back), "A layout was kept for a device whose twin could not be opened, so it is never asked again.");
			}
			finally
			{
				hub.Stop();
				manager.Dispose();
			}
		}

		#endregion

		#region Attached controllers

		[TestMethod, TestCategory("devices")]
		[Description("Read as the worker reads the machine, every attached Raw Input game controller takes its layout from its DirectInput twin, whether the twin was listed before or not, and no DirectInput device is acquired")]
		public void Attached_controllers_take_their_twins_layouts_without_acquiring()
		{
			var attached = RawInputDevice.GetGameControllers();
			var hub = new RawInputHub();
			try
			{
				if (attached.Count == 0)
					Assert.Inconclusive("No HID game controller is attached.");
				hub.Start();
				var expected = attached.Select(x => x.InstanceGuid).ToArray();
				Ui.WaitFor(() => expected.All(hub.Devices.ContainsKey) ? "arrived" : null, TimeSpan.FromSeconds(3), "the attached controllers to arrive at the hub");
				// Listed by an earlier run, as at start-up: known, with no interface path yet, so each twin is opened for the purpose.
				var known = new Dictionary<Guid, string>();
				using (var manager = new DirectInput())
					foreach (var instance in manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
						known[instance.InstanceGuid] = "";
				ReadAndCheck(hub, known, "Twins listed before");
				// Arrived since the last read: each twin is the device the read makes for the list.
				ReadAndCheck(hub, new Dictionary<Guid, string>(), "Twins new to the list");
			}
			finally
			{
				hub.Stop();
				foreach (var device in attached)
					device.Dispose();
			}
		}

		static void ReadAndCheck(RawInputHub hub, Dictionary<Guid, string> known, string what)
		{
			var layouts = new Dictionary<RawInputHubDevice, RawInputLayout>();
			var read = Call(null, "ReadDeviceList", known, hub, layouts);
			var made = (Dictionary<Guid, Joystick>)ReadType.GetField("Made").GetValue(read);
			try
			{
				var error = (Exception)ReadType.GetField("Error").GetValue(read);
				Assert.IsNull(error, what + ": the read failed: " + error);
				Console.WriteLine("{0}: {1}", what, ReadType.GetField("Phases").GetValue(read));
				var listings = Listings(read);
				Assert.IsTrue(listings.Length > 0, what + ": no Raw Input device listed.");
				foreach (var listing in listings)
				{
					var device = Field<RawInputDevice>(listing, "Device");
					var twin = Field<DeviceInstance>(listing, "Twin");
					Console.WriteLine("{0}: \"{1}\" {2}, twin \"{3}\"", what, device.ProductName, device.InterfacePath, twin == null ? null : twin.InstanceName);
					Assert.IsNotNull(twin, what + ": \"" + device.ProductName + "\" has no DirectInput twin.");
					Assert.IsNotNull(Field<DeviceInfo>(listing, "Interface"), what + ": \"" + device.ProductName + "\" has no interface.");
					var entry = hub.Devices[device.InstanceGuid];
					Assert.IsTrue(layouts.ContainsKey(entry), what + ": \"" + device.ProductName + "\" did not take its twin's layout.");
					Assert.AreSame(layouts[entry], Field<RawInputLayout>(listing, "Layout"), what + ": the listing's layout is not the twin's.");
				}
				// Every device the read made for the list, twins among them when new, is handed over as it was opened: not acquired.
				foreach (var joystick in made.Values)
				{
					try
					{
						joystick.GetCurrentState();
						Assert.Fail(what + ": \"" + joystick.Information.InstanceName + "\" was acquired by the read.");
					}
					catch (SharpDXException ex)
					{
						Assert.AreEqual(SharpDX.DirectInput.ResultCode.NotAcquired.Code, ex.ResultCode.Code, what + ": \"" + joystick.Information.InstanceName + "\": " + ex.Message);
					}
				}
			}
			finally
			{
				foreach (var joystick in made.Values)
					joystick.Dispose();
			}
		}

		#endregion

		#region Hub lifetime

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The Raw Input hub starts with the update thread the first time and runs, the same hub, through every stop and start of the update thread until the helper is disposed; a device arriving or leaving asks for a device list read")]
		public void The_hub_runs_until_the_helper_is_disposed()
		{
			// Suspended: the thread runs and reads nothing.
			var helper = new DInputHelper { Suspended = true };
			try
			{
				Assert.IsFalse(helper.RawInput.IsRunning, "Running before the update thread started.");
				helper.Start();
				Assert.IsTrue(helper.RawInput.IsRunning, "The hub did not start with the update thread.");
				Assert.IsNull(helper.RawInputError, "" + helper.RawInputError);
				// A hub started again would have another window, and its devices would start again with their usages' slots.
				var window = helper.RawInput.WindowHandle;
				for (var round = 1; round <= 2; round++)
				{
					var where = "Round " + round;
					// As on every change of Windows settings: the update thread stops, and starts again a second later.
					Assert.IsTrue(helper.Stop(), where + ": the update thread did not stop.");
					Assert.IsTrue(helper.RawInput.IsRunning, where + ": the hub stopped with the update thread.");
					Assert.AreEqual(window, helper.RawInput.WindowHandle, where + ": the hub was started again.");
					helper.Start();
					Assert.AreEqual(window, helper.RawInput.WindowHandle, where + ": the hub was started again with the update thread.");
					Assert.IsNull(helper.RawInputError, where + ": " + helper.RawInputError);
					// The hub raises its change on its own thread; the update thread is told by the flag it reads once a pass.
					helper.UpdateDevicesEnabled = false;
					var changed = (EventHandler)typeof(RawInputHub).GetField("DevicesChanged", Private).GetValue(helper.RawInput);
					Assert.IsNotNull(changed, where + ": nothing listens to the hub.");
					changed(helper.RawInput, EventArgs.Empty);
					Assert.IsTrue(helper.UpdateDevicesEnabled, where + ": a Raw Input device that came or went does not ask for a device list read.");
				}
			}
			finally
			{
				helper.Dispose();
			}
			Assert.IsFalse(helper.RawInput.IsRunning, "The hub outlived the helper.");
		}

		/// <summary>A helper whose Raw Input hub never starts, as when Windows refuses its window or registration.</summary>
		class RefusedHub : DInputHelper
		{
			public const string Refusal = "Raw Input refused for the test.";

			protected override void StartRawInput()
			{
				throw new System.ComponentModel.Win32Exception(5, Refusal);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A hub that does not start is recorded and written once, and the update thread runs its passes all the same")]
		public void A_hub_that_does_not_start_leaves_the_update_thread_running()
		{
			var faults = new List<Exception>();
			EventHandler<LogHelperEventArgs> keep = (sender, e) =>
			{
				if (e.Exception.Message != RefusedHub.Refusal)
					return;
				faults.Add(e.Exception);
				e.Cancel = true;
			};
			var log = LogHelper.Current;
			var game = SettingsManager.CurrentGame;
			// No game: a pass reads no device and feeds nothing, and still counts.
			SettingsManager.CurrentGame = null;
			log.WritingException += keep;
			var helper = new RefusedHub();
			try
			{
				for (var round = 1; round <= 2; round++)
				{
					var where = "Round " + round;
					var passes = helper.PassCount;
					helper.Start();
					Ui.WaitFor(() => helper.PassCount > passes + 10 ? "passing" : null, TimeSpan.FromSeconds(10), where + ": the update thread to run passes");
					Assert.IsInstanceOfType(helper.RawInputError, typeof(System.ComponentModel.Win32Exception), where + ": the refusal was not recorded.");
					Assert.IsFalse(helper.RawInput.IsRunning, where + ": a refused hub runs.");
					Assert.IsTrue(helper.Stop(), where + ": the update thread did not stop.");
				}
				Assert.AreEqual(1, faults.Count, "Two refused starts wrote " + faults.Count + " fault reports.");
			}
			finally
			{
				helper.Dispose();
				log.WritingException -= keep;
				SettingsManager.CurrentGame = game;
			}
		}

		#endregion
	}
}
