// @under-test: App.v4/Common/DInput/XInputPlaces.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// What the XInput column prints, for every controller on the machine this runs on.
	/// </summary>
	/// <remarks>
	/// Both faces of one controller must answer the same, because a person reading the list is
	/// looking at one thing however many devices Windows built for it. A blank is a real answer:
	/// where two controllers share the places left over, neither can be named, and naming one
	/// anyway would have somebody map a controller against a place it does not hold.
	/// </remarks>
	[TestClass]
	public class XInputPlaceColumnTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("Shows the place the list column would print for each controller")]
		public void What_the_column_would_say()
		{
			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
			var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
			var places = XInputPlaces.Resolve(all, byId);
			var capable = all.Where(XInputPlaces.IsXInputCapable).OrderBy(x => x.DeviceId).ToList();
			if (capable.Count == 0)
				Assert.Inconclusive("No controller XInput could see is attached.");

			Console.WriteLine("controllers XInput can see : {0}", capable.Count);
			Console.WriteLine();
			foreach (var device in capable)
			{
				int place;
				var shown = places.TryGetValue(device.DeviceId, out place)
					? XInputPlaces.Describe(place, VirtualDriverInstaller.IsVirtualPad(device, byId), VirtualDriverInstaller.IsOneOfOurs(device, byId))
					: string.Empty;
				Console.WriteLine("  column shows : {0,-10}  {1}",
					string.IsNullOrEmpty(shown) ? "(blank)" : shown, device.Description);
				Console.WriteLine("                             {0}", device.DeviceId);
			}

			// Every face of one controller has to give the same answer, whatever that answer is.
			// Two faces of one controller disagreeing would put two different places against one
			// piece of hardware in the list, and both cannot be right.
			foreach (var group in capable.GroupBy(d => XInputPlaces.HardwareOf(d, byId), StringComparer.OrdinalIgnoreCase))
			{
				var answers = group
					.Select(d => { int p; return places.TryGetValue(d.DeviceId, out p) ? p : XInputPlaces.Unknown; })
					.Distinct()
					.ToArray();
				Assert.AreEqual(1, answers.Length,
					"The faces of one controller are given different places: " + group.Key);
			}
		}
	}
}
