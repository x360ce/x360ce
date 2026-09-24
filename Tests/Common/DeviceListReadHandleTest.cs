// @under-test: App.v4/Common/DInput/DInputHelper.Step1.UpdateDevices.cs
// @area: devices   @layer: integration-db
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using x360ce.App;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// Reading the device list again and again must not leave devices open behind it.
	/// </summary>
	/// <remarks>
	/// Every device that comes or goes on the machine starts a read, and a read opens each device it
	/// has not listed yet. The pads this program feeds are never listed, so every read opened them
	/// again, and the device thread set them aside without closing them. With two pads on and a
	/// noisy device nearby, a program left running gained about two handles a second, and nothing
	/// ever gave them back.
	///
	/// This plugs a controller of ours in, so it needs the virtual bus, and takes it away again.
	/// </remarks>
	[TestClass]
	public class DeviceListReadHandleTest
	{
		const int Reads = 20;

		static int HandleCount()
		{
			using (var process = Process.GetCurrentProcess())
				return process.HandleCount;
		}

		/// <summary>Asks for the device list and takes the result in, as the device thread does.</summary>
		static void TakeIn(DInputHelper helper, MethodInfo updateDiDevices, DirectInput manager)
		{
			helper.UpdateDevicesPending = true;
			Ui.WaitFor(() =>
			{
				updateDiDevices.Invoke(helper, new object[] { manager });
				return helper.UpdateDevicesPending ? null : "taken in";
			}, TimeSpan.FromSeconds(30), "the device list to be read and taken in");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Reading the device list with our pad on the bus leaves no handles behind")]
		public void Reading_the_device_list_with_our_pad_on_the_bus_leaves_no_handles_behind()
		{
			if (!ViGEmClient.isVBusExists(true))
				Assert.Inconclusive("The virtual bus is not installed on this machine.");
			var updateDiDevices = typeof(DInputHelper).GetMethod("UpdateDiDevices", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(updateDiDevices, "DInputHelper.UpdateDiDevices was not found.");
			var listed = SettingsManager.UserDevices.ItemsToArraySyncronized();
			var padsBefore = XInputPlaces.VirtualHardwareNow();
			var client = ViGEmClient.Current;
			if (client.Targets == null)
			{
				client.Targets = new Xbox360Controller[4];
				for (var i = 0; i < 4; i++)
					client.Targets[i] = new Xbox360Controller(client);
			}
			var helper = new DInputHelper();
			var manager = new DirectInput();
			try
			{
				Assert.IsTrue(client.PlugIn(1), "The test could not plug a controller in.");
				Ui.WaitFor(() => XInputPlaces.VirtualHardwareNow().Except(padsBefore).Any() ? "arrived" : null,
					TimeSpan.FromSeconds(15), "our controller to arrive on the bus");
				// The first read lists the machine's own devices, which stay open as the program keeps them.
				TakeIn(helper, updateDiDevices, manager);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				var before = HandleCount();
				for (var i = 0; i < Reads; i++)
					TakeIn(helper, updateDiDevices, manager);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				var grown = HandleCount() - before;
				Console.WriteLine("{0} reads with our controller on the bus grew the handle count by {1}.", Reads, grown);
				Assert.IsTrue(grown < Reads, string.Format(
					"{0} reads left {1} more handles open. Each read is opening a device and not closing it.", Reads, grown));
			}
			finally
			{
				client.UnPlug(1);
				ViGEmClient.DisposeCurrent();
				foreach (var ud in SettingsManager.UserDevices.ItemsToArraySyncronized().Except(listed).ToArray())
				{
					lock (SettingsManager.UserDevices.SyncRoot)
						SettingsManager.UserDevices.Items.Remove(ud);
					if (ud.Device != null)
						ud.Device.Dispose();
				}
				helper.Dispose();
				manager.Dispose();
			}
		}
	}
}
