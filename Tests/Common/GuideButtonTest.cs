// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Windows.Forms;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>The keys the guide button presses are read from the option once per change of it.</summary>
	[TestClass]
	public class GuideButtonTest
	{
		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The guide keys are read once per change of the option, and a press makes nothing")]
		public void The_guide_keys_are_read_once_per_change()
		{
			var text = "{7}{LWin}";
			var keys = DInputHelper.GuideKeys(text);
			CollectionAssert.AreEqual(new[] { (Keys)7, Keys.LWin }, keys);
			Assert.AreSame(keys, DInputHelper.GuideKeys(text), "The option is read again on every press.");
			CollectionAssert.AreEqual(new[] { Keys.Escape }, DInputHelper.GuideKeys("{Escape}"), "A changed option is not read.");
			Assert.AreEqual(0, DInputHelper.GuideKeys(null).Length, "No option gives keys.");
			const int presses = 2000;
			DInputHelper.GuideKeys(text);
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < presses; i++)
					GC.KeepAlive(DInputHelper.GuideKeys(text));
			});
			Assert.IsTrue(allocated < presses, presses + " presses handed the collector " + allocated + " bytes; reading the keys for a press must make nothing.");
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			Assert.IsFalse(step5.Contains("guideLock"), "The engine takes a lock nobody else takes on every changed report.");
			StringAssert.Contains(step5, "GuideKeys(SettingsManager.Options.GuideButtonAction)");
			StringAssert.Contains(step5, "keys.Length > 0");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Each pad's guide state is its own, so a change on one pad does not affect another")]
		public void Each_pad_keeps_its_own_guide_state()
		{
			var isGuideDown = new bool[4];
			Assert.AreEqual(true, DInputHelper.GuideChanged(isGuideDown, 1, true), "Pad 1's first press was not read as a press.");
			Assert.IsNull(DInputHelper.GuideChanged(isGuideDown, 2, false), "Pad 2, never pressed, read a release as a change.");
			Assert.IsNull(DInputHelper.GuideChanged(isGuideDown, 1, true), "Pad 1 held down was read as pressed again.");
			Assert.AreEqual(true, DInputHelper.GuideChanged(isGuideDown, 2, true), "Pad 2's own press was affected by pad 1's held guide.");
			Assert.AreEqual(false, DInputHelper.GuideChanged(isGuideDown, 1, false), "Pad 1's release was not read as a release.");
			Assert.IsNull(DInputHelper.GuideChanged(isGuideDown, 2, true), "Pad 1's release cleared pad 2's held guide.");
			Assert.AreEqual(false, DInputHelper.GuideChanged(isGuideDown, 2, false), "Pad 2's release was not read as a release.");
			CollectionAssert.AreEqual(new[] { false, false, false, false }, isGuideDown, "Every pad's flag should be clear once both are released.");
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(step5, "bool[] IsGuideDown = new bool[4]", "One flag shared by every pad lets a change on one pad release or press another's keys.");
		}
	}
}
