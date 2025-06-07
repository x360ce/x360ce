using JocysCom.ClassLibrary.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using AEI = System.Windows.Automation.AutomationElement.AutomationElementInformation;

namespace JocysCom.ClassLibrary.Windows
{
	public class AutomationHelper
	{
		#region Path Helper Methods


		/// <summary>
		/// Retrieves the XPath-like path of the given automation element.
		/// </summary>
		public static string GetPath(AutomationElement element, bool enableValidation = false)
		{
			if (element == null)
				return null;

			var pathSegments = new List<string>();
			var currentElement = element;
			var partialPath = "";
			while (currentElement != null)
			{
				var controlTypeName = currentElement.Current.ControlType.ProgrammaticName.Split('.').Last();
				var automationId = currentElement.Current.AutomationId;
				var name = currentElement.Current.Name;
				var className = currentElement.Current.ClassName;

				var segment = controlTypeName;
				var predicates = new List<string>();

				if (!string.IsNullOrEmpty(automationId))
					predicates.Add($"@{nameof(AEI.AutomationId)}='{EscapeXPathValue(automationId)}'");

				if (!string.IsNullOrEmpty(className))
					predicates.Add($"@{nameof(AEI.ClassName)}='{EscapeXPathValue(className)}'");

				if (ShouldIncludeNameInPath(currentElement))
					predicates.Add($"@{nameof(AEI.Name)}='{EscapeXPathValue(name)}'");

				if (predicates.Count == 0)
				{
					int index = GetChildIndex(currentElement) + 1; // Adjust indexing as necessary
					predicates.Add($"position()={index}");
				}

				if (predicates.Count > 0)
					segment += $"[{string.Join(" and ", predicates)}]";

				pathSegments.Insert(0, segment);
				partialPath = string.Join("/", pathSegments);
				var parentElement = TreeWalker.RawViewWalker.GetParent(currentElement);
				// If validation is enabled, validate the current segment
				if (enableValidation)
				{
					// Use GetElement to check if we can retrieve the current element with the partial path
					var retrievedElement = GetElement(segment, parentElement);
					if (retrievedElement == null || !Equals(currentElement, retrievedElement))
					{
						// Collect detailed information
						var properties = GetElementProperties(currentElement);
						var errorMessage = new StringBuilder();
						errorMessage.AppendLine($"Validation failed at segment '{segment}'.");
						errorMessage.AppendLine($"Unable to retrieve element using path '{partialPath}'.");
						errorMessage.AppendLine("Current Element Properties:");
						foreach (var prop in properties)
							errorMessage.AppendLine($"{prop.Key}: {prop.Value}");
						throw new Exception(errorMessage.ToString());
					}
				}
				currentElement = parentElement;
			}
			return "/" + partialPath;
		}


		/// <summary>
		/// Retrieves an automation element based on the provided XPath-like path.
		/// </summary>
		/// <param name="path">XPath-like path of the given automation element.</param>
		/// <param name="currentElement">Root element. Desktop Window if not specified.</param>
		public static AutomationElement GetElement(string path, AutomationElement currentElement = null)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			var pathSegments = SplitXPath(path);

			// If no starting element is provided, default to the desktop window
			if (currentElement == null)
			{
				IntPtr desktopHandle = NativeMethods.GetDesktopWindow();
				currentElement = AutomationElement.FromHandle(desktopHandle);
			}

			foreach (var segment in pathSegments)
			{
				string controlTypeName = segment.Item1;
				var predicates = segment.Item2;

				var controlType = GetControlTypeByName(controlTypeName);

				// Build the conditions
				var conditions = new List<Condition>
				{
					new PropertyCondition(AutomationElement.ControlTypeProperty, controlType)
				};

				if (predicates.TryGetValue(nameof(AEI.AutomationId), out string automationId))
					conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

				if (predicates.TryGetValue(nameof(AEI.ClassName), out string className))
					conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty, className));

				if (predicates.TryGetValue(nameof(AEI.Name), out string name))
					conditions.Add(new PropertyCondition(AutomationElement.NameProperty, name));

				var finalCondition = conditions.Count > 1 ? new AndCondition(conditions.ToArray()) : conditions.First();

				// Include the current element in the search by using TreeScope.Element | TreeScope.Children
				var candidates = currentElement.FindAll(TreeScope.Element | TreeScope.Children, finalCondition).Cast<AutomationElement>().ToList();

				// Handle position() predicate if present
				AutomationElement nextElement = null;
				if (predicates.TryGetValue("position()", out string positionStr) && int.TryParse(positionStr, out int position))
				{
					if (position > 0 && position <= candidates.Count)
						nextElement = candidates[position - 1]; // Convert to 0-based index
					else
						return null;
				}
				else
				{
					nextElement = candidates.Count > 0 ? candidates[0] : null;
				}
				if (nextElement == null)
					return null;
				currentElement = nextElement;
			}
			return currentElement;
		}

		/// <summary>
		/// Retrieves properties of the given automation element.
		/// </summary>
		/// <param name="element">The AutomationElement to inspect.</param>
		/// <returns>Dictionary containing property names and values.</returns>
		public static Dictionary<string, object> GetElementProperties(AutomationElement element)
		{
			var properties = new Dictionary<string, object>();

			// Get the AutomationElementInformation structure
			var aeInfo = element.Current;
			// Use reflection to get all public properties of AutomationElement.AutomationElementInformation
			var aeInfoType = typeof(AutomationElement.AutomationElementInformation);
			var propInfos = aeInfoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var propInfo in propInfos)
			{
				string name = propInfo.Name;
				try
				{
					// Get the property name and value
					object value = propInfo.GetValue(aeInfo);
					// Special handling for certain property types
					if (value is ControlType controlType)
					{
						value = controlType.ProgrammaticName;
					}
					else if (value is System.Windows.Rect rect)
					{
						value = rect.ToString();
					}
					else if (value is System.Windows.Point point)
					{
						value = point.ToString();
					}
					else if (value is IntPtr ptr)
					{
						value = ptr.ToInt64(); // Convert pointer to Int64 for readability
					}
					else if (value != null && value.GetType().IsEnum)
					{
						value = value.ToString(); // Convert enums to their string representation
					}
					properties[name] = value;
				}
				catch (Exception ex)
				{
					// Handle any exceptions (e.g., property not supported)
					properties[name] = $"Error retrieving value: {ex.Message}";
				}
			}
			// Include Supported Patterns
			properties["SupportedPatterns"] = element.GetSupportedPatterns()
				.Select(p => p.ProgrammaticName)
				.ToArray();
			return properties;
		}

		public class ElementNode
		{
			public string Name { get; set; }
			public string AutomationId { get; set; }
			public string ControlType { get; set; }
			public string ClassName { get; set; }
			public List<ElementNode> Children { get; set; }
		}

		public ElementNode BuildElementTree(AutomationElement element)
		{
			var node = new ElementNode
			{
				Name = element.Current.Name,
				AutomationId = element.Current.AutomationId,
				ControlType = element.Current.ControlType.ProgrammaticName,
				ClassName = element.Current.ClassName,
				Children = new List<ElementNode>()
			};

			var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
			foreach (AutomationElement child in children)
			{
				var childNode = BuildElementTree(child);
				node.Children.Add(childNode);
			}

			return node;
		}

		/// <summary>
		///  Find process window by regular expression.
		/// </summary>
		public static AutomationElement WaitForWindow(Process p, Regex rx, int timeoutMilliseconds = 30000)
		{
			var watch = Stopwatch.StartNew();
			do
			{
				List<AutomationElement> windows = FindWindowsByProcessId(p.Id);
				foreach (var window in windows)
				{
					if (rx.IsMatch(window.Current.Name))
					{
						watch.Stop();
						return window;
					}
				}
				// Logical delay without blocking the current hardware thread.
				Task.Delay(1000).Wait();
			} while (watch.ElapsedMilliseconds < timeoutMilliseconds);
			throw new Exception("Window not found");
		}

		public AutomationElement WaitForElement(string elementPath, int timeoutInMilliseconds)
		{
			int elapsed = 0;
			const int waitInterval = 100; // milliseconds
			AutomationElement element = null;

			while (elapsed < timeoutInMilliseconds)
			{
				element = GetElement(elementPath);
				if (element != null)
					break;

				System.Threading.Thread.Sleep(waitInterval);
				elapsed += waitInterval;
			}

			return element;
		}

		#endregion

		#region Element Search Methods

		/// <summary>
		/// Finds elements matching specific conditions.
		/// </summary>
		/// <param name="conditions">Dictionary of property names and values to match.</param>
		/// <returns>List of matching AutomationElements.</returns>
		public List<AutomationElement> FindElementsByConditions(List<KeyValue> conditions)
		{
			if (conditions == null || conditions.Count == 0)
				throw new ArgumentException("Conditions dictionary cannot be null or empty.", nameof(conditions));
			var condList = new List<Condition>();
			foreach (var kvp in conditions)
			{
				var property = GetAutomationPropertyByName(kvp.Key);
				if (property == null)
					continue; // Skip unknown properties
				var cond = new PropertyCondition(property, kvp.Value);
				condList.Add(cond);
			}
			Condition finalCondition;
			if (condList.Count == 1)
			{
				finalCondition = condList.First();
			}
			else
			{
				finalCondition = new AndCondition(condList.ToArray());
			}
			var elements = AutomationElement.RootElement.FindAll(TreeScope.Descendants, finalCondition)
				.Cast<AutomationElement>()
				.ToList();
			return elements;
		}

		/// <summary>
		/// Gets the AutomationProperty corresponding to the given property name.
		/// </summary>
		private AutomationProperty GetAutomationPropertyByName(string propertyName)
		{
			// Append "Property" to the property name to match the static field names
			string fieldName = propertyName + "Property";
			var fieldInfo = typeof(AutomationElement).GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
			if (fieldInfo != null && fieldInfo.FieldType == typeof(AutomationProperty))
				return (AutomationProperty)fieldInfo.GetValue(null);
			return null;
		}

		/// <summary>
		/// Finds automation elements by process ID.
		/// </summary>
		public static List<AutomationElement> FindByProcessId(int id)
		{
			var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, id);
			var elements = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Children, condition)
				.Cast<AutomationElement>()
				.ToList();
			return elements;
		}

		/// <summary>
		/// Finds window elements by process ID.
		/// </summary>
		public static List<AutomationElement> FindWindowsByProcessId(int id)
		{
			var condition = new AndCondition(
				new PropertyCondition(AutomationElement.ProcessIdProperty, id),
				new PropertyCondition(AutomationElement.IsEnabledProperty, true),
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
			);
			var windows = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Children, condition)
				.Cast<AutomationElement>()
				.ToList();
			return windows;
		}



		/// <summary>
		/// Finds window patterns by process ID.
		/// </summary>
		public static List<WindowPattern> FindWindowPatternsByProcessId(int id)
		{
			var windows = FindWindowsByProcessId(id)
				.Select(x => x.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern)
				.ToList();
			return windows;
		}

		/// <summary>
		/// Finds automation elements by automation ID.
		/// </summary>
		public static List<AutomationElement> FindByAutomationId(string automationId)
		{
			var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
			var elements = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Descendants, condition)
				.Cast<AutomationElement>()
				.ToList();
			return elements;
		}

		/// <summary>
		/// Finds toolbars by process ID.
		/// </summary>
		public static List<AutomationElement> FindToolBarByProcessId(int id)
		{
			var condition = new AndCondition(
				new PropertyCondition(AutomationElement.ProcessIdProperty, id),
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar)
			);

			var toolbars = AutomationElement.RootElement
				.FindAll(TreeScope.Element | TreeScope.Children, condition)
				.Cast<AutomationElement>()
				.ToList();

			return toolbars;
		}

		/// <summary>
		/// Finds an element containing the specified substring.
		/// </summary>
		public AutomationElement FindElementBySubstring(AutomationElement element, ControlType controlType, string searchTerm)
		{
			var descendants = element.FindAll(
				TreeScope.Descendants,
				new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));

			foreach (AutomationElement el in descendants)
			{
				if (el.Current.Name.Contains(searchTerm))
					return el;
			}

			return null;
		}

		#endregion

		#region Toolbar and Notification Area Methods

		/// <summary>
		/// Finds the toolbar window in the notification area.
		/// </summary>
		public static AutomationElement FindToolbarWindow()
		{
			var rootElement = AutomationElement.RootElement;
			var trayWnd = FindFirstChild(rootElement, ControlType.Pane, "Shell_TrayWnd");
			var trayNotifyWnd = FindFirstChild(trayWnd, ControlType.Pane, "TrayNotifyWnd");
			var sysPager = FindFirstChild(trayNotifyWnd, ControlType.Pane, "SysPager");
			var toolbarWindow = FindFirstChild(sysPager, ControlType.ToolBar, "ToolbarWindow32");

			return toolbarWindow;
		}

		/// <summary>
		/// Finds all buttons in the toolbar window.
		/// </summary>
		public static List<AutomationElement> FindToolbarButtons()
		{
			var toolbarWindow = FindToolbarWindow();
			var buttons = FindAllChildren(toolbarWindow, ControlType.Button);
			Console.WriteLine("Number of buttons in the notification area: " + buttons.Count);
			return buttons;
		}

		#endregion

		#region Interaction Methods

		/// <summary>
		/// Simulates a click on the specified button element.
		/// </summary>
		public static void ClickButton(AutomationElement button, bool native = true)
		{
			if (button == null)
				throw new ArgumentNullException(nameof(button));

			if (native)
			{
				var rect = button.Current.BoundingRectangle;
				var x = (int)rect.Left + (int)(rect.Width / 2);
				var y = (int)rect.Top + (int)(rect.Height / 2);

				NativeMethods.SetCursorPos(x, y);
				NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
				NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, x, y, 0, 0);
			}
			else
			{
				if (button.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
				{
					((InvokePattern)pattern).Invoke();
				}
				else
				{
					throw new InvalidOperationException("The control does not support InvokePattern.");
				}
			}
		}

		/// <summary>
		/// Waits for a window to be ready for input.
		/// </summary>
		private static WindowPattern WaitForWindowToBeReady(AutomationElement targetControl)
		{
			if (targetControl == null)
				throw new ArgumentNullException(nameof(targetControl));

			try
			{
				var windowPattern = targetControl.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
				if (windowPattern == null)
					return null;

				if (!windowPattern.WaitForInputIdle(10000))
					return null;

				return windowPattern;
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		/// <summary>
		/// Waits for the specified window to close.
		/// </summary>
		public static void WaitForWindowToClose(WindowPattern windowPattern)
		{
			if (windowPattern == null)
				throw new ArgumentNullException(nameof(windowPattern));

			while (windowPattern.Current.WindowInteractionState != WindowInteractionState.Closing)
			{
				Task.Delay(100).Wait();
			}
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// Performs the specified action on the given automation element.
		/// </summary>
		/// <param name="element">The AutomationElement to interact with.</param>
		/// <param name="action">
		/// The action to perform. Note that some actions require specific parameters:
		/// - SetValue: parameters[0] is the string value to set.
		/// - WaitForInputIdle: parameters[0] (optional) is the wait time in milliseconds (string representing an integer).
		/// - ScrollVertical: parameters[0] is a string representing a `ScrollAmount` enum value (e.g., "LargeIncrement", "SmallIncrement").
		/// - ScrollHorizontal: parameters[0] is a string representing a `ScrollAmount` enum value (e.g., "LargeIncrement", "SmallIncrement").
		/// - SetScrollPercent: parameters[0] is the horizontal scroll percent (string representing a double), parameters[1] is the vertical scroll percent.
		/// - Move: parameters[0] is the x-coordinate (string representing a double), parameters[1] is the y-coordinate.
		/// - Resize: parameters[0] is the width (string representing a double), parameters[1] is the height.
		/// - SetRangeValue: parameters[0] is the value to set (string representing a double).
		/// Actions not listed above do not require parameters.
		/// </param>
		/// <param name="parameters">Optional parameters required for the action, as an array of strings.</param>
		/// <returns>True if the action was performed successfully.</returns>
		public bool PerformAction(AutomationElement element, AutomationAction action, string[] parameters = null)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			switch (action)
			{
				case AutomationAction.SetValue:
					if (parameters != null && parameters.Length == 1)
					{
						string text = parameters[0];
						SetValue(element, text);
						return true;
					}
					throw new ArgumentException("Parameters must be a string array with a single element for 'SetValue' action.");

				case AutomationAction.MouseLeftClick:
				case AutomationAction.MouseLeftDoubleClick:
				case AutomationAction.MouseRightClick:
				case AutomationAction.MouseRightDoubleClick:
					// Handle click actions (no parameters required)
					if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object invokePatternObj))
					{
						var invokePattern = (InvokePattern)invokePatternObj;
						invokePattern.Invoke();
						return true;
					}
					else
					{
						// Fallback to simulate a click if InvokePattern is not available
						var clickablePoint = element.GetClickablePoint();
						System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)clickablePoint.X, (int)clickablePoint.Y);
						var clicks = action.ToString().IndexOf("Double", StringComparison.OrdinalIgnoreCase) > -1 ? 2 : 1;
						bool isRightClick = action.ToString().IndexOf("Right", StringComparison.OrdinalIgnoreCase) > -1;
						var mouseDownFlag = isRightClick ? NativeMethods.MOUSEEVENTF_RIGHTDOWN : NativeMethods.MOUSEEVENTF_LEFTDOWN;
						var mouseUpFlag = isRightClick ? NativeMethods.MOUSEEVENTF_RIGHTUP : NativeMethods.MOUSEEVENTF_LEFTUP;
						for (int i = 0; i < clicks; i++)
						{
							NativeMethods.mouse_event((int)mouseDownFlag, 0, 0, 0, 0);
							NativeMethods.mouse_event((int)mouseUpFlag, 0, 0, 0, 0);
						}
						return true;
					}

				case AutomationAction.Select:
					var selectionItemPattern = GetPattern<SelectionItemPattern>(element);
					selectionItemPattern.Select();
					return true;

				case AutomationAction.SetFocus:
					element.SetFocus();
					return true;

				case AutomationAction.SetMinimized:
				case AutomationAction.SetNormal:
				case AutomationAction.SetMaximize:
				case AutomationAction.Close:
				case AutomationAction.WaitForInputIdle:
					var windowPattern = GetPattern<WindowPattern>(element);
					if (action == AutomationAction.SetMinimized)
						windowPattern.SetWindowVisualState(WindowVisualState.Minimized);
					else if (action == AutomationAction.SetNormal)
						windowPattern.SetWindowVisualState(WindowVisualState.Normal);
					else if (action == AutomationAction.SetMaximize)
						windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
					else if (action == AutomationAction.Close)
						windowPattern.Close();
					else if (action == AutomationAction.WaitForInputIdle)
					{
						int milliseconds = 0;
						if (parameters != null && parameters.Length > 0)
							int.TryParse(parameters[0], out milliseconds);
						windowPattern.WaitForInputIdle(milliseconds);
					}
					return true;

				case AutomationAction.Toggle:
					var togglePattern = GetPattern<TogglePattern>(element);
					togglePattern.Toggle();
					return true;

				case AutomationAction.Expand:
				case AutomationAction.Collapse:
					var expandCollapsePattern = GetPattern<ExpandCollapsePattern>(element);
					if (action == AutomationAction.Expand)
						expandCollapsePattern.Expand();
					else if (action == AutomationAction.Collapse)
						expandCollapsePattern.Collapse();
					return true;

				case AutomationAction.ScrollVertical:
				case AutomationAction.ScrollHorizontal:
				case AutomationAction.SetScrollPercent:
					var scrollPattern = GetPattern<ScrollPattern>(element);
					if (action == AutomationAction.ScrollVertical)
					{
						if (parameters == null || parameters.Length != 1)
							throw new ArgumentException("Parameters must be a single string representing ScrollAmount for 'ScrollVertical' action.");
						if (!Enum.TryParse(parameters[0], true, out ScrollAmount scrollAmountV))
							throw new ArgumentException("Invalid ScrollAmount value provided for 'ScrollVertical' action.");
						scrollPattern.Scroll(ScrollAmount.NoAmount, scrollAmountV);
						return true;
					}
					else if (action == AutomationAction.ScrollHorizontal)
					{
						if (parameters == null || parameters.Length != 1)
							throw new ArgumentException("Parameters must be a single string representing ScrollAmount for 'ScrollHorizontal' action.");
						if (!Enum.TryParse(parameters[0], true, out ScrollAmount scrollAmountH))
							throw new ArgumentException("Invalid ScrollAmount value provided for 'ScrollHorizontal' action.");
						scrollPattern.Scroll(scrollAmountH, ScrollAmount.NoAmount);
						return true;
					}
					else if (action == AutomationAction.ScrollHorizontal)
					{
						if (parameters == null || parameters.Length != 2)
							throw new ArgumentException("Parameters must be an array of two strings representing doubles for 'SetScrollPercent' action.");
						if (!double.TryParse(parameters[0], out double horizontalPercent) || !double.TryParse(parameters[1], out double verticalPercent))
							throw new ArgumentException("Parameters must be two valid doubles for 'SetScrollPercent' action.");
						scrollPattern.SetScrollPercent(horizontalPercent, verticalPercent);
						return true;
					}
					return true;

				case AutomationAction.Move:
				case AutomationAction.Resize:
					var transformPattern = GetPattern<TransformPattern>(element);
					if (parameters == null || parameters.Length != 2)
						throw new ArgumentException($"Parameters must be an array of two strings representing doubles for '{action}' action.");
					if (!double.TryParse(parameters[0], out double v1) || !double.TryParse(parameters[1], out double v2))
						throw new ArgumentException($"Parameters must be two valid doubles for '{action}' action.");
					if (action == AutomationAction.Resize)
						transformPattern.Resize(v1, v2);
					if (action == AutomationAction.Move)
						transformPattern.Resize(v1, v2);
					return true;

				case AutomationAction.SetRangeValue:
					var rangeValuePattern = GetPattern<RangeValuePattern>(element);
					if (parameters == null || parameters.Length != 1)
						throw new ArgumentException($"Parameters must be a single string representing a double for '{action}' action.");
					if (!double.TryParse(parameters[0], out double value))
						throw new ArgumentException($"Parameters must be a valid double for '{action}' action.");
					rangeValuePattern.SetValue(value);
					return true;

				default:
					throw new NotSupportedException($"Action '{action}' is not supported.");
			}
		}


		private TPattern GetPattern<TPattern>(AutomationElement element) where TPattern : BasePattern
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			// Get the static 'Pattern' field from the pattern type using reflection.
			var patternField = typeof(TPattern).GetField("Pattern", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			if (patternField == null)
				throw new NotSupportedException($"Pattern type '{typeof(TPattern).Name}' does not have a static 'Pattern' field.");
			var automationPattern = (AutomationPattern)patternField.GetValue(null);
			if (element.TryGetCurrentPattern(automationPattern, out object patternObj))
				throw new InvalidOperationException($"Element does not support the '{typeof(TPattern).Name}' pattern.");
			return (TPattern)patternObj;
		}

		/// <summary>
		/// Clicks on the specified automation element.
		/// </summary>
		/// <param name="element">The AutomationElement to click.</param>
		/// <returns>True if the click action was successful.</returns>
		private bool ClickElement(AutomationElement element)
		{
			try
			{
				// Try using the InvokePattern
				if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
				{
					((InvokePattern)pattern).Invoke();
				}
				else
				{
					// Fallback to simulate a mouse click
					var rect = element.Current.BoundingRectangle;
					if (rect.IsEmpty)
						return false;

					var x = rect.Left + rect.Width / 2;
					var y = rect.Top + rect.Height / 2;

					NativeMethods.SetCursorPos((int)x, (int)y);
					NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
					NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Retrieves all child elements of the specified automation element.
		/// </summary>
		/// <param name="parent">The parent AutomationElement.</param>
		/// <returns>A list of child AutomationElements.</returns>
		public List<AutomationElement> FindAllChildren(AutomationElement parent)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			var children = parent.FindAll(TreeScope.Children, Condition.TrueCondition)
				.Cast<AutomationElement>()
				.ToList();

			return children;
		}

		/// <summary>
		/// Finds all child elements matching the specified control type.
		/// </summary>
		public List<AutomationElement> FindAllChildren(string parentPath, ControlType controlType = null)
		{
			var parent = GetElement(parentPath);
			var children = FindAllChildren(parent, controlType);
			return children;
		}

		/// <summary>
		/// Finds all child elements matching the specified control type.
		/// </summary>
		public static List<AutomationElement> FindAllChildren(AutomationElement parent, ControlType controlType = null)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			Condition condition = controlType != null
				? new PropertyCondition(AutomationElement.ControlTypeProperty, controlType)
				: Condition.TrueCondition;

			var children = parent.FindAll(TreeScope.Children, condition);
			if (children.Count == 0)
				Console.WriteLine("Warning: Unable to find child elements with specified conditions.");

			return children.Cast<AutomationElement>().ToList();
		}

		/// <summary>
		/// Finds the first child element matching the specified conditions.
		/// </summary>
		public static AutomationElement FindFirstChild(
			AutomationElement parent,
			ControlType controlType = null,
			string className = null,
			string automationId = null,
			int? processId = null)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			var conditions = new List<Condition>();

			if (controlType != null)
				conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));

			if (className != null)
				conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty, className));

			if (automationId != null)
				conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

			if (processId.HasValue)
				conditions.Add(new PropertyCondition(AutomationElement.ProcessIdProperty, processId.Value));

			Condition condition;
			if (conditions.Count == 0)
			{
				condition = Condition.TrueCondition;
			}
			else if (conditions.Count == 1)
			{
				condition = conditions.First();
			}
			else
			{
				condition = new AndCondition(conditions.ToArray());
			}

			var child = parent.FindFirst(TreeScope.Children, condition);
			if (child == null)
				Console.WriteLine("Error: Unable to find child element with specified conditions.");

			return child;
		}

		/// <summary>
		/// Retrieves all child controls and their paths.
		/// </summary>
		public static Dictionary<AutomationElement, string> GetAll(AutomationElement control, string path, bool includeTop = false)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			var controls = new Dictionary<AutomationElement, string>();

			if (includeTop && !controls.ContainsKey(control))
				controls.Add(control, path);

			var children = FindAllChildren(control);
			foreach (var child in children)
			{
				var childPath = $"{path}.{child.Current.ControlType.ProgrammaticName.Split('.').Last()}";
				var childControls = GetAll(child, childPath, true);

				foreach (var kvp in childControls)
				{
					if (!controls.ContainsKey(kvp.Key))
						controls.Add(kvp.Key, kvp.Value);
				}
			}

			return controls;
		}

		#endregion

		#region Private Helper Methods

		private static List<Tuple<string, Dictionary<string, string>>> SplitXPath(string path)
		{
			var segments = new List<Tuple<string, Dictionary<string, string>>>();
			var parts = path.Trim('/').Split('/');

			foreach (var part in parts)
			{
				var match = Regex.Match(part, @"^(?<controlType>\w+)(\[(?<predicates>.+?)\])?$");

				if (match.Success)
				{
					var controlTypeName = match.Groups["controlType"].Value;
					var predicateStr = match.Groups["predicates"].Value;
					var predicates = ParsePredicates(predicateStr);
					segments.Add(Tuple.Create(controlTypeName, predicates));
				}
			}

			return segments;
		}

		private static Dictionary<string, string> ParsePredicates(string predicateStr)
		{
			var predicates = new Dictionary<string, string>();

			if (string.IsNullOrEmpty(predicateStr))
				return predicates;

			var parts = predicateStr.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var part in parts)
			{
				var kvpMatch = Regex.Match(part, @"@(?<key>\w+)=['""](?<value>.*?)['""]");
				if (kvpMatch.Success)
				{
					var key = kvpMatch.Groups["key"].Value;
					var value = UnescapeXPathValue(kvpMatch.Groups["value"].Value);
					predicates[key] = value;
				}
				else if (part.StartsWith("position()="))
				{
					var positionValue = part.Substring("position()=".Length);
					predicates["position()"] = positionValue;
				}
			}

			return predicates;
		}

		private static ControlType GetControlTypeByName(string typeName)
		{
			var fields = typeof(ControlType).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			foreach (var field in fields)
			{
				if (field.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
					return (ControlType)field.GetValue(null);
			}

			return ControlType.Custom;
		}

		private static int GetChildIndex(AutomationElement element)
		{
			var parent = TreeWalker.RawViewWalker.GetParent(element);
			if (parent == null)
				return 0;  // Root element
						   // Use the same conditions used when retrieving siblings
			var siblings = parent.FindAll(TreeScope.Children, Condition.TrueCondition);
			int index = 0;
			foreach (AutomationElement sibling in siblings)
			{
				if (element.Equals(sibling))
					return index;
				index++;
			}
			return -1; // Should not reach here
		}

		private static bool ShouldIncludeNameInPath(AutomationElement element)
		{
			var controlType = element.Current.ControlType;
			var className = element.Current.ClassName;
			var name = element.Current.Name;

			// Exclude controls that support ValuePattern or TextPattern
			// These controls often use Name for content rather than identification
			if (element.TryGetCurrentPattern(ValuePattern.Pattern, out _) ||
				element.TryGetCurrentPattern(TextPattern.Pattern, out _))
			{
				return false;
			}

			// Exclude specific control types that are known to use Name for content
			if (controlType == ControlType.Document || controlType == ControlType.Edit)
			{
				return false;
			}

			// Exclude specific classes known to use Name for content (like Scintilla)
			if (className == "Scintilla")
			{
				return false;
			}

			// If Name is null or empty, don't include it
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}

			// Include Name for other control types
			return true;
		}

		// Escapes single quotes in XPath values
		private static string EscapeXPathValue(string value)
		{
			return value.Replace("'", "&apos;");
		}

		private static string UnescapeXPathValue(string value)
		{
			return value.Replace("&apos;", "'");
		}

		#endregion

		#region Native Methods

		internal static class NativeMethods
		{

			[DllImport("user32.dll")]
			internal static extern IntPtr GetDesktopWindow();

			[DllImport("user32.dll")]
			internal static extern bool SetCursorPos(int x, int y);

			[DllImport("user32.dll")]
			internal static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

			public const int MOUSEEVENTF_LEFTDOWN = 0x02;
			public const int MOUSEEVENTF_LEFTUP = 0x04;
			public const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
			public const uint MOUSEEVENTF_RIGHTUP = 0x10;
		}

		#endregion

		#region Manage Content

		// P/Invoke declarations
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
			uint dwSize, uint flAllocationType, uint flProtect);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
			uint dwSize, uint dwFreeType);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess,
			IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteProcessMemory(IntPtr hProcess,
			IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out IntPtr lpNumberOfBytesWritten);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		// Process access rights
		private const uint PROCESS_VM_OPERATION = 0x0008;
		private const uint PROCESS_VM_READ = 0x0010;
		private const uint PROCESS_VM_WRITE = 0x0020;
		private const uint PROCESS_QUERY_INFORMATION = 0x0400;

		// Memory allocation constants
		private const uint MEM_COMMIT = 0x1000;
		private const uint MEM_RESERVE = 0x2000;
		private const uint MEM_RELEASE = 0x8000;
		private const uint PAGE_READWRITE = 0x04;

		// Scintilla message constants
		private const uint SCI_GETTEXT = 2182;
		private const uint SCI_GETTEXTLENGTH = 2183;
		private const uint SCI_SETTEXT = 2181;

		// Other constants
		private const uint WM_GETTEXTLENGTH = 0x000E;
		private const uint WM_GETTEXT = 0x000D;

		public static string GetValue(AutomationElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			// Try ValuePattern first
			if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object vp))
			{
				var valuePattern = (ValuePattern)vp;
				return valuePattern.Current.Value;
			}
			// Try TextPattern
			else if (element.TryGetCurrentPattern(TextPattern.Pattern, out object tp))
			{
				var textPattern = (TextPattern)tp;
				var documentRange = textPattern.DocumentRange;
				return documentRange.GetText(-1).TrimEnd('\r', '\n');
			}
			else if (element.Current.ClassName == "Scintilla")
			{
				// Handle Scintilla control in another process
				IntPtr hwnd = new IntPtr(element.Current.NativeWindowHandle);

				uint processId;
				GetWindowThreadProcessId(hwnd, out processId);

				// Open the target process
				IntPtr hProcess = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, processId);
				if (hProcess == IntPtr.Zero)
					throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to open target process.");

				try
				{
					// Get the length of the text
					int length = SendMessage(hwnd, SCI_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero).ToInt32();

					// Allocate memory in the target process
					IntPtr remoteBuffer = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)(length + 1), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
					if (remoteBuffer == IntPtr.Zero)
						throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to allocate memory in target process.");

					try
					{
						// Send the SCI_GETTEXT message to the Scintilla control
						IntPtr result = SendMessage(hwnd, SCI_GETTEXT, new IntPtr(length + 1), remoteBuffer);

						if (result.ToInt64() == 0)
							throw new InvalidOperationException("Failed to retrieve text from Scintilla control.");

						// Create a buffer in our process to receive the text
						byte[] localBuffer = new byte[length];

						// Read the text from the target process's memory
						IntPtr bytesRead;
						bool success = ReadProcessMemory(hProcess, remoteBuffer, localBuffer, (uint)length, out bytesRead);
						if (!success || bytesRead.ToInt32() != length)
							throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to read process memory.");

						// Convert the bytes to string using UTF-8 encoding
						string text = Encoding.UTF8.GetString(localBuffer);

						return text;
					}
					finally
					{
						// Free the memory allocated in the target process
						VirtualFreeEx(hProcess, remoteBuffer, 0, MEM_RELEASE);
					}
				}
				finally
				{
					// Close the handle to the target process
					CloseHandle(hProcess);
				}
			}
			else
			{
				throw new InvalidOperationException("The control does not support ValuePattern or TextPattern.");
			}
		}

		[STAThread]
		public void SetValue(AutomationElement element, string value)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			// Try ValuePattern first
			if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object vp))
			{
				var valuePattern = (ValuePattern)vp;
				valuePattern.SetValue(value);
			}
			else if (element.Current.ClassName == "Scintilla")
			{
				// Handle Scintilla control in another process
				IntPtr hwnd = new IntPtr(element.Current.NativeWindowHandle);

				uint processId;
				GetWindowThreadProcessId(hwnd, out processId);

				// Open the target process
				IntPtr hProcess = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION, false, processId);
				if (hProcess == IntPtr.Zero)
					throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to open target process.");

				try
				{
					// Prepare the text to write (null-terminated UTF-8)
					byte[] textBytes = Encoding.UTF8.GetBytes(value + "\0");
					int length = textBytes.Length;

					// Allocate memory in the target process
					IntPtr remoteBuffer = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
					if (remoteBuffer == IntPtr.Zero)
						throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to allocate memory in target process.");

					try
					{
						// Write the text to the allocated memory in the target process
						IntPtr bytesWritten;
						bool success = WriteProcessMemory(hProcess, remoteBuffer, textBytes, (uint)length, out bytesWritten);
						if (!success || bytesWritten.ToInt32() != length)
							throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to write process memory.");

						// Send the SCI_SETTEXT message to the Scintilla control
						IntPtr result = SendMessage(hwnd, SCI_SETTEXT, IntPtr.Zero, remoteBuffer);

						if (result.ToInt64() == 0)
							throw new InvalidOperationException("Failed to set text in Scintilla control.");
					}
					finally
					{
						// Free the memory allocated in the target process
						VirtualFreeEx(hProcess, remoteBuffer, 0, MEM_RELEASE);
					}
				}
				finally
				{
					// Close the handle to the target process
					CloseHandle(hProcess);
				}
			}
			// If TextPattern is read-only, use key simulation
			else if (element.Current.IsKeyboardFocusable)
			{
				// Try to set focus and send keystrokes
				element.SetFocus();

				// Wait briefly to ensure the control has focus
				System.Threading.Thread.Sleep(100);

				// Clear existing text
				System.Windows.Forms.SendKeys.SendWait("^a"); // Ctrl+A to select all
				System.Windows.Forms.SendKeys.SendWait("{DEL}"); // Delete

				// Send the new text
				System.Windows.Forms.SendKeys.SendWait(value);
			}
			else
			{
				throw new InvalidOperationException("Cannot set value: control is not focusable and does not support ValuePattern.");
			}
		}

		public static string GetSelectedTextFromElement(AutomationElement element)
		{
			if (element == null)
				return null;

			try
			{
				// Try to get the TextPattern
				if (element.TryGetCurrentPattern(TextPattern.Pattern, out object textPatternObj))
				{
					var textPattern = (TextPattern)textPatternObj;
					var selection = textPattern.GetSelection();
					if (selection != null && selection.Length > 0)
					{
						return selection[0].GetText(-1).TrimEnd('\r', '\n');
					}
				}

				// For edit controls that support ValuePattern
				if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
				{
					var valuePattern = (ValuePattern)valuePatternObj;
					return valuePattern.Current.Value;
				}

				return null;
			}
			catch (Exception)
			{
				// Handle exceptions
				return null;
			}
		}


		#endregion

	}
}
