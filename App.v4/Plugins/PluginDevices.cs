using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Plugins;

namespace x360ce.App.Plugins
{
	/// <summary>The devices plugins add, as rows of the device list, and how the engine reads them.</summary>
	public static class PluginDevices
	{
		/// <summary>The HID class every game controller is filed under, given to plugin devices so they are listed with them.</summary>
		static readonly Guid HidClassGuid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030");

		/// <summary>A new device of this kind for the device list, named "{Name} {n}", with the controls the plugin said it has.</summary>
		public static UserDevice NewUserDevice(PluginDeviceType type)
		{
			// Snapshot once: the interface thread can add or remove devices meanwhile.
			var existing = SettingsManager.UserDevices.ItemsToArraySynchronized();
			string instanceName;
			for (var i = 1; ; i++)
			{
				instanceName = type.Name + " " + i;
				if (!existing.Any(x => x.InstanceName == instanceName))
					break;
			}
			var ud = new UserDevice();
			ud.InputSourceType = (int)InputSourceType.Plugin;
			ud.DevManufacturer = type.File.Name;
			ud.InstanceGuid = Guid.NewGuid();
			ud.InstanceName = instanceName;
			ud.ProductGuid = type.ProductGuid;
			ud.ProductName = type.Name;
			ud.HidDescription = instanceName;
			ud.HidClassGuid = HidClassGuid;
			ud.IsEnabled = true;
			Describe(ud, type);
			ud.IsOnline = !type.Faulted;
			return ud;
		}

		/// <summary>Gives a plugin row the controls its kind of device has.</summary>
		public static void Describe(UserDevice ud, PluginDeviceType type)
		{
			ud.CapAxeCount = type.AxisCount + type.SliderCount;
			ud.CapButtonCount = type.ButtonCount;
			ud.CapPovCount = type.PovCount;
			// A standard gamepad, attached, as Windows describes one; a plugin device takes no DirectInput force.
			ud.CapType = (int)DeviceType.Gamepad;
			ud.CapSubtype = 258;
			ud.CapFlags = (int)(DeviceFlags.Attached | DeviceFlags.Emulated);
			ud.DeviceObjects = GetDeviceObjects(type.AxisCount, type.SliderCount, type.PovCount, type.ButtonCount);
			ud.DiAxeMask = (1 << type.AxisCount) - 1;
			ud.DiSliderMask = (1 << type.SliderCount) - 1;
			ud.DeviceEffects = new DeviceEffectItem[0];
		}

		/// <summary>The offsets DirectInput gives sliders, in the order <see cref="PluginState.Sliders"/> holds them.</summary>
		static readonly JoystickOffset[] SliderOffsets =
		{
			JoystickOffset.Sliders0, JoystickOffset.Sliders1,
			JoystickOffset.VelocitySliders0, JoystickOffset.VelocitySliders1,
			JoystickOffset.AccelerationSliders0, JoystickOffset.AccelerationSliders1,
			JoystickOffset.ForceSliders0, JoystickOffset.ForceSliders1,
		};

		/// <summary>The controls of a device with these counts, described as DirectInput describes a game controller's.</summary>
		public static DeviceObjectItem[] GetDeviceObjects(int axes, int sliders, int povs, int buttons)
		{
			var axisOffsets = new[] { JoystickOffset.X, JoystickOffset.Y, JoystickOffset.Z, JoystickOffset.RotationX, JoystickOffset.RotationY, JoystickOffset.RotationZ };
			var axisGuids = new[] { ObjectGuid.XAxis, ObjectGuid.YAxis, ObjectGuid.ZAxis, ObjectGuid.RxAxis, ObjectGuid.RyAxis, ObjectGuid.RzAxis };
			var axisNames = new[] { "X Axis", "Y Axis", "Z Axis", "X Rotation", "Y Rotation", "Z Rotation" };
			var list = new List<DeviceObjectItem>();
			for (var i = 0; i < axes && i < axisOffsets.Length; i++)
				list.Add(new DeviceObjectItem((int)axisOffsets[i], axisGuids[i], ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, i, axisNames[i]) { UsagePage = 1, Usage = 0x30 + i });
			for (var i = 0; i < sliders && i < SliderOffsets.Length; i++)
				list.Add(new DeviceObjectItem((int)SliderOffsets[i], ObjectGuid.Slider, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, axes + i, "Slider " + i) { UsagePage = 1, Usage = 0x36 });
			for (var i = 0; i < povs && i < 4; i++)
				list.Add(new DeviceObjectItem((int)JoystickOffset.PointOfViewControllers0 + i * 4, ObjectGuid.PovController, 0, DeviceObjectTypeFlags.PointOfViewController, i, i == 0 ? "Hat Switch" : "Hat Switch " + (i + 1)) { UsagePage = 1, Usage = 0x39 });
			for (var i = 0; i < buttons && i < 128; i++)
				list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons0 + i, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, i, "Button " + i) { UsagePage = 9, Usage = i + 1 });
			foreach (var item in list)
				item.DiIndex = item.Instance;
			return list.ToArray();
		}

		/// <summary>Marks each plugin row online while its kind of device is loaded and working.</summary>
		/// <remarks>
		/// A row of a kind a plugin adds, written down as DirectInput before plugins existed (the Test Device), is made a
		/// plugin row here, so its mappings keep working. Runs on the device refresh thread; a change is marshalled to the
		/// thread that owns the list, which locks it there.
		/// </remarks>
		public static void UpdateOnline(PluginManager manager)
		{
			foreach (var ud in SettingsManager.UserDevices.ItemsToArraySynchronized())
			{
				var type = manager == null ? null : manager.FindDeviceType(ud.ProductGuid);
				if (ud.InputSourceType != (int)InputSourceType.Plugin)
				{
					if (type == null || !ud.IsDirectInput || !string.IsNullOrEmpty(ud.HidDevicePath))
						continue;
					ud.InputSourceType = (int)InputSourceType.Plugin;
				}
				var online = type != null && !type.Faulted;
				if (online && (ud.DeviceObjects == null || ud.CapButtonCount != type.ButtonCount || ud.CapPovCount != type.PovCount
					|| ud.CapAxeCount != type.AxisCount + type.SliderCount))
					Describe(ud, type);
				if (ud.IsOnline != online)
					ud.IsOnline = online;
			}
		}
	}

	/// <summary>One plugin device the engine reads: the plugin's instance, and the state it writes into.</summary>
	/// <remarks>Made on the first read and kept, so a read makes nothing.</remarks>
	public sealed class PluginDeviceReader : IDisposable
	{
		public PluginDeviceReader(PluginDeviceType type, IPluginDevice device)
		{
			Type = type;
			Device = device;
		}

		public PluginDeviceType Type { get; }
		public IPluginDevice Device { get; }
		public readonly PluginState State = new PluginState();

		/// <summary>Copies what the plugin wrote into the state the engine maps.</summary>
		public void CopyTo(SourceState state)
		{
			Array.Copy(State.Axes, state.Axis, State.Axes.Length);
			Array.Copy(State.Sliders, state.Sliders, State.Sliders.Length);
			Array.Copy(State.Povs, state.Povs, State.Povs.Length);
			Array.Copy(State.Buttons, state.Buttons, State.Buttons.Length);
		}

		public void Dispose()
		{
			var disposable = Device as IDisposable;
			if (disposable != null)
				disposable.Dispose();
		}
	}
}
