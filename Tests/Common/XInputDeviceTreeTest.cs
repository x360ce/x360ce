// @under-test: App.v4/Common/DInput/VirtualDriverInstaller.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// What the device tree says about each controller XInput can see, and how much of its XInput
	/// place can be worked out from that alone.
	/// </summary>
	/// <remarks>
	/// A row in the device list should be able to say which XInput place its hardware holds. For a
	/// controller this program made, the place was watched as it arrived and is simply known. For a
	/// real controller nothing reports it: XInput hands out places and never says which device got
	/// which, and the two lists share no key.
	///
	/// What is left is elimination. The places that are taken can be read; the ones ours occupy are
	/// known; so the rest belong to somebody else. With one real controller that names it exactly.
	/// With two it says only that both are in those places, without saying which is in which.
	///
	/// This prints what can be established on the machine it runs on, so the column can be built to
	/// say what is true rather than what would be convenient.
	/// </remarks>
	[TestClass]
	public class XInputDeviceTreeTest
	{
		/// <summary>The piece of hardware a controller device belongs to.</summary>
		/// <remarks>
		/// Walks up until the identifier stops carrying the XInput marker. One controller appears as
		/// a small family - the thing itself, and a face for each way of reading it - and only the
		/// faces carry the marker. So the first ancestor without it is the controller.
		/// </remarks>
		static string HardwareRoot(DeviceInfo device, Dictionary<string, DeviceInfo> byId)
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var current = device;
			while (current != null && !string.IsNullOrEmpty(current.ParentDeviceId) && seen.Add(current.ParentDeviceId))
			{
				DeviceInfo parent;
				if (!byId.TryGetValue(current.ParentDeviceId, out parent))
					break;
				if (!VirtualDriverInstaller.CarriesInputGroup(parent.DeviceId)
					&& !VirtualDriverInstaller.CarriesInputGroup(parent.HardwareIds))
					return parent.DeviceId;
				current = parent;
			}
			return device.DeviceId;
		}

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("Shows what the device tree and the places together can establish")]
		public void What_can_be_established_about_each_controllers_place()
		{
			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
			var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);

			// Anything Windows marks as XInput-capable. The marker is put in the identifier by the
			// driver that serves the XInput face, so it is present on both real and made controllers.
			var capable = all
				.Where(x => VirtualDriverInstaller.CarriesInputGroup(x.HardwareIds)
					|| VirtualDriverInstaller.CarriesInputGroup(x.DeviceId))
				.OrderBy(x => x.DeviceId)
				.ToList();

			var places = new bool[4];
			for (var i = 0; i < 4; i++)
				places[i] = SystemXInput.IsConnected(i);

			Console.WriteLine("places taken : {0}", string.Join(" ", Enumerable.Range(0, 4)
				.Select(i => string.Format("{0}:{1}", i + 1, places[i] ? "taken" : "free")).ToArray()));
			Console.WriteLine("controllers XInput can see : {0}", capable.Count);
			Console.WriteLine();

			// One controller is several devices: the hardware, the face XInput reads, and the face
			// DirectInput reads, each its own entry. Counting entries counts one controller two or
			// three times, so they are gathered by the piece of hardware they all descend from.
			var hardwareOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var realHardware = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var real = new List<DeviceInfo>();
			foreach (var device in capable)
			{
				var ours = VirtualDriverInstaller.IsVirtualPad(device, byId);
				var hardware = HardwareRoot(device, byId);
				hardwareOf[device.DeviceId] = hardware;
				if (!ours)
				{
					real.Add(device);
					realHardware.Add(hardware);
				}
				Console.WriteLine("{0}", device.DeviceId);
				Console.WriteLine("    made by this program : {0}", ours ? "yes" : "no");
				Console.WriteLine("    description          : {0}", device.Description);
				// The chain upward, which is how a controller is told from the family of devices
				// Windows builds around it, and how a made one is told from a real one.
				var chain = new List<string>();
				var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var current = device;
				while (current != null && !string.IsNullOrEmpty(current.ParentDeviceId) && seen.Add(current.ParentDeviceId))
				{
					DeviceInfo parent;
					if (!byId.TryGetValue(current.ParentDeviceId, out parent))
						break;
					chain.Add(parent.DeviceId);
					current = parent;
				}
				Console.WriteLine("    parents              : {0}",
					chain.Count == 0 ? "(none)" : string.Join("  ->  ", chain.ToArray()));
				Console.WriteLine("    the hardware itself  : {0}", hardware);
				Console.WriteLine();
			}

			Console.WriteLine("SUMMARY");
			Console.WriteLine("  places taken            : {0}", places.Count(x => x));
			Console.WriteLine("  entries for real controllers : {0}", real.Count);
			Console.WriteLine("  real controllers, counted as hardware : {0}", realHardware.Count);
			foreach (var h in realHardware)
				Console.WriteLine("      {0}", h);
			// None of ours are plugged in while this runs, so every taken place belongs to somebody
			// else. One real controller and one taken place name each other; more of either leaves
			// the pairing open, and the column should say nothing rather than choose.
			var taken = Enumerable.Range(0, 4).Where(i => places[i]).ToArray();
			Console.WriteLine("  can a real controller be given a place by elimination? : {0}",
				realHardware.Count == 1 && taken.Length == 1
					? "yes - it is the only one, and place " + (taken[0] + 1) + " is the only one taken"
					: realHardware.Count == 0
						? "no real controllers to name"
						: "no - " + realHardware.Count + " controller(s) share " + taken.Length + " place(s)");
		}
	}
}
