using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace x360ce.Tests
{
	public static class AutomationHelper
	{

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


	}
}
