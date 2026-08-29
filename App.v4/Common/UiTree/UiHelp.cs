using System.Windows.Forms;
using x360ce.App.Controls;

namespace x360ce.App.UiTree
{
	/// <summary>Shows what the mouse is over in the header, for every control at once.</summary>
	/// <remarks>
	/// One call wires the whole window. The text comes from AccessibleName and
	/// AccessibleDescription, the same two properties a screen reader announces and the exported
	/// navigation tree is built from, so the header cannot drift from either.
	///
	/// What it replaces only covered controls registered as settings, took its words from a
	/// separate copy held beside each setting, never filled in the subject, and sent help through
	/// the channel used for status messages, which stamped the time onto it.
	/// </remarks>
	public static class UiHelp
	{
		/// <summary>Wires a window and everything inside it.</summary>
		public static void Attach(Control root)
		{
			if (root == null)
				return;
			Wire(root);
			foreach (Control child in root.Controls)
				Attach(child);
			var strip = root as ToolStrip;
			if (strip != null)
				foreach (ToolStripItem item in strip.Items)
					Wire(item);
		}

		static void Wire(Control control)
		{
			// Nothing to say about it, and its parent may still have something.
			if (string.IsNullOrEmpty(control.AccessibleDescription))
				return;
			control.MouseEnter += (sender, e) => Show(control, control.AccessibleName, control.AccessibleDescription);
			control.MouseLeave += (sender, e) => Clear(control);
		}

		static void Wire(ToolStripItem item)
		{
			if (string.IsNullOrEmpty(item.AccessibleDescription))
				return;
			item.MouseEnter += (sender, e) => Show(item.Owner, item.AccessibleName, item.AccessibleDescription);
			item.MouseLeave += (sender, e) => Clear(item.Owner);
		}

		static void Show(Control control, string name, string purpose)
		{
			var form = HeaderOf(control);
			if (form != null)
				form.ShowHelp(name, purpose);
		}

		static void Clear(Control control)
		{
			var form = HeaderOf(control);
			if (form != null)
				form.ClearHelp();
		}

		/// <summary>
		/// The window whose header should report this. Found by walking up rather than assuming the
		/// main window, so the same wiring works in the dialogs that carry a header of their own.
		/// </summary>
		static BaseFormWithHeader HeaderOf(Control control)
		{
			var walk = control;
			while (walk != null)
			{
				var form = walk as BaseFormWithHeader;
				if (form != null)
					return form;
				walk = walk.Parent;
			}
			return null;
		}
	}
}
