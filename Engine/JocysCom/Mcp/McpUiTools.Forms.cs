#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>The Windows Forms side of the interface tools: the program's forms, the page and focus on screen, and pointing at a control.</summary>
	public static partial class McpUiTools
	{
		static IEnumerable<object> OtherForms(object root)
		{
			var windows = Application.OpenForms.Cast<Form>()
				.Where(x => x != root && x.Visible && !(x is OverlayForm) && !string.IsNullOrEmpty(x.Name))
				.Cast<object>()
				.ToList();
			windows.AddRange(OpenMenus());
			return windows;
		}

		/// <summary>The path a menu open on its own goes by when it has no name of its own.</summary>
		const string MenuName = "Menu";

		delegate bool EnumThreadWindowsProc(IntPtr window, IntPtr data);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		static extern bool EnumThreadWindows(uint threadId, EnumThreadWindowsProc callback, IntPtr data);

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern uint GetCurrentThreadId();

		/// <summary>
		/// The menus open on their own, such as one a button shows under itself: each is a window of the program while it is
		/// open, named by its name or <see cref="MenuName"/>, and its entries are read and pressed by path. A menu that drops
		/// out of an entry is reached through that entry instead. Called on the interface thread, whose windows they are.
		/// </summary>
		static List<object> OpenMenus()
		{
			var menus = new List<object>();
			EnumThreadWindowsProc collect = (window, data) =>
			{
				var menu = Control.FromHandle(window) as ToolStripDropDown;
				if (menu != null && menu.Visible && menu.OwnerItem == null)
					menus.Add(menu);
				return true;
			};
			EnumThreadWindows(GetCurrentThreadId(), collect, IntPtr.Zero);
			GC.KeepAlive(collect);
			return menus;
		}

		/// <summary>What a press tells of a menu it left open: its path and the entries that can be pressed; null when none is open.</summary>
		static string OpenMenuNote()
		{
			var menu = OpenMenus().Cast<ToolStripDropDown>().LastOrDefault();
			if (menu == null)
				return null;
			var name = WindowName(menu);
			var entries = menu.Items.Cast<ToolStripItem>()
				.Where(x => x.Available && x.Enabled && !(x is ToolStripSeparator) && !string.IsNullOrEmpty(x.Name))
				.Select(x => name + "/" + x.Name + " (" + UiTreeWalker.ReadAlone(x, null).Name + ")")
				.ToArray();
			return "The menu " + name + " is open." + (entries.Length == 0 ? "" : " Its entries, pressed with ui_invoke: " + string.Join(", ", entries) + ".");
		}

		/// <summary>What ui_current reports for a Windows Forms window.</summary>
		static object CurrentOf(Control main)
		{
			var active = Form.ActiveForm;
			var others = OtherWindows();
			Control window = active != null && others.Contains(active) ? active : main;
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
				{ "Path", WindowName(x) }, { "Name", WindowTitle(x) }, { "Modal", IsModal(x) }, { "Active", x == window },
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

		/// <summary>The tab pages that hold a control, outermost first.</summary>
		static List<object> TabsOnTheWay(Control control)
		{
			var pages = new List<object>();
			for (var c = control == null ? null : control.Parent; c != null; c = c.Parent)
				if (c is TabPage && c.Parent is TabControl)
					pages.Insert(0, c);
			return pages;
		}

		/// <summary>Frames a control on screen, or says it is hidden.</summary>
		static void PointAt(Control control, string text, int seconds)
		{
			var window = control == null ? null : control.FindForm();
			if (control == null || !control.Visible || window == null || !window.Visible || window.WindowState == FormWindowState.Minimized)
				throw new InvalidOperationException("The element is hidden, so there is nothing to point at.");
			UiCallout.Show(control, text, seconds);
		}
	}
}
