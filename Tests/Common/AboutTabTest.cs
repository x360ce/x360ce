// @under-test: App.v4/Controls/AboutControl.Designer.cs
// @area: about   @layer: ui-wpf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// Credits and links on the About screen.
	/// </summary>
	/// <remarks>
	/// vigem.org stopped resolving when that project was archived, and the About screen went on
	/// pointing at it, so the shipping build carried a dead link nobody noticed. These tests read
	/// what the screen actually renders rather than trusting the designer file.
	/// </remarks>
	[TestClass]
	public class AboutTabTest
	{

		/// <summary>Addresses that must never appear again; their hosts are gone.</summary>
		static readonly string[] RetiredHosts = { "vigem.org" };

		[TestMethod, TestCategory("about"), TestCategory("ui-interactive")]
		[Description("The About screen credits each contributor and links somewhere that resolves")]
		public void About_screen_shows_credits_and_no_retired_links()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built. Build the solution before running UI tests.");

			Process process = null;
			try
			{
				process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
				var window = Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));

				var about = Ui.WaitFor(() => window
					.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem))
					.Cast<AutomationElement>()
					.FirstOrDefault(x => string.Equals(x.Current.Name, "About", StringComparison.OrdinalIgnoreCase)),
					TimeSpan.FromSeconds(20), "the About tab");

				about.GetCurrentPattern(SelectionItemPattern.Pattern);
				((SelectionItemPattern)about.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();

				// Only the captions and links count. The change log is also on this screen and it
				// quotes addresses when describing what was fixed, so reading every piece of text
				// would match a mention of a dead link and call it a dead link.
				var names = Ui.WaitFor(() =>
				{
					var found = window
						.FindAll(TreeScope.Descendants, Condition.TrueCondition)
						.Cast<AutomationElement>()
						.Where(x => x.Current.ControlType == ControlType.Text
							|| x.Current.ControlType == ControlType.Hyperlink)
						.Select(x => x.Current.Name)
						.Where(x => !string.IsNullOrEmpty(x))
						.ToArray();
					return found.Any(x => x.IndexOf("jocys.com", StringComparison.OrdinalIgnoreCase) >= 0)
						? found : null;
				}, TimeSpan.FromSeconds(20), "the About credits");

				var all = string.Join(" | ", names);
				Console.WriteLine("About screen text: " + all);

				foreach (var host in RetiredHosts)
					Assert.IsFalse(all.IndexOf(host, StringComparison.OrdinalIgnoreCase) >= 0,
						"The About screen still links to " + host + ", which no longer resolves.");

				// Match a caption exactly. Searching the joined text would find "Nefarius" inside
				// the address next to it and pass whatever the caption actually says.
				foreach (var credit in new[] { "Nefarius", "Jocys.com", "ToCA EDIT", "Nucleoprotein" })
					Assert.IsTrue(names.Any(x => string.Equals(x.Trim(), credit, StringComparison.Ordinal)),
						"The About screen has no caption reading " + credit + ".");
			}
			finally
			{
				Ui.CloseApp(process);
			}
		}

	}
}
