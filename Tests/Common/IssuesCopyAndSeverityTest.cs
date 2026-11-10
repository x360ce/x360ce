// @under-test: Engine/JocysCom/Controls/IssuesControl/IssuesUserControl.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace x360ce.Tests
{
	/// <summary>
	/// The Issues tab's severity icon, and the text its Copy puts on the clipboard.
	/// </summary>
	/// <remarks>
	/// The icon was asked for by a size the theme library does not hold, so every row showed none, and no one
	/// noticed for several releases: the grid draws an empty cell without complaint.
	/// </remarks>
	[TestClass]
	public class IssuesCopyAndSeverityTest
	{
		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Every severity an issue can have shows an icon, and none shows nothing")]
		public void Every_severity_has_its_icon()
		{
			foreach (var severity in new[] { IssueSeverity.Low, IssueSeverity.Important, IssueSeverity.Moderate, IssueSeverity.Critical })
				Assert.IsNotNull(IssuesUserControl.SeverityImage(severity), "No icon for " + severity + ".");
			Assert.IsNull(IssuesUserControl.SeverityImage(IssueSeverity.None));
			Assert.IsNull(IssuesUserControl.SeverityImage(null));
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Copy gives each issue's severity, name, description, fix and link, one issue after another")]
		public void Copied_text_holds_everything_the_row_shows()
		{
			var first = new IssueItem { Name = "Game Sees Two Controllers", FixName = "Open HID Hide" };
			first.SetSeverity(IssueSeverity.Low, 2, "Line one." + Environment.NewLine + "Line two.");
			var second = new IssueItem { Name = "Driver Missing", FixName = "Install", MoreInfo = new Uri("https://example.com/driver") };
			second.SetSeverity(IssueSeverity.Critical, 1, "No driver.");
			var text = IssuesUserControl.IssuesToText(new[] { first, second });
			StringAssert.StartsWith(text, "Information: Game Sees Two Controllers");
			StringAssert.Contains(text, "Line one." + Environment.NewLine + "Line two.");
			StringAssert.Contains(text, "Solution: Open HID Hide");
			StringAssert.Contains(text, "Error: Driver Missing");
			StringAssert.Contains(text, "More: https://example.com/driver");
			Assert.IsTrue(text.IndexOf("Game Sees") < text.IndexOf("Driver Missing"), "The issues keep the list's order.");
		}
	}
}
