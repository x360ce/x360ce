// @under-test: App.v4/Plugins/PluginManager.cs
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
			var signer = PluginManager.GetSigner(signed);
			if (signer == null)
				Assert.Inconclusive("System.dll on this machine carries no signature to read: " + signed);
			StringAssert.Contains(signer, "Microsoft", signed);
			Assert.IsNull(PluginManager.GetSigner(typeof(PluginManagerTest).Assembly.Location), "The test library is not signed.");
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
	}
}
