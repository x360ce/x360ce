// @under-test: App.v4/Forms/BaseFormWithHeader.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// Dragging the edge of a window with a header, as the main window is, lays nothing out until the drag ends, because
	/// Windows moves each child window on its own. A resize with no drag, as a maximise or a snap is, lays it out at once.
	/// </summary>
	/// <remarks>
	/// Windows starts and ends a drag with these two messages, which Windows Forms raises as ResizeBegin and ResizeEnd.
	/// The window is off every screen and never shown, so nothing appears.
	/// </remarks>
	[TestClass]
	public class MainWindowDragTest
	{
		const int WM_ENTERSIZEMOVE = 0x0231;
		const int WM_EXITSIZEMOVE = 0x0232;

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

		[TestMethod, TestCategory("ui")]
		[Description("While the window's edge is dragged what it holds keeps its size; the drag's end lays it out at the window's new size; a resize with no drag lays it out at once")]
		public void A_drag_lays_the_window_out_once_when_it_ends()
		{
			Ui.OnUiThread(() =>
			{
				using (var window = new BaseFormWithHeader { StartPosition = FormStartPosition.Manual, Location = new Point(-6000, -6000), ShowInTaskbar = false })
				{
					var handle = window.Handle;
					var header = window.Controls.Find(nameof(BaseFormWithHeader.TopPanel), true).Single();
					var width = header.Width;
					window.Width += 80;
					Assert.AreEqual(width + 80, header.Width, "A resize with no drag did not lay the window out.");
					width = header.Width;
					SendMessage(handle, WM_ENTERSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
					window.Width += 80;
					Assert.AreEqual(width, header.Width, "The window was laid out during the drag.");
					SendMessage(handle, WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
					Assert.AreEqual(width + 80, header.Width, "The drag's end did not lay the window out at its new size.");
					// A move starts and ends the same way, and leaves the layout as it found it.
					SendMessage(handle, WM_ENTERSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
					SendMessage(handle, WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
					width = header.Width;
					window.Width += 80;
					Assert.AreEqual(width + 80, header.Width, "After a drag, a resize no longer lays the window out.");
				}
			});
		}
	}
}
