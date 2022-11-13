using System.Runtime;
using System.Reflection;
using System.Diagnostics;
using JocysCom.WebSites.Engine.Security.Data;
using JocysCom.ClassLibrary.Web.Services;
using x360ce.Engine.Data;
using System.Windows.Controls;
using System.Windows;
#if NETCOREAPP
#else
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
#endif
#if NETCOREAPP
namespace x360ce.Net60Test
#else
namespace x360ce.Net48Test
#endif
{

	/// <summary>
	/// Test if the controls can be disposed and garbage collected and don't result in memory leaks.
	/// IMPORTANT!!!: Enable "Code Optimize" option for memory leak (dispose) test to work:
	///		NET 4.8: Build \ Configuration: Debug \ [x] Optimise Code".
	///		NET 6.0: Build \ General \ Optimize Code: [x] Debug
	/// Note: Compiler is trying to be helpful and Debug build can keep values rooted even if
	/// you set them to null i.e. 'wr.IsAlive' will always return 'true'.
	/// </summary>
	[TestClass]
	public class MemoryLeakTest
	{

		public const long TestMaxDurationPerClassTest = 5000;

		[TestMethod]
		public void Test_x360ce_App() =>
			Test(typeof(App.App).Assembly);

		[TestMethod]
		public void Test_x360ce_App_PadItem_AdvancedControl() =>
			Test<App.Controls.PadItem_AdvancedControl>();

		[TestMethod]
		public void Test_x360ce_Engine() =>
			Test(typeof(Engine.EngineHelper).Assembly,
				// Include types. null = Test all.
				null,
				// Exclude types.
				new Type[] {
					typeof(SecurityEntities),
					typeof(SoapHttpClientBase),
					typeof(x360ceModelContainer),
				});

		[TestMethod]
		public void Test_x360ce_Engine_IssuesUserControl() =>
			Test<JocysCom.ClassLibrary.Controls.IssuesControl.IssuesUserControl>();

		[TestMethod]
		public void Test_x360ce_Engine_IssuesControl() =>
			Test<JocysCom.ClassLibrary.Controls.IssuesControl.IssuesControl>();


		/// <summary>
		/// Simple ListView will be garbage collected successfully.
		/// </summary>
		[TestMethod]
		public void Test_ListView() =>
			Test<System.Windows.Controls.ListView>();

		/// <summary>
		/// Simple DataGrid fails garbage collection and leaks memory.
		/// </summary>
		[TestMethod]
		public void Test_DataGrid() =>
			Test<System.Windows.Controls.DataGrid>();

		[TestMethod]
		public void Test_TextBox() =>
			Test<System.Windows.Controls.TextBox>();

		[TestMethod]
		public void Test_StackPanel() =>
			Test<System.Windows.Controls.StackPanel>();



		#region TestMemoryLeak

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
		private static Thread MainThread;
		private static object MainWindowLock = new object();
		private static bool isMainWindowLoaded = false;
		private static bool isMainWindowUnloaded = false;
		private static bool isMainWindowClosed = false;

		private static void CheckMainWindow()
		{
			lock (MainWindowLock)
			{
				if (MainWindow != null)
					return;
				Action isolator = () =>
				{
					MainApp = new System.Windows.Application();
					var w = new System.Windows.Window();
					w.Topmost = true;
					w.Title = "Owner Window";
					w.Width = 100;
					w.Height = 32;
					//w.IsHitTestVisible = false;
					//w.ShowInTaskbar = false;
					//w.WindowState = WindowState.Minimized;
					w.SizeToContent = SizeToContent.WidthAndHeight;
					// Use weak reference events.
					RoutedEventHandler onLoaded = (sender, e) =>
					{
						//w.Dispatcher.BeginInvoke(new Action(() =>
						//{
						//	// Create control to add.
						//	var sp = new StackPanel();
						//	var label = new Label() { Content = "Main Window" };
						//	sp.Children.Add(label);
						//	w.Content = sp;
						//}));
						Console.WriteLine("Owner window loaded");
						isMainWindowLoaded = true;
					};
					WeakEventManager<Window, RoutedEventArgs>.AddHandler(w, nameof(Window.Loaded), new EventHandler<RoutedEventArgs>(onLoaded));
					// Use weak reference events.
					RoutedEventHandler onUnloaded = (sender, e) =>
					{
						Console.WriteLine("Owner window unloaded");
						isMainWindowUnloaded = true;
					};
					WeakEventManager<Window, RoutedEventArgs>.AddHandler(w, nameof(Window.Unloaded), new EventHandler<RoutedEventArgs>(onUnloaded));
					// Use weak reference events.
					EventHandler onClosed = (sender, e) =>
					{
						Console.WriteLine("Owner window closed");
						isMainWindowClosed = true;
					};
					WeakEventManager<Window, EventArgs>.AddHandler(w, nameof(Window.Closed), new EventHandler<EventArgs>(onClosed));
					w.Show();
					MainWindow = w;
					MainApp.Run(MainWindow);
				};
				var ts = new System.Threading.ThreadStart(isolator);
				MainThread = new System.Threading.Thread(ts);
				MainThread.IsBackground = false;
				MainThread.SetApartmentState(ApartmentState.STA);
				MainThread.Start();
				// Wait until window is loaded.
				while (!isMainWindowLoaded)
					Task.Delay(100);
			}
		}

		private static void W_Loaded(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private static void Test<T>()
		{
			Test(typeof(T).Assembly, new Type[] { typeof(T) });
		}

		public static void Test(Assembly assembly, Type[] includeTypes = null, Type[] excludeTypes = null)
		{
			// Make sure that owner window exists.
			CheckMainWindow();


			var results = TestMemoryLeakByAssembly(assembly, includeTypes, excludeTypes);
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

			MainApp.Dispatcher.Invoke(() =>
			{
				MainWindow.Close();
			});
			// Wait until window is unloaded and closed.
			while (!isMainWindowClosed)
				Task.Delay(100);
		}


		public static List<MemTestResult> TestMemoryLeakByAssembly(Assembly assembly, Type[] includeTypes, Type[] excludeTypes)
		{
			var results = new List<MemTestResult>();
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
			for (int t = 0; t < types.Length; t++)
			{
				var type = types[t];
				var result = TestType(type);
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

		public static void CollectGarbage()
		{
			// Try to remove object from the memory.
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			//GC.Collect();
			GC.WaitForFullGCComplete();
			GC.WaitForPendingFinalizers();
		}

		//[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
		private static MemTestResult TestType(Type type)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = new MemTestResult();
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
					var controlWr = new WeakReference(null);
					var testWindowWr = new WeakReference(null);
					var mainWindowWr = new WeakReference(null);
					long memBeforeCreate = 0;
					Action isolator = () =>
					{
						memBeforeCreate = GC.GetTotalMemory(false);
						var o = Activator.CreateInstance(type);
						result.MemObjectSize = GC.GetTotalMemory(false) - memBeforeCreate;
						controlWr.Target = o;
						// If WeakReference point to the intended object instance then...
						if (!Equals(controlWr.Target, o))
						{
							result.Level = TraceLevel.Error;
							result.Message += "Wrong Type!";
						}
						if (o is System.Windows.Window ucw)
						{
							Console.WriteLine("is Window");
						}
						else if (o is System.Windows.FrameworkElement uc1)
						{
							bool isTestLoaded = false;
							bool isTestClosed = false;
							Console.WriteLine("is FrameworkElement");
							mainWindowWr.Target = MainWindow;
							// Create control to add.
							var sp = new StackPanel();
							sp.Children.Add(uc1);
							var window = new System.Windows.Window();
							window.Owner = MainWindow;
							window.Topmost = true;
							window.Top = 100;
							window.Left = 100;
							window.Width = 100;
							window.Height = 100;
							//window.IsHitTestVisible = false;
							//window.ShowInTaskbar = false;
							//window.WindowState = WindowState.Minimized;

							window.Loaded += (sender, e) =>
							{
								Console.WriteLine("Test window loaded");
								isTestLoaded = true;
							};
							window.Unloaded += (sender, e) =>
							{
								Console.WriteLine("Test window unloaded");
							};
							window.Closing += (sender, e) =>
							{
								Console.WriteLine("Test window closing");
								isTestClosed = true;
							};
							window.Content = sp;
							window.Show();
							while (!isTestLoaded)
								Task.Delay(100).Wait();
							Task.Delay(1000).Wait();
							window.Close();
							while (!isTestClosed)
								Task.Delay(100).Wait();
							//window.Owner = null;
							//window.Content = null;
							Task.Delay(1000).Wait();
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

						if (o is DataGrid dg)
						{

						}
						// Trigger object dispose.
						o = null;
					};
					// Create new window on the same thread.
					MainWindow.Dispatcher.Invoke(isolator);
					// loop untill object allive, but no longer than  seconds.
					while (controlWr.IsAlive && stopwatch.ElapsedMilliseconds < TestMaxDurationPerClassTest)
					{
						Task.Delay(200).Wait();
						CollectGarbage();
						//Console.WriteLine("Collect Garbage");
					}
					result.MemDifference = GC.GetTotalMemory(true) - memBeforeCreate;
					result.IsAlive = controlWr.IsAlive;
					// if dispose failed. Strong references are left to the wr.Target.
					result.Message += controlWr.IsAlive
						? "Dispose Failed!"
						: "Disposed";
					Console.WriteLine($"Control IsAlive: {controlWr.IsAlive}");
					Console.WriteLine($"Main Window IsAlive: {mainWindowWr.IsAlive}");
					Console.WriteLine($"Test Window IsAlive: {testWindowWr.IsAlive}");
				}
				catch (Exception ex)
				{
					if (isDebug)
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

		#endregion

	}
}
