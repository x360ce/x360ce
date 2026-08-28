// @under-test: App.v4/Common/DInput/DInputHelper.Step1.UpdateDevices.cs, App.v4/MainForm.cs
// @area: engine   @layer: unit
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// What Windows has to say before the program reads every device on the machine again.
	/// </summary>
	/// <remarks>
	/// Reading every device takes about a second, and it happens on the thread that polls
	/// controllers, so each one costs a second of controller processing. The rate drops from a
	/// thousand a second to one or two while it runs.
	///
	/// Windows sends a device change for any device node change on the whole machine, over and over,
	/// for devices that have nothing to do with controllers. Acting on that message is what took the
	/// rate down, twice: it was removed once, then a second handler somewhere else put it back.
	/// Measuring the rate did not catch either, because a test machine plugs nothing in while it
	/// measures. So the rule is held here instead, and so is the fact that only one place applies it.
	/// </remarks>
	[TestClass]
	public class DeviceChangeRateTest
	{

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A device node change does not make the program read every device")]
		public void A_device_node_change_does_not_make_the_program_read_every_device()
		{
			Assert.IsFalse(DInputHelper.IsDeviceListChange(DBT.DBT_DEVNODES_CHANGED),
				"Reading every device again in answer to a machine-wide node change costs a second of " +
				"controller processing, and Windows sends that message constantly.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A controller arriving or leaving does make it read the list again")]
		public void A_controller_arriving_or_leaving_does_make_it_read_the_list_again()
		{
			// The other half of the rule. Ignoring everything would be cheap and would also mean a
			// controller plugged in never appears until the program is started again.
			Assert.IsTrue(DInputHelper.IsDeviceListChange(DBT.DBT_DEVICEARRIVAL));
			Assert.IsTrue(DInputHelper.IsDeviceListChange(DBT.DBT_DEVICEREMOVECOMPLETE));
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Only one place turns a device message into a device read")]
		public void Only_one_place_turns_a_device_message_into_a_device_read()
		{
			// The rule above was already correct when the rate collapsed. What went wrong was a second
			// handler, in another file, applying its own looser version of it. One rule is only one rule
			// while one place applies it.
			var sources = Directory
				.GetFiles(Path.Combine(Ui.RepoRoot.FullName, "App.v4"), "*.cs", SearchOption.AllDirectories)
				.Where(x => !x.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
				.Select(x => new { Path = x, Text = File.ReadAllText(x) })
				.ToArray();

			var listeners = sources
				.Where(x => x.Text.Contains("WM_DEVICECHANGE") || x.Text.Contains("DeviceChanged +="))
				.Select(x => Path.GetFileName(x.Path))
				.OrderBy(x => x)
				.ToArray();
			Assert.AreEqual(1, listeners.Length,
				"Device change messages are answered in more than one place: " + string.Join(", ", listeners)
				+ ". Each one reads every device on the machine, and they cannot be kept in step.");

			var noisy = sources
				.Where(x => x.Text.Contains("DBT_DEVNODES_CHANGED"))
				.Select(x => Path.GetFileName(x.Path))
				.ToArray();
			Assert.AreEqual(0, noisy.Length,
				"The machine-wide node change is named in " + string.Join(", ", noisy)
				+ ". Windows sends it constantly, so anything that acts on it stops controller "
				+ "processing for about a second at a time.");
		}

	}
}
