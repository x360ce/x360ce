// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadTabPages/SdlUserControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The SDL page is a page Windows shows on each controller tab, right after Direct Input, and shows the mapping Auto
	/// fills the selected device from: x360ce's controller database row, with SDL's where it maps nothing.
	/// </summary>
	[TestClass]
	public class SdlTabTest
	{
		/// <summary>A wheel's row in x360ce's controller database: vendor 0x1234, product 0x5678.</summary>
		const string X360ceRow = "03000000341200007856000000000000,Test Wheel,users:20,A:2,B:3,Left Analog X:1,platform:Windows,";

		/// <summary>The same wheel's row in SDL's controller database.</summary>
		const string SdlRow = "03000000341200007856000000000000,Test Wheel SDL,a:b0,b:b1,x:b2,leftx:a0,platform:Windows,";

		/// <summary>A wheel with three axes, twelve buttons and a hat.</summary>
		static UserDevice Wheel(int vendor = 0x1234, int product = 0x5678, string name = "Test Wheel")
		{
			return new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				ProductName = name,
				DevVendorId = vendor,
				DevProductId = product,
				CapType = (int)SharpDX.DirectInput.DeviceType.Driving,
				DiAxeMask = 0x7,
				CapButtonCount = 12,
				CapPovCount = 1,
			};
		}

		static SdlDatabase Database(params string[] rows)
		{
			return SdlDatabase.Merge(SdlSource.Parse(string.Join("\n", rows), SdlSourceKind.BuiltIn));
		}

		static SdlPositionRow Place(SdlUserControl page, MapCode code)
		{
			return ((IEnumerable<SdlPositionRow>)page.PositionsDataGridView.DataSource).First(x => x.Code == code);
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A controller only x360ce's controller database knows, as the G27, shows that row on its SDL page, as Auto fills it, and says where it is from; with both databases each place says which one fills it")]
		public void The_sdl_page_shows_what_auto_fills_the_device_from()
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new SdlUserControl())
				{
					var none = SdlDatabase.Merge();
					// Only x360ce's database knows it.
					page.UpdateFrom(Wheel(), new PadSetting(), none, Database(X360ceRow));
					Assert.IsNotNull(page.Translation, "The page shows no row for a controller x360ce's database knows: " + page.DeviceLabel.Text);
					StringAssert.Contains(page.DeviceLabel.Text, "x360ce’s database (built in, 1 Windows row) knows this controller as \"Test Wheel\"");
					var a = Place(page, MapCode.ButtonA);
					Assert.AreEqual("Button 2", a.Control);
					Assert.AreEqual("A:2", a.Binding);
					Assert.AreEqual("x360ce", a.From);
					Assert.IsTrue(page.ApplyButton.Enabled, "[Apply SDL Mapping] is off while the page shows a mapping.");
					// Both: x360ce's row first, SDL's where it maps nothing.
					page.UpdateFrom(Wheel(), new PadSetting(), Database(SdlRow), Database(X360ceRow));
					a = Place(page, MapCode.ButtonA);
					Assert.AreEqual("x360ce", a.From);
					Assert.AreEqual("Button 2", a.Control, "SDL's row took a place x360ce's row fills.");
					var x = Place(page, MapCode.ButtonX);
					Assert.AreEqual("SDL", x.From);
					Assert.AreEqual("Button 3", x.Control);
					StringAssert.Contains(page.DeviceLabel.Text, "\"Test Wheel SDL\"");
					// Only SDL's database knows it, as before.
					page.UpdateFrom(Wheel(), new PadSetting(), Database(SdlRow), none);
					a = Place(page, MapCode.ButtonA);
					Assert.AreEqual("SDL", a.From);
					Assert.AreEqual("Button 1", a.Control);
					StringAssert.Contains(page.DeviceLabel.Text, "SDL’s database (built in, 1 Windows row) knows it as \"Test Wheel SDL\"");
					// The G27 itself, with the databases the program carries: x360ce's knows it and SDL's does not.
					var sdlBuiltIn = SdlDatabase.Merge(SdlSource.Parse(SdlDatabase.ReadBuiltIn(), SdlSourceKind.BuiltIn));
					var x360ceBuiltIn = SdlDatabase.Merge(SdlSource.Parse(X360ceDatabase.ReadBuiltIn(), SdlSourceKind.BuiltIn));
					page.UpdateFrom(Wheel(0x046D, 0xC29B, "Logitech G27 Racing Wheel USB"), new PadSetting(), sdlBuiltIn, x360ceBuiltIn);
					Assert.IsNotNull(page.Translation, "The G27's SDL page shows no row: " + page.DeviceLabel.Text);
					StringAssert.Contains(page.DeviceLabel.Text, "knows this controller as \"Logitech G27 Racing Wheel USB\"");
				}
			});
		}

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
					var tabs = (TabControl)typeof(PadControl).GetField(nameof(PadControl.PadTabControl), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(page);
					var names = tabs.TabPages.Cast<TabPage>().Select(x => x.Name).ToList();
					Assert.AreEqual(names.IndexOf(nameof(PadControl.DirectInputTabPage)) + 1, names.IndexOf("SdlTabPage"),
						"The SDL page is not right after Direct Input: " + string.Join(", ", names));
					// Made only now, after the page was added, as when the program opens.
					var count = SendMessage(tabs.Handle, TCM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();
					Assert.AreEqual(tabs.TabPages.Count, count, "Windows shows another number of pages than the tab control holds.");
				}
			});
		}
	}
}
