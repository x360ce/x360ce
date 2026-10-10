// @under-test: Engine/JocysCom/Controls/UiTree/UiTreeWalker.Forms.Elements.cs, Engine/JocysCom/Controls/UiTree/UiTreeWalker.cs
// @area: accessibility   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.UiTree;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace x360ce.Tests
{
	/// <summary>
	/// The two kinds of element a person uses that are not controls: the entries on a bar, and the
	/// rows of a grid with the buttons drawn in them. Each has to be named by a path, described,
	/// read, set and pressed, or half of what the program offers is out of reach.
	/// </summary>
	[TestClass]
	public class UiTreeElementsTest
	{
		/// <summary>A bar with a toggle and a plain button, and a grid with one row that has a button in it.</summary>
		static Form Build()
		{
			var form = new Form { Name = "Main" };
			var bar = new ToolStrip { Name = "Bar" };
			var toggle = new ToolStripButton { Name = "Toggle", Text = "Show state", CheckOnClick = true };
			var press = new ToolStripButton { Name = "Press", Text = "Press" };
			bar.Items.Add(toggle);
			bar.Items.Add(press);
			var grid = new DataGridView { Name = "Grid", AllowUserToAddRows = false };
			grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NameColumn", HeaderText = "Name" });
			grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DetailColumn", HeaderText = "Detail" });
			grid.Columns.Add(new DataGridViewButtonColumn { Name = "FixColumn", HeaderText = "Solution" });
			grid.Rows.Add("Driver missing", "Install it again", "Remove");
			form.Controls.Add(grid);
			form.Controls.Add(bar);
			return form;
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("critical")]
		[Description("A bar entry and a grid row carry a path, a kind and a value, and the exported document carries no rows")]
		public void Entries_and_rows_are_described()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = Build())
				{
					form.Show();
					var root = UiTreeWalker.Read(form, false, "");
					var toggle = Find(root, "Bar/Toggle");
					Assert.IsNotNull(toggle, "A bar entry with no path cannot be set or pressed by a caller.");
					Assert.AreEqual("CheckBox", toggle.Role, "An entry that turns over on the click stands for something being on or off.");
					Assert.AreEqual("False", toggle.Value, "A toggle that says nothing about its state is half described.");
					var press = Find(root, "Bar/Press");
					Assert.IsNotNull(press, "The plain entry is not in the tree by path.");
					Assert.AreEqual("Button", press.Role);
					Assert.IsNull(press.Value, "A button holds nothing to read.");
					var row = Find(root, "Grid/rows/0");
					Assert.IsNotNull(row, "A grid read by path lists what is in it, or nothing in it can be pressed.");
					Assert.AreEqual("Row", row.Role);
					Assert.AreEqual("0", row.Id);
					Assert.AreEqual("Driver missing", row.Name, "A row is named by the column headed Name.");
					Assert.AreEqual("Name: Driver missing; Detail: Install it again", row.Value,
						"A row reads as what a person sees in it, headed by the column it sits under.");
					var fix = Find(root, "Grid/rows/0/FixColumn");
					Assert.IsNotNull(fix, "The button in the row has no path, so nothing can press it.");
					Assert.AreEqual("Button", fix.Role);
					Assert.AreEqual("Remove", fix.Name, "The button is called what it says on it.");
					// A row belongs to one machine at one moment. The exported document describes the
					// program, and a coverage check counts what is in it, so rows stay out of it.
					var exported = UiTreeWalker.Read(form);
					Assert.IsFalse(Any(exported, x => x.Role == "Row"),
						"The exported tree lists rows, which are data rather than features of the program.");
					// A rule about a control is a rule about the whole grid, so what is in it answers
					// with the grid's own name when the door checks what it may touch.
					Assert.AreEqual("Grid", UiTreeWalker.IdOf(UiTreeWalker.Find(form, "Grid/rows/0/FixColumn")));
					Assert.AreEqual("Toggle", UiTreeWalker.IdOf(UiTreeWalker.Find(form, "Bar/Toggle")));
				}
			});
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("critical")]
		[Description("A bar toggle is set by turning it over, a bar button and a row's button are pressed, and a row itself is not")]
		public void Entries_and_row_buttons_are_set_and_pressed()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = Build())
				{
					form.Show();
					var toggle = (ToolStripButton)UiTreeWalker.Find(form, "Bar/Toggle");
					var toggled = 0;
					toggle.Click += (s, e) => toggled++;
					// The work hangs off Click, so a toggle that was set without a click would leave the
					// picture changed and the program none the wiser.
					Assert.IsNull(UiTreeWalker.SetValue(toggle, "true"));
					Assert.AreEqual(1, toggled, "Setting a bar toggle must go through the click its handlers hang off.");
					Assert.IsTrue(toggle.Checked);
					Assert.IsNull(UiTreeWalker.SetValue(toggle, "true"), "Setting what is already set is done, not refused.");
					Assert.AreEqual(1, toggled, "Setting a toggle to what it already is must not press it again.");
					Assert.IsNotNull(UiTreeWalker.SetValue(toggle, "maybe"), "Anything but true or false must be refused.");

					var press = (ToolStripButton)UiTreeWalker.Find(form, "Bar/Press");
					var pressed = 0;
					press.Click += (s, e) => pressed++;
					Assert.IsNull(UiTreeWalker.Invoke(press));
					Assert.AreEqual(1, pressed, "A bar button was not pressed.");
					Assert.IsNotNull(UiTreeWalker.SetValue(press, "true"), "A button is pressed, not set.");
					press.Enabled = false;
					Assert.IsNotNull(UiTreeWalker.Invoke(press), "A disabled entry cannot be pressed and must say so.");
					Assert.AreEqual(1, pressed);

					var grid = (DataGridView)UiTreeWalker.Find(form, "Grid");
					var clicks = 0;
					var clickedColumn = -1;
					var clickedRow = -1;
					grid.CellContentClick += (s, e) => { clicks++; clickedColumn = e.ColumnIndex; clickedRow = e.RowIndex; };
					var cell = UiTreeWalker.Find(form, "Grid/rows/0/FixColumn");
					Assert.IsInstanceOfType(cell, typeof(DataGridViewButtonCell), "The column name inside a row names its cell.");
					Assert.IsNull(UiTreeWalker.Invoke(cell));
					Assert.AreEqual(1, clicks, "The button in the row was not pressed: its handler hangs off the grid's cell events.");
					Assert.AreEqual(grid.Columns["FixColumn"].Index, clickedColumn);
					Assert.AreEqual(0, clickedRow);
					Assert.AreSame(grid.Rows[0].Cells[clickedColumn], grid.CurrentCell,
						"A press lands on the cell the person would have clicked, so the row is the current one.");

					var row = UiTreeWalker.Find(form, "Grid/rows/0");
					var refused = UiTreeWalker.Invoke(row);
					Assert.IsNotNull(refused, "A row is not a button.");
					StringAssert.Contains(refused, "FixColumn", "A refusal that does not name the buttons leaves the caller stuck.");
					Assert.AreEqual(1, clicks, "A refused press must not reach the grid.");
					Assert.IsNull(UiTreeWalker.Find(form, "Grid/rows/7"), "A row that is not there names nothing.");
					Assert.IsNull(UiTreeWalker.Find(form, "Grid/rows"), "A grid's rows are named one at a time.");
				}
			});
		}

		/// <summary>A row of a bound grid, as a controller tab's list of mapped devices holds one.</summary>
		public class Row
		{
			public string Name { get; set; }
			public bool Enabled { get; set; }
			public bool Hidden { get; set; }
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("critical")]
		[Description("A check box in a row is described, ticked and unticked as a click does, whether the program or the grid changes it")]
		public void Row_check_boxes_are_read_and_set()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { Name = "Main" })
				{
					var rows = new System.ComponentModel.BindingList<Row> { new Row { Name = "Pad", Enabled = true } };
					var grid = new DataGridView { Name = "Grid", AllowUserToAddRows = false, AutoGenerateColumns = false };
					grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NameColumn", HeaderText = "Name", DataPropertyName = "Name" });
					// As on a controller tab: the box only shows the value, and a handler of the click changes it.
					var enabled = new DataGridViewCheckBoxColumn { Name = "IsEnabledColumn", HeaderText = "Enabled", DataPropertyName = "Enabled", ReadOnly = true };
					grid.Columns.Add(enabled);
					// A box the grid edits itself, with no handler behind it.
					grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "HiddenColumn", HeaderText = "Hidden", DataPropertyName = "Hidden" });
					var clicks = 0;
					grid.CellClick += (s, e) =>
					{
						if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex] != enabled)
							return;
						clicks++;
						var item = (Row)grid.Rows[e.RowIndex].DataBoundItem;
						item.Enabled = !item.Enabled;
					};
					grid.DataSource = rows;
					form.Controls.Add(grid);
					form.Show();

					var node = Find(UiTreeWalker.Read(form, false, ""), "Grid/rows/0/IsEnabledColumn");
					Assert.IsNotNull(node, "A row's check box has no path, so nothing can tick it.");
					Assert.AreEqual("CheckBox", node.Role);
					Assert.AreEqual("Enabled", node.Name, "A check box in a row is named by its column.");
					Assert.AreEqual("True", node.Value, "A check box that does not say whether it is ticked is half described.");

					var cell = UiTreeWalker.Find(form, "Grid/rows/0/IsEnabledColumn");
					Assert.IsNull(UiTreeWalker.SetValue(cell, "false"));
					Assert.AreEqual(1, clicks, "Unticking must go through the click the program's handler hangs off.");
					Assert.IsFalse(rows[0].Enabled, "The row the box stands for was not changed.");
					Assert.IsNull(UiTreeWalker.SetValue(cell, "false"), "Setting what is already set is done, not refused.");
					Assert.AreEqual(1, clicks, "A box already as asked must not be clicked again.");
					Assert.IsNull(UiTreeWalker.SetValue(cell, "true"));
					Assert.IsTrue(rows[0].Enabled);
					Assert.IsNotNull(UiTreeWalker.SetValue(cell, "maybe"), "Anything but true or false must be refused.");
					StringAssert.Contains(UiTreeWalker.Invoke(cell), "ui_set", "Pressing a check box must say how it is set.");

					var hidden = UiTreeWalker.Find(form, "Grid/rows/0/HiddenColumn");
					Assert.IsNull(UiTreeWalker.SetValue(hidden, "true"));
					Assert.IsTrue(rows[0].Hidden, "A box the grid edits itself was not set.");
					Assert.IsNotNull(UiTreeWalker.SetValue(UiTreeWalker.Find(form, "Grid/rows/0/NameColumn"), "x"), "A text cell is not set.");
				}
			});
		}

		static UiNode Find(UiNode node, string path)
		{
			if (node.Path == path)
				return node;
			if (node.Items == null)
				return null;
			return node.Items.Select(x => Find(x, path)).FirstOrDefault(x => x != null);
		}

		static bool Any(UiNode node, System.Func<UiNode, bool> test)
		{
			return test(node) || (node.Items != null && node.Items.Any(x => Any(x, test)));
		}
	}
}
