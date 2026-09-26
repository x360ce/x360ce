// @under-test: Engine/Common/ForceFeedbackDriver.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.IO;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Which effect a device is sent for each effect type, and how its driver is found.
	/// </summary>
	/// <remarks>
	/// The generic "USB Vibration" driver faults inside itself on a sawtooth and takes the program
	/// with it; a fault in a driver cannot be caught, so the only protection is never to ask.
	/// </remarks>
	[TestClass]
	public class ForceFeedbackDriverTest
	{
		const string UsbVibration = @"C:\WINDOWS\USB Vibration\7906\EZFRD64.DLL";

		[TestMethod, TestCategory("force-feedback")]
		public void A_sawtooth_is_never_sent_to_the_USB_Vibration_driver()
		{
			Assert.AreEqual(EffectGuid.Sine, ForceFeedbackDriver.EffectFor(ForceEffectType.PeriodicSawtooth, UsbVibration));
			Assert.AreEqual(EffectGuid.Sine, ForceFeedbackDriver.EffectFor(ForceEffectType.PeriodicSawtooth2, UsbVibration));
			Assert.AreEqual(EffectGuid.Sine, ForceFeedbackDriver.EffectFor(ForceEffectType.PeriodicSawtooth, @"c:\windows\usb vibration\7906\ezfrd32.dll"), "The 32 bit driver, and the name in any case.");
		}

		[TestMethod, TestCategory("force-feedback")]
		public void Every_other_driver_gets_what_the_setting_asks()
		{
			foreach (var driver in new[] { "", null, @"C:\WINDOWS\System32\WmJoyFrc.dll" })
			{
				Assert.AreEqual(EffectGuid.SawtoothDown, ForceFeedbackDriver.EffectFor(ForceEffectType.PeriodicSawtooth, driver));
				Assert.AreEqual(EffectGuid.SawtoothDown, ForceFeedbackDriver.EffectFor(ForceEffectType.PeriodicSawtooth2, driver));
				Assert.AreEqual(EffectGuid.Sine, ForceFeedbackDriver.EffectFor(ForceEffectType.PeriodicSine2, driver));
				Assert.AreEqual(EffectGuid.ConstantForce, ForceFeedbackDriver.EffectFor(ForceEffectType.Constant2, driver));
			}
			Assert.AreEqual(EffectGuid.ConstantForce, ForceFeedbackDriver.EffectFor(ForceEffectType.Constant, UsbVibration), "Only the sawtooth is taken from that driver.");
			Assert.AreEqual(EffectGuid.Sine, ForceFeedbackDriver.EffectFor(ForceEffectType.PeriodicSine, UsbVibration));
		}

		[TestMethod, TestCategory("force-feedback")]
		public void A_driver_is_found_by_its_class_identifier()
		{
			// DirectInput's own class is registered on every Windows, so it stands in for a driver's.
			var directInput8 = new Guid("25E609E4-B259-11CF-BFC7-444553540000");
			StringAssert.EndsWith(Path.GetFileName(ForceFeedbackDriver.FileOf(directInput8)).ToLowerInvariant(), "dinput8.dll");
			Assert.AreEqual("", ForceFeedbackDriver.FileOf(Guid.Empty), "A device without force feedback has no driver.");
			Assert.AreEqual("", ForceFeedbackDriver.FileOf(Guid.NewGuid()), "An identifier Windows does not know names no file.");
		}
	}
}
