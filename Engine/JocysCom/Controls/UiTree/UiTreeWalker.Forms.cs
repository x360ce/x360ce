#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	// The library's own WPF NumericUpDown in the parent namespace would otherwise hide this one.
	using NumericUpDown = System.Windows.Forms.NumericUpDown;

	/// <summary>The Windows Forms controls: described, found, set and pressed.</summary>
	public static partial class UiTreeWalker
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
			else
			{
				// The document names an element whose words follow the program's state the same on every computer.
				node.Name = UiText.LiveNameFor(control) ?? node.Name;
			}
			foreach (var child in ChildrenOf(control))
			{
				// A control without a name cannot be addressed, so it and its subtree carry no path.
				var childNode = Read(child, raw, ChildPath(path, child.Name));
				if (childNode != null)
					Attach(node, childNode, raw);
			}
			foreach (var item in ItemsOf(control))
			{
				if (!raw && IsSpacer(item))
					continue;
				Attach(node, Read(item, raw, ChildPath(path, item.Name)), raw);
			}
			AddRows(node, control as DataGridView, path);
			return node;
		}

		/// <summary>What a control holds, as text. Null for controls that hold nothing, and for what must not be read out.</summary>
		static string GetValue(Control control)
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
			if (list != null) return list.DropDownStyle != ComboBoxStyle.DropDownList ? list.Text : list.SelectedItem == null ? "" : list.GetItemText(list.SelectedItem);
			var tabs = control as TabControl;
			if (tabs != null) return tabs.SelectedTab == null ? "" : tabs.SelectedTab.Name;
			// A tab's hint can say what its colour cannot, such as what is connected and what is missing.
			var page = control as TabPage;
			if (page != null) return string.IsNullOrEmpty(page.ToolTipText) ? null : page.ToolTipText;
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
		static string SetValue(Control control, string value)
		{
			// A person cannot change a disabled control either.
			if (!control.Enabled)
				return "This element is disabled now.";
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
				// A list that takes typing takes any words, as a person may type a value not in it.
				if (list.DropDownStyle != ComboBoxStyle.DropDownList) { list.Text = value; return null; }
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

		/// <summary>Brings the pages above a control to the front, so the control is the one on screen; a page itself comes to the front too.</summary>
		static void Reveal(Control control)
		{
			for (var c = control; c != null; c = c.Parent)
			{
				var page = c as TabPage;
				var tabs = page == null ? null : page.Parent as TabControl;
				if (tabs != null)
					tabs.SelectedTab = page;
			}
		}

		/// <summary>Presses a button control. Returns null when done, otherwise why not.</summary>
		static string Invoke(Control control)
		{
			var button = control as Button;
			if (button == null)
				return "This element is not pressed. Use ui_set to change it.";
			if (!button.Enabled)
				return "This button is disabled now.";
			var window = button.FindForm();
			if (window == null || !window.Visible)
				return "The window is hidden, so nothing can be pressed. Restore it first.";
			Reveal(button);
			if (!button.CanSelect)
				return "This button cannot be pressed now: something above it is disabled.";
			button.PerformClick();
			return null;
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
				Name = NameOf(control.AccessibleName, UiText.NameFor(control), control.Text, HoldsValue(control)),
				Description = Clean(control.AccessibleDescription),
				Role = RoleOf(control),
				Id = control.Name,
				// The control's own flag, not whether it happens to be on screen. Everything on a tab that
				// is not the selected one reports itself as not visible, and a page a person reaches by
				// clicking its tab is not hidden.
				Hidden = !ControlsHelper.IsVisible(control) && !(control is TabPage),
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

		/// <summary>True for a box whose text is what the user typed or chose, which is a value and not a name.</summary>
		static bool HoldsValue(Control control)
		{
			return control is TextBoxBase || control is ComboBox || control is UpDownBase
				|| control is DateTimePicker || control is ListControl;
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
