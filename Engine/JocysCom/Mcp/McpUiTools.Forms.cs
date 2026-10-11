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
			return Application.OpenForms.Cast<Form>()
				.Where(x => x != root && x.Visible && !(x is OverlayForm) && !string.IsNullOrEmpty(x.Name))
				.Cast<object>()
				.ToList();
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
