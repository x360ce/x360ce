// @under-test: App.v4/Plugins/PluginManager.cs, App.v4/Common/CertificateHelper.cs
// @area: plugins   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using x360ce.App.Plugins;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>A plugin device for these tests: two axes, a hat and three buttons.</summary>
	[System.ComponentModel.Description("A device for the plugin tests.")]
	public class SamplePluginDevice : IPluginDevice
	{
		public static readonly Guid Product = new Guid("6a1b3c55-0d0e-4f53-9a51-3f0b6c1d2e77");
		public string Name { get { return "Sample Plugin Device"; } }
		public Guid ProductGuid { get { return Product; } }
		public int AxisCount { get { return 2; } }
		public int SliderCount { get { return 0; } }
		public int PovCount { get { return 1; } }
		public int ButtonCount { get { return 3; } }
		public void Read(PluginState state)
		{
			state.Axes[0] = PluginState.Middle;
			state.Buttons[1] = true;
		}
	}

	/// <summary>A plugin device that recommends its mapping as a whole SDL row, another device's GUID and name in it: a stick and a button.</summary>
	public class MappedPluginDevice : IPluginDevice, IPluginMapping
	{
		public static readonly Guid Product = new Guid("3c6f0a1e-8d2b-4e7a-b5c9-1f2e3d4c5b6a");
		public string Name { get { return "Mapped Plugin Device"; } }
		public Guid ProductGuid { get { return Product; } }
		public int AxisCount { get { return 2; } }
		public int SliderCount { get { return 0; } }
		public int PovCount { get { return 0; } }
		public int ButtonCount { get { return 1; } }
		public string Mapping { get { return " 03000000341200007856000000000000,Other Pad,a:b0,b:b5,leftx:a0,lefty:a1,platform:Windows, "; } }
		public bool Ready { get; set; }
		public void Read(PluginState state) { }
	}

	/// <summary>A plugin device that cannot be made, as one whose hardware is missing might be written.</summary>
	public class ThrowingPluginDevice : IPluginDevice
	{
		public ThrowingPluginDevice() { throw new InvalidOperationException("No hardware."); }
		public string Name { get { return "Throwing"; } }
		public Guid ProductGuid { get { return Guid.Empty; } }
		public int AxisCount { get { return 0; } }
		public int SliderCount { get { return 0; } }
		public int PovCount { get { return 0; } }
		public int ButtonCount { get { return 0; } }
		public void Read(PluginState state) { }
	}

	/// <summary>Not a kind of device: it cannot be made.</summary>
	public abstract class AbstractPluginDevice : IPluginDevice
	{
		public abstract string Name { get; }
		public Guid ProductGuid { get { return Guid.Empty; } }
		public int AxisCount { get { return 0; } }
		public int SliderCount { get { return 0; } }
		public int PovCount { get { return 0; } }
		public int ButtonCount { get { return 0; } }
		public void Read(PluginState state) { }
	}

	/// <summary>
	/// Which plugins load, and what x360ce learns from them. A plugin signed by the publisher of x360ce loads by itself;
	/// any other waits until the person allows third-party plugins, and the reason it waits is kept to show them.
	/// </summary>
	[TestClass]
	public class PluginManagerTest
	{
		const string Jocys = "CN=Jocys.com Ltd, O=Jocys.com Ltd, C=GB";

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A plugin signed like x360ce loads by itself; any other only when third-party plugins are allowed")]
		public void Trust_follows_the_signer_and_the_setting()
		{
			Assert.AreEqual(PluginTrust.SameSigner, PluginManager.Trust(Jocys, Jocys, false));
			Assert.AreEqual(PluginTrust.SameSigner, PluginManager.Trust(Jocys, Jocys, true));
			Assert.AreEqual(PluginTrust.Refused, PluginManager.Trust("CN=Someone Else", Jocys, false));
			Assert.AreEqual(PluginTrust.ThirdParty, PluginManager.Trust("CN=Someone Else", Jocys, true));
			Assert.AreEqual(PluginTrust.Refused, PluginManager.Trust(null, Jocys, false), "Unsigned.");
			Assert.AreEqual(PluginTrust.ThirdParty, PluginManager.Trust(null, Jocys, true), "Unsigned, allowed.");
			// A build that is not signed itself trusts no signature as its own.
			Assert.AreEqual(PluginTrust.Refused, PluginManager.Trust(Jocys, null, false));
			Assert.AreEqual(PluginTrust.Refused, PluginManager.Trust(null, null, false));
		}

		[TestMethod, TestCategory("plugins")]
		[Description("The signer of a signed .NET Framework library is read, and an unsigned library has none")]
		public void The_signer_is_read_from_the_signature()
		{
			var signed = typeof(Uri).Assembly.Location;
			var signer = x360ce.App.CertificateHelper.GetSigner(signed);
			if (signer == null)
				Assert.Inconclusive("System.dll on this machine carries no signature to read: " + signed);
			StringAssert.Contains(signer, "Microsoft", signed);
			Assert.IsNull(x360ce.App.CertificateHelper.GetSigner(typeof(PluginManagerTest).Assembly.Location), "The test library is not signed.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Every public class that implements IPluginDevice and can be made is a kind of device; one that throws is kept, switched off, with the reason")]
		public void Device_types_are_found_and_described()
		{
			var file = new PluginFile("test", false);
			var types = PluginManager.FindDeviceTypes(typeof(SamplePluginDevice).Assembly, file);
			Assert.IsFalse(types.Any(x => x.Type == typeof(AbstractPluginDevice)), "An abstract class is not a kind of device.");

			var sample = types.Single(x => x.Type == typeof(SamplePluginDevice));
			Assert.AreEqual("Sample Plugin Device", sample.Name);
			Assert.AreEqual(SamplePluginDevice.Product, sample.ProductGuid);
			Assert.AreEqual(2, sample.AxisCount);
			Assert.AreEqual(1, sample.PovCount);
			Assert.AreEqual(3, sample.ButtonCount);
			Assert.AreEqual("A device for the plugin tests.", sample.Description);
			Assert.IsFalse(sample.Faulted);
			Assert.AreSame(file, sample.File);

			var throwing = types.Single(x => x.Type == typeof(ThrowingPluginDevice));
			Assert.IsTrue(throwing.Faulted);
			StringAssert.Contains(throwing.FaultReason, "No hardware.");
			Assert.IsNull(sample.Mapping, "A device that recommends no mapping has one.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("mapping"), TestCategory("critical")]
		[Description("A plugin's recommended mapping is read as it loads, is no setting, and maps its device as SDL reads the row, under the device's own GUID and name, leaving out what the device lacks")]
		public void A_recommended_mapping_maps_the_device_as_sdl_reads_it()
		{
			var types = PluginManager.FindDeviceTypes(typeof(MappedPluginDevice).Assembly, new PluginFile("test", false));
			var type = types.Single(x => x.Type == typeof(MappedPluginDevice));
			Assert.IsFalse(type.Faulted, type.FaultReason);
			StringAssert.StartsWith(type.Mapping, "03000000341200007856000000000000,Other Pad,", "The mapping was not read, or not trimmed.");
			CollectionAssert.AreEqual(new[] { "Ready" }, type.Settings.Select(x => x.Name).ToArray(), "A member of the contract's interfaces is a setting.");
			var ud = PluginDevices.NewUserDevice(type);
			var mapping = x360ce.App.AutoMapHelper.GetPluginMapping(ud, type);
			Assert.IsNotNull(mapping, "The row mapped nothing.");
			Assert.AreEqual(x360ce.Engine.SdlDevice.From(ud).Guid, mapping.Mapping.Guid.Text, "The row keeps another device's GUID.");
			Assert.AreEqual("Mapped Plugin Device", mapping.Mapping.Name);
			Assert.AreEqual("1", mapping.Setting.ButtonA);
			Assert.AreEqual("a1", mapping.Setting.LeftThumbAxisX);
			Assert.AreEqual("a-2", mapping.Setting.LeftThumbAxisY, "SDL's Y grows downward, the Xbox controller's upward.");
			Assert.IsTrue(string.IsNullOrEmpty(mapping.Setting.ButtonB), "A button the device lacks was mapped.");
			StringAssert.Contains(mapping.Note, "b:b5", "What was left out is not said.");
			Assert.AreEqual(x360ce.Engine.SdlTranslation.FromPlugin, mapping.From[x360ce.Engine.MapCode.ButtonA]);
			var sample = types.Single(x => x.Type == typeof(SamplePluginDevice));
			Assert.IsNull(x360ce.App.AutoMapHelper.GetPluginMapping(PluginDevices.NewUserDevice(sample), sample),
				"A device that recommends nothing was given a mapping.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A plugin in a folder of its own waits for permission, says why, and loads once third-party plugins are allowed")]
		public void A_third_party_plugin_waits_for_permission()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.PluginManagerTest." + Guid.NewGuid().ToString("N"));
			var sub = Path.Combine(folder, "SamplePlugin");
			Directory.CreateDirectory(sub);
			try
			{
				// The test library is a plugin as good as any: it has a public class that implements IPluginDevice.
				var dll = Path.Combine(sub, "SamplePlugin.dll");
				File.Copy(typeof(SamplePluginDevice).Assembly.Location, dll);
				var allow = false;
				using (var manager = new PluginManager(folder, Jocys, () => allow, null))
				{
					manager.Scan();
					var file = manager.Files.Single();
					Assert.AreEqual(dll, file.Source);
					Assert.AreEqual("SamplePlugin", file.Name);
					Assert.AreEqual(PluginFileState.WaitingForPermission, file.State);
					StringAssert.Contains(file.Reason, "Not signed");
					StringAssert.Contains(file.Reason, "Allow third-party plugins");
					Assert.AreEqual(0, manager.DeviceTypes.Length);

					allow = true;
					manager.Scan();
					Assert.AreEqual(1, manager.Files.Length, "The same file, now loaded, not a second one.");
					Assert.AreEqual(PluginFileState.Loaded, file.State);
					Assert.IsNull(file.Reason);
					var type = manager.FindDeviceType(SamplePluginDevice.Product);
					Assert.IsNotNull(type, "The plugin's kind of device is listed.");
					Assert.AreEqual("Sample Plugin Device", type.Name);

					// Found again by a later scan, it is not loaded twice.
					manager.Scan();
					Assert.AreEqual(1, manager.Files.Length);
				}
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Only the DLL named like its folder is a plugin; a DLL beside it is its dependency, and a DLL loose in the plugins folder is listed with where it belongs")]
		public void A_plugin_is_the_dll_named_like_its_folder()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.PluginManagerTest." + Guid.NewGuid().ToString("N"));
			var sub = Path.Combine(folder, "SamplePlugin");
			Directory.CreateDirectory(sub);
			try
			{
				var plugin = typeof(SamplePluginDevice).Assembly.Location;
				File.Copy(plugin, Path.Combine(sub, "SamplePlugin.dll"));
				File.Copy(plugin, Path.Combine(sub, "Helper.dll"));
				File.Copy(plugin, Path.Combine(folder, "Loose.dll"));
				using (var manager = new PluginManager(folder, Jocys, () => true, null))
				{
					manager.Scan();
					CollectionAssert.AreEquivalent(new[] { "Loose", "SamplePlugin" }, manager.Files.Select(x => x.Name).ToArray(),
						"The DLL beside the plugin was taken for a plugin, or a plugin was missed.");
					Assert.AreEqual(PluginFileState.Loaded, manager.Files.Single(x => x.Name == "SamplePlugin").State);
					var loose = manager.Files.Single(x => x.Name == "Loose");
					Assert.AreEqual(PluginFileState.Failed, loose.State);
					StringAssert.Contains(loose.Reason, @"Plugins\Loose\Loose.dll");

					manager.Scan();
					Assert.AreEqual(2, manager.Files.Length, "A later scan lists nothing twice.");
				}
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Built-in plugins are enabled and loaded by default, can be disabled and re-enabled live, and stay unloaded across restarts while disabled")]
		public void Built_in_plugins_load_by_default_and_can_be_disabled_and_reenabled()
		{
			var appAssembly = typeof(PluginManager).Assembly;
			using (var manager = new PluginManager(null, null, () => false, appAssembly))
			{
				manager.LoadEmbedded();
				Assert.IsTrue(manager.Files.Length >= 4, "Expected embedded built-in plugins.");
				Assert.IsTrue(manager.Files.All(f => f.Embedded && f.Enabled && f.State == PluginFileState.Loaded),
					"All built-in plugins must be enabled and loaded by default.");
				Assert.IsTrue(manager.TelemetryTypes.Any(t => t.File.Name == "WheelTelemetry"),
					"WheelTelemetry provider should be active by default.");

				manager.SetEnabled("WheelTelemetry", false);
				var wt = manager.Files.Single(f => f.Name == "WheelTelemetry");
				Assert.IsFalse(wt.Enabled);
				Assert.AreEqual(PluginFileState.Disabled, wt.State);
				Assert.IsFalse(manager.TelemetryTypes.Any(t => t.File.Name == "WheelTelemetry"),
					"Disabling WheelTelemetry must remove its provider from TelemetryTypes immediately.");

				manager.SetEnabled("WheelTelemetry", true);
				Assert.IsTrue(wt.Enabled);
				Assert.AreEqual(PluginFileState.Loaded, wt.State);
				Assert.IsTrue(manager.TelemetryTypes.Any(t => t.File.Name == "WheelTelemetry"),
					"Re-enabling WheelTelemetry must restore its provider immediately.");
			}

			// Simulate next start with WheelTelemetry listed in DisabledPlugins: assembly must not load until re-enabled.
			using (var restarted = new PluginManager(null, null, () => false, appAssembly, name => !string.Equals(name, "WheelTelemetry", StringComparison.OrdinalIgnoreCase)))
			{
				restarted.LoadEmbedded();
				var wt = restarted.Files.Single(f => f.Name == "WheelTelemetry");
				Assert.IsFalse(wt.Enabled);
				Assert.AreEqual(PluginFileState.Disabled, wt.State);
				Assert.IsNull(wt.Assembly, "A plugin disabled at startup must not load its assembly.");
				Assert.AreEqual(0, restarted.TelemetryTypes.Length);

				restarted.SetEnabled("WheelTelemetry", true);
				Assert.IsTrue(wt.Enabled);
				Assert.AreEqual(PluginFileState.Loaded, wt.State);
				Assert.IsNotNull(wt.Assembly, "Enabling a plugin that started disabled must load its assembly.");
				Assert.IsTrue(restarted.TelemetryTypes.Any(t => t.File.Name == "WheelTelemetry"));
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A Fitness Machine device is blocked from cloud upload until connected to a real machine, then adopts its real manufacturer, product name and ProductGuid")]
		public void Fitness_machine_adopts_real_hardware_identity_and_gates_cloud_upload()
		{
			var appAssembly = typeof(PluginManager).Assembly;
			using (var manager = new PluginManager(null, null, () => false, appAssembly))
			{
				manager.LoadEmbedded();
				var fitnessType = manager.DeviceTypes.Single(t => t.File.Name == "FitnessMachine");
				Assert.IsTrue(fitnessType.RequiresHardwareIdentity, "FitnessMachine must require real hardware identity before cloud upload.");

				var ud = PluginDevices.NewUserDevice(fitnessType);
				Assert.IsFalse(PluginDevices.IsReadyForCloudUpload(ud, manager),
					"An unconnected Fitness Machine placeholder must not be ready for cloud upload.");

				// Build a real Bluetooth Device Information identity with PnP ID (VID=0x1234, PID=0x5678).
				var sessionType = fitnessType.Type.Assembly.GetType("x360ce.Plugins.Samples.FitnessSession", true);
				var build = sessionType.GetMethod("BuildHardwareIdentity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				Assert.IsNotNull(build);
				var pnp = new byte[] { 1, 0x34, 0x12, 0x78, 0x56, 0x01, 0x00 };
				var identity = (PluginDeviceIdentity)build.Invoke(null, new object[] { "E60R-1234", "Tunturi", "E60R", pnp });
				Assert.IsNotNull(identity);
				Assert.IsTrue(identity.IsConnected);
				Assert.AreEqual("Tunturi", identity.Manufacturer);
				Assert.AreEqual("Tunturi E60R", identity.ProductName);
				Assert.AreEqual(0x1234, identity.VendorId);
				Assert.AreEqual(0x5678, identity.ProductId);
				Assert.AreEqual(new Guid("56781234-0000-0000-0000-504944564944"), identity.ProductGuid);

				// Applying real hardware identity updates UserDevice and makes it ready for cloud / TOP Controllers.
				Assert.IsTrue(PluginDevices.ApplyHardwareIdentity(ud, fitnessType, identity));
				Assert.AreEqual("Tunturi", ud.DevManufacturer);
				Assert.AreEqual("Tunturi", ud.HidManufacturer);
				Assert.AreEqual("Tunturi E60R", ud.ProductName);
				Assert.AreEqual(0x1234, ud.DevVendorId);
				Assert.AreEqual(0x5678, ud.DevProductId);
				Assert.AreEqual(identity.ProductGuid, ud.ProductGuid);
				Assert.AreSame(fitnessType, PluginDevices.FindType(ud, manager),
					"x360ce must still resolve the owning FitnessMachine plugin after ProductGuid switches to the real machine.");
				Assert.IsTrue(PluginDevices.IsReadyForCloudUpload(ud, manager),
					"Once connected with real hardware identity, the machine is ready to upload to the cloud.");

				// Clean up PluginSettings entry written for the temporary test InstanceGuid.
				PluginSettings.Current.Reset(ud.InstanceGuid);
			}
		}
	}
}
