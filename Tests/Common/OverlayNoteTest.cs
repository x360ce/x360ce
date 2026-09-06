// @under-test: App.v4/Common/OverlayNote.cs, App.v4/Common/OverlayForm.cs
// @area: options   @layer: ui-winforms
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The note is drawn over a game, so it must never take the focus from it and must be where a
	/// person is looking. Both are read off the real window, not the source.
	/// </summary>
	[TestClass]
	public class OverlayNoteTest
	{
		[DllImport("user32.dll", SetLastError = true)]
		static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		const int GWL_EXSTYLE = -20;

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("The note is on top, off the taskbar, never takes the focus, and lets clicks through")]
		public void Note_stays_out_of_the_way()
		{
			Ui.OnUiThread(() =>
			{
				try
				{
					OverlayNote.Show("Emulated controllers on", Color.Green, 60);
					var window = OverlayNote.Window;
					Assert.IsNotNull(window, "No window was shown.");
					Assert.IsTrue(window.Visible, "The note is not visible.");
					Assert.IsTrue(window.TopMost, "The note would sit under the game.");
					Assert.IsFalse(window.ShowInTaskbar, "The note has a taskbar button.");
					var styles = GetWindowLong(window.Handle, GWL_EXSTYLE);
					Assert.AreEqual(OverlayForm.PassiveStyles, styles & OverlayForm.PassiveStyles,
						"The window would take the focus from the game, or swallow clicks meant for it.");
					Assert.AreEqual("Emulated controllers on", OverlayNote.Text);
				}
				finally
				{
					OverlayNote.Hide();
				}
				Assert.IsNull(OverlayNote.Window, "Hide left the note on screen.");
			});
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("A new note replaces the last rather than piling up")]
		public void A_new_note_replaces_the_last()
		{
			Ui.OnUiThread(() =>
			{
				try
				{
					OverlayNote.Show("first", Color.Green, 60);
					var first = OverlayNote.Window;
					OverlayNote.Show("second", Color.Gray, 60);
					Assert.AreEqual("second", OverlayNote.Text);
					Assert.IsTrue(first.IsDisposed, "The first note is still there under the second.");
				}
				finally
				{
					OverlayNote.Hide();
				}
			});
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("The note goes away by itself after its time")]
		public void Note_goes_away_by_itself()
		{
			Ui.OnUiThread(() =>
			{
				OverlayNote.Show("going", Color.Green, 1);
				var until = DateTime.UtcNow.AddSeconds(5);
				while (OverlayNote.Window != null && DateTime.UtcNow < until)
				{
					Application.DoEvents();
					System.Threading.Thread.Sleep(50);
				}
				Assert.IsNull(OverlayNote.Window, "The note did not go away after a second.");
			});
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("The note sits centred near the top of the monitor it is given, wherever that monitor is")]
		public void Note_is_placed_centred_near_the_top()
		{
			var words = new Size(200, 20);
			var main = OverlayNote.Place(new Rectangle(0, 0, 1920, 1080), words);
			Assert.AreEqual(108, main.Top, "A tenth of the way down.");
			Assert.AreEqual(960.0, main.Left + main.Width / 2.0, 1.0, "Centred.");
			Assert.IsTrue(main.Width > words.Width && main.Height > words.Height, "No room for the mark and the padding.");
			// A monitor to the left of the main one has negative coordinates, and the note must stay on it.
			var left = new Rectangle(-2560, 0, 2560, 1440);
			var placed = OverlayNote.Place(left, words);
			Assert.IsTrue(left.Contains(placed), "The note left its monitor: " + placed);
			Assert.AreEqual(-1280.0, placed.Left + placed.Width / 2.0, 1.0, "Centred on the other monitor.");
		}
	}
}
