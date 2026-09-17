// @under-test: Engine/JocysCom/IO/DeviceDetector.cs, App.v4/Common/DInput/VirtualDriverInstaller.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// What each way of asking the machine about devices actually costs, measured rather than assumed.
	/// </summary>
	/// <remarks>
	/// Reading every device on the machine is a millisecond a node, and a machine has hundreds; opening
	/// each HID interface for its strings is ten milliseconds more. Done on the device thread that used
	/// to be two to five seconds with no controller polled, and it was asked for on every device arrival
	/// and removal, the program's own virtual controllers included.
	///
	/// The cheap calls are what replaced it: ids alone, interfaces behind a filter that is asked before
	/// anything is opened, and the named devices with their ancestors. This measures each one directly,
	/// so a change that quietly makes one of them read the whole machine again is caught here rather
	/// than as a rate complaint months later. No application is started: these are library calls.
	/// </remarks>
	[TestClass]
	public class DeviceReadBenchmarkTest
	{
		/// <summary>Ids are one call to the configuration manager each; hundreds answer in a few milliseconds.</summary>
		const long IdsMs = 500;

		/// <summary>A filter that accepts nothing opens nothing, so this is the enumeration alone.</summary>
		const long FilteredInterfacesMs = 500;

		/// <summary>One device and its chain up to the root, out of hundreds on the machine.</summary>
		const long OneDeviceMs = 300;

		/// <summary>Ids, then the descriptions of the controller family only. It used to read every node.</summary>
		const long ControllerTreeMs = 1000;

		[TestMethod, TestCategory("performance")]
		[Description("Every device read the program makes costs milliseconds, and none of them reads the whole machine")]
		public void Device_reads_cost_milliseconds_not_seconds()
		{
			int idCount;
			var ids = new string[0];
			var idsMs = BestOfThree("DeviceDetector.GetDeviceIds()", () =>
			{
				ids = DeviceDetector.GetDeviceIds();
				return ids.Length;
			}, out idCount);
			if (idCount == 0)
				Assert.Inconclusive("The machine reports no devices at all, so there is nothing to measure.");
			Assert.IsTrue(idsMs <= IdsMs,
				"Reading the device ids took " + idsMs + " ms for " + idCount + " devices, over " + IdsMs + " ms.");

			int rejectedCount;
			var rejectedMs = BestOfThree("DeviceDetector.GetInterfaces(accept nothing)", () =>
				DeviceDetector.GetInterfaces((deviceId, devicePath) => false).Length, out rejectedCount);
			Assert.AreEqual(0, rejectedCount, "A filter that accepts nothing returned " + rejectedCount + " interfaces.");
			Assert.IsTrue(rejectedMs <= FilteredInterfacesMs,
				"Enumerating the interfaces behind a filter that accepts nothing took " + rejectedMs + " ms, over "
				+ FilteredInterfacesMs + " ms. The filter is asked before an interface is opened, so this is the "
				+ "enumeration alone and nothing should be read.");

			var firstId = ids[0];
			int oneCount;
			var oneMs = BestOfThree("DeviceDetector.GetDevices(one id, with parents)", () =>
				DeviceDetector.GetDevices(new[] { firstId }, true).Length, out oneCount);
			Assert.IsTrue(oneCount >= 1, "Reading one device by id returned nothing for " + firstId + ".");
			Assert.IsTrue(oneMs <= OneDeviceMs,
				"Reading one device and its ancestors took " + oneMs + " ms and returned " + oneCount + " devices, over "
				+ OneDeviceMs + " ms.");

			int treeCount;
			var treeMs = BestOfThree("VirtualDriverInstaller.ReadControllerTree()", () =>
				VirtualDriverInstaller.ReadControllerTree().Length, out treeCount);
			Assert.IsTrue(treeMs <= ControllerTreeMs,
				"Reading the controller tree took " + treeMs + " ms and returned " + treeCount + " devices, over "
				+ ControllerTreeMs + " ms.");
			Assert.IsTrue(treeCount * 2 < idCount,
				"The controller tree returned " + treeCount + " of the machine's " + idCount + " devices. It is a targeted "
				+ "read of the controller family and its ancestors; returning a good part of the machine means it is "
				+ "reading everything again.");
		}

		/// <summary>
		/// One warm-up call, then the best of three, because the first call of any of these pays for
		/// the configuration manager's own first look at the machine and would be timed as the cost of
		/// the call. The best of three is the cost when nothing else is in the way.
		/// </summary>
		/// <param name="what">Printed with the timing.</param>
		/// <param name="read">The call, returning how many it found.</param>
		/// <param name="count">What the last call returned.</param>
		/// <returns>The quickest of the three runs, in milliseconds.</returns>
		static long BestOfThree(string what, Func<int> read, out int count)
		{
			read();
			var best = long.MaxValue;
			var found = 0;
			var runs = Enumerable.Range(0, 3).Select(i =>
			{
				var watch = Stopwatch.StartNew();
				found = read();
				watch.Stop();
				return watch.ElapsedMilliseconds;
			}).ToArray();
			foreach (var run in runs)
				best = Math.Min(best, run);
			count = found;
			Console.WriteLine("{0,-46} {1,5} ms  (runs {2}, found {3})",
				what, best, string.Join("/", runs.Select(x => x.ToString()).ToArray()), found);
			return best;
		}
	}
}
