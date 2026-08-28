// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: integration-db
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>Whether a controller the bus calls attached actually exists in Windows.</summary>
	[TestClass]
	public class VirtualPadRealityTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("A controller the bus reports is a controller Windows has")]
		public void A_controller_the_bus_reports_is_one_windows_has()
		{
			if (!ViGEmClient.isVBusExists(true))
				Assert.Inconclusive("The virtual bus is not installed on this machine.");
			var client = ViGEmClient.Current;
			if (client.Targets == null)
			{
				client.Targets = new Xbox360Controller[4];
				for (var i = 0; i < 4; i++)
					client.Targets[i] = new Xbox360Controller(client);
			}
			for (uint i = 1; i <= 4; i++)
				client.UnPlug(i);
			try
			{
				var plugged = client.PlugIn(1);
				var serial = client.Targets[0].Serial;
				var attached = client.IsControllerConnected(1);
				Console.WriteLine("PlugIn returned {0}, serial {1}, bus says attached {2}", plugged, serial, attached);

				// Given time, because Windows builds the device after the bus accepts it.
				string found = null;
				var until = DateTime.UtcNow.AddSeconds(10);
				while (found == null && DateTime.UtcNow < until)
				{
					var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
					found = all
						.Where(x => VirtualDriverInstaller.TrailingNumber(x.DeviceId) == serial)
						.Select(x => x.DeviceId)
						.FirstOrDefault();
					if (found == null)
						System.Threading.Thread.Sleep(500);
				}
				Console.WriteLine("windows device for serial {0}: {1}", serial, found ?? "NONE");

				Assert.IsTrue(attached, "The bus does not report the controller it just accepted.");
				Assert.IsNotNull(found,
					"The bus reports a controller that Windows does not have. That is the green light " +
					"with no controller behind it: nothing downstream can work, and nothing says why.");
			}
			finally
			{
				client.UnPlug(1);
			}
		}
	}
}
