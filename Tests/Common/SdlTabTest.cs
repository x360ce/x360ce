// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadTabPages/SdlUserControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>The SDL page is a page Windows shows on each controller tab, right after Direct Input.</summary>
	[TestClass]
	public class SdlTabTest
	{
		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		const int TCM_GETITEMCOUNT = 0x1304;

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The tab strip Windows draws holds every page, the SDL page right after Direct Input")]
		public void The_sdl_page_is_shown_after_direct_input()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				{
					form.Controls.Add(page);
					var tabs = (TabControl)typeof(PadControl).GetField("PadTabControl", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(page);
					var names = tabs.TabPages.Cast<TabPage>().Select(x => x.Name).ToList();
					Assert.AreEqual(names.IndexOf("DirectInputTabPage") + 1, names.IndexOf("SdlTabPage"),
						"The SDL page is not right after Direct Input: " + string.Join(", ", names));
					// Made only now, after the page was added, as when the program opens.
					var count = SendMessage(tabs.Handle, TCM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();
					Assert.AreEqual(tabs.TabPages.Count, count, "Windows shows another number of pages than the tab control holds.");
				}
			});
		}
	}
}
