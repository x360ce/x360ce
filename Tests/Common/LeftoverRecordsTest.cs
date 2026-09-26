// @under-test: App.v4/Common/DInput/VirtualDriverInstaller.cs, App.v4/Issues/LeftoverVirtualPadsIssue.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// The records Windows keeps of virtual controllers no longer present are read only when asked
	/// for, so the device thread and the places cache never walk them, and when they are asked for
	/// they come on top of the present controllers, never instead of them.
	/// </summary>
	[TestClass]
	public class LeftoverRecordsTest
	{
		[TestMethod, TestCategory("devices")]
		[Description("The controller tree with records is a superset of the tree without, and every extra device is one Windows says is not present")]
		public void Records_come_on_top_of_the_present_controllers()
		{
			var present = VirtualDriverInstaller.ReadControllerTree();
			var withRecords = VirtualDriverInstaller.ReadControllerTree(true);
			var presentIds = present.Select(x => x.DeviceId).ToArray();
			var missing = presentIds.Except(withRecords.Select(x => x.DeviceId), StringComparer.OrdinalIgnoreCase).ToArray();
			Assert.AreEqual(0, missing.Length, "Present controllers dropped when records were asked for: " + string.Join(", ", missing));
			var extra = withRecords.Where(x => !presentIds.Contains(x.DeviceId, StringComparer.OrdinalIgnoreCase)).ToArray();
			// A record brings its ancestors with it, up to the root, and those are present: the hub a
			// controller once hung from is still there. A present device that is nobody's ancestor is.
			var byId = withRecords.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
			var ancestors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var record in extra.Where(x => !x.IsPresent))
			{
				var current = record;
				while (current != null && !string.IsNullOrEmpty(current.ParentDeviceId) && ancestors.Add(current.ParentDeviceId))
					byId.TryGetValue(current.ParentDeviceId, out current);
			}
			var strangers = extra.Where(x => x.IsPresent && !ancestors.Contains(x.DeviceId)).ToArray();
			Assert.AreEqual(0, strangers.Length, "Present devices that only the read with records found and that no record descends from: "
				+ string.Join(", ", strangers.Select(x => x.DeviceId)));
			Console.WriteLine("{0} present, {1} records, {2} ancestors of records", present.Length, extra.Count(x => !x.IsPresent), extra.Count(x => x.IsPresent));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Every leftover that is not present is a record of a virtual controller, never of something else")]
		public void A_record_counted_as_a_leftover_is_a_virtual_controller()
		{
			var leftovers = VirtualDriverInstaller.GetLeftoverVirtualPads();
			var strangers = leftovers
				.Where(x => !x.IsPresent)
				.Where(x => !x.DeviceId.StartsWith("USB\\VID_045E&PID_028E", StringComparison.OrdinalIgnoreCase)
					&& !VirtualDriverInstaller.CarriesInputGroup(x.DeviceId))
				.ToArray();
			Assert.AreEqual(0, strangers.Length, "Records that are not virtual controllers: "
				+ string.Join(", ", strangers.Select(x => x.DeviceId)));
			Console.WriteLine("{0} leftovers, {1} of them records", leftovers.Length, leftovers.Count(x => !x.IsPresent));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A second look at the leftovers with nothing changed does not read the records again")]
		public void A_second_look_with_nothing_changed_does_not_read_the_records_again()
		{
			// The issue check asks every few seconds. Reading the records is about ten milliseconds each,
			// and a machine where many runs ended badly keeps hundreds: seconds, every few seconds, under
			// the lock the device list read waits on.
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var first = VirtualDriverInstaller.GetLeftoverVirtualPads();
			var firstMs = watch.ElapsedMilliseconds;
			watch.Restart();
			var second = VirtualDriverInstaller.GetLeftoverVirtualPads();
			var secondMs = watch.ElapsedMilliseconds;
			Console.WriteLine("first look {0} ms, second {1} ms, {2} leftovers", firstMs, secondMs, first.Length);
			Assert.AreSame(first, second, "The second look read the machine again although nothing had changed.");
			Assert.IsTrue(secondMs < 500, "The second look took " + secondMs + " ms; asking which ids exist should take milliseconds.");
		}
	}
}
