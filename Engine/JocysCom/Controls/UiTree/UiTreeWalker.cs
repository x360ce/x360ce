#nullable disable
using System;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	/// <summary>Reads the interface the program built and describes it as a tree of elements.</summary>
	/// <remarks>
	/// Everything the designer created is walked, including pages and panels not on screen at the
	/// moment, because a page nobody has clicked yet is still a feature of the program. Windows Forms
	/// controls are handled in UiTreeWalker.Forms.cs, the bar entries and grid rows that are not
	/// controls in UiTreeWalker.Forms.Elements.cs, and WPF elements in UiTreeWalker.WPF.cs. This file
	/// holds what they share, and reaches the WPF side only through the partial methods at its end,
	/// so a Windows Forms program that leaves UiTreeWalker.WPF.cs out compiles and loads no WPF.
	/// </remarks>
	public static partial class UiTreeWalker
	{
		/// <summary>Namespaces whose types the program defines. Such a control is recorded with its type, and a composite one is described once.</summary>
		public static string[] OwnNamespaces = { "JocysCom" };

		/// <summary>Describes whatever a path named: a control, a bar entry, a grid row, a cell or a WPF element.</summary>
		public static UiNode Read(object element, bool raw, string path)
		{
			var control = element as Control;
			if (control != null)
				return Read(control, raw, path);
			var item = element as ToolStripItem;
			if (item != null)
				return Read(item, raw, path);
			var row = element as DataGridViewRow;
			if (row != null)
				return Read(row, path);
			var cell = element as DataGridViewCell;
			if (cell != null)
				return Read(cell, path);
			UiNode node = null;
			ReadWpf(element, raw, path, ref node);
			return node;
		}

		/// <summary>
		/// The one element a path names, or null. Paths are names from the root's children down,
		/// joined by '/': control names, then a bar entry's name, or 'rows' and an index inside a
		/// grid and a column name inside a row. A segment that matches two siblings names neither.
		/// </summary>
		public static object Find(object root, string path)
		{
			if (root == null || string.IsNullOrEmpty(path))
				return null;
			var current = root;
			var names = path.Split('/');
			for (var i = 0; i < names.Length; i++)
			{
				if (names[i].Length == 0)
					return null;
				// A row is named by two segments together, because an index alone would be a name a
				// column could also carry.
				var grid = current as DataGridView;
				if (grid != null && names[i] == RowsSegment)
				{
					if (i + 1 == names.Length)
						return null;
					current = RowOf(grid, names[++i]);
				}
				else
				{
					current = ChildOf(current, names[i]);
				}
				if (current == null)
					return null;
			}
			return current;
		}

		/// <summary>The one element inside another that a segment names, or null.</summary>
		static object ChildOf(object element, string name)
		{
			var child = ChildOfForms(element, name);
			ChildOfWpf(element, name, ref child);
			return child;
		}

		/// <summary>What an element holds, as text. Null for elements that hold nothing, and for what must not be read out.</summary>
		public static string GetValue(object element)
		{
			var item = element as ToolStripItem;
			if (item != null)
				return GetValue(item);
			var row = element as DataGridViewRow;
			if (row != null)
				return RowValue(row);
			var gridCell = element as DataGridViewCell;
			if (gridCell != null)
				return TextOf(gridCell);
			var control = element as Control;
			if (control != null)
				return GetValue(control);
			string value = null;
			GetValueWpf(element, ref value);
			return value;
		}

		/// <summary>Sets an element from text. Returns null when done, otherwise why not.</summary>
		public static string SetValue(object element, string value)
		{
			var item = element as ToolStripItem;
			if (item != null)
				return SetValue(item, value);
			if (element is DataGridViewRow)
				return "A row is selected by setting the grid it is in to the row's index.";
			var checkCell = element as DataGridViewCheckBoxCell;
			if (checkCell != null)
				return SetValue(checkCell, value);
			if (element is DataGridViewCell)
				return "Only a check box in a row is set. A button in a row is pressed with ui_invoke, and a row is selected by setting the grid to its index.";
			var control = element as Control;
			if (control != null)
				return SetValue(control, value);
			var refused = "There is nothing here to set.";
			SetValueWpf(element, value, ref refused);
			return refused;
		}

		/// <summary>Brings the pages above an element to the front, so the element is the one on screen; a page itself comes to the front too.</summary>
		public static void Reveal(object element)
		{
			var control = ControlOf(element);
			if (control != null)
				Reveal(control);
			RevealWpf(element);
		}

		/// <summary>
		/// Presses a button. What would refuse the press is checked before anything is touched, then
		/// the pages above the button are brought to the front, because a click on a button that is
		/// not showing does nothing and says nothing. Returns null when done, otherwise why not.
		/// </summary>
		public static string Invoke(object element)
		{
			var item = element as ToolStripItem;
			if (item != null)
				return Invoke(item);
			var row = element as DataGridViewRow;
			if (row != null)
				return Invoke(row);
			var cell = element as DataGridViewCell;
			if (cell != null)
				return Invoke(cell);
			var control = element as Control;
			if (control != null)
				return Invoke(control);
			var refused = "This element is not pressed. Use ui_set to change it.";
			InvokeWpf(element, ref refused);
			return refused;
		}

		/// <summary>
		/// The field name an element is guarded by. A row or a cell answers with its grid's name:
		/// what a grid holds is data, and a rule about a control is a rule about the whole grid.
		/// </summary>
		public static string IdOf(object element)
		{
			var item = element as ToolStripItem;
			if (item != null)
				return item.Name;
			var control = ControlOf(element);
			if (control != null)
				return control.Name;
			string name = null;
			IdOfWpf(element, ref name);
			return name;
		}

		/// <summary>
		/// Adds a child, or - when the child is only a container drawn to arrange others - adds what
		/// was inside it instead, so the tree holds the elements a person reaches rather than the
		/// scaffolding they are arranged with.
		/// </summary>
		static void Attach(UiNode parent, UiNode child, bool raw)
		{
			if (child == null)
				return;
			if (!raw && IsDecoration(child))
				return;
			// Several controls often set one value: a slider, a box to type in, and a box to step
			// through. Listing all three says the same thing three times, so only the first is kept.
			// Every one of them still carries the name and purpose for a screen reader.
			if (!raw && parent.Items != null && parent.Items.Exists(x => Same(x, child)))
				return;
			if (!raw && IsScaffolding(child))
			{
				if (child.Items != null)
					foreach (var inner in child.Items)
						parent.Add(inner);
				return;
			}
			parent.Add(child);
		}

		/// <summary>
		/// True for an element that is there to caption or decorate something else. A label reading
		/// "Dead Zone:" names the box beside it, which now carries that name itself, so keeping the
		/// label states the same fact twice and points at nothing a reader can use. Giving one a
		/// name or a purpose deliberately is how it earns a place in the tree.
		/// </summary>
		static bool IsDecoration(UiNode node)
		{
			if (node.Role != "Label" && node.Role != "Picture" && node.Role != "Status")
				return false;
			// A readout on the status bar says something worth knowing and is described; a caption
			// on a toolbar names the box beside it and is not. The description is what tells them
			// apart, because deciding to describe something is the act of saying it matters.
			return string.IsNullOrEmpty(node.Description);
		}

		/// <summary>
		/// True for a node that exists to position other elements and states nothing itself: no name
		/// of its own, and a kind that a person cannot act on. Such a node in the tree would say only
		/// that the program uses panels.
		/// </summary>
		static bool IsScaffolding(UiNode node)
		{
			if (node.Role != "Group")
				return false;
			// A control this program defines is a thing in its own right - the controller panel, the
			// mapping row - and is where a description belongs. Only the plain panels a designer
			// drops in to position other things are scaffolding.
			if (!string.IsNullOrEmpty(node.Type))
				return false;
			if (!string.IsNullOrEmpty(node.Description))
				return false;
			return string.IsNullOrEmpty(node.Name);
		}

		/// <summary>
		/// True when two elements would read identically. The kind is deliberately not compared:
		/// a slider and a box that set one value are one setting, and saying it once is the point.
		/// </summary>
		static bool Same(UiNode a, UiNode b)
		{
			if (string.IsNullOrEmpty(a.Name)
				|| !string.Equals(a.Name, b.Name, StringComparison.Ordinal)
				|| !string.Equals(a.Description, b.Description, StringComparison.Ordinal))
				return false;
			// One setting is often offered as a slider in per cent beside a box in raw units. They
			// read alike but do not accept alike, so both are kept and their ranges tell them apart.
			// Where neither states a range, or both state the same one, there is one thing to say.
			return a.Min == b.Min && a.Max == b.Max;
		}

		/// <summary>
		/// The name a person hears. The accessible name is the deliberate answer; the caption is what
		/// they read on screen. Neither means the element is unnamed, and the field name is not a
		/// substitute - it is left empty so the coverage check can see the gap.
		/// </summary>
		/// <param name="holdsValue">True for a box whose text is what the user typed or chose.</param>
		static string NameOf(string accessibleName, string documentName, string text, bool holdsValue)
		{
			var name = Clean(accessibleName);
			if (!string.IsNullOrEmpty(name))
				return name;
			// A reading on the status bar is deliberately left unnamed, so that what it reads out
			// stays audible. The document still wants a fixed name for it, and keeps one aside.
			name = Clean(documentName);
			if (!string.IsNullOrEmpty(name))
				return name;
			// A box holds what the user typed or chose. Its content is a value, and naming an
			// element after its value produces a document that describes one machine on one day.
			if (holdsValue)
				return null;
			return Clean(text);
		}

		static string Clean(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			return value.Replace("&", "").Replace("\r", " ").Replace("\n", " ").Trim();
		}

		/// <summary>The path of something inside an element, or null where either cannot be addressed.</summary>
		static string ChildPath(string path, string name)
		{
			if (path == null || string.IsNullOrEmpty(name))
				return null;
			return path.Length == 0 ? name : path + "/" + name;
		}

		/// <summary>True for a type the program defines: one in a namespace listed in <see cref="OwnNamespaces"/>.</summary>
		internal static bool IsOwnType(Type type)
		{
			var space = type.Namespace ?? "";
			foreach (var own in OwnNamespaces)
				if (space.StartsWith(own, StringComparison.Ordinal))
					return true;
			return false;
		}

		#region WPF elements

		// Written in UiTreeWalker.WPF.cs. Each one answers only for a WPF element and leaves the
		// answer it is given otherwise; without that file the calls are not compiled at all.

		static partial void ReadWpf(object element, bool raw, string path, ref UiNode node);

		static partial void ChildOfWpf(object element, string name, ref object child);

		static partial void GetValueWpf(object element, ref string value);

		static partial void SetValueWpf(object element, string value, ref string refused);

		static partial void RevealWpf(object element);

		static partial void InvokeWpf(object element, ref string refused);

		static partial void IdOfWpf(object element, ref string name);

		#endregion
	}
}
