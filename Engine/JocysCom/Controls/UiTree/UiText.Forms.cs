#nullable disable
using System;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	/// <summary>Writes the catalog onto Windows Forms controls and the entries of their bars and menus.</summary>
	public static partial class UiText
	{
		/// <summary>Names and describes everything inside a control that has an entry here.</summary>
		public static void Apply(Control root)
		{
			if (root == null)
				return;
			Apply(root, OwnerOf(root));
		}

		static void Apply(Control control, Type owner)
		{
			var composite = IsOwnComposite(control);
			var here = composite ? control.GetType() : owner;
			// A composite is described under its bare type name: the field holding it differs at
			// every place it is used, and what it is for does not.
			Write(control, composite ? null : here, composite ? here.Name : null);
			foreach (Control child in control.Controls)
				Apply(child, here);
			var strip = control as ToolStrip;
			if (strip != null)
				Apply(strip.Items, here);
			var context = control.ContextMenuStrip;
			if (context != null)
				Apply(context.Items, here);
		}

		/// <summary>Menu entries, and the entries of any menu that drops out of one.</summary>
		public static void Apply(ToolStripItemCollection items, Type owner)
		{
			foreach (ToolStripItem item in items)
			{
				Write(item, owner);
				var parent = item as ToolStripDropDownItem;
				if (parent != null)
					Apply(parent.DropDownItems, owner);
			}
		}

		static void Write(Control control, Type owner, string key = null)
		{
			Text text;
			if (!Find(owner, control.Name, key, out text))
				return;
			if (!text.DocumentOnly && string.IsNullOrEmpty(control.AccessibleName))
				control.AccessibleName = text.Name;
			if (string.IsNullOrEmpty(control.AccessibleDescription))
				control.AccessibleDescription = text.Purpose;
		}

		static void Write(ToolStripItem item, Type owner)
		{
			Text text;
			if (!Find(owner, item.Name, null, out text))
				return;
			if (!text.DocumentOnly && string.IsNullOrEmpty(item.AccessibleName))
				item.AccessibleName = text.Name;
			if (string.IsNullOrEmpty(item.AccessibleDescription))
				item.AccessibleDescription = text.Purpose;
		}

		/// <summary>
		/// The name for the document, including where it was deliberately not written onto the
		/// element. Used when describing the interface, never when announcing it.
		/// </summary>
		public static string NameFor(Control control)
		{
			Text text;
			return control != null && Find(OwnerOf(control), control.Name, null, out text)
				? text.Name : null;
		}

		/// <summary>The name for the document, for an entry on a menu or a bar.</summary>
		public static string NameFor(ToolStripItem item)
		{
			Text text;
			var owner = item == null || item.Owner == null ? null : OwnerOf(item.Owner);
			return owner != null && Find(owner, item.Name, null, out text)
				? text.Name : null;
		}

		/// <summary>
		/// The document's name for a control whose entry is <see cref="Live"/>, or null for any other. Its words on
		/// screen follow the program's state, so the document names it the same on every computer.
		/// </summary>
		public static string LiveNameFor(Control control)
		{
			Text text;
			return control != null && Find(OwnerOf(control), control.Name, null, out text) && text.DocumentOnly
				? text.Name : null;
		}

		/// <summary>The document's name for a menu or bar entry whose entry is <see cref="Live"/>, or null for any other.</summary>
		public static string LiveNameFor(ToolStripItem item)
		{
			Text text;
			var owner = item == null || item.Owner == null ? null : OwnerOf(item.Owner);
			return owner != null && Find(owner, item.Name, null, out text) && text.DocumentOnly
				? text.Name : null;
		}

		/// <summary>The panel or window a field belongs to, which is how the designer names it.</summary>
		static Type OwnerOf(Control control)
		{
			var walk = control;
			while (walk != null)
			{
				if (IsOwnComposite(walk))
					return walk.GetType();
				walk = walk.Parent;
			}
			return control == null ? null : control.GetType();
		}

		static bool IsOwnComposite(Control control)
		{
			if (!(control is UserControl) && !(control is Form))
				return false;
			return UiTreeWalker.IsOwnType(control.GetType());
		}
	}
}
