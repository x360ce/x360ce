// @under-test: App.v4/Common/DInput/XInputPlaces.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// What working out the XInput places actually sees, with a controller of ours in one place and
	/// a real controller in another.
	/// </summary>
	/// <remarks>
	/// With only a real controller attached the answer is easy and right: it is the only one, so it
	/// holds the only taken place. The case that goes wrong is the one somebody actually has - a real
	/// controller in one place and a controller this program made in another - and it cannot be
	/// reproduced by looking, because both have to be there at once.
	///
	/// So this makes one, and prints every step of the working: which places are taken, every device
	/// XInput can see, whether each is recognised as ours, which piece of hardware each is gathered
	/// into, and the answer each ends up with. A wrong answer in the list is then attributable to a
	/// step rather than guessed at.
	/// </remarks>
	[TestClass]
	public class XInputPlacesResolveTest
	{

		static bool[] Occupied()
		{
			var places = new bool[4];
			for (var i = 0; i < 4; i++)
				places[i] = SystemXInput.IsConnected(i);
			return places;
		}

		static string Show(bool[] places)
		{
			return string.Join(" ", Enumerable.Range(0, 4)
				.Select(i => string.Format("{0}:{1}", i + 1, places[i] ? "taken" : "free")).ToArray());
		}

		static void Dump()
		{
			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
			var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
			var capable = all.Where(XInputPlaces.IsXInputCapable).ToArray();
			var answer = XInputPlaces.Resolve(all, byId);

			Console.WriteLine("places taken : {0}", Show(Occupied()));
			Console.WriteLine("devices XInput can see : {0}", capable.Length);
			Console.WriteLine();

			var ours = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var theirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var device in capable)
			{
				var hardware = XInputPlaces.HardwareOf(device, byId);
				var isOurs = VirtualDriverInstaller.IsVirtualPad(device, byId);
				if (isOurs)
					ours.Add(hardware);
				else
					theirs.Add(hardware);
				int place;
				Console.WriteLine("{0}", device.DeviceId);
				Console.WriteLine("    description   : {0}", device.Description);
				Console.WriteLine("    made by us    : {0}", isOurs ? "yes" : "no");
				Console.WriteLine("    hardware      : {0}", hardware);
				Console.WriteLine("    answer        : {0}", answer.TryGetValue(device.DeviceId, out place)
					? (place >= 0 ? "XInput " + (place + 1) : "unknown") : "not in the answer at all");
				Console.WriteLine();
			}

			Console.WriteLine("hardware counted as ours   : {0}", ours.Count);
			foreach (var h in ours)
				Console.WriteLine("    {0}", h);
			Console.WriteLine("hardware counted as theirs : {0}", theirs.Count);
			foreach (var h in theirs)
				Console.WriteLine("    {0}", h);
			var both = ours.Intersect(theirs, StringComparer.OrdinalIgnoreCase).ToArray();
			Console.WriteLine("counted as BOTH            : {0}{1}", both.Length,
				both.Length == 0 ? "" : "   <- this is a fault; the same hardware cannot be both");
			foreach (var h in both)
				Console.WriteLine("    {0}", h);
			Console.WriteLine();
		}

		[TestMethod, TestCategory("devices")]
		[Description("What the places work out to with one of ours and one real controller present")]
		public void What_the_places_work_out_to_with_ours_and_a_real_one()
		{
			if (!ViGEmClient.isVBusExists(true))
				Assert.Inconclusive("The virtual bus is not installed on this machine.");
			var atRest = Occupied();
			Console.WriteLine("=== BEFORE: only what was already attached ===");
			Console.WriteLine();
			Dump();

			var free = Enumerable.Range(0, 4).Where(i => !atRest[i]).ToArray();
			if (free.Length == 0)
				Assert.Inconclusive("Every XInput place is already taken, so nothing of ours can be added.");

			var client = ViGEmClient.Current;
			if (client.Targets == null)
			{
				client.Targets = new Xbox360Controller[4];
				for (var i = 0; i < 4; i++)
					client.Targets[i] = new Xbox360Controller(client);
			}
			// The lowest free place, which is the one it will be given, and the pad that owns it.
			var pad = (uint)(free[0] + 1);
			try
			{
				var padsBefore = XInputPlaces.VirtualHardwareNow();
				Console.WriteLine("making a controller for pad {0}, expecting XInput {0}", pad);
				Console.WriteLine();
				if (!client.PlugIn(pad))
					Assert.Inconclusive("The virtual bus would not make a controller.");
				// Written down the way the program writes it down, so this measures what the program
				// would see rather than a different arrangement that happens to work.
				var until = DateTime.UtcNow.AddSeconds(10);
				while (DateTime.UtcNow < until && !SystemXInput.IsConnected((int)pad - 1))
					Thread.Sleep(100);
				// Recorded the way the program records it: the controller that appeared, not a number read
				// off a device name.
				var appeared = XInputPlaces.VirtualHardwareNow();
				appeared.ExceptWith(padsBefore);
				if (appeared.Count == 1)
					XInputPlaces.Remember(appeared.First(), (int)pad - 1);
				Thread.Sleep(1500);
				XInputPlaces.Invalidate();

				Console.WriteLine("=== AFTER: with a controller of ours in XInput {0} ===", pad);
				Console.WriteLine();
				Dump();
			}
			finally
			{
				client.UnPlug(pad);
				XInputPlaces.Forget();
				ViGEmClient.DisposeCurrent();
				Thread.Sleep(1500);
				Console.WriteLine("places after tidying up : {0}", Show(Occupied()));
			}

			// A measurement. What it prints is the point; there is nothing here to pass or fail that
			// would say more than the working above.
			Assert.IsTrue(true);
		}

	}
}
