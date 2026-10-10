#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>The program's windows of either kind: finding them, telling them apart, bringing one back, and the dialog the program waits on.</summary>
	public static partial class McpUiTools
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool IsWindowEnabled(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool IsWindowVisible(IntPtr hWnd);

		#region Windows

		/// <summary>
		/// The program's other windows that are open and showing: a dialog waiting for an answer, a
		/// list of warnings. The balloon that points at things is not one of them.
		/// </summary>
		static List<object> OtherWindows()
		{
			var root = RootWindow;
			var windows = new List<object>();
			windows.AddRange(OtherForms(root));
			AddOtherWpfWindows(root, windows);
			return windows;
		}

		static string WindowName(object window)
		{
			var control = window as Control;
			if (control != null)
				return control.Name;
			string name = null;
			WindowNameWpf(window, ref name);
			return name;
		}

		static string WindowTitle(object window)
		{
			var control = window as Control;
			if (control != null)
				return control.Text;
			string title = null;
			WindowTitleWpf(window, ref title);
			return title;
		}

		static bool IsModal(object window)
		{
			var form = window as Form;
			if (form != null)
				return form.Modal;
			var modal = false;
			IsModalWpf(window, ref modal);
			return modal;
		}

		/// <summary>True for a dialog given its answer: it is closing, and nothing waits on it.</summary>
		static bool IsAnswered(object window)
		{
			var form = window as Form;
			if (form != null)
				return form.DialogResult != DialogResult.None;
			var answered = false;
			IsAnsweredWpf(window, ref answered);
			return answered;
		}

		/// <summary>True for a window on screen and not minimized.</summary>
		static bool IsShowing(object window)
		{
			var form = window as Form;
			if (form != null)
				return form.Visible && form.WindowState != FormWindowState.Minimized;
			var control = window as Control;
			if (control != null)
				return control.Visible;
			var showing = false;
			IsShowingWpf(window, ref showing);
			return showing;
		}

		/// <summary>The window's handle, or zero when it has none yet.</summary>
		static IntPtr HandleOf(object window)
		{
			var control = window as Control;
			if (control != null)
				return control.IsHandleCreated ? control.Handle : IntPtr.Zero;
			var handle = IntPtr.Zero;
			HandleOfWpf(window, ref handle);
			return handle;
		}

		/// <summary>The window an element is in: the form a control is on, or the WPF window an element is in.</summary>
		static object WindowOf(object element)
		{
			var control = UiTreeWalker.ControlOf(element);
			if (control != null)
				return control.FindForm();
			object window = null;
			WindowOfWpf(element, ref window);
			return window;
		}

		/// <summary>Brings a window back from the tray and from the taskbar.</summary>
		static void RestoreWindow(object window)
		{
			var form = window as Form;
			if (form != null)
			{
				form.Show();
				form.WindowState = FormWindowState.Normal;
				return;
			}
			RestoreWindowWpf(window);
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
			var window = OtherWindows().FirstOrDefault(x => WindowName(x) == first);
			if (window == null)
				return null;
			return slash < 0 ? window : UiTreeWalker.Find(window, path.Substring(slash + 1));
		}

		/// <summary>
		/// The window the program waits on, told to the caller left waiting: the dialog in front and the buttons that
		/// answer it. Null when nothing waits. While a dialog waits, every other shown window of the program is
		/// disabled, so the dialog still enabled is the one in front, and a shown main window that is disabled with no
		/// such dialog waits on a system window. A main window minimised to the taskbar is shown too: a file chooser
		/// opened from it waits all the same. A dialog given its answer is closing, and nothing waits on it: it
		/// stays shown until the press that answered it returns, and a call queued behind that press can arrive first.
		/// </summary>
		public static string WindowWaiting()
		{
			var dialogs = OtherWindows().Where(x => IsModal(x) && HandleOf(x) != IntPtr.Zero).ToList();
			if (dialogs.Any(IsAnswered))
				return null;
			var front = dialogs.LastOrDefault(x => IsWindowEnabled(HandleOf(x)));
			if (front == null)
			{
				var main = RootWindow;
				var handle = main == null ? IntPtr.Zero : HandleOf(main);
				if (handle == IntPtr.Zero || !IsWindowVisible(handle) || IsWindowEnabled(handle))
					return null;
				return "A window the door cannot read waits for an answer, such as a file chooser or a system message box. What was asked carries on once the person answers it.";
			}
			var name = WindowName(front);
			var buttons = new List<UiNode>();
			CollectButtons(UiTreeWalker.Read(front, false, name), buttons);
			var said = "The window " + name + " (\"" + WindowTitle(front) + "\") waits for an answer, and what was asked carries on once it has one.";
			if (buttons.Count > 0)
				said += " Its buttons, pressed with ui_invoke: " + string.Join(", ", buttons.Select(x => x.Path + " (" + x.Name + ")")) + ".";
			return said + " ui_read " + name + " reads what it asks.";
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

		#endregion

		#region WPF windows

		// Written in McpUiTools.WPF.cs. Each one answers only for a WPF window or element and leaves the
		// answer it is given otherwise; without that file the calls are not compiled at all.

		static partial void AddOtherWpfWindows(object root, List<object> windows);

		static partial void WindowNameWpf(object window, ref string name);

		static partial void WindowTitleWpf(object window, ref string title);

		static partial void IsModalWpf(object window, ref bool modal);

		static partial void IsAnsweredWpf(object window, ref bool answered);

		static partial void IsShowingWpf(object window, ref bool showing);

		static partial void HandleOfWpf(object window, ref IntPtr handle);

		static partial void WindowOfWpf(object element, ref object window);

		static partial void RestoreWindowWpf(object window);

		#endregion
	}
}
