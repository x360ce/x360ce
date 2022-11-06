using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using JocysCom.WebSites.Engine.Security.Data;
using JocysCom.ClassLibrary.Web.Services;
using x360ce.Engine.Data;
using System.Windows.Controls;
using System.Windows;

namespace x360ce.Net48Test
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
				new[] {
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

		#region TestMemoryLeak

#if DEBUG
		static bool isDebug = true;
#else
		static bool isDebug = false;
#endif


		private static void Test<T>()
		{
			Test(typeof(T).Assembly, new[] { typeof(T) });
		}

		public static void Test(Assembly assembly, Type[] includeTypes = null, Type[] excludeTypes = null)
		{
			var results = TestMemoryLeakByAssembly(assembly, includeTypes, excludeTypes);
			var errors = results.Where(x => x.Level == TraceLevel.Error).ToList();
			var warnings = results.Where(x => x.Level == TraceLevel.Warning).ToList();
			var infoPass = results.Where(x => x.Level == TraceLevel.Info && !x.IsAlive).ToList();
			var infoFail = results.Where(x => x.Level == TraceLevel.Info && x.IsAlive).ToList();
			Console.WriteLine();
			Console.WriteLine($"Disposed: {infoPass.Count}, Dispose Failed: {infoFail.Count}");
			Console.WriteLine($"Warnings: {warnings.Count}, Dispose Errors: {errors.Count}");
			if (results.Count == 1)
				Console.WriteLine($"Duration: {results[0].Duration:#,##0} ms");
			// If more than one control was tested then...
			if (results.Count > 1)
			{
				// Recommend fixing the smallest control next, because
				// more likely that it does not contain other controls, but is used by other controls.
				var nextToFix = infoFail.OrderBy(x => x.MemObjectSize).FirstOrDefault();
				if (nextToFix != null)
				{
					Console.WriteLine();
					Console.WriteLine($"Smallest recommended control to fix: {nextToFix.Type.FullName}");
				}
			}
			Assert.IsTrue(infoPass.Count > 0);
			Assert.IsTrue(infoFail.Count == 0);
			Assert.IsTrue(warnings.Count == 0);
			Assert.IsTrue(errors.Count == 0);
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
				Debug.WriteLine(message);
				//Console.WriteLine(message);

			}
			return results;
		}

		public static void CollectGarbage()
		{
			// Try to remove object from the memory.
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			GC.WaitForFullGCComplete();
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
					var memBeforeCreate = GC.GetTotalMemory(false);
					var o = Activator.CreateInstance(type);
					result.MemObjectSize = GC.GetTotalMemory(false) - memBeforeCreate;
					var wr = new WeakReference(o);
					// If WeakReference point to the intended object instance then...
					if (!wr.Target.Equals(o))
					{
						result.Level = TraceLevel.Error;
						result.Message += "Wrong Type!";
					}
					if (o is Window ucw)
					{
					}
					else if (o is System.Windows.Controls.Control uc1)
					{
						try
						{

							var window = new System.Windows.Window();
							window.Content = uc1;
							window.Activate();
							window.Content = null;
							window = null;
							uc1 = null;
						}
						catch (Exception ex1)
						{
							var x1 = ex1;
							throw;
						}
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
					// loop untill object allive, but no longer than  seconds.
					while (wr.IsAlive && stopwatch.ElapsedMilliseconds < TestMaxDurationPerClassTest)
					{
						Task.Delay(100);
						CollectGarbage();
					}
					result.MemDifference = GC.GetTotalMemory(true) - memBeforeCreate;
					result.IsAlive = wr.IsAlive;
					// if dispose failed. Strong references are left to the wr.Target.
					result.Message += wr.IsAlive
						? "Dispose Failed!"
						: "Disposed";
				}
				catch (Exception ex)
				{
					if (isDebug)
						throw;
					result.Level = TraceLevel.Error;
					result.Message = ex.Message;
					result.Exception = ex;
				}
			}
			result.Duration = stopwatch.ElapsedMilliseconds;
			return result;
		}

		#endregion

	}
}
