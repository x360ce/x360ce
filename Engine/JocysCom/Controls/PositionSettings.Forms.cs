#nullable disable

using System.Drawing;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>The Windows Forms half: forms and the cursor.</summary>
	/// <remarks>
	/// Take this file alongside PositionSettings.cs in a project that uses Windows Forms. Together
	/// they need no WPF assembly, so a plain Windows Forms application can use them as they are.
	///
	/// Windows Forms places windows in pixels, so the screens handed to the shared part are asked
	/// for in pixels. A program that is not per-monitor aware is handed pixels Windows has already
	/// scaled for it, and those agree with the form's own coordinates, which is what matters.
	/// </remarks>
	public partial class PositionSettings
	{
		/// <summary>How the window was left, in the terms Windows Forms uses.</summary>
		/// <remarks>A view over State, not a second copy of it, so it is not stored twice.</remarks>
		[System.Xml.Serialization.XmlIgnore]
		public FormWindowState FormWindowState
		{
			get
			{
				switch (State)
				{
					case PositionState.Minimized: return System.Windows.Forms.FormWindowState.Minimized;
					case PositionState.Maximized: return System.Windows.Forms.FormWindowState.Maximized;
					default: return System.Windows.Forms.FormWindowState.Normal;
				}
			}
			set { State = ToState(value); }
		}

		/// <summary>
		/// Call on Form_FormClosing event.
		/// </summary>
		public void SavePosition(Form form)
		{
			if (form == null)
				return;
			// Where it would go back to, not where it sits while maximised or minimised: restoring a
			// maximised window needs the size it had before, or it stays that size for ever.
			var bounds = form.WindowState == System.Windows.Forms.FormWindowState.Normal
				? form.DesktopBounds
				: form.RestoreBounds;
			SaveBounds(bounds, ToState(form.WindowState), GetScreensInPixels());
		}

		/// <summary>
		/// Call before the form is shown, so it appears where it belongs rather than moving there.
		/// </summary>
		public void LoadPosition(Form form, FormWindowState? overrideState = null)
		{
			if (form == null || !IsEnabled)
				return;
			var min = new SizeF(form.MinimumSize.Width, form.MinimumSize.Height);
			var max = new SizeF(form.MaximumSize.Width, form.MaximumSize.Height);
			var bounds = LoadBounds(GetScreensInPixels(), min, max);
			form.StartPosition = FormStartPosition.Manual;
			form.DesktopBounds = Rectangle.Round(bounds);
			var state = overrideState ?? FormWindowState;
			// Restoring a window straight into minimised leaves the program with no window and no
			// obvious way back, so only being maximised is restored.
			form.WindowState = state == System.Windows.Forms.FormWindowState.Maximized
				? System.Windows.Forms.FormWindowState.Maximized
				: System.Windows.Forms.FormWindowState.Normal;
			RaisePositionLoaded();
		}

		static PositionState ToState(FormWindowState state)
		{
			switch (state)
			{
				case System.Windows.Forms.FormWindowState.Minimized: return PositionState.Minimized;
				case System.Windows.Forms.FormWindowState.Maximized: return PositionState.Maximized;
				default: return PositionState.Normal;
			}
		}

		/// <summary>
		/// Get cursor position for Windows Forms, relative to the virtual screen (all monitors).
		/// </summary>
		public static Point GetFormsCursorPosition()
		{
			return Cursor.Position;
		}
	}
}
