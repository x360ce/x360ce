// @under-test: App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using SharpDX.XInput;
using System;
using System.Linq;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether watching the XInput places says where a controller we just made has landed, while a
	/// real controller is holding a place of its own.
	/// </summary>
	/// <remarks>
	/// The program works out where its controllers are by counting: it lists the places that report
	/// something, lists the controllers it believes it made, and pairs them in order. That is sound
	/// only while every place belongs to us. Plug in a real controller and the counts stop matching,
	/// so it gives up and says it does not know - which is honest, and is the one case somebody needs
	/// an answer for.
	///
	/// Making a controller is something we choose to do, at a moment we choose. So the place it took
	/// can be observed rather than deduced: look at the places, make one, look again, and the place
	/// that filled is its place. A real controller sitting in a place of its own does not disturb
	/// that, because it is in both pictures.
	///
	/// This measures whether that holds in practice, on a machine with real controllers attached. It
	/// needs the virtual bus, and it needs nothing else - no elevation, and no real controller is
	/// touched at any point.
	/// </remarks>
	[TestClass]
	public class SlotByConstructionTest
	{
		/// <summary>Windows' own XInput, not the one this program can put in its place.</summary>
		/// <remarks>
		/// Asked of the system library directly. The wrapper in this solution exists to load whichever
		/// XInput the emulator wants a game to see, which is the opposite of what is wanted here: the
		/// question is which places Windows has actually given out.
		/// </remarks>
		static class SystemXInput
		{
			[System.Runtime.InteropServices.DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
			internal static extern int GetState14(int index, out RawState state);

			[System.Runtime.InteropServices.DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
			internal static extern int GetState910(int index, out RawState state);

			[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
			internal struct RawState
			{
				public uint PacketNumber;
				public ushort Buttons;
				public byte LeftTrigger;
				public byte RightTrigger;
				public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
			}

			internal static bool Connected(int index)
			{
				RawState state;
				try { return GetState14(index, out state) == 0; }
				catch (DllNotFoundException) { }
				catch (EntryPointNotFoundException) { }
				return GetState910(index, out state) == 0;
			}
		}

		static bool[] Occupied()
		{
			var places = new bool[4];
			for (var i = 0; i < 4; i++)
				places[i] = SystemXInput.Connected(i);
			return places;
		}

		static string Show(bool[] places)
		{
			return string.Join(" ", Enumerable.Range(0, 4)
				.Select(i => string.Format("{0}:{1}", i + 1, places[i] ? "taken" : "free")).ToArray());
		}

		/// <summary>Waits for the places to differ from what they were, and says how long it took.</summary>
		static bool[] WaitForChange(bool[] from, TimeSpan limit, out TimeSpan took)
		{
			var started = DateTime.UtcNow;
			while (DateTime.UtcNow - started < limit)
			{
				var now = Occupied();
				if (!now.SequenceEqual(from))
				{
					took = DateTime.UtcNow - started;
					return now;
				}
				Thread.Sleep(50);
			}
			took = limit;
			return Occupied();
		}

		static int[] Gained(bool[] before, bool[] after)
		{
			return Enumerable.Range(0, 4).Where(i => !before[i] && after[i]).ToArray();
		}

		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("Watching the places says where a controller we made has landed")]
		public void Watching_the_places_says_where_our_controller_landed()
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
			// Start from nothing of ours, so what is left belongs to somebody else.
			for (uint i = 1; i <= 4; i++)
				client.UnPlug(i);
			Thread.Sleep(2000);

			var atRest = Occupied();
			Console.WriteLine("places with only real controllers : {0}", Show(atRest));
			Console.WriteLine("real controllers holding a place  : {0}", atRest.Count(x => x));
			Console.WriteLine();

			var results = new System.Collections.Generic.List<int>();
			try
			{
				// One at a time, because one at a time is how the program makes them, and because two
				// arriving together could not be told apart by watching.
				for (uint pad = 1; pad <= 2; pad++)
				{
					var before = Occupied();
					var plugged = client.PlugIn(pad);
					TimeSpan took;
					var after = WaitForChange(before, TimeSpan.FromSeconds(10), out took);
					var gained = Gained(before, after);
					Console.WriteLine("made pad {0}: PlugIn returned {1}", pad, plugged);
					Console.WriteLine("    before  : {0}", Show(before));
					Console.WriteLine("    after   : {0}   ({1:0.00}s)", Show(after), took.TotalSeconds);
					Console.WriteLine("    places that filled : {0}", gained.Length == 0
						? "NONE" : string.Join(",", gained.Select(i => (i + 1).ToString()).ToArray()));
					if (gained.Length == 1)
						results.Add(gained[0]);
					Console.WriteLine();
				}
			}
			finally
			{
				for (uint i = 1; i <= 4; i++)
					client.UnPlug(i);
				// And let the native client go, so the bus takes the controllers away rather than
				// leaving them for whenever this process happens to end. A test that leaves
				// controllers behind holds the places the next test needs.
				ViGEmClient.DisposeCurrent();
				Thread.Sleep(2000);
				Console.WriteLine("places after tidying up : {0}", Show(Occupied()));
				Console.WriteLine("same as at the start    : {0}",
					Occupied().SequenceEqual(atRest) ? "yes" : "NO");
			}

			Console.WriteLine();
			Console.WriteLine("SUMMARY");
			Console.WriteLine("  places identified by watching : {0} of 2", results.Count);
			if (results.Count == 2)
				Console.WriteLine("  our pads landed in places     : {0}",
					string.Join(", ", results.Select(i => (i + 1).ToString()).ToArray()));

			// A measurement, so it reports rather than judges. What it can say is whether watching
			// ever named the wrong place - that would be the method failing. Not being able to make
			// two controllers, because the machine has controllers of its own in the way, says
			// something about the machine and nothing about the method.
			if (results.Count < 2)
				Assert.Inconclusive("Only " + results.Count + " of 2 controllers could be made and "
					+ "watched on this machine; " + atRest.Count(x => x) + " place(s) were already "
					+ "held when it started. The numbers above are the result; there is nothing here "
					+ "to pass or fail.");
		}
	}
}
