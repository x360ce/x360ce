using System.Windows.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace JocysCom.ClassLibrary.Windows
{
	public static class AutomationHelper
	{
		public static List<AutomationElement> FindByProcessId(int id)
		{
			var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, id);
			var list = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Children, condition)
				.Cast<AutomationElement>()
				.ToList();
			return list;
		}

		public static List<WindowPattern> FindWindowsByProcessId(int id)
		{
			var condition = new AndCondition(
			  new PropertyCondition(AutomationElement.ProcessIdProperty, id),
			  new PropertyCondition(AutomationElement.IsEnabledProperty, true),
			  new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
			);
			var list = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Children, condition)
				.Cast<AutomationElement>()
				.Select(x => x.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern)
				.ToList();
			return list;
		}

		public static List<AutomationElement> FindByAutomationId(int id)
		{
			var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, id);
			var list = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Descendants, condition)
				.Cast<AutomationElement>()
				.ToList();
			return list;
		}

		public static List<AutomationElement> FindToolBarByProcessId(int id)
		{
			var condition = new AndCondition(
				new PropertyCondition(AutomationElement.ProcessIdProperty, id),
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar)
			);
			var list = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Children, condition)
				.Cast<AutomationElement>()
				.ToList();
			return list;
		}

		delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

		/// <summary>
		/// Get window handles by process.
		/// </summary>
		public static IEnumerable<IntPtr> EnumerateProcessWindowHandles(Process p)
		{
			var handles = new List<IntPtr>();
			foreach (ProcessThread thread in p.Threads)
				EnumThreadWindows(thread.Id,
					(hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
			return handles;
		}

		/// <summary>
		///  Find process window by regular expression.
		/// </summary>
		public static AutomationElement FindWindow(Process p, Regex rx, int timeoutMilliseconds = 30000)
		{
			var watch = Stopwatch.StartNew();
			AutomationElement windowElement = null;
			do
			{
				var windows = EnumerateProcessWindowHandles(p);
				foreach (var window in windows)
				{
					var el = AutomationElement.FromHandle(window);
					if (rx.IsMatch(el.Current.Name))
					{
						windowElement = el;
						watch.Stop();
						return el;
					}
				}
				Task.Delay(1000).Wait();
			} while (watch.ElapsedMilliseconds < timeoutMilliseconds);
			throw new Exception("Window not found");
		}

		public static void FindNotificationIcon()
		{
			/*
			var arrText = new List<string>();
			string tskBarClassName = "Shell_TrayWnd";
			IntPtr tskBarHwnd = FindWindow(tskBarClassName, default);
			AutomationElement window = AutomationElement.FromHandle(tskBarHwnd);
			var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar);
			AutomationElementCollection elementCollection = window.FindAll(TreeScope.Descendants, condition);
			//(ToolbarWindow32, MSTaskListWClass),
			// for fun get all we can...
			foreach (AutomationElement aE in elementCollection)
			{
				if (aE.Current.Name.Equals("User Promoted Notification Area"))
				{
					foreach (AutomationElement ui in aE.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)))
						arrText.Add("Notification Area - " + ui.Current.HelpText.Replace('\n', ' ')); // removed line break as when shown it would show some on a new line in messagebox
				}
				else if (aE.Current.Name.Equals("Running applications"))
				{
					foreach (AutomationElement ui in aE.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)))
						arrText.Add("Toolbar Area - " + ui.Current.Name.Replace('\n', ' ')); // removed line break as when shown it would show some on a new line in messagebox
				}
			}
			if (arrText.Count > 0)
				MessageBox.Show(string.Join(Environment.NewLine, arrText.ToArray()));
			*/
		}



		//public static IEnumerable<AutomationElement> EnumNotificationIcons()
		//{
		//	var userArea = AutomationElement.RootElement.FindFirst("Notification Area");
		//	if (userArea != null)
		//	{
		//		// If there is a chevron, click it. There may not be a chevron if no
		//		// icons are hidden.
		//		var chevron = userArea.GetTopLevelElement().Find("NotificationChevron");
		//		if (chevron != null)
		//		{
		//			chevron.InvokeButton();
		//		}
		//		foreach (var button in userArea.EnumChildButtons())
		//		{
		//			yield return button;
		//		}
		//	}
		//}

		/*

		static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
		{
			var handles = new List<IntPtr>();
			foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
				EnumThreadWindows(thread.Id,
					(hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
			return handles;
		}

		public AutomationElement FindElementBySubstring(AutomationElement element, ControlType controlType, string searchTerm)
		{
			AutomationElementCollection textDescendants = element.FindAll(
				TreeScope.Descendants,
				new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));

			foreach (AutomationElement el in textDescendants)
			{
				if (el.Current.Name.Contains(searchTerm))
					return el;
			}
			return null;
		}

		/// <summary>
		/// Returns the first automation element that is a child of the element you passed in and contains the string you passed in.
		/// </summary>
		public AutomationElement GetElementByName(AutomationElement aeElement, string sSearchTerm)
		{
			AutomationElement aeFirstChild = TreeWalker.RawViewWalker.GetFirstChild(aeElement);
			AutomationElement aeSibling = null;
			while ((aeSibling = TreeWalker.RawViewWalker.GetNextSibling(aeFirstChild)) != null)
			{
				if (aeSibling.Current.Name.Contains(sSearchTerm))
				{
					return aeSibling;
				}
			}
			return aeSibling;
		}

		*/

		// <summary>
		/// Finds all enabled buttons in the specified window element.
		/// </summary>
		/// <param name="elementWindowElement">An application or dialog window.</param>
		/// <returns>A collection of elements that meet the conditions.</returns>
		public static AutomationElementCollection FindByMultipleConditions(AutomationElement elementWindowElement)
		{
			if (elementWindowElement == null)
				throw new ArgumentException();
			var conditions = new AndCondition(
			  new PropertyCondition(AutomationElement.IsEnabledProperty, true),
			  new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)
			  );
			// Find all children that match the specified conditions.
			AutomationElementCollection elementCollection =
				elementWindowElement.FindAll(TreeScope.Children, conditions);
			return elementCollection;
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static Dictionary<AutomationElement, string> GetAll(AutomationElement control, string path, bool includeTop = false)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			// Create new list.
			var controls = new Dictionary<AutomationElement, string>();
			// Add top control if required.
			if (includeTop && !controls.Keys.Contains(control))
				controls.Add(control, path);
			//var rawChildren = FindInRawView(control);
			var rawChildren = GetChildren(control);
			foreach (var child in rawChildren)
			{
				var children = GetAll(child, path + $".{child.Current.ControlType.ProgrammaticName}", true);
				var controlsToAdd = children.Where(x => !controls.ContainsKey(x.Key));
				foreach (var cta in controlsToAdd)
					controls.Add(cta.Key, cta.Value);
			}
			return controls;
		}

		public static List<AutomationElement> GetChildren(AutomationElement element)
		{
			//var condition = new AndCondition(
			//  new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane)
			//);
			var list = element
				.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition)
				.Cast<AutomationElement>()
				.ToList();
			return list;
		}

		//public static IEnumerable<AutomationElement> FindInRawView(AutomationElement root)
		//{
		//	var rawViewWalker = TreeWalker.RawViewWalker;
		//	var queue = new Queue<AutomationElement>();
		//	queue.Enqueue(root);
		//	while (queue.Count > 0)
		//	{
		//		var element = queue.Dequeue();
		//		yield return element;
		//		var sibling = rawViewWalker.GetNextSibling(element);
		//		if (sibling != null)
		//			queue.Enqueue(sibling);
		//		var child = rawViewWalker.GetFirstChild(element);
		//		if (child != null)
		//			queue.Enqueue(child);
		//	}
		//}


	}
}
