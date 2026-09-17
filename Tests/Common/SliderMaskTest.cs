// @under-test: App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// The engine works out which slider slots a device answers to, and the mapping list offers
	/// a slider only when its bit is set on the device. The answer was computed and dropped, so
	/// no real device ever offered a slider. Working out the mask needs a DirectInput device in
	/// hand, so what is pinned here is the one line that keeps the answer.
	/// </summary>
	[TestClass]
	public class SliderMaskTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The slider mask the engine works out is written to the device, not dropped")]
		public void Slider_mask_is_kept_on_the_device()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs");
			var source = File.ReadAllText(path);
			var kept = Regex.IsMatch(source, @"ud\.DiSliderMask\s*=\s*CustomDiState\.GetJoystickSlidersMask\(");
			Assert.IsTrue(kept, "GetJoystickSlidersMask is called without its result being assigned to ud.DiSliderMask, so no real device offers a slider.");
			var dropped = Regex.IsMatch(source, @"^\s*CustomDiState\.GetJoystickSlidersMask\(", RegexOptions.Multiline);
			Assert.IsFalse(dropped, "GetJoystickSlidersMask is called as a statement, which drops the mask.");
		}
	}
}
