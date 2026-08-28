// @under-test: App.v4/Common/DInput/DInputHelper.Step1.UpdateDevices.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// The order two lookups happen in while a controller is being read.
	/// </summary>
	/// <remarks>
	/// A controller is described twice by Windows: as a device node, and as the interface through
	/// which it is reached. The interface says more, and is the only one of the two that supplies the
	/// identifier the device node is found by.
	///
	/// Reading the node first therefore looks it up by an identifier nothing has filled in yet. The
	/// lookup finds nothing, the fields it feeds are cleared, and the row shows blanks or the plain
	/// DirectInput name. It only corrects itself if some later pass happens to run, so which pass ran
	/// decided what each list said about the same controller.
	///
	/// Nothing in the method's shape stops the two being swapped back, and swapping them compiles and
	/// runs. This is what says they must not be.
	/// </remarks>
	[TestClass]
	public class DeviceRefreshOrderTest
	{

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The interface is read before the device it identifies")]
		public void The_interface_is_read_before_the_device_it_identifies()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Common", "DInput", "DInputHelper.Step1.UpdateDevices.cs");
			Assert.IsTrue(File.Exists(path), path + " was not found.");
			var text = File.ReadAllText(path);

			var readsInterface = text.IndexOf("ud.LoadHidDeviceInfo(hid);");
			var findsDevice = text.IndexOf("allDevices.FirstOrDefault(x => x.DeviceId == ud.HidDeviceId)");
			Assert.AreNotEqual(-1, readsInterface, "The interface is no longer read where this expects.");
			Assert.AreNotEqual(-1, findsDevice, "The device is no longer found where this expects.");
			Assert.IsTrue(readsInterface < findsDevice,
				"The device is looked up by HidDeviceId before the interface has supplied it, so a " +
				"controller which has just been plugged in reads as blank until a later pass.");
		}

	}
}
