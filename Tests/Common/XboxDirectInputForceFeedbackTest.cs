// @under-test: App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// Asks DirectInput what it thinks each attached controller can do, and in particular whether an
	/// Xbox controller offers force feedback through that face.
	/// </summary>
	/// <remarks>
	/// The question behind this is whether an Xbox controller could keep its motors while giving up
	/// its XInput place - because if it could, nothing would need reordering: the real controller
	/// would simply stop competing for a place and still rumble.
	///
	/// It is asked by reading rather than by switching anything off. What DirectInput will do with a
	/// device is described by the report the device hands Windows when it arrives. If that report
	/// never mentions force feedback then there is nothing being withheld, and taking the XInput face
	/// away cannot produce something the description does not contain. Only if the capability is
	/// declared and then refused would exclusivity be the explanation worth testing the hard way.
	/// </remarks>
	[TestClass]
	public class XboxDirectInputForceFeedbackTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("requires-elevation")]
		[Description("Reports which attached controllers offer force feedback through DirectInput")]
		public void What_directinput_says_each_controller_can_do()
		{
			using (var dinput = new DirectInput())
			{
				var devices = dinput
					.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
					.ToList();
				if (devices.Count == 0)
					Assert.Inconclusive("No game controllers are attached, so there is nothing to ask about.");

				Console.WriteLine("{0} controller(s) attached", devices.Count);
				Console.WriteLine();
				var xbox = 0;
				foreach (var info in devices)
				{
					using (var device = new Joystick(dinput, info.InstanceGuid))
					{
						var caps = device.Capabilities;
						var ff = caps.Flags.HasFlag(DeviceFlags.ForceFeedback);
						var looksXbox = (info.ProductName ?? string.Empty).IndexOf("xbox",
							StringComparison.OrdinalIgnoreCase) >= 0;
						if (looksXbox)
							xbox++;
						Console.WriteLine("  {0}{1}", info.ProductName, looksXbox ? "   <- Xbox" : string.Empty);
						Console.WriteLine("      force feedback : {0}", ff ? "YES" : "no");
						Console.WriteLine("      axes {0}, buttons {1}, POVs {2}",
							caps.AxeCount, caps.ButtonCount, caps.PovCount);
						Console.WriteLine("      flags          : {0}", caps.Flags);
						if (ff)
						{
							var effects = device.GetEffects().Select(x => x.Name).ToArray();
							Console.WriteLine("      effects        : {0}",
								effects.Length == 0 ? "(none listed)" : string.Join(", ", effects));
						}
					}
				}

				Console.WriteLine();
				if (xbox == 0)
					Assert.Inconclusive("No Xbox controller is attached, so the question this exists to "
						+ "answer cannot be answered on this machine.");
			}
		}
	}
}
