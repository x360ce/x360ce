// @under-test: App.v4/Common/UiTree/UiTreeWalker.cs, App.v4/Common/UiTree/UiText.cs
// @area: accessibility   @layer: ui-winforms
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using x360ce.App.UiTree;

namespace x360ce.Tests
{
	/// <summary>
	/// The exported navigation tree is the account of the program other people and other programs
	/// read, so what it must contain is checked here rather than noticed months later.
	/// </summary>
	/// <remarks>
	/// The export is produced by running the program, because the program is what is being
	/// described. Anything else would test a second description of the interface and prove nothing
	/// about the first.
	/// </remarks>
	[TestClass]
	public class UiTreeTest
	{
		/// <summary>
		/// Elements a person acts on, navigates to, or reads a value from. A caption is not one of
		/// them: it names the control beside it, which carries that name itself.
		/// </summary>
		static readonly string[] Actionable =
		{
			"Button", "CheckBox", "Choice", "List", "Slider", "Number", "Text", "Link",
			"Grid", "Tab", "Command", "Tabs", "Toolbar", "Control", "Section",
			"Value", "Status",
		};

		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("Every element a person can reach says what it is called and what it is for")]
		public void Exported_tree_names_and_describes_everything_reachable()
		{
			var root = Export();
			var missingName = new List<string>();
			var missingPurpose = new List<string>();
			Walk(root, node =>
			{
				var role = node.Role;
				if (!Actionable.Contains(role))
					return;
				var where = role + " " + (node.Id ?? "?");
				if (string.IsNullOrEmpty(node.Name))
					missingName.Add(where);
				else if (string.IsNullOrEmpty(node.Description))
					missingPurpose.Add(where + " (" + node.Name + ")");
			});
			Assert.AreEqual(0, missingName.Count, string.Format(
				"{0} element(s) a person can reach have no accessible name, so a screen reader "
				+ "announces them as blank and the exported tree cannot say what they are. Give each "
				+ "one an entry in UiText: {1}",
				missingName.Count, string.Join(", ", missingName.Take(20).ToArray())));
			Assert.AreEqual(0, missingPurpose.Count, string.Format(
				"{0} element(s) are named but say nothing about what they do, which is the half that "
				+ "makes the document worth reading. Give each one an entry in UiText: {1}",
				missingPurpose.Count, string.Join(", ", missingPurpose.Take(20).ToArray())));
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("The tree covers every part of the program, not just the page that was open")]
		public void Exported_tree_covers_the_whole_program()
		{
			var root = Export();
			var names = new List<string>();
			Walk(root, node => names.Add(node.Name ?? ""));
			// Parts built at different moments: the designer, the staged start-up, and the menu that
			// hangs off the notification icon rather than off the window. An export taken too early
			// loses the second and third of those, and looks complete while missing most of the app.
			var required = new[]
			{
				"Controls", "App", "Tray",
				"Controller 1", "Options", "Games", "Devices", "Issues", "About",
				"PadControl", "AxisMapUserControl",
				"Exit",
			};
			var absent = required.Where(x => !names.Contains(x)).ToArray();
			Assert.AreEqual(0, absent.Length, string.Format(
				"The export is missing {0}. Parts of the window are built in stages after it opens, "
				+ "so an export taken before the program finished building describes only a fragment.",
				string.Join(", ", absent)));
			Assert.IsTrue(names.Count > 300, string.Format(
				"Only {0} elements were exported. The program has more than that, so something "
				+ "stopped the walk early.", names.Count));
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("Nothing is listed twice unless the two differ in kind or in what they accept")]
		public void Exported_tree_says_each_thing_once()
		{
			var root = Export();
			var repeated = new List<string>();
			Walk(root, node =>
			{
				var seen = new List<string>();
				foreach (var child in Items(node))
				{
					if (string.IsNullOrEmpty(child.Name))
						continue;
					// Kind and range are part of what a line says. A slider in per cent and a box in
					// raw units are two things to set, and listing both is right; two lines a reader
					// cannot tell apart are not.
					var line = string.Join("|", new[]
					{
						child.Name, child.Description, child.Role,
						child.Min.ToString(), child.Max.ToString(),
					});
					if (seen.Contains(line))
						repeated.Add(node.Name + " > " + child.Role + " " + child.Name);
					seen.Add(line);
				}
			});
			Assert.AreEqual(0, repeated.Count, string.Format(
				"{0} element(s) appear twice under the same parent and read identically: {1}. "
				+ "A reader cannot tell them apart, so one of them says nothing.",
				repeated.Count, string.Join(", ", repeated.Take(10).ToArray())));
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("A box that cannot be typed in is not described as one that can")]
		public void Exported_tree_tells_values_from_fields()
		{
			var root = Export();
			var values = 0;
			var fields = 0;
			Walk(root, node =>
			{
				if (node.Role == "Value")
					values++;
				if (node.Role == "Text")
					fields++;
			});
			// Many boxes in this program report something rather than accept it. Calling those
			// editable invites a reader to type into a box that ignores them. The count is of the
			// tree, where a control used in several places is described once, not of the source.
			Assert.IsTrue(values > 20, string.Format(
				"Only {0} elements are marked as read-only values. Read-only boxes are being "
				+ "described as fields a reader can type into.", values));
			Assert.IsTrue(fields > 0, "No editable text field was found, which cannot be right.");
			// One known case, so the count above cannot be satisfied by the wrong elements.
			var product = Find(root, "Product name");
			Assert.IsNotNull(product, "The device's product name is missing from the tree.");
			Assert.AreEqual("Value", product.Role,
				"The device's product name is read from the device and cannot be typed in.");
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("The exported JSON is JSON a strict reader accepts")]
		public void Exported_json_is_readable_by_a_strict_reader()
		{
			var folder = Run();
			var bytes = File.ReadAllBytes(Path.Combine(folder, "ui-tree.json"));
			// A byte order mark is not part of JSON and a strict reader refuses a document that
			// starts with one. This file exists to be read by other programs.
			Assert.IsFalse(bytes.Length > 2 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
				"The exported JSON starts with a byte order mark, which strict readers refuse.");
			var text = Encoding.UTF8.GetString(bytes);
			foreach (var ch in text)
			{
				Assert.IsFalse(char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t',
					"The exported JSON contains a raw control character, so it is not valid JSON.");
			}
			var parsed = JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromJson<UiNode>(text);
			Assert.IsNotNull(parsed, "The exported JSON could not be read back.");
			Assert.IsTrue(parsed.Items != null && parsed.Items.Count > 0,
				"The exported JSON read back empty.");
		}

		#region Running the program

		static UiNode _tree;
		static string _folder;

		/// <summary>Runs the export once, and reuses it: each run costs the program's start-up.</summary>
		static UiNode Export()
		{
			if (_tree != null)
				return _tree;
			var text = File.ReadAllText(Path.Combine(Run(), "ui-tree.json"));
			_tree = JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromJson<UiNode>(text);
			Assert.IsNotNull(_tree, "The exported tree could not be read back.");
			return _tree;
		}

		static string Run()
		{
			if (_folder != null)
				return _folder;
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-ui-tree-" + Guid.NewGuid().ToString("N"));
			var exe = Ui.FindApp("App.v4");
			var app = Process.Start(new ProcessStartInfo(exe, "/ExportUi=" + folder)
			{
				WorkingDirectory = Path.GetDirectoryName(exe),
			});
			// Long enough for a slow machine to read every device once, which start-up does.
			Assert.IsTrue(app.WaitForExit(180000),
				"The program did not finish exporting. It waits for its window to be fully built.");
			Assert.IsTrue(File.Exists(Path.Combine(folder, "ui-tree.json")),
				"The program exited without writing the navigation tree to " + folder + ".");
			_folder = folder;
			return folder;
		}

		#endregion

		#region Reading the tree

		static void Walk(UiNode node, Action<UiNode> visit)
		{
			visit(node);
			foreach (var child in Items(node))
				Walk(child, visit);
		}

		static UiNode Find(UiNode node, string name)
		{
			if (node.Name == name)
				return node;
			foreach (var child in Items(node))
			{
				var found = Find(child, name);
				if (found != null)
					return found;
			}
			return null;
		}

		static IEnumerable<UiNode> Items(UiNode node)
		{
			return node.Items ?? Enumerable.Empty<UiNode>();
		}

		#endregion
	}
}
