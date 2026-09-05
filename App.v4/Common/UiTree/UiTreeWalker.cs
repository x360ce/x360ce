using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.App.UiTree
{
	/// <summary>Reads the interface the program built and describes it as a tree of elements.</summary>
	/// <remarks>
	/// Everything the designer created is walked, including pages and panels not on screen at the
	/// moment, because a page nobody has clicked yet is still a feature of the program.
	/// </remarks>
	public static class UiTreeWalker
	{
		/// <summary>Describes a control and everything inside it.</summary>
		/// <param name="control">Control to start from, usually the main window.</param>
		/// <param name="raw">
		/// True keeps every control, including the panels that exist only to arrange others.
		/// Used to see the whole picture before deciding what a person can actually reach.
		/// </param>
		/// <param name="path">
		/// The control's own path, which also asks for state: null reads without either, as the
		/// export does; "" is the main window, which is no segment of any path; "Tabs/Page1" is a branch.
		/// </param>
		public static UiNode Read(Control control, bool raw = false, string path = null)
		{
			var node = Describe(control);
			if (path != null)
			{
				node.Path = path.Length == 0 ? null : path;
				node.Value = GetValue(control);
			}
			foreach (var child in ChildrenOf(control))
			{
				// A control without a name cannot be addressed, so it and its subtree carry no path.
				var childPath = path == null || string.IsNullOrEmpty(child.Name) ? null : path.Length == 0 ? child.Name : path + "/" + child.Name;
				var childNode = Read(child, raw, childPath);
				if (childNode != null)
					Attach(node, childNode, raw);
			}
			foreach (var item in ItemsOf(control))
			{
				if (!raw && IsSpacer(item))
					continue;
				Attach(node, Read(item, raw), raw);
			}
			return node;
		}

		/// <summary>The one control a path names, or null. Paths are control names from the root's children down, joined by '/'. A segment that matches two siblings names neither.</summary>
		public static Control Find(Control root, string path)
		{
			if (root == null || string.IsNullOrEmpty(path))
				return null;
			var current = root;
			foreach (var name in path.Split('/'))
			{
				if (name.Length == 0)
					return null;
				var matches = ChildrenOf(current).Where(c => c.Name == name).Take(2).ToList();
				if (matches.Count != 1)
					return null;
				current = matches[0];
			}
			return current;
		}

		/// <summary>What a control holds, as text. Null for controls that hold nothing, and for what must not be read out.</summary>
		public static string GetValue(Control control)
		{
			var check = control as CheckBox;
			if (check != null) return check.Checked.ToString();
			var choice = control as RadioButton;
			if (choice != null) return choice.Checked.ToString();
			var slider = control as TrackBar;
			if (slider != null) return slider.Value.ToString();
			var number = control as NumericUpDown;
			if (number != null) return number.Value.ToString();
			var list = control as ComboBox;
			if (list != null) return list.SelectedItem == null ? "" : list.GetItemText(list.SelectedItem);
			var tabs = control as TabControl;
			if (tabs != null) return tabs.SelectedTab == null ? "" : tabs.SelectedTab.Name;
			var grid = control as DataGridView;
			if (grid != null) return grid.CurrentRow == null ? "" : grid.CurrentRow.Index.ToString();
			// A password is not read out, and the help and log boxes are pages, not values.
			var text = control as TextBox;
			if (text != null) return text.UseSystemPasswordChar || text.PasswordChar != '\0' ? null : text.Text;
			if (control is RichTextBox) return null;
			var label = control as Label;
			if (label != null) return label.Text;
			return null;
		}

		/// <summary>Sets a control from text. Returns null when done, otherwise why not.</summary>
		public static string SetValue(Control control, string value)
		{
			var check = control as CheckBox;
			if (check != null) { bool b; if (!bool.TryParse(value, out b)) return "Expected true or false."; check.Checked = b; return null; }
			var choice = control as RadioButton;
			if (choice != null) { bool b; if (!bool.TryParse(value, out b)) return "Expected true or false."; choice.Checked = b; return null; }
			var slider = control as TrackBar;
			if (slider != null) { int i; if (!int.TryParse(value, out i) || i < slider.Minimum || i > slider.Maximum) return "Expected a number from " + slider.Minimum + " to " + slider.Maximum + "."; slider.Value = i; return null; }
			var number = control as NumericUpDown;
			if (number != null) { decimal d; if (!decimal.TryParse(value, out d) || d < number.Minimum || d > number.Maximum) return "Expected a number from " + number.Minimum + " to " + number.Maximum + "."; number.Value = d; return null; }
			var list = control as ComboBox;
			if (list != null)
			{
				for (var i = 0; i < list.Items.Count; i++)
					if (string.Equals(list.GetItemText(list.Items[i]), value, StringComparison.OrdinalIgnoreCase)) { list.SelectedIndex = i; return null; }
				return "No such item. Items: " + string.Join(", ", list.Items.Cast<object>().Select(x => list.GetItemText(x)));
			}
			var tabs = control as TabControl;
			if (tabs != null)
			{
				foreach (TabPage page in tabs.TabPages)
					if (page.Name == value || page.Text == value) { tabs.SelectedTab = page; return null; }
				return "No such tab.";
			}
			var grid = control as DataGridView;
			if (grid != null)
			{
				int i;
				var last = grid.Rows.Count - (grid.AllowUserToAddRows ? 2 : 1);
				if (!int.TryParse(value, out i) || i < 0 || i > last) return "Expected a row index from 0 to " + last + ".";
				var column = grid.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
				if (column == null || !grid.Rows[i].Visible) return "That row cannot be selected.";
				grid.CurrentCell = grid.Rows[i].Cells[column.Index];
				return null;
			}
			var text = control as TextBoxBase;
			if (text != null) { if (text.ReadOnly) return "This shows a value and cannot be typed into."; text.Text = value; return null; }
			return "This element is not one that is set. Use ui_invoke for buttons.";
		}

		/// <summary>
		/// Presses a button. What would refuse the press is checked before anything is touched, then
		/// the pages above the button are brought to the front, because a click on a button that is
		/// not showing does nothing and says nothing. Returns null when done, otherwise why not.
		/// </summary>
		public static string Invoke(Control control)
		{
			var button = control as Button;
			if (button == null)
				return "This element is not pressed. Use ui_set to change it.";
			if (!button.Enabled)
				return "This button is disabled now.";
			var window = button.FindForm();
			if (window == null || !window.Visible)
				return "The window is hidden, so nothing can be pressed. Restore it first.";
			for (var c = control.Parent; c != null; c = c.Parent)
			{
				var page = c as TabPage;
				var tabs = page == null ? null : page.Parent as TabControl;
				if (tabs != null)
					tabs.SelectedTab = page;
			}
			if (!button.CanSelect)
				return "This button cannot be pressed now: something above it is disabled.";
			button.PerformClick();
			return null;
		}

		/// <summary>Describes a menu or tool strip item and everything under it.</summary>
		static UiNode Read(ToolStripItem item, bool raw)
		{
			var node = Describe(item);
			var parent = item as ToolStripDropDownItem;
			if (parent != null)
				foreach (ToolStripItem child in parent.DropDownItems)
					Attach(node, Read(child, raw), raw);
			return node;
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
		/// True for a node that exists to position other elements and states nothing itself: no name
		/// of its own, and a kind that a person cannot act on. Such a node in the tree would say only
		/// that the program uses panels.
		/// </summary>
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

		/// <summary>Children in the order a person moves through them, rather than drawing order.</summary>
		static IEnumerable<Control> ChildrenOf(Control control)
		{
			var tabs = control as TabControl;
			if (tabs != null)
				return tabs.TabPages.Cast<Control>();
			// A grid, a box that spins and a box with a list attached are each one thing to a person.
			// The scroll bars and edit boxes they are assembled from are not places to navigate to.
			if (control is DataGridView || control is UpDownBase || control is ComboBox)
				return Enumerable.Empty<Control>();
			return control.Controls.Cast<Control>()
				.OrderBy(x => x.TabIndex)
				.ThenBy(x => x.Top)
				.ThenBy(x => x.Left);
		}

		/// <summary>Menu and tool strip entries, which are not controls and so are not children.</summary>
		static IEnumerable<ToolStripItem> ItemsOf(Control control)
		{
			var strip = control as ToolStrip;
			if (strip != null)
				return strip.Items.Cast<ToolStripItem>();
			return Enumerable.Empty<ToolStripItem>();
		}

		static UiNode Describe(Control control)
		{
			var node = new UiNode
			{
				Name = NameOf(control.AccessibleName, UiText.NameFor(control), control.Text, control),
				Description = Clean(control.AccessibleDescription),
				Role = RoleOf(control),
				Id = control.Name,
				// The control's own flag, not whether it happens to be on screen. Everything on a tab
				// that is not the selected one reports itself as not visible, and a page a person
				// reaches by clicking its tab is not hidden.
				Hidden = !JocysCom.ClassLibrary.Controls.ControlsHelper.IsVisible(control) && !(control is TabPage),
			};
			if (IsOwnType(control.GetType()))
				node.Type = control.GetType().Name;
			var slider = control as TrackBar;
			if (slider != null)
			{
				node.Min = slider.Minimum;
				node.Max = slider.Maximum;
			}
			var number = control as NumericUpDown;
			if (number != null)
			{
				node.Min = (int)number.Minimum;
				node.Max = (int)number.Maximum;
			}
			return node;
		}

		static UiNode Describe(ToolStripItem item)
		{
			return new UiNode
			{
				Name = NameOf(item.AccessibleName, UiText.NameFor(item), item.Text, null),
				Description = Clean(item.AccessibleDescription),
				Role = RoleOf(item),
				Id = item.Name,
				// Whether the program means to offer it, not whether the menu happens to be open.
				Hidden = !item.Available,
			};
		}

		/// <summary>
		/// A label on a bar reports something; a button does something. Telling them apart keeps a
		/// reading of the tree from suggesting a reader can press the frame rate.
		/// </summary>
		static string RoleOf(ToolStripItem item)
		{
			if (item is ToolStripSeparator)
				return "Separator";
			if (item is ToolStripLabel || item is ToolStripStatusLabel)
				return "Status";
			if (item is ToolStripTextBox)
				return "Text";
			if (item is ToolStripComboBox)
				return "List";
			return "Command";
		}

		/// <summary>
		/// True for a strip item that exists only to push the ones after it along. It states
		/// nothing, and a reader cannot reach it.
		/// </summary>
		static bool IsSpacer(ToolStripItem item)
		{
			var label = item as ToolStripStatusLabel;
			return label != null && label.Spring && string.IsNullOrWhiteSpace(label.Text)
				&& string.IsNullOrEmpty(label.AccessibleName);
		}

		/// <summary>
		/// The name a person hears. The accessible name is the deliberate answer; the caption is what
		/// they read on screen. Neither means the element is unnamed, and the field name is not a
		/// substitute - it is left empty so the coverage check can see the gap.
		/// </summary>
		static string NameOf(string accessibleName, string documentName, string text, Control control)
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
			if (control is TextBoxBase || control is ComboBox || control is UpDownBase
				|| control is DateTimePicker || control is ListControl)
				return null;
			return Clean(text);
		}

		static string Clean(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			return value.Replace("&", "").Replace("\r", " ").Replace("\n", " ").Trim();
		}

		static bool IsOwnType(Type type)
		{
			var space = type.Namespace ?? "";
			return space.StartsWith("x360ce", StringComparison.Ordinal)
				|| space.StartsWith("JocysCom", StringComparison.Ordinal);
		}

		/// <summary>What kind of thing this is, in words rather than type names.</summary>
		static string RoleOf(Control control)
		{
			if (control is TabPage) return "Tab";
			if (control is TabControl) return "Tabs";
			if (control is Form) return "Window";
			if (control is DataGridView) return "Grid";
			if (control is CheckBox) return "CheckBox";
			if (control is RadioButton) return "Choice";
			if (control is ComboBox) return "List";
			if (control is TrackBar) return "Slider";
			if (control is NumericUpDown) return "Number";
			if (control is Button) return "Button";
			if (control is TextBoxBase)
				return ((TextBoxBase)control).ReadOnly ? "Value" : "Text";
			if (control is LinkLabel) return "Link";
			if (control is Label) return "Label";
			if (control is PictureBox) return "Picture";
			if (control is ProgressBar) return "Progress";
			if (control is ListBox || control is ListView || control is TreeView) return "List";
			if (control is ToolStrip) return "Toolbar";
			if (control is GroupBox) return "Section";
			return "Group";
		}
	}
}
