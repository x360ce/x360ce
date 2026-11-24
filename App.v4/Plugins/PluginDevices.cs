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

		/// <summary>Internal plugin setting key storing the owning plugin kind's GUID once a hardware plugin adopts a real machine's ProductGuid.</summary>
		public const string PluginTypeSettingKey = "__PluginProductGuid";

		/// <summary>Finds the plugin device type that owns <paramref name="ud"/>, even after it adopted a real hardware ProductGuid.</summary>
		public static PluginDeviceType FindType(UserDevice ud, PluginManager manager)
		{
			if (ud == null || manager == null)
				return null;
			var type = manager.FindDeviceType(ud.ProductGuid);
			if (type != null)
				return type;
			var stored = PluginSettings.Current == null ? null : PluginSettings.Current.Get(ud.InstanceGuid, PluginTypeSettingKey);
			Guid pluginGuid;
			if (!string.IsNullOrEmpty(stored) && Guid.TryParse(stored, out pluginGuid))
				return manager.FindDeviceType(pluginGuid);
			return null;
		}

		/// <summary>
		/// Updates a plugin device row to the real physical hardware's identity (ProductGuid, Manufacturer, ProductName, VID/PID)
		/// so settings and TOP Controllers in the online database record the actual machine rather than the generic plugin name.
		/// </summary>
		public static bool ApplyHardwareIdentity(UserDevice ud, PluginDeviceType type, PluginDeviceIdentity identity)
		{
			if (ud == null || type == null || identity == null || !identity.IsConnected
				|| identity.ProductGuid == Guid.Empty || string.IsNullOrWhiteSpace(identity.ProductName))
				return false;

			// Remember which plugin owns this InstanceGuid before replacing ProductGuid with the real machine's ID.
			if (PluginSettings.Current != null)
				PluginSettings.Current.Set(ud.InstanceGuid, PluginTypeSettingKey, type.ProductGuid.ToString("D"));

			var changed = false;
			if (ud.ProductGuid != identity.ProductGuid)
			{
				ud.ProductGuid = identity.ProductGuid;
				changed = true;
			}
			var product = identity.ProductName.Trim();
			if (ud.ProductName != product)
			{
				// Replace generic placeholder InstanceName ("Fitness Machine 1") with the real machine name too.
				var genericPrefix = type.Name + " ";
				if (string.IsNullOrEmpty(ud.InstanceName)
					|| ud.InstanceName.StartsWith(genericPrefix, StringComparison.Ordinal)
					|| string.Equals(ud.InstanceName, ud.ProductName, StringComparison.Ordinal))
				{
					ud.InstanceName = product;
					ud.HidDescription = product;
				}
				ud.ProductName = product;
				changed = true;
			}
			var mfg = string.IsNullOrWhiteSpace(identity.Manufacturer) ? VendorOf(type) : identity.Manufacturer.Trim();
			if (ud.DevManufacturer != mfg) { ud.DevManufacturer = mfg; changed = true; }
			if (ud.HidManufacturer != mfg) { ud.HidManufacturer = mfg; changed = true; }
			if (identity.VendorId > 0)
			{
				if (ud.DevVendorId != identity.VendorId) { ud.DevVendorId = identity.VendorId; changed = true; }
				if (ud.HidVendorId != identity.VendorId) { ud.HidVendorId = identity.VendorId; changed = true; }
			}
			if (identity.ProductId > 0)
			{
				if (ud.DevProductId != identity.ProductId) { ud.DevProductId = identity.ProductId; changed = true; }
				if (ud.HidProductId != identity.ProductId) { ud.HidProductId = identity.ProductId; changed = true; }
			}

			if (changed)
			{
				// Keep mapped UserSetting rows aligned with the real hardware ProductGuid and ProductName.
				foreach (var setting in SettingsManager.UserSettings.ItemsToArraySynchronized())
				{
					if (setting.InstanceGuid != ud.InstanceGuid)
						continue;
					if (setting.ProductGuid != ud.ProductGuid)
						setting.ProductGuid = ud.ProductGuid;
					if (setting.ProductName != ud.ProductName)
						setting.ProductName = ud.ProductName;
				}
			}
			return changed;
		}

		/// <summary>
		/// True when a device is eligible to be uploaded to x360ce's online cloud database and TOP Controllers list.
		/// A hardware-backed plugin device (such as Fitness Machine) is NOT ready until connected to a real physical machine.
		/// </summary>
		public static bool IsReadyForCloudUpload(UserDevice ud, PluginManager manager)
		{
			if (ud == null)
				return false;
			if (ud.InputSourceType != (int)InputSourceType.Plugin)
				return true;
			var type = FindType(ud, manager);
			if (type == null)
				return false;
			if (!type.RequiresHardwareIdentity)
				return true;
			return ud.ProductGuid != Guid.Empty
				&& ud.ProductGuid != type.ProductGuid
				&& !string.IsNullOrWhiteSpace(ud.ProductName)
				&& !string.Equals(ud.ProductName, type.Name, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>True when a UserSetting's device is ready to be uploaded to the cloud.</summary>
		public static bool IsSettingReadyForCloudUpload(UserSetting setting, PluginManager manager)
		{
			if (setting == null)
				return false;
			var ud = SettingsManager.GetDevice(setting.InstanceGuid);
			return ud == null || IsReadyForCloudUpload(ud, manager);
		}

		/// <summary>
		/// Unchecks/disables the mapped controller setting(s) for <paramref name="instanceGuid"/> so the engine disconnects
		/// immediately until the user explicitly ticks Enabled again.
		/// </summary>
		public static bool DisableDevice(Guid instanceGuid)
		{
			if (instanceGuid == Guid.Empty)
				return false;
			var changed = false;
			foreach (var setting in SettingsManager.UserSettings.ItemsToArraySynchronized())
			{
				if (setting.InstanceGuid != instanceGuid || !setting.IsEnabled)
					continue;
				setting.IsEnabled = false;
				changed = true;
			}
			return changed;
		}

		/// <summary>A new device of this kind for the device list, named "{Name} {n}", with the controls the plugin said it has.</summary>
		/// <remarks>
		/// n is the lowest number whose name and instance id no listed device has. The id is made from the person's profile,
		/// the kind and n (<see cref="InstanceGuidOf"/>), so a device removed and added again gets its id back, and with it
		/// what was kept for it, while another person's device of the same name has another, so settings shared or uploaded
		/// never meet a device of theirs. A device saved before keeps the id it has.
		/// </remarks>
		public static UserDevice NewUserDevice(PluginDeviceType type)
		{
			// Snapshot once: the interface thread can add or remove devices meanwhile.
			var existing = SettingsManager.UserDevices.ItemsToArraySynchronized();
			var profileId = SettingsManager.Options.ProfileId;
			string instanceName;
			Guid instanceGuid;
			for (var i = 1; ; i++)
			{
				instanceName = type.Name + " " + i;
				instanceGuid = InstanceGuidOf(profileId, type.ProductGuid, i);
				if (!existing.Any(x => x.InstanceName == instanceName || x.InstanceGuid == instanceGuid))
					break;
			}
			var ud = new UserDevice();
			ud.InputSourceType = (int)InputSourceType.Plugin;
			ud.DevManufacturer = VendorOf(type);
			ud.HidManufacturer = ud.DevManufacturer;
			ud.InstanceGuid = instanceGuid;
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

		/// <summary>The instance id of the n-th device of a kind for one person: the same every time, another for another person, kind or number.</summary>
		/// <param name="profileId">The person's profile (<see cref="Options.ProfileId"/>); empty before it is known, as in a test, which gives the ids every empty profile gets.</param>
		/// <param name="productGuid">The kind of device.</param>
		/// <param name="number">The n of "{Name} {n}".</param>
		public static Guid InstanceGuidOf(Guid profileId, Guid productGuid, int number)
		{
			var bytes = profileId.ToByteArray().Concat(productGuid.ToByteArray()).Concat(BitConverter.GetBytes(number)).ToArray();
			return JocysCom.ClassLibrary.Security.MD5Helper.GetGuid(bytes);
		}

		/// <summary>Who makes a kind of device, as the Vendor Name columns show it: x360ce for a plugin inside x360ce, otherwise the plugin's name.</summary>
		public static string VendorOf(PluginDeviceType type)
		{
			return type.File.Embedded ? "x360ce" : type.File.Name;
		}

		/// <summary>
		/// Gives a plugin row the names its kind has now. A kind renamed renames the rows saved under its old name, its
		/// number kept: "Remote Controller 1" becomes "Remote MCP Controller 1", unless that name is taken.
		/// </summary>
		static void Name(UserDevice ud, PluginDeviceType type)
		{
			// The Devices page shows the HID manufacturer, a controller tab the device's; a plugin device has both.
			var vendor = VendorOf(type);
			if (ud.DevManufacturer != vendor)
				ud.DevManufacturer = vendor;
			if (ud.HidManufacturer != vendor)
				ud.HidManufacturer = vendor;
			if (ud.ProductName == type.Name)
				return;
			var old = ud.ProductName + " ";
			if (!string.IsNullOrEmpty(ud.ProductName) && ud.InstanceName != null && ud.InstanceName.StartsWith(old, StringComparison.Ordinal))
			{
				var instanceName = type.Name + " " + ud.InstanceName.Substring(old.Length);
				if (!SettingsManager.UserDevices.ItemsToArraySynchronized().Any(x => x != ud && x.InstanceName == instanceName))
				{
					if (ud.HidDescription == ud.InstanceName)
						ud.HidDescription = instanceName;
					ud.InstanceName = instanceName;
				}
			}
			ud.ProductName = type.Name;
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

		/// <summary>Marks each plugin row online while its kind of device is loaded and working, and gives it its kind's names.</summary>
		/// <remarks>
		/// A row of a kind a plugin adds, written down as DirectInput before plugins existed (the Test Device), is made a
		/// plugin row here, so its mappings keep working. Runs on the device refresh thread; a change is marshalled to the
		/// thread that owns the list, which locks it there.
		/// </remarks>
		public static void UpdateOnline(PluginManager manager)
		{
			foreach (var ud in SettingsManager.UserDevices.ItemsToArraySynchronized())
			{
				var type = FindType(ud, manager);
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
				// Preserve real hardware manufacturer/product name once a hardware-backed plugin device has identified a real machine.
				var hasRealHardwareIdentity = type != null && type.RequiresHardwareIdentity && ud.ProductGuid != type.ProductGuid;
				if (online && !hasRealHardwareIdentity)
					Name(ud, type);
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
			Force = device as IPluginForceFeedback;
			Identity = device as IPluginDeviceIdentity;
			TelemetryConsumer = device as IPluginTelemetryConsumer;
		}

		public PluginDeviceType Type { get; }
		public IPluginDevice Device { get; }
		public readonly PluginState State = new PluginState();

		/// <summary>The device as one that takes the game's force feedback, or null when its plugin takes none.</summary>
		public IPluginForceFeedback Force { get; }

		/// <summary>The device as one that reports real connected hardware identity, or null when static.</summary>
		public IPluginDeviceIdentity Identity { get; }

		/// <summary>The device as one that consumes live game telemetry, or null when its plugin uses none.</summary>
		public IPluginTelemetryConsumer TelemetryConsumer { get; }

		/// <summary>Last hardware identity reference applied to the UserDevice (reference-compared with zero allocation on the hot path).</summary>
		internal PluginDeviceIdentity AppliedIdentity;

		/// <summary>The large motor the device was given last, 0 to 65535, or -1 before it was given any.</summary>
		int _givenLarge = -1;

		/// <summary>The small motor the device was given last, 0 to 65535, or -1 before it was given any.</summary>
		int _givenSmall = -1;

		/// <summary>Gives the device the force its game asks for, when that is not what it was given last.</summary>
		/// <remarks>
		/// Called on the engine thread before each read of a device that takes force, so while the force stands it compares
		/// two numbers. The first call always gives, so a new device learns there is no force as well.
		/// </remarks>
		/// <param name="largeMotor">The large motor, 0 to 65535.</param>
		/// <param name="smallMotor">The small motor, 0 to 65535.</param>
		public void SetForce(int largeMotor, int smallMotor)
		{
			if (largeMotor == _givenLarge && smallMotor == _givenSmall)
				return;
			_givenLarge = largeMotor;
			_givenSmall = smallMotor;
			Force.SetForce(largeMotor, smallMotor);
		}

		/// <summary>The store's version the device was last given, or -1 before it was given anything.</summary>
		int _appliedVersion = -1;

		/// <summary>The text of each value the device was given, by setting.</summary>
		readonly Dictionary<string, string> _applied = new Dictionary<string, string>();

		/// <summary>Gives the device the values kept for it that it does not have yet.</summary>
		/// <remarks>
		/// Called on the engine thread before each read, so a plugin's settings are set on the thread that reads it and
		/// need no lock. While nothing changed it compares one number. A setting with no kept value is given the value a
		/// new device starts with, so a reset takes back what was set before. What a setter throws reaches the caller.
		/// </remarks>
		public void ApplySettings(PluginSettings store, Guid instanceGuid)
		{
			var version = store.Version;
			if (version == _appliedVersion)
				return;
			_appliedVersion = version;
			foreach (var setting in Type.Settings)
			{
				if (setting.Kind == PluginSettingKind.Status)
					continue;
				var text = store.Get(instanceGuid, setting.Name) ?? setting.DefaultText;
				string applied;
				// What the device started with counts as given.
				if (!_applied.TryGetValue(setting.Name, out applied))
					applied = setting.DefaultText;
				if (text == applied)
					continue;
				setting.Write(Device, text);
				_applied[setting.Name] = text;
			}
		}

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
