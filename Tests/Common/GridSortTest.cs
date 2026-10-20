// @under-test: App.v4/Controls/PadControl.Designer.cs, App.v4/Controls/UserDevicesUserControl.Designer.cs, App.v4/Controls/PadTabPages/GamesControl.Designer.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// F3 sorts a grid on its current column, and a grid bound to data can be sorted only on a column bound to it. On any
	/// other column the grid throws "Data-bound DataGridView control can only be sorted on data-bound columns", as a
	/// 4.26.38.0 report showed, so such a column must not be sortable.
	/// </summary>
	[TestClass]
	public class GridSortTest
	{
		[TestMethod, TestCategory("ui")]
		[Description("A grid bound to data has no sortable column that is not bound to it, which F3 would sort and throw on")]
		public void Bound_grids_sort_only_bound_columns()
		{
			var root = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var wrong = new List<string>();
			foreach (var designer in Directory.GetFiles(root, "*.Designer.cs", SearchOption.AllDirectories))
			{
				if (designer.Contains(@"\obj\") || designer.Contains(@"\bin\"))
					continue;
				var text = File.ReadAllText(designer);
				var code = designer.Substring(0, designer.Length - ".Designer.cs".Length) + ".cs";
				var source = File.Exists(code) ? File.ReadAllText(code) : "";
				foreach (Match grid in Regex.Matches(text, @"this\.(\w+)\.Columns\.AddRange\(new System\.Windows\.Forms\.DataGridViewColumn\[\] \{([^}]*)\}"))
				{
					var name = grid.Groups[1].Value;
					if (!Regex.IsMatch(source, @"\b" + name + @"\.DataSource\s*=\s*[^n]"))
						continue;
					foreach (Match column in Regex.Matches(grid.Groups[2].Value, @"this\.(\w+)"))
					{
						var c = column.Groups[1].Value;
						if (!text.Contains("this." + c + " = new System.Windows.Forms.DataGridViewTextBoxColumn()"))
							continue;
						if (Regex.IsMatch(text, @"this\." + c + @"\.DataPropertyName = ""[^""]+"""))
							continue;
						if (Regex.IsMatch(text, @"this\." + c + @"\.SortMode = System\.Windows\.Forms\.DataGridViewColumnSortMode\.(NotSortable|Programmatic)"))
							continue;
						wrong.Add(Path.GetFileName(designer) + ": " + name + "." + c);
					}
				}
			}
			Assert.AreEqual(0, wrong.Count, "Sortable columns that are not bound to their grid's data, which F3 sorts and the grid throws on: "
				+ string.Join(", ", wrong));
		}
	}
}
