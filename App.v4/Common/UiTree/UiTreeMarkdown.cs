using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App.UiTree
{
	/// <summary>Draws the tree as text, in the shape a person reads in a terminal.</summary>
	public static class UiTreeMarkdown
	{
		/// <summary>Width of the kind, which stands at the left of every line.</summary>
		/// <remarks>
		/// The kind leads rather than follows the branch, because a column of kinds can only be
		/// read down if it starts where the line does. Behind the branch it would step right with
		/// the depth, which is the one place it cannot be read down.
		/// </remarks>
		const int KindWidth = 12;

		/// <summary>Width reserved for the branch and the name together.</summary>
		/// <remarks>
		/// Reserved rather than measured. Widening to fit the longest name in the document would
		/// move every line whenever one name changed; a fixed column moves only the line that
		/// outgrew it. Seventy-six leaves 1% of the lines in this program overhanging, against 16%
		/// at fifty-six, and the branch alone is forty characters deep at its deepest.
		/// </remarks>
		const int NameWidth = 76;

		/// <summary>The whole document: a short preamble, then the tree.</summary>
		public static string Write(UiNode root)
		{
			var sb = new StringBuilder();
			sb.AppendLine("# " + root.Name + " navigation tree");
			sb.AppendLine();
			sb.AppendLine("Written by the program itself, so it describes the build it came from.");
			sb.AppendLine("Regenerate with `x360ce.exe /ExportUi=<folder>`. A relative folder is taken");
			sb.AppendLine("from the program's own folder, because that is where the program works from.");
			sb.AppendLine();
			sb.AppendLine("- **Controls** describes each control that appears in more than one place, once.");
			sb.AppendLine("- **App** is the main window. A `-> Name` line stands for a control described above.");
			sb.AppendLine("- **Tray** is the menu behind the icon in the notification area.");
			sb.AppendLine();
			sb.AppendLine("Every line carries three things in columns of their own, so each can be read");
			sb.AppendLine("straight down: what kind of element it is, where it sits and what it is called,");
			sb.AppendLine("and what it is for. A range such as `0..100` appears where the element holds a");
			sb.AppendLine("number, and says what it will accept.");
			sb.AppendLine();
			sb.AppendLine("A setting offered through several controls at once is listed once, unless they");
			sb.AppendLine("accept different ranges - a slider in per cent beside a box in raw units are two");
			sb.AppendLine("different things to set, so both are kept.");
			sb.AppendLine();
			sb.AppendLine("Kinds: `Tab`, `Tabs`, `Section` and `Group` hold other elements. `Button`,");
			sb.AppendLine("`Command` and `Link` are pressed. `CheckBox`, `Choice`, `List`, `Slider`,");
			sb.AppendLine("`Number` and `Text` are set. `Value`, `Status` and `Grid` are read, not typed in.");
			sb.AppendLine();
			sb.AppendLine("```");
			sb.AppendLine(Columns("[Kind]", "Where it sits and what it is called", "What it is for"));
			sb.AppendLine(Columns("", root.Name, null));
			WriteChildren(sb, root, "");
			sb.AppendLine("```");
			return sb.ToString();
		}

		static void WriteChildren(StringBuilder sb, UiNode node, string indent)
		{
			var items = Shown(node);
			for (var i = 0; i < items.Count; i++)
			{
				var child = items[i];
				var last = i == items.Count - 1;
				var branch = indent + (last ? "└── " : "├── ");
				sb.AppendLine(Line(child, branch));
				WriteChildren(sb, child, indent + (last ? "    " : "│   "));
			}
		}

		/// <summary>Separators divide a menu visually and are nothing to navigate to.</summary>
		static List<UiNode> Shown(UiNode node)
		{
			if (node.Items == null)
				return new List<UiNode>();
			return node.Items.Where(x => x.Role != "Separator").ToList();
		}

		static string Line(UiNode node, string branch)
		{
			var name = new StringBuilder(branch);
			name.Append(string.IsNullOrEmpty(node.Name) ? "(" + node.Id + ")" : node.Name);
			if (node.Min.HasValue && node.Max.HasValue)
			{
				name.Append(" ");
				name.Append(node.Min.Value);
				name.Append("..");
				name.Append(node.Max.Value);
			}
			if (!string.IsNullOrEmpty(node.SameAs))
				name.Append(" -> " + node.SameAs);
			if (node.Hidden)
				name.Append(" (hidden)");
			return Columns("[" + node.Role + "]", name.ToString(), node.Description);
		}

		/// <summary>
		/// Lays the three parts of a line into their columns. A part that has outgrown its column
		/// pushes the rest along rather than being cut, because a name is worth more than a margin.
		/// </summary>
		static string Columns(string kind, string name, string purpose)
		{
			var line = new StringBuilder();
			line.Append(Pad(kind, KindWidth));
			if (string.IsNullOrEmpty(purpose))
				return (line + name).TrimEnd();
			line.Append(Pad(name, NameWidth));
			line.Append("# ");
			line.Append(purpose);
			return line.ToString();
		}

		static string Pad(string text, int width)
		{
			return text.Length < width ? text.PadRight(width) : text + "  ";
		}
	}
}
