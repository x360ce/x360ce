using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace x360ce.App.UiTree
{
	/// <summary>The name and purpose of each part of the interface, in one place.</summary>
	/// <remarks>
	/// These are written onto AccessibleName and AccessibleDescription, which is what a screen
	/// reader announces and what an automation tool searches by. The same two properties are what
	/// the exported navigation tree is built from, so a control described here is described
	/// everywhere at once, and there is one place to correct a wrong word.
	///
	/// Keys are "OwningType.FieldName", the same pair a developer sees in the designer. A control
	/// already named where it is built keeps that name; nothing here overwrites a deliberate one.
	/// </remarks>
	public static partial class UiText
	{
		/// <summary>What one element is called and what it is for.</summary>
		public struct Text
		{
			public Text(string name, string purpose, bool documentOnly = false)
			{
				Name = name;
				Purpose = purpose;
				DocumentOnly = documentOnly;
			}

			public readonly string Name;
			public readonly string Purpose;

			/// <summary>True where the name describes the element but must not be written onto it.</summary>
			public readonly bool DocumentOnly;
		}

		/// <summary>
		/// An element whose text is its value rather than its label: a reading on the status bar,
		/// the help header.
		/// </summary>
		/// <remarks>
		/// A label carries no value of its own, so what a screen reader reads out is its name. Give
		/// one a fixed name and the reading disappears behind it - "Controller rate" announced over
		/// and over while the number it is announcing can no longer be heard at all. So the name is
		/// kept for the exported document, where a fixed name is what is wanted, and the element
		/// itself is given only its purpose.
		/// </remarks>
		static Text Live(string name, string purpose)
		{
			return new Text(name, purpose, true);
		}

		static Dictionary<string, Text> _items;

		static Dictionary<string, Text> Items
		{
			get
			{
				if (_items == null)
				{
					var items = new Dictionary<string, Text>();
					AddMainWindow(items);
					AddControllerPanel(items);
					AddMapping(items);
					AddOptions(items);
					AddLists(items);

					AddMappingPickers(items);

					AddDeviceDetails(items);

					AddSwitches(items);
					_items = items;
				}
				return _items;
			}
		}

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

		static bool Find(Type owner, string field, string key, out Text text)
		{
			text = default(Text);
			if (!string.IsNullOrEmpty(key))
				return Items.TryGetValue(key, out text);
			if (owner == null || string.IsNullOrEmpty(field))
				return false;
			return Items.TryGetValue(owner.Name + "." + field, out text);
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
			var space = control.GetType().Namespace ?? "";
			return space.StartsWith("x360ce", StringComparison.Ordinal)
				|| space.StartsWith("JocysCom", StringComparison.Ordinal);
		}
	}
}
