using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using x360ce.Engine.UiTree;

namespace x360ce.Engine.Mcp
{
	/// <summary>
	/// What an assistant or a script can ask of the program through its interface, in every version
	/// of it. Each public static method marked McpTool is one tool; its name, arguments and
	/// description are read from the method, so a tool is defined here and nowhere else. The
	/// catalogue runs every body on the interface thread; a tool refuses by throwing, and the
	/// message is what the caller reads. Each program adds tools of its own in a class it lists in
	/// <see cref="McpCatalog.Sources"/>, and fills in the hooks below.
	/// </summary>
	public static class McpUiTools
	{
		/// <summary>Window the path tools start from. The main window unless a test says otherwise.</summary>
		public static Control Root;

		/// <summary>The program's main window.</summary>
		public static Func<Form> MainWindow = () => null;

		/// <summary>The menu behind the tray icon, or null where there is none.</summary>
		public static Func<ContextMenuStrip> TrayMenu = () => null;

		/// <summary>Brings the main window back from the tray, the way the program itself does.</summary>
		public static Action<Form> Restore = window =>
		{
			window.Show();
			window.WindowState = FormWindowState.Normal;
		};

		/// <summary>The help page the program shows, as Markdown.</summary>
		public static Func<string> HelpText = () => "";

		/// <summary>
		/// Field names of the elements that install or remove drivers, turn on debug mode or
		/// otherwise administer the computer. Below Administer these may not be touched.
		/// </summary>
		public static string[] AdminControls = new string[0];

		/// <summary>
		/// Field names of the door's own controls on the Options tab. No caller may touch them at
		/// any level: the level is a person's choice, the port is where the door is, and the token
		/// is what lets the caller in.
		/// </summary>
		public static string[] DoorControls = new string[0];

		static Control RootWindow { get { return Root ?? MainWindow(); } }

		/// <summary>
		/// The program's other windows that are open and showing: a dialog waiting for an answer, a
		/// list of warnings. The balloon that points at things is not one of them.
		/// </summary>
		static IEnumerable<Form> OtherWindows()
		{
			return Application.OpenForms.Cast<Form>()
				.Where(x => x != RootWindow && x.Visible && !(x is OverlayForm) && !string.IsNullOrEmpty(x.Name))
				.ToList();
		}

		/// <summary>
		/// The element a path names, or null. A path starts in the main window; one that starts with
		/// the name of another open window starts in that window instead, so a dialog can be read
		/// and answered like the window it came from.
		/// </summary>
		public static object FindElement(string path)
		{
			if (string.IsNullOrEmpty(path))
				return RootWindow;
			var element = UiTreeWalker.Find(RootWindow, path);
			if (element != null)
				return element;
			var slash = path.IndexOf('/');
			var first = slash < 0 ? path : path.Substring(0, slash);
			var window = OtherWindows().FirstOrDefault(x => x.Name == first);
			if (window == null)
				return null;
			return slash < 0 ? window : UiTreeWalker.Find(window, path.Substring(slash + 1));
		}

		/// <summary>
		/// The window the program waits on, told to the caller left waiting: the dialog in front and the buttons that
		/// answer it. Null when nothing waits. While a dialog waits, every other shown window of the program is
		/// disabled, so the dialog still enabled is the one in front, and a shown main window that is disabled with no
		/// such dialog waits on a system window.
		/// </summary>
		public static string WindowWaiting()
		{
			var front = OtherWindows().LastOrDefault(x => x.Modal && x.IsHandleCreated && JocysCom.ClassLibrary.Win32.NativeMethods.IsWindowEnabled(x.Handle));
			if (front == null)
			{
				var main = RootWindow;
				if (main == null || !main.Visible || !main.IsHandleCreated || JocysCom.ClassLibrary.Win32.NativeMethods.IsWindowEnabled(main.Handle))
					return null;
				return "A window the door cannot read waits for an answer, such as a file chooser or a system message box. What was asked carries on once the person answers it.";
			}
			var buttons = new List<UiNode>();
			CollectButtons(UiTreeWalker.Read(front, false, front.Name), buttons);
			var said = "The window " + front.Name + " (\"" + front.Text + "\") waits for an answer, and what was asked carries on once it has one.";
			if (buttons.Count > 0)
				said += " Its buttons, pressed with ui_invoke: " + string.Join(", ", buttons.Select(x => x.Path + " (" + x.Name + ")")) + ".";
			return said + " ui_read " + front.Name + " reads what it asks.";
		}

		/// <summary>The buttons a window shows, leaving out those in a grid's rows, which are its data rather than its answers.</summary>
		static void CollectButtons(UiNode node, List<UiNode> buttons)
		{
			if (node.Hidden)
				return;
			if (node.Role == "Button" && node.Path != null)
				buttons.Add(node);
			if (node.Items == null || node.Role == "Row")
				return;
			foreach (var child in node.Items)
				CollectButtons(child, buttons);
		}

		[McpTool(AiAccess.Read, "The program's windows that are open besides the main one, such as a dialog waiting for an answer, as JSON: Path, Name, Modal. Read one with ui_read and its Path; the other tools take paths that start with it.")]
		public static object UiWindows()
		{
			return OtherWindows().Select(x => (object)new Dictionary<string, object>
			{
				{ "Path", x.Name }, { "Name", x.Text }, { "Modal", x.Modal },
			}).ToArray();
		}

		[McpTool(AiAccess.Read, "What the person is looking at now, as JSON: Windows (every open window of the program: Path, Name, Modal, Active), Window (the path of the window in front, empty for the main one), Tabs (the pages shown in it, from the top down), Focus (the element with keyboard focus: Path, Role, Name, Value) and Row (the current row, when Focus is a grid). The paths work with the other tools.")]
		public static object UiCurrent()
		{
			var main = RootWindow;
			var active = Form.ActiveForm;
			var others = OtherWindows();
			var window = active != null && others.Contains(active) ? active : main;
			if (window == null)
				throw new InvalidOperationException("No window is open.");
			var prefix = window == main ? "" : window.Name;
			// Every window, because a dialog waiting behind another one is still what the program is waiting on.
			var windows = new List<object>
			{
				new Dictionary<string, object> { { "Path", "" }, { "Name", main == null ? null : main.Text }, { "Modal", false }, { "Active", window == main } },
			};
			windows.AddRange(others.Select(x => (object)new Dictionary<string, object>
			{
				{ "Path", x.Name }, { "Name", x.Text }, { "Modal", x.Modal }, { "Active", x == window },
			}));
			var answer = new Dictionary<string, object> { { "Windows", windows.ToArray() }, { "Window", prefix } };
			var pages = new List<string>();
			for (var tabs = FirstTabs(window); tabs != null && tabs.SelectedTab != null; tabs = FirstTabs(tabs.SelectedTab))
				pages.Add(PathOf(tabs.SelectedTab, window, prefix));
			answer["Tabs"] = pages.Where(x => x != null).ToArray();
			// Each container remembers which of its children is active, down to the one with focus.
			Control focus = window;
			ContainerControl container;
			while ((container = focus as ContainerControl) != null && container.ActiveControl != null)
				focus = container.ActiveControl;
			if (focus == window)
				return answer;
			var path = PathOf(focus, window, prefix);
			answer["Focus"] = Current(focus, path);
			var grid = focus as DataGridView;
			if (grid != null && grid.CurrentRow != null && path != null)
				answer["Row"] = Current(grid.CurrentRow, path + "/" + UiTreeWalker.RowsSegment + "/" + grid.CurrentRow.Index);
			return answer;
		}

		/// <summary>An element as ui_current reports it.</summary>
		static Dictionary<string, object> Current(object element, string path)
		{
			var node = UiTreeWalker.Read(element, false, path);
			return new Dictionary<string, object>
			{
				{ "Path", path }, { "Role", node?.Role }, { "Name", node?.Name }, { "Value", node?.Value },
			};
		}

		/// <summary>The path of a control from the window it is in, or null when something on the way has no name.</summary>
		static string PathOf(Control control, Control window, string prefix)
		{
			var names = new List<string>();
			for (var c = control; c != null && c != window; c = c.Parent)
			{
				if (string.IsNullOrEmpty(c.Name))
					return null;
				names.Insert(0, c.Name);
			}
			if (!string.IsNullOrEmpty(prefix))
				names.Insert(0, prefix);
			return string.Join("/", names);
		}

		/// <summary>The nearest tab control inside, looking only in the page each tab control shows.</summary>
		static TabControl FirstTabs(Control top)
		{
			var queue = new Queue<Control>();
			queue.Enqueue(top);
			while (queue.Count > 0)
			{
				var control = queue.Dequeue();
				var tabs = control as TabControl;
				foreach (Control child in control.Controls)
				{
					if (tabs != null && child != tabs.SelectedTab)
						continue;
					var found = child as TabControl;
					if (found != null)
						return found;
					queue.Enqueue(child);
				}
			}
			return null;
		}

		[McpTool(AiAccess.Read, "The interface as a tree: every element with its kind, name, purpose, path and current value, as JSON. Pass a path to read one branch; a grid read this way lists its rows and the buttons in them. Sibling controls that read alike are listed once; setting the one shown sets both. A path may start with a window from ui_windows.")]
		public static string UiRead([Description("Element path from an earlier ui_read; omit for the whole window.")] string path = null)
		{
			path = path ?? "";
			var start = FindElement(path);
			if (start == null)
				throw new InvalidOperationException("No element at " + path + ".");
			var node = UiTreeWalker.Read(start, false, path);
			// The contract serialiser writes '/' as '\/', which is valid JSON and useless as a path.
			return JocysCom.ClassLibrary.Runtime.Serializer.SerializeToJson(node).Replace("\\/", "/");
		}

		[McpTool(AiAccess.Configure, "Sets an element by path: true/false for a CheckBox or Choice, a number for a Slider or Number, an item's shown text for a List, a tab name for Tabs, a row index for a Grid, text for Text.")]
		public static string UiSet([Description("Element path from ui_read.")] string path, [Description("The value, as text.")] string value)
		{
			var refused = UiTreeWalker.SetValue(Resolve(path), value);
			if (refused != null)
				throw new InvalidOperationException(refused);
			return null;
		}

		[McpTool(AiAccess.Configure, "Presses a Button by path, whether it stands on its own, on a bar, or in a grid row; the tabs above it are selected first. A press that opens a window answers as soon as the window waits, with its path and the buttons that answer it; press one with ui_invoke, and the press that opened it carries on. Buttons that install drivers need Administer access.")]
		public static string UiInvoke([Description("Element path from ui_read.")] string path)
		{
			var refused = UiTreeWalker.Invoke(Resolve(path));
			if (refused != null)
				throw new InvalidOperationException(refused);
			return null;
		}

		/// <summary>How long a walk points at each tab on the way, in milliseconds.</summary>
		public static int WalkStepMs = 1500;

		/// <summary>The tab pages that hold a control, outermost first: the tabs a person opens, in order, to reach it.</summary>
		public static List<TabPage> TabsOnTheWay(Control control)
		{
			var pages = new List<TabPage>();
			for (var c = control == null ? null : control.Parent; c != null; c = c.Parent)
				if (c is TabPage && c.Parent is TabControl)
					pages.Insert(0, (TabPage)c);
			return pages;
		}

		[McpTool(AiAccess.Read, "Points at an element for the person: brings its page to the front, restores the window from the tray if need be, frames the element and shows a balloon with your words beside it. A tab page is framed by its tab. With walk, first opens and points at each tab on the way, outermost first, a moment each, so the person learns the way there. Waits the seconds before answering, so several calls in a row make a paced walkthrough.", OnUiThread = false)]
		public static string UiShow([Description("Element path from ui_read.")] string path, [Description("What to say beside it, in the person's language.")] string text = null, [Description("How long to point, 1 to 60 seconds.")] int seconds = 5,
			[Description("True opens and points at each tab on the way first, so the person learns the way there.")] bool walk = false)
		{
			seconds = Math.Max(1, Math.Min(60, seconds));
			object element = null;
			Control control = null;
			var way = new List<TabPage>();
			McpCatalog.OnUiThread(() =>
			{
				element = FindElement(path);
				if (element == null)
					throw new InvalidOperationException("No element at " + path + ".");
				// A frame is drawn around a control, so an entry on a bar is pointed at by its bar and
				// a row by its grid, which is where the person has to look anyway.
				control = UiTreeWalker.ControlOf(element);
				// "Show me" means the window too: a person asking cannot see a tray icon's insides.
				var main = MainWindow();
				if (main != null && control != null && control.FindForm() == main && (main.WindowState == FormWindowState.Minimized || !main.Visible))
					Restore(main);
				if (walk)
					way = TabsOnTheWay(control);
			});
			// A tab at a time, as the person would click them, each balloon naming the click.
			foreach (var page in way)
			{
				McpCatalog.OnUiThread(() =>
				{
					((TabControl)page.Parent).SelectedTab = page;
					UiCallout.Show(page, "Open the " + page.Text.Trim() + " tab", Math.Max(1, (WalkStepMs + 999) / 1000));
				});
				Thread.Sleep(WalkStepMs);
			}
			McpCatalog.OnUiThread(() =>
			{
				UiTreeWalker.Reveal(element);
				var window = control == null ? null : control.FindForm();
				if (control == null || !control.Visible || window == null || !window.Visible || window.WindowState == FormWindowState.Minimized)
					throw new InvalidOperationException("The element is hidden, so there is nothing to point at.");
				UiCallout.Show(control, text, seconds);
			});
			// The pause is the point: the person reads the balloon before the next step arrives.
			Thread.Sleep(seconds * 1000);
			return null;
		}

		/// <summary>The most elements one search returns: a common word matches hundreds across the four controllers' pages.</summary>
		public const int FindLimit = 40;

		[McpTool(AiAccess.Read, "Finds elements with every word somewhere in their name, purpose, field name or path, in any order and case, as JSON: Path, Role, Name, Description, Value. Best matches first, a word in the name counting most; at most 40, and when there are more a last element with Role Note says how many. Each controller's page is PadNTabPage, so Pad2 among the words keeps to controller 2. Cheaper than reading the whole tree; use the Path with the other tools.")]
		public static object UiFind([Description("Words to look for, any order and case.")] string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new InvalidOperationException("Give a word to look for.");
			var words = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			var found = new List<KeyValuePair<int, object>>();
			Collect(UiTreeWalker.Read(RootWindow, false, ""), words, found);
			foreach (var window in OtherWindows())
				Collect(UiTreeWalker.Read(window, false, window.Name), words, found);
			// The sort keeps the tree's order among equal matches.
			var shown = found.OrderByDescending(x => x.Key).Select(x => x.Value).Take(FindLimit).ToList();
			// Cut off without a word, a search reads as complete, and the setting that was left out is
			// reported as one the program does not have.
			if (found.Count > FindLimit)
				shown.Add(new Dictionary<string, object>
				{
					{ "Path", null }, { "Role", "Note" },
					{ "Name", FindLimit + " of " + found.Count + " shown, best matches first. Add a word, such as Pad2 or a page's name, to narrow the search." },
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

		[McpTool(AiAccess.Read, "Runs a small script, one step per line, in order: 'show <path> | <words> | <seconds>' points at an element; 'walk <path> | <words> | <seconds>' does the same after opening and pointing at each tab on the way; 'click <path>' presses a button; 'set <path> | <value>' sets an element; 'wait <seconds>' pauses. Lines starting with # are ignored. Stops at the first step that fails and says which. A click that opens a window goes on to the next line, which may answer it, and a window still waiting at the end is named with its buttons. show and wait need Read access; click and set need Configure.", OnUiThread = false, Changes = true)]
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
							throw new InvalidOperationException("Unknown step '" + verb + "'. Steps are show, walk, click, set and wait.");
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
			return HelpText();
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
				throw new InvalidOperationException("This control is the person's alone, on the Options tab; it cannot be changed or pressed through this door.");
			if (McpCatalog.Level() < AiAccess.Administer && AdminControls.Contains(name))
				throw new InvalidOperationException(McpCatalog.Refusal(AiAccess.Administer));
			return element;
		}
	}
}
