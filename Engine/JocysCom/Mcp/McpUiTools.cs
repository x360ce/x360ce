#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// What an assistant or a script can ask of the program through its interface. Each public
	/// static method marked McpTool is one tool; its name, arguments and description are read from
	/// the method, so a tool is defined here and nowhere else. The catalogue runs every body on the
	/// interface thread; a tool refuses by throwing, and the message is what the caller reads. Each
	/// program adds tools of its own in a class it lists in <see cref="McpCatalog.Sources"/>, and
	/// fills in the hooks below. A window is a Windows Forms form or a WPF window; what differs
	/// between the two is in McpUiTools.Forms.cs and McpUiTools.WPF.cs. The shared files reach the
	/// WPF side only through partial methods, so a Windows Forms program that leaves
	/// McpUiTools.WPF.cs out compiles and loads no WPF.
	/// </summary>
	public static partial class McpUiTools
	{
		/// <summary>Window the path tools start from: a form, a control or a WPF window. The main window unless a test says otherwise.</summary>
		public static object Root;

		/// <summary>The program's main window: a form or a WPF window.</summary>
		public static Func<object> MainWindow = () => null;

		/// <summary>The menu behind the tray icon, or null where there is none.</summary>
		public static Func<ContextMenuStrip> TrayMenu = () => null;

		/// <summary>Brings the main window back from the tray, the way the program itself does.</summary>
		public static Action<object> Restore = RestoreWindow;

		/// <summary>The help page the program shows, as Markdown.</summary>
		public static Func<string> HelpText = () => "";

		/// <summary>Turns the help's links to the program's other documents into addresses a caller can open. Leaves the text as it is unless the program says otherwise.</summary>
		public static Func<string, string> ResolveLinks = text => text;

		/// <summary>
		/// Field names of the elements that administer the computer or act with the person's access:
		/// install a driver, run a job. Below Administer these may not be touched.
		/// </summary>
		public static string[] AdminControls = new string[0];

		/// <summary>
		/// Field names of the door's own controls. No caller may touch them at any level: the level is
		/// a person's choice, the port is where the door is, and the token is what lets the caller in.
		/// </summary>
		public static string[] DoorControls = new string[0];

		/// <summary>
		/// Field names of the elements whose value is a secret: a key or a token. Their value is
		/// never read out through the door, and never written to the log.
		/// </summary>
		public static string[] SecretControls = new string[0];

		static object RootWindow { get { return Root ?? MainWindow(); } }

		/// <summary>The tree as read, with the value of every secret left out, at any depth.</summary>
		static UiNode Withheld(UiNode node)
		{
			if (node == null)
				return null;
			if (SecretControls.Contains(node.Id))
				node.Value = null;
			if (node.Items != null)
				foreach (var child in node.Items)
					Withheld(child);
			return node;
		}

		[McpTool(AiAccess.Read, "The program's windows that are open besides the main one, such as a dialog waiting for an answer, as JSON: Path, Name, Modal. Read one with ui_read and its Path; the other tools take paths that start with it.")]
		public static object UiWindows()
		{
			return OtherWindows().Select(x => (object)new Dictionary<string, object> { { "Path", WindowName(x) }, { "Name", WindowTitle(x) }, { "Modal", IsModal(x) }, }).ToArray();
		}

		[McpTool(AiAccess.Read, "What the person is looking at now, as JSON: Windows (every open window of the program: Path, Name, Modal, Active), Window (the path of the window in front, empty for the main one), Tabs (the pages shown in it, from the top down), Focus (the element with keyboard focus: Path, Role, Name, Value) and Row (the current row, when Focus is a grid). The paths work with the other tools.")]
		public static object UiCurrent()
		{
			var root = RootWindow;
			object current = null;
			CurrentOfWpf(root, ref current);
			return current ?? CurrentOf(root as Control);
		}

		/// <summary>An element as ui_current reports it.</summary>
		static Dictionary<string, object> Current(object element, string path)
		{
			var node = Withheld(UiTreeWalker.Read(element, false, path));
			return new Dictionary<string, object>
			{
				{ "Path", path }, { "Role", node?.Role }, { "Name", node?.Name }, { "Value", node?.Value },
			};
		}

		[McpTool(AiAccess.Read, "The interface as a tree: every element with its kind, name, purpose, path and current value, as JSON. Pass a path to read one branch; a grid read this way lists its rows and the buttons in them. Sibling controls that read alike are listed once; setting the one shown sets both. A path may start with a window from ui_windows.")]
		public static string UiRead([Description("Element path from an earlier ui_read; omit for the whole window.")] string path = null)
		{
			path = path ?? "";
			var start = FindElement(path);
			if (start == null)
				throw new InvalidOperationException("No element at " + path + ".");
			var node = Withheld(UiTreeWalker.Read(start, false, path));
			return UiTreeExporter.ToJson(node, false);
		}

		[McpTool(AiAccess.Configure, "Sets an element by path: true/false for a CheckBox or Choice, a number for a Slider or Number, an item's shown text for a List, a tab name for Tabs, a row index for a Grid, text for Text.")]
		public static string UiSet([Description("Element path from ui_read.")] string path, [Description("The value, as text.")] string value)
		{
			var refused = UiTreeWalker.SetValue(Resolve(path), value);
			if (refused != null)
				throw new InvalidOperationException(refused);
			return null;
		}

		[McpTool(AiAccess.Configure, "Presses a Button by path, whether it stands on its own, on a bar, or in a grid row; the tabs above it are selected first. A press that opens a window answers as soon as the window waits, with its path and the buttons that answer it; press one with ui_invoke, and the press that opened it carries on. Buttons that administer the computer or run work with the person's access need Administer access.")]
		public static string UiInvoke([Description("Element path from ui_read.")] string path)
		{
			var refused = UiTreeWalker.Invoke(Resolve(path));
			if (refused != null)
				throw new InvalidOperationException(refused);
			return null;
		}

		/// <summary>How long a walk points at each tab on the way, in milliseconds.</summary>
		public static int WalkStepMs = 1500;

		/// <summary>The tab pages that hold an element, outermost first: the tabs a person opens, in order, to reach it.</summary>
		public static List<object> TabsOnTheWay(object element)
		{
			List<object> pages = null;
			TabsOnTheWayWpf(element, ref pages);
			return pages ?? TabsOnTheWay(UiTreeWalker.ControlOf(element));
		}

		/// <summary>Brings a tab to the front, as a click on it does.</summary>
		static void SelectTab(object page)
		{
			var tab = page as TabPage;
			if (tab != null)
			{
				((TabControl)tab.Parent).SelectedTab = tab;
				return;
			}
			SelectTabWpf(page);
		}

		/// <summary>The words on a tab, as a person reads them.</summary>
		static string TabText(object page)
		{
			var tab = page as TabPage;
			if (tab != null)
				return tab.Text.Trim();
			string text = null;
			TabTextWpf(page, ref text);
			return text;
		}

		/// <summary>Frames an element for the person, or says it is hidden: a frame around nothing on screen points at nothing.</summary>
		static void PointAt(object element, string text, int seconds)
		{
			var pointed = false;
			PointAtWpf(element, text, seconds, ref pointed);
			if (!pointed)
				PointAt(UiTreeWalker.ControlOf(element), text, seconds);
		}

		[McpTool(AiAccess.Read, "Points at an element for the person: brings its page to the front, restores the window from the tray if need be, frames the element and shows a balloon with your words beside it. A tab page is framed by its tab. With walk, first opens and points at each tab on the way, outermost first, a moment each, so the person learns the way there. Waits the seconds before answering, so several calls in a row make a paced walkthrough. With 0 seconds it answers at once and the frame stays until the person clicks or types in the program, the element moves, or ui_hide takes it away, for at most 10 minutes, so a screenshot can show it; Shift, Ctrl, Alt, Windows and Print Screen, which take screenshots, leave it.", OnUiThread = false)]
		public static string UiShow([Description("Element path from ui_read.")] string path, [Description("What to say beside it, in the person's language.")] string text = null, [Description("How long to point, 1 to 60 seconds; 0 keeps it until the person acts or ui_hide.")] int seconds = 5,
			[Description("True opens and points at each tab on the way first, so the person learns the way there.")] bool walk = false)
		{
			seconds = seconds <= 0 ? 0 : Math.Min(60, seconds);
			object element = null;
			var way = new List<object>();
			McpCatalog.OnUiThread(() =>
			{
				element = FindElement(path);
				if (element == null)
					throw new InvalidOperationException("No element at " + path + ".");
				// "Show me" means the window too: a person asking cannot see a tray icon's insides.
				var main = MainWindow();
				if (main != null && WindowOf(element) == main && !IsShowing(main))
					Restore(main);
				if (walk)
					way = TabsOnTheWay(element);
			});
			// A tab at a time, as the person would click them, each balloon naming the click.
			foreach (var page in way)
			{
				McpCatalog.OnUiThread(() =>
				{
					SelectTab(page);
					PointAt(page, "Open the " + TabText(page) + " tab", Math.Max(1, (WalkStepMs + 999) / 1000));
				});
				Thread.Sleep(WalkStepMs);
			}
			McpCatalog.OnUiThread(() =>
			{
				UiTreeWalker.Reveal(element);
				PointAt(element, text, seconds);
			});
			// The pause is the point: the person reads the balloon before the next step arrives.
			Thread.Sleep(seconds * 1000);
			return null;
		}

		[McpTool(AiAccess.Read, "Takes away the frame and balloon ui_show left on screen, such as one shown with 0 seconds once its screenshot is taken.")]
		public static string UiHide()
		{
			if (UiCallout.Target == null)
				return "Nothing is pointed at.";
			UiCallout.Hide();
			return null;
		}

		/// <summary>The most elements one search returns: a common word matches hundreds across the program's pages.</summary>
		public const int FindLimit = 40;

		[McpTool(AiAccess.Read, "Finds elements with every word somewhere in their name, purpose, field name or path, in any order and case, as JSON: Path, Role, Name, Description, Value. Best matches first, a word in the name counting most; at most 40, and when there are more a last element with Role Note says how many. A page's name among the words keeps the search to that page. Cheaper than reading the whole tree; use the Path with the other tools.")]
		public static object UiFind([Description("Words to look for, any order and case.")] string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new InvalidOperationException("Give a word to look for.");
			var words = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			var found = new List<KeyValuePair<int, object>>();
			Collect(Withheld(UiTreeWalker.Read(RootWindow, false, "")), words, found);
			foreach (var window in OtherWindows())
				Collect(Withheld(UiTreeWalker.Read(window, false, WindowName(window))), words, found);
			// The sort keeps the tree's order among equal matches.
			var shown = found.OrderByDescending(x => x.Key).Select(x => x.Value).Take(FindLimit).ToList();
			// Cut off without a word, a search reads as complete, and the setting that was left out is
			// reported as one the program does not have.
			if (found.Count > FindLimit)
				shown.Add(new Dictionary<string, object>
				{
					{ "Path", null }, { "Role", "Note" },
					{ "Name", FindLimit + " of " + found.Count + " shown, best matches first. Add a word, such as a page's name, to narrow the search." },
				});
			return shown.ToArray();
		}

		static void Collect(UiNode node, string[] words, List<KeyValuePair<int, object>> found)
		{
			if (node.Path != null && words.All(w => Has(node.Name, w) || Has(node.Description, w) || Has(node.Id, w) || Has(node.Path, w)))
				found.Add(new KeyValuePair<int, object>(Score(node, words), new Dictionary<string, object>
				{
					{ "Path", node.Path }, { "Role", node.Role }, { "Name", node.Name }, { "Description", node.Description }, { "Value", node.Value },
				}));
			if (node.Items != null)
				foreach (var child in node.Items)
					Collect(child, words, found);
		}

		/// <summary>How well an element answers the words: each counts most in the name a person reads, then in its purpose, then in its field name, and nothing when only the path has it.</summary>
		static int Score(UiNode node, string[] words)
		{
			return words.Sum(w => Has(node.Name, w) ? 3 : Has(node.Description, w) ? 2 : Has(node.Id, w) ? 1 : 0);
		}

		static bool Has(string text, string query)
		{
			return text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		[McpTool(AiAccess.Read, "Runs a small script, one step per line, in order: 'show <path> | <words> | <seconds>' points at an element, and 0 seconds keeps it until the person acts; 'walk <path> | <words> | <seconds>' does the same after opening and pointing at each tab on the way; 'hide' takes the frame away; 'click <path>' presses a button; 'set <path> | <value>' sets an element; 'wait <seconds>' pauses. Lines starting with # are ignored. Stops at the first step that fails and says which. A click that opens a window goes on to the next line, which may answer it, and a window still waiting at the end is named with its buttons. show, walk, hide and wait need Read access; click and set need Configure.", OnUiThread = false, Changes = true)]
		public static string UiScript([Description("The steps, one per line.")] string script)
		{
			var lines = (script ?? "").Replace("\r", "").Split('\n');
			var done = 0;
			var opened = false;
			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i].Trim();
				if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
					continue;
				var space = line.IndexOf(' ');
				var verb = (space < 0 ? line : line.Substring(0, space)).ToLowerInvariant();
				// A value or a sentence may hold a '|' of its own: only as many parts are cut as the step has.
				var parts = (space < 0 ? "" : line.Substring(space + 1)).Split(new[] { '|' }, verb == "set" ? 2 : 3).Select(x => x.Trim()).ToArray();
				try
				{
					switch (verb)
					{
						case "show":
						case "walk":
							int showFor;
							UiShow(parts[0], parts.Length > 1 ? parts[1] : null, parts.Length > 2 && int.TryParse(parts[2], out showFor) ? showFor : 5, verb == "walk");
							break;
						case "hide":
							McpCatalog.OnUiThread(UiCallout.Hide);
							break;
						case "wait":
							int waitFor;
							Thread.Sleep(Math.Max(1, Math.Min(60, int.TryParse(parts[0], out waitFor) ? waitFor : 1)) * 1000);
							break;
						case "click":
							RequireConfigure();
							McpCatalog.OnUiThread(() => UiInvoke(parts[0]));
							break;
						case "set":
							RequireConfigure();
							if (parts.Length < 2)
								throw new InvalidOperationException("set needs a path and a value: set <path> | <value>.");
							McpCatalog.OnUiThread(() => UiSet(parts[0], parts[1]));
							break;
						default:
							throw new InvalidOperationException("Unknown step '" + verb + "'. Steps are show, walk, hide, click, set and wait.");
					}
				}
				catch (WindowWaitingException)
				{
					// The step is done, and the window it opened waits for a later line to answer it.
					opened = true;
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException("Line " + (i + 1) + " failed after " + done + " step(s): " + ex.Message);
				}
				done++;
			}
			string waiting = null;
			if (opened)
				McpCatalog.OnUiThread(() => waiting = WindowWaiting());
			return done + " step(s) done." + (waiting == null ? "" : " " + waiting);
		}

		static void RequireConfigure()
		{
			if (McpCatalog.Level() < AiAccess.Configure)
				throw new InvalidOperationException(McpCatalog.Refusal(AiAccess.Configure));
		}

		[McpTool(AiAccess.Read, "The help page the program shows, as Markdown.")]
		public static string Help()
		{
			// The page links to the program's other documents, which the caller does not have.
			return ResolveLinks(HelpText());
		}

		[McpTool(AiAccess.Read, "Every element of the running program's interface with its purpose, as Markdown.")]
		public static string UiTree()
		{
			return UiTreeMarkdown.Write(UiTreeExporter.Read(MainWindow(), TrayMenu()));
		}

		/// <summary>The element a path names, refused when it is the door's own or administers below Administer.</summary>
		public static object Resolve(string path)
		{
			var element = string.IsNullOrEmpty(path) ? null : FindElement(path);
			if (element == null)
				throw new InvalidOperationException("No element at " + path + ".");
			var name = UiTreeWalker.IdOf(element);
			if (DoorControls.Contains(name))
				throw new InvalidOperationException("This control is the person's alone, " + McpCatalog.SettingsPlace + "; it cannot be changed or pressed through this door.");
			if (McpCatalog.Level() < AiAccess.Administer && AdminControls.Contains(name))
				throw new InvalidOperationException(McpCatalog.Refusal(AiAccess.Administer));
			return element;
		}

		#region WPF windows and elements

		// Written in McpUiTools.WPF.cs. Each one answers only for a WPF window or element and leaves the
		// answer it is given otherwise; without that file the calls are not compiled at all.

		static partial void CurrentOfWpf(object root, ref object current);

		static partial void TabsOnTheWayWpf(object element, ref List<object> pages);

		static partial void SelectTabWpf(object page);

		static partial void TabTextWpf(object page, ref string text);

		static partial void PointAtWpf(object element, string text, int seconds, ref bool pointed);

		#endregion
	}
}
