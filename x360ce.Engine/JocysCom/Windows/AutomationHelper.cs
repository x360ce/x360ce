using System.Windows.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

		[DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		public static void FindNotificationIcon()
		{
			// Find notification area on Windows 11.
			var rootElement = AutomationElement.RootElement;
			var trayWnd = FindFirstChild(rootElement, ControlType.Pane, "Shell_TrayWnd");
			var trayNotifyWnd = FindFirstChild(trayWnd, ControlType.Pane, "TrayNotifyWnd");
			var sysPager = FindFirstChild(trayNotifyWnd, ControlType.Pane, "SysPager");
			var toolbarWindow = FindFirstChild(sysPager, ControlType.ToolBar, "ToolbarWindow32");
			// Find all buttorns.
			var buttonElements = FindAllChildren(toolbarWindow, ControlType.Button);
			Console.WriteLine("Number of buttons in the notification area: " + buttonElements.Count);
			foreach (AutomationElement buttonElement in buttonElements)
				Console.WriteLine("Button name: " + buttonElement.Current.Name);
		}

		public static List<AutomationElement> FindAllChildren(AutomationElement parent, ControlType controlType = null)
		{
			var conditions = new List<Condition>();
			if (controlType != null)
				conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
			var children = conditions.Count >= 2
				? parent.FindAll(TreeScope.Children, new AndCondition(conditions.ToArray()))
				: parent.FindAll(TreeScope.Children, conditions.FirstOrDefault() ?? Condition.TrueCondition);
			if (children.Count == 0)
				Console.WriteLine("Warn: Unable to find child element with conditions: " + conditions);
			return children.Cast<AutomationElement>().ToList();
		}

		public static AutomationElement FindFirstChild(AutomationElement parent, ControlType controlType = null, string className = null, object automationId = null)
		{
			var conditions = new List<Condition>();
			if (controlType != null)
				conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
			if (className != null)
				conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty, className));
			if (automationId != null)
				conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
			var child = conditions.Count >= 2
				? parent.FindFirst(TreeScope.Children, new AndCondition(conditions.ToArray()))
				: parent.FindFirst(TreeScope.Children, conditions.FirstOrDefault() ?? Condition.TrueCondition);
			if (child == null)
				Console.WriteLine("Error: Unable to find child element with conditions: " + conditions);
			return child;
		}

		/// <summary>
		/// Waits for element to be ready for processing.
		/// </summary>
		/// <param name="targetControl">The target control.</param>
		/// <returns>The WindowPattern.</returns>
		private static WindowPattern WaitForElement(AutomationElement targetControl)
		{
			WindowPattern windowPattern = null;
			try
			{
				windowPattern = targetControl.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
			}
			// If object doesn't support the WindowPattern control pattern
			catch (InvalidOperationException)
			{
				return null;
			}
			// If object not responding in a timely manner then return
			if (!windowPattern.WaitForInputIdle(10000))
				return null;
			// Element is usable.
			return windowPattern;
		}

		//public AutomationElement FindElementBySubstring(AutomationElement element, ControlType controlType, string searchTerm)
		//{
		//	AutomationElementCollection textDescendants = element.FindAll(
		//		TreeScope.Descendants,
		//		new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
		//	foreach (AutomationElement el in textDescendants)
		//	{
		//		if (el.Current.Name.Contains(searchTerm))
		//			return el;
		//	}
		//	return null;
		//}


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
			var rawChildren = FindAllChildren(control);
			foreach (var child in rawChildren)
			{
				var children = GetAll(child, path + $".{child.Current.ControlType.ProgrammaticName.Split('.').Last()}", true);
				var controlsToAdd = children.Where(x => !controls.ContainsKey(x.Key));
				foreach (var cta in controlsToAdd)
					controls.Add(cta.Key, cta.Value);
			}
			return controls;
		}

	}
}
