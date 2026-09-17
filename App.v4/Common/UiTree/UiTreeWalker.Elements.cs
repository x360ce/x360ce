using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace x360ce.App.UiTree
{
	/// <summary>
	/// The parts of the interface that are not controls: the entries on a bar or a menu, and the
	/// rows of a grid together with the buttons drawn in them. A person presses these as readily
	/// as a button, so each is addressed by a path, described, read, set and pressed like any
	/// other element.
	/// </summary>
	public static partial class UiTreeWalker
	{
		/// <summary>The segment that stands between a grid's path and the index of one of its rows.</summary>
		const string RowsSegment = "rows";

		#region Addressing

		/// <summary>
		/// The one element inside another that a segment names, or null. A name matching two
		/// siblings names neither, because a path that points at two places points at nothing.
		/// </summary>
		static object ChildOf(object element, string name)
		{
			var control = element as Control;
			if (control != null)
			{
				var controls = ChildrenOf(control).Where(x => x.Name == name).Take(2).ToList();
				if (controls.Count > 0)
					return controls.Count == 1 ? controls[0] : null;
				return OneItem(ItemsOf(control), name);
			}
			var parent = element as ToolStripDropDownItem;
			if (parent != null)
				return OneItem(parent.DropDownItems.Cast<ToolStripItem>(), name);
			var row = element as DataGridViewRow;
			if (row == null || row.DataGridView == null)
				return null;
			var column = row.DataGridView.Columns.Cast<DataGridViewColumn>().FirstOrDefault(x => x.Name == name);
			return column == null ? null : row.Cells[column.Index];
		}

		static ToolStripItem OneItem(IEnumerable<ToolStripItem> items, string name)
		{
			var matches = items.Where(x => x.Name == name).Take(2).ToList();
			return matches.Count == 1 ? matches[0] : null;
		}

		/// <summary>The row a grid's path and index name, or null when there is no such row.</summary>
		static DataGridViewRow RowOf(DataGridView grid, string index)
		{
			int i;
			if (!int.TryParse(index, out i) || i < 0 || i >= grid.Rows.Count)
				return null;
			var row = grid.Rows[i];
			// The empty row a grid offers for typing a new one into is not a row of the data.
			return row.IsNewRow ? null : row;
		}

		/// <summary>
		/// The control an element is drawn in: itself, the bar an entry sits on, or the grid a row
		/// belongs to. What points at an element on screen has to be a control, so this is what a
		/// frame is drawn around and what a window is found from.
		/// </summary>
		public static Control ControlOf(object element)
		{
			var control = element as Control;
			if (control != null)
				return control;
			var item = element as ToolStripItem;
			if (item != null)
				return StripOf(item);
			var cell = element as DataGridViewCell;
			if (cell != null)
				return cell.DataGridView;
			var row = element as DataGridViewRow;
			return row == null ? null : row.DataGridView;
		}

		/// <summary>
		/// The bar an entry is on, following a menu that dropped out of another back to the bar it
		/// hangs from, because a menu that is not open is nowhere on screen.
		/// </summary>
		static Control StripOf(ToolStripItem item)
		{
			var walk = item;
			while (walk != null)
			{
				var owner = walk.Owner;
				var dropDown = owner as ToolStripDropDown;
				var parent = dropDown == null ? null : dropDown.OwnerItem;
				if (parent == null)
					return owner;
				walk = parent;
			}
			return null;
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
			return control == null ? null : control.Name;
		}

		#endregion

		#region Bar and menu entries

		/// <summary>Describes a menu or tool strip entry and everything under it.</summary>
		static UiNode Read(ToolStripItem item, bool raw, string path)
		{
			var node = Describe(item);
			if (path != null)
			{
				node.Path = path.Length == 0 ? null : path;
				node.Value = GetValue(item);
			}
			var parent = item as ToolStripDropDownItem;
			if (parent != null)
				foreach (ToolStripItem child in parent.DropDownItems)
					Attach(node, Read(child, raw, ChildPath(path, child.Name)), raw);
			return node;
		}

		/// <summary>The path of something inside an element, or null where either cannot be addressed.</summary>
		static string ChildPath(string path, string name)
		{
			if (path == null || string.IsNullOrEmpty(name))
				return null;
			return path.Length == 0 ? name : path + "/" + name;
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
		/// A label on a bar reports something; a button does something; a button that stays pressed
		/// says whether something is on. Telling them apart keeps a reading of the tree from
		/// suggesting a reader can press the frame rate, and lets the third kind be set rather than
		/// only pressed.
		/// </summary>
		static string RoleOf(ToolStripItem item)
		{
			if (item is ToolStripSeparator)
				return "Separator";
			if (item is ToolStripTextBox)
				return "Text";
			if (item is ToolStripComboBox)
				return "List";
			if (IsToggle(item))
				return "CheckBox";
			if (item is ToolStripLabel || item is ToolStripStatusLabel)
				return "Status";
			return "Button";
		}

		/// <summary>
		/// True for an entry that stands for something being on or off rather than for an action.
		/// A bar entry turns over on the click when it is told to; where the program turns it over
		/// itself, it says so by giving the entry the role of a check button, which is also what a
		/// screen reader announces.
		/// </summary>
		static bool IsToggle(ToolStripItem item)
		{
			if (item.AccessibleRole == AccessibleRole.CheckButton)
				return true;
			var button = item as ToolStripButton;
			if (button != null)
				return button.CheckOnClick || button.Checked;
			var menu = item as ToolStripMenuItem;
			return menu != null && (menu.CheckOnClick || menu.Checked);
		}

		static bool? CheckedOf(ToolStripItem item)
		{
			var button = item as ToolStripButton;
			if (button != null)
				return button.Checked;
			var menu = item as ToolStripMenuItem;
			return menu == null ? (bool?)null : menu.Checked;
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

		static string GetValue(ToolStripItem item)
		{
			var role = RoleOf(item);
			if (role == "CheckBox")
			{
				var check = CheckedOf(item);
				return check.HasValue ? check.Value.ToString() : null;
			}
			return role == "Text" || role == "List" || role == "Status" ? item.Text : null;
		}

		static string SetValue(ToolStripItem item, string value)
		{
			var role = RoleOf(item);
			if (role == "CheckBox")
			{
				bool wanted;
				if (!bool.TryParse(value, out wanted))
					return "Expected true or false.";
				// The work is done in the handlers on Click, and an entry told to turn over does so
				// on the click as well, so setting the state here would change the picture and
				// nothing else.
				return CheckedOf(item) == wanted ? null : Invoke(item);
			}
			var list = item as ToolStripComboBox;
			if (list != null)
			{
				for (var i = 0; i < list.Items.Count; i++)
					if (string.Equals(list.ComboBox.GetItemText(list.Items[i]), value, StringComparison.OrdinalIgnoreCase)) { list.SelectedIndex = i; return null; }
				return "No such item. Items: " + string.Join(", ", list.Items.Cast<object>().Select(x => list.ComboBox.GetItemText(x)).ToArray());
			}
			var text = item as ToolStripTextBox;
			if (text != null) { text.Text = value; return null; }
			if (role == "Status")
				return "This shows a value and cannot be typed into.";
			return "This element is not one that is set. Use ui_invoke for buttons.";
		}

		static string Invoke(ToolStripItem item)
		{
			var role = RoleOf(item);
			if (role != "Button" && role != "CheckBox")
				return "This element is not pressed. Use ui_set to change it.";
			if (!item.Enabled)
				return "This button is disabled now.";
			if (!item.Available)
				return "This button is not offered now.";
			var strip = StripOf(item);
			var window = strip == null ? null : strip.FindForm();
			if (window == null || !window.Visible)
				return "The window is hidden, so nothing can be pressed. Restore it first.";
			Reveal(strip);
			item.PerformClick();
			return null;
		}

		#endregion

		#region Grid rows

		/// <summary>
		/// Adds a grid's rows, which are described only when the tree is read with a path. Rows are
		/// what one machine holds at one moment rather than a feature of the program, so the
		/// exported document says what the grid is and leaves what is in it to whoever is looking.
		/// </summary>
		static void AddRows(UiNode node, DataGridView grid, string path)
		{
			if (grid == null || path == null)
				return;
			var rows = path.Length == 0 ? RowsSegment : path + "/" + RowsSegment;
			for (var i = 0; i < grid.Rows.Count; i++)
			{
				var row = grid.Rows[i];
				if (row.IsNewRow)
					continue;
				node.Add(Read(row, rows + "/" + i));
			}
		}

		/// <summary>One row, named and read from the cells a person can see, with its buttons under it.</summary>
		static UiNode Read(DataGridViewRow row, string path)
		{
			var node = new UiNode
			{
				Name = RowName(row),
				Role = "Row",
				Id = row.Index.ToString(),
				Hidden = !row.Visible,
				Path = path,
				Value = RowValue(row),
			};
			foreach (var cell in row.Cells.Cast<DataGridViewCell>().Where(x => x is DataGridViewButtonCell && IsShown(x)))
				node.Add(Read(cell, ChildPath(path, cell.OwningColumn.Name)));
			return node;
		}

		/// <summary>One cell: a button to press, or a value to read.</summary>
		static UiNode Read(DataGridViewCell cell, string path)
		{
			var column = cell.OwningColumn;
			var text = TextOf(cell);
			return new UiNode
			{
				Name = text ?? Clean(column == null ? null : column.HeaderText),
				Role = cell is DataGridViewButtonCell ? "Button" : "Value",
				Id = column == null ? null : column.Name,
				Hidden = !IsShown(cell),
				Path = path,
				Value = text,
			};
		}

		/// <summary>
		/// What the row is called: the cell under a column headed Name, because that is the column a
		/// person reads a row by, and the first cell with anything in it where there is no such column.
		/// </summary>
		static string RowName(DataGridViewRow row)
		{
			string first = null;
			foreach (var cell in TextCells(row))
			{
				var column = cell.OwningColumn;
				if (string.Equals(column.HeaderText, "Name", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(column.Name, "Name", StringComparison.OrdinalIgnoreCase))
					return TextOf(cell);
				if (first == null)
					first = TextOf(cell);
			}
			return first;
		}

		/// <summary>The row as a person reads it: each column that says something, with its heading.</summary>
		static string RowValue(DataGridViewRow row)
		{
			var parts = new List<string>();
			foreach (var cell in TextCells(row))
			{
				var text = TextOf(cell);
				if (string.IsNullOrEmpty(text))
					continue;
				var column = cell.OwningColumn;
				var header = Clean(column.HeaderText) ?? column.Name;
				parts.Add(header + ": " + text);
			}
			return string.Join("; ", parts.ToArray());
		}

		/// <summary>
		/// The cells that carry words. A picture says nothing that can be written down here, and a
		/// button is an action rather than part of what the row says, so both are left out.
		/// </summary>
		static IEnumerable<DataGridViewCell> TextCells(DataGridViewRow row)
		{
			return row.Cells.Cast<DataGridViewCell>()
				.Where(x => IsShown(x) && !(x is DataGridViewImageCell) && !(x is DataGridViewButtonCell));
		}

		static bool IsShown(DataGridViewCell cell)
		{
			return cell.OwningColumn != null && cell.OwningColumn.Visible;
		}

		static string TextOf(DataGridViewCell cell)
		{
			var value = cell.FormattedValue;
			return value == null ? null : Clean(value.ToString());
		}

		static string Invoke(DataGridViewRow row)
		{
			var buttons = row.Cells.Cast<DataGridViewCell>()
				.Where(x => x is DataGridViewButtonCell && IsShown(x))
				.Select(x => x.OwningColumn.Name).ToArray();
			if (buttons.Length == 0)
				return "A row is not pressed, and this one holds no buttons.";
			return "A row is not pressed. Press one of its buttons: " + string.Join(", ", buttons) + ".";
		}

		/// <summary>
		/// Presses the button drawn in a cell. Windows Forms offers no public way to press one: the
		/// grid raises the cell events itself when a person clicks, and the handlers that do the
		/// work hang off those events, so the same two are raised here through the protected methods
		/// that raise them.
		/// </summary>
		static string Invoke(DataGridViewCell cell)
		{
			if (!(cell is DataGridViewButtonCell))
				return "This cell is not pressed. Use ui_set on the grid to select a row.";
			var grid = cell.DataGridView;
			if (grid == null)
				return "This cell is no longer in a grid.";
			if (!grid.Enabled)
				return "This grid is disabled now.";
			var window = grid.FindForm();
			if (window == null || !window.Visible)
				return "The window is hidden, so nothing can be pressed. Restore it first.";
			var rowIndex = cell.RowIndex;
			var columnIndex = cell.ColumnIndex;
			if (rowIndex < 0)
				return "This row is not one that can be pressed.";
			Reveal(grid);
			// Bringing the page to the front can rebuild the grid's rows - the Issues page refreshes
			// its list as it is shown - which leaves the cell found a moment ago belonging to no
			// grid. The press goes to whatever now sits at the same place, or to nothing.
			if (rowIndex >= grid.Rows.Count || columnIndex >= grid.Columns.Count)
				return "The grid changed while the page was brought to the front. Read it again.";
			cell = grid.Rows[rowIndex].Cells[columnIndex];
			if (!(cell is DataGridViewButtonCell))
				return "The grid changed while the page was brought to the front. Read it again.";
			if (!IsShown(cell) || !cell.OwningRow.Visible)
				return "This button is not shown now.";
			grid.CurrentCell = cell;
			Raise(grid, "OnCellClick", cell);
			Raise(grid, "OnCellContentClick", cell);
			return null;
		}

		static void Raise(DataGridView grid, string method, DataGridViewCell cell)
		{
			var raise = typeof(DataGridView).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
			raise.Invoke(grid, new object[] { new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex) });
		}

		#endregion
	}
}
