// @under-test: App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// Which device failures are conditions the next poll handles, and which are faults to report.
	/// </summary>
	/// <remarks>
	/// Every failure treated as a fault is mailed to support. Device conditions - unplugged, taken
	/// by another program, a driver mid-reset - were the largest source of that mail, and each new
	/// one found is added here so it stays out of the mailbox and stays listed.
	/// </remarks>
	[TestClass]
	public class BenignDeviceResultTest
	{
		[TestMethod, TestCategory("devices")]
		public void Device_conditions_are_not_reported()
		{
			foreach (var code in new[]
			{
				unchecked((int)0x80004005), // E_FAIL on Acquire while the device goes away (4.22.21.0 report).
				unchecked((int)0x80070005), // E_ACCESSDENIED, another program holds the device.
				unchecked((int)0x8007048F), // ERROR_DEVICE_NOT_CONNECTED.
				unchecked((int)0x80004001), // E_NOTIMPL, effect not implemented.
			})
				Assert.IsTrue(DInputHelper.IsBenignDeviceResult(new Result(code)), string.Format("0x{0:X8} is a device condition, yet it would be reported.", code));
		}

		[TestMethod, TestCategory("devices")]
		public void Other_failures_are_still_reported()
		{
			foreach (var code in new[]
			{
				unchecked((int)0x8007000E), // E_OUTOFMEMORY.
				unchecked((int)0x80070006), // E_HANDLE.
				unchecked((int)0x80040154), // REGDB_E_CLASSNOTREG, DirectInput itself is missing.
			})
				Assert.IsFalse(DInputHelper.IsBenignDeviceResult(new Result(code)), string.Format("0x{0:X8} is a fault, yet it would be swallowed.", code));
		}
	}
}
