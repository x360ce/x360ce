using System.Runtime;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using JocysCom.ClassLibrary.Controls;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.ComponentModel;
using System.Xml;

namespace x360ce.Tests
{

	/// <summary>
	/// Test if the controls can be disposed and garbage collected and don't result in memory leaks.
	/// IMPORTANT!!!: Enable "Code Optimize" option for memory leak (dispose) test to work:
	///		NET 4.8: Build \ Configuration: Debug \ [x] Optimise Code".
	///		NET 6.0: Build \ General \ Optimize Code: [x] Debug
	/// Note: Compiler is trying to be helpful and Debug build can keep values rooted even if
	/// you set them to null i.e. 'wr.IsAlive' will always return 'true'.
	/// </summary>
	public class MemoryLeakHelper
	{

		private static long TestMaxDurationPerClassTest = 5000;
		private static int TestWindowDisplayDelay = 1000;
		private static bool ThrowExceptionIfDebug = false;
		private static int SkipControls = 70; //70;

		private static Dictionary<Type, PropertyInfo[]> TypesWithContentProperty;

		private static Dictionary<Type, PropertyInfo[]> GetTypesWithContentProperty()
		{
			var results = new Dictionary<Type, PropertyInfo[]>();
			var types = typeof(Label).Assembly.GetTypes();
			foreach (var t in types)
			{
				var all = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				var props = all.Where(x =>
						(x.CanWrite && x.Name == nameof(ContentControl.Content)) ||
						// Setting data cintext to null could result in failing to dispose.
						//(x.CanWrite && x.Name == nameof(ContentControl.DataContext)) ||
						(x.CanWrite && typeof(UIElement).IsAssignableFrom(x.PropertyType) && x.Name == "Child") ||
						(typeof(UIElementCollection).IsAssignableFrom(x.PropertyType) && x.Name == "Children")
					)
					.OrderBy(x => x.Name)
					.ToArray();
				if (props.Any())
					results.Add(t, props);
			}
			return results;
		}


#if DEBUG
		static bool isDebug = true;
#else
		static bool isDebug = false;
#endif

		/// <summary>
		/// The main (main) application window never dispose until the application closes.
		/// The secondary window must have the main window as owner in order to be disposed out correctly.
		/// the secondary window will be used for dispose tests.
		/// </summary>
		private static System.Windows.Application MainApp;
		private static System.Windows.Window MainWindow;
		private static System.Windows.Controls.Label MainLabel;
		private static System.Windows.Controls.Label MainSubLabel;
		private static Thread MainThread;
		private static object MainWindowLock = new object();

		private static SemaphoreSlim MainWindowLoadedSemaphore;
		private static SemaphoreSlim ApplicationExitsSemaphore;

		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;
		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private static System.Timers.Timer _unloadTimer = new System.Timers.Timer(1000);

		private static void CheckMainWindow()
		{
			lock (MainWindowLock)
			{
				if (MainWindow != null)
					return;
				MainWindowLoadedSemaphore = new SemaphoreSlim(0);
				ApplicationExitsSemaphore = new SemaphoreSlim(0);

				Action isolator = () =>
				{
					// One app per app domain.
					if (MainApp == null)
#if NETCOREAPP
						MainApp = new System.Windows.Application();
#else
						// Load styles from resources.
						MainApp = new MemoryLeakApp();
#endif
					var mainWindow = new Window();
					var w = GetWindow(mainWindow, null, true, MainWindowLoadedSemaphore);
					// Create content control.
					var sp = new StackPanel();
					MainLabel = new System.Windows.Controls.Label() { Content = $"Test control: ..." };
					MainSubLabel = new System.Windows.Controls.Label() { };
					sp.Orientation = Orientation.Vertical;
					sp.Children.Add(MainLabel);
					sp.Children.Add(MainSubLabel);
					w.Content = sp;
					// Use weak reference events.
					WeakEventManager<Application, ExitEventArgs>.AddHandler(MainApp, nameof(MainApp.Exit), (sender, e) =>
					{
						Console.WriteLine($"Application {nameof(Application.Exit)}");
						ApplicationExitsSemaphore.Release();
					});
					MainWindow = w;
					MainApp.Run(MainWindow);
				};
				var ts = new System.Threading.ThreadStart(isolator);
				MainThread = new System.Threading.Thread(ts);
				MainThread.IsBackground = false;
				MainThread.SetApartmentState(ApartmentState.STA);
				MainThread.Start();
				MainWindowLoadedSemaphore.Wait();
			}
		}

		private static void W_Loaded(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		public static void Test<T>()
		{
			Test(typeof(T).Assembly, new Type[] { typeof(T) }, null);
		}

		public class ReferenceResults
		{
			public string Path { get; set; }
			public Type Type { get; set; }
			public WeakReference Reference { get; set; }
		}

		public static void Test(Assembly assembly, Type[] includeTypes = null, Type[] excludeTypes = null)
		{
			_unloadTimer.Stop();
			// Make sure that owner window exists.
			CheckMainWindow();
			TypesWithContentProperty = TypesWithContentProperty ?? GetTypesWithContentProperty();

			var mainWindowWr = new WeakReference(null);
			mainWindowWr.Target = MainWindow;
			var results = TestMemoryLeakByAssembly(assembly, includeTypes, excludeTypes);
			/*
			// Shutdow will terminate multiple tests.
			MainApp.Dispatcher.Invoke(() =>
			{
				MainApp.Shutdown();
			});
			// Wait until application exits.
			ApplicationExitsSemaphore.Wait();
			*/
			var errors = results.Where(x => x.Level == TraceLevel.Error).ToList();
			var warnings = results.Where(x => x.Level == TraceLevel.Warning).ToList();
			var passed = results.Where(x => x.Level == TraceLevel.Info && !x.IsAlive).ToList();
			var failed = results.Where(x => x.Level == TraceLevel.Info && x.IsAlive).ToList();
			Console.WriteLine();
			Console.WriteLine($"Passed: {passed.Count}");
			Console.WriteLine($"Failed: {failed.Count}");
			Console.WriteLine($"Errors: {errors.Count}");
			Console.WriteLine();
			Console.WriteLine($"Warnings: {warnings.Count}");
			if (results.Count == 1)
				Console.WriteLine($"Duration: {results[0].Duration:#,##0} ms");
			// If more than one control was tested then...
			if (results.Count > 1)
			{
				// Recommend fixing the smallest control next, because
				// more likely that it does not contain other controls, but is used by other controls.
				var nextToFix = failed.OrderBy(x => x.MemObjectSize).FirstOrDefault();
				if (nextToFix != null)
				{
					Console.WriteLine();
					Console.WriteLine($"Smallest recommended control to fix: {nextToFix.Type.FullName}");
				}
			}
			Assert.IsTrue(passed.Count > 0);
			Assert.IsTrue(failed.Count == 0);
			Assert.IsTrue(errors.Count == 0);
			// Make the test completing faster (immediately) by collecting garbage.
			// Wait a bit to allow the application to remove the remaining references.
			Task.Delay(100).Wait();
			CollectGarbage();
		}

		private static void MainApp_Exit(object sender, ExitEventArgs e)
		{
			throw new NotImplementedException();
		}

		public static List<MemoryTestResult> TestMemoryLeakByAssembly(Assembly assembly, Type[] includeTypes, Type[] excludeTypes)
		{
			var results = new List<MemoryTestResult>();
			// Test public non-abstracts classes only.
			var types = assembly.GetTypes()
				.Where(x => includeTypes == null || includeTypes.Length == 0 || includeTypes.Contains(x))
				.Where(x => excludeTypes == null || excludeTypes.Length == 0 || !excludeTypes.Contains(x))
				.Where(x => x.IsClass)
				.Where(x => x.IsPublic)
				.Where(x => !x.IsAbstract)
				.ToArray();
			var zeros = new string('0', types.Length.ToString().Length);
			var pad = "\r\n" + new string(' ', $"[----] {zeros}/{zeros}: ".Length);
			var skip = includeTypes?.Length == 1 ? 0 : SkipControls;
			for (int t = skip; t < types.Length; t++)
			{
				MainApp.Dispatcher.Invoke(() =>
				{
					MainLabel.Content = $"Test control: {t + 1}/{types.Length}";
				});
				var type = types[t];
				var result = TestType(type, includeTypes?.Length == 1);
				results.Add(result);
				var isSuccess = !result.IsAlive && result.Level == TraceLevel.Info;
				// If object was ddisposed without errors then continue
				if (isSuccess)
					continue;
				var status = isSuccess ? "[Pass]" : "[" + result.Level.ToString().Substring(0, 4) + "]";
				var index = string.Format("{0:" + zeros + "}/{1:" + zeros + "}", t + 1, types.Length);
				// Create main message.
				var message = $"{status} {index}: {type.FullName}";
				// Create extra messages lines.
				message += $"{pad}{result.Message}";
				if (result.MemObjectSize.HasValue || result.MemDifference.HasValue)
				{
					message += $"{pad}Object Size: {result.MemObjectSize:#,##0}";
					message += $", Memory Difference: {result.MemDifference:+#,##0;-#,##0;#,##0}";
					message += $", Duration: {result.Duration:#,##0} ms";
				}
				if (isDebug)
					Debug.WriteLine(message);
				else
					Console.WriteLine(message);
			}
			return results;
		}

		//[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
		private static MemoryTestResult TestType(Type type, bool logMoreDetails)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = new MemoryTestResult();
			result.Type = type;
			var constructor = type.GetConstructor(Type.EmptyTypes);
			if (type.ContainsGenericParameters)
			{
				result.Level = TraceLevel.Warning;
				result.Message += "Requires generic parameters to create instance";
			}
			else if (constructor == null)
			{
				result.Level = TraceLevel.Warning;
				result.Message += "Requires parameters to create instance";
			}
			else
			{
				CollectGarbage();
				try
				{
					var testControlWr = new WeakReference(null);
					var testWindowWr = new WeakReference(null);
					var mainWindowWr = new WeakReference(null);
					long memBeforeCreate = 0;
					Action isolator = () =>
					{
						memBeforeCreate = GC.GetTotalMemory(false);
						var o = Activator.CreateInstance(type);
						result.MemObjectSize = GC.GetTotalMemory(false) - memBeforeCreate;
						testControlWr.Target = o;
						// If WeakReference point to the intended object instance then...
						if (!Equals(testControlWr.Target, o))
						{
							result.Level = TraceLevel.Error;
							result.Message += "Wrong Type!";
						}
						if (o is System.Windows.Window ucw)
						{
							var testLoadedSemaphore = new SemaphoreSlim(0);
							var testClosedSemaphore = new SemaphoreSlim(0);
							mainWindowWr.Target = MainWindow;
							// MainWindow will be set as owner to properly dispose after closing.
							var testWindow = GetWindow(ucw, MainWindow, logMoreDetails, testLoadedSemaphore, testClosedSemaphore);
							testWindow.Show();
							testLoadedSemaphore.Wait();
							Task.Delay(TestWindowDisplayDelay).Wait();
							testWindow.Close();
							testClosedSemaphore.Wait();
						}
						else if (o is System.Windows.FrameworkElement uc1)
						{
							var testLoadedSemaphore = new SemaphoreSlim(0);
							var testClosedSemaphore = new SemaphoreSlim(0);
							mainWindowWr.Target = MainWindow;
							var parentWindow = new Window();
							var testWindow = GetWindow(parentWindow, MainWindow, logMoreDetails, testLoadedSemaphore, testClosedSemaphore);
							// Create content control.
							var sp = new StackPanel()
							{
								Orientation = Orientation.Vertical,
								Margin = new Thickness(8),
							};
							sp.Children.Add(new Label() { Content = "Test Control:" });
							sp.Children.Add(uc1);
							testWindow.Content = sp;
							// Control events.
							WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(uc1, nameof(uc1.Loaded), (sender, e) =>
							{
								if (logMoreDetails)
									Console.WriteLine("    Test control loaded");
							});
							WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(uc1, nameof(uc1.Unloaded), (sender, e) =>
							{
								if (logMoreDetails)
									Console.WriteLine("    Test control unloaded");
							});
							testWindow.Show();
							testLoadedSemaphore.Wait();
							Task.Delay(TestWindowDisplayDelay).Wait();
							testWindow.Close();
							testClosedSemaphore.Wait();
						}
						else if (o is System.Windows.Forms.UserControl uc2)
						{
							uc2.Dispose();
							uc2 = null;
						}
						else if (o is IDisposable uc3)
						{
							uc3.Dispose();
							uc3 = null;
						}
						// Trigger object dispose.
						o = null;
					};
					// Create new window on the same thread.
					MainWindow.Dispatcher.Invoke(isolator);
					// Try to re-collect for some time if object is alive.
					CollectGarbage(() => testControlWr.IsAlive);
					result.MemDifference = GC.GetTotalMemory(true) - memBeforeCreate;
					result.IsAlive = testControlWr.IsAlive;
					// if dispose failed. Strong references are left to the wr.Target.
					result.Message += testControlWr.IsAlive
						? "Dispose Failed!"
						: "Disposed";
					if (logMoreDetails)
					{
						Console.WriteLine($"Main Window  IsAlive: {mainWindowWr.IsAlive}");
						Console.WriteLine($"Test Window  IsAlive: {testWindowWr.IsAlive}");
						Console.WriteLine($"Test Control IsAlive: {testControlWr.IsAlive}");
						// If control is still alive then...
						// Note: seems like controls with "x:Names" fail to dispose.
						// If control is dependency object then...
						if (testControlWr.IsAlive && testControlWr.Target is DependencyObject dpo)
						{
							// Create list of weak references from all objects.
							List<ReferenceResults> references = null;
							MainWindow.Dispatcher.Invoke(new Action(() =>
							{
								var arr = GetAllWeakReferences(dpo).ToArray();
								arr.Reverse();
								references = arr.ToList();
							}));
							ApplyDisposeCommand("Clear: FrameworkElement", references, (x) =>
							{
								if (!(x.Reference.Target is FrameworkElement o))
									return;
								o.Style = null;
								o.Resources?.Clear();
								o.DataContext = null;
								if (!string.IsNullOrEmpty(o.Name) && o.Parent is FrameworkContentElement parentContent)
								{
									Console.WriteLine($"\tUnregisterName: {o.Name}");
									parentContent.UnregisterName(o.Name);
								}
								if (!string.IsNullOrEmpty(o.Name) && o.Parent is FrameworkElement parent)
								{
									Console.WriteLine($"\tUnregisterName: {o.Name}");
									parent.UnregisterName(o.Name);
								}
							});
							ApplyDisposeCommand("Clear: FrameworkContentElement", references, (x) =>
							{
								if (!(x.Reference.Target is FrameworkContentElement o))
									return;
								o.Style = null;
								o.Resources?.Clear();
								o.DataContext = null;
								if (!string.IsNullOrEmpty(o.Name) && o.Parent is FrameworkContentElement parentContent)
								{
									Console.WriteLine($"\tUnregisterName: {o.Name}");
									parentContent.UnregisterName(o.Name);
								}
								if (!string.IsNullOrEmpty(o.Name) && o.Parent is FrameworkElement parent)
								{
									Console.WriteLine($"\tUnregisterName: {o.Name}");
									parent.UnregisterName(o.Name);
								}

							});
							ApplyDisposeCommand("Set FrameworkElement.Style to null", references, (x) =>
							{
								if (!(x.Reference.Target is FrameworkElement o))
									return;
								// Remove style.
								o.Style = null;
							});
							ApplyDisposeCommand("Clear FrameworkElement.Resources", references, (x) =>
							{
								if (!(x.Reference.Target is FrameworkElement o))
									return;
								// Clear resources.
								o.Resources.Clear();
							});
							ApplyDisposeCommand("Clear All Bindings", references, (x) =>
							{
								if (!(x.Reference.Target is DependencyObject o))
									return;
								BindingOperations.ClearAllBindings(o);
							});
							ApplyDisposeCommand("Clear Dependency Property Values", references, (x) =>
							{
								if (!(x.Reference.Target is DependencyObject o))
									return;
								var dprops = GetAttachedProperties(o);
								foreach (var dprop in dprops)
								{
									if (!dprop.ReadOnly)
										o.ClearValue(dprop);
								}
							});
							ApplyDisposeCommand("Dispose Object", references, (x) =>
							{
								if (!(x.Reference.Target is DependencyObject o))
									return;
								DisposeObject(o);
							});
							ApplyDisposeCommand("Clear: Grid : Panel", references, (x) =>
							{
								if (!(x.Reference.Target is Grid o))
									return;
								o.RowDefinitions.Clear();
								o.ColumnDefinitions.Clear();
							});
							ApplyDisposeCommand("Clear: Panel: FrameworkElement", references, (x) =>
							{
								if (!(x.Reference.Target is Panel o))
									return;
								o.Children.Clear();
							});
							ApplyDisposeCommand("Clear: ItemsControl : Control", references, (x) =>
							{
								if (!(x.Reference.Target is ItemsControl o))
									return;
								o.Items.Clear();
								o.ItemsSource = null;
							});
							ApplyDisposeCommand("Clear: Control : FrameworkElement", references, (x) =>
							{
								if (!(x.Reference.Target is Control o))
									return;
								o.Template = null;
							});
							ApplyDisposeCommand("Dispose : IDisposable", references, (x) =>
							{
								if (!(x.Reference.Target is IDisposable o))
									return;
								o.Dispose();
							});
							MainWindow.Dispatcher.Invoke(new Action(() =>
							{
								MainSubLabel.Content = "";
							}));
							var childCount = references.Count();
							var childFailCount = references.Count(x => x.Reference.IsAlive);
							var childPassCount = references.Count(x => !x.Reference.IsAlive);
							Console.WriteLine($"Child Controls: Count = {childCount}, Fail = {childFailCount}, Pass = {childPassCount}");
							var rx = new Regex("[^.]+[.]+");
							foreach (var item in references)
							{
								// Make control path smaller.
								var path = rx.Replace(item.Path, "\t");
								var id = path.Contains(" ") ? "+" : " ";
								Console.WriteLine(id + " " + (item.Reference.IsAlive ? "Fail" : "----") + $" {path}");
							}
						}
					}
				}
				catch (Exception ex)
				{
					//Debug.WriteLine($"Type: {type.FullName}");
					//Console.WriteLine($"Type: {type.FullName}");
					if (isDebug && ThrowExceptionIfDebug)
						throw;
					result.Level = TraceLevel.Error;
					result.Message = ex.Message;
					result.Exception = ex;
					Console.WriteLine(ex.ToString());
				}
			}
			result.Duration = stopwatch.ElapsedMilliseconds;
			return result;
		}

		private static void ApplyDisposeCommand(string title, List<ReferenceResults> items, Action<ReferenceResults> action)
		{
			Console.WriteLine(title);
			var error = "";
			var aliveItems = items.Where(x => x.Reference.IsAlive).ToList();
			MainWindow.Dispatcher.Invoke(new Action(() =>
			{
				MainSubLabel.Content = title;
				foreach (var item in items)
				{
					// Get control.
					DependencyObject control = item.Reference.Target as DependencyObject;
					// Skip if object is gone or control is null
					if (!item.Reference.IsAlive || control == null)
						continue;
					try
					{

						action(item);
					}
					catch (Exception ex2)
					{
						error = ex2.Message;
					}
					if (!string.IsNullOrEmpty(error))
						Console.WriteLine("!!!Error: " + error);
				}
			}));
			//CollectGarbage(() => items.Any(x => x.Reference.IsAlive));
			CollectGarbage();
			var disposedItems = aliveItems.Where(x => !x.Reference.IsAlive).ToList();
			if (disposedItems.Count > 0)
			{
				for (int i = 0; i < disposedItems.Count; i++)
				{
					var item = disposedItems[i];
					//Console.WriteLine($"  Disposed: {item.Path}");
				}
				Console.WriteLine($"  Disposed: {disposedItems.Count} control(s)");
			}
		}

		private static void _CollectGarbage()
		{
			// Try to remove object from the memory.
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			//GC.Collect();
			GC.WaitForFullGCComplete();
			GC.WaitForPendingFinalizers();
		}

		public static void CollectGarbage(Func<bool> whileCondition = null)
		{
			if (whileCondition == null)
			{
				_CollectGarbage();
				return;
			}
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			// loop untill object allive, but no longer than  seconds.
			while (whileCondition() && stopwatch.ElapsedMilliseconds < TestMaxDurationPerClassTest)
			{
				Task.Delay(200).Wait();
				_CollectGarbage();
			}
		}

		public static IList<DependencyProperty> GetAttachedProperties(DependencyObject obj)
		{
			var result = new List<DependencyProperty>();
			foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj,
				new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) }))
			{
				DependencyPropertyDescriptor dpd =
					DependencyPropertyDescriptor.FromProperty(pd);
				if (dpd != null)
					result.Add(dpd.DependencyProperty);
			}
			return result;
		}

		public static void DisposeObject(object o)
		{
			var contentType = TypesWithContentProperty.Keys.FirstOrDefault(x => x.IsAssignableFrom(o.GetType()));
			if (contentType != null)
			{
				var props = TypesWithContentProperty[contentType];
				foreach (var prop in props)
				{
					if (typeof(UIElement).IsAssignableFrom(prop.PropertyType))
						prop.SetValue(o, null);
					else if (typeof(UIElementCollection).IsAssignableFrom(prop.PropertyType))
						(prop.GetValue(o) as UIElementCollection)?.Clear();
					else if (prop.Name == nameof(ContentControl.Content))
						prop.SetValue(o, null);
					else if (prop.Name == nameof(ContentControl.DataContext))
						prop.SetValue(o, null);
				}
			}
		}

		/// <summary>
		/// Get list of weak references pointing to all chidren of main control.
		/// </summary>
		public static List<ReferenceResults> GetAllWeakReferences(DependencyObject o)
		{
			List<ReferenceResults> list = new List<ReferenceResults>();
			Action isolator = () =>
			{
				// Create list of weak references from all objects.
				var controls = ControlsHelper.GetAll("", o, null, true);
				list = controls.Select(x => new ReferenceResults()
				{
					Path = x.Key,
					Type = x.Value.GetType(),
					Reference = new WeakReference(x.Value),
				}).ToList();
				controls.Clear();
			};
			o.Dispatcher.Invoke(isolator);
			return list;
		}

		/// <summary>
		/// Get main app window or child test window.
		/// </summary>
		public static Window GetWindow(
			Window w,
			Window parentWindow,
			bool logMoreDetails,
			SemaphoreSlim loadedSemaphore = null,
			SemaphoreSlim closedSemaphore = null
		)
		{
			w = w ?? new Window();
			w.Background = SystemColors.ControlBrush;
			w.Topmost = true;
			w.IsHitTestVisible = false;
			w.SizeToContent = SizeToContent.WidthAndHeight;
			// If this is main window then...
			if (parentWindow == null)
			{
				w.Title = "Main Window";
			}
			else
			{
				w.Title = "Test Window";
				// Owner must be set to properly dispose after closing.
				w.Owner = parentWindow;
				w.Top = parentWindow.Top + parentWindow.ActualHeight;
				w.Left = parentWindow.Left;
			}
			var prefix = parentWindow == null ? "" : "  ";
			w.Loaded += (sender, e) =>
			{
				// If main window.
				if (w.Owner == null)
				{
					// Hide title buttons.
					var hwnd = new WindowInteropHelper(w).Handle;
					SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
				}
				if (logMoreDetails)
					Console.WriteLine($"{prefix}{w.Title} loaded");
				if (loadedSemaphore != null)
					loadedSemaphore.Release();
			};
			w.Unloaded += (sender, e) =>
			{
				if (logMoreDetails)
					Console.WriteLine($"{prefix}{w.Title} unloaded");
			};
			w.Closed += (sender, e) =>
			{
				if (logMoreDetails)
					Console.WriteLine($"{prefix}{w.Title} closed");
				if (closedSemaphore != null)
					closedSemaphore.Release();
			};
			return w;
		}

		public static void ExtractDefaultStyle<T>(string extractPath = null)
		{
			var control = Application.Current.FindResource(typeof(T));
			var path = extractPath ?? $"\\Temp\\{typeof(T).Name}_DefaultStyleTemplate.xml";
			using (var writer = new XmlTextWriter(path, System.Text.Encoding.UTF8))
			{
				writer.Formatting = Formatting.Indented;
				System.Windows.Markup.XamlWriter.Save(control, writer);
			}
		}

	}
}
